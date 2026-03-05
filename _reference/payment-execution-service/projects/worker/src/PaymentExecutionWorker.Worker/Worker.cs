using MediatR;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;

namespace PaymentExecutionWorker.Worker;

public class Worker(ILogger<Worker> logger, TimeProvider timeProvider, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (IServiceScope scope = serviceScopeFactory.CreateScope())
                {
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new ProcessCompleteMessagesCommand(), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // This is expected during graceful shutdown, don't log as error
                logger.LogInformation(ExecutionConstants.InfoMessages.WorkerOperationCancelledDueToShutdown);
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{ExecutionConstants.ErrorMessages.UnexpectedMessageProcessingError}: " + "{ExceptionMessage}", ex.Message);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(20), timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // This is expected during graceful shutdown, don't log as error
                logger.LogInformation(ExecutionConstants.InfoMessages.WorkerDelayCancelledDueToShutdown);
                break;
            }
        }
    }
}
