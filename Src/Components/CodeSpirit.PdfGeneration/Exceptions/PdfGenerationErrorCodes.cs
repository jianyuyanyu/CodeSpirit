namespace CodeSpirit.PdfGeneration.Exceptions;

/// <summary>
/// PDF生成错误代码
/// </summary>
public static class PdfGenerationErrorCodes
{
    /// <summary>
    /// 基础错误代码
    /// </summary>
    private const int BaseErrorCode = 40000;

    /// <summary>
    /// 服务未初始化
    /// </summary>
    public const int NotInitialized = BaseErrorCode + 1;

    /// <summary>
    /// 操作超时
    /// </summary>
    public const int Timeout = BaseErrorCode + 2;

    /// <summary>
    /// 浏览器实例获取失败
    /// </summary>
    public const int BrowserAcquisitionFailed = BaseErrorCode + 3;

    /// <summary>
    /// PDF生成失败
    /// </summary>
    public const int GenerationFailed = BaseErrorCode + 4;

    /// <summary>
    /// 浏览器实例池已满
    /// </summary>
    public const int BrowserPoolExhausted = BaseErrorCode + 5;

    /// <summary>
    /// 浏览器实例已关闭
    /// </summary>
    public const int BrowserClosed = BaseErrorCode + 6;

    /// <summary>
    /// 无效的HTML内容
    /// </summary>
    public const int InvalidHtmlContent = BaseErrorCode + 7;

    /// <summary>
    /// 无效的PDF选项
    /// </summary>
    public const int InvalidPdfOptions = BaseErrorCode + 8;
}