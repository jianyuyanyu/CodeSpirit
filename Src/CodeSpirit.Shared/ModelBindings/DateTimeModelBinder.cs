using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

public class DateTimeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        // 获取前端传入的值
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        try
        {
            // 解析前端传入的本地时间
            DateTime localDateTime = DateTime.Parse(value);
            // 转换为UTC时间存储
            DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.Local);
            
            bindingContext.Result = ModelBindingResult.Success(utcDateTime);
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, 
                "Invalid datetime format.");
        }

        return Task.CompletedTask;
    }
} 