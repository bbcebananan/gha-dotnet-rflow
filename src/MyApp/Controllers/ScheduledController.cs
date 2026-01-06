using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Services;

namespace MyApp.Controllers;

/// <summary>
/// REST endpoint controller for scheduled task operations
/// Requires Windows Authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ScheduledController : ControllerBase
{
    private readonly IScheduledTaskService _scheduledTaskService;
    private readonly ILogger<ScheduledController> _logger;

    public ScheduledController(
        IScheduledTaskService scheduledTaskService,
        ILogger<ScheduledController> logger)
    {
        _scheduledTaskService = scheduledTaskService;
        _logger = logger;
    }

    /// <summary>
    /// Manually triggers a scheduled task run
    /// </summary>
    /// <param name="taskName">Optional specific task name to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task execution result</returns>
    [HttpPost("Run")]
    [ProducesResponseType(typeof(TaskRunResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TaskRunResult>> Run(
        [FromQuery] string? taskName = null,
        CancellationToken cancellationToken = default)
    {
        var user = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation(
            "Manual task run requested by {User}, task: {TaskName}",
            user, taskName ?? "default (DataSync)");

        var result = await _scheduledTaskService.RunAsync(taskName, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Task {TaskName} failed: {Error}",
                result.TaskName, result.ErrorMessage);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the status of all scheduled tasks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of task statuses</returns>
    [HttpGet("Status")]
    [ProducesResponseType(typeof(IReadOnlyList<Services.TaskStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<Services.TaskStatus>>> GetStatus(
        CancellationToken cancellationToken = default)
    {
        var statuses = await _scheduledTaskService.GetStatusAsync(cancellationToken);
        return Ok(statuses);
    }

    /// <summary>
    /// Gets the execution history of scheduled tasks
    /// </summary>
    /// <param name="taskName">Optional filter by task name</param>
    /// <param name="limit">Maximum number of records (default 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution history</returns>
    [HttpGet("History")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskExecutionRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TaskExecutionRecord>>> GetHistory(
        [FromQuery] string? taskName = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var history = await _scheduledTaskService.GetHistoryAsync(taskName, limit, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Gets all available task names
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available tasks</returns>
    [HttpGet("Tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TaskInfo>>> GetTasks(
        CancellationToken cancellationToken = default)
    {
        var statuses = await _scheduledTaskService.GetStatusAsync(cancellationToken);

        var tasks = statuses.Select(s => new TaskInfo
        {
            Name = s.Name,
            Description = s.Description,
            IsEnabled = s.IsEnabled,
            IntervalMinutes = s.IntervalMinutes
        }).ToList();

        return Ok(tasks);
    }
}

/// <summary>
/// Basic task information
/// </summary>
public sealed class TaskInfo
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
    /// Schedule interval in minutes
    /// </summary>
    public int IntervalMinutes { get; set; }
}
