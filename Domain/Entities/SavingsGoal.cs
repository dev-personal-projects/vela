namespace FinanceTracker.Domain.Entities;

public sealed class SavingsGoal
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public string Name { get; } = string.Empty;
    public decimal TargetAmount { get; }
    public decimal CurrentAmount { get; private set; }
    public DateTime? Deadline { get; }
    public DateTime CreatedAt { get; }

    public decimal Progress =>
        TargetAmount == 0 ? 0 : Math.Round(CurrentAmount / TargetAmount * 100, 2);

    private SavingsGoal() { }

    public SavingsGoal(Guid userId, string name, decimal targetAmount, DateTime? deadline = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (targetAmount <= 0)
        {
            throw new ArgumentException("TargetAmount must be greater than 0.", nameof(targetAmount));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Name = name.Trim();
        TargetAmount = targetAmount;
        CurrentAmount = 0;
        Deadline = deadline;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(decimal newAmount)
    {
        if (newAmount < 0)
        {
            throw new ArgumentException("Amount cannot be negative.", nameof(newAmount));
        }

        if (newAmount > TargetAmount)
        {
            throw new ArgumentException("Amount cannot exceed TargetAmount.", nameof(newAmount));
        }

        CurrentAmount = newAmount;
    }
}
