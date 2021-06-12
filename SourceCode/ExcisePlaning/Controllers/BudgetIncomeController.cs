using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// จัดสรรเงินงบประมาณที่มีอยู่ ลงไปให้กับหน่วยงานต่างๆในพื้นที่ โดยใช้ข้อมูลคำของบประมาณ
    /// เป็นตัวอ้างอิงรายการค่าใช้จ่าย ที่จะจัดสรรไปลงไปให้กับหน่วยงาน
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetIncomeController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageType">การแสดงผลข้อมูลการจัดสรรงบประมาณ, budget = แสดงเฉพาะงบประมาณ, off_budget=แสดงเฉพาะเงินนอกงบประมาณ</param>
        /// <returns></returns>
        public ActionResult GetForm(string pageType)
        {
            string currentMenuConst = AppConfigConst.MENU_CONST_ALL_BUDGET_INCOME_GORNVERMENT_MENU;
            if ("budget".Equals(pageType))
                currentMenuConst = AppConfigConst.MENU_CONST_BUDGET_INCOME_GORNVERMENT_MENU;
            else if ("off_budget".Equals(pageType))
                currentMenuConst = AppConfigConst.MENU_CONST_OFF_BUDGET_INCOME_GORNVERMENT_MENU;

            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(currentMenuConst);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กรณีไม่ผ่านค่า type เข้าไปให้เด้งกลับไปหน้า Dashboard/หน้าแรก
            List<string> acceptPageTypes = new List<string>() { "all", "budget", "off_budget" };
            if (string.IsNullOrEmpty(pageType) || acceptPageTypes.IndexOf(pageType) == -1)
                return RedirectToAction(menuIndexItem.ActionName, menuIndexItem.RouteName);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = currentMenuConst;
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
                ActionName = menuItem.ActionName,
                QueryString = menuItem.QueryString
            });
            ViewBag.Breadcrumps = breadcrumps;


            ViewBag.FiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.PageType = pageType;
            return View();
        }


        [HttpPost]
        public ActionResult Retrieve(short? fiscalYear)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() {
                { "netBudgetAmounts", 0 }, // จัดสรรจากรัฐบาลสุทธิ (งบประมาณ
                { "netActualBudgetAmounts", 0 }, // ได้รับจัดสรรจริง
                { "netUseBudgetAmounts", 0 }, // ยอดเบิกจ่าย เงินงบประมาณ
                { "netAllocateBudgetToDepartmentAmounts", 0 }, // จัดสรรให้หน่วยงาน
                { "netReserveBudgetAmounts", 0 }, // กันเงินงบประมาณ
                { "netReserveWithdrawalBudgetAmounts", 0 }, // เบิกจ่ายงบประมาณ
                { "netRemainBudgetAmounts", 0 }, // เงินงบประมาณคงเหลือสุทธิ

                { "netOffBudgetAmounts", 0 }, // จัดสรรจากรัฐบาลสุทธิ (เงินนอกงบประมาณ)
                { "netOffActualBudgetAmounts", 0 }, // จัดเก็บรายได้สุทธิ
                { "netOffUseBudgetAmounts", 0 }, // ยอดเบิกจ่าย เงินนอกงบประมาณ
                { "netAllocateOffBudgetToDepartmentAmounts", 0 }, // จัดสรรให้หน่วยงาน
                { "netReserveOffBudgetAmounts", 0 }, // กันเงินนอกงบประมาณ
                { "netReserveWithdrawalOffBudgetAmounts", 0 }, // เบิกจ่ายงบนอกประมาณ
                { "netOffRemainBudgetAmounts", 0 }, // เงินนอกงบประมาณ คงเหลือสุทธิ


                { "editAble", false }, // รายการจัดสรรเงิน งปม. จากรัฐบาลสามารถแก้ไขได้หรือไม่ (น้อยกว่าปี งปม. ปัจจุบันแก้ไขไม่ได้)
                { "budgetId", null }, // เลขที่กำกับของปีงบประมาณ ใช้ตรวจสอบเวลาบันทึกข้อมูล
                { "offBudgetSpeadToExpenses", 0 }, // กระจายงบประมาณลงไปแต่ละรายการค่าใช้จ่าย
                { "expenses", null } // รายการค่าใช้จ่ายที่ได้รับจัดสรร
            };

            if (null == fiscalYear)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.proc_GetExpensesIncomeBudgetFromGovernmentByYear(fiscalYear).ToList();
                var finalExpr = expr.GroupBy(e => new
                {
                    e.PLAN_ID,
                    e.PLAN_NAME,
                    e.PLAN_ORDER_SEQ,
                    e.PRODUCE_ID,
                    e.PRODUCE_NAME,
                    e.PRODUCE_ORDER_SEQ,
                    e.ACTIVITY_ID,
                    e.ACTIVITY_NAME,
                    e.ACTIVITY_ORDER_SEQ,
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.BUDGET_TYPE_ORDER_SEQ,
                    e.BUDGET_TYPE_GOVERNMENT_REFER_CODE,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.EXPENSES_GROUP_GOVERNMENT_REFER_CODE
                }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.Key.EXPENSES_GROUP_ORDER_SEQ)
                .Select(e => new
                {
                    GroupBy = e.Key,
                    ExpensesItems = e.OrderBy(x => x.EXPENSES_ORDER_SEQ).ToList()
                }).ToList();

                res["expenses"] = finalExpr;

                // สรุปข้อมูลการจัดสรร งปม. จากรัฐบาลตามปีที่ขอแสดงผล ข้อมูล
                if (expr.Any())
                {

                    var exprDepartmentBudget = db.T_BUDGET_ALLOCATEs.Where(e => e.YR.Equals(fiscalYear));
                    // รายการกันเงินงบประมาณ บางรายการ ที่ยกเลิกมีการกันเบิกจ่ายไปแล้ว
                    // จะต้องนำมาด้วย
                    var exprReserve = db.T_BUDGET_RESERVEs.Where(e => (e.ACTIVE.Equals(1) || (e.ACTIVE.Equals(-1) && e.USE_AMOUNT > 0)) && e.YR.Equals(fiscalYear));
                    var exprBudgetReserve = exprReserve.Where(e => e.BUDGET_TYPE.Equals(1)); // กันเงิน งบประมาณ
                    var exprOffBudgetReserve = exprReserve.Where(e => e.BUDGET_TYPE.Equals(2)); // กันเงิน นอกงบประมาณ


                    res["netBudgetAmounts"] = expr.Sum(e => e.BUDGET_AMOUNT);
                    res["netActualBudgetAmounts"] = expr.Sum(e => e.ACTUAL_BUDGET_AMOUNT);
                    // การใช้จ่ายงบประมาณ
                    res["netUseBudgetAmounts"] = expr.Sum(e => e.USE_BUDGET_AMOUNT);
                    res["netAllocateBudgetToDepartmentAmounts"] = exprDepartmentBudget.Any() ? exprDepartmentBudget.Sum(e => e.ALLOCATE_BUDGET_AMOUNT) : decimal.Zero;
                    if (exprBudgetReserve.Any())
                    {
                        var reserveAmount = exprBudgetReserve.Sum(e => e.RESERVE_BUDGET_AMOUNT);
                        var withdrawalAmount = exprBudgetReserve.Sum(e => e.USE_AMOUNT);
                        res["netReserveBudgetAmounts"] = reserveAmount - withdrawalAmount;
                        res["netReserveWithdrawalBudgetAmounts"] = withdrawalAmount;
                    }
                    // ยอดคงเหลือสุทธิ
                    res["netRemainBudgetAmounts"] = expr.Sum(e => e.REMAIN_BUDGET_AMOUNT);

                    var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear)).FirstOrDefault();
                    res["netOffBudgetAmounts"] = exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT;
                    res["netOffActualBudgetAmounts"] = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT;
                    // การใช้จ่ายเงินนอกงบประมาณ
                    res["netOffUseBudgetAmounts"] = exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                    res["netAllocateOffBudgetToDepartmentAmounts"] = exprDepartmentBudget.Any() ? exprDepartmentBudget.Sum(e => e.ALLOCATE_OFF_BUDGET_AMOUNT) : decimal.Zero;
                    if (exprOffBudgetReserve.Any())
                    {
                        var reserveAmount = exprOffBudgetReserve.Sum(e => e.RESERVE_BUDGET_AMOUNT);
                        var withdrawalAmount = exprOffBudgetReserve.Sum(e => e.USE_AMOUNT);
                        res["netReserveOffBudgetAmounts"] = reserveAmount - withdrawalAmount;
                        res["netReserveWithdrawalOffBudgetAmounts"] = withdrawalAmount;
                    }
                    // ยอดคงเหลือสุทธิ
                    res["netOffRemainBudgetAmounts"] = exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT;

                    // ให้แก้ไขข้อมูลได้เฉพาะข้อมูลปีงบประมาณ มากกว่า หรือ เท่ากับปีงบประมาณปัจจุบัน
                    res["editAble"] = expr.First().YR.CompareTo(AppUtils.GetCurrYear()) != -1;

                    int? budgetId = expr.First().BUDGET_ID;
                    res["budgetId"] = budgetId;
                    res["offBudgetSpeadToExpenses"] = exprBudgetMas.OFF_BUDGET_SPREAD_TO_EXPENSES ? 1 : 0;
                }
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// แบบฟอร์ม รับเงินงบประมาณรายโครงการ จากรัฐบาล
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalExpensesProjectForm()
        {
            return View();
        }

        /// <summary>
        /// ค้นหาโครงการที่อยู่ภายใต้รายการค่าใช้จ่าย สำหรับแสดงผลให้ระบุข้อมูลงบประมาณที่ได้รับ
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="pageType">budget = เงินงบประมาณ, off_budget = เงินนอกงบประมาณ</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveExpensesProject(int fiscalYear, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, string pageType)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                int projectForType = 1;
                if ("off_budget".Equals(pageType))
                    projectForType = 2;
                return Json(db.V_GET_BUDGET_EXPENSES_PROJECT_INFORMATIONs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR == fiscalYear
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId
                    && e.PROJECT_FOR_TYPE == projectForType)
                    .Select(e => new
                    {
                        e.YR,
                        e.PLAN_ID,
                        e.PLAN_NAME,
                        e.PRODUCE_ID,
                        e.PRODUCE_NAME,
                        e.ACTIVITY_ID,
                        e.ACTIVITY_NAME,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_ID,
                        e.EXPENSES_NAME,
                        e.PROJECT_ID,
                        e.PROJECT_NAME,

                        // ข้อมูลงบประมาณ
                        e.BUDGET_AMOUNT,
                        e.ACTUAL_BUDGET_AMOUNT,
                        e.USE_BUDGET_AMOUNT,
                        e.REMAIN_BUDGET_AMOUNT,

                        // ข้อมูลเงินนอกงบประมาณ
                        e.OFF_BUDGET_AMOUNT,
                        e.ACTUAL_OFF_BUDGET_AMOUNT,
                        e.USE_OFF_BUDGET_AMOUNT,
                        e.REMAIN_OFF_BUDGET_AMOUNT
                    }).ToList(), JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// แบบฟอร์ม แสดงประวัติการรับเงิน งวดที่จัดสรร
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalViewIncomeHistoryForm()
        {
            return View();
        }
        /// <summary>
        /// ค้นหาประวัติการ รับเงินงวดที่รัฐบาลจัดสรร
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="projectId"></param>
        /// <param name="pageType">all = เงิน งปม./เงินนอก, budget = เฉพาะเงิน งปม., off_budget = เฉพาะเงินนอก งปม.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetriveIncomeHistory(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int expensesGroupId, int expensesId, int? projectId, string pageType)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_BUDGET_EXPENSES_INCOME_INFORMATIONs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR.Equals(fiscalYear)
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId);
                if (projectId != null)
                    expr = expr.Where(e => e.PROJECT_ID == projectId);

                if ("budget".Equals(pageType))
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(1));
                else if ("off_budget".Equals(pageType))
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(2));

                return Json(expr.Select(e => new
                {
                    e.INCOME_ID,
                    e.YR,
                    e.PLAN_ID,
                    e.PLAN_NAME,
                    e.PRODUCE_ID,
                    e.PRODUCE_NAME,
                    e.ACTIVITY_ID,
                    e.ACTIVITY_NAME,
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.PROJECT_ID,
                    e.PROJECT_NAME,
                    e.BUDGET_TYPE,
                    e.RECEIVE_BUDGET_AMOUNT,
                    e.REMARK_TEXT,
                    e.CREATED_DATETIME,
                    e.CREATED_NAME
                }).ToList(), JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ยกเลิกประวัติการรับเงินงวด จะต้องปรับปรุงยอด
        /// 1. งบภาพรวมของกรมสรรพามิต
        /// 2. งบภาพรวม ของรายการค่าใช้จ่าย
        /// 3. งบภาพรวม ของโครงการ
        /// </summary>
        /// <param name="incomeId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitRejectIncomeHistory(int? incomeId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>() { { "errorText", null } };
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprExpensesIncome = db.T_BUDGET_EXPENSES_INCOMEs.Where(e => e.ACTIVE.Equals(1) && e.INCOME_ID.Equals(incomeId)).FirstOrDefault();
                if (null == exprExpensesIncome)
                {
                    res["errorText"] = "ไม่พบประวัติการรับเงินที่ต้องการยกเลิก";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // คืนเงินไปยัง งบประมาณ/เงินนอก งปม.
                decimal rollbackBudgetAmounts = decimal.Zero;
                decimal rollbackOffBudgetAmounts = decimal.Zero;
                if (exprExpensesIncome.BUDGET_TYPE.Equals(1))
                    rollbackBudgetAmounts = exprExpensesIncome.RECEIVE_BUDGET_AMOUNT;
                else
                    rollbackOffBudgetAmounts = exprExpensesIncome.RECEIVE_BUDGET_AMOUNT;

                // ปรับปรุง งปม./นอก งปม. ภาพรวมของกรมฯ
                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(exprExpensesIncome.YR)).FirstOrDefault();
                if (null != exprBudgetMas)
                {
                    exprBudgetMas.ACTUAL_BUDGET_AMOUNT -= rollbackBudgetAmounts;
                    exprBudgetMas.REMAIN_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_BUDGET_AMOUNT - exprBudgetMas.USE_BUDGET_AMOUNT;
                    if (exprBudgetMas.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "งบประมาณคงเหลือสุทธิของกรมฯ น้อยกว่าศูนย์ ไม่สามารถยกเลิกได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT -= rollbackOffBudgetAmounts;
                    exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                    if (exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "เงินนอกงบประมาณคงเหลือสุทธิของกรมฯ น้อยกว่าศูนย์ ไม่สามารถยกเลิกได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                }

                // ปรับปรุง งปม./นอก งปม. ภาพรวมของค่าใช้จ่าย
                var exprBudgetExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR.Equals(exprExpensesIncome.YR)
                    && e.PLAN_ID == exprExpensesIncome.PLAN_ID
                    && e.PRODUCE_ID == exprExpensesIncome.PRODUCE_ID
                    && e.ACTIVITY_ID == exprExpensesIncome.ACTIVITY_ID
                    && e.BUDGET_TYPE_ID == exprExpensesIncome.BUDGET_TYPE_ID
                    && e.EXPENSES_GROUP_ID == exprExpensesIncome.EXPENSES_GROUP_ID
                    && e.EXPENSES_ID == exprExpensesIncome.EXPENSES_ID).FirstOrDefault();
                if (null != exprBudgetExpenses)
                {
                    exprBudgetExpenses.ACTUAL_BUDGET_AMOUNT -= rollbackBudgetAmounts;
                    exprBudgetExpenses.REMAIN_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_BUDGET_AMOUNT - exprBudgetExpenses.USE_BUDGET_AMOUNT;
                    if (exprBudgetExpenses.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "งบประมาณคงเหลือสุทธิของค่าใช้จ่าย น้อยกว่าศูนย์ ไม่สามารถยกเลิกได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    exprBudgetExpenses.ACTUAL_OFF_BUDGET_AMOUNT -= rollbackOffBudgetAmounts;
                    exprBudgetExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT;
                    if (exprBudgetExpenses.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "เงินนอกงบประมาณคงเหลือสุทธิของค่าใช้จ่าย น้อยกว่าศูนย์ ไม่สามารถยกเลิกได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                }

                // ปรับปรุง งปม./นอก งปม. ภาพรวมของโครงการ
                var exprBudgetProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR.Equals(exprExpensesIncome.YR)
                    && e.PLAN_ID == exprExpensesIncome.PLAN_ID
                    && e.PRODUCE_ID == exprExpensesIncome.PRODUCE_ID
                    && e.ACTIVITY_ID == exprExpensesIncome.ACTIVITY_ID
                    && e.BUDGET_TYPE_ID == exprExpensesIncome.BUDGET_TYPE_ID
                    && e.EXPENSES_GROUP_ID == exprExpensesIncome.EXPENSES_GROUP_ID
                    && e.EXPENSES_ID == exprExpensesIncome.EXPENSES_ID
                    && e.PROJECT_ID == exprExpensesIncome.PROJECT_ID).FirstOrDefault();
                if (null != exprBudgetProject)
                {
                    exprBudgetProject.ACTUAL_BUDGET_AMOUNT -= rollbackBudgetAmounts;
                    exprBudgetProject.REMAIN_BUDGET_AMOUNT = exprBudgetProject.ACTUAL_BUDGET_AMOUNT - exprBudgetProject.USE_BUDGET_AMOUNT;
                    if (exprBudgetProject.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "งบประมาณคงเหลือสุทธิของโครงการ น้อยกว่าศูนย์ ไม่สามารถยกเลิกได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    exprBudgetProject.ACTUAL_OFF_BUDGET_AMOUNT -= rollbackOffBudgetAmounts;
                    exprBudgetProject.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetProject.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetProject.USE_OFF_BUDGET_AMOUNT;
                    if (exprBudgetProject.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "เงินนอกงบประมาณคงเหลือสุทธิของโครงการ น้อยกว่าศูนย์ ไม่สามารถยกเลิกได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                }


                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                exprExpensesIncome.ACTIVE = -1;
                exprExpensesIncome.DELETED_DATETIME = DateTime.Now;
                exprExpensesIncome.DELETED_ID = userAuthorizeProfile.EmpId;

                db.SubmitChanges();
            }
            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public ActionResult SubmitSave(BudgetIncomeFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errorText", null },
                { "errors", null }
            };

            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState).Where(e => !e.Key.Contains("Expenses")).ToDictionary(e => e.Key, e => e.Value);
            if ("budget".Equals(model.PageType))
            {
                if (string.IsNullOrEmpty(model.ReferDocNo))
                    modelErrors.Add("ReferDocNo", new ModelValidateErrorProperty("ReferDocNo", new List<string>(1) { "โปรดระบุค่านี้ก่อน" }));
                else if (Regex.Replace(model.ReferDocNo, @"[^\d]", "", RegexOptions.IgnoreCase).Length != 10)
                    modelErrors.Add("ReferDocNo", new ModelValidateErrorProperty("ReferDocNo", new List<string>(1) { "ความยาว 10 ตัวอักษร" }));
            }
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบความถูกต้องของรายการ คชจ.
            if (null != model.Expenses)
            {
                Dictionary<string, Dictionary<string, ModelValidateErrorProperty>> expensesErrors = new Dictionary<string, Dictionary<string, ModelValidateErrorProperty>>();
                model.Expenses.ForEach(expensesItem =>
                {
                    string errorKey = string.Format("{0}{1}{2}{3}{4}{5}"
                        , expensesItem.PLAN_ID
                        , expensesItem.PRODUCE_ID
                        , expensesItem.ACTIVITY_ID
                        , expensesItem.BUDGET_TYPE_ID
                        , expensesItem.EXPENSES_GROUP_ID
                        , expensesItem.EXPENSES_ID);
                    expensesErrors.Add(errorKey, new Dictionary<string, ModelValidateErrorProperty>());

                    decimal budgetIncomeAmounts = expensesItem.ACTUAL_BUDGET_AMOUNT;
                    if (expensesItem.NewAllocateBudgetAmounts != null)
                        budgetIncomeAmounts += expensesItem.NewAllocateBudgetAmounts.Value;
                    decimal offBudgetIncomeAmounts = expensesItem.ACTUAL_OFF_BUDGET_AMOUNT;
                    if (expensesItem.NewAllocateOffBudgetAmounts != null)
                        offBudgetIncomeAmounts += expensesItem.NewAllocateOffBudgetAmounts.Value;

                    if (expensesItem.BUDGET_AMOUNT.CompareTo(budgetIncomeAmounts) == -1)
                        expensesErrors[errorKey].Add("NewAllocateBudgetAmounts", new ModelValidateErrorProperty("NewAllocateBudgetAmounts", new List<string>() { "งบประมาณ น้อยกว่า เงินประจำงวด" }));
                    if (expensesItem.OFF_BUDGET_AMOUNT.CompareTo(offBudgetIncomeAmounts) == -1)
                        expensesErrors[errorKey].Add("NewAllocateOffBudgetAmounts", new ModelValidateErrorProperty("NewAllocateOffBudgetAmounts", new List<string>() { "งบประมาณ น้อยกว่า เงินประจำงวด" }));
                });
                if (expensesErrors.Any(error => error.Value.Any()))
                {
                    res["errors"] = new Dictionary<string, object>() { { "Expenses", expensesErrors } };
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
            }

            // แก้ไขข้อมูลงบประมาณปีก่อนหน้าไม่ได้ 
            if (!AppUtils.CanChangeDataByIntervalYear(model.FiscalYear.Value, AppUtils.GetCurrYear()))
            {
                res["errorText"] = "ไม่สามารถแก้ไขข้อมูลงบประมาณของปีก่อนหน้าได้";
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                decimal budgetIncomeAmount = decimal.Zero;
                decimal offBudgetIncomeAmount = decimal.Zero;
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                string[] incomePeriodParts = model.ReceivePeriod.Split(new char[] { '/' });
                if (incomePeriodParts.Length != 2)
                {
                    res["errorText"] = "รูปแบบงวดที่รับเงินงบประมาณ ไม่ถูกต้อง";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                short incomeYear = Convert.ToInt16(incomePeriodParts[1])
                    , incomeMonth = Convert.ToInt16(incomePeriodParts[0]);
                if (incomeMonth > 12)
                {
                    res["errorText"] = "รูปแบบงวดที่รับเงินงบประมาณ เดือนไม่ถูกต้อง";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // งบประมาณภวพรวม ของ กรมสรรพสามิตฯ
                if (null != model.Expenses)
                {
                    budgetIncomeAmount = model.Expenses.Sum(e => e.NewAllocateBudgetAmounts == null ? decimal.Zero : e.NewAllocateBudgetAmounts.Value);
                    offBudgetIncomeAmount = model.Expenses.Sum(e => e.NewAllocateOffBudgetAmounts == null ? decimal.Zero : e.NewAllocateOffBudgetAmounts.Value);
                }

                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(model.FiscalYear)).FirstOrDefault();
                exprBudgetMas.ACTUAL_BUDGET_AMOUNT += budgetIncomeAmount;
                exprBudgetMas.REMAIN_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_BUDGET_AMOUNT - exprBudgetMas.USE_BUDGET_AMOUNT;
                exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT += offBudgetIncomeAmount;
                exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                if (budgetIncomeAmount.CompareTo(decimal.Zero) == 1)
                    exprBudgetMas.LATEST_INCOME_BUDGET = DateTime.Now;
                if (offBudgetIncomeAmount.CompareTo(decimal.Zero) == 1)
                    exprBudgetMas.LATEST_INCOME_OFF_BUDGET = DateTime.Now;


                if ("off_budget".Equals(model.PageType))
                    exprBudgetMas.OFF_BUDGET_SPREAD_TO_EXPENSES = model.OffBudgetSpeadToExpenses.Equals(1);

                if (null != model.Expenses)
                    model.Expenses.ForEach(expensesItem =>
                    {
                        budgetIncomeAmount = null != expensesItem.NewAllocateBudgetAmounts ? expensesItem.NewAllocateBudgetAmounts.Value : decimal.Zero;
                        offBudgetIncomeAmount = null != expensesItem.NewAllocateOffBudgetAmounts ? expensesItem.NewAllocateOffBudgetAmounts.Value : decimal.Zero;
                        if (null != expensesItem.Projects)
                        {
                            budgetIncomeAmount = expensesItem.Projects.Sum(e => e.AllocateBudgetAmounts == null ? decimal.Zero : e.AllocateBudgetAmounts.Value);
                            offBudgetIncomeAmount = expensesItem.Projects.Sum(e => e.AllocateOffBudgetAmounts == null ? decimal.Zero : e.AllocateOffBudgetAmounts.Value);
                        }

                        // งบประมาณภาพรวม ของ รายการค่าใช้จ่าย
                        var exprExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                            && e.YR.Equals(model.FiscalYear)
                            && e.PLAN_ID == expensesItem.PLAN_ID
                            && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                            && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                            && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                            && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                            && e.EXPENSES_ID == expensesItem.EXPENSES_ID).FirstOrDefault();
                        if (null != exprExpenses)
                        {
                            exprExpenses.ACTUAL_BUDGET_AMOUNT += budgetIncomeAmount;
                            exprExpenses.REMAIN_BUDGET_AMOUNT = exprExpenses.ACTUAL_BUDGET_AMOUNT - exprExpenses.USE_BUDGET_AMOUNT;

                            exprExpenses.ACTUAL_OFF_BUDGET_AMOUNT += offBudgetIncomeAmount;
                            exprExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprExpenses.ACTUAL_OFF_BUDGET_AMOUNT - exprExpenses.USE_OFF_BUDGET_AMOUNT;

                            exprExpenses.LATEST_RECEIVE_BUDGET = DateTime.Now;
                        }


                        if (expensesItem.Projects != null)
                            expensesItem.Projects.ForEach(projectItem =>
                            {
                                budgetIncomeAmount = projectItem.AllocateBudgetAmounts == null ? decimal.Zero : projectItem.AllocateBudgetAmounts.Value;
                                offBudgetIncomeAmount = projectItem.AllocateOffBudgetAmounts == null ? decimal.Zero : projectItem.AllocateOffBudgetAmounts.Value;

                                var exprExpensesProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1)
                                    && e.YR.Equals(model.FiscalYear)
                                    && e.PLAN_ID == expensesItem.PLAN_ID
                                    && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                                    && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                                    && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                                    && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                                    && e.EXPENSES_ID == expensesItem.EXPENSES_ID
                                    && e.PROJECT_ID == projectItem.PROJECT_ID).FirstOrDefault();
                                if (null != exprExpensesProject)
                                {
                                    exprExpensesProject.ACTUAL_BUDGET_AMOUNT += budgetIncomeAmount;
                                    exprExpensesProject.REMAIN_BUDGET_AMOUNT = exprExpensesProject.ACTUAL_BUDGET_AMOUNT - exprExpensesProject.USE_BUDGET_AMOUNT;
                                    exprExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT += offBudgetIncomeAmount;
                                    exprExpensesProject.REMAIN_OFF_BUDGET_AMOUNT = exprExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT - exprExpensesProject.USE_OFF_BUDGET_AMOUNT;
                                    exprExpensesProject.LATEST_RECEIVE_BUDGET_DATETIME = DateTime.Now;

                                    if (budgetIncomeAmount.CompareTo(decimal.Zero) == 1)
                                        db.T_BUDGET_EXPENSES_INCOMEs.InsertOnSubmit(new T_BUDGET_EXPENSES_INCOME()
                                        {
                                            YR = model.FiscalYear.Value,
                                            PERIOD_YR = incomeYear,
                                            PERIOD_MN = incomeMonth,
                                            REFER_DOC_NO = model.ReferDocNo,
                                            PLAN_ID = expensesItem.PLAN_ID,
                                            PRODUCE_ID = expensesItem.PRODUCE_ID,
                                            ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                            BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                            EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                            EXPENSES_ID = expensesItem.EXPENSES_ID,
                                            PROJECT_ID = projectItem.PROJECT_ID,
                                            RECEIVE_BUDGET_AMOUNT = budgetIncomeAmount,
                                            BUDGET_TYPE = 1, // รับเงิน งปม.
                                            CREATED_DATETIME = DateTime.Now,
                                            USER_ID = userAuthorizeProfile.EmpId,
                                            REMARK_TEXT = expensesItem.RemarkText,
                                            ACTIVE = 1
                                        });
                                    if (offBudgetIncomeAmount.CompareTo(decimal.Zero) == 1)
                                        db.T_BUDGET_EXPENSES_INCOMEs.InsertOnSubmit(new T_BUDGET_EXPENSES_INCOME()
                                        {
                                            YR = model.FiscalYear.Value,
                                            PERIOD_YR = incomeYear,
                                            PERIOD_MN = incomeMonth,
                                            REFER_DOC_NO = model.ReferDocNo,
                                            PLAN_ID = expensesItem.PLAN_ID,
                                            PRODUCE_ID = expensesItem.PRODUCE_ID,
                                            ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                            BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                            EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                            EXPENSES_ID = expensesItem.EXPENSES_ID,
                                            PROJECT_ID = projectItem.PROJECT_ID,
                                            RECEIVE_BUDGET_AMOUNT = offBudgetIncomeAmount,
                                            BUDGET_TYPE = 2, // รับเงิน นอก งปม.
                                            CREATED_DATETIME = DateTime.Now,
                                            USER_ID = userAuthorizeProfile.EmpId,
                                            REMARK_TEXT = expensesItem.RemarkText,
                                            ACTIVE = 1
                                        });
                                }
                            });
                        else
                        {
                            if (budgetIncomeAmount.CompareTo(decimal.Zero) == 1)
                                db.T_BUDGET_EXPENSES_INCOMEs.InsertOnSubmit(new T_BUDGET_EXPENSES_INCOME()
                                {
                                    YR = model.FiscalYear.Value,
                                    PERIOD_YR = incomeYear,
                                    PERIOD_MN = incomeMonth,
                                    REFER_DOC_NO = model.ReferDocNo,
                                    PLAN_ID = expensesItem.PLAN_ID,
                                    PRODUCE_ID = expensesItem.PRODUCE_ID,
                                    ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                    BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                    EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                    EXPENSES_ID = expensesItem.EXPENSES_ID,
                                    PROJECT_ID = null,
                                    RECEIVE_BUDGET_AMOUNT = budgetIncomeAmount,
                                    BUDGET_TYPE = 1, // รับเงิน งปม.
                                    CREATED_DATETIME = DateTime.Now,
                                    USER_ID = userAuthorizeProfile.EmpId,
                                    REMARK_TEXT = expensesItem.RemarkText,
                                    ACTIVE = 1
                                });
                            if (offBudgetIncomeAmount.CompareTo(decimal.Zero) == 1)
                                db.T_BUDGET_EXPENSES_INCOMEs.InsertOnSubmit(new T_BUDGET_EXPENSES_INCOME()
                                {
                                    YR = model.FiscalYear.Value,
                                    PERIOD_YR = incomeYear,
                                    PERIOD_MN = incomeMonth,
                                    REFER_DOC_NO = model.ReferDocNo,
                                    PLAN_ID = expensesItem.PLAN_ID,
                                    PRODUCE_ID = expensesItem.PRODUCE_ID,
                                    ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                    BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                    EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                    EXPENSES_ID = expensesItem.EXPENSES_ID,
                                    PROJECT_ID = null,
                                    RECEIVE_BUDGET_AMOUNT = offBudgetIncomeAmount,
                                    BUDGET_TYPE = 2, // รับเงิน นอก งปม.
                                    CREATED_DATETIME = DateTime.Now,
                                    USER_ID = userAuthorizeProfile.EmpId,
                                    REMARK_TEXT = expensesItem.RemarkText,
                                    ACTIVE = 1
                                });
                        }
                    });

                db.SubmitChanges();
            }


            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class BudgetIncomeFormMapper
        {
            /// <summary>
            /// เลขกำกับ ปีงบประมาณ ค่านี้จะไม่ซ้ำกันในแต่ละปี 
            /// สามารถใช้เป็นคีย์ในการค้นหา
            /// </summary>
            public int? BudgetId { get; set; }

            /// <summary>
            /// ปีงบประมาณ (ค.ศ.) ที่ได้รับเงินจากรัฐบาล 
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? FiscalYear { get; set; }

            /// <summary>
            /// งวดที่รับเงินจากรัฐบาล ระบุ เดือน (2 หลัก)/ปี (ค.ศ.)
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ReceivePeriod { get; set; }

            /// <summary>
            /// เลขที่เอกสารในการรับเงินประจำงวด
            /// กรณี เงินงบประมาณ ให้บังคับให้ระบุค่านี้ เนื่องจากเงินนอกใช้อีกหน้าจอในการรับเงินประจำงวด
            /// </summary>
            public string ReferDocNo { get; set; }

            /// <summary>
            /// เงินนอกงบประมาณ กระจายงบประมาณลงไปสู่รายการค่าใช้จ่ายหรือไม่
            /// 1 = กระจายลงไป, 0 = ไม่กระจาย
            /// ถ้ากระจายลงไปให้แต่ละรายการค่าใช้จ่าย ตอนจัดสรรงบประมาณให้หน่วยงานภายนอก และ กันเงินงบประมาณ รายการค่าใช้จ่ายที่กำลังทำรายการ
            /// จะมีต้องงบประมาณเพียงพอถึงจะสามารถทำรายการได้
            /// </summary>
            public short OffBudgetSpeadToExpenses { get; set; }

            /// <summary>
            /// รายการที่ส่งมาบันทึก ทำรายการผ่านหน้าจอใด
            /// budget = จัดเก็บรายได้เงินงบประมาณ
            /// off_budget = จัดเก็บรายได้เงินนอกงบประมาณ
            /// </summary>
            public string PageType { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่ได้รับจัดสรรจากรัฐบาล
            /// </summary>
            [Required(ErrorMessage = "ไม่พบรายการค่าใช้จ่ายที่รับเงินจัดสรรรายงวด จากรัฐบาล")]
            public List<ExpensesIncomeBudgetProperty> Expenses { get; set; }
        }

        public class ExpensesIncomeBudgetProperty
        {
            /// <summary>
            /// เลขที่รายการของ รายการค่าใช้จ่าย ที่ถูกบันทึกไว้ก่อนหน้าแล้ว
            /// </summary>
            public int? SEQ_ID { get; set; }

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
            /// งบรายจ่าย (งบดำเนินงาน งบอุดหนุน งบอื่นๆ ฯลฯ)
            /// </summary>
            public int BUDGET_TYPE_ID { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_GROUP_ID { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่ได้รับเงิน งปม. รายรัฐบาล
            /// </summary>
            public int EXPENSES_ID { get; set; }

            /// <summary>
            /// รายการ คชจ. นี้สามารถเพิ่มโครงการได้หรือไม่
            /// </summary>
            public bool CAN_ADD_PROJECT { get; set; }

            /// <summary>
            /// เงินงบประมาณจัดสรร สะสม
            /// </summary>
            public decimal BUDGET_AMOUNT { get; set; }

            /// <summary>
            /// เงินงบประมาณประจำงวด สะสม
            /// </summary>
            public decimal ACTUAL_BUDGET_AMOUNT { get; set; }

            /// <summary>
            /// จำนวนเงิน งปม. ที่จัดสรรจากรัฐบาล
            /// </summary>
            public decimal? NewAllocateBudgetAmounts { get; set; }

            /// <summary>
            /// เงินนอก งปม. จัดสรรสะสม
            /// </summary>
            public decimal OFF_BUDGET_AMOUNT { get; set; }

            /// <summary>
            /// เงินนอกงบประมาณ ประจำงวดสะสม
            /// </summary>
            public decimal ACTUAL_OFF_BUDGET_AMOUNT { get; set; }

            /// <summary>
            /// จำนวนเงินนอก งปม. ที่จัดสรรจากรัฐบาล
            /// </summary>
            public decimal? NewAllocateOffBudgetAmounts { get; set; }

            [MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }

            /// <summary>
            /// รายชื่อโครงการทั้งหมดที่อยู่ภายใต้ ค่าใช้จ่าย ในแต่ละปีงบประมาณ
            /// </summary>
            public List<ExpensesProjectProperty> Projects { get; set; }
        }


        public class ExpensesProjectProperty
        {
            /// <summary>
            /// รหัสโครงการที่สร้างโดยระบบ
            /// </summary>
            public int? PROJECT_ID { get; set; }

            /// <summary>
            /// ชื่อโครงการที่อยู่ภายใต้รายการค่าใช้จ่าย ในแต่ล่ะปีงบประมาณ
            /// </summary>
            public string PROJECT_NAME { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณสุทธิ ที่ได้รับจัดสรรจากรัฐิบาย
            /// </summary>
            public decimal? AllocateBudgetAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินนอก งปม. สุทธิ ที่ได้รับจัดสรรจากรัฐบาล
            /// </summary>
            public decimal? AllocateOffBudgetAmounts { get; set; }
        }
    }
}