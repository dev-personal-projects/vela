using HotChocolate.Authorization;
using System.Security.Claims;

namespace FinanceTracker.Api.GraphQL;

public sealed class Query
{
    public string Health() => "Finance Tracker API is running!";

    [Authorize]
    public string Me(ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue("sub");

        return userId ?? "unknown";
    }
}
