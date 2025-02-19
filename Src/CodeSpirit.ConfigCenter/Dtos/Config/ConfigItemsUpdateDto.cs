using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ConfigItemsUpdateDto : IValidatableObject
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [Required]
    public string Environment { get; set; }

    private string _configs;
    private JObject _parsedConfigs;

    /// <summary>
    /// 配置集合的JSON字符串
    /// </summary>
    [Required]
    public string Configs 
    { 
        get => _configs;
        set
        {
            _configs = value;
            try
            {
                _parsedConfigs = JObject.Parse(_configs);
            }
            catch
            {
                _parsedConfigs = null;
            }
        }
    }

    /// <summary>
    /// 获取解析后的配置对象
    /// </summary>
    public JObject ParsedConfigs => _parsedConfigs;

    /// <summary>
    /// 验证配置数据
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Configs))
        {
            yield return new ValidationResult("配置内容不能为空", new[] { nameof(Configs) });
            yield break;
        }

        if (_parsedConfigs == null)
        {
            yield return new ValidationResult("配置内容必须是有效的JSON对象", new[] { nameof(Configs) });
            yield break;
        }

        // 验证配置键格式
        foreach (var property in _parsedConfigs.Properties())
        {
            var key = property.Name;
            if (string.IsNullOrEmpty(key))
            {
                yield return new ValidationResult("配置键不能为空", new[] { nameof(Configs) });
                continue;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"^[a-zA-Z0-9_:.]+$"))
            {
                yield return new ValidationResult(
                    $"配置键 '{key}' 格式无效，只能包含字母、数字、下划线、冒号和点", 
                    new[] { nameof(Configs) });
            }
        }
    }
} 