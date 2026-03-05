using Microsoft.Extensions.Logging;
using Moq;

namespace PaymentExecution.TestUtilities;

public static class LoggerAssertions
{
    public static void VerifyLogMessagesWithPrefixAtLevel<T>(Mock<ILogger<T>> logger, LogLevel level, string messagePrefix, int times)
    {
        logger.Verify(x => x.Log(
            It.Is<LogLevel>(logLevel => logLevel == level),
            It.Is<EventId>(eventId => eventId.Id == 0),
            It.Is<It.IsAnyType>((@object, @type) =>
                @object.ToString()!.StartsWith(messagePrefix) && @type.Name == "FormattedLogValues"),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!), Times.Exactly(times));
    }
}
