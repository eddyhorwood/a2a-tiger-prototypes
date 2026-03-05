namespace PaymentExecution.Repository;

public class PaymentExecutionDbConnectionOptions
{
    public static readonly string Key = "DataAccess:PaymentExecutionDB";
    public required string ConnectionString { get; set; }
    public int MinPoolSize { get; set; }
}
