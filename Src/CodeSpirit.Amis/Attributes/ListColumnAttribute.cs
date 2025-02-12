namespace CodeSpirit.Amis.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ListColumnAttribute : AmisColumnAttribute
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Placeholder { get; set; }

        public ListColumnAttribute(string title = null, string subTitle = null, string placeholder = null)
        {
            Title = title;
            SubTitle = subTitle;
            Placeholder = placeholder;
        }
    }
}
