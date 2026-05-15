using FinanceTracker.Api.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.UseUtcTimestamp = true;
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

await builder.Configuration.AddFinanceTrackerKeyVaultAsync(builder.Environment);

builder.Services.AddApiAuth(builder.Configuration);
builder.Services.AddSupabaseClient(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDashboardCaching(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddServices();

builder.Build().Run();
