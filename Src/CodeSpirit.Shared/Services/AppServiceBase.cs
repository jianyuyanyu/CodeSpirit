using CodeSpirit.Shared.Services.Dtos;

namespace CodeSpirit.Shared.Services
{
    public abstract class AppServiceBase
    {
        /// <summary>
        /// 调用结果异常时抛出服务异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="apiResult"></param>
        /// <exception cref="AppServiceException"></exception>
        public virtual void ThrowAppServiceExceptionWhenCallError<T>(ApiResult<T> apiResult)
        {
            if (apiResult != null && apiResult.Code != 0)
            {
                throw new AppServiceException(apiResult.Code, apiResult.Error);
            }
        }

        /// <summary>
        /// 调用结果异常时抛出服务异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="apiResult"></param>
        /// <exception cref="AppServiceException"></exception>
        public virtual void ThrowAppServiceExceptionWhenCallError(ApiResult apiResult)
        {
            if (apiResult != null && apiResult.Code != 0)
            {
                throw new AppServiceException(apiResult.Code, apiResult.Error);
            }
        }
    }
}