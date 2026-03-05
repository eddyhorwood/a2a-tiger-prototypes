using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PaymentExecutionService.ComponentTests;

public static class ProblemDetailsExtensions
{
    public static string ExtractErrorDetailsJsonString(this ProblemDetails problemDetails)
    {
        if (problemDetails.Extensions.TryGetValue("errors", out var errorDetailsObj) &&
            errorDetailsObj is JsonElement errorsElement)
        {
            return errorsElement.GetRawText();
        }

        throw new Exception("No 'errors' extension found in ProblemDetails object");
    }
}
