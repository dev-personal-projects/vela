namespace FinanceTracker.Domain.Entities;

public sealed class LoanRepayment
{
    public Guid Id { get; }
    public Guid LoanId { get; }
    public decimal Amount { get; }
    public DateTime Date { get; }
    public string Note { get; } = string.Empty;
    public DateTime CreatedAt { get; }

    private LoanRepayment() { }

    public LoanRepayment(Guid loanId, decimal amount, DateTime date, string? note = null)
    {
        if (loanId == Guid.Empty)
        {
            throw new ArgumentException("LoanId is required.", nameof(loanId));
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.", nameof(amount));
        }

        Id = Guid.NewGuid();
        LoanId = loanId;
        Amount = amount;
        Date = date;
        Note = note?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
