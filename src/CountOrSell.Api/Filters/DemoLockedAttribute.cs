using CountOrSell.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CountOrSell.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class DemoLockedAttribute : TypeFilterAttribute
{
    public DemoLockedAttribute() : base(typeof(DemoLockedFilter)) { }
}

public class DemoLockedFilter : IActionFilter
{
    private readonly IDemoModeService _demoModeService;

    public DemoLockedFilter(IDemoModeService demoModeService)
    {
        _demoModeService = demoModeService;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (_demoModeService.IsDemo)
        {
            context.Result = new ObjectResult(
                new { error = "This action is not available in demo mode." })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
