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
    public class PlanConfigureController : Controller
    {
        // GET: PlanConfigure
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_PLAN_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_PLAN_CONFIGURE;
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
        public ActionResult GetModalForm()
        {
            return View();
        }

        [HttpPost, Route("PlanCode:string,PlanName:string, pageSize:int, pageIndex:int")]
        public ActionResult Retrieve(string PlanCode, string PlanName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(PlanCode))
                    expr = expr.Where(e => e.PLAN_CODE.Contains(PlanCode));

                if (!string.IsNullOrEmpty(PlanName))
                    expr = expr.Where(e => e.PLAN_NAME.Contains(PlanName));

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.PLAN_ID,
                    e.PLAN_CODE,
                    e.PLAN_NAME,
                    e.ORDER_SEQ,
                    e.SHORT_NAME
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("PlanID:int?")]
        public void SubmitDelete(int? PlanID)
        {
            if (PlanID == null)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_PLAN_CONFIGUREs.Where(e => e.PLAN_ID.Equals(PlanID.Value)).FirstOrDefault();
                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                Expr.UPDATED_DATETIME = DateTime.Now;
                Expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                Expr.ACTIVE = -1;
                db.SubmitChanges();
            };
        }

        [HttpPost]
        public ActionResult SubmitSave(PlanConfugureFormMapper model)
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

                T_PLAN_CONFIGURE Expr = null;

                if (model.PlanID != null)
                    Expr = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1) && e.PLAN_ID.Equals(model.PlanID)).FirstOrDefault();

                if (null == Expr)
                {
                    Expr = new T_PLAN_CONFIGURE()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_PLAN_CONFIGUREs.InsertOnSubmit(Expr);
                }
                else
                {
                    Expr.UPDATED_DATETIME = DateTime.Now;
                    Expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }
                Expr.PLAN_CODE = model.PlanCode;
                Expr.PLAN_NAME = model.PlanName;
                Expr.ORDER_SEQ = model.OrderSeq.Value;
                Expr.SHORT_NAME = model.ShortName;

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class PlanConfugureFormMapper
        {
            public PlanConfugureFormMapper() { }

            public short? PlanID { get; set; }

            public string PlanCode { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียงข้อมูล
            /// </summary> 
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? OrderSeq { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"),MaxLength(200, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string PlanName { get; set; }

            [MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ShortName { get; set; }
        }
    }
}