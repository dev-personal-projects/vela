using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Vela.Api.Functions.Support;

public sealed class ApiObjectResult(object? value, int statusCode, string requestId) : ObjectResult(value)
{
    public string RequestId { get; } = requestId;

    public override Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.Headers["X-Request-Id"] = RequestId;
        StatusCode = statusCode;
        return base.ExecuteResultAsync(context);
    }
}

public sealed class ApiContentResult(string content, int statusCode, string requestId) : ContentResult
{
    public override Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.Headers["X-Request-Id"] = requestId;
        Content = content;
        ContentType = "application/json; charset=utf-8";
        StatusCode = statusCode;
        return base.ExecuteResultAsync(context);
    }
}

public sealed class ApiStatusCodeResult(int statusCode, string requestId) : StatusCodeResult(statusCode)
{
    public override Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.Headers["X-Request-Id"] = requestId;
        return base.ExecuteResultAsync(context);
    }
}
