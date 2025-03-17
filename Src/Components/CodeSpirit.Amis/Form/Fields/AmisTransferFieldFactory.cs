using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 穿梭框字段工厂类
    /// </summary>
    public class AmisTransferFieldFactory : AmisFieldAttributeFactoryBase
    {
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisTransferFieldAttribute attr) = CreateField<AmisTransferFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                field["source"] = attr.Source;
                field["valueField"] = attr.ValueField;
                field["labelField"] = attr.LabelField;
                field["searchable"] = attr.Searchable;
                field["multiple"] = attr.Multiple;
                field["joinValues"] = attr.JoinValues;
                field["delimiter"] = attr.Delimiter;
                field["showStats"] = attr.ShowStats;
                field["sortable"] = attr.Sortable;
                field["searchPlaceholder"] = attr.SearchPlaceholder;
                field["resultSearchPlaceholder"] = attr.ResultSearchPlaceholder;
                field["noDataText"] = attr.NoDataText;
                field["resultNoDataText"] = attr.ResultNoDataText;
                field["sourceLabel"] = attr.SourceLabel;
                field["targetLabel"] = attr.TargetLabel;
            }
            return field;
        }
    }
} 