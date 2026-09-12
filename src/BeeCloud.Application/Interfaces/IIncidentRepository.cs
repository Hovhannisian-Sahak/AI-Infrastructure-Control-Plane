using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.Interfaces;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Incident>> GetAllAsync(
        Guid? computeNodeId = null,
        IncidentSeverity? severity = null,
        IncidentStatus? status = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Incident incident,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}