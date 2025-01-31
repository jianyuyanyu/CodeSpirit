using CodeSpirit.Amis.App;

namespace CodeSpirit.Amis.Services
{
    public interface IPageCollector
    {
        Task<Dictionary<string, Page>> CollectPagesAsync();
    }
}
