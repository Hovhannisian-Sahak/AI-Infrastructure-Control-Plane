using BeeCloud.Application.DTOs.ComputeNodes;
using BeeCloud.Application.DTOs.Networks;
using BeeCloud.Application.Validators;

namespace BeeCloud.UnitTests.Application;

[TestFixture]
public class RequestValidatorTests
{
    [Test]
    public void CreateComputeNodeRequestValidator_AcceptsBoundaryValues()
    {
        var validator = new CreateComputeNodeRequestValidator();

        var result = validator.Validate(new CreateComputeNodeRequest
        {
            Name = new string('n', 100),
            GpuModel = new string('g', 100),
            GpuCount = 16
        });

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("", "NVIDIA A100", 1)]
    [TestCase("node", "", 1)]
    [TestCase("node", "NVIDIA A100", 0)]
    [TestCase("node", "NVIDIA A100", 17)]
    public void CreateComputeNodeRequestValidator_RejectsInvalidFields(
        string name,
        string gpuModel,
        int gpuCount)
    {
        var validator = new CreateComputeNodeRequestValidator();

        var result = validator.Validate(new CreateComputeNodeRequest
        {
            Name = name,
            GpuModel = gpuModel,
            GpuCount = gpuCount
        });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CreateNetworkRequestValidator_AcceptsOptionalDescriptionAndMaximumName()
    {
        var validator = new CreateNetworkRequestValidator();

        var result = validator.Validate(new CreateNetworkRequest
        {
            Name = new string('n', 100),
            Description = null
        });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void CreateNetworkRequestValidator_RejectsEmptyName()
    {
        var validator = new CreateNetworkRequestValidator();

        var result = validator.Validate(new CreateNetworkRequest
        {
            Name = "",
            Description = "description"
        });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CreateNetworkRequestValidator_AcceptsEmptyOptionalDescription()
    {
        var validator = new CreateNetworkRequestValidator();

        var result = validator.Validate(new CreateNetworkRequest
        {
            Name = "valid-name",
            Description = ""
        });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void CreateNetworkRequestValidator_RejectsDescriptionOverMaximumLength()
    {
        var validator = new CreateNetworkRequestValidator();

        var result = validator.Validate(new CreateNetworkRequest
        {
            Name = "valid-network",
            Description = new string('d', 501)
        });

        Assert.That(result.IsValid, Is.False);
    }
}
