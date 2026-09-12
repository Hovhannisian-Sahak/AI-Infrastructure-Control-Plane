using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.Infrastructure.Persistence.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public IncidentRepository(ApplicationDbContext dbContext)
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
        Guid? computeNodeId = null,
        IncidentSeverity? severity = null,
        IncidentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Incidents
            .AsNoTracking()
            .AsQueryable();

        if (computeNodeId.HasValue)
        {
            query = query.Where(
                incident => incident.ComputeNodeId == computeNodeId.Value);
        }

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