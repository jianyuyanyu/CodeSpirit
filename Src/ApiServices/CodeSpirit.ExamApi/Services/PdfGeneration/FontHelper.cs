namespace CodeSpirit.ExamApi.Services.PdfGeneration;

/// <summary>
/// PDF 字体辅助类
/// </summary>
public static class FontHelper
{
    /// <summary>
    /// 获取支持中文的字体名称
    /// </summary>
    /// <remarks>
    /// <para>容器环境中需要安装中文字体包：</para>
    /// <code>
    /// # Dockerfile 中添加：
    /// RUN apt-get update &amp;&amp; apt-get install -y \
    ///     fonts-noto-cjk \
    ///     fontconfig \
    ///     &amp;&amp; fc-cache -f -v \
    ///     &amp;&amp; apt-get clean \
    ///     &amp;&amp; rm -rf /var/lib/apt/lists/*
    /// </code>
    /// <para>字体优先级：</para>
    /// <list type="number">
    /// <item>Windows: SimSun (宋体)</item>
    /// <item>Linux: Noto Sans CJK SC (思源黑体)</item>
    /// <item>macOS: PingFang SC (苹方)</item>
    /// </list>
    /// </remarks>
    /// <returns>字体名称</returns>
    public static string GetChineseFont()
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows 系统默认字体
            return "SimSun"; // 宋体
        }
        else if (OperatingSystem.IsLinux())
        {
            // Linux 容器环境
            // 使用 Google 思源黑体
            return "Noto Sans CJK SC"; // 思源黑体
        }
        else
        {
            // macOS 或其他系统
            return "PingFang SC"; // 苹方
        }
    }

    /// <summary>
    /// 获取后备字体名称
    /// </summary>
    /// <returns>后备字体名称</returns>
    public static string GetFallbackFont()
    {
        // 统一使用 SimSun 作为后备字体
        return "SimSun";
    }

    /// <summary>
    /// 获取支持符号的字体名称（用于特殊符号如 ✓、✗、☑、☐）
    /// </summary>
    /// <remarks>
    /// 这些符号字体通常包含 Unicode 符号字符。
    /// 如果系统字体不支持，可以使用 DejaVu Sans 或 Noto Sans Symbols。
    /// </remarks>
    /// <returns>符号字体名称</returns>
    public static string GetSymbolFont()
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows 默认符号字体
            return "Segoe UI Symbol";
        }
        else if (OperatingSystem.IsLinux())
        {
            // Linux 容器环境使用 DejaVu Sans（通常预装）
            return "DejaVu Sans";
        }
        else
        {
            // macOS
            return "Apple Symbols";
        }
    }
}

