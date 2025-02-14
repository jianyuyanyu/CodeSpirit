using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeSpirit.Core.Extensions
{
    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
        }

        ///// <summary>
        ///// 将字符串转换为小驼峰格式（过时版本，建议使用下方ToCamelCase重载）
        ///// </summary>
        //public static string ToCamelCase(this string input)
        //{
        //    if (string.IsNullOrEmpty(input) || !char.IsUpper(input[0]))
        //    {
        //        return input;
        //    }

        //    char[] chars = input.ToCharArray();
        //    for (int i = 0; i < chars.Length; i++)
        //    {
        //        if (i == 0 || (i > 0 && char.IsUpper(chars[i])))
        //        {
        //            chars[i] = char.ToLower(chars[i]);
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //    return new string(chars);
        //}

        /// <summary>
        /// 判断字符串是否为null或空
        /// </summary>
        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        /// <summary>
        /// 判断字符串是否为null或空白
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        /// <summary>
        /// 从字符串开头获取指定长度的子字符串
        /// </summary>
        /// <param name="str">原字符串</param>
        /// <param name="len">截取长度</param>
        /// <exception cref="ArgumentNullException">当字符串为null时抛出</exception>
        /// <exception cref="ArgumentException">当长度超过字符串实际长度时抛出</exception>
        public static string Left(this string str, int len)
        {
            return str == null
                ? throw new ArgumentNullException(nameof(str))
                : str.Length < len ? throw new ArgumentException("截取长度不能超过字符串实际长度") : str[..len];
        }

        /// <summary>
        /// 统一换行符为当前环境换行符
        /// </summary>
        public static string NormalizeLineEndings(this string str)
            => str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);

        /// <summary>
        /// 移除字符串结尾的指定后缀（匹配第一个符合的后缀）
        /// </summary>
        /// <param name="postFixes">要移除的后缀集合</param>
        public static string? RemovePostFix(this string str, params string[] postFixes)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length == 0)
            {
                return string.Empty;
            }

            if (postFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (string postfix in postFixes)
            {
                if (str.EndsWith(postfix))
                {
                    return str.Left(str.Length - postfix.Length);
                }
            }
            return str;
        }

        /// <summary>
        /// 移除字符串开头的指定前缀（匹配第一个符合的前缀）
        /// </summary>
        /// <param name="preFixes">要移除的前缀集合</param>
        public static string? RemovePreFix(this string str, params string[] preFixes)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length == 0)
            {
                return string.Empty;
            }

            if (preFixes.IsNullOrEmpty())
            {
                return str;
            }

            foreach (string prefix in preFixes)
            {
                if (str.StartsWith(prefix))
                {
                    return str.Right(str.Length - prefix.Length);
                }
            }
            return str;
        }

        /// <summary>
        /// 从字符串结尾获取指定长度的子字符串
        /// </summary>
        /// <exception cref="ArgumentNullException">当字符串为null时抛出</exception>
        /// <exception cref="ArgumentException">当长度超过字符串实际长度时抛出</exception>
        public static string Right(this string str, int len)
        {
            return str == null
                ? throw new ArgumentNullException(nameof(str))
                : str.Length < len ? throw new ArgumentException("截取长度不能超过字符串实际长度") : str.Substring(str.Length - len, len);
        }

        /// <summary>
        /// 使用指定分隔符分割字符串
        /// </summary>
        public static string[] Split(this string str, string separator)
            => str.Split(new[] { separator }, StringSplitOptions.None);

        /// <summary>
        /// 使用指定分隔符和选项分割字符串
        /// </summary>
        public static string[] Split(this string str, string separator, StringSplitOptions options)
            => str.Split(new[] { separator }, options);

        /// <summary>
        /// 按换行符分割字符串
        /// </summary>
        public static string[] SplitToLines(this string str)
            => Split(str, Environment.NewLine);

        /// <summary>
        /// 按换行符和指定选项分割字符串
        /// </summary>
        public static string[] SplitToLines(this string str, StringSplitOptions options)
            => Split(str, Environment.NewLine, options);

        /// <summary>
        /// 将字符串转换为小驼峰格式（推荐使用）
        /// </summary>
        /// <param name="invariantCulture">是否使用不变文化规则</param>
        public static string ToCamelCase(this string str, bool invariantCulture = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return invariantCulture ? str.ToLowerInvariant() : str.ToLower();
            }

            char firstChar = invariantCulture
                ? char.ToLowerInvariant(str[0])
                : char.ToLower(str[0]);
            return firstChar + str[1..];
        }

        /// <summary>
        /// 使用指定文化规则转换为小驼峰格式
        /// </summary>
        public static string ToCamelCase(this string str, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(str) ? str : str.Length == 1 ? str.ToLower(culture) : char.ToLower(str[0], culture) + str[1..];
        }

        /// <summary>
        /// 将驼峰格式字符串转换为句子格式（使用正则表达式）
        /// </summary>
        /// <param name="invariantCulture">是否使用不变文化规则</param>
        public static string ToSentenceCase(this string str, bool invariantCulture = false)
        {
            return string.IsNullOrWhiteSpace(str)
                ? str
                : Regex.Replace(str, "[a-z][A-Z]", m =>
                    $"{m.Value[0]} {(invariantCulture ? char.ToLowerInvariant(m.Value[1]) : char.ToLower(m.Value[1]))}");
        }

        /// <summary>
        /// 使用指定文化规则转换为句子格式
        /// </summary>
        public static string ToSentenceCase(this string str, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(str)
                ? str
                : Regex.Replace(str, "[a-z][A-Z]", m =>
                    $"{m.Value[0]} {char.ToLower(m.Value[1], culture)}");
        }

        /// <summary>
        /// 将字符串转换为枚举类型
        /// </summary>
        /// <exception cref="ArgumentNullException">当值为null时抛出</exception>
        public static T ToEnum<T>(this string value) where T : struct
        {
            return value == null ? throw new ArgumentNullException(nameof(value)) : (T)Enum.Parse(typeof(T), value);
        }

        /// <summary>
        /// 将字符串转换为枚举类型（支持大小写忽略）
        /// </summary>
        public static T ToEnum<T>(this string value, bool ignoreCase) where T : struct
        {
            return value == null ? throw new ArgumentNullException(nameof(value)) : (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        /// <summary>
        /// 计算字符串的MD5哈希值
        /// </summary>
        public static string ToMd5(this string str)
        {
            using MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            StringBuilder sb = new();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将字符串转换为大驼峰格式
        /// </summary>
        /// <param name="invariantCulture">是否使用不变文化规则</param>
        public static string ToPascalCase(this string str, bool invariantCulture = true)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.Length == 1)
            {
                return invariantCulture ? str.ToUpperInvariant() : str.ToUpper();
            }

            char firstChar = invariantCulture
                ? char.ToUpperInvariant(str[0])
                : char.ToUpper(str[0]);
            return firstChar + str[1..];
        }

        /// <summary>
        /// 使用指定文化规则转换为大驼峰格式
        /// </summary>
        public static string ToPascalCase(this string str, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(str) ? str : str.Length == 1 ? str.ToUpper(culture) : char.ToUpper(str[0], culture) + str[1..];
        }

        /// <summary>
        /// 截断字符串到指定长度
        /// </summary>
        public static string? Truncate(this string str, int maxLength)
        {
            return str == null ? null : str.Length <= maxLength ? str : str[..maxLength];
        }

        /// <summary>
        /// 截断字符串并添加省略号后缀
        /// </summary>
        public static string TruncateWithPostfix(this string str, int maxLength)
            => TruncateWithPostfix(str, maxLength, "...");

        /// <summary>
        /// 截断字符串并添加自定义后缀
        /// </summary>
        /// <param name="postfix">要添加的后缀</param>
        public static string? TruncateWithPostfix(this string str, int maxLength, string postfix)
        {
            return str == null
                ? null
                : str == string.Empty || maxLength == 0
                ? string.Empty
                : str.Length <= maxLength
                ? str
                : maxLength <= postfix.Length ? postfix[..maxLength] : str[..(maxLength - postfix.Length)] + postfix;
        }

        /// <summary>
        /// 生成权限Code
        /// </summary>
        /// <param name="rawCode"></param>
        /// <returns></returns>
        public static string GenerateShortCode(this string rawCode)
        {
            // 使用 MD5 哈希并取前8位字符生成简短的权限代码
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(rawCode);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                string shortCode = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8);  // 截取前8位
                return shortCode;
            }
        }

        public static string ToKebabCase(this string str)
        {
            return string.IsNullOrEmpty(str)
                ? str
                : string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString() : x.ToString())).ToLower();
        }

        public static string ToSpacedWords(this string str)
        {
            return string.IsNullOrEmpty(str) ? str : string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
        }
    }
}