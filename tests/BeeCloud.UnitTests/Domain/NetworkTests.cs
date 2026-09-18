using BeeCloud.Domain.Entities;

namespace BeeCloud.UnitTests.Domain;

[TestFixture]
public class NetworkTests
{
    [Test]
    public void Constructor_WithValidName_ShouldCreateNetwork()
    {
        var before = DateTime.UtcNow;

        var network = new Network(
            "production-network",
            "Production GPU network");

        var after = DateTime.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(network.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(
                network.Name,
                Is.EqualTo("production-network"));
            Assert.That(
                network.Description,
                Is.EqualTo("Production GPU network"));
            Assert.That(
                network.CreatedAt,
                Is.InRange(before, after));
        });
    }

    [Test]
    public void Constructor_WithNameOnly_ShouldCreateNetworkWithoutDescription()
    {
        var network = new Network("production-network");

        Assert.Multiple(() =>
        {
            Assert.That(network.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(
                network.Name,
                Is.EqualTo("production-network"));
            Assert.That(network.Description, Is.Null);
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException(
        string? name)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Network(name!));

        Assert.That(
            exception!.ParamName,
            Is.EqualTo("name"));
    }
}