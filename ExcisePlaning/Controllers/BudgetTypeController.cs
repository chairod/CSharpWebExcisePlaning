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
    public class BudgetTypeController : Controller
    {
        [CustomAuthorize(Roles = "Admin")]
        // GET: BudgetTypeForm
        public ActionResult BudgetTypeForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_TYPE_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_TYPE_MENU;
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
            return View("_BudgetTypeModal");
        }

        [HttpPost, Route("BudgetTypeName:string, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveForm(string BudgetTypeName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                var expr = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1));

                expr = expr.OrderBy(e => e.ORDER_SEQ);

                if (!string.IsNullOrEmpty(BudgetTypeName))
                    expr = expr.Where(e => e.BUDGET_TYPE_NAME.Contains(BudgetTypeName));

                var currYear = AppUtils.GetCurrYear();
                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.CAN_PAY_OVER_BUDGET_EXPENSES,
                    e.ORDER_SEQ,
                    // เลขที่อ้างอิงงบประมาณจากรัฐบาล
                    BUDGET_REFER_CODE = db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.Where(sub => sub.YR.Equals(currYear) && sub.BUDGET_TYPE_ID.Equals(e.BUDGET_TYPE_ID))
                        .Select(sub => sub.GOVERNMENT_REFER_CODE).FirstOrDefault()
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult GetBudgetTypeInfo(int? budgetTypeId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) {
                { "governmentRefer", null }
            };
            if (null == budgetTypeId)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // เลขที่อ้างอิงของรายการค่าใช้จ่ายจากรัฐบาล
                res["governmentRefer"] = db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId))
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


        [HttpPost, Route("BudgetTypeID:int?")]
        public void SubmitDelete(int? BudgetTypeID)
        {
            if (BudgetTypeID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var BudgetTypeExpr = db.T_BUDGET_TYPEs.Where(e => e.BUDGET_TYPE_ID.Equals(BudgetTypeID)).FirstOrDefault();
                if (BudgetTypeExpr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                BudgetTypeExpr.UPDATED_DATETIME = DateTime.Now;
                BudgetTypeExpr.UPDATED_ID = userAuthorizeProfile.EmpId;
                BudgetTypeExpr.ACTIVE = -1;
                db.SubmitChanges();


            };
        }

        [HttpPost]
        public ActionResult SubmitSave(BudgetTypeFormMapper model)
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
                    res["errorText"] = "ระบุปีงบประมาณซ้ำกัน";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var exprBudgetType = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1) && e.BUDGET_TYPE_ID.Equals(model.BudgetTypeID)).FirstOrDefault();

                if (null == exprBudgetType)
                {
                    exprBudgetType = new T_BUDGET_TYPE()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_BUDGET_TYPEs.InsertOnSubmit(exprBudgetType);
                }
                else
                {
                    exprBudgetType.UPDATED_DATETIME = DateTime.Now;
                    exprBudgetType.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                exprBudgetType.ORDER_SEQ = model.OrderSeq.Value;
                exprBudgetType.BUDGET_TYPE_NAME = model.BudgetTypeName;
                exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES = model.CanPayOverBudgetExpenses;
                db.SubmitChanges();

                // ปรับปรุงรายการอ้างอิงแหล่งเงินของรัฐบาล
                db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.DeleteAllOnSubmit(db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.Where(e => e.BUDGET_TYPE_ID.Equals(exprBudgetType.BUDGET_TYPE_ID)));
                if (null != model.GovernmentRefers)
                    model.GovernmentRefers.ForEach(governmentReferItem =>
                    {
                        db.T_BUDGET_TYPE_GOVERNMENT_REFER_CODEs.InsertOnSubmit(new T_BUDGET_TYPE_GOVERNMENT_REFER_CODE()
                        {
                            BUDGET_TYPE_ID = exprBudgetType.BUDGET_TYPE_ID,
                            YR = governmentReferItem.Yr.Value,
                            GOVERNMENT_REFER_CODE = governmentReferItem.ReferCode,
                            REMARK_TEXT = governmentReferItem.RemarkText
                        });
                    });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class BudgetTypeFormMapper
        {
            public BudgetTypeFormMapper() { }

            public short? BudgetTypeID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string BudgetTypeName { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย และ รายการค่าใช้จ่าย ที่อยู่ภายใต้งบรายจ่ายนี้สามารถใช้เงินเกินจำนวน เงินประจำงวดได้ และ จะต้องไม่เกินภาพรวมของ งบรายจ่าย
            /// 1 = ใช้, 0 = ไม่สามารถทำได้
            /// </summary>
            public short CanPayOverBudgetExpenses { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียงข้อมูล
            /// </summary> 
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? OrderSeq { get; set; }

            /// <summary>
            /// ข้อมูลอ้างอิงแหล่งเงิน งบประมาณของรัฐบาล
            /// </summary>
            public List<GovernmentReferProperty> GovernmentRefers { get; set; }
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