namespace BeeCloud.Application.DTOs.Health;

public class HealthCheckRequest
{
    public bool IsHealthy { get; set; }

    public double? CpuUsagePercent { get; set; }

    public double? GpuUsagePercent { get; set; }

    public double? GpuTemperatureCelsius { get; set; }
}