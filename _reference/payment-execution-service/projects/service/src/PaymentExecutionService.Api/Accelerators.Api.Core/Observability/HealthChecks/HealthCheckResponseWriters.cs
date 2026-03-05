// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace Xero.Accelerators.Api.Core.Observability.HealthChecks;

public static class HealthCheckResponseWriters
{
    public static Task WriteEmptyHealthReportAsync(HttpContext context, HealthReport _)
    {
        return context.Response.WriteAsync(string.Empty);
    }

    public static Task WriteHealthReportAsync(HttpContext context, HealthReport report)
    {
        var response = new HealthCheckResponse
        {
            Type = context.Request.GetDisplayUrl(),
            Status = report.Status.ToString(),
            TotalCheckExecutionTimeInSeconds = report.TotalDuration
        };

        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            foreach (var (checkName, result) in report.Entries)
            {
                response.Dependencies.Add(new()
                {
                    Name = checkName,
                    Data = result.Data,
                    Description = result.Description,
                    Duration = result.Duration,
                    Exception = result.Exception != null ? new HealthCheckResponse.SerializableException(result.Exception) : null,
                    Status = result.Status.ToString()
                });
            }
        }

        return report.Status == HealthStatus.Unhealthy
            ? context.WriteProblemJsonAsync(response)
            : context.WriteJsonAsync(response);
    }

    private static Task WriteProblemJsonAsync(this HttpContext context, object value)
    {
        return context.WriteJsonAsync(value, "application/problem+json");
    }
    private static Task WriteJsonAsync(
        this HttpContext context, object value, string contentType = "application/json")
    {
        context.Response.ContentType = contentType;
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return context.Response.WriteAsync(json);
    }
}

public class HealthCheckResponse
{
    public class SerializableException(Exception exception)
    {
        public string Message { get; } = exception.Message;
        public string? StackTrace { get; } = exception.StackTrace;
    }

    public class Dependency
    {
        public string? Name { get; set; }
        public IReadOnlyDictionary<string, object>? Data { get; set; }
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public SerializableException? Exception { get; set; }
        public string? Status { get; set; }
    }

    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? Detail { get; set; }
    public TimeSpan TotalCheckExecutionTimeInSeconds { get; set; }
    public List<Dependency> Dependencies { get; set; } = new();
}
