using System;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Amis.Attributes
{
    /// <summary>
    /// CRUD对话框操作特性，用于在弹窗中显示列表数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class CrudDialogOperationAttribute : OperationAttribute
    {
        /// <summary>
        /// 数据API路由（支持模板变量，如 ${id}）
        /// </summary>
        public string DataApi { get; set; }

        /// <summary>
        /// 要显示的数据类型（DTO）
        /// </summary>
        public Type DataType { get; set; }

        /// <summary>
        /// 是否启用分页，默认 true
        /// </summary>
        public bool EnablePagination { get; set; } = true;

        /// <summary>
        /// 每页数量，默认 10
        /// </summary>
        public int PerPage { get; set; } = 10;

        /// <summary>
        /// 每页选项，默认 [10, 20, 50, 100]
        /// </summary>
        public int[] PerPageOptions { get; set; } = new[] { 10, 20, 50, 100 };

        /// <summary>
        /// 是否启用搜索，默认 false
        /// </summary>
        public bool EnableSearch { get; set; } = false;

        /// <summary>
        /// 是否启用导出，默认 false
        /// </summary>
        public bool EnableExport { get; set; } = false;

        /// <summary>
        /// 是否启用刷新，默认 true
        /// </summary>
        public bool EnableRefresh { get; set; } = true;

        /// <summary>
        /// 行操作按钮配置（JSON格式）
        /// </summary>
        public string RowActions { get; set; }

        /// <summary>
        /// 自定义列配置（JSON格式，可选）
        /// </summary>
        public string CustomColumns { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="label">按钮标签</param>
        /// <param name="api">Schema API地址（可选，默认使用当前方法）</param>
        /// <param name="confirmText">确认文本（可选）</param>
        /// <param name="visibleOn">显示条件（可选）</param>
        /// <param name="isBulkOperation">是否批量操作，默认 false</param>
        public CrudDialogOperationAttribute(
            string label,
            string api = null,
            string confirmText = null,
            string visibleOn = null,
            bool isBulkOperation = false)
            : base(label, OperationActionType.CrudDialog, api, confirmText, visibleOn, isBulkOperation)
        {
        }
    }
}

