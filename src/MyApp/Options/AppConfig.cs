namespace MyApp.Options;

/// <summary>
/// Application configuration options bound from appsettings.json
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "AppConfig";

    /// <summary>
    /// Application display name
    /// </summary>
    public string ApplicationName { get; set; } = "MyApp";

    /// <summary>
    /// Whether to show detailed error messages (should be false in production)
    /// </summary>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    /// Base URL for the external REST API
    /// </summary>
    public string ExternalApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for external service authentication (encrypted with DPAPI in production)
    /// </summary>
    public string ExternalApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether scheduled tasks are enabled
    /// </summary>
    public bool ScheduledTaskEnabled { get; set; } = true;

    /// <summary>
    /// Interval in minutes between scheduled task runs
    /// </summary>
    public int ScheduledTaskIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Number of days to retain data before cleanup
    /// </summary>
    public int DataRetentionDays { get; set; } = 30;

    /// <summary>
    /// Maximum number of concurrent requests to process
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 100;
}
