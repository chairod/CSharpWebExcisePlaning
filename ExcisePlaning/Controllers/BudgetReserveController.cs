using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// กันเงินงบประมาณเพื่อใช้ภายในหน่วยงานของกรมสรรพสามิต
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReserveController : Controller
    {
        /// <summary>
        /// ถ้ามีการส่ง Parameter เลขที่ใบกันมา ให้แสดงผลข้อมูลเลขที่ใบกันนั้น
        /// </summary>
        /// <param name="reserveId"></param>
        /// <returns></returns>
        public ActionResult GetForm(string reserveId)
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            // ตรวจสอบการเข้าทำงานของจอ
            var fiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            var verifyBudget = BudgetUtils.VerifyBudget(fiscalYear, null);
            if (!verifyBudget.IsComplete)
                return RedirectToAction("GetPageWarning", "BudgetAllocateDepartmentGroup");

            // ตรวจสอบหน่วยงานของผู้ทำรายการกันเงิน
            // มีอำนาจตามที่ระบบได้ให้สิทธิ์ไว้หรือไม่
            var appSettings = AppSettingProperty.ParseXml();
            if (appSettings.GetAreaIdsCanReserveBudgetToList().IndexOf(userAuthorizeProfile.AreaId.Value) == -1)
                return RedirectToAction("UnableToReserveBudgetForm", "BudgetReserve");


            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_RESERVE_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_RESERVE_MENU;
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

            ViewBag.ReserveId = reserveId;
            ViewBag.FiscalYear = AppUtils.GetCurrYear();
            ViewBag.DepId = userAuthorizeProfile.DepId;
            ViewBag.EmpFullName = userAuthorizeProfile.EmpFullname;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // หน่วยงานภายในกรมสรรพสามิต ที่จะกันเงิน
                var areaIdsCanReserveBudget = appSettings.GetAreaIdsCanReserveBudgetToList();
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


        /// <summary>
        /// แสดงแบบฟอร์มไม่สามารถทำรายการกันเงินได้
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult UnableToReserveBudgetForm()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Retrieve(string reserveId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "row", null },
                { "withdrawals", null }, // ประวัติการเบิกจ่าย
                { "histories", null } // ประวัติการปรับปรุงรายการ ใบกันเงิน
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // รายการกันเงิน
                res["row"] = db.V_GET_BUDGET_RESERVE_INFORMATIONs.Where(e => e.RESERVE_ID.Equals(reserveId))
                        .Select(e => new
                        {
                            e.RESERVE_ID,
                            e.DEP_ID,
                            e.SUB_DEP_ID,
                            e.YR,
                            e.RESERVE_DATE,
                            e.RESERVE_BUDGET_AMOUNT,
                            e.USE_AMOUNT,
                            e.REMAIN_AMOUNT,
                            e.CASHBACK_AMOUNT,
                            e.CREATED_DATETIME,
                            e.RESERVE_NAME,
                            e.REMARK_TEXT,
                            e.RESERVE_TYPE,
                            e.BUDGET_TYPE,
                            e.ACTIVE,

                            // เบิกจ่ายล่าสุด
                            e.LATEST_WITHDRAWAL_DATETIME,
                            e.LATEST_WITHDRAWAL_NAME,

                            // กลุ่ม คชจ.
                            e.PLAN_ID,
                            e.PRODUCE_ID,
                            e.ACTIVITY_ID,
                            e.BUDGET_TYPE_ID,
                            e.EXPENSES_GROUP_ID,
                            e.EXPENSES_ID,
                            e.PROJECT_ID
                        }).FirstOrDefault();

                // ประวัติการปรับปรุงรายการเบิกจ่าย
                res["histories"] = db.V_GET_BUDGET_RESERVE_HISTORY_INFORMATIONs.Where(e => e.RESERVE_ID.Equals(reserveId)).Select(e => new
                {
                    e.SEQ_NO,
                    e.RESERVE_ID,
                    e.TRAN_TYPE,
                    e.ADJUSTMENT_REFER_CODE,
                    e.CURR_RESERVE_AMOUNT,
                    e.CURR_WITHDRAWAL_AMOUNT,
                    e.ADJUSTMENT_AMOUNT,
                    e.BALANCE_AMOUNT,
                    e.CASHBACK_AMOUNT,
                    e.REMARK_TEXT,
                    e.CREATED_DATETIME,
                    e.CREATED_NAME
                }).OrderByDescending(e => e.SEQ_NO).ToList();
                // ประวัติการเบิกจ่าย รายการกันเงิน
                res["withdrawals"] = db.V_GET_BUDGET_RESERVE_WITHDRAWAL_INFORMATIONs.Where(e => e.RESERVE_ID.Equals(reserveId))
                        .OrderBy(e => e.SEQ_NO)
                        .Select(e => new
                        {
                            e.SEQ_NO,
                            e.RESERVE_ID,
                            e.WITHDRAWAL_CODE,
                            e.WITHDRAWAL_AMOUNT,
                            e.WITHDRAWAL_DATE,
                            WITHDRAWAL_DATETIME = e.CREATED_DATETIME,
                            WITHDRAWAL_NAME = e.CREATED_NAME,
                            e.REMARK_TEXT,
                            e.ACTIVE
                        }).OrderByDescending(e => e.SEQ_NO).ToList();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveProjectBy(int? fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int budgetType)
        {
            if (budgetTypeId == null || expensesGroupId == null || expensesId == null)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                int projectForType = 1;
                if (budgetType.Equals(2))
                    projectForType = 2;
                return Json(db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR == fiscalYear
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId
                    && e.PROJECT_FOR_TYPE.Equals(projectForType)) // 1 = โครงการของเงินงบ, 2 = โครงการของเงินนอกงบ
                    .Select(e => new { e.PROJECT_ID, e.PROJECT_NAME }).ToList(), JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// แสดงแบบฟอร์มการปรับปรุงยอดเงินในใบกันเงิน
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalAdjustmentReserveForm()
        {
            return View();
        }

        /// <summary>
        /// ปรับปรุงจำนวนเงินในใบกันเงิน
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitAdjustment(AdjustmentReserveFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "errors", null }, { "errorText", null } };


            // ตรวจสอบความถูกต้องของค่า ที่รับจากฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.RESERVE_ID.Equals(model.ReserveId)).FirstOrDefault();
                if (null == exprReserve)
                {
                    res["errorText"] = "ไม่พบข้อมูลใบกันที่ต้องการปรับปรุง";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (exprReserve.ACTIVE.Equals(-1))
                {
                    res["errorText"] = string.Format("ใบกันเงินถูกยกเลิกไปแล้ว เวลา: {0}", exprReserve.DELETED_DATETIME.Value.ToString("dd/MM/yyyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ปรับปรุงยอดใบกันเงิน
                var oldReserveAmounts = exprReserve.RESERVE_BUDGET_AMOUNT;
                string adjustmentType = BudgetUtils.ADJUSTMENT_PAY;
                if (model.AdjustmentType.Equals(2))
                {
                    exprReserve.RESERVE_BUDGET_AMOUNT -= model.AdjustmentAmounts.Value;
                    //exprReserve.CASHBACK_AMOUNT+= model.AdjustmentAmounts.Value;
                    adjustmentType = BudgetUtils.ADJUSTMENT_CASHBACK;
                }
                else if (model.AdjustmentType.Equals(3))
                    exprReserve.RESERVE_BUDGET_AMOUNT += model.AdjustmentAmounts.Value;

                exprReserve.REMAIN_AMOUNT = exprReserve.RESERVE_BUDGET_AMOUNT - exprReserve.USE_AMOUNT;
                if (exprReserve.RESERVE_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1 || exprReserve.REMAIN_AMOUNT.CompareTo(decimal.Zero) == -1)
                {
                    res["errorText"] = "หลังปรับปรุงยอดใบกันทำให้จำนวนเงินคงเหลือสุทธิใบกันน้อยกว่าศูนย์ (0) โปรดตรวจสอบ";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                // เก็บประวัติการกันเงิน
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                db.T_BUDGET_RESERVE_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_HISTORY()
                {
                    RESERVE_ID = exprReserve.RESERVE_ID,
                    SEQ_NO = db.T_BUDGET_RESERVE_HISTORies.Where(e => e.RESERVE_ID.Equals(exprReserve.RESERVE_ID)).Count() + 1,
                    DEP_ID = exprReserve.DEP_ID,
                    SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                    TRAN_TYPE = Convert.ToInt16(model.AdjustmentType),
                    BUDGET_TYPE = exprReserve.BUDGET_TYPE,
                    RESERVE_TYPE = exprReserve.RESERVE_TYPE,

                    CURR_RESERVE_AMOUNT = oldReserveAmounts,
                    CURR_WITHDRAWAL_AMOUNT = exprReserve.USE_AMOUNT,
                    ADJUSTMENT_AMOUNT = model.AdjustmentAmounts.Value,
                    CASHBACK_AMOUNT = model.AdjustmentType.Equals(2) ? model.AdjustmentAmounts.Value : decimal.Zero,
                    BALANCE_AMOUNT = exprReserve.REMAIN_AMOUNT,

                    REMARK_TEXT = model.RemarkText,
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId,
                    LATEST_WITHDRAWAL_DATETIME = exprReserve.LATEST_WITHDRAWAL_DATETIME,
                    LATEST_WITHDRAWAL_ID = exprReserve.LATEST_WITHDRAWAL_ID
                });


                // ปรับปรุงยอดงบประมาณ คงเหลือในคลัง (ภาพรวมทั้งกรมฯ รายการค่าใช้จ่าย รายโครงการ)
                var result = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprReserve.YR
                        , exprReserve.PLAN_ID, exprReserve.PRODUCE_ID
                        , exprReserve.ACTIVITY_ID, exprReserve.BUDGET_TYPE_ID
                        , exprReserve.EXPENSES_GROUP_ID, exprReserve.EXPENSES_ID
                        , exprReserve.PROJECT_ID, exprReserve.BUDGET_TYPE
                        , adjustmentType, model.AdjustmentAmounts.Value);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ยกเลิก รายการเบิกจ่าย
        /// </summary>
        /// <param name="reserveId"></param>
        /// <param name="seqNo"></param>
        /// <returns></returns>
        //[HttpPost]
        //public ActionResult SubmitCancelWithdrawal(string reserveId, int seqNo)
        //{
        //    Dictionary<string, string> res = new Dictionary<string, string>() { { "errorText", null } };
        //    if (string.IsNullOrEmpty(reserveId))
        //        return Json(res, JsonRequestBehavior.DenyGet);

        //    using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
        //    {
        //        // ประวัติการเบิกจ่าย
        //        var exprWithdrawal = db.T_BUDGET_RESERVE_WITHDRAWALs.Where(e => e.ACTIVE.Equals(1)
        //            && e.RESERVE_ID.Equals(reserveId) && e.SEQ_NO.Equals(seqNo)).FirstOrDefault();
        //        if (null == exprWithdrawal)
        //        {
        //            res["errorText"] = "ไม่พบข้อมูลประวัติการเบิกจ่ายที่ระบุ";
        //            return Json(res, JsonRequestBehavior.DenyGet);
        //        }
        //        var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
        //        exprWithdrawal.ACTIVE = -1;
        //        exprWithdrawal.DELETED_DATETIME = DateTime.Now;
        //        exprWithdrawal.DELETED_ID = userAuthorizeProfile.EmpId;

        //        // รายการกันเงินงบประมาณ
        //        var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(reserveId)).FirstOrDefault();
        //        exprReserve.USE_AMOUNT -= exprWithdrawal.WITHDRAWAL_AMOUNT;
        //        exprReserve.REMAIN_AMOUNT = exprReserve.RESERVE_BUDGET_AMOUNT - exprReserve.USE_AMOUNT;

        //        db.SubmitChanges();
        //    }

        //    return Json(res, JsonRequestBehavior.DenyGet);
        //}

        /// <summary>
        /// บันทึกรายการกันเงินงบประมาณ
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitSave(BudgetReserveFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errorText", null },
                { "errors", null },
                { "reserveId", null }
            };


            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (model.ProjectIdRequired && model.ProjectId == null)
                modelErrors.Add("ProjectId", new ModelValidateErrorProperty("ProjectId", new List<string>(1) { "โปรดระบุโครงการก่อน" }));
            if (!modelErrors.ContainsKey("ReserveDateStr") && AppUtils.TryValidUserDateStr(model.ReserveDateStr) == DateTime.MinValue)
                modelErrors.Add("ReserveDateStr", new ModelValidateErrorProperty("ReserveDateStr", new List<string>() { "รูปแบบวันที่ไม่ถูกต้อง" }));
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                if (model.ReserveId == null)
                    model.FiscalYear = AppUtils.GetCurrYear();

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var appSettings = AppSettingProperty.ParseXml();
                if (appSettings.GetAreaIdsCanReserveBudgetToList().IndexOf(userAuthorizeProfile.AreaId.Value) == -1)
                {
                    res["errorText"] = "หน่วยงานที่ท่านสังกัดไม่สามารถทำรายการกันเงินงบประมาณได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var reserveDate = AppUtils.TryValidUserDateStr(model.ReserveDateStr);
                var result = BudgetUtils.DoReserveBudget(db, model.ReserveId, model.FiscalYear.Value, model.DepId.Value
                        , model.PlanId, model.ProduceId, model.ActivityId, model.BudgetTypeId.Value
                        , model.ExpensesGroupId.Value, model.ExpensesId.Value, model.ProjectId
                        , model.BudgetType.Value, model.ReserveType.Value, model.ReserveAmounts.Value
                        , reserveDate, model.RemarkText, userAuthorizeProfile);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                res["reserveId"] = result.RunningCode;
                db.SubmitChanges();


                // ย้ายโค้ดไปไว้ BudgetUtils.DoReserveBudget
                // เนื่องจากมีการเรียกใช้จาก เมนู ปรับปรุงบัญชี
                //var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.RESERVE_ID.Equals(model.ReserveId)).FirstOrDefault();
                //if (null != exprReserve)
                //{
                //    if (exprReserve.ACTIVE.Equals(-1))
                //    {
                //        res["errorText"] = string.Format("ใบกันเงินนี้ถูกยกเลิกไปแล้ว เมื่อ {0} ไม่สามารถปรับปรุงใบกันเงินนี้ได้", exprReserve.DELETED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                //        return Json(res, JsonRequestBehavior.DenyGet);
                //    }

                //    exprReserve.RESERVE_DATE = reserveDate;
                //    exprReserve.REMARK_TEXT = model.RemarkText;
                //}
                //else
                //{
                //    var depId = db.T_SUB_DEPARTMENTs.Where(e => e.SUB_DEP_ID.Equals(model.SubDepId)).Select(e => e.DEP_ID).FirstOrDefault();
                //    var activityOrderSeq = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVITY_ID.Equals(model.ActivityId)).Select(e => e.ORDER_SEQ).FirstOrDefault();
                //    var budgetTypeOrderSeq = db.T_BUDGET_TYPEs.Where(e => e.BUDGET_TYPE_ID.Equals(model.BudgetTypeId)).Select(e => e.ORDER_SEQ).FirstOrDefault();
                //    var expensesGroupOrderSeq = db.T_EXPENSES_GROUPs.Where(e => e.EXPENSES_GROUP_ID.Equals(model.ExpensesGroupId)).Select(e => e.ORDER_SEQ).FirstOrDefault();

                //    var fiscalYear2Digits = (model.FiscalYear + 543).ToString().Substring(2);
                //    model.ReserveId = AppUtils.GetNextKey(string.Format("BUDGET_RESERVE.RESERVE_ID_{0}_{1}_{2}_{3}", fiscalYear2Digits, model.BudgetType, model.ActivityId, model.BudgetTypeId)
                //        , string.Format("{0}{1}{2}{3}{4}"
                //            , fiscalYear2Digits
                //            , model.BudgetType
                //            , activityOrderSeq
                //            , budgetTypeOrderSeq
                //            , expensesGroupOrderSeq), 4, false, true);
                //    exprReserve = new T_BUDGET_RESERVE()
                //    {
                //        RESERVE_ID = model.ReserveId,
                //        DEP_ID = depId,
                //        SUB_DEP_ID = model.SubDepId,
                //        YR = Convert.ToInt16(model.FiscalYear.Value),
                //        RESERVE_DATE = reserveDate,

                //        PLAN_ID = model.PlanId,
                //        PRODUCE_ID = model.ProduceId,
                //        ACTIVITY_ID = model.ActivityId,
                //        BUDGET_TYPE_ID = model.BudgetTypeId.Value,
                //        EXPENSES_GROUP_ID = model.ExpensesGroupId.Value,
                //        EXPENSES_ID = model.ExpensesId.Value,
                //        PROJECT_ID = model.ProjectId,

                //        RESERVE_TYPE = model.ReserveType.Value,
                //        BUDGET_TYPE = model.BudgetType.Value,

                //        RESERVE_BUDGET_AMOUNT = model.ReserveAmounts.Value,
                //        USE_AMOUNT = decimal.Zero,
                //        REMAIN_AMOUNT = model.ReserveAmounts.Value,
                //        CASHBACK_AMOUNT = decimal.Zero,

                //        REMARK_TEXT = model.RemarkText,
                //        ACTIVE = 1,
                //        CREATED_DATETIME = DateTime.Now,
                //        USER_ID = userAuthorizeProfile.EmpId
                //    };
                //    db.T_BUDGET_RESERVEs.InsertOnSubmit(exprReserve);

                //    // ปรับปรุงยอด งบประมาณคงเหลือ
                //    var adjustmentResult = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprReserve.YR
                //            , exprReserve.PLAN_ID, exprReserve.PRODUCE_ID
                //            , exprReserve.ACTIVITY_ID, exprReserve.BUDGET_TYPE_ID
                //            , exprReserve.EXPENSES_GROUP_ID, exprReserve.EXPENSES_ID
                //            , exprReserve.PROJECT_ID, exprReserve.BUDGET_TYPE
                //            , BudgetUtils.ADJUSTMENT_PAY, exprReserve.RESERVE_BUDGET_AMOUNT);
                //    if (!adjustmentResult.Completed)
                //    {
                //        res["errorText"] = adjustmentResult.CauseErrorMessage;
                //        return Json(res, JsonRequestBehavior.DenyGet);
                //    }

                //    // เก็บประวัติการกันเงิน
                //    db.T_BUDGET_RESERVE_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_HISTORY()
                //    {
                //        RESERVE_ID = exprReserve.RESERVE_ID,
                //        SEQ_NO = 1,
                //        DEP_ID = exprReserve.DEP_ID,
                //        SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                //        TRAN_TYPE = 1, // กันเงินงบประมาณ
                //        BUDGET_TYPE = exprReserve.BUDGET_TYPE,
                //        RESERVE_TYPE = exprReserve.RESERVE_TYPE,
                //        CURR_RESERVE_AMOUNT = model.ReserveAmounts.Value,
                //        ADJUSTMENT_AMOUNT = decimal.Zero,
                //        CASHBACK_AMOUNT = decimal.Zero,
                //        BALANCE_AMOUNT = model.ReserveAmounts.Value,
                //        CURR_WITHDRAWAL_AMOUNT = decimal.Zero,
                //        REMARK_TEXT = model.RemarkText,
                //        CREATED_DATETIME = DateTime.Now,
                //        USER_ID = userAuthorizeProfile.EmpId,
                //        LATEST_WITHDRAWAL_DATETIME = null,
                //        LATEST_WITHDRAWAL_ID = null
                //    });
                //}

                //res["reserveId"] = exprReserve.RESERVE_ID;
                //db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ยกเลิกใบกันเงินงบประมาณ
        /// (ยกเลิกได้เฉพาะ ที่ยังไม่มีการเบิกจ่าย)
        /// </summary>
        /// <param name="reserveId"></param>
        /// <param name="remarkText">เหตุผลการยกเลิก</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitReject(string reserveId, string remarkText)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) { { "errorText", null } };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.RESERVE_ID.Equals(reserveId)).FirstOrDefault();
                if (null == exprReserve)
                {
                    res["errorText"] = "ไม่พบเลขที่ใบกันเงินที่ต้องการยกเลิก";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (exprReserve.ACTIVE.Equals(-1))
                {
                    res["errorText"] = string.Format("ใบกันเงินนี้ ถูกยกเลิกไปแล้วเมื่อ {0}", exprReserve.DELETED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var oldReserveAmounts = exprReserve.RESERVE_BUDGET_AMOUNT;
                var cashbackAmounts = exprReserve.RESERVE_BUDGET_AMOUNT - exprReserve.USE_AMOUNT;
                exprReserve.RESERVE_BUDGET_AMOUNT = exprReserve.USE_AMOUNT; // ยอดกันเงินเท่ากับยอดใช้ไป (กรณียกเลิกใบกัน)
                exprReserve.REMAIN_AMOUNT = decimal.Zero; // ปรับยอดคงเหลือศูนย์บาท
                exprReserve.CASHBACK_AMOUNT = cashbackAmounts;
                exprReserve.ACTIVE = -1;
                exprReserve.DELETED_DATETIME = DateTime.Now;
                exprReserve.DELETED_ID = userAuthorizeProfile.EmpId;
                exprReserve.REJECT_REMARK_TEXT = remarkText;

                // มียอดคงเหลือ ให้นำไปคืนส่วนกลาง
                if (cashbackAmounts.CompareTo(decimal.Zero) == 1)
                {
                    var adjustmentResult = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprReserve.YR
                        , exprReserve.PLAN_ID, exprReserve.PRODUCE_ID
                        , exprReserve.ACTIVITY_ID, exprReserve.BUDGET_TYPE_ID
                        , exprReserve.EXPENSES_GROUP_ID, exprReserve.EXPENSES_ID
                        , exprReserve.PROJECT_ID, exprReserve.BUDGET_TYPE
                        , BudgetUtils.ADJUSTMENT_CASHBACK, cashbackAmounts);
                    if (!adjustmentResult.Completed)
                    {
                        res["errorText"] = adjustmentResult.CauseErrorMessage;
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                }

                db.T_BUDGET_RESERVE_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_HISTORY()
                {
                    RESERVE_ID = exprReserve.RESERVE_ID,
                    SEQ_NO = db.T_BUDGET_RESERVE_HISTORies.Where(e => e.RESERVE_ID.Equals(reserveId)).Count() + 1,
                    DEP_ID = exprReserve.DEP_ID,
                    SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                    RESERVE_TYPE = exprReserve.RESERVE_TYPE,
                    BUDGET_TYPE = exprReserve.BUDGET_TYPE,

                    CURR_RESERVE_AMOUNT = oldReserveAmounts,
                    CURR_WITHDRAWAL_AMOUNT = exprReserve.USE_AMOUNT,
                    ADJUSTMENT_AMOUNT = cashbackAmounts,
                    CASHBACK_AMOUNT = cashbackAmounts,
                    BALANCE_AMOUNT = exprReserve.USE_AMOUNT,

                    LATEST_WITHDRAWAL_DATETIME = exprReserve.LATEST_WITHDRAWAL_DATETIME,
                    LATEST_WITHDRAWAL_ID = exprReserve.LATEST_WITHDRAWAL_ID,
                    REMARK_TEXT = remarkText, //"ยกเลิกใบกันเงิน",
                    TRAN_TYPE = 4, // ยกเลิกใบกันเงิน
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId,
                });
                db.SubmitChanges();

                //// ตรวจสอบการเบิกจ่าย
                ////if(db.T_BUDGET_RESERVE_WITHDRAWALs.Any(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(expr.RESERVE_ID)))
                ////{
                ////    res["errorText"] = "รายการกันนี้เงินเบิกจ่ายไปแล้ว จะต้องยกเลิกรายการเบิกจ่ายทั้งหมด แล้วค่อยยกเลิกรายการกันเงิน";
                ////    return Json(res, JsonRequestBehavior.DenyGet);
                ////}

                //// คืนเงินตามยอดคงเหลือในใบกัน
                //var reserveRemainBudgetAmount = expr.BUDGET_TYPE.Equals(1) ? expr.REMAIN_AMOUNT : decimal.Zero;
                //var reserveRemainOffBudgetAmount = expr.BUDGET_TYPE.Equals(2) ? expr.REMAIN_AMOUNT : decimal.Zero;

                //// งบกลาง ทั้งหมดของกรมสรรพสามิต
                //var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(expr.YR)).FirstOrDefault();
                //if (null != exprBudgetMas)
                //{
                //    // ปรับปรุงยอดการใช้เงินงบประมาณ
                //    exprBudgetMas.USE_BUDGET_AMOUNT -= reserveRemainBudgetAmount;
                //    exprBudgetMas.REMAIN_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_BUDGET_AMOUNT - exprBudgetMas.USE_BUDGET_AMOUNT;

                //    // ปรับปรุงยอดการใช้ เงินนอกงบประมาณ
                //    exprBudgetMas.USE_OFF_BUDGET_AMOUNT -= reserveRemainOffBudgetAmount;
                //    exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                //}

                //// งบกลาง ส่วนรายการค่าใช้จ่ายของกรมสรรพสามามิต
                //var exprBudgetExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.YR.Equals(expr.YR)
                //    && e.ACTIVE.Equals(1)
                //    && e.PLAN_ID == expr.PLAN_ID
                //    && e.PRODUCE_ID == expr.PRODUCE_ID
                //    && e.ACTIVITY_ID == expr.ACTIVITY_ID
                //    && e.BUDGET_TYPE_ID == expr.BUDGET_TYPE_ID
                //    && e.EXPENSES_GROUP_ID == expr.EXPENSES_GROUP_ID
                //    && e.EXPENSES_ID == expr.EXPENSES_ID).FirstOrDefault();
                //if (null != exprBudgetExpenses)
                //{
                //    // ปรับปรุงยอดการใช้เงินงบประมาณ
                //    exprBudgetExpenses.USE_BUDGET_AMOUNT -= reserveRemainBudgetAmount;
                //    exprBudgetExpenses.REMAIN_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_BUDGET_AMOUNT - exprBudgetExpenses.USE_BUDGET_AMOUNT;

                //    // ปรับปรุงยอดการใช้ เงินนอกงบประมาณ
                //    exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT -= reserveRemainOffBudgetAmount;
                //    exprBudgetExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT;
                //}

                //// งบกลาง ส่วนโครงการ
                //if (expr.PROJECT_ID != null)
                //{
                //    // งบกลาง เฉพาะรายการค่าใช้จ่ายของกรมสรรพสามามิต
                //    var exprBudgetExpensesProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.YR.Equals(expr.YR)
                //        && e.ACTIVE.Equals(1)
                //        && e.PLAN_ID == expr.PLAN_ID
                //        && e.PRODUCE_ID == expr.PRODUCE_ID
                //        && e.ACTIVITY_ID == expr.ACTIVITY_ID
                //        && e.BUDGET_TYPE_ID == expr.BUDGET_TYPE_ID
                //        && e.EXPENSES_GROUP_ID == expr.EXPENSES_GROUP_ID
                //        && e.EXPENSES_ID == expr.EXPENSES_ID
                //        && e.PROJECT_ID == expr.PROJECT_ID).FirstOrDefault();
                //    if (null != exprBudgetExpensesProject)
                //    {
                //        // ปรับปรุงยอดการใช้เงินงบประมาณ
                //        exprBudgetExpensesProject.USE_BUDGET_AMOUNT -= reserveRemainBudgetAmount;
                //        exprBudgetExpensesProject.REMAIN_BUDGET_AMOUNT = exprBudgetExpensesProject.ACTUAL_BUDGET_AMOUNT - exprBudgetExpensesProject.USE_BUDGET_AMOUNT;

                //        // ปรับปรุงยอดการใช้ เงินนอกงบประมาณ
                //        exprBudgetExpensesProject.USE_OFF_BUDGET_AMOUNT -= reserveRemainOffBudgetAmount;
                //        exprBudgetExpensesProject.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetExpensesProject.USE_OFF_BUDGET_AMOUNT;
                //    }
                //}


                //// ยกเลิกรายการกันเงิน
                //var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                //expr.CASHBACK_AMOUNT = expr.REMAIN_AMOUNT;
                //expr.REJECT_REMARK_TEXT = "ขอยกเลิกใบกัน คืนเงินกลับคงคลังตามยอดคงเหลือ";
                //expr.ACTIVE = -1;
                //expr.DELETED_DATETIME = DateTime.Now;
                //expr.DELETED_ID = userAuthorizeProfile.EmpId;

                //// ยกเลิกประวัติรายการเบิกจ่ายทั้งหมด
                //var exprReserveWithdrawal = db.T_BUDGET_RESERVE_WITHDRAWALs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(expr.RESERVE_ID)).ToList();
                //exprReserveWithdrawal.ForEach(entity =>
                //{
                //    entity.ACTIVE = -1;
                //    entity.DELETED_DATETIME = DateTime.Now;
                //    entity.DELETED_ID = userAuthorizeProfile.EmpId;
                //});

                //db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetReserveFormMapper
        {
            /// <summary>
            /// เลขที่กันเงินงบประมาณ (สำหรับกรณีแก้ไข)
            /// </summary>
            public string ReserveId { get; set; }

            /// <summary>
            /// หน่วยงานภายในที่กันงบ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? DepId { get; set; }

            /// <summary>
            /// ปีงบประมาณ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? FiscalYear { get; set; }

            /// <summary>
            /// แผนงาน
            /// </summary>
            public int? PlanId { get; set; }

            /// <summary>
            /// ผลผลิต
            /// </summary>
            public int? ProduceId { get; set; }

            /// <summary>
            /// กิจกรรม จะเป็นส่วนหนึ่งของการสร้างรหัสของ เลขที่ขอเบิก
            /// จึงต้องบังคับให้ระบุค่า
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? ActivityId { get; set; }

            /// <summary>
            /// งบรายจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? BudgetTypeId { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? ExpensesGroupId { get; set; }

            /// <summary>
            /// ค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? ExpensesId { get; set; }

            /// <summary>
            /// โครงการ ของรายการค่าใช้จ่าย
            /// </summary>
            public int? ProjectId { get; set; }

            /// <summary>
            /// คำขอกันเงินนี้จำเป็นต้องบังคับให้ใส่ โครงการหรือไม่
            /// </summary>
            public bool ProjectIdRequired { get; set; }

            /// <summary>
            /// จำนวนเงินที่ต้องการกันไว้ (บาท)
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(0.01, 99999999999, ErrorMessage = "ค่าอยู่ระหว่าง {1} - {2}")]
            public decimal? ReserveAmounts { get; set; }

            /// <summary>
            /// ประเภทการกันเงิน 1 = ผูกพัน, 2 = กันไว้เบิก
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, 2, ErrorMessage = "ค่าอยู่ระหว่าง {1} - {2}")]
            public short? ReserveType { get; set; }

            /// <summary>
            /// งบประมาณที่ใช้ในการกันเงิน 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, 2, ErrorMessage = "ค่าอยู่ระหว่าง {1} - {2}")]
            public short? BudgetType { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(120, ErrorMessage = "ข้อความไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }


            /// <summary>
            /// วันที่กันเงินงบประมาณ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ReserveDateStr { get; set; }
        }


        /// <summary>
        /// คลาส Mapping ข้อมูลในฟอร์มการขอปรับปรุงยอดเงินใบกัน
        /// </summary>
        public class AdjustmentReserveFormMapper
        {
            public string ReserveId { get; set; }

            /// <summary>
            /// จำนวนเงินที่ต้องการปรับปรุง
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0", "999999999999999999.99", ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public decimal? AdjustmentAmounts { get; set; }

            /// <summary>
            /// 2 = ปรับลดยอด, 3 = ปรับเพิ่มยอด
            /// </summary>
            public int AdjustmentType { get; set; }

            /// <summary>
            /// หมายเหตุอื่นๆ
            /// </summary>
            [MaxLength(120, ErrorMessage = "ข้อความไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}