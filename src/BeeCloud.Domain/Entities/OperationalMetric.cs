namespace BeeCloud.Domain.Entities;

public class OperationalMetric
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public long Value { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private OperationalMetric()
    {
    }

    public OperationalMetric(
        string name,
        long value = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Metric name cannot be empty.",
                nameof(name));

        if (value < 0)
            throw new ArgumentException(
                "Metric value cannot be negative.",
                nameof(value));

        Id = Guid.NewGuid();
        Name = name;
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Increment(long amount = 1)
    {
        if (amount <= 0)
            throw new ArgumentException(
                "Increment amount must be greater than zero.",
                nameof(amount));

        Value += amount;
        UpdatedAt = DateTime.UtcNow;
    }
}