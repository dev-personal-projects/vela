using FinanceTracker.Api.GraphQL;
using FinanceTracker.Api.GraphQL.ErrorHandling;
using FinanceTracker.Infrastructure.Auth;
using HotChocolate.AspNetCore.Serialization;

namespace FinanceTracker.Api.Extensions;

public static class GraphQlExtensions
{
    public static IServiceCollection AddGraphQl(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        services.AddHttpResponseFormatter<GraphQlHttpResponseFormatter>();

        var executionTimeoutSeconds = config.GetValue<int?>("GraphQL:ExecutionTimeoutSeconds") ?? 60;

        var builder = services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddAuthorizationCore()
            .AddAuthorizationHandler<GraphQlAuthorizationHandler>()
            .AddErrorFilter<GraphQlErrorFilter>();

        // Avoid leaking secrets through exception details and give slow remote DB writes
        // more room before GraphQL aborts the whole operation.
        builder.ModifyRequestOptions(o =>
        {
            o.IncludeExceptionDetails = false;
            o.ExecutionTimeout = TimeSpan.FromSeconds(executionTimeoutSeconds);
        });

        return services;
    }
}
