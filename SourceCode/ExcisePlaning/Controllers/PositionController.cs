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
    /// กำหนดตำแหน่งในระบบ ระบุสิทธิ์ต่างๆ อาทิเช่น สิทธิ์การอนุมัติการลา OT และอื่นๆ
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class PositionController : Controller
    {
        // GET: Position
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_POSITION_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_POSITION_CONFIGURE;
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
                ViewBag.PersonTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1))
                        .OrderBy(e => e.PERSON_TYPE_ID)
                        .Select(e => new PersonnelTypeShortFieldProperty()
                        {
                            PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                            PERSON_TYPE_NAME = e.ITEM_TEXT
                        }).ToList();
            }

            return View();
        }

        [HttpPost]
        public ActionResult RetrieveAuthorizeExpenses(int? positionId)
        {
            if (null == positionId)
                return Json(null, JsonRequestBehavior.DenyGet);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.T_POSITION_AUTHORIZE_EXPENSEs.Where(e => e.POSITION_ID.Equals(positionId)).Select(e => e.EXPENSES_ID).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        [HttpPost]
        public ActionResult Retrieve(int? personTypeId, string positionName, int pageIndex, int pageSize)
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
                var expr = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1));
                if (null != personTypeId)
                    expr = expr.Where(e => e.PERSON_TYPE_ID != null && e.PERSON_TYPE_ID.Equals(personTypeId));
                if (!string.IsNullOrEmpty(positionName))
                    expr = expr.Where(e => e.POSITION_NAME.Contains(positionName));

                var entites = expr.OrderBy(e => e.SORT_INDEX);

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = entites.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = entites.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.POSITION_ID,
                    e.POSITION_NAME,
                    e.CREATED_DATETIME,
                    e.UPDATED_DATETIME,
                    e.ITEM_DESCRIPTION,
                    e.EXPENSES_FLAG,
                    COUNT_USED = db.T_PERSONNELs.Count(p => p.ACTIVE.Equals(1) && p.POSITION_ID.Equals(e.POSITION_ID)),
                    e.PERSON_TYPE_ID,
                    PERSON_TYPE_NAME = db.T_PERSONNEL_TYPEs.Where(s => s.PERSON_TYPE_ID.Equals(e.PERSON_TYPE_ID))
                        .Select(s => s.ITEM_TEXT).FirstOrDefault()
                }).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.PersonTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1))
                        .OrderBy(e => e.PERSON_TYPE_ID)
                        .Select(e => new PersonnelTypeShortFieldProperty()
                        {
                            PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                            PERSON_TYPE_NAME = e.ITEM_TEXT
                        }).ToList();
            }
            return View();
        }

        [HttpPost, Route("positionId:int?")]
        public void SubmitDelete(int? positionId)
        {
            if (null == positionId)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ค้นหาตำแหน่งงาน ที่ Active อยู่และตรงกับ รหัสตำแหน่งงานที่ร้องขอยกเลิก
                var entity = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1) && e.POSITION_ID.Equals(positionId)).FirstOrDefault();
                if (entity == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                entity.ACTIVE = -1;
                entity.UPDATED_DATETIME = DateTime.Now;
                entity.UPDATED_ID = userAuthorizeProfile.EmpId;

                db.SubmitChanges();
            }
        }


        [HttpPost]
        public ActionResult SubmitSave(PositionFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) {
                { "errors", null }
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
                T_POSITION entity = null;
                if (model.PositionId != null)
                    entity = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1) && e.POSITION_ID.Equals(model.PositionId)).FirstOrDefault();
                if (entity == null)
                {
                    entity = new T_POSITION()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_POSITIONs.InsertOnSubmit(entity);
                }
                else
                {
                    entity.UPDATED_DATETIME = DateTime.Now;
                    entity.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                entity.PERSON_TYPE_ID = model.PersonTypeId;
                entity.POSITION_NAME = model.PositionName;
                // 1 = ใช้ได้ทุกรายการ คชจ., 2 = ใช้ได้เฉพาะบางรายการ คชจ.
                entity.EXPENSES_FLAG = Convert.ToInt16(model.ExpensesIds == null || model.ExpensesIds.Count == 0 ? 1 : 2);
                entity.ITEM_DESCRIPTION = model.RemarkText;
                db.SubmitChanges();

                // บันทึกการให้สิทธิ์ รายการค่าใช้จ่ายที่สามารถใช้ตำแหน่งงานนี้ได้
                db.T_POSITION_AUTHORIZE_EXPENSEs.DeleteAllOnSubmit(db.T_POSITION_AUTHORIZE_EXPENSEs.Where(e => e.POSITION_ID.Equals(entity.POSITION_ID)));
                if (null != model.ExpensesIds)
                    model.ExpensesIds.ForEach(expensesId =>
                    {
                        db.T_POSITION_AUTHORIZE_EXPENSEs.InsertOnSubmit(new T_POSITION_AUTHORIZE_EXPENSE()
                        {
                            POSITION_ID = entity.POSITION_ID,
                            EXPENSES_ID = expensesId
                        });
                    });
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class PositionFormMapper
        {
            /// <summary>
            /// ประเภทบุคลากร
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? PersonTypeId { get; set; }

            public int? PositionId { get; set; }

            /// <summary>
            /// ชื่อตำแหน่งงาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(200, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string PositionName { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่สามารถใช้ ตำแหน่งงานนี้ได้
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุรายการค่าใช้จ่ายที่สามารถใช้ตำแหน่งงานนี้ได้")]
            public List<int> ExpensesIds { get; set; }

            /// <summary>
            /// หมายเหตุอื่นๆเพิ่มเติม
            /// </summary>
            [MaxLength(150, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}