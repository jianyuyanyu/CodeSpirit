// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Form.Fields;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Attributes;
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
        private readonly AiFormFieldEnhancer _aiEnhancer;

        /// <summary>
        /// 初始化表单字段帮助类实例
        /// </summary>
        /// <param name="permissionService">权限校验服务</param>
        /// <param name="utilityHelper">通用工具类</param>
        /// <param name="fieldFactories">字段工厂集合</param>
        /// <param name="aiEnhancer">AI表单字段增强器</param>
        /// <exception cref="ArgumentNullException">当任何参数为null时抛出</exception>
        public FormFieldHelper(
            IHasPermissionService permissionService,
            UtilityHelper utilityHelper,
            IEnumerable<IAmisFieldFactory> fieldFactories,
            AiFormFieldEnhancer aiEnhancer)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _utilityHelper = utilityHelper ?? throw new ArgumentNullException(nameof(utilityHelper));
            _fieldFactories = fieldFactories?.ToList() ?? throw new ArgumentNullException(nameof(fieldFactories));
            _aiEnhancer = aiEnhancer ?? throw new ArgumentNullException(nameof(aiEnhancer));
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

        /// <summary>
        /// 从属性集合生成AMIS表单字段配置
        /// </summary>
        /// <param name="properties">属性集合</param>
        /// <returns>AMIS字段配置列表</returns>
        public List<JObject> GetAmisFormFieldsFromProperties(IEnumerable<PropertyInfo> properties)
        {
            List<JObject> fields = [];

            foreach (PropertyInfo prop in properties ?? Enumerable.Empty<PropertyInfo>())
            {
                if (!ShouldProcess(prop))
                {
                    continue;
                }

                // 优先使用工厂创建字段
                JObject factoryField = CreateFieldUsingFactories(prop);
                if (factoryField != null)
                {
                    fields.Add(factoryField);
                    continue;
                }

                JObject field = ProcessProperty(prop);
                if (field != null)
                {
                    fields.Add(field);
                }
            }

            return fields;
        }

        /// <summary>
        /// 从方法参数生成带全局AI组件的AMIS表单字段配置
        /// </summary>
        /// <param name="parameters">方法参数集合</param>
        /// <param name="dtoType">DTO类型（用于检测AI填充特性，可选）</param>
        /// <returns>AMIS字段配置列表</returns>
        public List<JObject> GetAmisFormFieldsFromParameters(IEnumerable<ParameterInfo> parameters, Type dtoType)
        {
            List<JObject> fields = [];

            // 如果指定了DTO类型，尝试创建全局AI填充组件
            if (dtoType != null)
            {
                var globalAiComponent = _aiEnhancer.CreateGlobalAiFillComponent(dtoType);
                if (globalAiComponent != null)
                {
                    fields.Add(globalAiComponent);
                }
            }
            else
            {
                // 如果没有指定DTO类型，尝试从第一个复杂类型参数推断
                var firstComplexParam = parameters?.FirstOrDefault(p => !_utilityHelper.IsSimpleType(p.ParameterType));
                if (firstComplexParam != null)
                {
                    var globalAiComponent = _aiEnhancer.CreateGlobalAiFillComponent(firstComplexParam.ParameterType);
                    if (globalAiComponent != null)
                    {
                        fields.Add(globalAiComponent);
                    }
                }
            }

            // 添加常规字段
            fields.AddRange(GetAmisFormFieldsFromParameters(parameters));

            return fields;
        }

        /// <summary>
        /// 从属性集合生成带全局AI组件的AMIS表单字段配置
        /// </summary>
        /// <param name="properties">属性集合</param>
        /// <param name="dtoType">DTO类型（用于检测AI填充特性）</param>
        /// <returns>AMIS字段配置列表</returns>
        public List<JObject> GetAmisFormFieldsFromProperties(IEnumerable<PropertyInfo> properties, Type dtoType)
        {
            List<JObject> fields = [];

            // 尝试创建全局AI填充组件
            var globalAiComponent = _aiEnhancer.CreateGlobalAiFillComponent(dtoType);
            if (globalAiComponent != null)
            {
                fields.Add(globalAiComponent);
            }

            // 添加常规字段
            fields.AddRange(GetAmisFormFieldsFromProperties(properties));

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
        internal JObject CreateFieldUsingFactories(ICustomAttributeProvider member)
        {
            try
            {
                // 获取成员上的所有特性
                var attributes = GetCustomAttributes(member);
                if (!attributes.Any())
                {
                    return null;
                }

                // 查找匹配的工厂
                foreach (var attr in attributes)
                {
                    foreach (var factory in _fieldFactories)
                    {
                        if (factory.CanHandle(attr.GetType()))
                        {
                            var field = factory.CreateField(member, _utilityHelper);
                            if (field != null)
                            {
                                return field;
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                // 异常处理 - 记录日志或其他处理
                return null;
            }
        }

        /// <summary>
        /// 获取成员上的所有自定义特性
        /// </summary>
        private IEnumerable<Attribute> GetCustomAttributes(ICustomAttributeProvider member)
        {
            try
            {
                if (member is MemberInfo memberInfo)
                {
                    return Attribute.GetCustomAttributes(memberInfo, true);
                }
                else if (member is ParameterInfo parameterInfo)
                {
                    return Attribute.GetCustomAttributes(parameterInfo, true);
                }
                
                return member.GetCustomAttributes(true).Cast<Attribute>();
            }
            catch
            {
                return Enumerable.Empty<Attribute>();
            }
        }

        /// <summary>
        /// 处理单个参数生成字段配置
        /// </summary>
        private IEnumerable<JObject> ProcessParameter(ParameterInfo param)
        {
            if (_utilityHelper.IsSimpleType(param.ParameterType))
            {
                var field = param.CreateFormField();
                if (field != null)
                {
                    field = ApplyAiEnhancement(field, param);
                }
                return [field];
            }
            else
            {
                return ProcessComplexType(param);
            }
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
            // 优先使用工厂创建字段
            var field = CreateFieldUsingFactories(prop);
            
            // 如果工厂未创建字段，使用默认方法创建
            if (field == null)
            {
                field = prop.CreateFormField();
                
                // 对于非工厂创建的字段，也应用AI增强功能
                if (field != null)
                {
                    field = ApplyAiEnhancement(field, prop);
                }
            }
            
            return field;
        }

        /// <summary>
        /// 应用AI增强功能到字段
        /// </summary>
        /// <param name="field">字段配置</param>
        /// <param name="member">成员信息</param>
        /// <returns>增强后的字段配置</returns>
        private JObject ApplyAiEnhancement(JObject field, ICustomAttributeProvider member)
        {
            if (field == null || member == null) return field;
            
            // 获取DTO类型
            var dtoType = GetDtoTypeFromContext(member);
            if (dtoType != null && dtoType != typeof(object))
            {
                // 应用AI增强
                field = _aiEnhancer.EnhanceField(field, member, dtoType);
            }
            
            return field;
        }

        /// <summary>
        /// 从上下文获取DTO类型
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <returns>DTO类型</returns>
        private Type GetDtoTypeFromContext(ICustomAttributeProvider member)
        {
            // 如果是属性，获取其声明类型
            if (member is PropertyInfo property)
            {
                return property.DeclaringType;
            }

            // 如果是参数，尝试获取其声明方法的类型
            if (member is ParameterInfo parameter)
            {
                return parameter.Member?.DeclaringType;
            }

            return typeof(object); // 返回默认类型而不是null
        }
        #endregion

        #region 权限与忽略规则
        /// <summary>
        /// 检查参数编辑权限
        /// </summary>
        private bool HasEditPermission(ParameterInfo param)
        {
            if (param == null)
            {
                return false;
            }

            PermissionAttribute permissionAttr = param.GetAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Name);
        }

        /// <summary>
        /// 检查属性编辑权限
        /// </summary>
        private bool HasEditPermission(PropertyInfo prop)
        {
            PermissionAttribute permissionAttr = prop.GetAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Name);
        }

        /// <summary>
        /// 判断是否忽略参数
        /// </summary>
        private bool IsIgnoredParameter(ParameterInfo param)
        {
            return param != null && param.Name.Equals("id", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断是否忽略属性
        /// </summary>
        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            return prop != null && prop.Name.Equals("CreatedDate", StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}