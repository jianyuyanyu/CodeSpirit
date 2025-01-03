namespace CodeSpirit.Shared.Services.Dtos
{
    /// <summary>
    /// 通用API返回结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResult<T>
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; } = 0;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 成功
        /// </summary>
        public ApiResult<T> Successful(T data)
        {
            Code = 0;
            Data = data;
            return this;
        }

        /// <summary>
        /// 创建成功返回结果
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ApiResult<TData> CreateSuccessApiResult<TData>(TData data)
        {
            var result = new ApiResult<TData>()
            {
                Code = 0,
                Data = data
            };
            return result;
        }

        public static ApiResult<TData> CreateErrorApiResult<TData>(int code, string error)
        {
            var result = new ApiResult<TData>()
            {
                Code = code,
                Error = error,
                Data = default
            };
            return result;
        }
    }

    /// <summary>
    /// 通用API返回结果
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; } = 0;

        /// <summary>
        /// 错误消息
        /// </summary>
        /// <example>服务器繁忙，请稍后再试！</example>
        public virtual string Error { get; set; }

        public static ApiResult<object> CreateErrorApiResult(int code, string error)
        {
            var result = new ApiResult<object>()
            {
                Code = 0,
                Error = error,
                Data = default
            };
            return result;
        }

        /// <summary>
        /// 创建成功返回结果
        /// </summary>
        /// <returns></returns>
        public static ApiResult CreateSuccessApiResult()
        {
            var result = new ApiResult()
            {
                Code = 0,
            };
            return result;
        }
    }

    public class ApiValidationResult<T> : ApiResult<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public IList<string> Errors { get; set; }
    }
    public class ApiValidationResult : ApiResult
    {
        /// <summary>
        /// 
        /// </summary>
        public IList<string> Errors { get; set; }
    }
}
