using BeeCloud.Domain.Entities;
using NUnit.Framework;

namespace BeeCloud.UnitTests.Domain;

[TestFixture]
public class HealthCheckTests
{
    [Test]
    public void NewHealthCheck_ShouldBeHealthy()
    {
        var nodeId = Guid.NewGuid();

        var healthCheck = new HealthCheck(
            nodeId,
            true,
            25,
            40,
            55);

        Assert.That(healthCheck.ComputeNodeId, Is.EqualTo(nodeId));
        Assert.That(healthCheck.IsHealthy, Is.True);
    }

    [Test]
    public void NewHealthCheck_ShouldStoreMetrics()
    {
        var healthCheck = new HealthCheck(
            Guid.NewGuid(),
            true,
            25,
            40,
            55);

        Assert.That(healthCheck.CpuUsagePercent, Is.EqualTo(25));
        Assert.That(healthCheck.GpuUsagePercent, Is.EqualTo(40));
        Assert.That(healthCheck.GpuTemperatureCelsius, Is.EqualTo(55));
    }

    [Test]
    public void NewHealthCheck_ShouldSetCheckedAt()
    {
        var before = DateTime.UtcNow;

        var healthCheck = new HealthCheck(
            Guid.NewGuid(),
            true);

        var after = DateTime.UtcNow;

        Assert.That(
            healthCheck.CheckedAt,
            Is.InRange(before, after));
    }

    [Test]
    public void NewHealthCheck_WithEmptyNodeId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new HealthCheck(
                Guid.Empty,
                true));
    }

    [Test]
    public void NewHealthCheck_WithInvalidCpuUsage_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new HealthCheck(
                Guid.NewGuid(),
                true,
                101));
    }

    [Test]
    public void NewHealthCheck_WithInvalidGpuUsage_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new HealthCheck(
                Guid.NewGuid(),
                true,
                null,
                -1));
    }

    [Test]
    public void NewHealthCheck_WithInvalidTemperature_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new HealthCheck(
                Guid.NewGuid(),
                true,
                null,
                null,
                201));
    }
}