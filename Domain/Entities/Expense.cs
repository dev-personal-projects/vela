namespace FinanceTracker.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public string Category { get; }
    public string Description { get; }
    public DateTime Date { get; }
    public DateTime CreatedAt { get; }

    public Expense(Guid userId, decimal amount, string category, string description, DateTime date)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Amount = amount;
        Category = category.Trim();
        Description = description?.Trim() ?? string.Empty;
        Date = date;
        CreatedAt = DateTime.UtcNow;
    }
}
