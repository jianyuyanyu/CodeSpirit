using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Form;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers;

/// <summary>
/// 侧边栏帮助类，用于生成页面aside配置
/// </summary>
public class AsideHelper
{
    private readonly FormFieldHelper _formFieldHelper;
    private readonly AmisContext _amisContext;

    /// <summary>
    /// 初始化AsideHelper
    /// </summary>
    /// <param name="formFieldHelper">表单字段帮助类</param>
    /// <param name="amisContext">Amis上下文</param>
    public AsideHelper(FormFieldHelper formFieldHelper, AmisContext amisContext)
    {
        _formFieldHelper = formFieldHelper;
        _amisContext = amisContext;
    }

    /// <summary>
    /// 检查是否需要生成aside配置
    /// </summary>
    /// <param name="queryDtoType">查询DTO类型</param>
    /// <returns>是否需要生成aside</returns>
    public bool ShouldGenerateAside(Type? queryDtoType)
    {
        if (queryDtoType == null) return false;

        // 检查查询DTO的属性上是否有PageAsideAttribute特性
        return queryDtoType.GetProperties()
            .Any(prop => prop.GetCustomAttribute<PageAsideAttribute>() != null);
    }

    /// <summary>
    /// 生成aside配置
    /// </summary>
    /// <param name="queryDtoType">查询DTO类型</param>
    /// <param name="crudName">CRUD组件名称，当PageAside的Target为空时使用此名称</param>
    /// <returns>aside配置的JSON对象</returns>
    public JObject? GenerateAsideConfig(Type? queryDtoType, string? crudName = null)
    {
        if (queryDtoType == null) return null;

        // 获取带有PageAsideAttribute特性的属性
        var asideProperties = queryDtoType.GetProperties()
            .Where(prop => prop.GetCustomAttribute<PageAsideAttribute>() != null)
            .ToList();

        if (!asideProperties.Any()) return null;

        // 获取这些属性对应的表单字段
        var allFormFields = _formFieldHelper.GetAmisFormFieldsFromProperties(asideProperties);
        var asideFields = new List<JObject>();

        foreach (var formField in allFormFields)
        {
            var asideField = new JObject(formField);
            
            // 为树形选择字段添加特殊配置
            if (formField["type"]?.ToString() == "input-tree")
            {
                asideField["inputClassName"] = "no-border no-padder mt-1";
                asideField["heightAuto"] = true;
                asideField["submitOnChange"] = true;
                asideField["selectFirst"] = true;
                asideField["inputOnly"] = true;
            }
            
            asideFields.Add(asideField);
        }

        if (!asideFields.Any()) return null;

        // 使用第一个属性的配置作为默认配置
        var firstAsideAttr = asideProperties[0].GetCustomAttribute<PageAsideAttribute>();

        // 确定target值：优先使用特性中的Target，如果为空则使用crudName，最后回退到"window"
        string targetValue = "window"; // 默认值
        if (!string.IsNullOrWhiteSpace(firstAsideAttr?.Target))
        {
            targetValue = firstAsideAttr.Target;
        }
        else if (!string.IsNullOrWhiteSpace(crudName))
        {
            targetValue = crudName;
        }

        return new JObject
        {
            ["type"] = "form",
            ["wrapWithPanel"] = firstAsideAttr?.WrapWithPanel ?? false,
            ["target"] = targetValue,
            ["submitOnInit"] = firstAsideAttr?.SubmitOnInit ?? true,
            ["body"] = new JArray(asideFields)
        };
    }
}
