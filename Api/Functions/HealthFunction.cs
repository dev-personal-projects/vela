using FinanceTracker.Api.Functions.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FinanceTracker.Api.Functions;

public sealed class HealthFunction
{
    [Function("Health")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Health)]
        HttpRequestData req) =>
        new OkObjectResult(new
        {
            status = "healthy",
            service = "Finance Tracker API",
            timestamp = DateTime.UtcNow
        });
}
