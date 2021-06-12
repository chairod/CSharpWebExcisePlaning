using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// ปรับปรุงข้อมูลใบกัน ที่ยังไม่มีการเบิกจ่าย 
    /// เช่น หน่วยงาน ประเภทบัญชี (เงินงบ เงินนอกงบ) กลุ่มค่าใช้จ่าย (แผนงาน ผลผลิต กิจกรรม ... โครงการ)
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReserveAdjustmentController : Controller
    {
        // GET: BudgetReserveAdjustment
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_RESERVE_ADJUSTMENT_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_RESERVE_ADJUSTMENT_MENU;
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
                var appSettings = AppSettingProperty.ParseXml();
                var areaIdsCanReserveBudget = appSettings.GetAreaIdsCanReserveBudgetToList();
                // หน่วยงานภายในกรมสรรพสามิต ที่จะกันเงิน
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && areaIdsCanReserveBudget.Contains(e.AREA_ID.Value))
                    .OrderBy(e => e.SORT_INDEX)
                    .Select(e => new DepartmentShortFieldProperty()
                    {
                        DEP_ID = e.DEP_ID,
                        DEP_NAME = e.DEP_NAME
                    }).ToList();
                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();
                // งบรายจ่าย
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();
            }

            return View();
        }


        [HttpPost]
        public ActionResult Retrieve(string reserveId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "row", null }, { "errorText", null } };
            if (string.IsNullOrEmpty(reserveId))
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_BUDGET_RESERVE_INFORMATIONs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(reserveId))
                        .Select(e => new
                        {
                            e.YR,
                            e.RESERVE_ID,
                            e.DEP_ID,
                            e.BUDGET_TYPE,
                            e.RESERVE_TYPE,

                            e.PLAN_ID,
                            e.PRODUCE_ID,
                            e.ACTIVITY_ID,
                            e.BUDGET_TYPE_ID,
                            e.EXPENSES_GROUP_ID,
                            e.EXPENSES_ID,
                            e.PROJECT_ID,

                            e.RESERVE_BUDGET_AMOUNT,
                            e.USE_AMOUNT,
                            e.RESERVE_DATE,
                            e.RESERVE_NAME,
                            e.REMARK_TEXT,
                            e.CREATED_DATETIME
                        }).FirstOrDefault();

                if (null == expr)
                    res["errorText"] = "ไม่พบข้อมูลการกันเงิน";
                else if (expr.USE_AMOUNT.CompareTo(decimal.Zero) == 1)
                    res["errorText"] = "มีการเบิกจ่ายไปแล้ว ไม่สามารถปรับปรุงใบกันได้";
                else if (expr.RESERVE_BUDGET_AMOUNT.CompareTo(decimal.Zero) == 0)
                    res["errorText"] = "คืนเงินกลับไปส่วนกลางเต็มจำนวนแล้ว ไม่สามารถปรับปรุงใบกันได้";
                res["row"] = expr;
                return Json(res, JsonRequestBehavior.DenyGet);
            }
        }


        [HttpPost]
        public ActionResult SubmitSave(AdjustmentReserveFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2)
            {
                { "errors", null },
                { "errorText", null }
            };

            // ตรวจสอบความถูกต้องของข้อมูลที่ส่งจากฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            var reserveDate = AppUtils.TryValidUserDateStr(model.ReserveDateStr);
            if (!modelErrors.ContainsKey("ReserveDateStr") && DateTime.MinValue == reserveDate)
                modelErrors.Add("ReserveDateStr", new ModelValidateErrorProperty("ReserveDateStr", new List<string>(1) { "รูปแบบวันที่ไม่ถูกต้อง" }));
            if (model.ProjectIdRequired && null == model.PROJECT_ID)
                modelErrors.Add("PROJECT_ID", new ModelValidateErrorProperty("PROJECT_ID", new List<string>(1) { "โปรดระบุค่านี้ก่อน" }));
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(model.RESERVE_ID)).FirstOrDefault();
                if (null == exprReserve)
                {
                    res["errorText"] = string.Format("ไม่พบใบกันเลขที่ {0} หรือถูกยกเลิกแล้ว", model.RESERVE_ID);
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (exprReserve.USE_AMOUNT.CompareTo(decimal.Zero) == 1)
                {
                    res["errorText"] = "ใบกันนี้เบิกจ่ายแล้ว ไม่สามารถแก้ไขข้อมูลได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (!exprReserve.BUDGET_TYPE.Equals(model.BUDGET_TYPE.Value))
                {
                    res["errorText"] = "การเปลี่ยนแปลงประเภทงบประมาณของใบกันจะทำให้เลขที่ใบกันไม่ถูกต้อง ไม่สามารถเปลี่ยนแปลงได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (exprReserve.RESERVE_BUDGET_AMOUNT.CompareTo(decimal.Zero) == 0)
                {
                    res["errorText"] = "ใบกันคืนเงินกลับไปส่วนกลางเต็มจำนวนในใบกันแล้ว ไม่สามารถแก้ไขได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ไม่ให้แก้ไขข้อมูลใบกัน ปีก่อนหน้า
                if (!AppUtils.CanChangeDataByIntervalYear(exprReserve.YR, AppUtils.GetCurrYear()))
                {
                    res["errorText"] = "ใบกันนี้เป็นของปีงบประมาณอื่น ไม่สามารถแก้ไขได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบการเปลี่ยนแปลงข้อมูล
                StringBuilder sbFormData = new StringBuilder();
                sbFormData.Append(model.DEP_ID).Append("_")
                    .Append(model.BUDGET_TYPE).Append("_")
                    .Append(model.RESERVE_TYPE).Append("_")
                    .Append(model.PLAN_ID).Append("_")
                    .Append(model.PRODUCE_ID).Append("_")
                    .Append(model.ACTIVITY_ID).Append("_")
                    .Append(model.BUDGET_TYPE_ID).Append("_")
                    .Append(model.EXPENSES_GROUP_ID).Append("_")
                    .Append(model.EXPENSES_ID).Append("_")
                    .Append(model.PROJECT_ID).Append("_")
                    .Append(reserveDate.ToString("ddMMyyyy")).Append("_")
                    .Append(model.REMARK_TEXT).Append("_")
                    .Append(model.RESERVE_BUDGET_AMOUNT.Value.ToString("#,##0.00"));
                StringBuilder sbOrinData = new StringBuilder();
                sbOrinData.Append(exprReserve.DEP_ID).Append("_")
                    .Append(exprReserve.BUDGET_TYPE).Append("_")
                    .Append(exprReserve.RESERVE_TYPE).Append("_")
                    .Append(exprReserve.PLAN_ID).Append("_")
                    .Append(exprReserve.PRODUCE_ID).Append("_")
                    .Append(exprReserve.ACTIVITY_ID).Append("_")
                    .Append(exprReserve.BUDGET_TYPE_ID).Append("_")
                    .Append(exprReserve.EXPENSES_GROUP_ID).Append("_")
                    .Append(exprReserve.EXPENSES_ID).Append("_")
                    .Append(exprReserve.PROJECT_ID).Append("_")
                    .Append(exprReserve.RESERVE_DATE.Value.ToString("ddMMyyyy")).Append("_")
                    .Append(exprReserve.REMARK_TEXT).Append("_")
                    .Append(exprReserve.RESERVE_BUDGET_AMOUNT.ToString("#,##0.00"));
                if (sbFormData.Equals(sbOrinData))
                {
                    res["errorText"] = "โปรดแก้ไขข้อมูลในใบกัน อย่างน้อย 1 รายการก่อนปรับปรุง";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // คืนเงินงบประมาณ กลับไปให้ส่วนกลาง ตามยอดก่อนปรับปรุงใบกัน
                var result = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprReserve.YR
                        , exprReserve.PLAN_ID, exprReserve.PRODUCE_ID
                        , exprReserve.ACTIVITY_ID, exprReserve.BUDGET_TYPE_ID
                        , exprReserve.EXPENSES_GROUP_ID, exprReserve.EXPENSES_ID
                        , exprReserve.PROJECT_ID, exprReserve.BUDGET_TYPE
                        , BudgetUtils.ADJUSTMENT_CASHBACK, exprReserve.RESERVE_BUDGET_AMOUNT);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ขอกันเงินงบประมาณใหม่
                // ตามจำนวนเงินในใบกัน เนื่องจากไม่สามารถแก้ไขยอดกันเงินได้
                result = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprReserve.YR
                        , model.PLAN_ID, model.PRODUCE_ID
                        , model.ACTIVITY_ID, model.BUDGET_TYPE_ID.Value
                        , model.EXPENSES_GROUP_ID.Value, model.EXPENSES_ID.Value
                        , model.PROJECT_ID, model.BUDGET_TYPE.Value
                        , BudgetUtils.ADJUSTMENT_PAY, exprReserve.RESERVE_BUDGET_AMOUNT);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                //var depId = db.T_SUB_DEPARTMENTs.Where(e => e.SUB_DEP_ID.Equals(model.SUB_DEP_ID.Value)).Select(e => e.DEP_ID).FirstOrDefault();
                exprReserve.DEP_ID = model.DEP_ID;
                exprReserve.SUB_DEP_ID = null;
                exprReserve.BUDGET_TYPE = model.BUDGET_TYPE.Value;
                exprReserve.RESERVE_TYPE = model.RESERVE_TYPE.Value;

                exprReserve.PLAN_ID = model.PLAN_ID;
                exprReserve.PRODUCE_ID = model.PRODUCE_ID;
                exprReserve.ACTIVITY_ID = model.ACTIVITY_ID;
                exprReserve.BUDGET_TYPE_ID = model.BUDGET_TYPE_ID.Value;
                exprReserve.EXPENSES_GROUP_ID = model.EXPENSES_GROUP_ID.Value;
                exprReserve.EXPENSES_ID = model.EXPENSES_ID.Value;
                exprReserve.PROJECT_ID = model.PROJECT_ID;

                exprReserve.RESERVE_DATE = reserveDate;
                exprReserve.REMARK_TEXT = model.REMARK_TEXT;
                exprReserve.RESERVE_BUDGET_AMOUNT = model.RESERVE_BUDGET_AMOUNT.Value;
                exprReserve.USE_AMOUNT = decimal.Zero;
                exprReserve.REMAIN_AMOUNT = model.RESERVE_BUDGET_AMOUNT.Value;

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class AdjustmentReserveFormMapper
        {
            /// <summary>
            /// เลขที่กันเงิน ที่จะปรับปรุง
            /// </summary>
            public string RESERVE_ID { get; set; }

            /// <summary>
            /// หน่วยงานที่ได้รับกันเงิน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? DEP_ID { get; set; }

            /// <summary>
            /// 1 = เงินงบ, 2 = เงินนอกงบ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? BUDGET_TYPE { get; set; }

            /// <summary>
            /// 1 = ผูกพัน, 2 = กันไว้เบิก
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? RESERVE_TYPE { get; set; }

            /// <summary>
            /// แผนงาน
            /// </summary>
            public int? PLAN_ID { get; set; }

            /// <summary>
            /// ผลผลิต
            /// </summary>
            public int? PRODUCE_ID { get; set; }

            /// <summary>
            /// กิจกรรม
            /// </summary>
            public int? ACTIVITY_ID { get; set; }

            /// <summary>
            /// งบรายจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? BUDGET_TYPE_ID { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? EXPENSES_GROUP_ID { get; set; }

            /// <summary>
            /// ค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? EXPENSES_ID { get; set; }

            /// <summary>
            /// โครงการ
            /// </summary>
            public int? PROJECT_ID { get; set; }

            /// <summary>
            /// กลุ่มค่าใช้จ่ายที่เลือก บังคับให้ระบุโครงการหรือไม่
            /// true = บังคับให้ระบุ
            /// </summary>
            public bool ProjectIdRequired { get; set; }

            /// <summary>
            /// วันที่กันเงิน
            /// รูปแบบ dd/MM/yyyy (ปี พ.ศ.)
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ReserveDateStr { get; set; }

            /// <summary>
            /// จำนวนเงินที่ต้องการกัน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "1", "9999999999.99", ErrorMessage = "ค่าที่ระบุได้ {1} - {2}")]
            public decimal? RESERVE_BUDGET_AMOUNT { get; set; }


            /// <summary>
            /// หมายเหตุอื่นๆ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(120, ErrorMessage = "ข้อความไม่เกิน {1} ตัวอักษร")]
            public string REMARK_TEXT { get; set; }
        }
    }
}