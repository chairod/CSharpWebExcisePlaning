using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// วาดรายชื่อเมนูสำหรับเข้ารายงาน เฉพาะส่วนที่ได้รับสิทธิ์
    /// </summary>
    [CustomAuthorize]
    public class ReportMainPageController : Controller
    {
        // GET: ReportMainPage
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserMenuGroupProperty menuItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = "REPORT_MAIN_PAGE";
            ViewBag.Title = menuItem.GroupName;
            ViewBag.MenuGroups = userAuthorizeProfile.MenuGroups;
            ViewBag.PageName = menuItem.GroupName;
            ViewBag.PageDescription = "";
            ViewBag.LoginName = userAuthorizeProfile.EmpFullname;

            // กำหนด Breadcrump
            List<Breadcrump> breadcrumps = new List<Breadcrump>(2);
            breadcrumps.Add(new Breadcrump()
            {
                Text = menuIndexItem.MenuName,
                CssIcon = menuIndexItem.MenuIcon,
                ControllerName = menuIndexItem.RouteName,
                ActionName = menuIndexItem.ActionName
            });
            breadcrumps.Add(new Breadcrump()
            {
                Text = menuItem.GroupName,
                CssIcon = menuItem.GroupIcon,
                ControllerName = "ReportMainPage",
                ActionName = "GetForm"
            });
            ViewBag.Breadcrumps = breadcrumps;

            ViewBag.ReportMenuItems = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName))
                    .Select(e => e.UserMenus).ToList();

            return View("Report_Main_Page_Form");
        }
    }
}