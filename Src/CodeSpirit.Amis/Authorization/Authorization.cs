// Authorization/PermissionRequirement.cs

// Authorization/PermissionRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace CodeSpirit.Amis.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionName { get; }

        public PermissionRequirement(string permissionName)
        {
            PermissionName = permissionName;
        }
    }
}
