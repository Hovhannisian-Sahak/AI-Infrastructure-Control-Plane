using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Application.DTOs.Pagination;

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

    Task<PageResponse<IncidentResponse>> GetPageAsync(
        IncidentSeverity? severity,
        IncidentStatus? status,
        Guid? computeNodeId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
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