// Controllers/AuthController.cs
using CodeSpirit.Core;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ApiControllerBase
    {

        /// <summary>
        /// 处理图片上传
        /// </summary>
        /// <param name="file">上传的图片文件</param>
        /// <returns>返回符合 Amis InputImage 组件预期的响应格式</returns>
        [HttpPost("avatar")]
        public Task<ActionResult<ApiResponse<ImageDto>>> UploadAvatar([FromForm] IFormFile file)
        {
            return Task.FromResult(SuccessResponse(new ImageDto
            {
                value = "",
                filename = "logo",
                url = "https://xin-lai.com/imgs/xinlai-logo_9d2c29c2794e6a173738bf92b056ab69.png"
            }));
        }
    }

    public class ImageDto
    {
        public string value { get; set; }

        public string filename { get; set; }

        public string url { get; set; }
    }
}