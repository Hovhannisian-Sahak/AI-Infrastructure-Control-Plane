namespace BeeCloud.Application.Interfaces;

public interface IOperationalMetricsService
{
    Task IncrementProvisioningAsync(
        CancellationToken cancellationToken = default);

    Task IncrementProvisioningFailureAsync(
        CancellationToken cancellationToken = default);

    Task IncrementRemediationAsync(
        CancellationToken cancellationToken = default);

    Task IncrementRemediationFailureAsync(
        CancellationToken cancellationToken = default);
}