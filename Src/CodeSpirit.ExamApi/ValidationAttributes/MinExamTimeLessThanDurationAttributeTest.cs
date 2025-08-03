using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.ValidationAttributes;

/// <summary>
/// 测试MinExamTimeLessThanDurationAttribute验证特性
/// </summary>
public class MinExamTimeLessThanDurationAttributeTest
{
    /// <summary>
    /// 测试用的DTO类
    /// </summary>
    public class TestDto
    {
        public int Duration { get; set; }
        
        [MinExamTimeLessThanDuration]
        public int MinExamTime { get; set; }
    }

    /// <summary>
    /// 测试验证功能
    /// </summary>
    public static void Test()
    {
        var context = new ValidationContext(new TestDto());
        var attribute = new MinExamTimeLessThanDurationAttribute();

        // 测试用例1：正常情况 - MinExamTime < Duration
        var testObj1 = new TestDto { Duration = 120, MinExamTime = 30 };
        context = new ValidationContext(testObj1) { MemberName = nameof(TestDto.MinExamTime) };
        var result1 = attribute.GetValidationResult(testObj1.MinExamTime, context);
        Console.WriteLine($"测试1 (30 < 120): {(result1 == ValidationResult.Success ? "通过" : "失败")}");

        // 测试用例2：错误情况 - MinExamTime >= Duration
        var testObj2 = new TestDto { Duration = 60, MinExamTime = 60 };
        context = new ValidationContext(testObj2) { MemberName = nameof(TestDto.MinExamTime) };
        var result2 = attribute.GetValidationResult(testObj2.MinExamTime, context);
        Console.WriteLine($"测试2 (60 >= 60): {(result2 != ValidationResult.Success ? "通过" : "失败")} - {result2?.ErrorMessage}");

        // 测试用例3：错误情况 - MinExamTime > Duration
        var testObj3 = new TestDto { Duration = 90, MinExamTime = 120 };
        context = new ValidationContext(testObj3) { MemberName = nameof(TestDto.MinExamTime) };
        var result3 = attribute.GetValidationResult(testObj3.MinExamTime, context);
        Console.WriteLine($"测试3 (120 > 90): {(result3 != ValidationResult.Success ? "通过" : "失败")} - {result3?.ErrorMessage}");
    }
}