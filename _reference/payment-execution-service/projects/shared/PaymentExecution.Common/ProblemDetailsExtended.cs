using System.Text.Json;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace PaymentExecution.Common;

public class ProblemDetailsExtended : ProblemDetails
{
    public string? ErrorCode { get; set; }
    public string? ProviderErrorCode { get; set; }

    public static Result<ProblemDetailsExtended> TryDeserializeResponseContent(string responseStringContent, JsonSerializerOptions deserializerOptions)
    {
        try
        {
            var deserializedResponse = JsonSerializer.Deserialize<ProblemDetailsExtended>(responseStringContent, deserializerOptions);
            if (deserializedResponse == null)
            {
                return Result.Fail("Invalid JSON format: Deserialization returned null.");
            }
            return Result.Ok(deserializedResponse);
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Invalid JSON format: {ex.Message}");
        }
    }
}
