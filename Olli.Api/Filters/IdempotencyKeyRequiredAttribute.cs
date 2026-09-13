using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Olli.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotencyKeyRequiredAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.ContainsKey("IdempotencyKey"))
            context.Result = new BadRequestObjectResult("Header IdempotencyKey e obrigatorio para operacoes POST.");
    }
}
