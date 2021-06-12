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
    /// กำหนดข้อมูลตั้งต้น ประเภทบุคลากร T_PERSONNEL_TYPE
    /// อนุญาตให้เข้าได้เฉพาะ Admin Role
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class PersonnelTypeController : Controller
    {
        // GET: PersonnelType
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_PERSONNEL_TYPE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_PERSONNEL_TYPE;
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

            return View("Personnel_Type_Form");
        }

        [HttpPost, Route("personTypeName:string, pageIndex:int, pageSize:int")]
        public ActionResult RetrievePersonType(string personTypeName, int pageIndex, int pageSize)
        {
            // จัดเตรียมข้อมูล Pagging สำหรับตอบกลับ
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(personTypeName))
                    expr = expr.Where(e => e.ITEM_TEXT.Contains(personTypeName));

                var entites = expr.OrderBy(e => e.SORT_INDEX).ThenBy(e => e.PERSON_TYPE_ID)
                    .Select(e => new
                    {
                        PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                        LEAVE_OVERFLOW_ID = e.LEAVE_OVERFLOW_ID,
                        ITEM_TEXT = e.ITEM_TEXT,
                        ITEM_DESCRIPTION = e.ITEM_DESCRIPTION,
                        CREATED_DATETIME = e.CREATED_DATETIME,
                        UPDATED_DATETIME = e.UPDATED_DATETIME,
                        COUNT_USED = db.T_PERSONNELs.Count(p => p.ACTIVE.Equals(1) && p.PERSON_TYPE_ID.Equals(e.PERSON_TYPE_ID))
                    }).ToList();

                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = entites.Skip(offset).Take(pageSize);
                pagging.totalRecords = entites.Count;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
            }


            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpGet]
        public ActionResult GetResource()
        {
            return View("_Personnel_Type_Modal_Form");
        }

        [HttpPost, Route("personTypeId:int?")]
        public void SubmitDelete(int? personTypeId)
        {
            if (!personTypeId.HasValue)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ค้นหาตำแหน่งงาน ที่ Active อยู่และตรงกับ รหัสตำแหน่งงานที่ร้องขอยกเลิก
                var entity = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1) && e.PERSON_TYPE_ID.Equals(personTypeId.Value)).FirstOrDefault();
                if (entity == null)
                    return;

                // Profile ผู้ใช้งานที่ออนไลน์
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                entity.ACTIVE = -1;
                entity.UPDATED_DATETIME = DateTime.Now;
                entity.UPDATED_ID = userAuthorizeProfile.EmpId;

                db.SubmitChanges();
            }
        }

        [HttpPost]
        public ActionResult SubmitSave(PersonTypeFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) {
                { "errors", null },
                { "errorText", null }
            };

            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0)
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                T_PERSONNEL_TYPE expr = db.T_PERSONNEL_TYPEs.Where(e => e.PERSON_TYPE_ID.Equals(model.PersonTypeId)).FirstOrDefault();
                if (expr == null)
                {
                    expr = new T_PERSONNEL_TYPE();
                    db.T_PERSONNEL_TYPEs.InsertOnSubmit(expr);
                    expr.CREATED_DATETIME = DateTime.Now;
                    expr.USER_ID = userAuthorizeProfile.EmpId;
                    expr.ACTIVE = 1;
                }
                else
                {
                    if (expr.ACTIVE.Equals(-1))
                    {
                        res["errorText"] = "ประเภทบุคลากรนี้ยกเลิกไปแล้ว";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    expr.UPDATED_DATETIME = DateTime.Now;
                    expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                expr.ITEM_TEXT = model.PersonTypeName;
                expr.ITEM_DESCRIPTION = model.PersonTypeDescription;

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class PersonTypeFormMapper
        {
            public int? PersonTypeId { get; set; }

            /// <summary>
            /// ชื่อตำแหน่งงาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(150, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string PersonTypeName { get; set; }

            /// <summary>
            /// ชื่อตำแหน่งงาน
            /// </summary>
            [MaxLength(180, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string PersonTypeDescription { get; set; }
        }
    }
}