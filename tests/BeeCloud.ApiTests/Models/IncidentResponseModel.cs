namespace BeeCloud.ApiTests.Models;

public class IncidentResponseModel
{
    public Guid Id { get; set; }

    public Guid ComputeNodeId { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public int OccurrenceCount { get; set; }

    public DateTime? ResolvedAt { get; set; }
}