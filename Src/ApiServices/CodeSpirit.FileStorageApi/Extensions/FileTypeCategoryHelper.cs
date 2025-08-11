using CodeSpirit.FileStorageApi.Entities;

namespace CodeSpirit.FileStorageApi.Extensions;

/// <summary>
/// 文件类型分类工具类
/// </summary>
public static class FileTypeCategoryHelper
{
    private static readonly Dictionary<string, FileTypeCategory> ContentTypeMapping = new()
    {
        // 图片类型
        { "image/jpeg", FileTypeCategory.Image },
        { "image/jpg", FileTypeCategory.Image },
        { "image/png", FileTypeCategory.Image },
        { "image/gif", FileTypeCategory.Image },
        { "image/bmp", FileTypeCategory.Image },
        { "image/webp", FileTypeCategory.Image },
        { "image/svg+xml", FileTypeCategory.Image },
        { "image/tiff", FileTypeCategory.Image },
        { "image/x-icon", FileTypeCategory.Image },
        
        // 视频类型
        { "video/mp4", FileTypeCategory.Video },
        { "video/avi", FileTypeCategory.Video },
        { "video/mov", FileTypeCategory.Video },
        { "video/wmv", FileTypeCategory.Video },
        { "video/flv", FileTypeCategory.Video },
        { "video/webm", FileTypeCategory.Video },
        { "video/mkv", FileTypeCategory.Video },
        { "video/3gp", FileTypeCategory.Video },
        
        // 音频类型
        { "audio/mp3", FileTypeCategory.Audio },
        { "audio/mpeg", FileTypeCategory.Audio },
        { "audio/wav", FileTypeCategory.Audio },
        { "audio/flac", FileTypeCategory.Audio },
        { "audio/aac", FileTypeCategory.Audio },
        { "audio/ogg", FileTypeCategory.Audio },
        { "audio/wma", FileTypeCategory.Audio },
        { "audio/m4a", FileTypeCategory.Audio },
        
        // 文档类型
        { "application/pdf", FileTypeCategory.Document },
        { "application/msword", FileTypeCategory.Document },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileTypeCategory.Document },
        { "application/vnd.ms-excel", FileTypeCategory.Document },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileTypeCategory.Document },
        { "application/vnd.ms-powerpoint", FileTypeCategory.Document },
        { "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileTypeCategory.Document },
        { "text/plain", FileTypeCategory.Document },
        { "text/csv", FileTypeCategory.Document },
        { "text/html", FileTypeCategory.Document },
        { "text/xml", FileTypeCategory.Document },
        { "application/json", FileTypeCategory.Document },
        { "application/rtf", FileTypeCategory.Document },
        
        // 压缩包类型
        { "application/zip", FileTypeCategory.Archive },
        { "application/x-rar-compressed", FileTypeCategory.Archive },
        { "application/x-7z-compressed", FileTypeCategory.Archive },
        { "application/gzip", FileTypeCategory.Archive },
        { "application/x-tar", FileTypeCategory.Archive },
        { "application/x-bzip2", FileTypeCategory.Archive }
    };
    
    private static readonly Dictionary<string, FileTypeCategory> ExtensionMapping = new()
    {
        // 图片扩展名
        { "jpg", FileTypeCategory.Image },
        { "jpeg", FileTypeCategory.Image },
        { "png", FileTypeCategory.Image },
        { "gif", FileTypeCategory.Image },
        { "bmp", FileTypeCategory.Image },
        { "webp", FileTypeCategory.Image },
        { "svg", FileTypeCategory.Image },
        { "tiff", FileTypeCategory.Image },
        { "tif", FileTypeCategory.Image },
        { "ico", FileTypeCategory.Image },
        
        // 视频扩展名
        { "mp4", FileTypeCategory.Video },
        { "avi", FileTypeCategory.Video },
        { "mov", FileTypeCategory.Video },
        { "wmv", FileTypeCategory.Video },
        { "flv", FileTypeCategory.Video },
        { "webm", FileTypeCategory.Video },
        { "mkv", FileTypeCategory.Video },
        { "3gp", FileTypeCategory.Video },
        { "m4v", FileTypeCategory.Video },
        
        // 音频扩展名
        { "mp3", FileTypeCategory.Audio },
        { "wav", FileTypeCategory.Audio },
        { "flac", FileTypeCategory.Audio },
        { "aac", FileTypeCategory.Audio },
        { "ogg", FileTypeCategory.Audio },
        { "wma", FileTypeCategory.Audio },
        { "m4a", FileTypeCategory.Audio },
        
        // 文档扩展名
        { "pdf", FileTypeCategory.Document },
        { "doc", FileTypeCategory.Document },
        { "docx", FileTypeCategory.Document },
        { "xls", FileTypeCategory.Document },
        { "xlsx", FileTypeCategory.Document },
        { "ppt", FileTypeCategory.Document },
        { "pptx", FileTypeCategory.Document },
        { "txt", FileTypeCategory.Document },
        { "csv", FileTypeCategory.Document },
        { "html", FileTypeCategory.Document },
        { "htm", FileTypeCategory.Document },
        { "xml", FileTypeCategory.Document },
        { "json", FileTypeCategory.Document },
        { "rtf", FileTypeCategory.Document },
        
        // 压缩包扩展名
        { "zip", FileTypeCategory.Archive },
        { "rar", FileTypeCategory.Archive },
        { "7z", FileTypeCategory.Archive },
        { "tar", FileTypeCategory.Archive },
        { "gz", FileTypeCategory.Archive },
        { "bz2", FileTypeCategory.Archive }
    };
    
    /// <summary>
    /// 根据内容类型获取文件类型分类
    /// </summary>
    /// <param name="contentType">内容类型</param>
    /// <returns>文件类型分类</returns>
    public static FileTypeCategory GetCategoryFromContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return FileTypeCategory.Unknown;
            
        // 精确匹配
        if (ContentTypeMapping.TryGetValue(contentType.ToLowerInvariant(), out var category))
            return category;
            
        // 模糊匹配
        var lowerContentType = contentType.ToLowerInvariant();
        if (lowerContentType.StartsWith("image/"))
            return FileTypeCategory.Image;
        if (lowerContentType.StartsWith("video/"))
            return FileTypeCategory.Video;
        if (lowerContentType.StartsWith("audio/"))
            return FileTypeCategory.Audio;
        if (lowerContentType.StartsWith("text/") || lowerContentType.Contains("document") || lowerContentType.Contains("pdf"))
            return FileTypeCategory.Document;
        if (lowerContentType.Contains("zip") || lowerContentType.Contains("archive") || lowerContentType.Contains("compressed"))
            return FileTypeCategory.Archive;
            
        return FileTypeCategory.Other;
    }
    
    /// <summary>
    /// 根据文件扩展名获取文件类型分类（备用方案）
    /// </summary>
    /// <param name="extension">文件扩展名</param>
    /// <returns>文件类型分类</returns>
    public static FileTypeCategory GetCategoryFromExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return FileTypeCategory.Unknown;
            
        var ext = extension.TrimStart('.').ToLowerInvariant();
        
        return ExtensionMapping.TryGetValue(ext, out var category) 
            ? category 
            : FileTypeCategory.Other;
    }
    
    /// <summary>
    /// 根据文件名和内容类型获取文件类型分类
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>文件类型分类</returns>
    public static FileTypeCategory GetCategory(string fileName, string contentType)
    {
        // 首先尝试根据内容类型获取
        var categoryFromContentType = GetCategoryFromContentType(contentType);
        if (categoryFromContentType != FileTypeCategory.Unknown && categoryFromContentType != FileTypeCategory.Other)
            return categoryFromContentType;
        
        // 如果内容类型无法确定，则根据文件扩展名获取
        if (!string.IsNullOrEmpty(fileName))
        {
            var extension = Path.GetExtension(fileName);
            var categoryFromExtension = GetCategoryFromExtension(extension);
            if (categoryFromExtension != FileTypeCategory.Unknown && categoryFromExtension != FileTypeCategory.Other)
                return categoryFromExtension;
        }
        
        return categoryFromContentType != FileTypeCategory.Unknown 
            ? categoryFromContentType 
            : FileTypeCategory.Other;
    }
    
    /// <summary>
    /// 验证文件类型是否允许
    /// </summary>
    /// <param name="contentType">内容类型</param>
    /// <param name="fileName">文件名</param>
    /// <param name="allowedTypes">允许的类型模式</param>
    /// <param name="forbiddenTypes">禁止的类型模式</param>
    /// <returns>是否允许</returns>
    public static bool IsFileTypeAllowed(string? contentType, string? fileName, 
        string? allowedTypes = null, string? forbiddenTypes = null)
    {
        // 检查禁止的类型
        if (!string.IsNullOrEmpty(forbiddenTypes))
        {
            var forbidden = forbiddenTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant());
            
            foreach (var forbiddenType in forbidden)
            {
                if (IsTypeMatch(contentType, fileName, forbiddenType))
                    return false;
            }
        }
        
        // 检查允许的类型
        if (!string.IsNullOrEmpty(allowedTypes))
        {
            var allowed = allowedTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant());
            
            foreach (var allowedType in allowed)
            {
                if (IsTypeMatch(contentType, fileName, allowedType))
                    return true;
            }
            
            return false; // 如果定义了允许类型但没有匹配，则不允许
        }
        
        return true; // 没有限制则允许
    }
    
    /// <summary>
    /// 检查类型是否匹配
    /// </summary>
    private static bool IsTypeMatch(string? contentType, string? fileName, string? pattern)
    {
        var lowerContentType = contentType?.ToLowerInvariant() ?? "";
        var lowerPattern = pattern?.ToLowerInvariant() ?? "";
        
        // 通配符匹配
        if (lowerPattern.Contains("*"))
        {
            var patternRegex = lowerPattern.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(lowerContentType, patternRegex);
        }
        
        // 精确匹配内容类型
        if (lowerContentType == lowerPattern)
            return true;
        
        // 根据扩展名匹配
        if (!string.IsNullOrEmpty(fileName))
        {
            var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant();
            if (extension == lowerPattern)
                return true;
        }
        
        return false;
    }
}
