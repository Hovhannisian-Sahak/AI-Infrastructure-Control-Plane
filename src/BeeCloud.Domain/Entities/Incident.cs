using BeeCloud.Domain.Enums;

namespace BeeCloud.Domain.Entities;

public class Incident
{
    public Guid Id { get; private set; }

    public Guid ComputeNodeId { get; private set; }

    public IncidentSeverity Severity { get; private set; }

    public IncidentStatus Status { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    private Incident()
    {
    }

    public Incident(
        Guid computeNodeId,
        IncidentSeverity severity,
        string title,
        string? description = null)
    {
        if (computeNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Compute node ID cannot be empty.",
                nameof(computeNodeId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Incident title cannot be empty.",
                nameof(title));
        }

        Id = Guid.NewGuid();
        ComputeNodeId = computeNodeId;
        Severity = severity;
        Status = IncidentStatus.Open;
        Title = title;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void StartInvestigation()
    {
        if (Status != IncidentStatus.Open)
        {
            throw new InvalidOperationException(
                $"Cannot investigate an incident with status '{Status}'.");
        }

        Status = IncidentStatus.Investigating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        if (Status == IncidentStatus.Resolved)
        {
            throw new InvalidOperationException(
                "Incident is already resolved.");
        }

        Status = IncidentStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = ResolvedAt.Value;
    }
}