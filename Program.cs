using FinanceTracker.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSupabaseAuth(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddGraphQl();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();

app.Run();
