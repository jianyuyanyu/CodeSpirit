using CodeSpirit.Core;
using CodeSpirit.Shared.Services.Background.Dtos;
using CodeSpirit.Shared.Services.Files;
using CodeSpirit.Shared.Services.Files.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using CodeSpirit.Audit.Attributes;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 文件操作控制器
    /// </summary>
    [DisplayName("文件服务")]
    [ApiController]
    [Route("api/tempfiles")]
    //[Authorize(policy: "DynamicPermissions")]
    [AllowAnonymous]
    [NoAudit("文件操作控制器不需要审计")]
    public class TempFilesController : ApiControllerBase
    {
        private readonly ITempFileService _fileService;
        private readonly ILogger<TempFilesController> _logger;

        /// <summary>
        /// 初始化文件控制器
        /// </summary>
        /// <param name="fileService">文件服务</param>
        /// <param name="logger">日志服务</param>
        public TempFilesController(ITempFileService fileService, ILogger<TempFilesController> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取导出任务信息
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>导出任务信息</returns>
        [HttpGet("export-tasks/{taskId}")]
        [DisplayName("获取导出任务信息")]
        public async Task<ActionResult<ApiResponse<ExportTaskDto>>> GetExportTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return BadRequest(new ApiResponse<ExportTaskDto>(1, "任务ID不能为空", null));
            }

            try
            {
                _logger.LogInformation("获取导出任务信息，任务ID: {TaskId}", taskId);
                var taskInfo = await _fileService.GetExportTaskAsync(taskId);

                if (taskInfo == null)
                {
                    return NotFound(new ApiResponse<ExportTaskDto>(1, "找不到指定的导出任务", null));
                }

                return Ok(new ApiResponse<ExportTaskDto>(0, "获取成功", taskInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取导出任务信息时发生错误，任务ID: {TaskId}", taskId);
                return StatusCode(500, new ApiResponse<ExportTaskDto>(1, $"获取导出任务信息失败: {ex.Message}", null));
            }
        }

        /// <summary>
        /// 上传文件到分布式缓存
        /// </summary>
        /// <returns>上传结果</returns>
        [HttpPost("upload-to-cache")]
        [DisplayName("上传文件到缓存")]
        public async Task<ActionResult<ApiResponse<FileUploadResult>>> UploadToCache()
        {
            try
            {
                _logger.LogInformation("正在上传文件到分布式缓存");
                
                if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                {
                    return BadRequest(new ApiResponse<FileUploadResult>(1, "未找到上传的文件", null));
                }

                var file = Request.Form.Files[0];
                using var stream = file.OpenReadStream();
                var result = await _fileService.UploadToCacheAsync(stream, file.FileName, file.ContentType);

                return Ok(new ApiResponse<FileUploadResult>(0, "上传成功", result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传文件到分布式缓存时发生错误");
                return StatusCode(500, new ApiResponse<FileUploadResult>(1, $"上传文件失败: {ex.Message}", null));
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>文件流</returns>
        [HttpGet("download/{fileId}")]
        [DisplayName("下载文件")]
        public async Task<IActionResult> DownloadFile(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return BadRequest(new ApiResponse(1, "文件ID不能为空"));
            }

            try
            {
                _logger.LogInformation("正在下载文件，文件ID: {FileId}", fileId);
                var result = await _fileService.DownloadFileAsync(fileId);

                if (!result.IsSuccess)
                {
                    return NotFound(new ApiResponse(1, result.ErrorMessage ?? "找不到指定的文件"));
                }

                return base.DownloadFile(result.FileStream, result.FileName, result.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载文件时发生错误，文件ID: {FileId}", fileId);
                return StatusCode(500, new ApiResponse(1, $"下载文件失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>文件信息</returns>
        [HttpGet("{fileId}")]
        [DisplayName("获取文件信息")]
        public async Task<ActionResult<ApiResponse<TempFileInfo>>> GetFileInfo(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return BadRequest(new ApiResponse<TempFileInfo>(1, "文件ID不能为空", null));
            }

            try
            {
                _logger.LogInformation("获取文件信息，文件ID: {FileId}", fileId);
                var fileInfo = await _fileService.GetFileInfoAsync(fileId);

                if (fileInfo == null)
                {
                    return NotFound(new ApiResponse<FileInfo>(1, "找不到指定的文件", null));
                }

                return Ok(new ApiResponse<TempFileInfo>(0, "获取成功", fileInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件信息时发生错误，文件ID: {FileId}", fileId);
                return StatusCode(500, new ApiResponse<FileInfo>(1, $"获取文件信息失败: {ex.Message}", null));
            }
        }
    }
} 