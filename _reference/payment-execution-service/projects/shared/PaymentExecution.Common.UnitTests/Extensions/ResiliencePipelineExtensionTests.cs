using System.Net;
using FluentAssertions;
using PaymentExecution.Common.Extensions;
using Polly;

namespace PaymentExecution.Common.UnitTests.Extensions;

public class ResiliencePipelineExtensionTests
{
    public static TheoryData<HttpStatusCode, bool> RetryStatusCodeData =>
        new()
        {
            { HttpStatusCode.InternalServerError, true },
            { HttpStatusCode.ServiceUnavailable, true },
            { HttpStatusCode.TooManyRequests, true }, // Non 500 code, but is still a transient error
            { HttpStatusCode.BadRequest, false }, // Failure code, but is not a transient error
            { HttpStatusCode.OK, false }
        };

    [Fact]
    public void GivenHttpRequestExceptionWithNoStatusCode_TransientErrorsPredicateExecutes_ThenResultShouldBeFalse()
    {
        // Arrange
        var predicate = new PredicateBuilder().HandleTransientErrors().Build();

        // Act
        var exception = new HttpRequestException("Test exception", null, null);
        var result = predicate(Outcome.FromException<object>(exception));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GivenNonHttpResponseMessage_TransientErrorsPredicateExecutes_ThenResultShouldBeFalse()
    {
        // Arrange
        var predicate = new PredicateBuilder().HandleTransientErrors().Build();

        // Act
        var responseMessage = new object();
        var result = predicate(Outcome.FromResult(responseMessage));

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(RetryStatusCodeData))]
    public void GivenHttpRequestException_TransientErrorsPredicateExecutes_ThenResultShouldBeAsExpected(
        HttpStatusCode httpStatusCode, bool expectedResult)
    {
        // Arrange
        var predicate = new PredicateBuilder().HandleTransientErrors().Build();

        // Act
        var exception = new HttpRequestException("Test exception", null, httpStatusCode);
        var result = predicate(Outcome.FromException<object>(exception));

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(RetryStatusCodeData))]
    public void GivenHttpResponseMessage_TransientErrorsPredicateExecutes_ThenResultShouldBeAsExpected(
        HttpStatusCode httpStatusCode, bool expectedResult)
    {
        // Arrange
        var predicate = new PredicateBuilder().HandleTransientErrors().Build();

        // Act
        var responseMessage = new HttpResponseMessage(httpStatusCode);
        var result = predicate(Outcome.FromResult<object>(responseMessage));

        // Assert
        result.Should().Be(expectedResult);
    }
}
