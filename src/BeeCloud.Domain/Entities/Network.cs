namespace BeeCloud.Domain.Entities;

public class Network
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Network()
    {
        // Required by EF Core.
    }

    public Network(
        string name,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Network name cannot be empty.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}