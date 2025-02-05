
using Microsoft.AspNetCore.Http;

namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class IdentityAccessor : IIdentityAccessor, IDisposable
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public IdentityAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        private int? _tenantId;

        /// <summary>
        /// 登录用户ID
        /// </summary>
        public int? UserId
        {
            get
            {
                string userId = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (userId != null)
                {
                    int id = 0;
                    int.TryParse(userId, out id);
                    return id;
                }

                return null;
            }
        }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserName
        {
            get
            {
                string roleIds = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
                if (string.IsNullOrWhiteSpace(roleIds))
                {
                    return string.Empty;
                }

                return roleIds;
            }
        }

        /// <summary>
        /// 租户ID
        /// </summary>
        public int? TenantId
        {
            get
            {
                string tenant = _httpContextAccessor.HttpContext?.Request.Headers["TenantId"].ToString();
                if (string.IsNullOrEmpty(tenant))
                    tenant = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
                if (!string.IsNullOrEmpty(tenant))
                {
                    int tenantId = 0;
                    int.TryParse(tenant, out tenantId);
                    return tenantId;
                }

                return null;
            }

        }
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string Name => _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        /// <summary>
        /// 身份类型
        /// </summary>
        public int? IdentityType
        {
            get
            {
                string identity = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "identity")?.Value;
                if (identity != null)
                {
                    int identityType = 0;
                    int.TryParse(identity, out identityType);
                    return identityType;
                }

                return null;
            }

        }

        public int? RoleId
        {
            get
            {
                string roleId = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "role_id")?.Value;
                if (roleId != null)
                {
                    int id = 0;
                    int.TryParse(roleId, out id);
                    return id;
                }

                return null;
            }
        }

        private bool _disposed = false;
        private string _originalTenantId;
        public IDisposable ChangeTenantId(int tenantId)
        {
            _originalTenantId = TenantId.HasValue ? TenantId.ToString() : "";
            _httpContextAccessor.HttpContext.Request.Headers["TenantId"] = tenantId.ToString();

            return this;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Request.Headers["tenantId"] = _originalTenantId; // 还原原始租户 ID
                }
                _disposed = true;
            }
        }
    }
}
