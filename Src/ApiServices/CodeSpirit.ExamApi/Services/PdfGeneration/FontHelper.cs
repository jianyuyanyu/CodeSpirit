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
    ///     fonts-wqy-microhei \
    ///     fonts-wqy-zenhei \
    ///     fonts-noto-cjk \
    ///     fontconfig \
    ///     &amp;&amp; fc-cache -f -v \
    ///     &amp;&amp; apt-get clean \
    ///     &amp;&amp; rm -rf /var/lib/apt/lists/*
    /// </code>
    /// <para>字体优先级：</para>
    /// <list type="number">
    /// <item>Windows: SimSun (宋体)</item>
    /// <item>Linux: WenQuanYi Micro Hei (文泉驿微米黑)</item>
    /// <item>Linux 备选: Noto Sans CJK SC</item>
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
            // QuestPDF 会自动查找系统中可用的字体
            return "WenQuanYi Micro Hei"; // 文泉驿微米黑
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
        if (OperatingSystem.IsLinux())
        {
            return "Noto Sans CJK SC"; // 思源黑体
        }
        return "SimSun";
    }
}

