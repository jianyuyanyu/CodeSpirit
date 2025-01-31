using System;

namespace CodeSpirit.Core.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public class PermissionAttribute : Attribute
    {
        public string Permission { get; }

        public PermissionAttribute(string permission)
        {
            Permission = permission;
        }
    }
}
