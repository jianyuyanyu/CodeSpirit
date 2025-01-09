// 文件路径: Controllers/Dtos/ApiResponse.cs
namespace CodeSpirit.IdentityApi.Controllers.Dtos
{
    /// <summary>
    /// 通用 API 响应封装类
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ApiResponse<T> where T : class
    {
        /// <summary>
        /// 状态码，0 表示成功，非 0 表示错误
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public T Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(int status, string msg, T data)
        {
            Status = status;
            Msg = msg;
            Data = data;
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ApiResponse<T> Success(T data, string msg = "操作成功！")
        {
            return data == null ? throw new ArgumentNullException(nameof(data)) : new ApiResponse<T>(0, msg, data);
        }

        /// <summary>
        /// 创建错误响应
        /// </summary>
        public static ApiResponse<T> Error(int status, string msg, T data = null)
        {
            if (status == 0) throw new ArgumentException("Error status code cannot be 0.", nameof(status));
            if (string.IsNullOrWhiteSpace(msg)) throw new ArgumentException("Error message cannot be empty.", nameof(msg));
            return new ApiResponse<T>(status, msg, data);
        }
    }
}
