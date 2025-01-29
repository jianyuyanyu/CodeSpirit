using CodeSpirit.IdentityApi.Controllers.Dtos;

namespace CodeSpirit.IdentityApi.Amis
{
    public interface ISiteConfigurationService
    {
        ApiResponse<CodeSpirit.IdentityApi.Amis.App.App> GetSiteConfiguration();
    }
}
