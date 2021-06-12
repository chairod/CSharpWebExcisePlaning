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
    /// <summary>
    /// กำหนดกลุ่มของหมวดรายการค่าใช้จ่าย
    /// เช่น เงินเดือน, ค่าจ้างประจำ จะอยู่ในกลุ่มหมวดค่าใช้จ่าย เงินเดือนและค่าจ้างประจำ เป็นต้น
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class ExpensesMasterController : Controller
    {
        // GET: ExpensesMaster
        public ActionResult ExpensesMasterForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_EXPENSES_MASTER);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_EXPENSES_MASTER;
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
            return View("_ExpensesMasterModal");
        }

        [HttpPost]
        public ActionResult RetrieveForm(string masterName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_EXPENSES_MASTERs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(masterName))
                    expr = expr.Where(e => e.EXPENSES_MASTER_NAME.Contains(masterName));

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalPages / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    EXPENSES_MASTER_ID = e.EXPENSES_MASTER_ID,
                    EXPENSES_MASTER_NAME = e.EXPENSES_MASTER_NAME,
                    ACTIVE = e.ACTIVE
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public void SubmitDelete(int? masterId)
        {
            if (masterId == null)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_EXPENSES_MASTERs.Where(e => e.EXPENSES_MASTER_ID.Equals(masterId.Value)).FirstOrDefault();
                if (expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                expr.UPDATED_DATETIME = DateTime.Now;
                expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                expr.ACTIVE = -1;
                db.SubmitChanges();
            };
        }

        [HttpPost]
        public ActionResult SubmitSave(ExpensesMasterFormMapper model)
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

                var expr = db.T_EXPENSES_MASTERs.Where(e => e.ACTIVE.Equals(1) && e.EXPENSES_MASTER_ID.Equals(model.MasterId)).FirstOrDefault();
                if (null == expr)
                {
                    expr = new T_EXPENSES_MASTER()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    };
                    db.T_EXPENSES_MASTERs.InsertOnSubmit(expr);
                }
                else
                {
                    expr.UPDATED_DATETIME = DateTime.Now;
                    expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                expr.EXPENSES_MASTER_NAME = model.MasterName;
                expr.ACTIVE = 1;
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class ExpensesMasterFormMapper
        {
            public ExpensesMasterFormMapper() { }

            public short? MasterId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string MasterName { get; set; }

        }
    }
}