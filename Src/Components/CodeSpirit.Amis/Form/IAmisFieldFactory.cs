// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    public interface IAmisFieldFactory
    {
        /// <summary>
        /// 尝试根据成员信息创建 AMIS 字段配置。
        /// </summary>
        /// <param name="member">成员信息（参数或属性）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <returns>如果成功创建则返回字段配置，否则返回 null。</returns>
        JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper);
    }
}
