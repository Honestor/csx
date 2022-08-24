using Framework.Timing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Mvc.ModelBinding
{
    public class DateTimeModelBinder : IModelBinder
    {
        private readonly Type _type;
        private readonly SimpleTypeModelBinder _simpleTypeModelBinder;
        private readonly IClock _clock;
        public DateTimeModelBinder(ModelBinderProviderContext context)
        {
            _type = context.Metadata.ModelType;
            _clock = context.Services.GetRequiredService<IClock>();
            _simpleTypeModelBinder = new SimpleTypeModelBinder(context.Metadata.ModelType,
                context.Services.GetRequiredService<ILoggerFactory>());
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            await _simpleTypeModelBinder.BindModelAsync(bindingContext);

            if (!bindingContext.Result.IsModelSet)
            {
                return;
            }

            if (_type == typeof(DateTime))
            {
                var dateTime = (DateTime)bindingContext.Result.Model;
                bindingContext.Result = ModelBindingResult.Success(_clock.Normalize(dateTime));
            }
            else
            {
                var dateTime = (DateTime?)bindingContext.Result.Model;
                if (dateTime != null)
                {
                    bindingContext.Result = ModelBindingResult.Success(_clock.Normalize(dateTime.Value));
                }
            }
        }
    }
}
