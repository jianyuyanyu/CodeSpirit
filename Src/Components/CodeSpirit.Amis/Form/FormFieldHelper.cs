// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Form.Fields;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;
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

            // 添加常规字段，支持表单项组
            fields.AddRange(ProcessParametersWithGroups(parameters, dtoType));

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

            // 处理表单项组
            var groupedFields = ProcessFormGroups(properties, dtoType);
            fields.AddRange(groupedFields);

            return fields;
        }

        /// <summary>
        /// 从属性集合生成带表单项组的AMIS表单字段配置
        /// </summary>
        /// <param name="properties">属性集合</param>
        /// <param name="dtoType">DTO类型（用于检测表单项组特性）</param>
        /// <returns>AMIS字段配置列表</returns>
        public List<JObject> GetAmisFormFieldsFromPropertiesWithGroups(IEnumerable<PropertyInfo> properties, Type dtoType)
        {
            return ProcessFormGroups(properties, dtoType);
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

        /// <summary>
        /// 处理表单项组，将属性按组织织并生成相应的AMIS配置
        /// </summary>
        /// <param name="properties">属性集合</param>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>包含分组的字段配置列表</returns>
        private List<JObject> ProcessFormGroups(IEnumerable<PropertyInfo> properties, Type dtoType)
        {
            List<JObject> result = [];
            var propertyList = properties?.ToList() ?? [];

            // 获取类型上的表单项组特性
            var groupAttributes = dtoType?.GetCustomAttributes<FormGroupAttribute>(true)?.ToList() ?? [];

            if (!groupAttributes.Any())
            {
                // 如果没有定义组，直接返回常规字段
                return GetAmisFormFieldsFromProperties(propertyList);
            }

            // 按Order排序组
            groupAttributes = groupAttributes.OrderBy(g => g.Order).ToList();

            // 创建字段名到属性的映射
            var propertyMap = propertyList.Where(ShouldProcess)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            // 跟踪已分组的字段
            var groupedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 处理每个组
            foreach (var groupAttr in groupAttributes)
            {
                var groupConfig = CreateGroupFromAttribute(groupAttr);
                if (groupConfig == null) continue;

                var groupFields = new List<JObject>();

                // 解析组内字段
                if (!string.IsNullOrEmpty(groupAttr.Fields))
                {
                    var fieldNames = groupAttr.Fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(f => f.Trim());

                    foreach (var fieldName in fieldNames)
                    {
                        if (propertyMap.TryGetValue(fieldName, out var property))
                        {
                            var field = ProcessProperty(property);
                            if (field != null)
                            {
                                groupFields.Add(field);
                                groupedProperties.Add(fieldName);
                            }
                        }
                    }
                }

                // 将字段添加到组的body中
                if (groupFields.Any())
                {
                    groupConfig["body"] = new JArray(groupFields);
                    result.Add(groupConfig);
                    result.Add(new JObject { ["type"] = "divider" } ); // 添加分隔符
                }
            }

            // 添加未分组的字段
            var ungroupedProperties = propertyList.Where(p => 
                ShouldProcess(p) && !groupedProperties.Contains(p.Name));

            foreach (var property in ungroupedProperties)
            {
                var field = ProcessProperty(property);
                if (field != null)
                {
                    result.Add(field);
                }
            }

            return result;
        }

        /// <summary>
        /// 处理方法参数，支持表单项组
        /// </summary>
        /// <param name="parameters">方法参数集合</param>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>字段配置列表</returns>
        private List<JObject> ProcessParametersWithGroups(IEnumerable<ParameterInfo> parameters, Type dtoType)
        {
            var parameterList = parameters?.ToList() ?? [];
            
            // 如果只有一个复杂类型参数，且该参数类型有表单项组特性，则使用该参数类型的属性进行分组
            var complexParam = parameterList.FirstOrDefault(p => !_utilityHelper.IsSimpleType(p.ParameterType));
            if (complexParam != null && complexParam.ParameterType.HasFormGroups())
            {
                var properties = complexParam.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                return ProcessFormGroups(properties, complexParam.ParameterType);
            }
            
            // 如果指定了dtoType且有表单项组特性，使用dtoType的属性进行分组
            if (dtoType != null && dtoType.HasFormGroups())
            {
                var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                return ProcessFormGroups(properties, dtoType);
            }
            
            // 否则使用原有的参数处理逻辑
            return GetAmisFormFieldsFromParameters(parameterList);
        }

        /// <summary>
        /// 从FormGroupAttribute创建组配置
        /// </summary>
        /// <param name="groupAttr">表单项组特性</param>
        /// <returns>组配置</returns>
        private JObject CreateGroupFromAttribute(FormGroupAttribute groupAttr)
        {
            if (groupAttr == null) return null;

            // 使用工厂创建组字段（这里模拟特性在类型上的情况）
            var factory = _fieldFactories.FirstOrDefault(f => f.CanHandle(typeof(FormGroupAttribute)));
            if (factory is FormGroupFieldFactory groupFactory)
            {
                // 创建一个临时的特性提供者来传递给工厂
                var attributeProvider = new FormGroupAttributeProvider(groupAttr);
                return groupFactory.CreateField(attributeProvider, _utilityHelper);
            }

            // 如果没有找到工厂，手动创建基础配置
            var jObj= new JObject
            {
                ["type"] = "group",
                ["body"] = new JArray()
            };

            if (!string.IsNullOrEmpty(groupAttr.Title))
            {
                jObj["label"] = groupAttr.Title;
            }
            return jObj;
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