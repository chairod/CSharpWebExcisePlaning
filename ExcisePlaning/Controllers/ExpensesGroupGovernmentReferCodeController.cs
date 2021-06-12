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
    public class ExpensesGroupGovernmentReferCodeController : Controller
    {
        // GET: ExpensesGroupGovernmentReferCode
        public ActionResult ExpensesGroupGovernmentReferCodeForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_EXPENSES_GROUP_GOVERNMENT_REFER_CODE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_EXPENSES_GROUP_GOVERNMENT_REFER_CODE;
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
                ViewBag.ExpensesGroup = db.T_EXPENSES_GROUPs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.EXPENSES_GROUP_NAME)
                    .Select(e => new ExpensesGroupShortFieldProperty()
                    {
                        
                        EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                        EXPENSES_GROUP_NAME = e.EXPENSES_GROUP_NAME
                    }).ToList();
            }

           
            return View();
        }

        [HttpGet]
        public ActionResult GetModalResource()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.ExpensesGroup = db.T_EXPENSES_GROUPs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.EXPENSES_GROUP_NAME)
                    .Select(e => new ExpensesGroupShortFieldProperty()
                    {

                        EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                        EXPENSES_GROUP_NAME = e.EXPENSES_GROUP_NAME
                    }).ToList();
            }
            return View("_ExpensesGroupGovernmentReferCodeModal");
        }

        [HttpPost, Route("ExpensesGroupID:int?,ReferCode:string,Yr:int?,string ExpensesGroupName, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveForm(int? ExpensesGroupID, string ReferCode, int? Yr, string ExpensesGroupName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                var expr = db.V_GET_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.Where(e => e.EXPENSES_GROUP_ID >= 1);

                if (ExpensesGroupID != null)
                {
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(ExpensesGroupID.Value));

                }
                else if (!string.IsNullOrEmpty(ReferCode))
                {
                    expr = expr.Where(e => e.GOVERNMENT_REFER_CODE.Contains(ReferCode));
                }
                else if (Yr != null)
                {
                    expr = expr.Where(e => e.YR.Equals(Yr.Value));
                }


                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalPages / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {

                    EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                    GOVERNMENT_REFER_CODE = e.GOVERNMENT_REFER_CODE,
                    YR = e.YR,
                    EXPENSES_GROUP_NAME = e.EXPENSES_GROUP_NAME
                }).ToList();




            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult SubmitSave(ExpensesGroupGovernmentReferCodeFormMapper model)
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

                T_EXPENSES_GROUP_GOVERNMENT_REFER_CODE Expr = null;
                if (model.ExpensesGroupID != null)
                    Expr = db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.Where(e => e.EXPENSES_GROUP_ID.Equals(model.ExpensesGroupID_Old)
                                        && e.YR.Equals(model.Yr_Old.Value)
                                        && e.GOVERNMENT_REFER_CODE.Equals(model.ReferCode_Old)).FirstOrDefault();



                if (Expr != null)
                {
                    db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.DeleteOnSubmit(Expr);
                    db.SubmitChanges();
                }

                Expr = new T_EXPENSES_GROUP_GOVERNMENT_REFER_CODE()
                {
                    EXPENSES_GROUP_ID = model.ExpensesGroupID.Value,
                    GOVERNMENT_REFER_CODE = model.ReferCode,
                    YR = model.Yr.Value

                };
                db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.InsertOnSubmit(Expr);


                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("ExpensesGroupID:int?,ReferCode:string,Yr:int?")]
        public void SubmitDelete(int? ExpensesGroupID, string ReferCode, int? Yr)
        {
            if (ExpensesGroupID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.Where(e => e.EXPENSES_GROUP_ID.Equals(ExpensesGroupID.Value)
                                                            && e.YR.Equals(Yr.Value) && e.GOVERNMENT_REFER_CODE.Equals(ReferCode)).FirstOrDefault();

                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.DeleteOnSubmit(Expr);
                db.SubmitChanges();


            };
        }


        public class ExpensesGroupGovernmentReferCodeFormMapper
        {
            public ExpensesGroupGovernmentReferCodeFormMapper() { }

            public short? ExpensesGroupID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ReferCode { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? Yr { get; set; }

            public short? ExpensesGroupID_Old { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ReferCode_Old { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? Yr_Old { get; set; }

        }
    }
}