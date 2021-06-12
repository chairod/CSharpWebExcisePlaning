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
    public class PersonnelLevelCompensationRateController : Controller
    {
        // GET: PersonnelLevelCompensationRate
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_PERSONNEL_LEVEL_COMPENSATION_RATE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_PERSONNEL_LEVEL_COMPENSATION_RATE;
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
                // ระดับ C ของบุคลากร
                ViewBag.LevelID = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.LEVEL_ID)
                    .Select(e => new PersonnelLevelShortFieldProperty()
                    {
                        LEVEL_ID = e.LEVEL_ID,
                        LEVEL_NAME = e.LEVEL_NAME
                    }).ToList();
                // ประเภทค่าตอบแทน
                ViewBag.CompensationType = db.T_COMPENSATION_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.COMPENSATION_TYPE_ID)
                    .Select(e => new CompensationTypeShortFieldProperty()
                    {
                        COMPENSATION_TYPE_ID = e.COMPENSATION_TYPE_ID,
                        COMPENSATION_TYPE_NAME = e.COMPENSATION_TYPE_NAME
                    }).ToList();
            }


            return View();
        }

        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ระดับ C ของบุคลากร
                ViewBag.LevelID = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.LEVEL_ID)
                    .Select(e => new PersonnelLevelShortFieldProperty()
                    {
                        LEVEL_ID = e.LEVEL_ID,
                        LEVEL_NAME = e.LEVEL_NAME
                    }).ToList();
                // ประเภทค่าตอบแทน
                ViewBag.CompensationType = db.T_COMPENSATION_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.COMPENSATION_TYPE_ID)
                    .Select(e => new CompensationTypeShortFieldProperty()
                    {
                        COMPENSATION_TYPE_ID = e.COMPENSATION_TYPE_ID,
                        COMPENSATION_TYPE_NAME = e.COMPENSATION_TYPE_NAME
                    }).ToList();
            }

            return View();
        }

        [HttpPost, Route("CompensationTypeID:int?,LevelID:int?, pageSize:int, pageIndex:int")]
        public ActionResult Retrieve(int? CompensationTypeID, int? LevelID, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_PERSONNEL_LEVEL_COMPENSATION_RATE_INFOMATIONs.Where(e => e.ACTIVE.Equals(1));
                if (CompensationTypeID != null)
                    expr = expr.Where(e => e.COMPENSATION_TYPE_ID.Equals(CompensationTypeID));
                if (LevelID != null)
                    expr = expr.Where(e => e.LEVEL_ID.Equals(LevelID));

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.COMPENSATION_TYPE_ID,
                    e.LEVEL_ID,
                    e.RATE_AMOUNT, // อัตราค่าตอบแทน
                    e.RATE_TYPE, // 1 = ต่อเดือน, 2 = ต่อ ชม., 3 = ต่อวัน
                    e.PERSONNEL_LEVEL_NAME, // ระดับ C ของบุคลากร
                    e.COMPENSATION_TYPE_NAME // ประเภทของค่าตอบแทน
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ค้นหาอัตราค่าตอบแทน ตามประเภทค่าตอบแทน และ ระดับ C ของบุคลากร
        /// </summary>
        /// <param name="compensationTypeId"></param>
        /// <param name="levelId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveCompensationRateBy(int? compensationTypeId, int? levelId)
        {
            if (null == compensationTypeId || null == levelId)
                return Json(null, JsonRequestBehavior.DenyGet);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.T_PERSONNEL_LEVEL_COMPENSATION_RATEs.Where(e => e.ACTIVE.Equals(1) && e.COMPENSATION_TYPE_ID.Equals(compensationTypeId) && e.LEVEL_ID.Equals(levelId))
                        .Select(e => e.RATE_AMOUNT).FirstOrDefault(), JsonRequestBehavior.DenyGet);
            }
        }

        [HttpPost, Route("CompensationTypeID:int?,LevelID:int?")]
        public void SubmitDelete(int? CompensationTypeID, int? LevelID)
        {
            if (CompensationTypeID == null || LevelID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_PERSONNEL_LEVEL_COMPENSATION_RATEs.Where(e => e.COMPENSATION_TYPE_ID.Equals(CompensationTypeID.Value) && e.LEVEL_ID.Equals(LevelID.Value)).FirstOrDefault();
                if (expr == null)
                    return;
                db.T_PERSONNEL_LEVEL_COMPENSATION_RATEs.DeleteOnSubmit(expr);
                db.SubmitChanges();
            };
        }


        [HttpPost]
        public ActionResult SubmitSave(PersonnelLevelCompensationRateFormMapper model)
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
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var expr = db.T_PERSONNEL_LEVEL_COMPENSATION_RATEs.Where(e => e.ACTIVE.Equals(1) && e.COMPENSATION_TYPE_ID.Equals(model.CompensationTypeId)
                                && e.LEVEL_ID.Equals(model.LevelId)).FirstOrDefault();
                if (null == expr)
                {
                    expr = new T_PERSONNEL_LEVEL_COMPENSATION_RATE()
                    {
                        COMPENSATION_TYPE_ID = model.CompensationTypeId.Value,
                        LEVEL_ID = model.LevelId.Value,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_PERSONNEL_LEVEL_COMPENSATION_RATEs.InsertOnSubmit(expr);
                }

                expr.RATE_AMOUNT = model.RateAmount.Value;
                expr.RATE_TYPE = model.RateType;
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class PersonnelLevelCompensationRateFormMapper
        {
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? CompensationTypeId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? LevelId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public decimal? RateAmount { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, 3, ErrorMessage = "ค่าที่ระบุได้ 1,2,3 เท่านั้น")]
            public short? RateType { get; set; }
        }
    }
}