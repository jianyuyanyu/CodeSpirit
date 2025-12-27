using AutoMapper;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Dtos.Permission;
using CodeSpirit.Navigation.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.System;

/// <summary>
/// 系统平台权限管理控制器
/// </summary>
[DisplayName("权限管理")]
[Navigation(Icon = "fa-solid fa-key", PlatformType = PlatformType.System, TitleResourceKey = "Controller.SystemPermissions", TitleResourceType = typeof(NavigationResources))]
public class SystemPermissionsController : ApiControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IMapper _mapper;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionService">权限服务</param>
    /// <param name="mapper">对象映射器</param>
    public SystemPermissionsController(IPermissionService permissionService, IMapper mapper)
    {
        _permissionService = permissionService;
        _mapper = mapper;
    }

    /// <summary>
    /// 获取系统权限列表
    /// </summary>
    /// <returns>系统权限列表</returns>
    [HttpGet]
    [DisplayName("获取系统权限列表")]
    public ActionResult<ApiResponse<PageList<PermissionDto>>> GetSystemPermissions()
    {
        List<PermissionNode> permissions = _permissionService.GetPermissionTree();

        // 使用扩展方法过滤出系统级权限
        List<PermissionNode> systemPermissions = permissions.GetSystemPermissions();

        List<PermissionDto> permissionDtos = _mapper.Map<List<PermissionDto>>(systemPermissions);

        PageList<PermissionDto> listData = new(permissionDtos, permissionDtos.Count);

        return SuccessResponse(listData);
    }

    /// <summary>
    /// 获取系统权限树(根节点)
    /// </summary>
    /// <returns>系统权限树列表</returns>
    [HttpGet("tree")]
    [DisplayName("获取系统权限树")]
    public ActionResult<ApiResponse<List<PermissionTreeDto>>> GetSystemPermissionTree()
    {
        var permissions = _permissionService.GetPermissionTree();
        
        // 使用扩展方法过滤出系统级权限
        var systemPermissions = permissions.GetSystemPermissions();

        var treeNodes = systemPermissions.Where(p => p.Name != "default").Select(p => new PermissionTreeDto
        {
            Id = p.Name,
            Label = p.DisplayName,
            Value = p.Name,
            Defer = p.Children.Any(),
            Icon = "fa fa-folder"
        }).ToList();

        return SuccessResponse(treeNodes);
    }

    /// <summary>
    /// 获取指定系统权限的子权限
    /// </summary>
    /// <param name="id">父权限标识</param>
    /// <returns>子权限树节点</returns>
    [HttpGet("tree/{id}")]
    [DisplayName("获取系统子权限")]
    public ActionResult<ApiResponse<PermissionTreeDeferDto>> GetSystemPermissionChildren(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("无效的权限标识");
        }

        var allPermissions = _permissionService.GetPermissionTree();
        var systemPermissions = allPermissions.GetSystemPermissions();
        var node = FindNode(systemPermissions, id);
        
        if (node == null)
        {
            return NotFound("未找到指定的系统权限");
        }

        var result = node.Children.Select(c => new PermissionTreeDto
        {
            Id = c.Name,
            Label = c.DisplayName,
            Value = c.Name,
            Defer = c.Children.Any(),
            Icon = GetMethodIcon(c.RequestMethod)
        }).ToList();

        return SuccessResponse(new PermissionTreeDeferDto { Options = result });
    }

    /// <summary>
    /// 获取系统权限统计信息
    /// </summary>
    /// <returns>权限统计信息</returns>
    [HttpGet("statistics")]
    [DisplayName("获取系统权限统计")]
    public ActionResult<ApiResponse<object>> GetSystemPermissionStatistics()
    {
        try
        {
            var permissions = _permissionService.GetPermissionTree();
            var systemPermissions = permissions.GetSystemPermissions();

            object statistics = new
            {
                TotalSystemPermissions = CountAllPermissions(systemPermissions),
                SystemModules = systemPermissions.Count,
                SystemControllers = systemPermissions.Sum(p => p.Children.Count),
                SystemActions = systemPermissions.Sum(p => CountAllActions(p)),
                LastUpdated = DateTimeOffset.Now
            };

            return SuccessResponse(statistics);
        }
        catch (Exception)
        {
            return BadResponse<object>("获取系统权限统计失败");
        }
    }



    /// <summary>
    /// 在权限树中查找指定节点
    /// </summary>
    private PermissionNode FindNode(List<PermissionNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (node.Name == id)
            {
                return node;
            }

            if (node.Children?.Any() == true)
            {
                var found = FindNode(node.Children, id);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 根据HTTP方法获取对应的图标
    /// </summary>
    private string GetMethodIcon(string method)
    {
        return method?.ToUpper() switch
        {
            "GET" => "fa fa-eye",           // 查看操作
            "POST" => "fa fa-plus",         // 新增操作
            "PUT" => "fa fa-edit",          // 编辑操作
            "DELETE" => "fa fa-trash",      // 删除操作
            _ => "fa fa-cog"                // 默认图标使用齿轮图标
        };
    }

    /// <summary>
    /// 递归计算权限节点总数
    /// </summary>
    private int CountAllPermissions(List<PermissionNode> permissions)
    {
        int count = permissions.Count;
        foreach (var permission in permissions)
        {
            if (permission.Children?.Any() == true)
            {
                count += CountAllPermissions(permission.Children);
            }
        }
        return count;
    }

    /// <summary>
    /// 递归计算权限节点下的操作数量
    /// </summary>
    private int CountAllActions(PermissionNode permission)
    {
        int count = 0;
        if (permission.Children?.Any() == true)
        {
            foreach (var child in permission.Children)
            {
                // 如果子节点还有子节点，递归计算
                if (child.Children?.Any() == true)
                {
                    count += CountAllActions(child);
                }
                else
                {
                    // 叶子节点算作一个操作
                    count++;
                }
            }
        }
        return count;
    }
} 