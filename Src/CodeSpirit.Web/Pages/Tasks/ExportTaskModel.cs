using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Tasks
{
    /// <summary>
    /// 导出任务页面模型
    /// </summary>
    public class ExportTaskModel : PageModel
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string TaskId { get; set; }

        /// <summary>
        /// 页面处理方法
        /// </summary>
        public void OnGet()
        {
            // 页面处理逻辑
        }
    }
} 