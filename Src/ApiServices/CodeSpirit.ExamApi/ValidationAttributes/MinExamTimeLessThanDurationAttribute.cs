using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.ValidationAttributes;

/// <summary>
/// 验证最小考试时间必须小于考试时长的自定义验证特性
/// </summary>
public class MinExamTimeLessThanDurationAttribute : ValidationAttribute
{
    private readonly string _durationPropertyName;

    /// <summary>
    /// 初始化MinExamTimeLessThanDurationAttribute实例
    /// </summary>
    /// <param name="durationPropertyName">考试时长属性名称</param>
    public MinExamTimeLessThanDurationAttribute(string durationPropertyName = "Duration")
    {
        _durationPropertyName = durationPropertyName;
        ErrorMessage = "最小考试时间不能大于考试时长";
    }

    /// <summary>
    /// 验证方法
    /// </summary>
    /// <param name="value">要验证的值（MinExamTime）</param>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        // 获取考试时长属性
        var durationProperty = validationContext.ObjectType.GetProperty(_durationPropertyName);
        if (durationProperty == null)
        {
            return new ValidationResult($"未找到属性 {_durationPropertyName}");
        }

        var durationValue = durationProperty.GetValue(validationContext.ObjectInstance);
        if (durationValue == null)
        {
            return ValidationResult.Success;
        }

        // 比较最小考试时间和考试时长
        if (value is int minExamTime && durationValue is int duration)
        {
            if (minExamTime > duration)
            {
                return new ValidationResult(
                    $"最小考试时间（{minExamTime}分钟）必须小于等于考试时长（{duration}分钟）",
                    new[] { validationContext.MemberName ?? "" });
            }
        }

        return ValidationResult.Success;
    }
}