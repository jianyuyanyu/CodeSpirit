using CodeSpirit.Amis.App;

namespace CodeSpirit.Amis
{
    public interface ISiteConfigurationService
    {
        Task<ApiResponse<AmisApp>> GetSiteConfigurationAsync();
    }
}
