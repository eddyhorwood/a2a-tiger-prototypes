using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PaymentExecutionService.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class LowerCaseIgnoreNullActionFilter : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var settings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(new LowerCaseNamingPolicy()) },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            context.Result = new JsonResult(objectResult.Value, settings)
            {
                StatusCode = objectResult.StatusCode
            };
        }

        base.OnActionExecuted(context);
    }
}

public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) =>
        name.ToLowerInvariant();
}
