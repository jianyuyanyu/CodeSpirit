using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TplColumnAttribute : Attribute
    {
        public string Template { get; }

        public TplColumnAttribute(string template)
        {
            Template = template;
        }
    }
}