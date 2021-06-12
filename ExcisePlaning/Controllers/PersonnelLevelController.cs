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
    /// ระดับ C ของบุคลากร
    /// อนุญาตให้เข้าได้เฉพาะ Admin Role
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class PersonnelLevelController : Controller
    {
        // GET: PersonnelLevel
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_PERSONNEL_LEVEL);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_PERSONNEL_LEVEL;
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
        public ActionResult RetrieveAuthorizeExpenses(int? levelId)
        {
            if (null == levelId)
                return Json(null, JsonRequestBehavior.DenyGet);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.T_PERSONNEL_LEVEL_AUTHORIZE_EXPENSEs.Where(e => e.LEVEL_ID.Equals(levelId)).Select(e => e.EXPENSES_ID).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        [HttpPost]
        public ActionResult Retrieve(string levelName, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(levelName))
                    expr = expr.Where(e => e.LEVEL_NAME.Contains(levelName));

                expr = expr.OrderBy(e => e.LEVEL_ID);

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
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Expenses = db.V_GET_EXPENSES_INFORMATIONs.Where(e => e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ID
                }).ToList();
            }

            return View();
        }


        [HttpPost]
        public void SubmitDelete(int? levelId)
        {
            if (null == levelId)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_PERSONNEL_LEVELs.Where(e => e.LEVEL_ID.Equals(levelId)).FirstOrDefault();
                if (null == expr)
                    return;

                expr.ACTIVE = -1;
                db.SubmitChanges();
            }
        }

        [HttpPost]
        public ActionResult SubmitSave(PersonnelLevelFormMapper model)
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
                var expr = db.T_PERSONNEL_LEVELs.Where(e => e.LEVEL_ID.Equals(model.LevelId)).FirstOrDefault();
                if (null != expr && expr.ACTIVE.Equals(-1))
                {
                    res["errorText"] = "ระดับ C ของบุคลากรนี้ ถูกยกเลิกไปแล้ว";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (null == expr)
                {
                    expr = new T_PERSONNEL_LEVEL();
                    expr.ACTIVE = 1;
                    db.T_PERSONNEL_LEVELs.InsertOnSubmit(expr);
                }

                expr.LEVEL_NAME = model.LevelName;
                // 1 = ใช้ได้ทุกรายการ คชจ., 2 = ใช้ได้เฉพาะบางรายการ คชจ.
                expr.EXPENSES_FLAG = Convert.ToInt16(model.ExpensesIds == null || model.ExpensesIds.Count == 0 ? 1 : 2);
                db.SubmitChanges();


                // บันทึกการให้สิทธิ์ รายการค่าใช้จ่ายที่สามารถใช้ระดับ C นี้ได้
                db.T_PERSONNEL_LEVEL_AUTHORIZE_EXPENSEs.DeleteAllOnSubmit(db.T_PERSONNEL_LEVEL_AUTHORIZE_EXPENSEs.Where(e => e.LEVEL_ID.Equals(expr.LEVEL_ID)));
                if (null != model.ExpensesIds)
                    model.ExpensesIds.ForEach(expensesId =>
                    {
                        db.T_PERSONNEL_LEVEL_AUTHORIZE_EXPENSEs.InsertOnSubmit(new T_PERSONNEL_LEVEL_AUTHORIZE_EXPENSE()
                        {
                            LEVEL_ID = expr.LEVEL_ID,
                            EXPENSES_ID = expensesId
                        });
                    });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class PersonnelLevelFormMapper
        {
            public int? LevelId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(180, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string LevelName { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่สามารถใช้ ระดับ C นี้ได้
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุรายการค่าใช้จ่ายที่สามารถใช้ระดับ C นี้ได้")]
            public List<int> ExpensesIds { get; set; }
        }
    }
}