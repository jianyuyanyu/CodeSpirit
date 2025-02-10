// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Authorization;
using CodeSpirit.Core.Authorization;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    /// <summary>
    /// AMIS 表单字段生成帮助类
    /// <para>提供从方法参数生成AMIS表单字段配置的核心逻辑</para>
    /// </summary>
    public class FormFieldHelper
    {
        private readonly IHasPermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;
        private readonly IEnumerable<IAmisFieldFactory> _fieldFactories;

        /// <summary>
        /// 初始化表单字段帮助类实例
        /// </summary>
        /// <param name="permissionService">权限校验服务</param>
        /// <param name="utilityHelper">通用工具类</param>
        /// <param name="fieldFactories">字段工厂集合</param>
        /// <exception cref="ArgumentNullException">当任何参数为null时抛出</exception>
        public FormFieldHelper(
            IHasPermissionService permissionService,
            UtilityHelper utilityHelper,
            IEnumerable<IAmisFieldFactory> fieldFactories)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _utilityHelper = utilityHelper ?? throw new ArgumentNullException(nameof(utilityHelper));
            _fieldFactories = fieldFactories?.ToList() ?? throw new ArgumentNullException(nameof(fieldFactories));
        }

        /// <summary>
        /// 从方法参数生成AMIS表单字段配置
        /// </summary>
        /// <param name="parameters">方法参数集合</param>
        /// <returns>AMIS字段配置列表</returns>
        public List<JObject> GetAmisFormFieldsFromParameters(IEnumerable<ParameterInfo> parameters)
        {
            List<JObject> fields = [];

            foreach (ParameterInfo param in parameters ?? Enumerable.Empty<ParameterInfo>())
            {
                if (!ShouldProcess(param))
                {
                    continue;
                }

                // 优先使用工厂创建字段
                JObject factoryField = CreateFieldUsingFactories(param);
                if (factoryField != null)
                {
                    fields.Add(factoryField);
                    continue;
                }

                fields.AddRange(ProcessParameter(param));
            }

            return fields;
        }

        #region 处理逻辑
        /// <summary>
        /// 检查成员（参数或属性）是否可以处理（权限和忽略检查）。
        /// </summary>
        /// <param name="member">参数或属性的信息。</param>
        /// <returns>如果可以处理则返回 true，否则返回 false。</returns>
        private bool ShouldProcess(ICustomAttributeProvider member)
        {
            return member != null && member switch
            {
                ParameterInfo param => HasEditPermission(param) && !IsIgnoredParameter(param),
                PropertyInfo prop => HasEditPermission(prop) && !IsIgnoredProperty(prop),
                _ => false
            };
        }

        /// <summary>
        /// 使用注册的字段工厂创建字段配置
        /// </summary>
        private JObject CreateFieldUsingFactories(ICustomAttributeProvider member)
        {
            return _fieldFactories
                .Select(factory => factory.CreateField(member, _utilityHelper))
                .FirstOrDefault(field => field != null);
        }

        /// <summary>
        /// 处理单个参数生成字段配置
        /// </summary>
        private IEnumerable<JObject> ProcessParameter(ParameterInfo param)
        {
            return _utilityHelper.IsSimpleType(param.ParameterType)
                ? [param.CreateFormField()]
                : ProcessComplexType(param);
        }

        /// <summary>
        /// 处理复杂类型参数生成嵌套字段
        /// </summary>
        private IEnumerable<JObject> ProcessComplexType(ParameterInfo param)
        {
            return param.ParameterType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(ShouldProcess)
                .Select(ProcessProperty)
                .Where(field => field != null);
        }

        /// <summary>
        /// 处理单个属性生成字段配置
        /// </summary>
        private JObject ProcessProperty(PropertyInfo prop)
        {
            return CreateFieldUsingFactories(prop) ?? prop.CreateFormField();
        }
        #endregion
        
        #region 权限与忽略规则
        /// <summary>
        /// 检查参数编辑权限
        /// </summary>
        private bool HasEditPermission(ParameterInfo param)
        {
            PermissionAttribute permissionAttr = param.GetAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Code);
        }

        /// <summary>
        /// 检查属性编辑权限
        /// </summary>
        private bool HasEditPermission(PropertyInfo prop)
        {
            PermissionAttribute permissionAttr = prop.GetAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Code);
        }

        /// <summary>
        /// 判断是否忽略参数
        /// </summary>
        private bool IsIgnoredParameter(ParameterInfo param)
        {
            return param.Name.Equals("id", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断是否忽略属性
        /// </summary>
        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            return prop.Name.Equals("CreatedDate", StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}