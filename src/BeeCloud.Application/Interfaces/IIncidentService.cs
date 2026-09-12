using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.Interfaces;

public interface IIncidentService
{
    Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
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

    Task<IncidentResponse> ResolveAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}