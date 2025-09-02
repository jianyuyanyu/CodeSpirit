using Microsoft.AspNetCore.Mvc;
using CodeSpirit.Core;
using System.ComponentModel;

namespace CodeSpirit.Web.Controllers.Api.Common;

/// <summary>
/// 图标控制器 - 提供图标选择器数据
/// </summary>
[Route("api/common/[controller]")]
[ApiController]
[DisplayName("图标管理")]
public class IconsController : ControllerBase
{
    /// <summary>
    /// 获取可用图标列表
    /// </summary>
    /// <param name="iconType">图标类型，如：fontawesome</param>
    /// <param name="search">搜索关键词</param>
    /// <param name="page">页码</param>
    /// <param name="limit">每页数量</param>
    /// <returns>图标列表</returns>
    [HttpGet]
    public ActionResult<ApiResponse<object>> GetIcons(
        [FromQuery] string iconType = "fontawesome", 
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        try
        {
            var icons = GetIconsByType(iconType, search, page, limit);
            
            return ApiResponse<object>.Success(new
            {
                items = icons,
                total = GetTotalIconCount(iconType, search),
                page,
                limit,
                categories = GetIconCategories(iconType)
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Error(500, $"获取图标列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据类型获取图标列表
    /// </summary>
    /// <param name="iconType">图标类型</param>
    /// <param name="search">搜索关键词</param>
    /// <param name="page">页码</param>
    /// <param name="limit">每页数量</param>
    /// <returns>图标列表</returns>
    private List<object> GetIconsByType(string iconType, string? search, int page, int limit)
    {
        return iconType.ToLower() switch
        {
            "fontawesome" => GetFontAwesomeIcons(search, page, limit),
            "iconfont" => GetIconFontIcons(search, page, limit),
            _ => GetFontAwesomeIcons(search, page, limit)
        };
    }

    /// <summary>
    /// 获取FontAwesome图标列表
    /// </summary>
    /// <param name="search">搜索关键词</param>
    /// <param name="page">页码</param>
    /// <param name="limit">每页数量</param>
    /// <returns>图标列表</returns>
    private List<object> GetFontAwesomeIcons(string? search, int page, int limit)
    {
        // 常用的FontAwesome图标列表
        var allIcons = new List<object>
        {
            // 通用图标
            new { className = "fa-solid fa-folder", name = "文件夹", category = "通用" },
            new { className = "fa-solid fa-file", name = "文件", category = "通用" },
            new { className = "fa-solid fa-home", name = "首页", category = "通用" },
            new { className = "fa-solid fa-user", name = "用户", category = "通用" },
            new { className = "fa-solid fa-users", name = "用户组", category = "通用" },
            new { className = "fa-solid fa-cog", name = "设置", category = "通用" },
            new { className = "fa-solid fa-search", name = "搜索", category = "通用" },
            new { className = "fa-solid fa-plus", name = "添加", category = "操作" },
            new { className = "fa-solid fa-edit", name = "编辑", category = "操作" },
            new { className = "fa-solid fa-trash", name = "删除", category = "操作" },
            new { className = "fa-solid fa-save", name = "保存", category = "操作" },
            new { className = "fa-solid fa-download", name = "下载", category = "操作" },
            new { className = "fa-solid fa-upload", name = "上传", category = "操作" },
            
            // 业务相关图标
            new { className = "fa-solid fa-book", name = "书籍", category = "教育" },
            new { className = "fa-solid fa-graduation-cap", name = "毕业帽", category = "教育" },
            new { className = "fa-solid fa-school", name = "学校", category = "教育" },
            new { className = "fa-solid fa-chalkboard-teacher", name = "教师", category = "教育" },
            new { className = "fa-solid fa-exam", name = "考试", category = "教育" },
            new { className = "fa-solid fa-certificate", name = "证书", category = "教育" },
            new { className = "fa-solid fa-clipboard-list", name = "问卷", category = "调查" },
            new { className = "fa-solid fa-poll", name = "投票", category = "调查" },
            new { className = "fa-solid fa-chart-bar", name = "柱状图", category = "统计" },
            new { className = "fa-solid fa-chart-pie", name = "饼图", category = "统计" },
            new { className = "fa-solid fa-chart-line", name = "折线图", category = "统计" },
            
            // 状态图标
            new { className = "fa-solid fa-check", name = "成功", category = "状态" },
            new { className = "fa-solid fa-times", name = "失败", category = "状态" },
            new { className = "fa-solid fa-exclamation", name = "警告", category = "状态" },
            new { className = "fa-solid fa-info", name = "信息", category = "状态" },
            new { className = "fa-solid fa-question", name = "疑问", category = "状态" },
            new { className = "fa-solid fa-star", name = "星星", category = "状态" },
            new { className = "fa-solid fa-heart", name = "心形", category = "状态" },
            new { className = "fa-solid fa-thumbs-up", name = "点赞", category = "状态" },
            
            // 系统图标
            new { className = "fa-solid fa-database", name = "数据库", category = "系统" },
            new { className = "fa-solid fa-server", name = "服务器", category = "系统" },
            new { className = "fa-solid fa-cloud", name = "云", category = "系统" },
            new { className = "fa-solid fa-network-wired", name = "网络", category = "系统" },
            new { className = "fa-solid fa-shield-alt", name = "安全", category = "系统" },
            new { className = "fa-solid fa-key", name = "密钥", category = "系统" },
            new { className = "fa-solid fa-lock", name = "锁定", category = "系统" },
            new { className = "fa-solid fa-unlock", name = "解锁", category = "系统" },
            
            // 导航图标
            new { className = "fa-solid fa-arrow-left", name = "左箭头", category = "导航" },
            new { className = "fa-solid fa-arrow-right", name = "右箭头", category = "导航" },
            new { className = "fa-solid fa-arrow-up", name = "上箭头", category = "导航" },
            new { className = "fa-solid fa-arrow-down", name = "下箭头", category = "导航" },
            new { className = "fa-solid fa-chevron-left", name = "左雪佛龙", category = "导航" },
            new { className = "fa-solid fa-chevron-right", name = "右雪佛龙", category = "导航" },
            new { className = "fa-solid fa-chevron-up", name = "上雪佛龙", category = "导航" },
            new { className = "fa-solid fa-chevron-down", name = "下雪佛龙", category = "导航" },
            
            // 媒体图标
            new { className = "fa-solid fa-image", name = "图片", category = "媒体" },
            new { className = "fa-solid fa-video", name = "视频", category = "媒体" },
            new { className = "fa-solid fa-music", name = "音乐", category = "媒体" },
            new { className = "fa-solid fa-play", name = "播放", category = "媒体" },
            new { className = "fa-solid fa-pause", name = "暂停", category = "媒体" },
            new { className = "fa-solid fa-stop", name = "停止", category = "媒体" },
            
            // 通信图标
            new { className = "fa-solid fa-envelope", name = "邮件", category = "通信" },
            new { className = "fa-solid fa-phone", name = "电话", category = "通信" },
            new { className = "fa-solid fa-mobile-alt", name = "手机", category = "通信" },
            new { className = "fa-solid fa-comment", name = "评论", category = "通信" },
            new { className = "fa-solid fa-comments", name = "多评论", category = "通信" },
            new { className = "fa-solid fa-bell", name = "铃铛", category = "通信" },
            
            // 时间图标
            new { className = "fa-solid fa-clock", name = "时钟", category = "时间" },
            new { className = "fa-solid fa-calendar", name = "日历", category = "时间" },
            new { className = "fa-solid fa-calendar-alt", name = "日历2", category = "时间" },
            new { className = "fa-solid fa-stopwatch", name = "秒表", category = "时间" },
            new { className = "fa-solid fa-hourglass", name = "沙漏", category = "时间" }
        };

        // 应用搜索过滤
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            allIcons = allIcons.Where(icon => 
            {
                var iconData = (dynamic)icon;
                return iconData.name.ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                       iconData.className.ToString().Contains(searchLower) ||
                       iconData.category.ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase);
            }).ToList();
        }

        // 应用分页
        return allIcons
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// 获取IconFont图标列表
    /// </summary>
    /// <param name="search">搜索关键词</param>
    /// <param name="page">页码</param>
    /// <param name="limit">每页数量</param>
    /// <returns>图标列表</returns>
    private List<object> GetIconFontIcons(string? search, int page, int limit)
    {
        // 这里可以根据实际需要实现IconFont图标列表
        // 暂时返回空列表
        return new List<object>();
    }

    /// <summary>
    /// 获取图标总数
    /// </summary>
    /// <param name="iconType">图标类型</param>
    /// <param name="search">搜索关键词</param>
    /// <returns>总数</returns>
    private int GetTotalIconCount(string iconType, string? search)
    {
        var icons = GetIconsByType(iconType, search, 1, int.MaxValue);
        return icons.Count;
    }

    /// <summary>
    /// 获取图标分类列表
    /// </summary>
    /// <param name="iconType">图标类型</param>
    /// <returns>分类列表</returns>
    private List<object> GetIconCategories(string iconType)
    {
        return iconType.ToLower() switch
        {
            "fontawesome" => new List<object>
            {
                new { label = "全部", value = "" },
                new { label = "通用", value = "通用" },
                new { label = "操作", value = "操作" },
                new { label = "教育", value = "教育" },
                new { label = "调查", value = "调查" },
                new { label = "统计", value = "统计" },
                new { label = "状态", value = "状态" },
                new { label = "系统", value = "系统" },
                new { label = "导航", value = "导航" },
                new { label = "媒体", value = "媒体" },
                new { label = "通信", value = "通信" },
                new { label = "时间", value = "时间" }
            },
            _ => new List<object>()
        };
    }
}
