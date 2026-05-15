using Azure.Monitor.OpenTelemetry.AspNetCore;
using FinanceTracker.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.UseUtcTimestamp = true;
});

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

await builder.Configuration.AddFinanceTrackerKeyVaultAsync(builder.Environment);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return new BadRequestObjectResult(new { error = string.Join(" ", errors) });
        };
    });
builder.Services.AddApiAuth(builder.Configuration);
builder.Services.AddSupabaseClient(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDashboardCaching(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddGraphQl(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGraphQL();

app.Run();
