# CodeSpirit Statistics Cards Guide

## Overview

CodeSpirit provides an automatic statistics cards generation feature that allows developers to automatically generate statistics indicator cards at the top of pages through strongly-typed configuration on controllers. Statistics cards display key metric data in card format, supporting automatic refresh, responsive layout, and custom styling.

![image-20260109154846653](../../Res/image-20260109154846653.png)

### Key Features

- ✅ **Strongly-Typed Configuration**: Use generic attributes + configuration classes for complete compile-time type checking
- ✅ **Automatic Layout**: Automatically generate responsive grid layouts that adapt to different screen sizes
- ✅ **Auto Refresh**: Support configurable auto-refresh intervals for real-time statistic updates
- ✅ **Type Safety**: All configurations are validated at compile time, avoiding runtime errors
- ✅ **Easy Extension**: Fluent API design with clear and intuitive configuration
- ✅ **Style Customization**: Support custom icons, color themes, and card spacing

## Use Cases

Statistics cards are suitable for scenarios where key metrics need to be displayed at the top of the page, such as:

- **Today's execution count, success count, failure count, success rate**
- **Total orders, pending orders, completed orders**
- **Total users, active users, new users**
- **System resource usage, error rate, response time**

## Quick Start

### Step 1: Create Statistics Cards Configuration Class

Create a configuration class in the `Configuration/Statistics` directory:

```csharp
using CodeSpirit.Amis.StatisticsCards;

namespace YourApi.Configuration.Statistics;

/// <summary>
/// Scheduled task statistics cards configuration
/// </summary>
public class ScheduledTaskStatisticsConfig : StatisticsCardsConfigBase
{
    public override void Configure(StatisticsCardsBuilder builder)
    {
        builder
            .SetApi("statistics/cards")
            .SetRefreshInterval(60)
            .SetColumnsCount(4)
            .SetGap(15)
            .AddCard("todayExecutions", "Today's Executions")
                .WithIcon("fa-play-circle")
                .WithColor(CardColor.Info)
            .AddCard("todaySuccessExecutions", "Today's Success")
                .WithIcon("fa-check-circle")
                .WithColor(CardColor.Success)
            .AddCard("todayFailedExecutions", "Today's Failures")
                .WithIcon("fa-times-circle")
                .WithColor(CardColor.Danger)
            .AddCard("successRate", "Success Rate")
                .WithIcon("fa-chart-line")
                .WithColor(CardColor.Warning);
    }
}
```

### Step 2: Apply Attribute to Controller

Add the `StatisticsCards` attribute to the controller class:

```csharp
using CodeSpirit.Amis.Attributes;
using YourApi.Configuration.Statistics;

[DisplayName("Scheduled Tasks")]
[StatisticsCards<ScheduledTaskStatisticsConfig>]
public class ScheduledTasksController : ApiControllerBase
{
    // Controller code...
}
```

### Step 3: Add Statistics API

Add an API method in the controller to return statistics cards data:

```csharp
/// <summary>
/// Get statistics cards data
/// </summary>
[HttpGet("statistics/cards")]
[DisplayName("Get Statistics Cards")]
public async Task<ActionResult<ApiResponse>> GetStatisticsCards()
{
    var stats = await _queryService.GetTaskStatisticsAsync();

    var data = new
    {
        todayExecutions = stats.TodayExecutions,
        todaySuccessExecutions = stats.TodaySuccessExecutions,
        todayFailedExecutions = stats.TodayFailedExecutions,
        successRate = $"{stats.SuccessRate:F1}%"
    };

    return Ok(ApiResponse<object>.Success(data));
}
```

## Configuration Reference

### StatisticsCardsBuilder Methods

| Method | Parameters | Description | Default Value |
|--------|-----------|-------------|---------------|
| `SetApi` | `string api` | Statistics data API relative path | `"statistics/cards"` |
| `SetRefreshInterval` | `int seconds` | Auto-refresh interval (seconds), 0 means no auto-refresh | `0` |
| `SetColumnsCount` | `int count` | Number of card columns per row | `4` |
| `SetGap` | `int gap` | Card spacing (pixels) | `15` |
| `AddCard` | `string field, string title` | Add a card, returns builder for continued configuration | - |
| `WithIcon` | `string icon` | Set FontAwesome icon for current card | - |
| `WithColor` | `CardColor color` | Set color theme for current card | - |

### CardColor Enumeration

| Enum Value | Description | Use Cases |
|-----------|-------------|------------|
| `Info` | Info color (blue) | General information display |
| `Success` | Success color (green) | Success metrics |
| `Warning` | Warning color (yellow) | Metrics requiring attention |
| `Danger` | Danger color (red) | Error or exception metrics |
| `Primary` | Primary color | Main metrics |
| `Secondary` | Secondary color | Secondary metrics |

## Configuration Examples

### Basic Configuration

```csharp
public class BasicStatisticsConfig : StatisticsCardsConfigBase
{
    public override void Configure(StatisticsCardsBuilder builder)
    {
        builder
            .SetApi("statistics/cards")
            .AddCard("totalCount", "Total Count")
                .WithIcon("fa-database")
                .WithColor(CardColor.Primary)
            .AddCard("activeCount", "Active Count")
                .WithIcon("fa-check-circle")
                .WithColor(CardColor.Success);
    }
}
```

### Complete Configuration

```csharp
public class FullStatisticsConfig : StatisticsCardsConfigBase
{
    public override void Configure(StatisticsCardsBuilder builder)
    {
        builder
            .SetApi("statistics/cards")
            .SetRefreshInterval(60)        // Auto-refresh every 60 seconds
            .SetColumnsCount(4)            // 4 cards per row
            .SetGap(20)                    // 20px card spacing
            .AddCard("todayOrders", "Today's Orders")
                .WithIcon("fa-shopping-cart")
                .WithColor(CardColor.Info)
            .AddCard("pendingOrders", "Pending")
                .WithIcon("fa-clock")
                .WithColor(CardColor.Warning)
            .AddCard("completedOrders", "Completed")
                .WithIcon("fa-check-circle")
                .WithColor(CardColor.Success)
            .AddCard("cancelledOrders", "Cancelled")
                .WithIcon("fa-times-circle")
                .WithColor(CardColor.Danger);
    }
}
```

## API Data Format

The statistics cards API should return a flattened JSON object where field names correspond to the `field` parameters in the configuration:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "todayExecutions": 125,
    "todaySuccessExecutions": 118,
    "todayFailedExecutions": 7,
    "successRate": "94.4%"
  }
}
```

## Page Layout

Statistics cards are automatically displayed at the top of the page, before the CRUD list or Tabs:

```
┌─────────────────────────────────────────────────┐
│  [📊 Today's Executions]  [✅ Today's Success]  [❌ Today's Failures]  [📈 Success Rate] │
│      125            118            7           94.4%   │
└─────────────────────────────────────────────────┘
│  [Filter]  [Refresh]  [Add]  [Bulk Actions]  ...        │
│                                                  │
│  Task list table ...                                │
```

## Responsive Layout

Statistics cards support responsive layout and automatically adjust based on screen size:

- **Large screens (≥992px)**: Display configured number of columns (default 4 columns)
- **Medium screens (768px-991px)**: Automatically adjust to 2 columns
- **Small screens (<768px)**: Automatically adjust to 1 column

## Best Practices

### 1. Field Naming Convention

Use camelCase naming, consistent with API returned field names:

```csharp
.AddCard("todayExecutions", "Today's Executions")      // ✅ Recommended
.AddCard("TodayExecutions", "Today's Executions")      // ❌ Not recommended
```

### 2. Icon Selection

Use FontAwesome icons with semantic clarity:

```csharp
.WithIcon("fa-play-circle")      // ✅ Execution related
.WithIcon("fa-check-circle")     // ✅ Success related
.WithIcon("fa-times-circle")     // ✅ Failure related
.WithIcon("fa-chart-line")       // ✅ Statistics related
```

### 3. Color Theme Selection

Choose appropriate colors based on metric meaning:

- **Success metrics**: Use `CardColor.Success` (green)
- **Error metrics**: Use `CardColor.Danger` (red)
- **Warning metrics**: Use `CardColor.Warning` (yellow)
- **General information**: Use `CardColor.Info` (blue)

### 4. Refresh Interval Setting

Set reasonable refresh intervals based on data update frequency:

- **Real-time data**: 30-60 seconds
- **Near real-time data**: 60-300 seconds
- **Static data**: Do not set auto-refresh (`SetRefreshInterval(0)`)

### 5. Card Count

It's recommended to display 2-4 cards per row; too many will affect readability:

```csharp
.SetColumnsCount(4)  // ✅ Recommended: 2-4 cards
.SetColumnsCount(6)  // ⚠️ Not recommended: too many cards
```

## FAQ

### Q: Statistics cards not displaying?

**A:** Check the following:
1. Whether the `StatisticsCards<TConfig>` attribute is correctly applied to the controller
2. Whether the configuration class correctly inherits `StatisticsCardsConfigBase`
3. Whether the API path is correctly configured
4. Whether the API returns data in the correct format

### Q: How to customize card styles?

**A:** Statistics cards use the Amis Card component. Styles can be customized through CSS class names. Cards automatically apply the `statistics-card` class name.

### Q: Are multiple statistics card groups supported?

**A:** Currently, each controller supports only one group of statistics cards. If you need multiple statistics areas, you can add more cards in the configuration class.

### Q: How is statistics card data updated?

**A:** There are two ways:
1. **Auto-refresh**: Set refresh interval via `SetRefreshInterval`
2. **Manual refresh**: Page reload or automatic reload after CRUD operations

## Technical Architecture

Statistics cards functionality is implemented based on the following components:

- **StatisticsCardsAttribute<TConfig>**: Generic attribute specifying the configuration class
- **StatisticsCardsConfigBase**: Configuration base class defining the configuration interface
- **StatisticsCardsBuilder**: Fluent API builder
- **StatisticsCardsHelper**: Configuration parser and JSON generator

Statistics cards are automatically embedded in the Page component's body array as the first element displayed.

## Related Documentation

- [Page Top Tabs Guide](./codespirit-page-tabs-guide-zh-CN.md)
- [Amis Card Mode Guide](./codespirit-amis-card-mode-guide-zh-CN.md)
- [Amis Engine Documentation](./codespirit-amis-engine-zh-CN.md)
