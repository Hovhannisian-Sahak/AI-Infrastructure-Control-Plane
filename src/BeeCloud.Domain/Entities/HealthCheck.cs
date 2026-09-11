namespace BeeCloud.Domain.Entities;

public class HealthCheck
{
    public Guid Id { get; private set; }

    public Guid ComputeNodeId { get; private set; }

    public bool IsHealthy { get; private set; }

    public double? CpuUsagePercent { get; private set; }

    public double? GpuUsagePercent { get; private set; }

    public double? GpuTemperatureCelsius { get; private set; }

    public DateTime CheckedAt { get; private set; }

    private HealthCheck()
    {
    }

    public HealthCheck(
        Guid computeNodeId,
        bool isHealthy,
        double? cpuUsagePercent = null,
        double? gpuUsagePercent = null,
        double? gpuTemperatureCelsius = null)
    {
        if (computeNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Compute node ID cannot be empty.",
                nameof(computeNodeId));
        }

        ValidatePercentage(cpuUsagePercent, nameof(cpuUsagePercent));
        ValidatePercentage(gpuUsagePercent, nameof(gpuUsagePercent));

        if (gpuTemperatureCelsius is < -100 or > 200)
        {
            throw new ArgumentException(
                "GPU temperature must be between -100 and 200 degrees Celsius.",
                nameof(gpuTemperatureCelsius));
        }

        Id = Guid.NewGuid();
        ComputeNodeId = computeNodeId;
        IsHealthy = isHealthy;
        CpuUsagePercent = cpuUsagePercent;
        GpuUsagePercent = gpuUsagePercent;
        GpuTemperatureCelsius = gpuTemperatureCelsius;
        CheckedAt = DateTime.UtcNow;
    }

    private static void ValidatePercentage(
        double? value,
        string parameterName)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentException(
                $"{parameterName} must be between 0 and 100.",
                parameterName);
        }
    }
}