namespace CodeSpirit.Shared.Extensions
{
    /// <summary>
    /// 唯一短名生成器
    /// </summary>
    public static class UniqueNameGenerator
    {
        /// <summary>
        ///  该方法将生成一个8个字符的名称，并且几乎可以保证是唯一的。
        ///  该名称基于GUID，通过去掉前两个字节中的时间戳和Base64编码的处理，可以将GUID的长度从32个字符减少到10个字符。
        ///  最后，只选择Base64字符串的前8个字符作为名称。
        /// </summary>
        /// <returns></returns>
        public static string Generate()
        {
            var guid = Guid.NewGuid();
            var bytes = guid.ToByteArray();

            // Get rid of the first couple of bytes which contain timestamps
            bytes = bytes.Skip(2).ToArray();

            // Base64 url-encode the remaining bytes
            var base64String = Convert.ToBase64String(bytes)
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "");

            // Take the first 8 characters
            return base64String.Substring(0, 8);
        }
    }
}
