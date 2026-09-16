namespace BeeCloud.Domain.Entities;

public class NodeMetric
{
    public Guid Id { get; private set; }
    public Guid ComputeNodeId { get; private set; }

    public double CpuUsagePercent { get; private set; }
    public double GpuUsagePercent { get; private set; }
    public double GpuTemperatureCelsius { get; private set; }

    public DateTime RecordedAt { get; private set; }

    private NodeMetric()
    {
    }

    public NodeMetric(
        Guid computeNodeId,
        double cpuUsagePercent,
        double gpuUsagePercent,
        double gpuTemperatureCelsius)
    {
        if (computeNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Compute node ID cannot be empty.",
                nameof(computeNodeId));
        }

        ValidatePercentage(
            cpuUsagePercent,
            nameof(cpuUsagePercent));

        ValidatePercentage(
            gpuUsagePercent,
            nameof(gpuUsagePercent));

        if (gpuTemperatureCelsius is < -100 or > 200)
        {
            throw new ArgumentException(
                "GPU temperature must be between -100 and 200 degrees Celsius.",
                nameof(gpuTemperatureCelsius));
        }

        Id = Guid.NewGuid();
        ComputeNodeId = computeNodeId;
        CpuUsagePercent = cpuUsagePercent;
        GpuUsagePercent = gpuUsagePercent;
        GpuTemperatureCelsius = gpuTemperatureCelsius;
        RecordedAt = DateTime.UtcNow;
    }

    private static void ValidatePercentage(
        double value,
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