using System.Security.Cryptography;
using System.Text;

namespace CodeSpirit.Shared.Utilities;

/// <summary>
/// 访问码生成器
/// </summary>
public static class AccessCodeGenerator
{
    private static readonly char[] AllowedChars = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    
    /// <summary>
    /// 生成问卷公开访问码（短码）
    /// </summary>
    /// <param name="length">访问码长度，默认8位</param>
    /// <returns>短访问码</returns>
    public static string GenerateSurveyAccessCode(int length = 8)
    {
        if (length <= 0 || length > 16)
            throw new ArgumentException("访问码长度必须在1-16之间", nameof(length));
            
        return GenerateRandomString(length);
    }
    
    /// <summary>
    /// 生成指定长度的随机字符串
    /// </summary>
    /// <param name="length">字符串长度</param>
    /// <returns>随机字符串</returns>
    public static string GenerateRandomString(int length)
    {
        if (length <= 0)
            throw new ArgumentException("长度必须大于0", nameof(length));
            
        var result = new StringBuilder(length);
        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[4];
        
        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(buffer);
            var randomIndex = BitConverter.ToUInt32(buffer, 0) % AllowedChars.Length;
            result.Append(AllowedChars[randomIndex]);
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// 验证访问码格式
    /// </summary>
    /// <param name="accessCode">访问码</param>
    /// <returns>是否有效</returns>
    public static bool IsValidAccessCode(string accessCode)
    {
        if (string.IsNullOrWhiteSpace(accessCode))
            return false;
            
        // 支持4-16位访问码
        if (accessCode.Length < 4 || accessCode.Length > 16)
            return false;
            
        return accessCode.All(c => AllowedChars.Contains(char.ToLower(c)));
    }
    
    /// <summary>
    /// 生成短访问码（用于简化分享）
    /// </summary>
    /// <param name="length">访问码长度，默认8位</param>
    /// <returns>短访问码</returns>
    public static string GenerateShortAccessCode(int length = 8)
    {
        if (length <= 0 || length > 32)
            throw new ArgumentException("长度必须在1-32之间", nameof(length));
            
        return GenerateRandomString(length);
    }
}
