namespace BeeCloud.ApiTests.Models.Requests;

public class CreateNodeRequestModel
{
    public string Name { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public int GpuCount { get; set; }
}