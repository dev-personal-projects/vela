using Vela.Api.Functions.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;

namespace Vela.Api.Functions;

public sealed class HealthFunction
{
    [Function("Health")]
    [OpenApiOperation(operationId: "Health", tags: [ApiDocumentation.HealthTag], Summary = "Checks API health.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(HealthResponse), Description = "The API is healthy.")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Health)]
        HttpRequestData req) =>
        FunctionHttpResults.Ok(req, new HealthResponse("healthy", "Finance Tracker API", DateTime.UtcNow));
}
