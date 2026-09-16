using BeeCloud.Domain.Entities;

namespace BeeCloud.UnitTests.NodeMetrics;

[TestFixture]
public class NodeMetricTests
{
    [Test]
    public void Constructor_WithValidValues_ShouldCreateMetric()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var metric = new NodeMetric(
            nodeId,
            50,
            75,
            70);

        // Assert
        Assert.That(metric.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(metric.ComputeNodeId, Is.EqualTo(nodeId));
        Assert.That(metric.CpuUsagePercent, Is.EqualTo(50));
        Assert.That(metric.GpuUsagePercent, Is.EqualTo(75));
        Assert.That(metric.GpuTemperatureCelsius, Is.EqualTo(70));
        Assert.That(metric.RecordedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void Constructor_WithEmptyNodeId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new NodeMetric(
                Guid.Empty,
                50,
                75,
                70));
    }

    [Test]
    public void Constructor_WithCpuUsageBelowZero_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new NodeMetric(
                Guid.NewGuid(),
                -1,
                75,
                70));
    }

    [Test]
    public void Constructor_WithCpuUsageAbove100_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new NodeMetric(
                Guid.NewGuid(),
                101,
                75,
                70));
    }

    [Test]
    public void Constructor_WithGpuUsageBelowZero_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new NodeMetric(
                Guid.NewGuid(),
                50,
                -1,
                70));
    }

    [Test]
    public void Constructor_WithGpuUsageAbove100_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new NodeMetric(
                Guid.NewGuid(),
                50,
                101,
                70));
    }

    [Test]
    public void Constructor_WithTemperatureBelowMinus100_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new NodeMetric(
                Guid.NewGuid(),
                50,
                75,
                -101));
    }

    [Test]
    public void Constructor_WithTemperatureAbove200_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            new NodeMetric(
                Guid.NewGuid(),
                50,
                75,
                201));
    }
}