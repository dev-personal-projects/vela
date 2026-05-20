namespace Vela.Domain.Entities;

public sealed class SavingsGoal
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public string Name { get; private set; } = string.Empty;
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public DateTime? Deadline { get; private set; }
    public DateTime CreatedAt { get; }

    public decimal RemainingAmount => TargetAmount - CurrentAmount;

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

    public void Update(string name, decimal targetAmount, DateTime? deadline)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (targetAmount <= 0)
        {
            throw new ArgumentException("TargetAmount must be greater than 0.", nameof(targetAmount));
        }

        if (targetAmount < CurrentAmount)
        {
            throw new ArgumentException("TargetAmount cannot be less than the current saved amount.", nameof(targetAmount));
        }

        Name = name.Trim();
        TargetAmount = targetAmount;
        Deadline = deadline;
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

    public void AddContribution(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Contribution amount must be greater than 0.", nameof(amount));
        }

        if (CurrentAmount + amount > TargetAmount)
        {
            throw new ArgumentException("Contribution would exceed the target amount.", nameof(amount));
        }

        CurrentAmount += amount;
    }
}
