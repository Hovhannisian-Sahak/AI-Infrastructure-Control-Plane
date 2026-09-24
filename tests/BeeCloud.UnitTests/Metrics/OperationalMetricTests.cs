using BeeCloud.Domain.Entities;

namespace BeeCloud.UnitTests.Metrics;

[TestFixture]
public class OperationalMetricTests
{
    [Test]
    public void Constructor_WithValidName_ShouldCreateMetric()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        Assert.That(metric.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(metric.Name, Is.EqualTo("provisioning_total"));
        Assert.That(metric.Value, Is.EqualTo(0));
        Assert.That(metric.UpdatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void Constructor_WithInitialValue_ShouldSetValue()
    {
        var metric = new OperationalMetric(
            "provisioning_total",
            5);

        Assert.That(metric.Value, Is.EqualTo(5));
    }

    [Test]
    public void Constructor_WithEmptyName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new OperationalMetric(""));
    }

    [Test]
    public void Constructor_WithWhitespaceName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new OperationalMetric("   "));
    }

    [Test]
    public void Constructor_WithNegativeValue_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new OperationalMetric(
                "provisioning_total",
                -1));
    }

    [Test]
    public void Increment_ShouldIncreaseValue()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        metric.Increment();

        Assert.That(metric.Value, Is.EqualTo(1));
    }

    [Test]
    public void Increment_WithAmount_ShouldIncreaseValueByAmount()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        metric.Increment(5);

        Assert.That(metric.Value, Is.EqualTo(5));
    }

    [Test]
    public void Increment_MultipleTimes_ShouldAccumulateValue()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        metric.Increment();
        metric.Increment(3);
        metric.Increment(2);

        Assert.That(metric.Value, Is.EqualTo(6));
    }

    [Test]
    public void Increment_WithZero_ShouldThrow()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        Assert.Throws<ArgumentException>(
            () => metric.Increment(0));
    }

    [Test]
    public void Increment_WithNegativeAmount_ShouldThrow()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        Assert.Throws<ArgumentException>(
            () => metric.Increment(-1));
    }

    [Test]
    public void Increment_ShouldUpdateUpdatedAt()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        var previousUpdatedAt = metric.UpdatedAt;

        Thread.Sleep(10);

        metric.Increment();

        Assert.That(
            metric.UpdatedAt,
            Is.GreaterThan(previousUpdatedAt));
    }
}