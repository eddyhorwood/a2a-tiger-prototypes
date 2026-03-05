using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PaymentExecution.Domain;

[ExcludeFromCodeCoverage]
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ISkipLoggingBehavior)
        {
            return await next();
        }

        _logger.LogInformation("Handling {Message}", typeof(TRequest).Name);
        var response = await next();
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType)
        {
            _logger.LogInformation("Response {Message}", responseType.GetGenericArguments()[0]);
        }
        else
        {
            _logger.LogInformation("Response {Message}", typeof(TResponse).Name);
        }

        return response;
    }
}
