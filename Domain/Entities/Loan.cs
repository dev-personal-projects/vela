using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

public sealed class Loan
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public string CounterpartyName { get; private set; } = string.Empty;
    public decimal Amount { get; }
    public decimal AmountRepaid { get; private set; }
    public LoanType Type { get; }
    public string Description { get; private set; } = string.Empty;
    public DateTime Date { get; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; }

    public decimal RemainingBalance => Amount - AmountRepaid;
    public bool IsSettled => AmountRepaid >= Amount;

    private Loan() { }

    public Loan(
        Guid userId,
        string counterpartyName,
        decimal amount,
        LoanType type,
        string description,
        DateTime date,
        DateTime? dueDate = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(counterpartyName))
        {
            throw new ArgumentException("CounterpartyName is required.", nameof(counterpartyName));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        }

        if (dueDate.HasValue && dueDate.Value <= date)
        {
            throw new ArgumentException("DueDate must be after Date.", nameof(dueDate));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        CounterpartyName = counterpartyName.Trim();
        Amount = amount;
        AmountRepaid = 0;
        Type = type;
        Description = description?.Trim() ?? string.Empty;
        Date = date;
        DueDate = dueDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void Repay(decimal repaymentAmount)
    {
        if (repaymentAmount <= 0)
        {
            throw new ArgumentException("Repayment amount must be greater than 0.", nameof(repaymentAmount));
        }

        if (repaymentAmount > RemainingBalance)
        {
            throw new ArgumentException("Repayment exceeds remaining balance.", nameof(repaymentAmount));
        }

        AmountRepaid += repaymentAmount;
    }

    public void Update(string counterpartyName, string description, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(counterpartyName))
        {
            throw new ArgumentException("CounterpartyName is required.", nameof(counterpartyName));
        }

        if (dueDate.HasValue && dueDate.Value <= Date)
        {
            throw new ArgumentException("DueDate must be after Date.", nameof(dueDate));
        }

        CounterpartyName = counterpartyName.Trim();
        Description = description?.Trim() ?? string.Empty;
        DueDate = dueDate;
    }
}
