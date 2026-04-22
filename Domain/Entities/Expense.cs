namespace FinanceTracker.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public decimal Amount { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public DateTime CreatedAt { get; }

    private Expense() { }

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

    public void Update(decimal amount, string category, string description, DateTime date)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        Amount = amount;
        Category = category.Trim();
        Description = description?.Trim() ?? string.Empty;
        Date = date;
    }
}
