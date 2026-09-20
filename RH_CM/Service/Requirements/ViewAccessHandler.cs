//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc.Filters;
//using RH_CM.Service.Requirements;
//using System.Threading.Tasks;

//public class ViewAccessHandler : AuthorizationHandler<ViewAccessRequirement>
//{
//    private readonly IAccessService _accessService;

//    public ViewAccessHandler(IAccessService accessService)
//    {
//        _accessService = accessService;
//    }

//    protected override async Task HandleRequirementAsync(
//        AuthorizationHandlerContext context,
//        ViewAccessRequirement requirement)
//    {
//        if (context.Resource is AuthorizationFilterContext mvcContext)
//        {
//            string controller = mvcContext.RouteData.Values["controller"]?.ToString() ?? "";
//            string action = mvcContext.RouteData.Values["action"]?.ToString() ?? "";
//            string userId = context.User.Identity.Name;

//            bool hasAccess = await _accessService.HasAccessAsync(userId, controller, action);

//            if (hasAccess)
//                context.Succeed(requirement);
//            else

//            context.Fail();
//        }
//        else
//        {
//            Console.WriteLine($"Tipo de Resource: {context.Resource?.GetType().Name}");
//            context.Fail();
//        }
//    }
//}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using RH_CM.Service.Requirements;
using System.Threading.Tasks;

public class ViewAccessHandler : AuthorizationHandler<ViewAccessRequirement>
{
    private readonly IAccessService _accessService;

    public ViewAccessHandler(IAccessService accessService)
    {
        _accessService = accessService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ViewAccessRequirement requirement)
    {
        string controller = "";
        string action = "";
        string userId = context.User.Identity?.Name ?? "";

        // Try to get controller/action depending on the resource type
        switch (context.Resource)
        {
            case AuthorizationFilterContext mvcContext:
                controller = mvcContext.RouteData.Values["controller"]?.ToString() ?? "";
                action = mvcContext.RouteData.Values["action"]?.ToString() ?? "";
                break;

            case DefaultHttpContext httpContext:
                controller = httpContext.Request.RouteValues["controller"]?.ToString() ?? "";
                action = httpContext.Request.RouteValues["action"]?.ToString() ?? "";
                break;

            default:
                // Can't determine the view → deny access
                context.Fail();
                return;
        }

        if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
        {
            context.Fail();
            return;
        }

        // Administrador always has access. Without this, a brand-new controller/action with
        // no CT_PERMISSION row yet would lock out even the highest-trust role, including the
        // person who needs to go grant that very permission.
        if (context.User.IsInRole("Administrador"))
        {
            context.Succeed(requirement);
            return;
        }

        bool hasAccess = await _accessService.HasAccessAsync(userId, controller, action);

        if (hasAccess)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
