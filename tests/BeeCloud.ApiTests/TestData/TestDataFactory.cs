using BeeCloud.ApiTests.Models.Requests;

namespace BeeCloud.ApiTests.TestData;

public static class TestDataFactory
{
    public static CreateNodeRequestModel CreateNodeRequest()
    {
        return new CreateNodeRequestModel
        {
            Name = $"api-test-node-{Guid.NewGuid():N}",
            GpuModel = "NVIDIA A100",
            GpuCount = 2
        };
    }
}