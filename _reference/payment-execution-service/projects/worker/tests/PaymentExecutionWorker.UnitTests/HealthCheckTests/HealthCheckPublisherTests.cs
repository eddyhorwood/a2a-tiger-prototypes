using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentExecutionWorker.Worker.HealthCheck;

namespace PaymentExecutionWorker.UnitTests.HealthCheckTests;

public class HealthCheckPublisherTests
{
    private static readonly string _healthFileFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private static readonly string _healthFilePath = Path.Join(_healthFileFolder, "/healthy.txt");
    private static readonly Mock<IReadOnlyDictionary<string, HealthReportEntry>> _entry = new();

    [Fact]
    public async Task GivenLivenessStatusIsHealthyAndHealthFileDoesNotExist_WhenPublishAsyncIsCalled_ThenFileIsCreated()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_healthFileFolder);
        var sut = new HealthCheckPublisher(mockFileSystem);
        SetupHealthReportEntries(HealthStatus.Healthy, HealthStatus.Unhealthy);
        var report = new HealthReport(_entry.Object, HealthStatus.Healthy, new TimeSpan());

        // Act
        await sut.PublishAsync(report, new CancellationToken());

        // Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeTrue();
        mockFileSystem.GetFile(_healthFilePath).TextContents.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GivenDbreadyStatusIsHealthy_WhenPublishAsyncIsCalled_ThenFileContainsDbReady()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_healthFileFolder);
        var sut = new HealthCheckPublisher(mockFileSystem);
        SetupHealthReportEntries(HealthStatus.Healthy, HealthStatus.Healthy);
        var report = new HealthReport(_entry.Object, HealthStatus.Healthy, new TimeSpan());

        // Act
        await sut.PublishAsync(report, new CancellationToken());

        // Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeTrue();
        mockFileSystem.GetFile(_healthFilePath).TextContents.Should().BeEquivalentTo("dbready");
    }

    [Fact]
    public async Task GivenLivessStatusIsHealthyAndDbreadyStatusIsUnhealthyAndOldHealthFileExists_WhenPublishAsyncIsCalled_ThenHealthFileIsRecreated()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [_healthFilePath] = new("OLD FILE")
        });

        SetupHealthReportEntries(HealthStatus.Healthy, HealthStatus.Unhealthy);
        var sut = new HealthCheckPublisher(mockFileSystem);
        var report = new HealthReport(_entry.Object, HealthStatus.Healthy, new TimeSpan());

        //Act
        await sut.PublishAsync(report, new CancellationToken());

        //Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeTrue();
        mockFileSystem.GetFile(_healthFilePath).TextContents.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GivenDbreadyStatusIsHealthyAndFileExists_WhenPublishAsyncIsCalled_ThenHealthFileIsOverwritten()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [_healthFilePath] = new("OLD FILE")
        });

        SetupHealthReportEntries(HealthStatus.Healthy, HealthStatus.Healthy);
        var sut = new HealthCheckPublisher(mockFileSystem);
        var report = new HealthReport(_entry.Object, HealthStatus.Healthy, new TimeSpan());

        //Act
        await sut.PublishAsync(report, new CancellationToken());

        //Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeTrue();
        mockFileSystem.GetFile(_healthFilePath).TextContents.Should().BeEquivalentTo("dbready");
    }

    [Fact]
    public async Task GivenLivenessStatusIsUnHealthyAndHealthFileExists_WhenPublishAsyncIsCalled_ThenHealthFileIsDeleted()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [_healthFilePath] = new("OLD FILE")
        });

        SetupHealthReportEntries(HealthStatus.Unhealthy, HealthStatus.Healthy);
        var sut = new HealthCheckPublisher(mockFileSystem);
        var report = new HealthReport(_entry.Object, HealthStatus.Unhealthy, new TimeSpan());

        //Act
        await sut.PublishAsync(report, new CancellationToken());

        //Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task GivenDbreadyStatusIsUnHealthyAndLivenessIsHealthy_WhenPublishAsyncIsCalled_ThenHealthFileIsEmpty()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [_healthFilePath] = new("dbready")
        });

        SetupHealthReportEntries(HealthStatus.Healthy, HealthStatus.Unhealthy);
        var sut = new HealthCheckPublisher(mockFileSystem);
        var report = new HealthReport(_entry.Object, HealthStatus.Healthy, new TimeSpan());

        //Act
        await sut.PublishAsync(report, new CancellationToken());

        //Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeTrue();
        mockFileSystem.GetFile(_healthFilePath).Contents.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenAllHealthChecksAreHealthy_WhenPublishAsyncIsCalled_ThenFileContainsDbReady()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_healthFileFolder);
        var sut = new HealthCheckPublisher(mockFileSystem);
        SetupHealthReportEntries(HealthStatus.Healthy, HealthStatus.Healthy);
        var report = new HealthReport(_entry.Object, HealthStatus.Healthy, new TimeSpan());

        // Act
        await sut.PublishAsync(report, new CancellationToken());

        // Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeTrue();
        mockFileSystem.GetFile(_healthFilePath).TextContents.Should().BeEquivalentTo("dbready");
    }

    [Fact]
    public async Task GivenLivenessStatusIsUnhealthyAndDbreadyStatusIsHealthy_WhenPublishAsyncIsCalled_ThenHealthFileIsDeleted()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [_healthFilePath] = new("OLD FILE")
        });
        mockFileSystem.AddDirectory(_healthFileFolder);
        var sut = new HealthCheckPublisher(mockFileSystem);
        SetupHealthReportEntries(HealthStatus.Unhealthy, HealthStatus.Healthy);
        var report = new HealthReport(_entry.Object, HealthStatus.Unhealthy, new TimeSpan());

        // Act
        await sut.PublishAsync(report, new CancellationToken());

        // Assert
        mockFileSystem.FileExists(_healthFilePath).Should().BeFalse();
    }

    private static void SetupHealthReportEntries(HealthStatus liveness, HealthStatus dbReadiness)
    {
        var liveEntry = new HealthReportEntry(liveness, "live", new TimeSpan(), null, null, new[] { "live" });
        var dbReadyEntry = new HealthReportEntry(dbReadiness, "ready", new TimeSpan(), null, null, new[] { "dbready" });
        var entries = new Dictionary<string, HealthReportEntry>
        {
            { "live", liveEntry },
            { "dbready", dbReadyEntry }
        };
        _entry.SetupAllProperties();
        _entry.Setup(x => x.GetEnumerator()).Returns(entries.GetEnumerator());
        _entry.Setup(x => x.Values).Returns(entries.Values);
    }
}
