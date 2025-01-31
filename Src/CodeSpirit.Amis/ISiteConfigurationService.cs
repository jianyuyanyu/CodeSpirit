using CodeSpirit.Amis.App;
using CodeSpirit.Core;

namespace CodeSpirit.Amis
{
    public interface ISiteConfigurationService
    {
        Task<ApiResponse<AmisApp>> GetSiteConfigurationAsync();
    }
}
