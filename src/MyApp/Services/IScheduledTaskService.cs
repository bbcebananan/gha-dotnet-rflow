namespace MyApp.Services;

/// <summary>
/// Service interface for scheduled task operations (REST endpoint)
/// </summary>
public interface IScheduledTaskService
{
    /// <summary>
    /// Executes a scheduled task run
    /// </summary>
    /// <param name="taskName">Optional specific task name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The task execution result</returns>
    Task<TaskRunResult> RunAsync(string? taskName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of scheduled tasks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Status information for all tasks</returns>
    Task<IReadOnlyList<TaskStatus>> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the execution history of scheduled tasks
    /// </summary>
    /// <param name="taskName">Optional filter by task name</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task execution history</returns>
    Task<IReadOnlyList<TaskExecutionRecord>> GetHistoryAsync(string? taskName = null, int limit = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a scheduled task run
/// </summary>
public sealed class TaskRunResult
{
    /// <summary>
    /// Whether the task run was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Task name that was executed
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of items processed
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Execution end time
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Any error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed execution log
    /// </summary>
    public IReadOnlyList<string> ExecutionLog { get; set; } = [];
}

/// <summary>
/// Current status of a scheduled task
/// </summary>
public sealed class TaskStatus
{
    /// <summary>
    /// Task name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether the task is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether the task is currently running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Last execution time
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Next scheduled execution time
    /// </summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>
    /// Last execution result
    /// </summary>
    public string LastResult { get; set; } = string.Empty;

    /// <summary>
    /// Schedule interval in minutes
    /// </summary>
    public int IntervalMinutes { get; set; }
}

/// <summary>
/// Historical record of a task execution
/// </summary>
public sealed class TaskExecutionRecord
{
    /// <summary>
    /// Unique execution identifier
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Task name
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Execution end time
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of items processed
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Result summary
    /// </summary>
    public string ResultSummary { get; set; } = string.Empty;

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Who or what triggered the execution
    /// </summary>
    public string TriggeredBy { get; set; } = string.Empty;
}
