using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;

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

    public async Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ComputeNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Compute node ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException(
                "Incident title is required.");
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

        return incident is null
            ? null
            : MapToResponse(incident);
    }

    public async Task<IReadOnlyList<IncidentResponse>> GetAllAsync(
        Guid? computeNodeId = null,
        IncidentSeverity? severity = null,
        IncidentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var incidents = await _incidentRepository.GetAllAsync(
            computeNodeId,
            severity,
            status,
            cancellationToken);

        return incidents
            .Select(MapToResponse)
            .ToList();
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
            ResolvedAt = incident.ResolvedAt
        };
    }
}