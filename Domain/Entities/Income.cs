namespace FinanceTracker.Domain.Entities;

public sealed class Income
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public string Source { get; }
    public string Description { get; }
    public DateTime Date { get; }
    public DateTime CreatedAt { get; }

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
}
