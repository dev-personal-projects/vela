using FinanceTracker.Api.GraphQL;
using FinanceTracker.Api.GraphQL.ErrorHandling;
using FinanceTracker.Infrastructure.Auth;
using HotChocolate.AspNetCore.Serialization;

namespace FinanceTracker.Api.Extensions;

public static class GraphQlExtensions
{
    public static IServiceCollection AddGraphQl(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.AddHttpResponseFormatter<GraphQlHttpResponseFormatter>();

        var builder = services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddAuthorizationCore()
            .AddAuthorizationHandler<GraphQlAuthorizationHandler>()
            .AddErrorFilter<GraphQlErrorFilter>();

        // Avoid leaking secrets (e.g., connection strings) through exception details.
        builder.ModifyRequestOptions(o => o.IncludeExceptionDetails = false);

        return services;
    }
}
