using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ExcisePlaning
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Call Route Pattern
            // https://stackoverflow.com/questions/22396905/httprouteurl-and-attribute-routing-in-webapi-2

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Authorize", action = "LoginForm", id = UrlParameter.Optional }
            );
        }
    }
}
