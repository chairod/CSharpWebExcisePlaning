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
    public class RentHouseRateConfigureController : Controller
    {
        // GET: RentHouseRateConfigure
        [CustomAuthorize(Roles = "Admin")]

        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_RENT_HOUSE_RATE_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_RENT_HOUSE_RATE_CONFIGURE;
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
                ViewBag.LevelID = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.LEVEL_NAME).
                    Select(e => new PersonnelLevelShortFieldProperty()
                    {
                        LEVEL_ID = e.LEVEL_ID,
                        LEVEL_NAME = e.LEVEL_NAME
                    }).ToList();
            }


            return View();
        }

        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.LevelID = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.LEVEL_ID).
                    Select(e => new PersonnelLevelShortFieldProperty()
                    {
                        LEVEL_ID = e.LEVEL_ID,
                        LEVEL_NAME = e.LEVEL_NAME
                    }).ToList();
            }

            return View();
        }


        [HttpPost]
        public ActionResult Retrieve(int? levelId, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_RENT_HOUSE_RATE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1));
                if (levelId != null)
                    expr = expr.Where(e => e.LEVEL_ID.Equals(levelId));
                expr = expr.OrderBy(e => e.LEVEL_NAME).ThenBy(e => e.FROM_SALARY)
                        .ThenBy(e => e.TO_SALARY);

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.RATE_ID,
                    e.LEVEL_ID,
                    e.FROM_SALARY,
                    e.TO_SALARY,
                    e.RATE_AMOUNT,
                    e.LEVEL_NAME
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public void SubmitDelete(short? rateId)
        {
            if (rateId == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_RENT_HOUSE_RATE_CONFIGUREs.Where(e => e.RATE_ID.Equals(rateId)).FirstOrDefault();
                if (expr == null || expr.ACTIVE.Equals(-1))
                    return;

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                expr.ACTIVE = -1;
                expr.DELETED_DATETIME = DateTime.Now;
                expr.DELETED_ID = userAuthorizeProfile.EmpId;
                db.SubmitChanges();
            };
        }


        [HttpPost]
        public ActionResult SubmitSave(RentHouseRateFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "errors", null }, { "errorText", null } };

            // ตรวจสอบการระบุค่า จากผู้ใช้งาน ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0)
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var expr = db.T_RENT_HOUSE_RATE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1) && e.RATE_ID.Equals(model.RateId)).FirstOrDefault();
                if (null == expr)
                {
                    // ตรวจสอบรายการคาบเกี่ยวของ ช่วงเงินเดือน
                    var overlapCount = db.T_RENT_HOUSE_RATE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)
                        && e.LEVEL_ID.Equals(model.LevelId)
                        && !((model.FromSalary < e.FROM_SALARY && model.ToSalary < e.FROM_SALARY) 
                            || (model.FromSalary > e.TO_SALARY && model.ToSalary > e.TO_SALARY)
                            )).Count();
                    if(overlapCount > 0)
                    {
                        res["errorText"] = "ช่วงของเงินเดือนที่ระบุคาบเกี่ยวกับรายการอื่น ของระดับบุคลากรที่ทำรายการไว้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    expr = new T_RENT_HOUSE_RATE_CONFIGURE()
                    {
                        LEVEL_ID = model.LevelId.Value,
                        FROM_SALARY = model.FromSalary.Value,
                        TO_SALARY = model.ToSalary.Value,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_RENT_HOUSE_RATE_CONFIGUREs.InsertOnSubmit(expr);
                }
                else
                {
                    expr.UPDATED_DATETIME = DateTime.Now;
                    expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                expr.RATE_AMOUNT = model.RateAmount.Value;
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class RentHouseRateFormMapper
        {
            public short? RateId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? LevelId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0.00", "999999999.99", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public decimal? FromSalary { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0.00", "999999999.99", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public decimal? ToSalary { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0.00", "999999999.99", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public decimal? RateAmount { get; set; }
        }
    }
}