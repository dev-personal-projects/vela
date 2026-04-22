using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Infrastructure.Auth;

public sealed class GraphQlAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : HotChocolate.Authorization.IAuthorizationHandler
{
    public ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken)
    {
        var user = context.GetUser();
        return ValueTask.FromResult(Evaluate(user, directive.Roles));
    }

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;

        foreach (var directive in directives)
        {
            var result = Evaluate(user, directive.Roles);
            if (result != AuthorizeResult.Allowed)
            {
                return ValueTask.FromResult(result);
            }
        }

        return ValueTask.FromResult(AuthorizeResult.Allowed);
    }

    private static AuthorizeResult Evaluate(ClaimsPrincipal? user, IReadOnlyList<string>? roles)
    {
        if (user is null || user.Identity?.IsAuthenticated != true)
        {
            return AuthorizeResult.NotAuthenticated;
        }

        if (roles is { Count: > 0 } && !roles.Any(user.IsInRole))
        {
            return AuthorizeResult.NotAllowed;
        }

        return AuthorizeResult.Allowed;
    }
}
