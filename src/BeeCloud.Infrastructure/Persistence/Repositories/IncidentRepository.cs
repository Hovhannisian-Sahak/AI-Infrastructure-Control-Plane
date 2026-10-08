using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public IncidentRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Incident?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Incidents
            .FirstOrDefaultAsync(
                incident => incident.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Incident>> GetAllAsync(
        IncidentSeverity? severity = null,
        IncidentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Incidents
            .AsNoTracking()
            .AsQueryable();

        if (severity.HasValue)
        {
            query = query.Where(
                incident => incident.Severity == severity.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(
                incident => incident.Status == status.Value);
        }

        return await query
            .OrderByDescending(incident => incident.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Incident> Items, int TotalCount)> GetPageAsync(
        IncidentSeverity? severity,
        IncidentStatus? status,
        Guid? computeNodeId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(severity, status, computeNodeId, from, to);

        var latestResolvedIncidentIds = query
            .Where(incident => incident.Status == IncidentStatus.Resolved)
            .GroupBy(incident => incident.ComputeNodeId)
            .Select(group => group
                .OrderByDescending(incident => incident.ResolvedAt)
                .ThenByDescending(incident => incident.CreatedAt)
                .ThenByDescending(incident => incident.Id)
                .Select(incident => incident.Id)
                .First());

        query = query.Where(incident =>
            incident.Status != IncidentStatus.Resolved ||
            latestResolvedIncidentIds.Contains(incident.Id));

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (long)(page - 1) * pageSize;
        if (offset > int.MaxValue)
            return (Array.Empty<Incident>(), totalCount);

        var items = await query
            .OrderByDescending(incident => incident.CreatedAt)
            .ThenByDescending(incident => incident.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<Incident> BuildQuery(
        IncidentSeverity? severity,
        IncidentStatus? status,
        Guid? computeNodeId,
        DateTime? from,
        DateTime? to)
    {
        var query = _dbContext.Incidents.AsNoTracking().AsQueryable();
        if (severity.HasValue)
            query = query.Where(incident => incident.Severity == severity.Value);
        if (status.HasValue)
            query = query.Where(incident => incident.Status == status.Value);
        if (computeNodeId.HasValue)
            query = query.Where(incident => incident.ComputeNodeId == computeNodeId.Value);
        if (from.HasValue)
            query = query.Where(incident => incident.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(incident => incident.CreatedAt <= to.Value);
        return query;
    }

    public async Task<Incident?> GetActiveForNodeAsync(
        Guid computeNodeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Incidents
            .FirstOrDefaultAsync(
                incident =>
                    incident.ComputeNodeId == computeNodeId &&
                    (incident.Status == IncidentStatus.Open ||
                     incident.Status == IncidentStatus.Investigating),
                cancellationToken);
    }

    public async Task AddAsync(
        Incident incident,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Incidents.AddAsync(
            incident,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}