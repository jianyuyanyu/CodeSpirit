using CodeSpirit.Core;
using CodeSpirit.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 默认AI表单填充控制器
    /// 提供通用的AI填充端点，供所有业务API使用
    /// </summary>
    [DisplayName("AI表单填充")]
    [Route("api/ai-form-fill")]
    public class AiFormFillController : ApiControllerBase
    {
        private readonly IAiFormFillService _aiFormFillService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="aiFormFillService">AI表单填充服务</param>
        public AiFormFillController(IAiFormFillService aiFormFillService)
        {
            _aiFormFillService = aiFormFillService;
        }

        /// <summary>
        /// 通用AI填充端点
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <param name="request">请求对象</param>
        /// <returns>AI填充结果</returns>
        [HttpPost("ai-fill")]
        [DisplayName("AI填充")]
        public async Task<ActionResult<ApiResponse<T>>> AiFill<T>([FromBody] T request) where T : class, new()
        {
            return await this.HandleAiFillAsync(_aiFormFillService, request);
        }

    }

    /// <summary>
    /// AI表单填充控制器基类
    /// 其他控制器可以继承此基类来获得默认的AI填充功能
    /// </summary>
    public abstract class AiEnabledControllerBase : ApiControllerBase
    {
        private readonly IAiFormFillService _aiFormFillService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="aiFormFillService">AI表单填充服务</param>
        protected AiEnabledControllerBase(IAiFormFillService aiFormFillService)
        {
            _aiFormFillService = aiFormFillService;
        }

        /// <summary>
        /// 默认AI填充端点
        /// 子类可以重写此方法来自定义行为
        /// </summary>
        /// <typeparam name="T">DTO类型</typeparam>
        /// <param name="request">请求对象</param>
        /// <returns>AI填充结果</returns>
        [HttpPost("ai-fill")]
        [DisplayName("AI填充")]
        public virtual async Task<ActionResult<ApiResponse<T>>> AiFill<T>([FromBody] T request) where T : class, new()
        {
            return await this.HandleAiFillAsync(_aiFormFillService, request);
        }

        /// <summary>
        /// 获取AI填充服务实例
        /// </summary>
        protected IAiFormFillService AiFormFillService => _aiFormFillService;
    }
}
