using System.Net;
using Polly;

namespace PaymentExecution.Common.Extensions;

public static class ResiliencePipelineExtension
{
    public static PredicateBuilder<object> HandleTransientErrors(this PredicateBuilder predicateBuilder)
    {
        return predicateBuilder
            .HandleResult(HandleResult)
            .Handle<HttpRequestException>(HandleHttpRequestException);

        static bool HandleResult(object result)
        {
            if (result is not HttpResponseMessage responseMessage)
            {
                return false;
            }

            return IsStatusCodeTransientError(responseMessage.StatusCode);
        }

        static bool HandleHttpRequestException(HttpRequestException ex)
        {
            return ex.StatusCode != null && IsStatusCodeTransientError(ex.StatusCode.Value);
        }

        static bool IsStatusCodeTransientError(HttpStatusCode statusCode)
            => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
    }
}
