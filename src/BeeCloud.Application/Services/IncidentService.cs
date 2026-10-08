using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Application.DTOs.Pagination;

namespace BeeCloud.Application.Services;

public class IncidentService : IIncidentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IComputeNodeRepository _computeNodeRepository;

    public IncidentService(
        IIncidentRepository incidentRepository,
        IComputeNodeRepository computeNodeRepository)
    {
        _incidentRepository = incidentRepository;
        _computeNodeRepository = computeNodeRepository;
    }

    // ============================================================
    // REST API
    // ============================================================

    public async Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ComputeNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Compute node ID cannot be empty.",
                nameof(request.ComputeNodeId));
        }

        var node = await _computeNodeRepository.GetByIdAsync(
            request.ComputeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{request.ComputeNodeId}' was not found.");
        }

        var incident = new Incident(
            request.ComputeNodeId,
            request.Severity,
            request.Title,
            request.Description);

        await _incidentRepository.AddAsync(
            incident,
            cancellationToken);

        await _incidentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(incident);
    }

    public async Task<IncidentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (incident is null)
        {
            return null;
        }

        return MapToResponse(incident);
    }

    public async Task<IReadOnlyList<IncidentResponse>> GetAllAsync(
        IncidentSeverity? severity = null,
        IncidentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var incidents = await _incidentRepository.GetAllAsync(
            severity,
            status,
            cancellationToken);

        return incidents
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<PageResponse<IncidentResponse>> GetPageAsync(
        IncidentSeverity? severity,
        IncidentStatus? status,
        Guid? computeNodeId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        if (computeNodeId == Guid.Empty)
            throw new ArgumentException("Compute node ID cannot be empty.", nameof(computeNodeId));
        if (from.HasValue && to.HasValue && from > to)
            throw new ArgumentException("'from' must be earlier than or equal to 'to'.");

        var result = await _incidentRepository.GetPageAsync(
            severity, status, computeNodeId, from, to, page, pageSize, cancellationToken);

        return new PageResponse<IncidentResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<IncidentResponse> StartInvestigationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetIncidentOrThrowAsync(
            id,
            cancellationToken);

        incident.StartInvestigation();

        await _incidentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(incident);
    }

    public async Task<IncidentResponse> ResolveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetIncidentOrThrowAsync(
            id,
            cancellationToken);

        incident.Resolve();

        await _incidentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(incident);
    }

    // ============================================================
    // HEALTH MONITORING / WORKER
    // ============================================================

    public async Task<IncidentResponse?> CreateForUnhealthyNodeAsync(
        ComputeNode node,
        HealthCheck healthCheck,
        CancellationToken cancellationToken = default)
    {
        var existingIncident =
            await _incidentRepository.GetActiveForNodeAsync(
                node.Id,
                cancellationToken);

        if (existingIncident is not null)
        {
            existingIncident.RecordOccurrence(healthCheck.CheckedAt);
            await _incidentRepository.SaveChangesAsync(
                cancellationToken);
            return MapToResponse(existingIncident);
        }

        var severity = DetermineSeverity(healthCheck);

        var incident = new Incident(
            node.Id,
            severity,
            BuildTitle(healthCheck),
            BuildDescription(healthCheck),
            healthCheck.CheckedAt);

        await _incidentRepository.AddAsync(
            incident,
            cancellationToken);

        await _incidentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(incident);
    }

    public async Task ResolveForNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var incident =
            await _incidentRepository.GetActiveForNodeAsync(
                nodeId,
                cancellationToken);

        if (incident is null)
        {
            return;
        }

        incident.Resolve();

        await _incidentRepository.SaveChangesAsync(
            cancellationToken);
    }

    // ============================================================
    // PRIVATE METHODS
    // ============================================================

    private async Task<Incident> GetIncidentOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException(
                $"Incident with id '{id}' was not found.");
        }

        return incident;
    }

    private static IncidentSeverity DetermineSeverity(
        HealthCheck healthCheck)
    {
        if (healthCheck.GpuTemperatureCelsius >= 100)
        {
            return IncidentSeverity.Critical;
        }

        if (healthCheck.GpuTemperatureCelsius >= 90)
        {
            return IncidentSeverity.High;
        }

        if (healthCheck.GpuUsagePercent <= 1)
        {
            return IncidentSeverity.High;
        }

        return IncidentSeverity.Medium;
    }

    private static string BuildTitle(
        HealthCheck healthCheck)
    {
        if (healthCheck.GpuTemperatureCelsius >= 100)
        {
            return "GPU overheat detected";
        }

        if (healthCheck.GpuUsagePercent <= 1)
        {
            return "GPU failure detected";
        }

        return "Compute node health check failed";
    }

    private static string BuildDescription(
        HealthCheck healthCheck)
    {
        return
            $"Health check failed. " +
            $"CPU: {healthCheck.CpuUsagePercent:F1}%, " +
            $"GPU: {healthCheck.GpuUsagePercent:F1}%, " +
            $"GPU temperature: {healthCheck.GpuTemperatureCelsius:F1}°C.";
    }

    private static IncidentResponse MapToResponse(
        Incident incident)
    {
        return new IncidentResponse
        {
            Id = incident.Id,
            ComputeNodeId = incident.ComputeNodeId,
            Severity = incident.Severity,
            Status = incident.Status,
            Title = incident.Title,
            Description = incident.Description,
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt,
            LastSeenAt = incident.LastSeenAt,
            OccurrenceCount = incident.OccurrenceCount,
            ResolvedAt = incident.ResolvedAt
        };
    }
}