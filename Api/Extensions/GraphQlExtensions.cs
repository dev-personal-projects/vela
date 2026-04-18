using FinanceTracker.Api.GraphQL;

namespace FinanceTracker.Api.Extensions;

public static class GraphQlExtensions
{
    public static IServiceCollection AddGraphQl(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddAuthorizationCore();

        return services;
    }
}
