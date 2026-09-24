using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Services;

public class OperationalMetricsService
    : IOperationalMetricsService
{
    private const string ProvisioningTotal =
        "provisioning_total";

    private const string ProvisioningFailures =
        "provisioning_failures_total";

    private const string RemediationTotal =
        "remediation_total";

    private const string RemediationFailures =
        "remediation_failures_total";

    private readonly IOperationalMetricRepository _repository;

    public OperationalMetricsService(
        IOperationalMetricRepository repository)
    {
        _repository = repository;
    }

    public Task IncrementProvisioningAsync(
        CancellationToken cancellationToken = default)
    {
        return IncrementAsync(
            ProvisioningTotal,
            cancellationToken);
    }

    public Task IncrementProvisioningFailureAsync(
        CancellationToken cancellationToken = default)
    {
        return IncrementAsync(
            ProvisioningFailures,
            cancellationToken);
    }

    public Task IncrementRemediationAsync(
        CancellationToken cancellationToken = default)
    {
        return IncrementAsync(
            RemediationTotal,
            cancellationToken);
    }

    public Task IncrementRemediationFailureAsync(
        CancellationToken cancellationToken = default)
    {
        return IncrementAsync(
            RemediationFailures,
            cancellationToken);
    }

    private async Task IncrementAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var metric = await _repository.GetByNameAsync(
            name,
            cancellationToken);

        if (metric is null)
        {
            metric = new OperationalMetric(name);

            await _repository.AddAsync(
                metric,
                cancellationToken);
        }

        metric.Increment();

        await _repository.SaveChangesAsync(
            cancellationToken);
    }
}