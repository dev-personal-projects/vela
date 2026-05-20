using Vela.Domain.Entities;
using Vela.Domain.Enums;
using UnitTests.Common;

namespace UnitTests.Builders;

internal sealed class LoanBuilder
{
    private string _counterpartyName = "John Smith";
    private string? _counterpartyPhoneNumber = "1234567890";
    private decimal _amount = 1000m;
    private decimal _amountRepaid;
    private LoanType _type = LoanType.Owed;
    private string _description = "Loan for testing";
    private DateTime _date = TestDates.OneWeekAgo;
    private DateTime? _dueDate = TestDates.NextMonth;

    internal LoanBuilder WithCounterpartyName(string counterpartyName)
    {
        _counterpartyName = counterpartyName;
        return this;
    }

    internal LoanBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    internal LoanBuilder WithAmountRepaid(decimal amountRepaid)
    {
        _amountRepaid = amountRepaid;
        return this;
    }

    internal LoanBuilder WithType(LoanType type)
    {
        _type = type;
        return this;
    }

    internal LoanBuilder WithDate(DateTime date)
    {
        _date = date;
        return this;
    }

    internal LoanBuilder WithDueDate(DateTime? dueDate)
    {
        _dueDate = dueDate;
        return this;
    }

    internal Vela.Domain.Entities.Loan Build(Guid? userId = null)
    {
        var loan = new Vela.Domain.Entities.Loan(
            userId ?? TestUsers.PrimaryUserId,
            _counterpartyName,
            _counterpartyPhoneNumber,
            _amount,
            _type,
            _description,
            _date,
            _dueDate);

        if (_amountRepaid > 0)
        {
            loan.Repay(_amountRepaid);
        }

        return loan;
    }
}
