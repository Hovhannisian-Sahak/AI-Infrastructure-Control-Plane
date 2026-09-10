namespace BeeCloud.Application.DTOs.ComputeNodes;

public class CreateComputeNodeRequest
{
    public string Name { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public int GpuCount { get; set; }
}