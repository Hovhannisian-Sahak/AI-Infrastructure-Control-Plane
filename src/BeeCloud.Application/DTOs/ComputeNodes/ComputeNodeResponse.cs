namespace BeeCloud.Application.DTOs.ComputeNodes;

public class ComputeNodeResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public int GpuCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastHealthCheck { get; set; }
}