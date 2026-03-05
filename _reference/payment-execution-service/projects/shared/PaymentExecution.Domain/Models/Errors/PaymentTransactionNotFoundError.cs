using FluentResults;

namespace PaymentExecution.Domain.Models.Errors;

public class PaymentTransactionNotFoundError() : Error(ErrorMessage.PaymentTransactionRecordNotFound);
