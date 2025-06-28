using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 穿梭框字段工厂类
    /// </summary>
    public class AmisTransferFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisTransferFieldAttribute).IsAssignableFrom(attributeType);
        }

        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisTransferFieldAttribute attr) = CreateField<AmisTransferFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                // 基础数据源配置
                if (!string.IsNullOrEmpty(attr.Source))
                {
                    field["source"] = attr.Source;
                }
                
                // 懒加载配置
                if (!string.IsNullOrEmpty(attr.DeferApi))
                {
                    field["deferApi"] = attr.DeferApi;
                }
                
                // 选择模式配置
                if (!string.IsNullOrEmpty(attr.SelectMode) && attr.SelectMode != "list")
                {
                    field["selectMode"] = attr.SelectMode;
                }
                
                // 结果列表模式跟随选择模式配置
                if (attr.ResultListModeFollowSelect)
                {
                    field["resultListModeFollowSelect"] = attr.ResultListModeFollowSelect;
                }
                
                // 字段映射配置
                if (!string.IsNullOrEmpty(attr.ValueField) && attr.ValueField != "value")
                {
                    field["valueField"] = attr.ValueField;
                }
                
                if (!string.IsNullOrEmpty(attr.LabelField) && attr.LabelField != "label")
                {
                    field["labelField"] = attr.LabelField;
                }
                
                // 搜索和多选配置
                if (attr.Searchable)
                {
                    field["searchable"] = attr.Searchable;
                }
                
                if (!attr.Multiple)
                {
                    field["multiple"] = attr.Multiple;
                }
                
                if (!attr.JoinValues)
                {
                    field["joinValues"] = attr.JoinValues;
                }
                
                if (attr.ExtractValue)
                {
                    field["extractValue"] = attr.ExtractValue;
                }
                
                if (!string.IsNullOrEmpty(attr.Delimiter) && attr.Delimiter != ",")
                {
                    field["delimiter"] = attr.Delimiter;
                }
                
                // 显示配置
                if (!attr.ShowStats)
                {
                    field["showStats"] = attr.ShowStats;
                }
                
                if (attr.Sortable)
                {
                    field["sortable"] = attr.Sortable;
                }
                
                if (!attr.Clearable)
                {
                    field["clearable"] = attr.Clearable;
                }
                
                if (!attr.ShowDeleteBtn)
                {
                    field["showDeleteBtn"] = attr.ShowDeleteBtn;
                }
                
                if (!attr.ShowSelectAll)
                {
                    field["showSelectAll"] = attr.ShowSelectAll;
                }
                
                // 搜索提示配置
                if (!string.IsNullOrEmpty(attr.SearchPlaceholder) && attr.SearchPlaceholder != "请搜索")
                {
                    field["searchPlaceholder"] = attr.SearchPlaceholder;
                }
                
                if (!string.IsNullOrEmpty(attr.ResultSearchPlaceholder) && attr.ResultSearchPlaceholder != "请搜索")
                {
                    field["resultSearchPlaceholder"] = attr.ResultSearchPlaceholder;
                }
                
                if (attr.ResultSearchable)
                {
                    field["resultSearchable"] = attr.ResultSearchable;
                }
                
                // 空数据提示配置
                if (!string.IsNullOrEmpty(attr.NoDataText) && attr.NoDataText != "暂无数据")
                {
                    field["noDataText"] = attr.NoDataText;
                }
                
                if (!string.IsNullOrEmpty(attr.ResultNoDataText) && attr.ResultNoDataText != "暂无数据")
                {
                    field["resultNoDataText"] = attr.ResultNoDataText;
                }
                
                // 标题配置
                if (!string.IsNullOrEmpty(attr.SourceLabel) && attr.SourceLabel != "待选项")
                {
                    field["sourceLabel"] = attr.SourceLabel;
                }
                
                if (!string.IsNullOrEmpty(attr.TargetLabel) && attr.TargetLabel != "已选项")
                {
                    field["targetLabel"] = attr.TargetLabel;
                }
                
                // 分页配置
                if (attr.ShowPagination && attr.PerPage > 0)
                {
                    field["pagination"] = new JObject
                    {
                        ["perPage"] = attr.PerPage,
                        ["page"] = attr.Page
                    };
                }
                
                // 自定义搜索API
                if (!string.IsNullOrEmpty(attr.SearchApi))
                {
                    field["searchApi"] = attr.SearchApi;
                }
            }
            return field;
        }
    }
} 