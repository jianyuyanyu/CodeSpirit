using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace CodeSpirit.IdentityApi.ModelBindings
{
    public class DateRangeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            // 检查参数类型是否为 DateTime[]
            if (context.Metadata.ModelType == typeof(DateTime[]))
            {
                return new BinderTypeModelBinder(typeof(DateRangeModelBinder));
            }

            return null;
        }
    }
}
