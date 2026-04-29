using NpgsqlTypes;

namespace FinanceTracker.Domain.Enums;

public enum LoanType
{
    [PgName("Owed")]
    Owed,

    [PgName("Given")]
    Given
}
