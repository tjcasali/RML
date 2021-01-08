using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;

namespace RML
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "DisplayLeague",
                "Sleeper/DisplayLeague/{leagueID}",
                new { controller = "Sleeper", action = "DisplayLeague" });

            routes.MapRoute(
                "TeamBreakdown",
                "Sleeper/TeamBreakdown/{leagueID}/{name}",
                new { controller = "Sleeper", action = "TeamBreakdown" });            

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
