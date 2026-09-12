using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.DTOs.Incidents;

public class IncidentResponse
{
    public Guid Id { get; set; }

    public Guid ComputeNodeId { get; set; }

    public IncidentSeverity Severity { get; set; }

    public IncidentStatus Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}