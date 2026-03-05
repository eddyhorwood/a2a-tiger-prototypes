using System.Text.RegularExpressions;
namespace PaymentExecution.Common.Extensions;

public static class RedactionHelper
{
    public const string EmailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+(?:\.[a-zA-Z]{2,})?";

    public static IRegexWrapper RegexWrapper { get; set; } = new RegexWrapper();

    public static string SanitizeMessage(string message)
    {
        try
        {
            return string.IsNullOrWhiteSpace(message)
                ? message
                :
                // Replace potentially sensitive data with masked values
                RegexWrapper.Replace(message, EmailPattern, "***MASKED***", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            return message + " Error sanitizing message due to timeout.";
        }

    }
}

