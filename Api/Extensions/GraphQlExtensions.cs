using FinanceTracker.Api.GraphQL;

namespace FinanceTracker.Api.Extensions;

public static class GraphQlExtensions
{
    public static IServiceCollection AddGraphQl(this IServiceCollection services, IWebHostEnvironment env)
    {
        var builder = services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddAuthorizationCore();

        if (env.IsDevelopment())
        {
            builder.ModifyRequestOptions(o => o.IncludeExceptionDetails = true);
        }

        return services;
    }
}
