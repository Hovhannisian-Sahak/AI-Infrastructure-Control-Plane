using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.DTOs.Incidents;

public class CreateIncidentRequest
{
    public Guid ComputeNodeId { get; set; }

    public IncidentSeverity Severity { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
}