using Microsoft.AspNetCore.Http;

namespace PaymentExecutionService.ProviderPactTests.Config;

public class ProviderStateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // We currently have no requirement to set up or tear down provider state. 
        // If future consumers have an interaction they require provider state for,
        // then we can implement it here.
        await next(context);
    }
}
