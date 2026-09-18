using BeeCloud.Domain.Entities;

namespace BeeCloud.UnitTests.Domain;

[TestFixture]
public class NetworkAttachmentTests
{
    [Test]
    public void Constructor_WithValidIds_ShouldCreateAttachment()
    {
        var computeNodeId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        var before = DateTime.UtcNow;

        var attachment = new NetworkAttachment(
            computeNodeId,
            networkId);

        var after = DateTime.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(
                attachment.Id,
                Is.Not.EqualTo(Guid.Empty));

            Assert.That(
                attachment.ComputeNodeId,
                Is.EqualTo(computeNodeId));

            Assert.That(
                attachment.NetworkId,
                Is.EqualTo(networkId));

            Assert.That(
                attachment.AttachedAt,
                Is.InRange(before, after));
        });
    }

    [Test]
    public void Constructor_WithEmptyComputeNodeId_ShouldThrowArgumentException()
    {
        var networkId = Guid.NewGuid();

        var exception = Assert.Throws<ArgumentException>(
            () => new NetworkAttachment(
                Guid.Empty,
                networkId));

        Assert.That(
            exception!.ParamName,
            Is.EqualTo("computeNodeId"));
    }

    [Test]
    public void Constructor_WithEmptyNetworkId_ShouldThrowArgumentException()
    {
        var computeNodeId = Guid.NewGuid();

        var exception = Assert.Throws<ArgumentException>(
            () => new NetworkAttachment(
                computeNodeId,
                Guid.Empty));

        Assert.That(
            exception!.ParamName,
            Is.EqualTo("networkId"));
    }
}