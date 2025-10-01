namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// 导入模板服务接口
    /// </summary>
    public interface IImportTemplateService
    {
        /// <summary>
        /// 生成Excel导入模板
        /// </summary>
        /// <typeparam name="T">导入DTO类型</typeparam>
        /// <param name="fileName">文件名</param>
        /// <returns>Excel文件字节数组</returns>
        Task<byte[]> GenerateExcelTemplateAsync<T>(string? fileName = null) where T : class;

        /// <summary>
        /// 根据类型名称生成Excel导入模板
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="fileName">文件名</param>
        /// <returns>Excel文件字节数组</returns>
        Task<byte[]> GenerateExcelTemplateByTypeNameAsync(string typeName, string? fileName = null);

        /// <summary>
        /// 获取导入模板的列信息
        /// </summary>
        /// <typeparam name="T">导入DTO类型</typeparam>
        /// <returns>列信息列表</returns>
        List<ImportColumnInfo> GetImportColumns<T>() where T : class;
    }

    /// <summary>
    /// 导入列信息
    /// </summary>
    public class ImportColumnInfo
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 示例值
        /// </summary>
        public string? ExampleValue { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string? Description { get; set; }
    }
}
