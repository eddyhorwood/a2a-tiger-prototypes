namespace PaymentExecution.Domain;

/// <summary>
/// Marker interface to indicate that a command should skip logging in the MediatR pipeline.
/// Implement this interface on any command that should not be logged by LoggingBehavior.
/// </summary>
public interface ISkipLoggingBehavior;

