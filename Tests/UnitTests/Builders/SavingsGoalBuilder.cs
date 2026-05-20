using Vela.Domain.Entities;
using UnitTests.Common;

namespace UnitTests.Builders;

internal sealed class SavingsGoalBuilder
{
    private string _name = "Emergency Fund";
    private decimal _targetAmount = 1000m;
    private decimal _currentAmount;
    private DateTime? _deadline = TestDates.NextWeek;

    internal SavingsGoalBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    internal SavingsGoalBuilder WithTargetAmount(decimal targetAmount)
    {
        _targetAmount = targetAmount;
        return this;
    }

    internal SavingsGoalBuilder WithCurrentAmount(decimal currentAmount)
    {
        _currentAmount = currentAmount;
        return this;
    }

    internal SavingsGoalBuilder WithDeadline(DateTime? deadline)
    {
        _deadline = deadline;
        return this;
    }

    internal SavingsGoal Build(Guid? userId = null)
    {
        var goal = new SavingsGoal(userId ?? TestUsers.PrimaryUserId, _name, _targetAmount, _deadline);

        if (_currentAmount > 0)
        {
            goal.UpdateProgress(_currentAmount);
        }

        return goal;
    }
}
