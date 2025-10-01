using Newtonsoft.Json;
using ClosedXML.Excel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// 导入模板服务实现
    /// </summary>
    public class ImportTemplateService : IImportTemplateService
    {
        /// <summary>
        /// 生成Excel导入模板
        /// </summary>
        /// <typeparam name="T">导入DTO类型</typeparam>
        /// <param name="fileName">文件名</param>
        /// <returns>Excel文件字节数组</returns>
        public async Task<byte[]> GenerateExcelTemplateAsync<T>(string? fileName = null) where T : class
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("import");
                
                var columns = GetImportColumns<T>();
                
                // 验证是否有列
                if (columns == null || columns.Count == 0)
                {
                    throw new InvalidOperationException($"类型 {typeof(T).Name} 没有可导出的属性");
                }
                
                // 设置表头
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    var cell = worksheet.Cell(1, i + 1);
                    
                    // 设置列名（使用JsonProperty的PropertyName或DisplayName）
                    // 关键：使用文本前缀强制为文本类型，避免ExcelJS解析问题
                    cell.Value = column.ColumnName ?? "";
                    
                    // 设置样式
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    
                    // 添加注释说明
                    var commentText = $"字段：{column.DisplayName}";
                    if (column.IsRequired)
                    {
                        commentText += "\n必填项";
                        cell.Style.Font.FontColor = XLColor.Red;
                    }
                    if (!string.IsNullOrEmpty(column.Description))
                    {
                        commentText += $"\n说明：{column.Description}";
                    }
                    if (!string.IsNullOrEmpty(column.ExampleValue))
                    {
                        commentText += $"\n示例：{column.ExampleValue}";
                    }
                    
                    var comment = cell.CreateComment();
                    comment.AddText(commentText);
                }
                
                // 添加示例数据行
                if (columns.Any(c => !string.IsNullOrEmpty(c.ExampleValue)))
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        var column = columns[i];
                        if (!string.IsNullOrEmpty(column.ExampleValue))
                        {
                            var exampleCell = worksheet.Cell(2, i + 1);
                            exampleCell.Value = column.ExampleValue;
                            exampleCell.Style.Font.Italic = true;
                            exampleCell.Style.Font.FontColor = XLColor.Gray;
                        }
                    }
                }
                
                // 自动调整列宽
                AdjustColumnWidths(worksheet, columns);
                
                // 冻结首行
                worksheet.SheetView.FreezeRows(1);
                
                // 保存到内存流
                using var stream = new MemoryStream();
                workbook.SaveAs(stream, new SaveOptions { EvaluateFormulasBeforeSaving = false, ValidatePackage = false });
                
                // 确保流中有数据
                if (stream.Length == 0)
                {
                    throw new InvalidOperationException("生成的Excel文件为空");
                }
                
                return stream.ToArray();
            });
        }

        /// <summary>
        /// 根据类型名称生成Excel导入模板
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="fileName">文件名</param>
        /// <returns>Excel文件字节数组</returns>
        public async Task<byte[]> GenerateExcelTemplateByTypeNameAsync(string typeName, string? fileName = null)
        {
            // 查找类型
            var type = FindTypeByName(typeName);
            if (type == null)
            {
                throw new ArgumentException($"找不到类型：{typeName}");
            }

            // 使用反射调用泛型方法
            var method = GetType().GetMethod(nameof(GenerateExcelTemplateAsync))!;
            var genericMethod = method.MakeGenericMethod(type);
            var task = (Task<byte[]>)genericMethod.Invoke(this, new object[] { fileName })!;
            
            return await task;
        }

        /// <summary>
        /// 获取导入模板的列信息
        /// </summary>
        /// <typeparam name="T">导入DTO类型</typeparam>
        /// <returns>列信息列表</returns>
        public List<ImportColumnInfo> GetImportColumns<T>() where T : class
        {
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = new List<ImportColumnInfo>();

            foreach (var property in properties)
            {
                var jsonProperty = property.GetCustomAttribute<JsonPropertyAttribute>();
                var displayName = property.GetCustomAttribute<DisplayNameAttribute>();
                var required = property.GetCustomAttribute<RequiredAttribute>();
                var description = property.GetCustomAttribute<DescriptionAttribute>();

                var columnInfo = new ImportColumnInfo
                {
                    ColumnName = jsonProperty?.PropertyName ?? displayName?.DisplayName ?? property.Name,
                    DisplayName = displayName?.DisplayName ?? property.Name,
                    IsRequired = required != null,
                    DataType = GetFriendlyTypeName(property.PropertyType),
                    Description = description?.Description,
                    ExampleValue = GenerateExampleValue(property)
                };

                columns.Add(columnInfo);
            }

            return columns;
        }

        /// <summary>
        /// 根据类型名称查找类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>类型</returns>
        private Type? FindTypeByName(string typeName)
        {
            // 在当前应用域的所有程序集中查找类型
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    var type = types.FirstOrDefault(t => 
                        t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                        t.Name.Equals($"{typeName}BatchImportDto", StringComparison.OrdinalIgnoreCase) ||
                        t.FullName?.EndsWith($".{typeName}", StringComparison.OrdinalIgnoreCase) == true);
                    
                    if (type != null)
                        return type;
                }
                catch
                {
                    // 忽略无法访问的程序集
                }
            }

            return null;
        }

        /// <summary>
        /// 获取友好的类型名称
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>友好名称</returns>
        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(string))
                return "文本";
            if (type == typeof(int) || type == typeof(int?))
                return "整数";
            if (type == typeof(decimal) || type == typeof(decimal?) || type == typeof(double) || type == typeof(double?))
                return "数字";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "日期时间";
            if (type == typeof(bool) || type == typeof(bool?))
                return "是/否";
            if (type.IsEnum)
                return "枚举";

            return "文本";
        }

        /// <summary>
        /// 调整列宽以适应内容
        /// </summary>
        /// <param name="worksheet">工作表</param>
        /// <param name="columns">列信息</param>
        private void AdjustColumnWidths(IXLWorksheet worksheet, List<ImportColumnInfo> columns)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var columnIndex = i + 1;
                var xlColumn = worksheet.Column(columnIndex);
                
                // 计算列标题长度（中文字符按2个字符计算）
                var headerLength = CalculateStringWidth(column.ColumnName);
                
                // 计算示例值长度
                var exampleLength = CalculateStringWidth(column.ExampleValue);
                
                // 取最大长度，但不超过50个字符，不少于12个字符
                var maxLength = Math.Max(headerLength, exampleLength);
                var columnWidth = Math.Min(Math.Max(maxLength + 2, 12), 50);
                
                xlColumn.Width = columnWidth;
            }
        }

        /// <summary>
        /// 计算字符串显示宽度（中文字符算2个单位）
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns>显示宽度</returns>
        private int CalculateStringWidth(string? text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            
            int width = 0;
            foreach (char c in text)
            {
                // 中文字符范围大致判断
                if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    width += 2; // 中文字符占2个单位宽度
                }
                else
                {
                    width += 1; // 英文字符占1个单位宽度
                }
            }
            return width;
        }

        /// <summary>
        /// 生成示例值
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <returns>示例值</returns>
        private string? GenerateExampleValue(PropertyInfo property)
        {
            // 优先使用 ExampleValueAttribute 特性中定义的示例值
            var exampleAttr = property.GetCustomAttribute<ExampleValueAttribute>();
            if (exampleAttr != null && !string.IsNullOrEmpty(exampleAttr.Value))
            {
                return exampleAttr.Value;
            }

            var propertyName = property.Name.ToLower();
            var type = property.PropertyType;

            // 根据属性名称生成示例值
            if (propertyName.Contains("name") || propertyName.Contains("姓名"))
                return "张三";
            if (propertyName.Contains("phone") || propertyName.Contains("手机"))
                return "13800138000";
            if (propertyName.Contains("email") || propertyName.Contains("邮箱"))
                return "zhangsan@example.com";
            if (propertyName.Contains("idno") || propertyName.Contains("身份证"))
                return "110101199001011234";
            if (propertyName.Contains("number") || propertyName.Contains("编号"))
                return "001";
            if (propertyName.Contains("gender") || propertyName.Contains("性别"))
                return "男";
            if (propertyName.Contains("address") || propertyName.Contains("地址"))
                return "北京市朝阳区";

            // 根据类型生成示例值
            if (type == typeof(string))
                return "示例文本";
            if (type == typeof(int) || type == typeof(int?))
                return "1";
            if (type == typeof(decimal) || type == typeof(decimal?))
                return "100.00";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return DateTime.Now.ToString("yyyy-MM-dd");
            if (type == typeof(bool) || type == typeof(bool?))
                return "是";

            return null;
        }
    }
}
