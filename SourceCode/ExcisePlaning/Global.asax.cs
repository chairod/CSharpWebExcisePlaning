using ExcisePlaning.App_Start;
using ExcisePlaning.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebGrease.Configuration;

namespace ExcisePlaning
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // ไม่ต้องทำ Minify
            BundleTable.EnableOptimizations = false;

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // ตรวจจับข้อผิดพลาดในระบบ และเขียนลงไฟล์
            GlobalFilters.Filters.Add(new CustomExceptionHandle());
        }
    }
}
