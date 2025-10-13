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

        // Intentar obtener controller/action dependiendo del tipo
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
                // No se puede determinar la vista → negar acceso
                context.Fail();
                return;
        }

        if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
        {
            context.Fail();
            return;
        }

        bool hasAccess = await _accessService.HasAccessAsync(userId, controller, action);

        if (hasAccess)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
