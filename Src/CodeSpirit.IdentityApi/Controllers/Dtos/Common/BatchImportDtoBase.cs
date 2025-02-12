namespace CodeSpirit.IdentityApi.Controllers.Dtos.Common
{
    public class BatchImportDtoBase<T>
    {
        [AmisInputExcelField(Label = "上传Excel", Placeholder = "请拖拽Excel文件到当前区域", CreateInputTable = true)]
        public List<T> ImportData { get; set; }

    }
}
