using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.Interfaces;

public interface IIncidentService
{
    // REST API operations

    Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default);

    Task<IncidentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentResponse>> GetAllAsync(
        IncidentSeverity? severity = null,
        IncidentStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResponse> StartInvestigationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IncidentResponse> ResolveAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Worker operations

    Task<IncidentResponse?> CreateForUnhealthyNodeAsync(
        ComputeNode node,
        HealthCheck healthCheck,
        CancellationToken cancellationToken = default);

    Task ResolveForNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);
}