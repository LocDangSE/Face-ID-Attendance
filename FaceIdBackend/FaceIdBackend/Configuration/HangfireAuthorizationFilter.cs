using Hangfire.Dashboard;

namespace FaceIdBackend;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// In production, implement proper authentication
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For development: Allow all requests
        // In production: Implement proper authentication/authorization
        // Example: Check if user is authenticated and has admin role
        // var httpContext = context.GetHttpContext();
        // return httpContext.User.Identity?.IsAuthenticated == true 
        //     && httpContext.User.IsInRole("Admin");

        return true; // Allow access for development
    }
}
