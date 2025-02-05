// 文件路径: Utilities/PasswordGenerator.cs
namespace CodeSpirit.IdentityApi.Utilities
{
    /// <summary>
    /// 密码生成工具
    /// </summary>
    public static class PasswordGenerator
    {
        /// <summary>
        /// 生成随机密码
        /// </summary>
        /// <param name="length">密码长度（默认12位）</param>
        /// <returns>随机生成的密码字符串</returns>
        public static string GenerateRandomPassword(int length = 12)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()-_=+[]{}|;:,.<>?";

            string allChars = upper + lower + digits + special;
            Random random = new Random();

            // 确保密码包含至少一个大写字母、小写字母、数字和特殊字符
            char[] password = new char[length];
            password[0] = upper[random.Next(upper.Length)];
            password[1] = lower[random.Next(lower.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // 打乱密码字符顺序
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }
}
