// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Authorization;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    /// <summary>
    /// AMIS 表单字段生成帮助类
    /// <para>提供从方法参数生成AMIS表单字段配置的核心逻辑</para>
    /// </summary>
    public class FormFieldHelper
    {
        private readonly IPermissionService _permissionService;
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
            IPermissionService permissionService,
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
                ? [CreateFormField(param)]
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
            return CreateFieldUsingFactories(prop) ?? CreateFormField(prop);
        }
        #endregion

        #region 字段创建
        /// <summary>
        /// 创建基础字段配置
        /// </summary>
        private JObject CreateFormField(ICustomAttributeProvider member)
        {
            (string name, Type type, string label) = GetMemberMetadata(member);
            bool isRequired = IsRequired(member);

            JObject field = new()
            {
                ["name"] = name,
                ["label"] = label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(type)
            };

            AddCommonValidations(member, field);
            AddTypeSpecificConfigurations(member, type, field);

            return field;
        }

        /// <summary>
        /// 获取成员元数据
        /// </summary>
        private (string name, Type type, string label) GetMemberMetadata(ICustomAttributeProvider member)
        {
            return member switch
            {
                ParameterInfo p => (
                    _utilityHelper.GetFieldName(p, null),
                    p.ParameterType,
                    _utilityHelper.GetDisplayName(p)
                ),
                PropertyInfo prop => (
                    _utilityHelper.GetFieldName(prop, null),
                    prop.PropertyType,
                    _utilityHelper.GetDisplayName(prop)
                ),
                _ => throw new NotSupportedException("不支持除参数和属性外的其他成员类型")
            };
        }

        /// <summary>
        /// 判断字段是否必填
        /// </summary>
        private bool IsRequired(ICustomAttributeProvider member)
        {
            return member.GetAttribute<RequiredAttribute>() != null
                || IsTypeRequired(member.GetMemberType());
        }


        /// <summary>
        /// 根据类型判断是否必填
        /// </summary>
        private bool IsTypeRequired(Type type) =>
            !_utilityHelper.IsNullable(type);

        /// <summary>
        /// 获取字段类型映射
        /// </summary>
        private string GetFormFieldType(Type type)
        {
            if (type.IsEnumType())
            {
                return "select";
            }

            return type.IsDateType()
                ? "datetime"
                : type switch
                {
                    Type t when t == typeof(string) => "input-text",
                    Type t when t == typeof(bool) => "switch",
                    Type t when t.IsNumericType() => "input-number",
                    Type t when t.IsImageType() => "image",
                    _ => "input-text"
                };
        }
        #endregion

        #region 验证规则
        /// <summary>
        /// 添加通用验证规则
        /// </summary>
        private void AddCommonValidations(ICustomAttributeProvider member, JObject field)
        {
            AddValidationAttributes(member, field);
            AddDescription(member, field);
        }

        /// <summary>
        /// 添加验证特性配置
        /// </summary>
        private void AddValidationAttributes(ICustomAttributeProvider member, JObject field)
        {
            JObject validations = [];
            JObject errors = [];

            ProcessStringLengthAttribute(member, validations, errors);
            ProcessRangeAttribute(member, validations, errors);
            ProcessRegexAttribute(member, validations, errors);
            ProcessDataTypeAttribute(member, validations, field);

            if (validations.HasValues)
            {
                field["validations"] = validations;
            }

            if (errors.HasValues)
            {
                field["validationErrors"] = errors;
            }
        }

        /// <summary>
        /// 处理字符串长度验证
        /// </summary>
        private void ProcessStringLengthAttribute(ICustomAttributeProvider member, JObject validations, JObject errors)
        {
            StringLengthAttribute attr = member.GetAttribute<StringLengthAttribute>();
            if (attr == null)
            {
                return;
            }

            if (attr.MinimumLength > 0)
            {
                validations["minLength"] = attr.MinimumLength;
            }

            if (attr.MaximumLength > 0)
            {
                validations["maxLength"] = attr.MaximumLength;
            }

            if (!string.IsNullOrEmpty(attr.ErrorMessage))
            {
                errors["minLength"] = attr.ErrorMessage;
                errors["maxLength"] = attr.ErrorMessage;
            }
        }

        /// <summary>
        /// 处理数值范围验证
        /// </summary>
        private void ProcessRangeAttribute(ICustomAttributeProvider member, JObject validations, JObject errors)
        {
            RangeAttribute attr = member.GetAttribute<RangeAttribute>();
            if (attr == null)
            {
                return;
            }

            if (attr.Minimum != null)
            {
                validations["minimum"] = Convert.ToDouble(attr.Minimum);
            }

            if (attr.Maximum != null)
            {
                validations["maximum"] = Convert.ToDouble(attr.Maximum);
            }

            if (!string.IsNullOrEmpty(attr.ErrorMessage))
            {
                errors["minimum"] = attr.ErrorMessage;
                errors["maximum"] = attr.ErrorMessage;
            }
        }

        /// <summary>
        /// 处理正则表达式验证
        /// </summary>
        private void ProcessRegexAttribute(ICustomAttributeProvider member, JObject validations, JObject errors)
        {
            RegularExpressionAttribute attr = member.GetAttribute<RegularExpressionAttribute>();
            if (attr == null)
            {
                return;
            }

            validations["matchRegexp"] = attr.Pattern;
            if (!string.IsNullOrEmpty(attr.ErrorMessage))
            {
                errors["matchRegexp"] = attr.ErrorMessage;
            }
        }

        /// <summary>
        /// 处理数据类型验证
        /// </summary>
        private void ProcessDataTypeAttribute(ICustomAttributeProvider member, JObject validations, JObject field)
        {
            DataTypeAttribute attr = member.GetAttribute<DataTypeAttribute>();
            if (attr == null)
            {
                return;
            }

            switch (attr.DataType)
            {
                case DataType.EmailAddress:
                    validations["isEmail"] = true;
                    break;
                case DataType.Url:
                    validations["isUrl"] = true;
                    break;
                case DataType.ImageUrl:
                    HandleImageType(member, field);
                    break;
            }
        }

        /// <summary>
        /// 添加描述信息
        /// </summary>
        private void AddDescription(ICustomAttributeProvider member, JObject field)
        {
            string description = member.GetAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrEmpty(description))
            {
                field["description"] = description;
            }
        }
        #endregion

        #region 类型特定配置
        /// <summary>
        /// 添加类型相关特殊配置
        /// </summary>
        private void AddTypeSpecificConfigurations(ICustomAttributeProvider member, Type type, JObject field)
        {
            if (type.IsEnumType())
            {
                field["options"] = _utilityHelper.GetEnumOptions(type);
            }

            if (type.IsDateType())
            {
                field["format"] = "YYYY-MM-DD";
            }

            HandleImageType(member, field);
        }

        /// <summary>
        /// 处理图片类型字段的特殊配置
        /// </summary>
        private void HandleImageType(ICustomAttributeProvider member, JObject field)
        {
            DataTypeAttribute dataTypeAttr = member.GetAttribute<DataTypeAttribute>();
            if (dataTypeAttr?.DataType == DataType.ImageUrl)
            {
                field["type"] = "image";
                field["src"] = $"${{{field["name"]}}}";
                field["altText"] = field["label"];
            }

            if (member.GetMemberName().Contains("Avatar", StringComparison.OrdinalIgnoreCase))
            {
                field["type"] = "avatar";
                field["src"] = $"${{{field["name"]}}}";
            }
        }
        #endregion

        #region 权限与忽略规则
        /// <summary>
        /// 检查参数编辑权限
        /// </summary>
        private bool HasEditPermission(ParameterInfo param)
        {
            PermissionAttribute permissionAttr = param.GetAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 检查属性编辑权限
        /// </summary>
        private bool HasEditPermission(PropertyInfo prop)
        {
            PermissionAttribute permissionAttr = prop.GetAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
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