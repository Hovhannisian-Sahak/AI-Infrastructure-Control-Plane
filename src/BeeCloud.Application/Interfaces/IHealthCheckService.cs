using BeeCloud.Application.DTOs.Health;

namespace BeeCloud.Application.Interfaces;

public interface IHealthCheckService
{
    Task<HealthCheckResponse> CreateAsync(
        Guid computeNodeId,
        HealthCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<HealthCheckResponse?> GetLatestAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthCheckResponse>> GetHistoryAsync(
        Guid computeNodeId,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}