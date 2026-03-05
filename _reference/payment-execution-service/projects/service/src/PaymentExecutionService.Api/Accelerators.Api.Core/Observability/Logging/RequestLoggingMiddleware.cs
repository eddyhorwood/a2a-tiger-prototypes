// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.IdentityModel.Tokens.Jwt;
using Xero.Accelerators.Api.Core.Observability.Monitoring;
using Xero.Accelerators.Api.Core.Security.XeroIdentity;
using static Xero.Accelerators.Api.Core.Constants;

namespace Xero.Accelerators.Api.Core.Observability.Logging;
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IMonitoringService monitoringService)
{
    private const string MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode}";

    public const string NewRelicTraceId = "NewRelicTraceId";
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    //NOSONAR comment needed because we can not spilt this out because Invoke is a special method for middleware
    public async Task Invoke(HttpContext httpContext) //NOSONAR
    {
        if (httpContext is null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        var logTags = new Dictionary<string, string>();

        var xeroUserIdFeature = httpContext.Features.Get<XeroUserIdFeature>();
        if (xeroUserIdFeature is not null)
        {
            logTags[HttpHeaders.XeroUserId] = xeroUserIdFeature.XeroUserId.ToString();
        }

        TryAddLogTagFromHeader(logTags, httpContext.Request.Headers, HttpHeaders.XeroCorrelationId);
        TryAddLogTagFromHeader(logTags, httpContext.Request.Headers, HttpHeaders.XeroTenantId);
        var jwtClientId = GetClientIdFromJWTToken(httpContext);
        if (!string.IsNullOrEmpty(jwtClientId))
        {
            logTags[HttpHeaders.XeroClientName] = jwtClientId;
        }
        else
        {
            TryAddLogTagFromHeader(logTags, httpContext.Request.Headers, HttpHeaders.XeroClientName);
        }

        logTags[NewRelicTraceId] = monitoringService.GetTraceId();
        using (logger.BeginScope(logTags))
        {
            try
            {
                await _next(httpContext);

                var statusCode = httpContext.Response.StatusCode;

                var level = statusCode is >= 500 and < 600 ? LogLevel.Error : LogLevel.Information;

                logger.Log(level, MessageTemplate, httpContext.Request.Method, httpContext.Request.Path, statusCode);
            }
            catch (Exception ex)
            {
                monitoringService.ReportError(ex, logTags.Where(s => s.Key == HttpHeaders.XeroCorrelationId)
                    .ToDictionary(dict => dict.Key, dict => dict.Value));
                logger.LogError(ex, MessageTemplate, httpContext.Request.Method, httpContext.Request.Path, StatusCodes.Status500InternalServerError);

                throw;
            }
        }
    }

    private static void TryAddLogTagFromHeader(IDictionary<string, string> logTags, IHeaderDictionary headers, string logTagToAdd)
    {
        var logTagExists = headers.TryGetValue(logTagToAdd, out var headerValue) && !string.IsNullOrEmpty(headerValue);
        if (logTagExists)
        {
            logTags[logTagToAdd] = headerValue!;
        }
    }

    private static string GetClientIdFromJWTToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var clientId = jwtToken.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value;

                if (!string.IsNullOrEmpty(clientId))
                {
                    // Store client ID in HttpContext for downstream use
                    return clientId;
                }
            }
            catch
            {
                // Token parsing failed return empty string
                return "";
            }
        }

        return "";
    }
}
