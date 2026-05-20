using Vela.Api.Functions.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Vela.Api.Functions;

public sealed class HealthFunction
{
    [Function("Health")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Health)]
        HttpRequestData req) =>
        FunctionHttpResults.Ok(req, new
        {
            status = "healthy",
            service = "Finance Tracker API",
            timestamp = DateTime.UtcNow
        });
}
