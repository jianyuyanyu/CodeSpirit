// Services/CustomSignInManager.cs
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CodeSpirit.IdentityApi.Services
{
    public class CustomSignInManager : SignInManager<ApplicationUser>
    {
        private readonly ApplicationDbContext _context;
        private readonly IClientIpService _clientIpService;

        public CustomSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation,
            ApplicationDbContext context,
            IClientIpService clientIpService)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _context = context;
            _clientIpService = clientIpService;
        }

        public override async Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, bool lockoutOnFailure)
        {
            SignInResult result = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);

            LoginLog loginLog = new LoginLog
            {
                UserId = user.Id,
                UserName = user.UserName,
                LoginTime = DateTime.UtcNow,
                IPAddress = _clientIpService.GetClientIpAddress(Context),
                IsSuccess = result.Succeeded,
                FailureReason = result.IsLockedOut ? "账户被锁定。" : !result.Succeeded ? "密码不正确。" : null
            };

            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();

            return result;
        }
    }
}
