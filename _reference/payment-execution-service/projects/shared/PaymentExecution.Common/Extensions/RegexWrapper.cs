using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
namespace PaymentExecution.Common.Extensions;

public interface IRegexWrapper
{
    string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout);
}

public class RegexWrapper : IRegexWrapper
{
    public string Replace(string input, string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout)
    {
        return Regex.Replace(input, pattern, replacement, options, matchTimeout);
    }
}
