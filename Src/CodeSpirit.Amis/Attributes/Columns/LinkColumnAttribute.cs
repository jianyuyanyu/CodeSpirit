using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LinkColumnAttribute : Attribute
    {
        public string Href { get; set; }
        public string Blank { get; set; }
        public string Icon { get; set; }
        public string Label { get; set; }
    }
}