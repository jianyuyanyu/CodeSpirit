// Authorization/AuthorizationExtensions.cs
using CodeSpirit.IdentityApi.Data;

namespace CodeSpirit.IdentityApi.Authorization
{
    public static class AuthorizationExtensions
    {
        public static void AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // 使用 IServiceProvider 获取 ApplicationDbContext
                using (var serviceProvider = services.BuildServiceProvider())
                {
                    var dbContext = serviceProvider.GetService<ApplicationDbContext>();
                    if (dbContext != null)
                    {
                        var permissions = dbContext.Permissions.ToList();

                        foreach (var permission in permissions)
                        {
                            options.AddPolicy(permission.Name, policy =>
                                policy.Requirements.Add(new PermissionRequirement(permission.Name)));
                        }
                    }
                }
            });
        }
    }
}
