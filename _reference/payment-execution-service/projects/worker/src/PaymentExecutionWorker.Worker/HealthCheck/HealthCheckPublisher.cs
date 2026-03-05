using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PaymentExecutionWorker.Worker.HealthCheck;

public class HealthCheckPublisher(IFileSystem fileSystem) : IHealthCheckPublisher
{
    // If you change this path, ensure you update the `liveness` block in `shared.yaml` 
    private readonly static string _fileFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private readonly static string _liveFileName = Path.Join(_fileFolder, "/healthy.txt");

    /// <summary>
    /// Creates / touches a file on the file system to indicate "healthy" (liveness) state of the pod
    /// Deletes the files to indicate "unhealthy"
    /// The file will then be checked by k8s livenessProbe
    /// </summary>
    /// <param name="report"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        // 1. Handle Liveness (Checks tagged with "live")
        // We consider it healthy if ALL "live" checks are Healthy
        var livenessStatus = GetStatusForTag(report, "live");
        if (livenessStatus != HealthStatus.Healthy)
        {
            DeleteFile(_liveFileName);
            return Task.CompletedTask;
        }

        CreateFile(_liveFileName);

        // 2. Check db health status (Checks tagged with "dbready")
        var dbHealthStatus = GetStatusForTag(report, "dbready");
        if (dbHealthStatus == HealthStatus.Healthy)
            CreateFile(_liveFileName, "dbready");

        return Task.CompletedTask;
    }

    private void CreateFile(string fileName, string fileContent = "")
    {
        using var _ = fileSystem.File.Create(fileName);
        if (!string.IsNullOrEmpty(fileContent))
            _.Write(System.Text.Encoding.UTF8.GetBytes(fileContent));
    }

    private void DeleteFile(string fileName)
    {
        if (fileSystem.File.Exists(fileName))
        {
            fileSystem.File.Delete(fileName);
        }
    }

    private static HealthStatus GetStatusForTag(HealthReport report, string tag)
    {
        var entrieValues = report.Entries.Values
            .Where(e => e.Tags.Contains(tag));

        if (entrieValues.All(e => e.Status == HealthStatus.Healthy))
            return HealthStatus.Healthy;

        return HealthStatus.Unhealthy;
    }
}
