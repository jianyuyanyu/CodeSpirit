using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Amis.Middleware
{
    public class AmisMiddleware
    {
        private const string SITE_PATH = "/amis/site";
        private readonly RequestDelegate _next;

        public AmisMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 检查是否是AMIS相关的请求
            if (IsAmisRequest(context))
            {
                await HandleAmisRequest(context);
                return;
            }
            // 不是AMIS请求，继续管道
            await _next(context);
        }

        private bool IsAmisRequest(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Options)
            {
                return false;
            }

            string path = context.Request.Path.Value ?? string.Empty;

            // 检查是否是 site 请求
            if (path.EndsWith(SITE_PATH, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 检查查询参数是否包含 amis
            return context.Request.QueryString.HasValue &&
                context.Request.QueryString.Value.Contains("?amis", StringComparison.OrdinalIgnoreCase);
        }

        //private string GetControllerName(Endpoint endpoint)
        //{
        //    if (endpoint == null)
        //    {
        //        return null;
        //    }

        //    // 方法 1: 通过 ControllerActionDescriptor (原方法)
        //    string controllerName = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName;
        //    return !string.IsNullOrEmpty(controllerName) ? controllerName : null;
        //}

        private async Task HandleAmisRequest(HttpContext context)
        {
            if (context.Request.Path.Value.EndsWith("/amis/site"))
            {
                ISiteConfigurationService _siteConfigurationService = context.RequestServices.GetRequiredService<ISiteConfigurationService>();
                ApiResponse<App.AmisApp> siteConfig = await _siteConfigurationService.GetSiteConfigurationAsync();
                await WriteJsonResponse(context, siteConfig);
                return;
            }

            Endpoint endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                context.Response.StatusCode = 404;
                await WriteJsonResponse(context, new { message = "Endpoint not found." });
                return;
            }

            AmisGenerator _amisGenerator = context.RequestServices.GetRequiredService<AmisGenerator>();
            // 处理普通的AMIS配置请求
            if (context.Request.Path.Value.EndsWith("Statistics", StringComparison.CurrentCultureIgnoreCase))
            {
                JObject statisticsJson = _amisGenerator.GenerateStatisticsAmisJson(endpoint);
                await WriteJsonResponse(context, statisticsJson);
                return;
            }

            JObject amisJson = _amisGenerator.GenerateAmisJsonForController(endpoint);
            if (amisJson == null)
            {
                context.Response.StatusCode = 404;
                await WriteJsonResponse(context, new { message = $"AMIS JSON for controller '{endpoint?.DisplayName}' not found or not supported." });
                return;
            }

            await WriteJsonResponse(context, amisJson);
        }

        private static async Task WriteJsonResponse(HttpContext context, object data)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            try
            {
                await context.Response.WriteAsync(
                    Newtonsoft.Json.JsonConvert.SerializeObject(data, new Newtonsoft.Json.JsonSerializerSettings
                    {
                        Formatting = Newtonsoft.Json.Formatting.None,
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    }));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Error serializing response", message = ex.Message });
            }
        }
    }
}