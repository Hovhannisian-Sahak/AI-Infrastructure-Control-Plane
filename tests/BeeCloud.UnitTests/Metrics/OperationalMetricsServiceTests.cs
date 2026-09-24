using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using Moq;

namespace BeeCloud.UnitTests.Metrics;

[TestFixture]
public class OperationalMetricsServiceTests
{
    private Mock<IOperationalMetricRepository>
        _repository = null!;

    private OperationalMetricsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository =
            new Mock<IOperationalMetricRepository>();

        _service =
            new OperationalMetricsService(
                _repository.Object);
    }

    [Test]
    public async Task IncrementProvisioningAsync_WhenMetricExists_ShouldIncrementMetric()
    {
        var metric = new OperationalMetric(
            "provisioning_total");

        _repository
            .Setup(repository =>
                repository.GetByNameAsync(
                    "provisioning_total",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        await _service.IncrementProvisioningAsync();

        Assert.That(metric.Value, Is.EqualTo(1));

        _repository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _repository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<OperationalMetric>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IncrementProvisioningAsync_WhenMetricDoesNotExist_ShouldCreateMetric()
    {
        _repository
            .Setup(repository =>
                repository.GetByNameAsync(
                    "provisioning_total",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((OperationalMetric?)null);

        await _service.IncrementProvisioningAsync();

        _repository.Verify(
            repository =>
                repository.AddAsync(
                    It.Is<OperationalMetric>(metric =>
                        metric.Name == "provisioning_total" &&
                        metric.Value == 1),
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _repository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task IncrementProvisioningFailureAsync_ShouldIncrementCorrectMetric()
    {
        var metric = new OperationalMetric(
            "provisioning_failures_total");

        _repository
            .Setup(repository =>
                repository.GetByNameAsync(
                    "provisioning_failures_total",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        await _service.IncrementProvisioningFailureAsync();

        Assert.That(metric.Value, Is.EqualTo(1));

        _repository.Verify(
            repository =>
                repository.GetByNameAsync(
                    "provisioning_failures_total",
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task IncrementRemediationAsync_ShouldIncrementCorrectMetric()
    {
        var metric = new OperationalMetric(
            "remediation_total");

        _repository
            .Setup(repository =>
                repository.GetByNameAsync(
                    "remediation_total",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        await _service.IncrementRemediationAsync();

        Assert.That(metric.Value, Is.EqualTo(1));
    }

    [Test]
    public async Task IncrementRemediationFailureAsync_ShouldIncrementCorrectMetric()
    {
        var metric = new OperationalMetric(
            "remediation_failures_total");

        _repository
            .Setup(repository =>
                repository.GetByNameAsync(
                    "remediation_failures_total",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(metric);

        await _service.IncrementRemediationFailureAsync();

        Assert.That(metric.Value, Is.EqualTo(1));
    }

    [Test]
    public async Task IncrementProvisioningAsync_ShouldPassCancellationToken()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var metric = new OperationalMetric(
            "provisioning_total");

        _repository
            .Setup(repository =>
                repository.GetByNameAsync(
                    "provisioning_total",
                    cancellationToken))
            .ReturnsAsync(metric);

        await _service.IncrementProvisioningAsync(
            cancellationToken);

        _repository.Verify(
            repository =>
                repository.GetByNameAsync(
                    "provisioning_total",
                    cancellationToken),
            Times.Once);
    }
}