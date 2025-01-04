// Services/CustomSignInManager.cs
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CodeSpirit.IdentityApi.Services
{
    public class CustomSignInManager : SignInManager<ApplicationUser>
    {
        private readonly ApplicationDbContext _context;

        public CustomSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation,
            ApplicationDbContext context)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _context = context;
        }

        public override async Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, bool lockoutOnFailure)
        {
            var result = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
            
            var loginLog = new LoginLog
            {
                UserId = user.Id,
                UserName = user.UserName,
                LoginTime = DateTime.UtcNow,
                IPAddress = Context.Connection.RemoteIpAddress?.ToString(),
                IsSuccess = result.Succeeded,
                FailureReason = result.IsLockedOut ? "账户被锁定。" : !result.Succeeded ? "密码不正确。" : null
            };

            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();

            return result;
        }
    }
}
