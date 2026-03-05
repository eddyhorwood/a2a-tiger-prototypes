using System.Text.Json;
using FluentAssertions;
using PaymentExecution.Domain.Converters;

namespace PaymentExecution.Domain.UnitTests.Converters;

public class CompleteMessageBodyConvertersTests
{
    [Fact]
    public void GivenGuidIsNull_WhenRead_ShouldThrowException()
    {
        // Arrange
        var converter = new NonEmptyGuidConverter();
        var json = "null";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        try
        {
            converter.Read(ref reader, typeof(Guid), new JsonSerializerOptions());
        }
        catch (JsonException ex)
        {
            ex.Message.Should().Be("Guid cannot be null in this context");
            return;
        }
        // Act & Assert
        Assert.Fail("Expected exception was not thrown");
    }

    [Fact]
    public void GivenGuidIsEmpty_WhenRead_ShouldThrowException()
    {
        // Arrange
        var converter = new NonEmptyGuidConverter();
        var json = "\"00000000-0000-0000-0000-000000000000\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        // Act & Assert
        try
        {
            converter.Read(ref reader, typeof(Guid), new JsonSerializerOptions());
        }
        catch (JsonException ex)
        {
            ex.Message.Should().Be("Guid cannot be an empty guid");
            return;
        }
        // Act & Assert
        Assert.Fail("Expected exception was not thrown");
    }

    [Fact]
    public void GivenGuidIsValid_WhenRead_ShouldReturnGuid()
    {
        // Arrange
        var converter = new NonEmptyGuidConverter();
        var validGuid = Guid.NewGuid();
        var json = $"\"{validGuid}\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        // Act
        var result = converter.Read(ref reader, typeof(Guid), new JsonSerializerOptions());

        // Assert
        Assert.Equal(validGuid, result);
    }

    [Fact]
    public void GivenValidGuid_WhenWrite_ShouldWriteGuid()
    {
        // Arrange
        var converter = new NonEmptyGuidConverter();
        var validGuid = Guid.NewGuid();
        var options = new JsonSerializerOptions();
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        converter.Write(writer, validGuid, options);
        writer.Flush();
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        Assert.Equal($"\"{validGuid}\"", json);
    }

    [Fact]
    public void GivenStatusIsNull_WhenValidCompleteFlowStatusConverterRead_ShouldThrowException()
    {
        // Arrange
        var converter = new ValidCompleteFlowStatusConverter();
        var json = "null";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        // Act & Assert
        try
        {
            converter.Read(ref reader, typeof(Guid), new JsonSerializerOptions());
        }
        catch (JsonException ex)
        {
            ex.Message.Should().Be("Status cannot be null in this context");
            return;
        }
        // Act & Assert
        Assert.Fail("Expected exception was not thrown");
    }

    [Fact]
    public void GivenStatusIsInvalid_WhenValidCompleteFlowStatusConverterRead_ShouldThrowException()
    {
        // Arrange
        var converter = new ValidCompleteFlowStatusConverter();
        var json = "\"InvalidStatus\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        // Act & Assert
        try
        {
            converter.Read(ref reader, typeof(Guid), new JsonSerializerOptions());
        }
        catch (JsonException ex)
        {
            ex.Message.Should().Be("Status must be either 'Succeeded', 'Failed' or 'Cancelled'. But found InvalidStatus");
            return;
        }
        // Act & Assert
        Assert.Fail("Expected exception was not thrown");
    }

    [Fact]
    public void GivenStatusIsValid_WhenValidCompleteFlowStatusConverterRead_ShouldReturnStatus()
    {
        // Arrange
        var converter = new ValidCompleteFlowStatusConverter();
        var validStatus = "Succeeded";
        var json = $"\"{validStatus}\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();
        // Act
        var result = converter.Read(ref reader, typeof(string), new JsonSerializerOptions());

        // Assert
        Assert.Equal(validStatus, result);
    }

    [Fact]
    public void GivenStatusIsValid_WhenValidCompleteFlowStatusConverterWrite_ShouldWriteStatus()
    {
        // Arrange
        var converter = new ValidCompleteFlowStatusConverter();
        var validStatus = "Succeeded";
        var options = new JsonSerializerOptions();
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        converter.Write(writer, validStatus, options);
        writer.Flush();
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        Assert.Equal($"\"{validStatus}\"", json);
    }
}
