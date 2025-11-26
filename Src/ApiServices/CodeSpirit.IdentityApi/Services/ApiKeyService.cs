#nullable enable
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.ApiKey;
using CodeSpirit.Shared.Repositories;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// API密钥服务实现
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository<ApiKey> _repository;
    private readonly IMapper _mapper;
    private readonly IIdGenerator _idGenerator;
    private readonly ILogger<ApiKeyService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ApiKeyService(
        ApplicationDbContext context,
        IRepository<ApiKey> repository,
        IMapper mapper,
        IIdGenerator idGenerator,
        ILogger<ApiKeyService> logger)
    {
        _context = context;
        _repository = repository;
        _mapper = mapper;
        _idGenerator = idGenerator;
        _logger = logger;
    }

    /// <summary>
    /// 创建API密钥
    /// </summary>
    public async Task<CreateApiKeyResultDto> CreateAsync(CreateApiKeyDto createDto, long currentUserId, string tenantId)
    {
        // 确定目标用户ID：如果DTO中指定了用户，则使用指定的用户；否则使用当前用户
        var targetUserId = createDto.UserId ?? currentUserId;

        // 如果指定了其他用户，验证该用户是否存在且属于同一租户
        if (createDto.UserId.HasValue && createDto.UserId.Value != currentUserId)
        {
            var targetUser = await _context.Users
                .Where(u => u.Id == createDto.UserId.Value && u.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (targetUser == null)
            {
                throw new BusinessException("指定的用户不存在或不属于当前租户。");
            }

            _logger.LogInformation("用户 {CurrentUserId} 为用户 {TargetUserId} 创建API密钥：{Name}", 
                currentUserId, targetUserId, createDto.Name);
        }
        else
        {
            _logger.LogInformation("用户 {UserId} 创建API密钥：{Name}", targetUserId, createDto.Name);
        }

        // 生成API密钥（格式：sk_{32个随机字符}）
        var apiKey = GenerateApiKey();
        var prefix = apiKey.Substring(0, 7); // sk_ + 前4个字符
        var keyHash = ComputeSha256Hash(apiKey);

        // 创建实体
        var entity = _mapper.Map<ApiKey>(createDto);
        entity.Id = _idGenerator.NewId();
        entity.UserId = targetUserId;
        entity.TenantId = tenantId;
        entity.Prefix = prefix;
        entity.KeyHash = keyHash;
        entity.IsActive = true;
        entity.CreatedBy = currentUserId;
        entity.CreatedAt = DateTime.UtcNow;

        // 保存到数据库
        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync();

        // 映射为返回结果
        var result = _mapper.Map<CreateApiKeyResultDto>(entity);
        result.ApiKey = apiKey; // 设置明文密钥（仅此一次）

        _logger.LogInformation("API密钥创建成功：ID={ApiKeyId}, Prefix={Prefix}, UserId={UserId}", 
            entity.Id, prefix, targetUserId);

        return result;
    }

    /// <summary>
    /// 验证API密钥
    /// </summary>
    public async Task<ApiKeyValidationDto?> ValidateAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("sk_"))
        {
            _logger.LogWarning("API密钥格式无效：{ApiKey}", apiKey);
            return null;
        }

        var keyHash = ComputeSha256Hash(apiKey);

        // 查询 ApiKey，并包含用户、用户角色信息
        var entity = await _context.ApiKeys
            .Include(k => k.User)
                .ThenInclude(u => u!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Where(k => k.KeyHash == keyHash && k.IsActive && !k.IsDeleted)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            _logger.LogWarning("API密钥验证失败：密钥不存在或已禁用");
            return null;
        }

        // 检查是否过期
        if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("API密钥已过期：ID={ApiKeyId}", entity.Id);
            return null;
        }

        // 查询租户名称
        var tenant = await _context.Tenants
            .Where(t => t.Id == entity.TenantId)
            .Select(t => new { t.Id, t.Name })
            .FirstOrDefaultAsync();

        // 构建角色列表
        string? rolesJson = null;
        if (entity.User?.UserRoles != null && entity.User.UserRoles.Any())
        {
            var roleNames = entity.User.UserRoles
                .Where(ur => ur.Role != null && ur.TenantId == entity.TenantId) // 只包含当前租户的角色
                .Select(ur => ur.Role!.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            if (roleNames.Any())
            {
                rolesJson = System.Text.Json.JsonSerializer.Serialize(roleNames);
            }
        }

        // 更新最后使用时间
        entity.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // 构建验证结果 DTO
        var result = new ApiKeyValidationDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TenantName = tenant?.Name,
            UserId = entity.UserId,
            User = entity.User != null ? new ApiKeyUserDto
            {
                Id = entity.User.Id.ToString(),
                UserName = entity.User.UserName ?? string.Empty,
                Name = entity.User.Name
            } : null,
            Roles = rolesJson,
            Permissions = entity.Permissions,
            LastUsedAt = entity.LastUsedAt
        };

        _logger.LogInformation("API密钥验证成功：ID={ApiKeyId}, UserId={UserId}, TenantId={TenantId}, TenantName={TenantName}, RoleCount={RoleCount}", 
            result.Id, result.UserId, result.TenantId, result.TenantName, 
            rolesJson != null ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(rolesJson)?.Count ?? 0 : 0);

        return result;
    }

    /// <summary>
    /// 获取API密钥列表
    /// </summary>
    public async Task<PageList<ApiKeyDto>> GetListAsync(ApiKeyQueryDto queryDto, long userId)
    {
        var predicate = PredicateBuilder.New<ApiKey>(true);

        // 只能查询自己的API密钥
        predicate = predicate.And(k => k.UserId == userId);

        // 应用搜索条件
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            var keyword = queryDto.Keywords.Trim().ToLower();
            predicate = predicate.And(k => k.Name.ToLower().Contains(keyword) || 
                                          (k.Description != null && k.Description.ToLower().Contains(keyword)));
        }

        if (queryDto.IsActive.HasValue)
        {
            predicate = predicate.And(k => k.IsActive == queryDto.IsActive.Value);
        }

        if (queryDto.IsExpired.HasValue)
        {
            if (queryDto.IsExpired.Value)
            {
                predicate = predicate.And(k => k.ExpiresAt.HasValue && k.ExpiresAt.Value < DateTimeOffset.UtcNow);
            }
            else
            {
                predicate = predicate.And(k => !k.ExpiresAt.HasValue || k.ExpiresAt.Value >= DateTimeOffset.UtcNow);
            }
        }

        var query = _repository.CreateQuery().Where(predicate);

        // 应用排序
        query = queryDto.OrderDir?.ToLower() == "asc"
            ? query.OrderBy(k => k.CreatedAt)
            : query.OrderByDescending(k => k.CreatedAt);

        // 分页
        var total = await query.CountAsync();
        var items = await query
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        var dtos = _mapper.Map<List<ApiKeyDto>>(items);

        return new PageList<ApiKeyDto>(dtos, total);
    }

    /// <summary>
    /// 获取API密钥详情
    /// </summary>
    public async Task<ApiKeyDto> GetAsync(long id, long userId)
    {
        var entity = await _context.ApiKeys
            .Where(k => k.Id == id && k.UserId == userId)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new BusinessException("API密钥不存在或无权访问。");
        }

        return _mapper.Map<ApiKeyDto>(entity);
    }

    /// <summary>
    /// 更新API密钥
    /// </summary>
    public async Task UpdateAsync(long id, UpdateApiKeyDto updateDto, long userId)
    {
        var entity = await _context.ApiKeys
            .Where(k => k.Id == id && k.UserId == userId)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new BusinessException("API密钥不存在或无权访问。");
        }

        entity.Name = updateDto.Name;
        entity.Description = updateDto.Description;
        entity.ExpiresAt = updateDto.ExpiresAt;
        entity.IsActive = updateDto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("API密钥已更新：ID={ApiKeyId}", id);
    }

    /// <summary>
    /// 删除API密钥
    /// </summary>
    public async Task DeleteAsync(long id, long userId)
    {
        var entity = await _context.ApiKeys
            .Where(k => k.Id == id && k.UserId == userId)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new BusinessException("API密钥不存在或无权访问。");
        }

        _context.ApiKeys.Remove(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API密钥已删除：ID={ApiKeyId}", id);
    }

    /// <summary>
    /// 撤销API密钥（软删除）
    /// </summary>
    public async Task RevokeAsync(long id, long userId)
    {
        var entity = await _context.ApiKeys
            .Where(k => k.Id == id && k.UserId == userId)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new BusinessException("API密钥不存在或无权访问。");
        }

        entity.IsActive = false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("API密钥已撤销：ID={ApiKeyId}", id);
    }

    /// <summary>
    /// 生成随机API密钥
    /// </summary>
    private static string GenerateApiKey()
    {
        const int keyLength = 32; // 32 bytes
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[keyLength];
        rng.GetBytes(bytes);
        var base64 = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
        return "sk_" + base64;
    }

    /// <summary>
    /// 重新生成API密钥
    /// </summary>
    public async Task<CreateApiKeyResultDto> RegenerateAsync(long id, long userId, string tenantId)
    {
        var entity = await _context.ApiKeys
            .Where(k => k.Id == id && k.UserId == userId && k.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new BusinessException("API密钥不存在或无权访问。");
        }

        _logger.LogInformation("用户 {UserId} 重新生成API密钥：ID={ApiKeyId}, Name={Name}", 
            userId, id, entity.Name);

        // 生成新的API密钥
        var apiKey = GenerateApiKey();
        var prefix = apiKey.Substring(0, 7); // sk_ + 前4个字符
        var keyHash = ComputeSha256Hash(apiKey);

        // 更新实体
        entity.Prefix = prefix;
        entity.KeyHash = keyHash;
        entity.IsActive = true;
        entity.IsDeleted = false;
        entity.LastUsedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        entity.DeletedAt = null;
        entity.DeletedBy = null;

        await _context.SaveChangesAsync();

        // 映射为返回结果
        var result = _mapper.Map<CreateApiKeyResultDto>(entity);
        result.ApiKey = apiKey; // 设置明文密钥（仅此一次）

        _logger.LogInformation("API密钥重新生成成功：ID={ApiKeyId}, NewPrefix={Prefix}", 
            entity.Id, prefix);

        return result;
    }

    /// <summary>
    /// 计算SHA256哈希
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

