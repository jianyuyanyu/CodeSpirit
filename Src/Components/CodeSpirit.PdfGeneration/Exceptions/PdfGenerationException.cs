using CodeSpirit.Core;

namespace CodeSpirit.PdfGeneration.Exceptions;

/// <summary>
/// PDF生成异常
/// </summary>
public class PdfGenerationException : BusinessException
{
    /// <summary>
    /// 创建PDF生成异常实例
    /// </summary>
    /// <param name="message">错误消息</param>
    public PdfGenerationException(string message) 
        : base(PdfGenerationErrorCodes.GenerationFailed, message)
    {
    }

    /// <summary>
    /// 创建PDF生成异常实例
    /// </summary>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误消息</param>
    public PdfGenerationException(int code, string message) 
        : base(code, message)
    {
    }

    /// <summary>
    /// 创建PDF生成异常实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public PdfGenerationException(string message, Exception innerException) 
        : base(PdfGenerationErrorCodes.GenerationFailed, message)
    {
    }

    /// <summary>
    /// 创建PDF生成异常实例
    /// </summary>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public PdfGenerationException(int code, string message, Exception innerException) 
        : base(code, message)
    {
    }
}

/// <summary>
/// PDF生成服务未初始化异常
/// </summary>
public class PdfGenerationNotInitializedException : PdfGenerationException
{
    /// <summary>
    /// 创建PDF生成服务未初始化异常实例
    /// </summary>
    public PdfGenerationNotInitializedException() 
        : base(PdfGenerationErrorCodes.NotInitialized, "PDF生成服务尚未初始化，请先调用 InitializeAsync 方法")
    {
    }
}

/// <summary>
/// PDF生成超时异常
/// </summary>
public class PdfGenerationTimeoutException : PdfGenerationException
{
    /// <summary>
    /// 创建PDF生成超时异常实例
    /// </summary>
    /// <param name="timeout">超时时间</param>
    public PdfGenerationTimeoutException(TimeSpan timeout) 
        : base(PdfGenerationErrorCodes.Timeout, $"PDF生成操作超时，超时时间：{timeout.TotalSeconds:N0}秒")
    {
    }
}

/// <summary>
/// 浏览器实例获取失败异常
/// </summary>
public class BrowserAcquisitionException : PdfGenerationException
{
    /// <summary>
    /// 创建浏览器实例获取失败异常实例
    /// </summary>
    /// <param name="message">错误消息</param>
    public BrowserAcquisitionException(string message) 
        : base(PdfGenerationErrorCodes.BrowserAcquisitionFailed, message)
    {
    }
    
    /// <summary>
    /// 创建浏览器实例获取失败异常实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public BrowserAcquisitionException(string message, Exception innerException) 
        : base(PdfGenerationErrorCodes.BrowserAcquisitionFailed, message)
    {
    }
}