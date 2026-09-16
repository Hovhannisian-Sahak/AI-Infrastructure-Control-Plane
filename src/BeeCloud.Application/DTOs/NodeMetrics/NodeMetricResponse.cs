namespace BeeCloud.Application.DTOs.NodeMetrics;

public class NodeMetricResponse
{
    public Guid Id { get; set; }
    public Guid ComputeNodeId { get; set; }

    public double CpuUsagePercent { get; set; }
    public double GpuUsagePercent { get; set; }
    public double GpuTemperatureCelsius { get; set; }

    public DateTime RecordedAt { get; set; }
}