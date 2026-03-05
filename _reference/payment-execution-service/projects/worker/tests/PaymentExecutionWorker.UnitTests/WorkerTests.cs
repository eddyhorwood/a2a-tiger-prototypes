using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using PaymentExecution.Common;
using PaymentExecution.Domain.Commands;
using static PaymentExecution.TestUtilities.LoggerAssertions;

namespace PaymentExecutionWorker.UnitTests;

public class WorkerTests
{
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly Mock<ILogger<Worker.Worker>> _mockLogger = new();
    private readonly FakeTimeProvider _fakeTimeProvider = new();
    private Worker.Worker _sut;

    [Fact]
    public async Task GivenMediatorReturnsResponse_WhenHostedServiceIsInvoked_ThenMediatorSendMethodIsCalled()
    {
        ConfigureHostedServiceAndServiceProvider();
        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessCompleteMessagesCommandResponse());

        await _sut.StartAsync(new CancellationToken());

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenProcessingCommandSuccess_WhenHostedServiceIsInvoked_ThenWaitsForDelayBeforeNextIteration()
    {
        ConfigureHostedServiceAndServiceProvider();
        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessCompleteMessagesCommandResponse());

        var cancellationTokenSource = new CancellationTokenSource();

        await _sut.StartAsync(cancellationTokenSource.Token);

        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(19));
        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(2));
        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task GivenMediatorThrowsException_WhenHostedServiceIsInvoked_ThenErrorIsLoggedAndContinuesNextIterationAfterDelay()
    {
        ConfigureHostedServiceAndServiceProvider();
        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("oopsie"));

        await _sut.StartAsync(new CancellationToken());

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _fakeTimeProvider.Advance(TimeSpan.FromSeconds(121));
        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyLogMessagesWithPrefixAtLevel<Worker.Worker>(_mockLogger, LogLevel.Error, ExecutionConstants.ErrorMessages.UnexpectedMessageProcessingError, 2);
    }

    [Fact]
    public async Task GivenCancellationRequested_WhenHostedServiceIsInvoked_ThenMediatorSendMethodIsNotCalled()
    {
        ConfigureHostedServiceAndServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await _sut.StartAsync(cancellationTokenSource.Token);

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task GivenMediatorThrowsOperationCanceledException_WhenCancellationTokenIsRequested_ThenShutdownIsLoggedAndWorkerExits()
    {
        ConfigureHostedServiceAndServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();

        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ProcessCompleteMessagesCommandResponse>, CancellationToken>((_, token) =>
            {
                // Cancel the token during processing to simulate shutdown
                cancellationTokenSource.Cancel();
            })
            .Throws(new OperationCanceledException("Operation was canceled", cancellationTokenSource.Token));

        await _sut.StartAsync(cancellationTokenSource.Token);

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogMessagesWithPrefixAtLevel<Worker.Worker>(_mockLogger, LogLevel.Information, ExecutionConstants.InfoMessages.WorkerOperationCancelledDueToShutdown, 1);
        cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task GivenMediatorThrowsTaskCanceledException_WhenCancellationTokenIsRequested_ThenShutdownIsLoggedAndWorkerExits()
    {
        ConfigureHostedServiceAndServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();

        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ProcessCompleteMessagesCommandResponse>, CancellationToken>((_, token) =>
            {
                // Cancel the token during processing to simulate shutdown
                cancellationTokenSource.Cancel();
            })
            .Throws(new TaskCanceledException("A task was canceled"));

        await _sut.StartAsync(cancellationTokenSource.Token);

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogMessagesWithPrefixAtLevel<Worker.Worker>(_mockLogger, LogLevel.Information, ExecutionConstants.InfoMessages.WorkerOperationCancelledDueToShutdown, 1);
        cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task GivenMediatorThrowsOperationCanceledException_WhenCancellationTokenIsNotRequested_ThenExceptionIsLoggedAsError()
    {
        ConfigureHostedServiceAndServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();

        // Throw OperationCanceledException but don't cancel the main token (simulates internal cancellation)
        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException("Operation was canceled"));

        await _sut.StartAsync(cancellationTokenSource.Token);

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogMessagesWithPrefixAtLevel<Worker.Worker>(_mockLogger, LogLevel.Error, ExecutionConstants.ErrorMessages.UnexpectedMessageProcessingError, 1);
        cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task GivenTaskDelayThrowsOperationCanceledException_WhenCancellationTokenIsRequested_ThenDelayShutdownIsLoggedAndWorkerExits()
    {
        ConfigureHostedServiceAndServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();

        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessCompleteMessagesCommandResponse());

        await _sut.StartAsync(cancellationTokenSource.Token);

        // Advance time to trigger the delay, then cancel
        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await cancellationTokenSource.CancelAsync();
        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(15));

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogMessagesWithPrefixAtLevel<Worker.Worker>(_mockLogger, LogLevel.Information, "Worker delay was cancelled due to shutdown request", 1);
        cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task GivenSuccessfulProcessing_WhenCancellationTokenIsRequestedDuringDelay_ThenWorkerExitsGracefullyWithoutSecondIteration()
    {
        ConfigureHostedServiceAndServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();

        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessCompleteMessagesCommandResponse());

        await _sut.StartAsync(cancellationTokenSource.Token);

        // First iteration completes successfully
        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);

        // Cancel during delay period
        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await cancellationTokenSource.CancelAsync();
        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(20));

        // Should not have second iteration
        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogMessagesWithPrefixAtLevel<Worker.Worker>(_mockLogger, LogLevel.Information, ExecutionConstants.InfoMessages.WorkerDelayCancelledDueToShutdown, 1);
        cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task GivenMultipleIterations_WhenShutdownRequestedDuringProcessing_ThenCurrentIterationCompletesBeforeGracefulShutdown()
    {
        ConfigureHostedServiceAndServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();
        var processingTasks = new Queue<TaskCompletionSource<ProcessCompleteMessagesCommandResponse>>();

        _mockMediator.Setup(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                var tcs = new TaskCompletionSource<ProcessCompleteMessagesCommandResponse>();
                processingTasks.Enqueue(tcs);
                return tcs.Task;
            });

        await _sut.StartAsync(cancellationTokenSource.Token);

        // Complete first iteration
        processingTasks.Dequeue().SetResult(new ProcessCompleteMessagesCommandResponse());
        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(20));

        // Start second iteration but cancel during processing
        await cancellationTokenSource.CancelAsync();
        processingTasks.Dequeue().SetCanceled(cancellationTokenSource.Token);

        _mockMediator.Verify(m => m.Send(It.IsAny<ProcessCompleteMessagesCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyLogMessagesWithPrefixAtLevel<Worker.Worker>(_mockLogger, LogLevel.Information, ExecutionConstants.InfoMessages.WorkerOperationCancelledDueToShutdown, 1);
        cancellationTokenSource.Dispose();
    }

    private void ConfigureHostedServiceAndServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<IMediator>(sp => _mockMediator.Object);
        serviceCollection.AddScoped(_ => _mockLogger.Object);
        serviceCollection.AddSingleton<TimeProvider>(_ => _fakeTimeProvider);
        serviceCollection.AddHostedService<Worker.Worker>();
        var provider = serviceCollection.BuildServiceProvider();
        _sut = provider.GetService<IHostedService>() as Worker.Worker;
    }
}
