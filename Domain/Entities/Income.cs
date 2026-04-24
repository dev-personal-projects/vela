namespace FinanceTracker.Domain.Entities;

public sealed class Income
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public decimal Amount { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public DateTime CreatedAt { get; }

    private Income() { }

    public Income(Guid userId, decimal amount, string source, string description, DateTime date)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;
        Source = source.Trim();
        Description = description?.Trim() ?? string.Empty;
        Date = date;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(decimal amount, string source, string description, DateTime date)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        Amount = amount;
        Source = source.Trim();
        Description = description?.Trim() ?? string.Empty;
        Date = date;
    }
}
