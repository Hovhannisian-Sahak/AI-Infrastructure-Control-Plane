using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.Interfaces;

public interface IIncidentService
{
    Task<IncidentResponse?> CreateForUnhealthyNodeAsync(
        ComputeNode node,
        HealthCheck healthCheck,
        CancellationToken cancellationToken = default);

    Task<IncidentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentResponse>> GetAllAsync(
        Guid? computeNodeId = null,
        IncidentSeverity? severity = null,
        IncidentStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResponse> StartInvestigationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task ResolveForNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);
}