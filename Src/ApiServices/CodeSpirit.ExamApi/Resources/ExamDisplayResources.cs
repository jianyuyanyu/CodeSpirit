using System.Globalization;
using System.Resources;

namespace CodeSpirit.ExamApi.Resources;

/// <summary>
/// 考试服务显示资源类
/// 资源文件: ExamDisplay.resx / ExamDisplay.en.resx
/// </summary>
public class ExamDisplayResources
{
    private static ResourceManager? _resourceManager;
    
    /// <summary>
    /// 资源管理器
    /// </summary>
    public static ResourceManager ResourceManager
    {
        get
        {
            if (_resourceManager == null)
            {
                _resourceManager = new ResourceManager(
                    "CodeSpirit.ExamApi.Resources.ExamDisplay",
                    typeof(ExamDisplayResources).Assembly);
            }
            return _resourceManager;
        }
    }
}
