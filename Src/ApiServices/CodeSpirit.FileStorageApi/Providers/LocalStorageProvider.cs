using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Options;
using Microsoft.Extensions.Options;

namespace CodeSpirit.FileStorageApi.Providers;

/// <summary>
/// 本地存储提供程序
/// </summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly StorageProviderOptions _options;
    private readonly ILogger<LocalStorageProvider> _logger;
    private readonly string _rootPath;
    private readonly string _baseUrl;

    public StorageProviderType ProviderType => StorageProviderType.Local;

    public LocalStorageProvider(StorageProviderOptions options, ILogger<LocalStorageProvider> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _rootPath = _options.GetProperty<string>("RootPath", "wwwroot/uploads");
        _baseUrl = _options.GetProperty<string>("BaseUrl", "");

        // 确保根目录存在
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    public async Task<StorageResult> UploadFileAsync(string bucketName, string fileName,
        Stream stream, string? contentType = null, IDictionary<string, string>? metadata = null)
    {
        try
        {
            var bucketPath = Path.Combine(_rootPath, bucketName);
            if (!Directory.Exists(bucketPath))
            {
                Directory.CreateDirectory(bucketPath);
            }

            var filePath = Path.Combine(bucketPath, fileName);

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 写入文件
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);

            var fileInfo = new FileInfo(filePath);
            var fileUrl = !string.IsNullOrEmpty(_baseUrl)
                ? $"{_baseUrl.TrimEnd('/')}/{bucketName}/{fileName}"
                : $"/{bucketName}/{fileName}";

            _logger.LogInformation("文件上传成功: {FilePath}, 大小: {Size} bytes", filePath, fileInfo.Length);

            return new StorageResult
            {
                Success = true,
                FileUrl = fileUrl,
                Size = fileInfo.Length,
                ETag = GenerateETag(filePath)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败: {BucketName}/{FileName}", bucketName, fileName);
            return new StorageResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    public async Task<Stream> DownloadFileAsync(string bucketName, string fileName)
    {
        try
        {
            var filePath = Path.Combine(_rootPath, bucketName, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {bucketName}/{fileName}");
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            return new MemoryStream(fileBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件下载失败: {BucketName}/{FileName}", bucketName, fileName);
            throw;
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public Task<bool> DeleteFileAsync(string bucketName, string fileName)
    {
        try
        {
            var filePath = Path.Combine(_rootPath, bucketName, fileName);
            _logger.LogInformation("准备删除文件：{FilePath}", filePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("文件删除成功: {FilePath}", filePath);
                return Task.FromResult(true);
            }

            _logger.LogWarning("尝试删除不存在的文件: {FilePath}", filePath);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件删除失败: {BucketName}/{FileName}", bucketName, fileName);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    public async Task<FileInfo> GetFileInfoAsync(string bucketName, string fileName)
    {
        try
        {
            var filePath = Path.Combine(_rootPath, bucketName, fileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            var fileInfo = new FileInfo(filePath);
            return await Task.FromResult(fileInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件信息失败: {BucketName}/{FileName}", bucketName, fileName);
            return null;
        }
    }

    /// <summary>
    /// 生成预签名URL
    /// </summary>
    public async Task<string> GeneratePresignedUrlAsync(string bucketName, string fileName,
        TimeSpan expirationTime, PresignedUrlOperation operation = PresignedUrlOperation.Read)
    {
        // 本地存储直接返回文件URL，不支持预签名
        var fileUrl = !string.IsNullOrEmpty(_baseUrl)
            ? $"{_baseUrl.TrimEnd('/')}/{bucketName}/{fileName}"
            : $"/{bucketName}/{fileName}";

        return await Task.FromResult(fileUrl);
    }

    /// <summary>
    /// 创建存储桶
    /// </summary>
    public async Task<bool> CreateBucketAsync(string bucketName, BucketCreationOptions? options = null)
    {
        try
        {
            var bucketPath = Path.Combine(_rootPath, bucketName);

            if (!Directory.Exists(bucketPath))
            {
                Directory.CreateDirectory(bucketPath);
                _logger.LogInformation("存储桶创建成功: {BucketPath}", bucketPath);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储桶创建失败: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// 删除存储桶
    /// </summary>
    public async Task<bool> DeleteBucketAsync(string bucketName)
    {
        try
        {
            var bucketPath = Path.Combine(_rootPath, bucketName);

            if (Directory.Exists(bucketPath))
            {
                Directory.Delete(bucketPath, true);
                _logger.LogInformation("存储桶删除成功: {BucketPath}", bucketPath);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储桶删除失败: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// 生成ETag
    /// </summary>
    private static string GenerateETag(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return $"{fileInfo.LastWriteTime.Ticks:X}-{fileInfo.Length:X}";
    }
}
