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
    public class BudgetTypeGovernmentReferCodeController : Controller
    {
        // GET: BudgetTypeGovernmentReferCode
        public ActionResult BudgetTypeGovernmentReferCodeForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_TYPE_GOVERNMENT_REFER_CODE_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_TYPE_GOVERNMENT_REFER_CODE_MENU;
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

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.BudgetType = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.BUDGET_TYPE_NAME)
                    .Select(e => new BudgetTypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();
            }

                return View();
        }

        [HttpGet]
        public ActionResult GetModalResource()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.BudgetType = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.BUDGET_TYPE_NAME)
                    .Select(e => new BudgetTypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();
            }
            return View("_BudgetTypeGovernmentReferCodeModal");
        }

        [HttpPost, Route("GovernmentID:int?,budgetTypeID:int?,GovernmentCode:string,Yr:int?,string budgetTypeName, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveForm(int? GovernmentID, int? budgetTypeID, string GovernmentCode, int? Yr, string budgetTypeName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                var expr = db.V_GET_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.Where(e => e.BUDGET_TYPE_ID >= 1);

                if (budgetTypeID != null)
                {
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeID.Value));

                }
                else if (!string.IsNullOrEmpty(GovernmentCode))
                {
                    expr = expr.Where(e => e.GOVERNMENT_REFER_CODE.Contains(GovernmentCode));
                }
                else if (Yr != null)
                {
                    expr = expr.Where(e => e.YR.Equals(Yr.Value));
                }
                expr.OrderBy(e => e.BUDGET_TYPE_ID);

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalPages / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {

                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME,
                    GOVERNMENT_REFER_CODE = e.GOVERNMENT_REFER_CODE,
                    YR = e.YR
                }).ToList();




            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult SubmitSave(GOVReferFormMapper model)
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

                T_BUDGET_TYPE_GOVERNMENT_REFER_CODE GOVReferExpr = null;
                if (model.BudgetTypeID != null)
                    GOVReferExpr = db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.Where(e => e.GOVERNMENT_REFER_CODE.Equals(model.GovernmentCode_Old)
                                        && e.YR.Equals(model.Yr_Old.Value)
                                        && e.BUDGET_TYPE_ID.Equals(model.BudgetTypeID_Old.Value)).FirstOrDefault();



                if (GOVReferExpr != null)
                {
                    db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.DeleteOnSubmit(GOVReferExpr);
                    db.SubmitChanges();
                }

                GOVReferExpr = new T_BUDGET_TYPE_GOVERNMENT_REFER_CODE()
                {
                    BUDGET_TYPE_ID = model.BudgetTypeID.Value,
                    GOVERNMENT_REFER_CODE = model.GovernmentCode,
                    YR = model.Yr.Value
                };
                db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.InsertOnSubmit(GOVReferExpr);


                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("budgetTypeID:int?,GovernmentCode:string,Yr:int?")]
        public void SubmitDelete(int? budgetTypeID, string GovernmentCode, int? Yr)
        {
            if (budgetTypeID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var GOVReferExpr = db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeID.Value)
                                                            && e.YR.Equals(Yr.Value) && e.GOVERNMENT_REFER_CODE.Equals(GovernmentCode)).FirstOrDefault();

                if (GOVReferExpr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.DeleteOnSubmit(GOVReferExpr);
                db.SubmitChanges();


            };
        }


        public class GOVReferFormMapper
        {
            public GOVReferFormMapper() { }

            public short? BudgetTypeID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string GovernmentCode { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? Yr { get; set; }

            public short? BudgetTypeID_Old { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string GovernmentCode_Old { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? Yr_Old { get; set; }

        }

        

        
    }
}