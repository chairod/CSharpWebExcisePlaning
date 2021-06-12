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
    public class ExpensesGroupController : Controller
    {
        // GET: ExpensesGroup
        public ActionResult ExpensesGroupForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_EXPENSES_GROUP);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_EXPENSES_GROUP;
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
                ViewBag.BudgetType = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.BUDGET_TYPE_NAME)
                    .Select(e => new T_Budget_TypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();

                ViewBag.ExpensesMaster = db.T_EXPENSES_MASTERs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.EXPENSES_MASTER_NAME)
                    .Select(e => new ExpensesMasterShortFieldProperty()
                    {
                        EXPENSES_MASTER_ID = e.EXPENSES_MASTER_ID,
                        EXPENSES_MASTER_NAME = e.EXPENSES_MASTER_NAME
                    }).ToList();
            }

            return View();
        }

        [HttpGet]
        public ActionResult GetModalResource()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.BudgetType = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.BUDGET_TYPE_NAME)
                    .Select(e => new T_Budget_TypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();

                ViewBag.ExpensesMaster = db.T_EXPENSES_MASTERs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.EXPENSES_MASTER_NAME)
                    .Select(e => new ExpensesMasterShortFieldProperty()
                    {
                        EXPENSES_MASTER_ID = e.EXPENSES_MASTER_ID,
                        EXPENSES_MASTER_NAME = e.EXPENSES_MASTER_NAME
                    }).ToList();
            }

            return View();
        }

        [HttpPost]
        public ActionResult RetrieveForm(int? masterId, int? budgetTypeId, string expensesGroupName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_EXPENSES_GROUP_INFORMATIONs.Where(e => e.ACTIVE.Equals(1));

                if (masterId != null)
                    expr = expr.Where(e => e.EXPENSES_MASTER_ID.Equals(masterId.Value));

                if (budgetTypeId != null)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId.Value));

                if (!string.IsNullOrEmpty(expensesGroupName))
                    expr = expr.Where(e => e.EXPENSES_GROUP_NAME.Contains(expensesGroupName));

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));

                var currYear = AppUtils.GetCurrYear();
                pagging.rows = expr.OrderBy(e => e.BUDGET_TYPE_ORDER_SEQ).ThenBy(e => e.ORDER_SEQ)
                    .Skip(offset).Take(pageSize).Select(e => new
                    {
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_MASTER_ID,
                        e.BUDGET_TYPE_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_MASTER_NAME,
                        e.BUDGET_TYPE_NAME,
                        e.ORDER_SEQ,
                        e.ALLOCATE_GROUP_FLAG, // 1 = จัดสรรเป็นก้อน
                        // เลขที่อ้างอิงงบประมาณจากรัฐบาล
                        BUDGET_REFER_CODE = db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.Where(sub => sub.YR.Equals(currYear) && sub.EXPENSES_GROUP_ID.Equals(e.EXPENSES_GROUP_ID))
                        .Select(sub => sub.GOVERNMENT_REFER_CODE).FirstOrDefault()
                    }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public ActionResult GetExpensesGroupInfo(int? expensesGroupId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "expenses", null },
                { "governmentRefer", null }
            };
            if (null == expensesGroupId)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // รายการค่าใช้จ่ายภายใต้ หมวดค่าใช้จ่าย
                res["expenses"] = db.T_EXPENSES_ITEMs.Where(e => e.ACTIVE.Equals(1) && e.EXPENSES_GROUP_ID.Equals(expensesGroupId))
                    .OrderBy(e => e.EXPENSES_ID)
                    .Select(e => new
                    {
                        ExpensesId = e.EXPENSES_ID,
                        ExpensesName = e.EXPENSES_NAME
                    }).ToList();

                // เลขที่อ้างอิงของรายการค่าใช้จ่ายจากรัฐบาล
                res["governmentRefer"] = db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId))
                        .OrderByDescending(e => e.YR)
                        .Select(e => new
                        {
                            ReferCode = e.GOVERNMENT_REFER_CODE,
                            Yr = e.YR,
                            RemarkText = e.REMARK_TEXT
                        }).ToList();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public void SubmitDelete(int? expensesGroupId)
        {
            if (expensesGroupId == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_EXPENSES_GROUPs.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId)).FirstOrDefault();
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
        public ActionResult SubmitSave(ExpensesGroupFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() {
                { "errors", null } ,
                { "errorText", null }
            };

            // ตรวจสอบการระบุค่า จากผู้ใช้งาน ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0 && modelErrors.Keys.Any(keyName => !keyName.Contains("GovernmentRefers[")))
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบความถูกต้องของข้อมูลอ้างอิงแหล่งเงิน
            if (model.GovernmentRefers != null)
            {
                List<Dictionary<string, ModelValidateErrorProperty>> governmentReferErrors = new List<Dictionary<string, ModelValidateErrorProperty>>();
                model.GovernmentRefers.ForEach(GovernmentReferItem =>
                {
                    governmentReferErrors.Add(ModelValidateErrorProperty.TryOneValidate(GovernmentReferItem));
                });
                if (governmentReferErrors.Any(e => null != e))
                {
                    res["errors"] = new Dictionary<string, object>(1) { { "GovernmentRefers", governmentReferErrors } };
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // มีการอ้างอิงข้อมูล ปีงบประมาณของแหล่งเงิน ซ้ำกัน
                if (model.GovernmentRefers.GroupBy(e => e.Yr).Select(e => new { year = e.Key, counts = e.Count() }).Any(e => e.counts > 1))
                {
                    res["errorText"] = "[แหล่งเงิน] ระบุปีงบประมาณซ้ำกัน";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var exprExpensesGroup = db.T_EXPENSES_GROUPs.Where(e => e.ACTIVE.Equals(1) && e.EXPENSES_GROUP_ID.Equals(model.ExpensesGroupId)).FirstOrDefault();
                if (null == exprExpensesGroup)
                {
                    exprExpensesGroup = new T_EXPENSES_GROUP()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_EXPENSES_GROUPs.InsertOnSubmit(exprExpensesGroup);
                }
                else
                {
                    exprExpensesGroup.UPDATED_DATETIME = DateTime.Now;
                    exprExpensesGroup.UPDATED_ID = userAuthorizeProfile.EmpId;
                }
                exprExpensesGroup.EXPENSES_MASTER_ID = model.MasterId;
                exprExpensesGroup.BUDGET_TYPE_ID = model.BudgetTypeId.Value;
                exprExpensesGroup.EXPENSES_GROUP_NAME = model.ExpensesGroupName;
                exprExpensesGroup.ORDER_SEQ = model.OrderSeq.Value;
                exprExpensesGroup.ALLOCATE_GROUP_FLAG = model.AllocateGroupFlag;
                db.SubmitChanges();

                // ปรับปรุงรายการค่าใช้จ่าย ให้อยู่ในหมวด
                if (model.ExpensesItems != null)
                {
                    model.ExpensesItems.ForEach(expensesItem =>
                    {
                        var exprExpenses = db.T_EXPENSES_ITEMs.Where(e => e.ACTIVE.Equals(1) && e.EXPENSES_ID.Equals(expensesItem.ExpensesId)).FirstOrDefault();
                        if (null != exprExpenses)
                        {
                            exprExpenses.EXPENSES_GROUP_ID = exprExpensesGroup.EXPENSES_GROUP_ID;
                            exprExpenses.UPDATED_DATETIME = DateTime.Now;
                            exprExpenses.UPDATED_ID = userAuthorizeProfile.EmpId;
                        }
                    });
                }
                // ปรับปรุงรายการอ้างอิงแหล่งเงินของรัฐบาล
                db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.DeleteAllOnSubmit(db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.Where(e => e.EXPENSES_GROUP_ID.Equals(exprExpensesGroup.EXPENSES_GROUP_ID)));
                if (null != model.GovernmentRefers)
                    model.GovernmentRefers.ForEach(governmentReferItem =>
                    {
                        db.T_EXPENSES_GROUP_GOVERNMENT_REFER_CODEs.InsertOnSubmit(new T_EXPENSES_GROUP_GOVERNMENT_REFER_CODE()
                        {
                            EXPENSES_GROUP_ID = exprExpensesGroup.EXPENSES_GROUP_ID,
                            YR = governmentReferItem.Yr.Value,
                            GOVERNMENT_REFER_CODE = governmentReferItem.ReferCode,
                            REMARK_TEXT = governmentReferItem.RemarkText
                        });
                    });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class ExpensesGroupFormMapper
        {
            public short? ExpensesGroupId { get; set; }

            /// <summary>
            /// กลุ่มหมวดค่าใช้จ่าย
            /// </summary>
            public short? MasterId { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย อยู่ภายใต้ งบรายจ่ายอะไร
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? BudgetTypeId { get; set; }

            /// <summary>
            /// ชื่อหมวดค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ExpensesGroupName { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่อยู่ภายใต้ หมวดค่าใช้จ่าย
            /// </summary>
            public List<ExpensesItemProperty> ExpensesItems { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียงข้อมูล
            /// </summary> 
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(short), "0", "9", ErrorMessage = "ตัวเลขอยู่ระหว่าง {1} - {2}`")]
            public short? OrderSeq { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(short), "0", "1", ErrorMessage = "ตัวเลขอยู่ระหว่าง {1} - {2}`")]
            public short AllocateGroupFlag { get; set; }

            /// <summary>
            /// ข้อมูลอ้างอิงแหล่งเงิน งบประมาณของรัฐบาล
            /// </summary>
            public List<GovernmentReferProperty> GovernmentRefers { get; set; }
        }

        public class ExpensesItemProperty
        {
            public int ExpensesId { get; set; }
            public string ExpensesName { get; set; }
        }

        public class GovernmentReferProperty
        {
            /// <summary>
            /// รหัสอ้างอิงแหล่งเงินใช้กับ ปี งบประมาณใด
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? Yr { get; set; }

            /// <summary>
            /// เลขที่อ้างอิงแหล่งเงิน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(50, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ReferCode { get; set; }

            /// <summary>
            /// หมายเหตุ/อื่นๆ
            /// </summary>
            [MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}