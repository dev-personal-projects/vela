using Vela.Domain.Enums;

namespace Vela.Domain.Entities;

public sealed class Loan
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public string CounterpartyName { get; private set; } = string.Empty;
    public string? CounterpartyPhoneNumber { get; private set; }
    public decimal Amount { get; private set; }
    public decimal AmountRepaid { get; private set; }
    public LoanType Type { get; }
    public string Description { get; private set; } = string.Empty;
    public DateTime Date { get; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; }

    public decimal RemainingBalance => Amount - AmountRepaid;
    public bool IsSettled => AmountRepaid >= Amount;
    public bool IsOverdue => !IsSettled && DueDate.HasValue && DueDate.Value < DateTime.UtcNow;
    public LoanStatus Status => IsSettled
        ? LoanStatus.Settled
        : IsOverdue
            ? LoanStatus.Overdue
            : LoanStatus.Active;

    private Loan() { }

    public Loan(
        Guid userId,
        string counterpartyName,
        string? counterpartyPhoneNumber,
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
        CounterpartyPhoneNumber = NormalizePhoneNumber(counterpartyPhoneNumber);
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

    public void ReverseRepayment(decimal repaymentAmount)
    {
        if (repaymentAmount <= 0)
        {
            throw new ArgumentException("Repayment amount must be greater than 0.", nameof(repaymentAmount));
        }

        if (repaymentAmount > AmountRepaid)
        {
            throw new ArgumentException("Cannot reverse more than the amount already repaid.", nameof(repaymentAmount));
        }

        AmountRepaid -= repaymentAmount;
    }

    public void Update(string counterpartyName, string? counterpartyPhoneNumber, decimal amount, string description, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(counterpartyName))
        {
            throw new ArgumentException("CounterpartyName is required.", nameof(counterpartyName));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        }

        if (amount < AmountRepaid)
        {
            throw new ArgumentException($"Amount cannot be less than the amount already repaid ({AmountRepaid:C}).", nameof(amount));
        }

        if (dueDate.HasValue && dueDate.Value <= Date)
        {
            throw new ArgumentException("DueDate must be after Date.", nameof(dueDate));
        }

        CounterpartyName = counterpartyName.Trim();
        CounterpartyPhoneNumber = NormalizePhoneNumber(counterpartyPhoneNumber);
        Amount = amount;
        Description = description?.Trim() ?? string.Empty;
        DueDate = dueDate;
    }

    private static string? NormalizePhoneNumber(string? phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
}
