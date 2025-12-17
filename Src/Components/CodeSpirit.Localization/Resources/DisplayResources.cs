using System.Globalization;
using System.Resources;

namespace CodeSpirit.Localization.Resources
{
    /// <summary>
    /// 字段显示名称资源类（自动生成）
    /// 资源文件: Display.resx / Display.en.resx
    /// </summary>
    public class DisplayResources
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
                    var resourceManager = new ResourceManager(
                        "CodeSpirit.Localization.Resources.Display",
                        typeof(DisplayResources).Assembly);
                    _resourceManager = resourceManager;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        /// 题目内容
        /// </summary>
        public static string Content
        {
            get { return ResourceManager.GetString("Content", CultureInfo.CurrentUICulture) ?? "Content"; }
        }
        
        /// <summary>
        /// 题目类型
        /// </summary>
        public static string Type
        {
            get { return ResourceManager.GetString("Type", CultureInfo.CurrentUICulture) ?? "Type"; }
        }
        
        /// <summary>
        /// 难度
        /// </summary>
        public static string Difficulty
        {
            get { return ResourceManager.GetString("Difficulty", CultureInfo.CurrentUICulture) ?? "Difficulty"; }
        }
        
        /// <summary>
        /// 选项
        /// </summary>
        public static string Options
        {
            get { return ResourceManager.GetString("Options", CultureInfo.CurrentUICulture) ?? "Options"; }
        }
        
        /// <summary>
        /// 正确答案
        /// </summary>
        public static string CorrectAnswer
        {
            get { return ResourceManager.GetString("CorrectAnswer", CultureInfo.CurrentUICulture) ?? "CorrectAnswer"; }
        }
        
        /// <summary>
        /// 解析
        /// </summary>
        public static string Analysis
        {
            get { return ResourceManager.GetString("Analysis", CultureInfo.CurrentUICulture) ?? "Analysis"; }
        }
        
        /// <summary>
        /// 分类
        /// </summary>
        public static string CategoryId
        {
            get { return ResourceManager.GetString("CategoryId", CultureInfo.CurrentUICulture) ?? "CategoryId"; }
        }
        
        /// <summary>
        /// 分值
        /// </summary>
        public static string DefaultScore
        {
            get { return ResourceManager.GetString("DefaultScore", CultureInfo.CurrentUICulture) ?? "DefaultScore"; }
        }
        
        /// <summary>
        /// 标签
        /// </summary>
        public static string Tags
        {
            get { return ResourceManager.GetString("Tags", CultureInfo.CurrentUICulture) ?? "Tags"; }
        }
        
        /// <summary>
        /// 知识点
        /// </summary>
        public static string KnowledgePoints
        {
            get { return ResourceManager.GetString("KnowledgePoints", CultureInfo.CurrentUICulture) ?? "KnowledgePoints"; }
        }
    }
}
