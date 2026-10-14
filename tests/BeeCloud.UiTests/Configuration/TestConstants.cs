namespace BeeCloud.UiTests.Configuration;

/// <summary>
/// Centralized test constants for consistent timeout and retry configuration
/// </summary>
public static class TestConstants
{
    // Timeouts (in milliseconds)
    public const int DefaultWaitTimeoutMs = 15_000;
    public const int QuickActionTimeoutMs = 5_000;
    public const int MediumActionTimeoutMs = 30_000;

    // Retry configuration
    public const int DefaultMaxRetries = 3;
    public const int InitialRetryDelayMs = 500;
    public const int MaxRetryDelayMs = 5_000;

    // Polling intervals
    public const int FastPollIntervalMs = 250;
    public const int DefaultPollIntervalMs = 500;
    public const int SlowPollIntervalMs = 1_000;

    // Pagination
    public const int NodePageSize = 8;
    public const int NetworkPageSize = 6;
    public const int IncidentPageSize = 12;
    public const int HealthNodePageSize = 10;
    public const int NetworkAttachmentPageSize = 3;

    // Test data constraints
    public const int MinNodeNameLength = 3;
    public const int MaxNodeNameLength = 50;
    public const int MinGpuCount = 1;
    public const int MaxGpuCount = 16;

    // Common test values
    public const string DefaultGpuModel = "NVIDIA A100";
    public const string DefaultGpuCount = "1";
    public const string GpuFaultIncidentTitle = "GPU failure detected";
}
