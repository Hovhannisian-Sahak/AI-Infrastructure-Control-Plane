namespace BeeCloud.Application.DTOs.Health;

public class HealthCheckResponse
{
    public Guid Id { get; set; }

    public Guid ComputeNodeId { get; set; }

    public bool IsHealthy { get; set; }

    public double? CpuUsagePercent { get; set; }

    public double? GpuUsagePercent { get; set; }

    public double? GpuTemperatureCelsius { get; set; }

    public DateTime CheckedAt { get; set; }
}