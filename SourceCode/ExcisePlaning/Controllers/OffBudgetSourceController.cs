using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class OffBudgetSourceController : Controller
    {
        // GET: OffBudgetSource

        public ActionResult OffBudgetSourceForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_OFF_BUDGET_SOURCE_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_OFF_BUDGET_SOURCE_MENU;
            ViewBag.Title = menuItem.MenuName;
            ViewBag.MenuGroups = userAuthorizeProfile.MenuGroups;
            ViewBag.PageName = menuItem.MenuName;
            ViewBag.PageDescription = menuItem.MenuDescription;
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
                Text = menuItem.MenuName,
                CssIcon = menuItem.MenuIcon,
                ControllerName = menuItem.RouteName,
                ActionName = menuItem.ActionName
            });
            ViewBag.Breadcrumps = breadcrumps;
            return View();
        }

        [HttpGet]
        public ActionResult GetModalResource()
        {
            return View("_OffBudgetSourceModal");
        }

        [HttpPost, Route("BudgetSourceName:string, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveForm(string BudgetSourceName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
             
                var expr = db.T_OFF_BUDGET_SOURCEs.Where(e => e.OFF_BUDGET_SOURCE_ID != 0 );
                if (!string.IsNullOrEmpty(BudgetSourceName))
                    expr = expr.Where(e => e.OFF_BUDGET_SOURCE_NAME.Contains(BudgetSourceName));

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalPages / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    OFF_BUDGET_SOURCE_ID = e.OFF_BUDGET_SOURCE_ID,
                    OFF_BUDGET_SOURCE_NAME = e.OFF_BUDGET_SOURCE_NAME
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult SubmitSave(OffBudgetSourceFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "errors", null } };

            // ตรวจสอบการระบุค่า จากผู้ใช้งาน ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0)
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                T_OFF_BUDGET_SOURCE BUDGETExpr = null;
                if (model.BudgetSourceName != "")
                    BUDGETExpr = db.T_OFF_BUDGET_SOURCEs.Where(e => e.OFF_BUDGET_SOURCE_ID.Equals(model.BudgetSourceID)).FirstOrDefault();

                if (BUDGETExpr == null)
                {
                    BUDGETExpr = new T_OFF_BUDGET_SOURCE();
                    db.T_OFF_BUDGET_SOURCEs.InsertOnSubmit(BUDGETExpr);
                }

                BUDGETExpr.OFF_BUDGET_SOURCE_NAME = model.BudgetSourceName;

               

                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("Budget_SourceID:int?")]
        public void SubmitDelete(int? Budget_SourceID)
        {
            if (Budget_SourceID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var BudgetExpr = db.T_OFF_BUDGET_SOURCEs.Where(e =>  e.OFF_BUDGET_SOURCE_ID.Equals(Budget_SourceID.Value)).FirstOrDefault();
                if (BudgetExpr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_OFF_BUDGET_SOURCEs.DeleteOnSubmit(BudgetExpr);
                db.SubmitChanges();


            };
        }
       

        public class OffBudgetSourceFormMapper
        {
            public OffBudgetSourceFormMapper() { }

            public short? BudgetSourceID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string BudgetSourceName { get; set; }

        }
    }
}