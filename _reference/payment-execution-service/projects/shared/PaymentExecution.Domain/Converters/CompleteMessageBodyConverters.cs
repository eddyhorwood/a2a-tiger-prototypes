using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentExecution.Domain.Models;

namespace PaymentExecution.Domain.Converters;

public class NonEmptyGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("Guid cannot be null in this context");
        }

        Guid guid = reader.GetGuid();
        if (guid == Guid.Empty)
        {
            throw new JsonException("Guid cannot be an empty guid");
        }
        return guid;
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class ValidCompleteFlowStatusConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("Status cannot be null in this context");
        }

        var status = reader.GetString();
        if (!Enum.TryParse<StripeValidCompleteStatus>(status, out _))
        {
            throw new JsonException(string.Format("Status must be either 'Succeeded', 'Failed' or 'Cancelled'. But found {0}", status));
        }

        return status;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
