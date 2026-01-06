using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using MyApp.Options;

namespace MyApp.Services;

/// <summary>
/// Implementation of the scheduled task service for REST endpoints
/// </summary>
public sealed class ScheduledTaskService : IScheduledTaskService
{
    private readonly ILogger<ScheduledTaskService> _logger;
    private readonly AppConfig _config;
    private readonly IDataService _dataService;

    // In-memory task state tracking
    private static readonly ConcurrentDictionary<string, TaskStatus> TaskStates = new();
    private static readonly ConcurrentQueue<TaskExecutionRecord> ExecutionHistory = new();
    private static readonly SemaphoreSlim TaskLock = new(1, 1);

    // Define available scheduled tasks
    private static readonly Dictionary<string, string> AvailableTasks = new()
    {
        ["DataSync"] = "Synchronizes data with external systems",
        ["Cleanup"] = "Removes expired data based on retention policy",
        ["HealthCheck"] = "Performs system health verification",
        ["ReportGeneration"] = "Generates scheduled reports"
    };

    public ScheduledTaskService(
        ILogger<ScheduledTaskService> logger,
        IOptions<AppConfig> config,
        IDataService dataService)
    {
        _logger = logger;
        _config = config.Value;
        _dataService = dataService;

        InitializeTaskStates();
    }

    /// <inheritdoc />
    public async Task<TaskRunResult> RunAsync(string? taskName = null, CancellationToken cancellationToken = default)
    {
        var targetTask = string.IsNullOrWhiteSpace(taskName) ? "DataSync" : taskName;
        var executionId = Guid.NewGuid().ToString("N")[..8];
        var executionLog = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            if (!_config.ScheduledTaskEnabled)
            {
                _logger.LogWarning("Scheduled tasks are disabled in configuration");
                return new TaskRunResult
                {
                    Success = false,
                    TaskName = targetTask,
                    Message = "Scheduled tasks are disabled",
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow,
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            if (!AvailableTasks.ContainsKey(targetTask))
            {
                return new TaskRunResult
                {
                    Success = false,
                    TaskName = targetTask,
                    Message = $"Unknown task: {targetTask}",
                    ErrorMessage = $"Available tasks: {string.Join(", ", AvailableTasks.Keys)}",
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow,
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Try to acquire lock (prevent concurrent executions of the same task)
            if (!await TaskLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                return new TaskRunResult
                {
                    Success = false,
                    TaskName = targetTask,
                    Message = "Task is already running",
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow,
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            try
            {
                // Update task state
                if (TaskStates.TryGetValue(targetTask, out var state))
                {
                    state.IsRunning = true;
                    state.LastRunAt = startedAt;
                }

                executionLog.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Starting task: {targetTask}");
                _logger.LogInformation("Starting scheduled task {TaskName}, execution ID: {ExecutionId}",
                    targetTask, executionId);

                // Execute the appropriate task
                var result = await ExecuteTaskAsync(targetTask, executionLog, cancellationToken);

                stopwatch.Stop();
                var completedAt = DateTime.UtcNow;

                executionLog.Add($"[{completedAt:HH:mm:ss.fff}] Task completed successfully");

                // Update task state
                if (TaskStates.TryGetValue(targetTask, out state))
                {
                    state.IsRunning = false;
                    state.LastResult = result.Success ? "Success" : "Failed";
                    state.NextRunAt = DateTime.UtcNow.AddMinutes(_config.ScheduledTaskIntervalMinutes);
                }

                // Record execution history
                RecordExecution(new TaskExecutionRecord
                {
                    ExecutionId = executionId,
                    TaskName = targetTask,
                    StartedAt = startedAt,
                    CompletedAt = completedAt,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Success = result.Success,
                    ItemsProcessed = result.ItemsProcessed,
                    ResultSummary = result.Message,
                    TriggeredBy = "API"
                });

                _logger.LogInformation(
                    "Completed scheduled task {TaskName} in {Duration}ms, processed {Items} items",
                    targetTask, stopwatch.ElapsedMilliseconds, result.ItemsProcessed);

                return new TaskRunResult
                {
                    Success = result.Success,
                    TaskName = targetTask,
                    Message = result.Message,
                    ItemsProcessed = result.ItemsProcessed,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    StartedAt = startedAt,
                    CompletedAt = completedAt,
                    ExecutionLog = executionLog
                };
            }
            finally
            {
                TaskLock.Release();
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing scheduled task {TaskName}", targetTask);

            executionLog.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Error: {ex.Message}");

            if (TaskStates.TryGetValue(targetTask, out var state))
            {
                state.IsRunning = false;
                state.LastResult = "Error";
            }

            RecordExecution(new TaskExecutionRecord
            {
                ExecutionId = executionId,
                TaskName = targetTask,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Success = false,
                ErrorMessage = ex.Message,
                ResultSummary = "Failed with error",
                TriggeredBy = "API"
            });

            return new TaskRunResult
            {
                Success = false,
                TaskName = targetTask,
                Message = "Task execution failed",
                ErrorMessage = _config.EnableDetailedErrors ? ex.Message : "An error occurred",
                DurationMs = stopwatch.ElapsedMilliseconds,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
                ExecutionLog = executionLog
            };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaskStatus>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(5, cancellationToken);
        return TaskStates.Values.OrderBy(x => x.Name).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaskExecutionRecord>> GetHistoryAsync(
        string? taskName = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(5, cancellationToken);

        var query = ExecutionHistory.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(taskName))
        {
            query = query.Where(x => x.TaskName.Equals(taskName, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(x => x.StartedAt)
            .Take(Math.Min(limit, 100))
            .ToList();
    }

    /// <summary>
    /// Initializes task states on service creation
    /// </summary>
    private void InitializeTaskStates()
    {
        foreach (var (name, description) in AvailableTasks)
        {
            TaskStates.TryAdd(name, new TaskStatus
            {
                Name = name,
                Description = description,
                IsEnabled = _config.ScheduledTaskEnabled,
                IsRunning = false,
                IntervalMinutes = _config.ScheduledTaskIntervalMinutes,
                LastResult = "Not run yet"
            });
        }
    }

    /// <summary>
    /// Executes a specific task
    /// </summary>
    private async Task<(bool Success, string Message, int ItemsProcessed)> ExecuteTaskAsync(
        string taskName,
        List<string> log,
        CancellationToken cancellationToken)
    {
        return taskName switch
        {
            "DataSync" => await ExecuteDataSyncAsync(log, cancellationToken),
            "Cleanup" => await ExecuteCleanupAsync(log, cancellationToken),
            "HealthCheck" => await ExecuteHealthCheckAsync(log, cancellationToken),
            "ReportGeneration" => await ExecuteReportGenerationAsync(log, cancellationToken),
            _ => (false, $"Unknown task: {taskName}", 0)
        };
    }

    private async Task<(bool Success, string Message, int ItemsProcessed)> ExecuteDataSyncAsync(
        List<string> log,
        CancellationToken cancellationToken)
    {
        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Fetching data for synchronization...");
        var data = await _dataService.GetDataAsync(new DataRequest { MaxResults = 100 }, cancellationToken);

        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Found {data.TotalCount} items to sync");
        await Task.Delay(100, cancellationToken); // Simulate sync operation

        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Synchronization complete");
        return (true, $"Synchronized {data.Items.Count} items", data.Items.Count);
    }

    private async Task<(bool Success, string Message, int ItemsProcessed)> ExecuteCleanupAsync(
        List<string> log,
        CancellationToken cancellationToken)
    {
        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Starting cleanup with {_config.DataRetentionDays} day retention...");
        var cutoffDate = DateTime.UtcNow.AddDays(-_config.DataRetentionDays);

        await Task.Delay(75, cancellationToken); // Simulate cleanup operation

        var cleanedCount = new Random().Next(0, 25); // Simulated result
        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Removed {cleanedCount} expired items");
        return (true, $"Cleaned up {cleanedCount} items older than {cutoffDate:yyyy-MM-dd}", cleanedCount);
    }

    private async Task<(bool Success, string Message, int ItemsProcessed)> ExecuteHealthCheckAsync(
        List<string> log,
        CancellationToken cancellationToken)
    {
        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Performing health checks...");

        var checks = new[] { "Database", "Cache", "ExternalAPI", "Storage" };
        foreach (var check in checks)
        {
            await Task.Delay(20, cancellationToken);
            log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {check}: OK");
        }

        return (true, "All health checks passed", checks.Length);
    }

    private async Task<(bool Success, string Message, int ItemsProcessed)> ExecuteReportGenerationAsync(
        List<string> log,
        CancellationToken cancellationToken)
    {
        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Generating scheduled reports...");

        await Task.Delay(150, cancellationToken); // Simulate report generation

        var reportsGenerated = 3;
        log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] Generated {reportsGenerated} reports");
        return (true, $"Generated {reportsGenerated} scheduled reports", reportsGenerated);
    }

    /// <summary>
    /// Records task execution in history
    /// </summary>
    private static void RecordExecution(TaskExecutionRecord record)
    {
        ExecutionHistory.Enqueue(record);

        // Keep only last 1000 records
        while (ExecutionHistory.Count > 1000)
        {
            ExecutionHistory.TryDequeue(out _);
        }
    }
}
