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
    public class ExpensesGLCodeController : Controller
    {
        // GET: ExpensesMaster
        [CustomAuthorize(Roles = "Admin")]
      
        public ActionResult ExpensesGLCodeForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_EXPENSES_GLCODE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_EXPENSES_GLCODE;
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
                ViewBag.ExpensesItem = db.T_EXPENSES_ITEMs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.EXPENSES_NAME)
                    .Select(e => new ExpensesItemShortFieldProperty()
                    {

                        EXPENSES_ID = e.EXPENSES_ID,
                        EXPENSES_NAME = e.EXPENSES_NAME
                    }).ToList();
            }

            return View();
        }

        [HttpGet]
        public ActionResult GetModalResource()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.ExpensesItem = db.T_EXPENSES_ITEMs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.EXPENSES_NAME)
                    .Select(e => new ExpensesItemShortFieldProperty()
                    {

                        EXPENSES_ID = e.EXPENSES_ID,
                        EXPENSES_NAME = e.EXPENSES_NAME
                    }).ToList();
            }
            return View("_ExpensesGLCodeModal");
        }

        [HttpPost, Route("ExpensesID:int?,GLCode:string,GLText:string, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveForm(int? ExpensesID ,string GLCode,string GLText, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                var expr = db.V_EXPENSES_GLCODEs.Where(e => e.EXPENSES_ID >= 1);

                if (ExpensesID != null)
                {
                    expr = expr.Where(e => e.EXPENSES_ID.Equals(ExpensesID.Value));
                }

                if (!string.IsNullOrEmpty(GLCode))
                    expr = expr.Where(e => e.GLCODE.Contains(GLCode));

                if (!string.IsNullOrEmpty(GLText))
                    expr = expr.Where(e => e.GLCODE.Contains(GLText));

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalPages / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    EXPENSES_ID = e.EXPENSES_ID,
                    GLCODE = e.GLCODE,
                    GL_TEXT = e.GL_TEXT,
                    EXPENSES_NAME  = e.EXPENSES_NAME
                }).ToList();
            };

            

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        public class ExpensesGLCodeFormMapper
        {
            public ExpensesGLCodeFormMapper() { }

            public short? ExpensesID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string GLCode { get; set; }


            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string GLText { get; set; }


            public short? ExpensesID_Old{ get; set; }
            public string GLCode_Old { get; set; }
            public string GLText_Old { get; set; }

        }


        [HttpPost]
        public ActionResult SubmitSave(ExpensesGLCodeFormMapper model)
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

                T_EXPENSES_GLCODE Expr = null;

                if (model.ExpensesID_Old != null)
                    Expr = db.T_EXPENSES_GLCODEs.Where(e =>  e.EXPENSES_ID.Equals(model.ExpensesID_Old.Value ) &&
                                                         e.GLCODE.Equals(model.GLCode_Old) && e.GL_TEXT.Equals(model.GLText_Old) ).FirstOrDefault();

                if (null != Expr)
                {
                    db.T_EXPENSES_GLCODEs.DeleteOnSubmit(Expr);
                }
                             
                Expr = new T_EXPENSES_GLCODE()
                {
                    EXPENSES_ID = model.ExpensesID.Value,
                    GLCODE = model.GLCode,
                    GL_TEXT = model.GLText

                };
                db.T_EXPENSES_GLCODEs.InsertOnSubmit(Expr);

                db.SubmitChanges();

            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("ExpensesID:int?,GLCode:string,GLText:string")]
        public void SubmitDelete(int? ExpensesID,string GLCode,string GLText)
        {
            if (ExpensesID == null)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_EXPENSES_GLCODEs.Where(e => e.EXPENSES_ID.Equals(ExpensesID.Value) && e.GLCODE.Equals(GLCode) && e.GL_TEXT.Equals(GLText) ).FirstOrDefault();
               
                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_EXPENSES_GLCODEs.DeleteOnSubmit(Expr);
                db.SubmitChanges();

            };
        }


        
    }
}