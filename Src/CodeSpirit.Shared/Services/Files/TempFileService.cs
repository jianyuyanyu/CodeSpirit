using CodeSpirit.Shared.Services.Background.Dtos;
using CodeSpirit.Shared.Services.Files.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace CodeSpirit.Shared.Services.Files;

/// <summary>
/// 文件服务实现
/// </summary>
public class TempFileServiceImpl : ITempFileService
{
    private readonly ILogger<TempFileServiceImpl> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _distributedCache;
    private readonly string _fileStoragePath;
    
    // 缓存键前缀
    private const string ExportTaskKeyPrefix = "ExportTask:";
    private const string CachedFileKeyPrefix = "CachedFile:";
    private const string FileInfoKeyPrefix = "FileInfo:";
    
    // 缓存文件默认过期时间
    private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromHours(1);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="configuration">配置</param>
    /// <param name="distributedCache">分布式缓存</param>
    public TempFileServiceImpl(ILogger<TempFileServiceImpl> logger, IConfiguration configuration, IDistributedCache distributedCache)
    {
        _logger = logger;
        _configuration = configuration;
        _distributedCache = distributedCache;
        
        // 获取文件存储路径，默认为应用程序根目录下的FileStorage文件夹
        _fileStoragePath = _configuration["FileStorage:Path"] ?? 
                          Path.Combine(AppContext.BaseDirectory, "FileStorage");
        
        // 确保存储目录存在
        if (!Directory.Exists(_fileStoragePath))
        {
            Directory.CreateDirectory(_fileStoragePath);
            _logger.LogInformation("已创建文件存储目录: {Path}", _fileStoragePath);
        }
    }

    /// <summary>
    /// 将文件上传到分布式缓存
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>上传结果</returns>
    /// <exception cref="InvalidOperationException">当文件大小超过512M限制时抛出</exception>
    public async Task<FileUploadResult> UploadToCacheAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            // 检查文件大小限制 (512MB)
            const long maxFileSizeBytes = 512L * 1024 * 1024;
            
            if (fileStream.Length > maxFileSizeBytes)
            {
                var fileSizeMB = Math.Round((double)fileStream.Length / (1024 * 1024), 2);
                _logger.LogWarning("文件 {FileName} 大小为 {FileSize}MB，超过最大限制512MB", fileName, fileSizeMB);
                throw new InvalidOperationException($"文件大小({fileSizeMB}MB)超过最大限制(512MB)");
            }
            
            // 生成唯一的文件ID
            var fileId = Guid.NewGuid().ToString();
            
            // 确保文件名是安全的
            var safeFileName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
            var fileExtension = Path.GetExtension(fileName);
            var newFileName = $"{safeFileName}_{fileId}{fileExtension}";
            
            // 将文件内容读入内存
            using var memoryStream = new MemoryStream();
            fileStream.Seek(0, SeekOrigin.Begin);
            await fileStream.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();
            
            // 文件大小（字节）
            long fileSize = fileBytes.Length;
            
            // 存储文件内容到分布式缓存
            var cacheKey = GetCachedFileKey(fileId);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultCacheExpiration
            };
            
            await _distributedCache.SetAsync(cacheKey, fileBytes, options);
            
            // 保存文件信息到缓存
            var fileInfo = new TempFileInfo
            {
                FileId = fileId,
                FileName = newFileName,
                ContentType = contentType,
                FileSize = fileSize,
                CreatedTime = DateTime.UtcNow,
                IsInCache = true,
                CacheExpireTime = DateTime.UtcNow.Add(_defaultCacheExpiration)
            };
            
            await SaveFileInfoAsync(fileInfo);
            
            // 构建文件URL
            var fileUrl = $"/api/tempfiles/download/{fileId}";
            
            _logger.LogInformation("文件 {FileName} 已上传到缓存，文件ID: {FileId}", fileName, fileId);
            
            return new FileUploadResult
            {
                FileId = fileId,
                FileName = newFileName,
                FileUrl = fileUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件 {FileName} 到缓存时发生错误", fileName);
            throw;
        }
    }

    /// <summary>
    /// 根据文件ID下载文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件下载结果</returns>
    public async Task<FileDownloadResult> DownloadFileAsync(string fileId)
    {
        try
        {
            // 获取文件信息
            var fileInfo = await GetFileInfoAsync(fileId);
            if (fileInfo == null)
            {
                return new FileDownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "找不到指定的文件"
                };
            }
            
            if (fileInfo.IsInCache)
            {
                // 从缓存获取文件
                var cacheKey = GetCachedFileKey(fileId);
                var fileData = await _distributedCache.GetAsync(cacheKey);
                
                if (fileData == null)
                {
                    // 文件在缓存中不存在
                    return new FileDownloadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "文件在缓存中不存在或已过期"
                    };
                }
                
                // 创建内存流并返回
                var memoryStream = new MemoryStream(fileData);
                
                _logger.LogInformation("从缓存下载文件，文件ID: {FileId}", fileId);
                
                return new FileDownloadResult
                {
                    FileStream = memoryStream,
                    FileName = fileInfo.FileName,
                    ContentType = fileInfo.ContentType,
                    FileSize = fileInfo.FileSize,
                    IsSuccess = true
                };
            }
            else
            {
                // 从文件系统获取文件
                var filePath = Path.Combine(_fileStoragePath, fileInfo.FileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return new FileDownloadResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "文件在文件系统中不存在"
                    };
                }
                
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                
                _logger.LogInformation("从文件系统下载文件，文件ID: {FileId}", fileId);
                
                return new FileDownloadResult
                {
                    FileStream = fileStream,
                    FileName = fileInfo.FileName,
                    ContentType = fileInfo.ContentType,
                    FileSize = fileInfo.FileSize,
                    IsSuccess = true
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载文件 {FileId} 时发生错误", fileId);
            return new FileDownloadResult
            {
                IsSuccess = false,
                ErrorMessage = $"下载文件时发生错误: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 根据文件ID获取文件信息
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件信息</returns>
    public async Task<TempFileInfo> GetFileInfoAsync(string fileId)
    {
        try
        {
            // 从缓存中获取文件信息
            var infoKey = GetFileInfoKey(fileId);
            var data = await _distributedCache.GetAsync(infoKey);
            
            if (data == null)
            {
                // 文件信息不在缓存中，尝试在文件系统中搜索
                // 注意：实际实现时，可能需要建立文件ID到文件名的映射关系
                // 这里简化处理，返回null
                return null;
            }
            
            // 反序列化文件信息
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<TempFileInfo>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件信息 {FileId} 时发生错误", fileId);
            return null;
        }
    }

    /// <summary>
    /// 保存文件信息到缓存
    /// </summary>
    /// <param name="fileInfo">文件信息</param>
    /// <returns>是否保存成功</returns>
    private async Task<bool> SaveFileInfoAsync(TempFileInfo fileInfo)
    {
        try
        {
            var infoKey = GetFileInfoKey(fileInfo.FileId);
            var json = JsonConvert.SerializeObject(fileInfo);
            var data = Encoding.UTF8.GetBytes(json);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultCacheExpiration
            };
            
            await _distributedCache.SetAsync(infoKey, data, options);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存文件信息 {FileId} 时发生错误", fileInfo.FileId);
            return false;
        }
    }

    /// <summary>
    /// 获取导出任务信息
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>导出任务信息</returns>
    public async Task<ExportTaskDto> GetExportTaskAsync(string taskId)
    {
        try
        {
            var cacheKey = GetExportTaskCacheKey(taskId);
            var data = await _distributedCache.GetAsync(cacheKey);
            
            if (data == null)
            {
                return null;
            }
            
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<ExportTaskDto>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取导出任务 {TaskId} 信息时发生错误", taskId);
            return null;
        }
    }

    /// <summary>
    /// 更新导出任务信息
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <returns>是否更新成功</returns>
    public async Task<bool> UpdateExportTaskAsync(ExportTaskDto taskInfo)
    {
        if (taskInfo == null || string.IsNullOrEmpty(taskInfo.TaskId))
        {
            return false;
        }
        
        try
        {
            var cacheKey = GetExportTaskCacheKey(taskInfo.TaskId);
            var json = JsonConvert.SerializeObject(taskInfo);
            var data = Encoding.UTF8.GetBytes(json);
            
            // 设置缓存过期时间，根据业务需求可调整
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) // 保存7天
            };
            
            await _distributedCache.SetAsync(cacheKey, data, options);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新导出任务 {TaskId} 信息时发生错误", taskInfo.TaskId);
            return false;
        }
    }
    
    /// <summary>
    /// 获取导出任务缓存键
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>缓存键</returns>
    private string GetExportTaskCacheKey(string taskId)
    {
        return $"{ExportTaskKeyPrefix}{taskId}";
    }
    
    /// <summary>
    /// 获取缓存文件的缓存键
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>缓存键</returns>
    private string GetCachedFileKey(string fileId)
    {
        return $"{CachedFileKeyPrefix}{fileId}";
    }
    
    /// <summary>
    /// 获取文件信息的缓存键
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>缓存键</returns>
    private string GetFileInfoKey(string fileId)
    {
        return $"{FileInfoKeyPrefix}{fileId}";
    }
} 