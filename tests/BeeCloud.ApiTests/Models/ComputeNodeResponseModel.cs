namespace BeeCloud.ApiTests.Models;

public class ComputeNodeResponseModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public int GpuCount { get; set; }

    public string Status { get; set; } = string.Empty;
}