using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Shared.ModelBindings
{
    public class DateRangeModelBinder : IModelBinder
    {
        private readonly ILogger<DateRangeModelBinder> _logger;

        // 构造函数接受ILogger实例
        public DateRangeModelBinder(ILogger<DateRangeModelBinder> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            _logger.LogDebug($"ModelName: {bindingContext.ModelName}, Value: {valueProviderResult.FirstValue}");


            if (valueProviderResult == ValueProviderResult.None)
            {
                _logger.LogWarning("No value found for model name: {ModelName}", bindingContext.ModelName);
                return Task.CompletedTask; // 没有值时无需处理
            }

            string value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                _logger.LogWarning("Received empty value for model name: {ModelName}", bindingContext.ModelName);
                return Task.CompletedTask;
            }

            // 记录接收到的值
            _logger.LogDebug("Received value for {ModelName}: {Value}", bindingContext.ModelName, value);
            // 解码并分割日期范围
            string[] values = value.Split(',');

            if (values.Length != 2)
            {
                _logger.LogError("Invalid date range format for {ModelName}: {Value}", bindingContext.ModelName, value);
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid date range format.");
                return Task.CompletedTask;
            }

            DateTime startDate, endDate;
            if (DateTime.TryParse(values[0], out startDate) && DateTime.TryParse(values[1], out endDate))
            {
                _logger.LogDebug("Parsed date range for {ModelName}: {StartDate} - {EndDate}", bindingContext.ModelName, startDate, endDate);
                bindingContext.Result = ModelBindingResult.Success(new DateTime[] { startDate, endDate });
            }
            else
            {
                _logger.LogError("Failed to parse dates for {ModelName}: {Value}", bindingContext.ModelName, value);
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid date format.");
            }

            return Task.CompletedTask;
        }
    }
}
