using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// หน่วยนับ
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class UnitController : Controller
    {
        // GET: Unit
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_UNIT);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_UNIT;
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

        [HttpPost]
        public ActionResult Retrieve(string unitName, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_UNITs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(unitName))
                    expr = expr.Where(e => e.UNIT_TEXT.Contains(unitName));

                expr = expr.OrderBy(e => e.UNIT_ID);

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        [HttpGet]
        public ActionResult GetModalForm()
        {
            return View();
        }


        [HttpPost]
        public void SubmitDelete(int? unitId)
        {
            if (null == unitId)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_UNITs.Where(e => e.UNIT_ID.Equals(unitId)).FirstOrDefault();
                if (null == expr)
                    return;

                expr.ACTIVE = -1;
                db.SubmitChanges();
            }
        }

        [HttpPost]
        public ActionResult SubmitSave(UnitFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "errorText", null }, { "errors", null } };

            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprUnit = db.T_UNITs.Where(e => e.ACTIVE.Equals(1) && e.UNIT_ID.Equals(model.UnitId)).FirstOrDefault();
                 if (null == exprUnit)
                {
                    exprUnit = new T_UNIT();
                    exprUnit.ACTIVE = 1;
                    exprUnit.UNIT_SHARED = 9; // ใช้ทั้งระบบ
                    db.T_UNITs.InsertOnSubmit(exprUnit);
                }

                exprUnit.UNIT_TEXT = model.UnitName;
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class UnitFormMapper
        {
            public int? UnitId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string UnitName { get; set; }
        }
    }
}