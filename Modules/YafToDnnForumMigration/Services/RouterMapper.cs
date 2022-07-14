using DotNetNuke.Web.Api;

namespace Upendo.Modules.YafToDnnForumMigration.Services
{
    public class RouterMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            mapRouteManager.MapHttpRoute("YafToDnnForumMigration", "default", "{controller}/{action}", new { }, new[] { "Upendo.Modules.YafToDnnForumMigration.Services" });
        }
    }
}