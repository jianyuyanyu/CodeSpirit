using CodeSpirit.Amis.Attributes;

namespace CodeSpirit.Shared.Dtos.Common
{
    /// <summary>
    /// 批量导入数据的基础DTO类
    /// </summary>
    /// <typeparam name="T">要导入的数据类型</typeparam>
    public class BatchImportDtoBase<T>
    {
        /// <summary>
        /// Excel导入的数据集合
        /// </summary>
        /// <remarks>
        /// 使用AmisInputExcelField特性配置Excel上传控件的显示属性：
        /// - Label: 显示的标签文本
        /// - Placeholder: 提示文本
        /// - CreateInputTable: 是否创建输入表格
        /// </remarks>
        [AmisInputExcelField(Label = "上传Excel", Placeholder = "请拖拽Excel文件到当前区域", CreateInputTable = true)]
        public List<T> ImportData { get; set; }
    }
}
