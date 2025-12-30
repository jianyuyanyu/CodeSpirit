using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Handlers;
using CodeSpirit.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Reflection;

namespace CodeSpirit.Amis.Controllers
{
    /// <summary>
    /// AMIS API 控制器基类
    /// </summary>
    public abstract class AmisApiControllerBase : ControllerBase
    {
        /// <summary>
        /// OPTIONS 请求处理
        /// </summary>
        [HttpOptions]
        public IActionResult Options() => Ok();

        /// <summary>
        /// 生成 CrudDialog 的 AMIS schema
        /// Service 组件会调用此方法获取 schema（仅初始化时调用一次，不会产生死循环）
        /// </summary>
        /// <param name="templateVariables">模板变量字典，用于替换 DataApi 中的变量（如 ${id}）</param>
        /// <returns>包含 body 字段的 schema 对象</returns>
        protected ActionResult<ApiResponse> GenerateCrudDialogSchema(Dictionary<string, string> templateVariables = null)
        {
            // 通过 ActionDescriptor 获取方法信息
            MethodInfo methodInfo = null;
            if (ControllerContext.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                methodInfo = controllerActionDescriptor.MethodInfo;
            }

            if (methodInfo == null)
            {
                return BadRequest(ApiResponse.Error(400, "无法获取方法信息"));
            }

            var operationAttr = methodInfo.GetCustomAttribute<CrudDialogOperationAttribute>();
            if (operationAttr == null)
            {
                return BadRequest(ApiResponse.Error(400, "必须使用 CrudDialogOperationAttribute"));
            }

            // 替换 DataApi 中的模板变量
            var dataApi = operationAttr.DataApi;
            if (templateVariables != null)
            {
                foreach (var kvp in templateVariables)
                {
                    dataApi = dataApi.Replace($"${{{kvp.Key}}}", kvp.Value);
                }
            }

            // 创建临时特性对象，替换模板变量
            var tempAttr = new CrudDialogOperationAttribute(
                operationAttr.Label,
                operationAttr.Api,
                operationAttr.ConfirmText,
                operationAttr.VisibleOn,
                operationAttr.IsBulkOperation)
            {
                DataApi = dataApi,
                DataType = operationAttr.DataType,
                Icon = operationAttr.Icon,
                DialogSize = operationAttr.DialogSize,
                EnablePagination = operationAttr.EnablePagination,
                PerPage = operationAttr.PerPage,
                PerPageOptions = operationAttr.PerPageOptions,
                EnableSearch = operationAttr.EnableSearch,
                EnableExport = operationAttr.EnableExport,
                EnableRefresh = operationAttr.EnableRefresh,
                RowActions = operationAttr.RowActions,
                CustomColumns = operationAttr.CustomColumns
            };

            // 注入 CrudDialogHandler 来生成 schema
            var crudDialogHandler = HttpContext.RequestServices.GetRequiredService<CrudDialogHandler>();
            var schema = crudDialogHandler.GenerateCrudDialogSchema(tempAttr);

            return Ok(ApiResponse<object>.Success(schema));
        }
    }
}
