using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Api.Extensions;

public static class SupabaseExtensions
{
    public static IServiceCollection AddSupabaseClient(this IServiceCollection services, IConfiguration config)
    {
        var supabaseUrl = config["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is not configured.");

        var anonKey = config["Supabase:AnonKey"]
            ?? throw new InvalidOperationException("Supabase:AnonKey is not configured.");

        services.AddHttpClient<ISupabaseAuthClient, SupabaseAuthClient>(client =>
        {
            client.BaseAddress = new Uri(supabaseUrl);
            client.DefaultRequestHeaders.Add("apikey", anonKey);
        });

        return services;
    }
}
