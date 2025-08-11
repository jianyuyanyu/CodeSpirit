using COSXML;
using COSXML.Auth;
using COSXML.Model.Bucket;
using COSXML.Model.Object;
using COSXML.Transfer;
using COSXML.CosException;
using CodeSpirit.FileStorageApi.Options;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Providers;

/// <summary>
/// 腾讯云COS存储提供程序实现
/// </summary>
[DisplayName("腾讯云COS存储")]
public class TencentCosStorageProvider : IStorageProvider
{
    private readonly TencentCosOptions _options;
    private readonly ILogger<TencentCosStorageProvider> _logger;
    private readonly CosXml _cosXml;
    private readonly TransferManager _transferManager;

    /// <summary>
    /// 初始化腾讯云COS存储提供程序
    /// </summary>
    /// <param name="options">COS配置选项</param>
    /// <param name="logger">日志记录器</param>
    public TencentCosStorageProvider(IOptions<TencentCosOptions> options, ILogger<TencentCosStorageProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!_options.IsValid())
        {
            throw new ArgumentException("腾讯云COS配置无效", nameof(options));
        }

        // 初始化COS服务
        _cosXml = InitializeCosService();
        
        // 初始化传输管理器（用于高级上传下载功能）
        var transferConfig = new TransferConfig();
        _transferManager = new TransferManager(_cosXml, transferConfig);

        _logger.LogInformation("腾讯云COS存储提供程序初始化完成，地域: {Region}", _options.Region);
    }

    /// <summary>
    /// 提供程序类型
    /// </summary>
    public StorageProviderType ProviderType => StorageProviderType.TencentCOS;

    /// <summary>
    /// 提供程序名称
    /// </summary>
    public string Name => "腾讯云COS";

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <param name="stream">文件流</param>
    /// <param name="contentType">内容类型</param>
    /// <param name="metadata">元数据</param>
    /// <returns>存储结果</returns>
    public async Task<StorageResult> UploadFileAsync(
        string bucketName, 
        string fileName, 
        Stream stream, 
        string? contentType = null, 
        IDictionary<string, string>? metadata = null)
    {
        try
        {
            _logger.LogDebug("开始上传文件到腾讯云COS: {BucketName}/{FileName}", bucketName, fileName);

            var fullBucketName = GetFullBucketName(bucketName);
            
            // 对于大文件使用分片上传，小文件使用简单上传
            if (stream.Length > 5 * 1024 * 1024) // 5MB阈值
            {
                return await UploadLargeFileAsync(fullBucketName, fileName, stream, contentType, metadata);
            }
            else
            {
                return await UploadSmallFileAsync(fullBucketName, fileName, stream, contentType, metadata);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件到腾讯云COS失败: {BucketName}/{FileName}", bucketName, fileName);
            return StorageResult.CreateFailed($"上传失败: {GetFriendlyErrorMessage(ex)}");
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <returns>文件流</returns>
    public async Task<Stream> DownloadFileAsync(string bucketName, string fileName)
    {
        try
        {
            _logger.LogDebug("开始从腾讯云COS下载文件: {BucketName}/{FileName}", bucketName, fileName);

            var fullBucketName = GetFullBucketName(bucketName);
            
            // 创建临时文件用于下载
            var tempFile = Path.GetTempFileName();
            var request = new GetObjectRequest(fullBucketName, fileName, tempFile, null);
            
            var result = await Task.Run(() => _cosXml.GetObject(request));
            
            _logger.LogDebug("从腾讯云COS下载文件成功: {BucketName}/{FileName}", bucketName, fileName);
            
            // 读取下载的文件并返回流
            var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从腾讯云COS下载文件失败: {BucketName}/{FileName}", bucketName, fileName);
            throw new InvalidOperationException($"下载失败: {GetFriendlyErrorMessage(ex)}", ex);
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <returns>是否成功</returns>
    public async Task<bool> DeleteFileAsync(string bucketName, string fileName)
    {
        try
        {
            _logger.LogDebug("开始从腾讯云COS删除文件: {BucketName}/{FileName}", bucketName, fileName);

            var fullBucketName = GetFullBucketName(bucketName);
            var request = new DeleteObjectRequest(fullBucketName, fileName);
            
            await Task.Run(() => _cosXml.DeleteObject(request));
            
            _logger.LogDebug("从腾讯云COS删除文件成功: {BucketName}/{FileName}", bucketName, fileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从腾讯云COS删除文件失败: {BucketName}/{FileName}", bucketName, fileName);
            return false;
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <returns>文件信息</returns>
    public async Task<FileInfo> GetFileInfoAsync(string bucketName, string fileName)
    {
        try
        {
            var fullBucketName = GetFullBucketName(bucketName);
            var request = new HeadObjectRequest(fullBucketName, fileName);
            
            var result = await Task.Run(() => _cosXml.HeadObject(request));
            
            // 创建一个临时文件来满足FileInfo构造函数的要求
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            
            // 创建临时文件用于构造FileInfo对象
            if (!File.Exists(tempPath))
            {
                await File.WriteAllTextAsync(tempPath, "");
            }
            
            var fileInfo = new FileInfo(tempPath);
            
            _logger.LogDebug("获取腾讯云COS文件信息成功: {BucketName}/{FileName}, Size: {Size}", 
                bucketName, fileName, result.size);
                
            return fileInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取腾讯云COS文件信息失败: {BucketName}/{FileName}", bucketName, fileName);
            throw new InvalidOperationException($"获取文件信息失败: {GetFriendlyErrorMessage(ex)}", ex);
        }
    }

    /// <summary>
    /// 生成预签名URL
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="fileName">文件名</param>
    /// <param name="expirationTime">过期时间</param>
    /// <param name="operation">操作类型</param>
    /// <returns>预签名URL</returns>
    public async Task<string> GeneratePresignedUrlAsync(
        string bucketName, 
        string fileName, 
        TimeSpan expirationTime, 
        PresignedUrlOperation operation = PresignedUrlOperation.Read)
    {
        try
        {
            var fullBucketName = GetFullBucketName(bucketName);
            var httpMethod = operation == PresignedUrlOperation.Write ? "PUT" : "GET";
            
            // 使用新的预签名URL方法
            var url = await Task.Run(() => 
            {
                var preSignatureStruct = new COSXML.Model.Tag.PreSignatureStruct
                {
                    appid = _options.AppId,
                    region = _options.Region,
                    bucket = bucketName,
                    key = fileName,
                    httpMethod = operation == PresignedUrlOperation.Write ? "PUT" : "GET",
                    isHttps = _options.UseHttps,
                    signDurationSecond = (long)expirationTime.TotalSeconds,
                    headers = null,
                    queryParameters = null
                };
                return _cosXml.GenerateSignURL(preSignatureStruct);
            });
            
            _logger.LogDebug("生成腾讯云COS预签名URL成功: {BucketName}/{FileName}", bucketName, fileName);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成腾讯云COS预签名URL失败: {BucketName}/{FileName}", bucketName, fileName);
            throw new InvalidOperationException($"生成预签名URL失败: {GetFriendlyErrorMessage(ex)}", ex);
        }
    }

    /// <summary>
    /// 创建存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="options">创建选项</param>
    /// <returns>是否成功</returns>
    public async Task<bool> CreateBucketAsync(string bucketName, BucketCreationOptions? options = null)
    {
        try
        {
            var fullBucketName = GetFullBucketName(bucketName);
            var request = new PutBucketRequest(fullBucketName);
            
            await Task.Run(() => _cosXml.PutBucket(request));
            
            _logger.LogInformation("创建腾讯云COS存储桶成功: {BucketName}", bucketName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建腾讯云COS存储桶失败: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// 删除存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>是否成功</returns>
    public async Task<bool> DeleteBucketAsync(string bucketName)
    {
        try
        {
            var fullBucketName = GetFullBucketName(bucketName);
            var request = new DeleteBucketRequest(fullBucketName);
            
            await Task.Run(() => _cosXml.DeleteBucket(request));
            
            _logger.LogInformation("删除腾讯云COS存储桶成功: {BucketName}", bucketName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除腾讯云COS存储桶失败: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // TransferManager 没有 Dispose 方法，但我们可以清理其他资源
        GC.SuppressFinalize(this);
    }

    #region 私有方法

    /// <summary>
    /// 初始化COS服务
    /// </summary>
    /// <returns>COS服务实例</returns>
    private CosXml InitializeCosService()
    {
        // 初始化配置
        var config = new CosXmlConfig.Builder()
            .IsHttps(_options.UseHttps)
            .SetRegion(_options.Region)
            .SetDebugLog(_options.EnableDebugLog)
            .SetConnectionTimeoutMs(_options.ConnectionTimeoutMs)
            .SetReadWriteTimeoutMs(_options.ReadWriteTimeoutMs)
            .Build();

        // 创建凭证提供程序
        QCloudCredentialProvider credentialProvider;
        
        if (_options.UseTemporaryCredentials && _options.TemporaryCredentials != null)
        {
            // 使用临时密钥
            credentialProvider = new CustomTemporaryCredentialProvider(_options, _logger);
        }
        else
        {
            // 使用永久密钥
            credentialProvider = new DefaultQCloudCredentialProvider(
                _options.SecretId, 
                _options.SecretKey, 
                _options.SignatureDurationSeconds);
        }

        return new CosXmlServer(config, credentialProvider);
    }

    /// <summary>
    /// 获取完整的存储桶名称（包含APPID）
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>完整的存储桶名称</returns>
    private string GetFullBucketName(string bucketName)
    {
        if (bucketName.Contains("-") && bucketName.EndsWith(_options.AppId))
        {
            return bucketName; // 已经包含APPID
        }
        return $"{bucketName}-{_options.AppId}";
    }

    /// <summary>
    /// 上传小文件（简单上传）
    /// </summary>
    private async Task<StorageResult> UploadSmallFileAsync(
        string fullBucketName, 
        string fileName, 
        Stream stream, 
        string? contentType, 
        IDictionary<string, string>? metadata)
    {
        var request = new PutObjectRequest(fullBucketName, fileName, stream);
        
        if (!string.IsNullOrEmpty(contentType))
        {
            request.SetRequestHeader("Content-Type", contentType);
        }

        if (metadata != null)
        {
            foreach (var meta in metadata)
            {
                request.SetRequestHeader($"x-cos-meta-{meta.Key}", meta.Value);
            }
        }

        var result = await Task.Run(() => _cosXml.PutObject(request));
        
        _logger.LogDebug("腾讯云COS小文件上传成功: {FileName}, ETag: {ETag}", fileName, result.eTag);
        
        var fileUrl = $"https://{fullBucketName}.cos.{_options.Region}.myqcloud.com/{fileName}";
        return StorageResult.CreateSuccess(result.eTag?.Trim('"') ?? "", fileUrl, stream.Length);
    }

    /// <summary>
    /// 上传大文件（分片上传）
    /// </summary>
    private async Task<StorageResult> UploadLargeFileAsync(
        string fullBucketName, 
        string fileName, 
        Stream stream, 
        string? contentType, 
        IDictionary<string, string>? metadata)
    {
        // 需要先将Stream写入临时文件，因为TransferManager需要文件路径
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fileStream = File.Create(tempFile))
            {
                await stream.CopyToAsync(fileStream);
            }

            var uploadTask = new COSXMLUploadTask(fullBucketName, fileName);
            uploadTask.SetSrcPath(tempFile);

            // 注意：在新版本SDK中，SetRequestHeader方法可能不可用
            // 可以通过设置请求的headers属性或者在上传时设置元数据

            // 设置进度回调
            uploadTask.progressCallback = (completed, total) =>
            {
                var progress = total > 0 ? (double)completed / total * 100 : 0;
                _logger.LogDebug("腾讯云COS大文件上传进度: {FileName} - {Progress:F2}%", fileName, progress);
            };

            var result = await _transferManager.UploadAsync(uploadTask);
            
            _logger.LogDebug("腾讯云COS大文件上传成功: {FileName}, ETag: {ETag}", fileName, result.eTag);
            
            var fileUrl = $"https://{fullBucketName}.cos.{_options.Region}.myqcloud.com/{fileName}";
            return StorageResult.CreateSuccess(result.eTag?.Trim('"') ?? "", fileUrl, stream.Length);
        }
        finally
        {
            // 清理临时文件
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除临时文件失败: {TempFile}", tempFile);
                }
            }
        }
    }

    /// <summary>
    /// 获取友好的错误信息
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>友好的错误信息</returns>
    private string GetFriendlyErrorMessage(Exception exception)
    {
        return exception switch
        {
            CosServerException serverEx => serverEx.statusCode switch
            {
                403 => "访问被拒绝，请检查密钥和权限配置",
                404 => "文件或存储桶不存在",
                409 => "资源冲突",
                429 => "请求过于频繁，请稍后重试",
                500 => "服务器内部错误",
                503 => "服务暂时不可用",
                _ => $"服务器错误 ({serverEx.statusCode}): {serverEx.statusMessage}"
            },
            CosClientException clientEx => $"客户端错误: {clientEx.Message}",
            _ => exception.Message
        };
    }

    #endregion
}

/// <summary>
/// 自定义临时密钥凭证提供程序
/// </summary>
internal class CustomTemporaryCredentialProvider : DefaultSessionQCloudCredentialProvider
{
    private readonly TencentCosOptions _options;
    private readonly ILogger _logger;
    private DateTime _lastRefreshTime;

    public CustomTemporaryCredentialProvider(TencentCosOptions options, ILogger logger) 
        : base(null, null, 0L, null)
    {
        _options = options;
        _logger = logger;
        _lastRefreshTime = DateTime.MinValue;
    }

    public override void Refresh()
    {
        try
        {
            // 检查是否需要刷新
            var now = DateTime.UtcNow;
            var refreshInterval = TimeSpan.FromSeconds(_options.TemporaryCredentials?.RefreshIntervalSeconds ?? 600);
            
            if (now - _lastRefreshTime < refreshInterval)
            {
                return;
            }

            _logger.LogDebug("开始刷新腾讯云COS临时密钥");

            // TODO: 这里应该调用STS服务获取临时密钥
            // 由于STS服务的具体实现依赖于业务场景，这里提供基本框架
            var temporaryCredentials = GetTemporaryCredentialsFromSts();
            
            if (temporaryCredentials != null)
            {
                SetQCloudCredential(
                    temporaryCredentials.SecretId,
                    temporaryCredentials.SecretKey,
                    $"{temporaryCredentials.StartTime};{temporaryCredentials.ExpiredTime}",
                    temporaryCredentials.Token);

                _lastRefreshTime = now;
                _logger.LogDebug("腾讯云COS临时密钥刷新成功");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新腾讯云COS临时密钥失败");
            throw;
        }
    }

    /// <summary>
    /// 从STS服务获取临时密钥
    /// </summary>
    /// <returns>临时密钥信息</returns>
    private TemporaryCredentials? GetTemporaryCredentialsFromSts()
    {
        // TODO: 实现STS服务调用
        // 这里需要根据具体的STS服务实现
        _logger.LogWarning("临时密钥获取功能需要具体实现STS服务调用");
        return null;
    }
}

/// <summary>
/// 临时密钥信息
/// </summary>
public class TemporaryCredentials
{
    /// <summary>
    /// 临时SecretId
    /// </summary>
    public required string SecretId { get; set; }

    /// <summary>
    /// 临时SecretKey
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// 临时Token
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// 开始时间（Unix时间戳）
    /// </summary>
    public long StartTime { get; set; }

    /// <summary>
    /// 过期时间（Unix时间戳）
    /// </summary>
    public long ExpiredTime { get; set; }
}