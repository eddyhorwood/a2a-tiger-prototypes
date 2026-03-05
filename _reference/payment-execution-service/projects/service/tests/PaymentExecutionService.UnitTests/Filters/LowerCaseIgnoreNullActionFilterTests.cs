using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using PaymentExecutionService.Filters;
using Xunit;

namespace PaymentExecutionService.UnitTests.Filters;

public class LowerCaseIgnoreNullActionFilterTests
{
    private readonly LowerCaseIgnoreNullActionFilter _sut = new();

    [Fact]
    public void GivenResultIsNotObjectResult_WhenOnActionExecuted_ThenResultRemainsUnchanged()
    {
        // Arrange
        var context = CreateActionExecutedContext();
        var expectedResult = new StatusCodeResult(StatusCodes.Status200OK);
        context.Result = expectedResult;

        // Act
        _sut.OnActionExecuted(context);

        // Assert
        context.Result.Should().BeSameAs(expectedResult);
    }

    [Fact]
    public void GivenObjectResultValueHasNullProperty_WhenOnActionExecuted_ThenNullPropertyExcluded()
    {
        // Arrange
        var context = CreateActionExecutedContext();
        var testObject = new TestClass()
        {
            FirstProperty = "hello",
            NullableSecondProperty = null,
        };
        context.Result = new ObjectResult(testObject);
        var expectedString = JsonSerializer.Serialize(new { firstProperty = "hello" });

        // Act
        _sut.OnActionExecuted(context);

        // Assert
        var jsonResult = context.Result.Should().BeOfType<JsonResult>().Subject;
        var preparedOptions = jsonResult.SerializerSettings.Should().BeOfType<JsonSerializerOptions>().Subject;
        var resultantJsonString = JsonSerializer.Serialize(jsonResult.Value, preparedOptions);

        resultantJsonString.Should().BeEquivalentTo(expectedString);
    }

    [Fact]
    public void GivenObjectResultValueWithEnum_WhenOnActionExecuted_ThenEnumSerializedInLowerCase()
    {
        // Arrange
        var context = CreateActionExecutedContext();
        var testObject = new TestClassWithEnum()
        {
            FirstProperty = "hello",
            EnumProperty = TestValue.FirstValue,
        };
        context.Result = new ObjectResult(testObject);
        var expectedString = JsonSerializer.Serialize(new { firstProperty = "hello", enumProperty = "firstvalue" });

        // Act
        _sut.OnActionExecuted(context);

        // Assert
        var jsonResult = context.Result.Should().BeOfType<JsonResult>().Subject;
        var preparedOptions = jsonResult.SerializerSettings.Should().BeOfType<JsonSerializerOptions>().Subject;
        var resultantJsonString = JsonSerializer.Serialize(jsonResult.Value, preparedOptions);

        resultantJsonString.Should().BeEquivalentTo(expectedString);
    }

    private static ActionExecutedContext CreateActionExecutedContext()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor()
        );

        return new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object()
        );
    }

    private enum TestValue
    {
        FirstValue,
    }

    private class TestClass
    {
        public required string FirstProperty { get; set; }
        public string? NullableSecondProperty { get; set; }
    }

    private class TestClassWithEnum
    {
        public required string FirstProperty { get; set; }
        public TestValue EnumProperty { get; set; }
    }
}
