using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Builders;

/// <summary>
/// 表格卡片建构器
/// </summary>
public class TableCardBuilder : IUdlCardBuilder<TableCardConfig>, IUdlCardBuilderBase
{
    private readonly ILogger<TableCardBuilder> _logger;

    /// <summary>
    /// 无参构造函数（用于测试）
    /// </summary>
    public TableCardBuilder() : this(NullLogger<TableCardBuilder>.Instance)
    {
    }

    /// <summary>
    /// 带日志的构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    public TableCardBuilder(ILogger<TableCardBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 支持的卡片类型
    /// </summary>
    public string CardType => "table";

    /// <summary>
    /// 构建Amis表格卡片配置
    /// </summary>
    public Dictionary<string, object> Build(TableCardConfig cardConfig)
    {
        var card = new Dictionary<string, object>
        {
            ["type"] = "table",
            ["id"] = cardConfig.Id,
            ["className"] = "amis-cards-table"
        };

        // 构建列配置
        if (cardConfig.Table?.Columns?.Count > 0)
        {
            card["columns"] = cardConfig.Table.Columns.Select(col =>
            {
                var column = new Dictionary<string, object>
                {
                    ["name"] = col.Name,
                    ["label"] = col.Label,
                    ["type"] = col.Type,
                    ["sortable"] = col.Sortable,
                    ["searchable"] = col.Searchable
                };

                // 设置列宽
                if (!string.IsNullOrEmpty(col.Width))
                {
                    column["width"] = col.Width;
                }

                // 设置对齐方式
                if (!string.IsNullOrEmpty(col.Align))
                {
                    column["align"] = col.Align;
                }

                // 设置固定列
                if (!string.IsNullOrEmpty(col.Fixed))
                {
                    column["fixed"] = col.Fixed;
                }

                // 设置数据映射（用于状态显示）
                if (col.Mapping?.Count > 0)
                {
                    column["map"] = col.Mapping;
                }

                // 设置自定义模板
                if (!string.IsNullOrEmpty(col.Template))
                {
                    column["tpl"] = col.Template;
                }

                // 设置显示条件
                if (!string.IsNullOrEmpty(col.VisibleOn))
                {
                    column["visibleOn"] = col.VisibleOn;
                }

                // 设置格式化配置
                if (col.Format != null)
                {
                    if (!string.IsNullOrEmpty(col.Format.DateFormat))
                    {
                        column["format"] = col.Format.DateFormat;
                    }
                    
                    if (col.Format.DecimalPlaces.HasValue)
                    {
                        column["precision"] = col.Format.DecimalPlaces.Value;
                    }

                    if (col.Format.ShowSeparator)
                    {
                        column["separator"] = true;
                    }

                    if (!string.IsNullOrEmpty(col.Format.CurrencySymbol))
                    {
                        column["currency"] = col.Format.CurrencySymbol;
                    }

                    if (col.Format.TruncateLength.HasValue)
                    {
                        column["truncate"] = col.Format.TruncateLength.Value;
                    }
                }

                return column;
            }).ToList();
        }

        // 设置表格样式配置
        if (cardConfig.Table != null)
        {
            if (cardConfig.Table.ShowIndex)
            {
                card["showIndex"] = true;
            }

            if (cardConfig.Table.ShowSelection)
            {
                card["showSelection"] = true;
            }

            if (!string.IsNullOrEmpty(cardConfig.Table.Size))
            {
                card["size"] = cardConfig.Table.Size;
            }

            if (!cardConfig.Table.ShowBorder)
            {
                card["bordered"] = false;
            }

            if (!cardConfig.Table.ShowStripe)
            {
                card["striped"] = false;
            }

            if (!cardConfig.Table.ShowHover)
            {
                card["hover"] = false;
            }

            // 分页配置
            if (cardConfig.Table.Pagination?.Enabled == true)
            {
                var pagination = new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["layout"] = new[] { "total", "sizes", "pager", "jumper" }
                };

                if (cardConfig.Table.Pagination.PageSizeOptions?.Count > 0)
                {
                    pagination["sizes"] = cardConfig.Table.Pagination.PageSizeOptions;
                }

                if (!cardConfig.Table.Pagination.ShowQuickJumper)
                {
                    pagination["layout"] = new[] { "total", "sizes", "pager" };
                }

                if (!cardConfig.Table.Pagination.ShowTotal)
                {
                    var layout = pagination["layout"] as string[];
                    pagination["layout"] = layout?.Where(x => x != "total").ToArray();
                }

                card["pagination"] = pagination;
            }
            else if (cardConfig.Table.Pagination?.Enabled == false)
            {
                card["pagination"] = false;
            }
        }

        // 构建数据源
        if (cardConfig.Data.StaticData?.Count > 0)
        {
            card["data"] = cardConfig.Data.StaticData.ToList();
        }
        else if (!string.IsNullOrEmpty(cardConfig.Data.ApiUrl))
        {
            card["api"] = new Dictionary<string, object>
            {
                ["method"] = "get",
                ["url"] = cardConfig.Data.ApiUrl
            };
        }

        // 设置默认分页大小
        if (cardConfig.Data.PageSize > 0)
        {
            card["perPage"] = cardConfig.Data.PageSize;
        }

        // 设置默认排序
        if (!string.IsNullOrEmpty(cardConfig.Data.DefaultSort))
        {
            card["orderBy"] = cardConfig.Data.DefaultSort;
            card["orderDir"] = cardConfig.Data.DefaultSortOrder ?? "asc";
        }

        // 设置标题和描述
        if (!string.IsNullOrEmpty(cardConfig.Title))
        {
            card["title"] = cardConfig.Title;
        }

        if (!string.IsNullOrEmpty(cardConfig.Description))
        {
            card["description"] = cardConfig.Description;
        }

        // 设置主题
        if (!string.IsNullOrEmpty(cardConfig.Theme))
        {
            card["theme"] = cardConfig.Theme;
        }

        // 设置标题和描述
        if (!string.IsNullOrEmpty(cardConfig.Title))
        {
            card["title"] = cardConfig.Title;
        }

        if (!string.IsNullOrEmpty(cardConfig.Description))
        {
            card["description"] = cardConfig.Description;
        }

        return card;
    }

    /// <summary>
    /// 验证卡片配置
    /// </summary>
    public bool Validate(TableCardConfig cardConfig)
    {
        if (cardConfig.Table.Columns.Count == 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 构建Amis卡片配置（非泛型接口实现）
    /// </summary>
    Dictionary<string, object> IUdlCardBuilderBase.Build(UdlCardConfig cardConfig)
    {
        if (cardConfig is not TableCardConfig tableConfig)
        {
            throw new ArgumentException($"配置类型不匹配，期望 {nameof(TableCardConfig)}");
        }
        return Build(tableConfig);
    }

    /// <summary>
    /// 验证卡片配置（非泛型接口实现）
    /// </summary>
    bool IUdlCardBuilderBase.Validate(UdlCardConfig cardConfig)
    {
        if (cardConfig is not TableCardConfig tableConfig)
        {
            return false;
        }
        return Validate(tableConfig);
    }
} 