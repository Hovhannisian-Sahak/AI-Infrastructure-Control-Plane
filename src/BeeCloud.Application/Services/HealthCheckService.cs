using BeeCloud.Application.DTOs.Health;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly IComputeNodeRepository _computeNodeRepository;
    private readonly IHealthCheckRepository _healthCheckRepository;

    public HealthCheckService(
        IComputeNodeRepository computeNodeRepository,
        IHealthCheckRepository healthCheckRepository)
    {
        _computeNodeRepository = computeNodeRepository;
        _healthCheckRepository = healthCheckRepository;
    }

    public async Task<HealthCheckResponse> CreateAsync(
        Guid computeNodeId,
        HealthCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var node = await _computeNodeRepository.GetByIdAsync(
            computeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{computeNodeId}' was not found.");
        }

        var healthCheck = new HealthCheck(
            computeNodeId,
            request.IsHealthy,
            request.CpuUsagePercent,
            request.GpuUsagePercent,
            request.GpuTemperatureCelsius);

        await _healthCheckRepository.AddAsync(
            healthCheck,
            cancellationToken);

        if (!request.IsHealthy)
        {
            node.MarkUnhealthy();
        }

        node.RecordHealthCheck(healthCheck.CheckedAt);

        await _healthCheckRepository.SaveChangesAsync(
            cancellationToken);

        await _computeNodeRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(healthCheck);
    }
    public async Task<HealthCheckResponse?> GetLatestAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        var node = await _computeNodeRepository.GetByIdAsync(
            computeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{computeNodeId}' was not found.");
        }

        var healthCheck =
            await _healthCheckRepository.GetLatestAsync(
                computeNodeId,
                cancellationToken);

        return healthCheck is null
            ? null
            : MapToResponse(healthCheck);
    }

    public async Task<IReadOnlyList<HealthCheckResponse>> GetHistoryAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        var node = await _computeNodeRepository.GetByIdAsync(
            computeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{computeNodeId}' was not found.");
        }

        var healthChecks =
            await _healthCheckRepository.GetHistoryAsync(
                computeNodeId,
                cancellationToken);

        return healthChecks
            .Select(MapToResponse)
            .ToList();
    }

    private static HealthCheckResponse MapToResponse(
        HealthCheck healthCheck)
    {
        return new HealthCheckResponse
        {
            Id = healthCheck.Id,
            ComputeNodeId = healthCheck.ComputeNodeId,
            IsHealthy = healthCheck.IsHealthy,
            CpuUsagePercent = healthCheck.CpuUsagePercent,
            GpuUsagePercent = healthCheck.GpuUsagePercent,
            GpuTemperatureCelsius =
                healthCheck.GpuTemperatureCelsius,
            CheckedAt = healthCheck.CheckedAt
        };
    }
}