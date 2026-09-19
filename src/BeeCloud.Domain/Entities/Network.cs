namespace BeeCloud.Domain.Entities;

public class Network
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int MaxAttachments { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Network()
    {
        // Required by EF Core.
    }

    public Network(
        string name,
        string? description = null,
        int maxAttachments = 4)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Network name cannot be empty.",
                nameof(name));
        }

        if (maxAttachments <= 0)
        {
            throw new ArgumentException(
                "Maximum attachments must be greater than zero.",
                nameof(maxAttachments));
        }

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        IsActive = true;
        MaxAttachments = maxAttachments;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}