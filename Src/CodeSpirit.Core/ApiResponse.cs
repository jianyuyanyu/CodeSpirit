// 文件路径: Controllers/Dtos/ApiResponse.cs
using System;

namespace CodeSpirit.Core
{
    /// <summary>
    /// 跳转方式枚举
    /// </summary>
    public enum RedirectType
    {
        /// <summary>
        /// 当前窗口跳转
        /// </summary>
        Self = 0,
        
        /// <summary>
        /// 新窗口打开
        /// </summary>
        Blank = 1,
        
        /// <summary>
        /// 替换当前页面
        /// </summary>
        Replace = 2
    }

    /// <summary>
    /// 跳转信息
    /// </summary>
    public class RedirectInfo
    {
        /// <summary>
        /// 跳转地址
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// 跳转方式
        /// </summary>
        public RedirectType Type { get; set; } = RedirectType.Self;
        
        /// <summary>
        /// 延迟时间（毫秒）
        /// </summary>
        public int Delay { get; set; } = 0;
        
        /// <summary>
        /// 是否显示跳转提示
        /// </summary>
        public bool ShowMessage { get; set; } = true;
        
        /// <summary>
        /// 跳转提示文本
        /// </summary>
        public string Message { get; set; } = "正在跳转...";
    }

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
        
        /// <summary>
        /// 跳转信息
        /// </summary>
        public RedirectInfo Redirect { get; set; }

        public ApiResponse() { }

        public ApiResponse(int status, string msg, T data)
        {
            Status = status;
            Msg = msg;
            Data = data;
        }

        public ApiResponse(int status, string msg, T data, RedirectInfo redirect)
        {
            Status = status;
            Msg = msg;
            Data = data;
            Redirect = redirect;
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ApiResponse<T> Success(T data, string msg = "操作成功！")
        {
            return data == null ? throw new ArgumentNullException(nameof(data)) : new ApiResponse<T>(0, msg, data);
        }
        
        /// <summary>
        /// 创建成功响应并跳转
        /// </summary>
        public static ApiResponse<T> SuccessWithRedirect(T data, string url, string msg = "操作成功！", RedirectType redirectType = RedirectType.Self, int delay = 1500)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            
            return new ApiResponse<T>(0, msg, data, new RedirectInfo
            {
                Url = url,
                Type = redirectType,
                Delay = delay,
                Message = msg
            });
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

    public class ApiResponse : ApiResponse<string>
    {
        public ApiResponse(int status, string msg)
        {
            Status = status;
            Msg = msg;
        }
        
        public ApiResponse(int status, string msg, RedirectInfo redirect)
        {
            Status = status;
            Msg = msg;
            Redirect = redirect;
        }
        
        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ApiResponse Success(string msg = "操作成功！")
        {
            return new ApiResponse(0, msg);
        }
        
        /// <summary>
        /// 创建成功响应并跳转
        /// </summary>
        public static ApiResponse SuccessWithRedirect(string url, string msg = "操作成功！", RedirectType redirectType = RedirectType.Self, int delay = 1500)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            
            return new ApiResponse(0, msg, new RedirectInfo
            {
                Url = url,
                Type = redirectType,
                Delay = delay,
                Message = msg
            });
        }
        
        /// <summary>
        /// 创建错误响应
        /// </summary>
        public static ApiResponse Error(int status, string msg)
        {
            if (status == 0) throw new ArgumentException("Error status code cannot be 0.", nameof(status));
            if (string.IsNullOrWhiteSpace(msg)) throw new ArgumentException("Error message cannot be empty.", nameof(msg));
            return new ApiResponse(status, msg);
        }
    }
}
