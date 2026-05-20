using NpgsqlTypes;

namespace Vela.Domain.Enums;

public enum LoanType
{
    [PgName("Owed")]
    Owed,

    [PgName("Given")]
    Given
}
