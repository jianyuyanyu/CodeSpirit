using Microsoft.Extensions.Logging.Abstractions;
using CodeSpirit.UdlCards.Builders;
using CodeSpirit.UdlCards.Models;

namespace CodeSpirit.UdlCards.Tests;

/// <summary>
/// TableCardBuilder 单元测试
/// </summary>
public class TableCardBuilderTests
{
    private readonly TableCardBuilder _builder;

    public TableCardBuilderTests()
    {
        _builder = new TableCardBuilder(NullLogger<TableCardBuilder>.Instance);
    }

    [Fact]
    public void CardType_ShouldReturnTable()
    {
        // Act & Assert
        _builder.CardType.Should().Be("table");
    }

    [Fact]
    public void Build_WithMinimalConfig_ShouldReturnBasicCard()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格"
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("table");
        result["id"].Should().Be("test-table");
        result["className"].Should().Be("amis-cards-table");
    }

    [Fact]
    public void Build_WithTableConfig_ShouldIncludeTableProperties()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" },
                    new() { Name = "name", Label = "姓名", Type = "text", Sortable = true }
                },
                ShowIndex = true,
                ShowSelection = true,
                ShowStripe = true,
                Pagination = new TablePaginationConfig
                {
                    Enabled = true,
                    PageSizeOptions = new List<int> { 10, 20, 50 }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("columns");
        var columns = result["columns"] as System.Collections.ICollection;
        columns.Should().NotBeNull();
        columns!.Count.Should().Be(2);
    }

    [Fact]
    public void Build_WithStaticData_ShouldIncludeDataProperties()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            },
            Data = new TableDataConfig
            {
                StaticData = new List<Dictionary<string, object>>
                {
                    new() { ["id"] = 1, ["name"] = "张三" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("data");
        var data = result["data"] as System.Collections.ICollection;
        data.Should().NotBeNull();
        data!.Count.Should().Be(1);
    }

    [Fact]
    public void Build_WithApiData_ShouldIncludeApiProperties()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            },
            Data = new TableDataConfig
            {
                ApiUrl = "/api/users",
                PageSize = 20
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("api");
        var api = result["api"] as Dictionary<string, object>;
        api.Should().NotBeNull();
        api!["method"].Should().Be("get");
        api["url"].Should().Be("/api/users");
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithoutColumns_ShouldReturnFalse()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>()
            }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IUdlCardBuilderBase_Build_WithCorrectType_ShouldWork()
    {
        // Arrange
        var builder = _builder as CodeSpirit.UdlCards.Core.IUdlCardBuilderBase;
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格"
        };

        // Act
        var result = builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("table");
    }

    #region 列配置测试

    [Fact]
    public void Build_WithColumnMapping_ShouldIncludeMapField()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new()
                    {
                        Name = "status",
                        Label = "状态",
                        Type = "mapping",
                        Mapping = new Dictionary<string, object>
                        {
                            ["active"] = "<span class=\"label label-success\">活跃</span>",
                            ["inactive"] = "<span class=\"label label-danger\">非活跃</span>"
                        }
                    }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("columns");
        var columns = result["columns"] as List<Dictionary<string, object>>;
        columns.Should().NotBeNull();
        columns![0].Should().ContainKey("map");
        
        var mapping = columns[0]["map"] as Dictionary<string, object>;
        mapping.Should().NotBeNull();
        mapping!["active"].Should().Be("<span class=\"label label-success\">活跃</span>");
        mapping["inactive"].Should().Be("<span class=\"label label-danger\">非活跃</span>");
    }

    [Fact]
    public void Build_WithColumnTemplate_ShouldIncludeTplField()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new()
                    {
                        Name = "progress",
                        Label = "进度",
                        Type = "text",
                        Template = "<div class=\"progress\"><div class=\"progress-bar\" style=\"width: ${progress}%\">${progress}%</div></div>"
                    }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("columns");
        var columns = result["columns"] as List<Dictionary<string, object>>;
        columns.Should().NotBeNull();
        columns![0].Should().ContainKey("tpl");
        columns[0]["tpl"].Should().Be("<div class=\"progress\"><div class=\"progress-bar\" style=\"width: ${progress}%\">${progress}%</div></div>");
    }

    [Fact]
    public void Build_WithColumnFormat_ShouldIncludeFormatFields()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new()
                    {
                        Name = "createTime",
                        Label = "创建时间",
                        Type = "date",
                        Format = new TableColumnFormat
                        {
                            DateFormat = "YYYY-MM-DD HH:mm:ss"
                        }
                    },
                    new()
                    {
                        Name = "amount",
                        Label = "金额",
                        Type = "number",
                        Format = new TableColumnFormat
                        {
                            DecimalPlaces = 2,
                            ShowSeparator = true,
                            CurrencySymbol = "¥"
                        }
                    }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("columns");
        var columns = result["columns"] as List<Dictionary<string, object>>;
        columns.Should().NotBeNull();
        
        // 检查日期格式
        columns![0].Should().ContainKey("format");
        columns[0]["format"].Should().Be("YYYY-MM-DD HH:mm:ss");
        
        // 检查数字格式
        columns[1].Should().ContainKey("precision");
        columns[1]["precision"].Should().Be(2);
        columns[1].Should().ContainKey("separator");
        columns[1]["separator"].Should().Be(true);
        columns[1].Should().ContainKey("currency");
        columns[1]["currency"].Should().Be("¥");
    }

    [Fact]
    public void Build_WithColumnProperties_ShouldIncludeAllProperties()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new()
                    {
                        Name = "name",
                        Label = "姓名",
                        Type = "text",
                        Width = "120px",
                        Align = "center",
                        Fixed = "left",
                        VisibleOn = "data.showName",
                        Sortable = true,
                        Searchable = true
                    }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("columns");
        var columns = result["columns"] as List<Dictionary<string, object>>;
        columns.Should().NotBeNull();
        
        var column = columns![0];
        column["name"].Should().Be("name");
        column["label"].Should().Be("姓名");
        column["type"].Should().Be("text");
        column["width"].Should().Be("120px");
        column["align"].Should().Be("center");
        column["fixed"].Should().Be("left");
        column["visibleOn"].Should().Be("data.showName");
        column["sortable"].Should().Be(true);
        column["searchable"].Should().Be(true);
    }

    [Fact]
    public void Build_WithColumnWithoutOptionalProperties_ShouldNotIncludeEmptyFields()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new()
                    {
                        Name = "id",
                        Label = "ID",
                        Type = "text",
                        Sortable = false,
                        Searchable = false
                    }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("columns");
        var columns = result["columns"] as List<Dictionary<string, object>>;
        columns.Should().NotBeNull();
        
        var column = columns![0];
        column.Should().NotContainKey("width");
        column.Should().NotContainKey("fixed");
        column.Should().NotContainKey("visibleOn");
        column.Should().NotContainKey("map");
        column.Should().NotContainKey("tpl");
        column.Should().NotContainKey("format");
        
        // align 有默认值 "left"，所以会被包含
        column.Should().ContainKey("align");
        column["align"].Should().Be("left");
    }

    #endregion

    #region 表格样式配置测试

    [Fact]
    public void Build_WithTableStyleConfig_ShouldIncludeStyleProperties()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                },
                ShowIndex = true,
                ShowSelection = true,
                Size = "small",
                ShowBorder = true,
                ShowStripe = true,
                ShowHover = true
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("showIndex");
        result.Should().ContainKey("showSelection");
        result.Should().ContainKey("size");
        result["showIndex"].Should().Be(true);
        result["showSelection"].Should().Be(true);
        result["size"].Should().Be("small");
        
        // 默认值不会显式设置
        result.Should().NotContainKey("bordered");
        result.Should().NotContainKey("striped");
        result.Should().NotContainKey("hover");
    }

    [Fact]
    public void Build_WithDisabledTableStyles_ShouldSetFalseValues()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                },
                ShowBorder = false,
                ShowStripe = false,
                ShowHover = false
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("bordered");
        result.Should().ContainKey("striped");
        result.Should().ContainKey("hover");
        result["bordered"].Should().Be(false);
        result["striped"].Should().Be(false);
        result["hover"].Should().Be(false);
    }

    #endregion

    #region 分页配置测试

    [Fact]
    public void Build_WithPaginationEnabled_ShouldIncludePaginationConfig()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                },
                Pagination = new TablePaginationConfig
                {
                    Enabled = true,
                    PageSizeOptions = new List<int> { 10, 20, 50, 100 },
                    ShowQuickJumper = true,
                    ShowTotal = true
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("pagination");
        var pagination = result["pagination"] as Dictionary<string, object>;
        pagination.Should().NotBeNull();
        pagination!["enabled"].Should().Be(true);
        
        var layout = pagination["layout"] as string[];
        layout.Should().Contain("total");
        layout.Should().Contain("sizes");
        layout.Should().Contain("pager");
        layout.Should().Contain("jumper");
        
        var sizes = pagination["sizes"] as List<int>;
        sizes.Should().BeEquivalentTo(new[] { 10, 20, 50, 100 });
    }

    [Fact]
    public void Build_WithPaginationDisabled_ShouldSetPaginationFalse()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                },
                Pagination = new TablePaginationConfig
                {
                    Enabled = false
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("pagination");
        result["pagination"].Should().Be(false);
    }

    [Fact]
    public void Build_WithPaginationPartialConfig_ShouldAdaptLayout()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                },
                Pagination = new TablePaginationConfig
                {
                    Enabled = true,
                    ShowQuickJumper = false,
                    ShowTotal = false
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("pagination");
        var pagination = result["pagination"] as Dictionary<string, object>;
        pagination.Should().NotBeNull();
        
        var layout = pagination!["layout"] as string[];
        layout.Should().NotContain("total");
        layout.Should().NotContain("jumper");
        layout.Should().Contain("sizes");
        layout.Should().Contain("pager");
    }

    #endregion

    #region 数据配置测试

    [Fact]
    public void Build_WithDataConfig_ShouldIncludeDataProperties()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            },
            Data = new TableDataConfig
            {
                PageSize = 25,
                DefaultSort = "createTime",
                DefaultSortOrder = "desc",
                StaticData = new List<Dictionary<string, object>>
                {
                    new() { ["id"] = 1, ["name"] = "测试" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("perPage");
        result.Should().ContainKey("orderBy");
        result.Should().ContainKey("orderDir");
        result["perPage"].Should().Be(25);
        result["orderBy"].Should().Be("createTime");
        result["orderDir"].Should().Be("desc");
    }

    [Fact]
    public void Build_WithDefaultSortOnly_ShouldUseAscOrder()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            },
            Data = new TableDataConfig
            {
                DefaultSort = "name",
                StaticData = new List<Dictionary<string, object>>
                {
                    new() { ["id"] = 1, ["name"] = "测试" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("orderBy");
        result.Should().ContainKey("orderDir");
        result["orderBy"].Should().Be("name");
        result["orderDir"].Should().Be("asc");
    }

    #endregion

    #region 卡片配置测试

    [Fact]
    public void Build_WithCardConfig_ShouldIncludeCardProperties()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Description = "这是一个测试表格",
            Theme = "primary",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("title");
        result.Should().ContainKey("description");
        result.Should().ContainKey("theme");
        result["title"].Should().Be("测试表格");
        result["description"].Should().Be("这是一个测试表格");
        result["theme"].Should().Be("primary");
    }

    [Fact]
    public void Build_WithoutOptionalCardProperties_ShouldNotIncludeEmptyFields()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotContainKey("title");
        result.Should().NotContainKey("description");
        result.Should().NotContainKey("theme");
    }

    #endregion

    #region 边界情况和异常测试

    [Fact]
    public void Build_WithNullTable_ShouldHandleGracefully()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = null
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotBeNull();
        result["type"].Should().Be("table");
    }

    [Fact]
    public void Build_WithEmptyApiUrl_ShouldNotIncludeApiConfig()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            },
            Data = new TableDataConfig
            {
                ApiUrl = "", // 空字符串
                StaticData = null
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().NotContainKey("api");
        result.Should().NotContainKey("data");
    }

    [Fact]
    public void Build_WithBothStaticDataAndApi_ShouldPrioritizeStaticData()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            },
            Data = new TableDataConfig
            {
                ApiUrl = "/api/data",
                StaticData = new List<Dictionary<string, object>>
                {
                    new() { ["id"] = 1, ["name"] = "测试数据" }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("data");
        result.Should().NotContainKey("api", "静态数据应该优先于API配置");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidId_ShouldStillPassBasicValidation(string invalidId)
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = invalidId,
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "id", Label = "ID", Type = "text" }
                }
            }
        };

        // Act
        var result = _builder.Validate(config);

        // Assert
        result.Should().BeTrue("当前验证逻辑主要检查列配置");
    }

    [Fact]
    public void Build_WithColumnMappingAndSearch_ShouldIncludeSearchConfiguration()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new() { Name = "name", Label = "姓名", Type = "text", Searchable = true },
                    new() { Name = "email", Label = "邮箱", Type = "text", Searchable = true },
                    new() { Name = "status", Label = "状态", Type = "text", Searchable = false }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        var columns = result["columns"] as System.Collections.ICollection;
        columns.Should().NotBeNull();
        columns!.Count.Should().Be(3);
        
        // 验证搜索配置正确映射
        var columnList = result["columns"] as List<Dictionary<string, object>>;
        columnList![0]["searchable"].Should().Be(true);
        columnList[1]["searchable"].Should().Be(true);
        columnList[2]["searchable"].Should().Be(false);
    }

    [Fact]
    public void Build_WithComplexColumnFormat_ShouldHandleAllFormatOptions()
    {
        // Arrange
        var config = new TableCardConfig
        {
            Id = "test-table",
            Title = "测试表格",
            Table = new TableConfig
            {
                Columns = new List<TableColumn>
                {
                    new()
                    {
                        Name = "description",
                        Label = "描述",
                        Type = "text",
                        Format = new TableColumnFormat
                        {
                            TruncateLength = 50
                        }
                    }
                }
            }
        };

        // Act
        var result = _builder.Build(config);

        // Assert
        result.Should().ContainKey("columns");
        var columns = result["columns"] as List<Dictionary<string, object>>;
        columns.Should().NotBeNull();
        columns![0].Should().ContainKey("truncate");
        columns[0]["truncate"].Should().Be(50);
    }

    #endregion
} 