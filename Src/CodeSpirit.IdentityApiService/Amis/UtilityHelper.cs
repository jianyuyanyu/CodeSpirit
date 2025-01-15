namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class UtilityHelper
    {
        public string ToTitleCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.Length == 1)
                return str.ToLower();

            return char.ToLower(str[0]) + str.Substring(1);
        }
    }
}

