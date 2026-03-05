namespace PaymentExecution.PaymentRequestClient.Models;

public record SourceContext
{
    public required Guid Identifier { get; set; }
    public Guid? RepeatingTemplateId { get; set; }
    public required string Type { get; set; }
}
