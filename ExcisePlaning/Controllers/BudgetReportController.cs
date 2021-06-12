using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml;
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
    /// หน่วยงานภูมิภาคบันทึกผลการใช้จ่ายเงินงบประมาณ
    /// ที่ได้รับจัดสรรจากส่วนกลาง (กรมสรรพสามิต)
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReportController : Controller
    {
        // GET: BudgetReport
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_REPORT_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_REPORT_MENU;
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

            //ViewBag.DepAuthorize = userAuthorizeProfile.DepAuthorize;
            ViewBag.DefaultYear = AppUtils.GetCurrYear();
            //ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            //ViewBag.CurrMonthNo = DateTime.Now.Month;
            ViewBag.DefaultDepartmentId = userAuthorizeProfile.DepId;
            ViewBag.CanReportBudgetMonthsNo = BudgetUtils.GetCanReportBudgetMonthsNo();
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.AreaName = db.T_AREAs.Where(e => e.AREA_ID.Equals(userAuthorizeProfile.AreaId)).Select(e => e.AREA_NAME).FirstOrDefault();
                ViewBag.DepartmentName = userAuthorizeProfile.DepName;

                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();
                // ผลผลิต
                ViewBag.Produces = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new ProduceShortFieldProperty()
                {
                    PRODUCE_ID = e.PRODUCE_ID,
                    PRODUCE_NAME = e.PRODUCE_NAME
                }).ToList();
                // กิจกรรม
                ViewBag.Activities = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new ActivityShortFieldProperty()
                {
                    ACTIVITY_ID = e.ACTIVITY_ID,
                    ACTIVITY_NAME = e.ACTIVITY_NAME
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
        /// ค้นหารายการค่าใช้จ่ายที่ได้รับจัดสรร เพื่อรายงานผลการใช้จ่ายงบประมาณ
        /// แสดงผลข้อมูลตาม Profile ผู้ใช้งานที่ทำรายการ ไม่แยก Super User
        /// </summary>
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="budgetType">1 = เงินงบ, 2 = เงินนอก</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int? budgetType)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var expr = db.V_GET_DEPARTMENT_EXPENSES_BUDGET_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear)
                    && e.DEP_ID.Equals(userAuthorizeProfile.DepId));

                // แผนงาน
                if (null != planId)
                    expr = expr.Where(e => e.PLAN_ID == planId);

                // ผลผลิต
                if (null != produceId)
                    expr = expr.Where(e => e.PRODUCE_ID == produceId);

                // กิจกรรม
                if (null != activityId)
                    expr = expr.Where(e => e.ACTIVITY_ID == activityId);

                // งบรายจ่าย
                if (null != budgetTypeId)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));

                // หมวดค่าใช้จ่าย
                if (null != expensesGroupId)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));

                // รายการค่าใช้จ่าย
                if (null != expensesId)
                    expr = expr.Where(e => e.EXPENSES_ID.Equals(expensesId));

                if (null != budgetType && budgetType.Value.Equals(1))
                    expr = expr.Where(e => (e.ALLOCATE_BUDGET_AMOUNT > 0 || e.EX_GRP_ALLOCATE_BUDGET_AMOUNT > 0));
                else if (null != budgetType && budgetType.Value.Equals(2))
                    expr = expr.Where(e => (e.ALLOCATE_OFF_BUDGET_AMOUNT > 0 || e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT > 0));

                var finalExpr = expr.Select(e => new
                {
                    e.SEQ_ID, // Primary key รายการผลการใช้จ่ายที่ได้รับจัดสรรจาก กรมสรรพสามิต
                    e.YR,
                    e.DEP_ID,
                    e.DEP_NAME,

                    e.DEP_NET_BUDGET_AMOUNT, // งบประมาณที่ได้รับจัดสรรสุทธิ ของหน่วยงาน
                    e.DEP_NET_USE_BUDGET_AMOUNT, // ใช้จ่ายงบประมาณ สะสม ของหน่วยงาน
                    e.DEP_NET_REMAIN_BUDGET_AMOUNT, // งบประมาณ คงเหลือสุทธิ ของหน่วยงาน

                    e.LATEST_REPORT_DATETIME,
                    e.LATEST_REPORT_NAME,
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
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.ALLOCATE_EXPENSES_GROUP_ID, // อ้างอิงรายการจัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.PROJECT_ID,
                    e.PROJECT_NAME,

                    e.ALLOCATE_BUDGET_AMOUNT,
                    e.USE_BUDGET_AMOUNT,
                    e.REMAIN_BUDGET_AMOUNT,

                    e.ALLOCATE_OFF_BUDGET_AMOUNT,
                    e.USE_OFF_BUDGET_AMOUNT,
                    e.REMAIN_OFF_BUDGET_AMOUNT,

                    e.NET_BUDGET_AMOUNT,
                    e.NET_USE_BUDGET_AMOUNT,
                    e.NET_REMAIN_BUDGET_AMOUNT,

                    // เงินงบ ที่จัดสรรเป็นก้อน
                    e.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                    e.EX_GRP_USE_BUDGET_AMOUNT,
                    e.EX_GRP_REMAIN_BUDGET_AMOUNT,
                    // เงินนอก ที่จัดสรรเป็นก้อน
                    e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                    e.EX_GRP_USE_OFF_BUDGET_AMOUNT,
                    e.EX_GRP_REMAIN_OFF_BUDGET_AMOUNT,
                    // เงินงบ + เงินนอก ที่จัดสรรเป็นก้อน
                    e.EX_GRP_NET_BUDGET_AMOUNT,
                    e.EX_GRP_NET_USE_BUDGET_AMOUNT,
                    e.EX_GRP_NET_REMAIN_BUDGET_AMOUNT,

                    e.REPORT_JAN,
                    e.REPORT_FEB,
                    e.REPORT_MAR,
                    e.REPORT_APR,
                    e.REPORT_MAY,
                    e.REPORT_JUN,
                    e.REPORT_JUL,
                    e.REPORT_AUG,
                    e.REPORT_SEP,
                    e.REPORT_OCT,
                    e.REPORT_NOV,
                    e.REPORT_DEC
                }).AsEnumerable()
                .GroupBy(e => new
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
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.ALLOCATE_EXPENSES_GROUP_ID,
                    // จัดสรรเป็นก้อนตาม หมวดค่าใช้จ่าย
                    e.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                    e.EX_GRP_REMAIN_BUDGET_AMOUNT,
                    e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                    e.EX_GRP_REMAIN_OFF_BUDGET_AMOUNT
                }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.Key.EXPENSES_GROUP_ORDER_SEQ).Select(e => new
                {
                    GroupBy = e.Key,
                    GroupSummary = new
                    {
                        // เงินงบประมาณ
                        TOTAL_BUDGET_AMOUNT = e.Sum(x => x.ALLOCATE_BUDGET_AMOUNT) + e.Key.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                        TOTAL_USE_BUDGET_AMOUNT = e.Sum(x => x.USE_BUDGET_AMOUNT),
                        TOTAL_REMAIN_BUDGET_AMOUNT = e.Sum(x => x.ALLOCATE_BUDGET_AMOUNT) + e.Key.EX_GRP_ALLOCATE_BUDGET_AMOUNT - e.Sum(x => x.USE_BUDGET_AMOUNT),
                        // เงินนอกงบประมาณ
                        TOTAL_OFF_BUDGET_AMOUNT = e.Sum(x => x.ALLOCATE_OFF_BUDGET_AMOUNT) + e.Key.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                        TOTAL_USE_OFF_BUDGET_AMOUNT = e.Sum(x => x.USE_OFF_BUDGET_AMOUNT),
                        TOTAL_REMAIN_OFF_BUDGET_AMOUNT = e.Sum(x => x.ALLOCATE_OFF_BUDGET_AMOUNT) + e.Key.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT - e.Sum(x => x.USE_OFF_BUDGET_AMOUNT)
                    },
                    Expenses = e.OrderBy(x => x.EXPENSES_ORDER_SEQ).ThenBy(x => x.PROJECT_ID).ToList()
                }).ToList();


                // สรุปข้อมูลภาพรวมของหน่วยงาน ที่กำลังแสดงผลบันทึกผลการใช้จ่าย
                var exprSummaries = expr.Select(e => new
                {
                    e.DEP_NET_BUDGET_AMOUNT,
                    e.DEP_NET_USE_BUDGET_AMOUNT,
                    e.DEP_NET_REMAIN_BUDGET_AMOUNT,
                    e.DEP_LATEST_ALLOCATE_DATETIME,
                    e.DEP_LATEST_ALLOCATE_NAME,
                    e.LATEST_REPORT_DATETIME,
                    e.LATEST_REPORT_NAME
                }).FirstOrDefault();
                return Json(new Dictionary<string, object>(2) { { "rows", finalExpr }, { "summaries", exprSummaries } }, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// โหลดแบบฟอร์ม แสดงผลประวัติการรายงานผลรายการค่าใช้จ่ายของหน่วยงาน
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalViewReportExpensesHistoryForm()
        {
            return View();
        }

        /// <summary>
        /// ค้นหาประวัติการรายงานผลการใช้จ่าย งปม. ของหน่วยงาน รายรายการ คชจ. รายเดือน
        /// </summary>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="monthNo"></param>
        /// <param name="yearNo"></param>
        /// <param name="depId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveReportExpenseHistory(int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, int monthNo, int yearNo, short budgetType, int depId)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_DEPARTMENT_REPORT_EXPENSES_HISTORies.Where(e => e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId
                    && e.MN == monthNo
                    && e.YR == yearNo
                    && e.BUDGET_TYPE.Equals(budgetType)
                    && e.DEP_ID == depId).OrderByDescending(e => e.REPORTED_DATETIME).Select(e => new
                    {
                        e.REPORT_ID,
                        e.REPORTED_NAME,
                        e.REPORTED_DATETIME,
                        e.REPORT_BUDGET_AMOUNT,
                        e.REPORT_CODE,
                        e.BUDGET_TYPE
                    }).ToList();
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ยกเลิก ประวัติการรายงานผลการใช้จ่าย งบประมาณ
        /// (ไม่สามารถยกเลิกของปี งปม. ก่อนหน้าได้)
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitRejectReportHistory(int reportId)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                Dictionary<string, string> res = new Dictionary<string, string>(1) { { "errorText", null } };
                var exprHistory = db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.Where(e => e.REPORT_ID.Equals(reportId) && e.ACTIVE == 1).FirstOrDefault();
                if (exprHistory == null)
                {
                    res["errorText"] = string.Format("ไม่พบประวัติการบันทึกผลการใช้จ่าย (เลขที่รายการ: {0})", reportId);
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                if (!AppUtils.CanChangeDataByIntervalYear(exprHistory.YR, AppUtils.GetCurrYear()))
                {
                    res["errorText"] = "ประวัติการบันทึกผลการใช้จ่ายงบประมาณเป็นของปีงบประมาณอื่น ไม่สามารถยกเลิกได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var reportBudgetAmounts = exprHistory.BUDGET_TYPE.Equals(1) ? exprHistory.REPORT_BUDGET_AMOUNT : decimal.Zero;
                var reportOffBudgetAmounts = exprHistory.BUDGET_TYPE.Equals(2) ? exprHistory.REPORT_BUDGET_AMOUNT : decimal.Zero;

                // ปรับปรุงยอดรายงานผล ภาพรวม หน่วยงาน
                // จัดสรรงบประมาณส่วน Master
                var exprAllocateMas = db.T_BUDGET_ALLOCATEs.Where(e => e.DEP_ID.Equals(exprHistory.DEP_ID) && e.YR.Equals(exprHistory.YR)).FirstOrDefault();
                if (null != exprAllocateMas)
                {
                    exprAllocateMas.NET_USE_BUDGET_AMOUNT -= exprHistory.REPORT_BUDGET_AMOUNT;
                    exprAllocateMas.NET_REMAIN_BUDGET_AMOUNT = exprAllocateMas.NET_BUDGET_AMOUNT - exprAllocateMas.NET_USE_BUDGET_AMOUNT;

                    exprAllocateMas.USE_BUDGET_AMOUNT -= reportBudgetAmounts;
                    exprAllocateMas.REMAIN_BUDGET_AMOUNT = exprAllocateMas.ALLOCATE_BUDGET_AMOUNT - exprAllocateMas.USE_BUDGET_AMOUNT;

                    exprAllocateMas.USE_OFF_BUDGET_AMOUNT -= reportOffBudgetAmounts;
                    exprAllocateMas.REMAIN_OFF_BUDGET_AMOUNT = exprAllocateMas.ALLOCATE_OFF_BUDGET_AMOUNT - exprAllocateMas.USE_OFF_BUDGET_AMOUNT;
                }

                // ปรับปรุงยอดรายงานผล รายการค่าใช้จ่าย
                // จัดสรรงบประมาณ รายการค่าใช้จ่าย
                var exprAllocateExpenses = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.DEP_ID.Equals(exprHistory.DEP_ID)
                    && e.YR.Equals(exprHistory.YR)
                    && e.ACTIVE.Equals(1)
                    && e.PLAN_ID == exprHistory.PLAN_ID
                    && e.PRODUCE_ID == exprHistory.PRODUCE_ID
                    && e.ACTIVITY_ID == exprHistory.ACTIVITY_ID
                    && e.BUDGET_TYPE_ID == exprHistory.BUDGET_TYPE_ID
                    && e.EXPENSES_GROUP_ID == exprHistory.EXPENSES_GROUP_ID
                    && e.EXPENSES_ID == exprHistory.EXPENSES_ID
                    && ((e.PROJECT_ID == null && exprHistory.PROJECT_ID == null) || (e.PROJECT_ID.Equals(exprHistory.PROJECT_ID)))).FirstOrDefault();
                if (null != exprAllocateExpenses)
                {
                    // เป็นการจัดสรรงบประมาณเป็นก้อน ตามหมวดค่าใช้จ่าย
                    if (!string.IsNullOrEmpty(exprAllocateExpenses.ALLOCATE_EXPENSES_GROUP_ID))
                    {
                        var exprDepAllocateGroup = db.T_BUDGET_ALLOCATE_EXPENSES_GROUPs.Where(e => e.ALLOCATE_EXPENSES_GROUP_ID.Equals(exprAllocateExpenses.ALLOCATE_EXPENSES_GROUP_ID)).FirstOrDefault();
                        if(null != exprDepAllocateGroup)
                        {
                            // ปรับปรุงยอดภาพรวม (เงินงบ + เงินนอก)
                            exprDepAllocateGroup.NET_USE_BUDGET_AMOUNT -= exprHistory.REPORT_BUDGET_AMOUNT;
                            exprDepAllocateGroup.NET_REMAIN_BUDGET_AMOUNT = exprDepAllocateGroup.NET_BUDGET_AMOUNT - exprDepAllocateGroup.NET_USE_BUDGET_AMOUNT;
                            // ปรับปรุงยอด เงินงบประมาณ
                            exprDepAllocateGroup.USE_BUDGET_AMOUNT -= reportBudgetAmounts;
                            exprDepAllocateGroup.REMAIN_BUDGET_AMOUNT = exprDepAllocateGroup.ALLOCATE_BUDGET_AMOUNT - exprDepAllocateGroup.USE_BUDGET_AMOUNT;
                            // ปรับปรุงยอด เงินนอกงบประมาณ
                            exprDepAllocateGroup.USE_OFF_BUDGET_AMOUNT -= reportOffBudgetAmounts;
                            exprDepAllocateGroup.REMAIN_OFF_BUDGET_AMOUNT = exprDepAllocateGroup.ALLOCATE_OFF_BUDGET_AMOUNT - exprDepAllocateGroup.USE_OFF_BUDGET_AMOUNT;

                            exprDepAllocateGroup.UPDATED_DATETIME = DateTime.Now;
                            exprDepAllocateGroup.UPDATED_ID = userAuthorizeProfile.EmpId;
                        }
                    }

                    // ปรับปรุงยอดคงเหลืองบประมาณ ของ ค่าใช้จ่าย/โครงการ
                    exprAllocateExpenses.NET_USE_BUDGET_AMOUNT -= exprHistory.REPORT_BUDGET_AMOUNT;
                    exprAllocateExpenses.NET_REMAIN_BUDGET_AMOUNT = exprAllocateExpenses.NET_BUDGET_AMOUNT - exprAllocateExpenses.NET_USE_BUDGET_AMOUNT;
                    // ปรับปรุงยอดคงเหลือ เฉพาะเงินงบประมาณ
                    exprAllocateExpenses.USE_BUDGET_AMOUNT -= reportBudgetAmounts;
                    exprAllocateExpenses.REMAIN_BUDGET_AMOUNT = exprAllocateExpenses.ALLOCATE_BUDGET_AMOUNT - exprAllocateExpenses.USE_BUDGET_AMOUNT;
                    // ปรับปรุงยอดคงเหลือ เฉพาะเงินนอกงบประมาณ
                    exprAllocateExpenses.USE_OFF_BUDGET_AMOUNT -= reportOffBudgetAmounts;
                    exprAllocateExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprAllocateExpenses.ALLOCATE_OFF_BUDGET_AMOUNT - exprAllocateExpenses.USE_OFF_BUDGET_AMOUNT;

                    exprAllocateExpenses.UPDATED_DATETIME = DateTime.Now;
                    exprAllocateExpenses.UPDATED_ID = userAuthorizeProfile.EmpId;

                    // ปรับปรุงยอด รายงานผลของเดือนที่ผู้ใช้กดยกเลิกประวัติ
                    if (exprHistory.MN == 1)
                        exprAllocateExpenses.REPORT_JAN -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 2)
                        exprAllocateExpenses.REPORT_FEB -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 3)
                        exprAllocateExpenses.REPORT_MAR -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 4)
                        exprAllocateExpenses.REPORT_APR -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 5)
                        exprAllocateExpenses.REPORT_MAY -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 6)
                        exprAllocateExpenses.REPORT_JUN -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 7)
                        exprAllocateExpenses.REPORT_JUL -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 8)
                        exprAllocateExpenses.REPORT_AUG -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 9)
                        exprAllocateExpenses.REPORT_SEP -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 10)
                        exprAllocateExpenses.REPORT_OCT -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 11)
                        exprAllocateExpenses.REPORT_NOV -= exprHistory.REPORT_BUDGET_AMOUNT;
                    else if (exprHistory.MN == 12)
                        exprAllocateExpenses.REPORT_DEC -= exprHistory.REPORT_BUDGET_AMOUNT;
                }

                // ยกเลิกประวัติการรายงานผล
                exprHistory.ACTIVE = -1;
                exprHistory.DELETED_DATETIME = DateTime.Now;
                exprHistory.DELETED_ID = userAuthorizeProfile.EmpId;

                db.SubmitChanges();

                return Json(res, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ตรวจสอบ ยอดเงิน ที่รายงานผลการเบิกจ่าย และ เลขที่เอกสารการเบิกจ่าย
        /// </summary>
        /// <param name="monthFieldName"></param>
        /// <param name="GFMISFieldName"></param>
        /// <param name="monthIndex">ลำดับเดือนที่เบิกจ่าย</param>
        /// <param name="budgetType">ประเภทงบ 1 = เงินงบ, 2 = เงินนอก</param>
        /// <param name="canReportBudgetMonthsNo">เดือนที่สามารถคีย์เบิกจ่ายได้ (จะเป็นเดือนปัจจุบัน หรือ เดือนก่อนหน้า)</param>
        /// <param name="reportAmounts">จำนวนที่ขอเบิกจ่าย</param>
        /// <param name="reportGFMISCode">เลขที่อ้างอิงเอกสาร</param>
        /// <param name="allocateBudgetAmounts">เงินงบ ที่ได้รับจัดสรร (บาท)</param>
        /// <param name="allocateOffBudgetAmounts">เงินนอกงบ ที่ได้รับจัดสรร (บาท)</param>
        /// <param name="cumulativeNetUseBudgetAmounts">ยอดใช้จ่ายสะสม เงินงบ + เงินนอก</param>
        /// <param name="cumulativeUseBudgetAmounts">ยอดใช้จ่ายสะสม เงินงบ</param>
        /// <param name="cumulativeUseOffBudgetAmounts">ยอดใช้จ่ายสะสม เงินนอกงบ</param>
        /// <param name="errorItem"></param>
        private void VerifyReportBudgetByMonth(string monthFieldName, string GFMISFieldName, int monthIndex, int budgetType, List<int> canReportBudgetMonthsNo, decimal? reportAmounts, string reportGFMISCode, decimal allocateBudgetAmounts, decimal allocateOffBudgetAmounts, decimal cumulativeNetUseBudgetAmounts, decimal cumulativeUseBudgetAmounts, decimal cumulativeUseOffBudgetAmounts, ref Dictionary<string, ModelValidateErrorProperty> errorItem)
        {
            if (reportAmounts == null)
                return;

            bool canReportBudget = canReportBudgetMonthsNo.IndexOf(monthIndex) > -1;
            decimal netAllocateBudgetAmounts = allocateBudgetAmounts + allocateOffBudgetAmounts;
            if (!canReportBudget)
                errorItem.Add(monthFieldName, new ModelValidateErrorProperty(monthFieldName, new List<string>() { "เดือนที่บันทึกผลการใช้จ่ายไม่ถูกต้อง" }));
            else if (cumulativeNetUseBudgetAmounts.CompareTo(netAllocateBudgetAmounts) == 1
                || (budgetType.Equals(1) && cumulativeUseBudgetAmounts.CompareTo(allocateBudgetAmounts) == 1)
                || (budgetType.Equals(2) && cumulativeUseOffBudgetAmounts.CompareTo(allocateOffBudgetAmounts) == 1))
                errorItem.Add(monthFieldName, new ModelValidateErrorProperty(monthFieldName, new List<string>() { "งบประมาณไม่เพียงพอ" }));
            else if (canReportBudget)
            {
                if (string.IsNullOrEmpty(reportGFMISCode))
                    errorItem.Add(GFMISFieldName, new ModelValidateErrorProperty(GFMISFieldName, new List<string>(1) { "ระบุเลขที่ขอเบิก GFMIS ก่อน" }));
                else if (Regex.Replace(reportGFMISCode, @"[^\d]", "", RegexOptions.IgnoreCase).Length != 10)
                    errorItem.Add(GFMISFieldName, new ModelValidateErrorProperty(GFMISFieldName, new List<string>(1) { "เลขที่ขอเบิก GFMIS ไม่ถูกต้อง" }));
            }
        }

        /// <summary>
        /// บันทึกผลการใช้จ่ายเงินงบประมาณ ของหน่วยงาน
        /// 1. ในการบันทึกผลการใช้จ่ายแต่ละครัั้งจำเป็นต้อง เลือกประเภทงบ (เงินงบ, เงินนอกงบ) 
        /// เพื่อให้สามารถรายงานติดต่อการใช้จ่ายเงินจากแหล่งต่างๆที่ละเอียดมากขึ้น
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitSave(BudgetReportFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) {
                { "errorText", null },
                { "errors", null }
            };

            if (model.FiscalYear.CompareTo(AppUtils.GetCurrYear()) == -1)
            {
                res["errorText"] = "ปีงบประมาณที่เลือกไม่ใช่ปีงบประมาณปัจจุบัน ไม่สามารถรายงานผลได้";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบค่าที่ส่งจากฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var canReportBudgetMonthsNo = BudgetUtils.GetCanReportBudgetMonthsNo();
                var isReportBudget = model.BudgetType.Value.Equals(1); // เป็นการบันทึกผลการใช้จ่าย จากเงินงบประมาณใช่หรือไม่

                // ตรวจสอบค่า ที่ขอบันทึกผลการใช้จ่าย
                // ระบุข้อมูลถูกต้อง และ ครบถ้วน หรือไม่ เช่น ยอดรายงานผลการใช้จ่ายไม่เกิน ยอดที่รับจัดสรร รายงานผลได้ถูก
                var exprTemps = db.V_GET_DEPARTMENT_EXPENSES_BUDGET_INFORMATIONs.Where(e => e.YR.Equals(model.FiscalYear)
                    && e.DEP_ID.Equals(userAuthorizeProfile.DepId)
                    ).Select(e => new
                    {
                        e.SEQ_ID,

                        // จัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย
                        e.ALLOCATE_EXPENSES_GROUP_ID,
                        e.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                        e.EX_GRP_USE_BUDGET_AMOUNT,
                        e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                        e.EX_GRP_USE_OFF_BUDGET_AMOUNT,
                        e.EX_GRP_NET_BUDGET_AMOUNT,
                        e.EX_GRP_NET_USE_BUDGET_AMOUNT,

                        // เงินงบ
                        e.ALLOCATE_BUDGET_AMOUNT,
                        e.USE_BUDGET_AMOUNT,

                        // เงินนอก
                        e.ALLOCATE_OFF_BUDGET_AMOUNT,
                        e.USE_OFF_BUDGET_AMOUNT,

                        // เงินงบ + เงินนอก ภาพรวมรายการค่าใช้จ่าย
                        e.NET_BUDGET_AMOUNT,
                        e.NET_USE_BUDGET_AMOUNT
                    }).AsEnumerable()
                    .GroupBy(e => e.ALLOCATE_EXPENSES_GROUP_ID)
                    .Select(e => new
                    {
                        GroupBy = e.Key,
                        Expenses = e.Select(x => new DepartmentBudgetAllocateProperty()
                        {
                            SEQ_ID = x.SEQ_ID,
                            ALLOCATE_BUDGET_AMOUNT = x.ALLOCATE_EXPENSES_GROUP_ID == null ? x.ALLOCATE_BUDGET_AMOUNT : e.Sum(y => y.ALLOCATE_BUDGET_AMOUNT) + x.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                            USE_BUDGET_AMOUNT = x.ALLOCATE_EXPENSES_GROUP_ID == null ? x.USE_BUDGET_AMOUNT : x.EX_GRP_USE_BUDGET_AMOUNT,

                            ALLOCATE_OFF_BUDGET_AMOUNT = x.ALLOCATE_EXPENSES_GROUP_ID == null ? x.ALLOCATE_OFF_BUDGET_AMOUNT : e.Sum(y => y.ALLOCATE_OFF_BUDGET_AMOUNT) + x.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                            USE_OFF_BUDGET_AMOUNT = x.ALLOCATE_EXPENSES_GROUP_ID == null ? x.USE_OFF_BUDGET_AMOUNT : x.EX_GRP_USE_OFF_BUDGET_AMOUNT,

                            NET_BUDGET_AMOUNT = x.ALLOCATE_EXPENSES_GROUP_ID == null ? x.NET_BUDGET_AMOUNT : e.Sum(y => y.NET_BUDGET_AMOUNT) + x.EX_GRP_NET_BUDGET_AMOUNT,
                            NET_USE_BUDGET_AMOUNT = x.ALLOCATE_EXPENSES_GROUP_ID == null ? x.NET_USE_BUDGET_AMOUNT : x.EX_GRP_NET_USE_BUDGET_AMOUNT
                        }).ToList()
                    }).ToList();
                IEnumerable<DepartmentBudgetAllocateProperty> exprAllocateExpenses = new List<DepartmentBudgetAllocateProperty>();
                exprTemps.ForEach(exprTemp => {
                    exprAllocateExpenses = exprAllocateExpenses.Concat(exprTemp.Expenses);
                });


                Dictionary<string, Dictionary<string, ModelValidateErrorProperty>> expensesErrors = new Dictionary<string, Dictionary<string, ModelValidateErrorProperty>>();
                model.Expenses.ForEach(expensesItem =>
                {
                    var errorItem = new Dictionary<string, ModelValidateErrorProperty>();
                    //var exprAllocate = exprAllocateExpenses.Where(e => e.PLAN_ID == expensesItem.PLAN_ID
                    //    && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                    //    && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                    //    && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                    //    && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                    //    && e.EXPENSES_ID == expensesItem.EXPENSES_ID).FirstOrDefault();
                    string errorKey = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}", expensesItem.PLAN_ID
                        , expensesItem.PRODUCE_ID, expensesItem.ACTIVITY_ID
                        , expensesItem.BUDGET_TYPE_ID, expensesItem.EXPENSES_GROUP_ID
                        , expensesItem.EXPENSES_ID, expensesItem.PROJECT_ID);

                    var exprAllocate = exprAllocateExpenses.Where(e => e.SEQ_ID.Equals(expensesItem.SEQ_ID)).FirstOrDefault();
                    var totalUseBudgetAmounts = exprAllocate.USE_BUDGET_AMOUNT; // ใช้จ่ายเงินงบประมาณ สะสม
                    var totalUseOffBudgetAmounts = exprAllocate.USE_OFF_BUDGET_AMOUNT; // ใช้จ่ายเงินนอกงบประมาณ สะสม
                    var netUseBudgetAmounts = exprAllocate.NET_USE_BUDGET_AMOUNT; // ใช้จ่ายเงินงบประมาณภาพรวม (เงินงบ + เงินนอก) สะสม

                    decimal budgetAmounts = decimal.Zero;
                    decimal reportBudgetAmounts = decimal.Zero;
                    decimal reportOffBudgetAmounts = decimal.Zero;

                    if (!string.IsNullOrEmpty(expensesItem.RemarkText) && expensesItem.RemarkText.Length > 150)
                        errorItem.Add("RemarkText", new ModelValidateErrorProperty("RemarkText", new List<string>(1) { "ความยาวไม่เกิน 150 ตัวอักษร" }));

                    // รายงานผลเดือน ต.ค.
                    budgetAmounts = expensesItem.NewReportOctAmounts == null ? decimal.Zero : expensesItem.NewReportOctAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportOctAmounts", "ReportCodeOct", 10, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportOctAmounts, expensesItem.ReportCodeOct
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน พ.ย.
                    budgetAmounts = expensesItem.NewReportNovAmounts == null ? decimal.Zero : expensesItem.NewReportNovAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportNovAmounts", "ReportCodeNov", 11, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportNovAmounts, expensesItem.ReportCodeNov
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน ธ.ค.
                    budgetAmounts = expensesItem.NewReportDecAmounts == null ? decimal.Zero : expensesItem.NewReportDecAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportDecAmounts", "ReportCodeDec", 12, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportDecAmounts, expensesItem.ReportCodeDec
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน ม.ค.
                    budgetAmounts = expensesItem.NewReportJanAmounts == null ? decimal.Zero : expensesItem.NewReportJanAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportJanAmounts", "ReportCodeJan", 1, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportJanAmounts, expensesItem.ReportCodeJan
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน ก.พ.
                    budgetAmounts = expensesItem.NewReportFebAmounts == null ? decimal.Zero : expensesItem.NewReportFebAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportFebAmounts", "ReportCodeFeb", 2, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportFebAmounts, expensesItem.ReportCodeFeb
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน มี.ค.
                    budgetAmounts = expensesItem.NewReportMarAmounts == null ? decimal.Zero : expensesItem.NewReportMarAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportMarAmounts", "ReportCodeMar", 3, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportMarAmounts, expensesItem.ReportCodeMar
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน เม.ย.
                    budgetAmounts = expensesItem.NewReportAprAmounts == null ? decimal.Zero : expensesItem.NewReportAprAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportAprAmounts", "ReportCodeApr", 4, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportAprAmounts, expensesItem.ReportCodeApr
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน พ.ค.
                    budgetAmounts = expensesItem.NewReportMayAmounts == null ? decimal.Zero : expensesItem.NewReportMayAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportMayAmounts", "ReportCodeMay", 5, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportMayAmounts, expensesItem.ReportCodeMay
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน มิ.ย.
                    budgetAmounts = expensesItem.NewReportJunAmounts == null ? decimal.Zero : expensesItem.NewReportJunAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportJunAmounts", "ReportCodeJun", 6, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportJunAmounts, expensesItem.ReportCodeJun
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน ก.ค.
                    budgetAmounts = expensesItem.NewReportJulAmounts == null ? decimal.Zero : expensesItem.NewReportJulAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportJulAmounts", "ReportCodeJul", 7, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportJulAmounts, expensesItem.ReportCodeJul
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน ส.ค.
                    budgetAmounts = expensesItem.NewReportAugAmounts == null ? decimal.Zero : expensesItem.NewReportAugAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportAugAmounts", "ReportCodeAug", 8, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportAugAmounts, expensesItem.ReportCodeAug
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    // รายงานผลเดือน ก.ย.
                    budgetAmounts = expensesItem.NewReportSepAmounts == null ? decimal.Zero : expensesItem.NewReportSepAmounts.Value;
                    reportBudgetAmounts = isReportBudget ? budgetAmounts : decimal.Zero;
                    reportOffBudgetAmounts = !isReportBudget ? budgetAmounts : decimal.Zero;
                    netUseBudgetAmounts += budgetAmounts;
                    totalUseBudgetAmounts += reportBudgetAmounts;
                    totalUseOffBudgetAmounts += reportOffBudgetAmounts;
                    VerifyReportBudgetByMonth("NewReportSepAmounts", "ReportCodeSep", 9, model.BudgetType.Value
                            , canReportBudgetMonthsNo, expensesItem.NewReportSepAmounts, expensesItem.ReportCodeSep
                            , exprAllocate.ALLOCATE_BUDGET_AMOUNT, exprAllocate.ALLOCATE_OFF_BUDGET_AMOUNT
                            , netUseBudgetAmounts, totalUseBudgetAmounts, totalUseOffBudgetAmounts
                            , ref errorItem);

                    expensesErrors.Add(errorKey, errorItem.Any() ? errorItem : null);
                });
                if (expensesErrors.Any(e => null != e.Value))
                {
                    res["errors"] = new Dictionary<string, object>() { { "Expenses", expensesErrors } };
                    return Json(res, JsonRequestBehavior.DenyGet);
                };


                // งบประมาณของหน่วยงาน
                var exprAllocateMas = db.T_BUDGET_ALLOCATEs.Where(e => e.YR.Equals(model.FiscalYear)
                    && e.DEP_ID.Equals(userAuthorizeProfile.DepId)).FirstOrDefault();

                // รายงานผลการใช้จ่าย ในแต่ละรายการค่าใช้จ่าย
                model.Expenses.ForEach(expensesItem =>
                {
                    //var exprExpenses = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                    //        && e.DEP_ID.Equals(userAuthorizeProfile.DepId)
                    //        && e.YR.Equals(model.FiscalYear)
                    //        && e.PLAN_ID == expensesItem.PLAN_ID
                    //        && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                    //        && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                    //        && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                    //        && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                    //        && e.EXPENSES_ID == expensesItem.EXPENSES_ID).FirstOrDefault();
                    var exprExpenses = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                        && e.SEQ_ID.Equals(expensesItem.SEQ_ID)).FirstOrDefault();
                    if (exprExpenses != null)
                    {
                        // ไตรมาส 1
                        exprExpenses.REPORT_OCT = AddNewValue(exprExpenses.REPORT_OCT, expensesItem.NewReportOctAmounts);
                        exprExpenses.REPORT_NOV = AddNewValue(exprExpenses.REPORT_NOV, expensesItem.NewReportNovAmounts);
                        exprExpenses.REPORT_DEC = AddNewValue(exprExpenses.REPORT_DEC, expensesItem.NewReportDecAmounts);
                        // ไตรมาส 2
                        exprExpenses.REPORT_JAN = AddNewValue(exprExpenses.REPORT_JAN, expensesItem.NewReportJanAmounts);
                        exprExpenses.REPORT_FEB = AddNewValue(exprExpenses.REPORT_FEB, expensesItem.NewReportFebAmounts);
                        exprExpenses.REPORT_MAR = AddNewValue(exprExpenses.REPORT_MAR, expensesItem.NewReportMarAmounts);
                        // ไตรมาส 3
                        exprExpenses.REPORT_APR = AddNewValue(exprExpenses.REPORT_APR, expensesItem.NewReportAprAmounts);
                        exprExpenses.REPORT_MAY = AddNewValue(exprExpenses.REPORT_MAY, expensesItem.NewReportMayAmounts);
                        exprExpenses.REPORT_JUN = AddNewValue(exprExpenses.REPORT_JUN, expensesItem.NewReportJunAmounts);
                        // ไตรมาส 4
                        exprExpenses.REPORT_JUL = AddNewValue(exprExpenses.REPORT_JUL, expensesItem.NewReportJulAmounts);
                        exprExpenses.REPORT_AUG = AddNewValue(exprExpenses.REPORT_AUG, expensesItem.NewReportAugAmounts);
                        exprExpenses.REPORT_SEP = AddNewValue(exprExpenses.REPORT_SEP, expensesItem.NewReportSepAmounts);

                        // ยอดเงินที่ระบุสำหรับการรายการผลการใช้จ่าย
                        var sumNewReportAmounts = GetDecimalNullableVal(expensesItem.NewReportOctAmounts) + GetDecimalNullableVal(expensesItem.NewReportNovAmounts) + GetDecimalNullableVal(expensesItem.NewReportDecAmounts)
                            + GetDecimalNullableVal(expensesItem.NewReportJanAmounts) + GetDecimalNullableVal(expensesItem.NewReportFebAmounts) + GetDecimalNullableVal(expensesItem.NewReportMarAmounts)
                            + GetDecimalNullableVal(expensesItem.NewReportAprAmounts) + GetDecimalNullableVal(expensesItem.NewReportMayAmounts) + GetDecimalNullableVal(expensesItem.NewReportJunAmounts)
                            + GetDecimalNullableVal(expensesItem.NewReportJulAmounts) + GetDecimalNullableVal(expensesItem.NewReportAugAmounts) + GetDecimalNullableVal(expensesItem.NewReportSepAmounts);

                        // สรุปผลการใช้จ่ายภาพรวมของ รายการค่าใช้จ่าย/โครงการ
                        exprExpenses.NET_USE_BUDGET_AMOUNT += sumNewReportAmounts;
                        exprExpenses.NET_REMAIN_BUDGET_AMOUNT = exprExpenses.NET_BUDGET_AMOUNT - exprExpenses.NET_USE_BUDGET_AMOUNT;

                        // ผลการใช้จ่ายเงินงบประมาณ
                        exprExpenses.USE_BUDGET_AMOUNT += isReportBudget ? sumNewReportAmounts : decimal.Zero;
                        exprExpenses.REMAIN_BUDGET_AMOUNT = exprExpenses.ALLOCATE_BUDGET_AMOUNT - exprExpenses.USE_BUDGET_AMOUNT;

                        // ผลการใช้จ่ายเงินนอกงบประมาณ
                        exprExpenses.USE_OFF_BUDGET_AMOUNT += !isReportBudget ? sumNewReportAmounts : decimal.Zero;
                        exprExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprExpenses.ALLOCATE_OFF_BUDGET_AMOUNT - exprExpenses.USE_OFF_BUDGET_AMOUNT;

                        // ผลการใช้จ่ายงบประมาณภาพรวมของหน่วยงาน
                        exprAllocateMas.USE_BUDGET_AMOUNT += isReportBudget ? sumNewReportAmounts : decimal.Zero;
                        exprAllocateMas.USE_OFF_BUDGET_AMOUNT += !isReportBudget ? sumNewReportAmounts : decimal.Zero;
                        exprAllocateMas.NET_USE_BUDGET_AMOUNT += sumNewReportAmounts;


                        // จัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย
                        string allocateExpensesGroupId = exprExpenses.ALLOCATE_EXPENSES_GROUP_ID;
                        if (!string.IsNullOrEmpty(allocateExpensesGroupId))
                        {
                            var exprSeeks = db.GetChangeSet().Updates.Where(e => e.GetType() == typeof(T_BUDGET_ALLOCATE_EXPENSES_GROUP)).Select(e => (T_BUDGET_ALLOCATE_EXPENSES_GROUP)e).ToList();
                            var exprAllocateGroup = exprSeeks.Where(e => e.ALLOCATE_EXPENSES_GROUP_ID.Equals(allocateExpensesGroupId)).FirstOrDefault();
                            if (null == exprAllocateGroup)
                                exprAllocateGroup = db.T_BUDGET_ALLOCATE_EXPENSES_GROUPs.Where(e => e.ALLOCATE_EXPENSES_GROUP_ID.Equals(allocateExpensesGroupId)).FirstOrDefault();
                            if (null != exprAllocateGroup)
                            {
                                // ใช้จ่ายเงินงบประมาณ
                                exprAllocateGroup.USE_BUDGET_AMOUNT += isReportBudget ? sumNewReportAmounts : decimal.Zero;
                                exprAllocateGroup.REMAIN_BUDGET_AMOUNT = exprAllocateGroup.ALLOCATE_BUDGET_AMOUNT - exprAllocateGroup.USE_BUDGET_AMOUNT;

                                // ใช้จ่ายเงินนอกงบประมาณ
                                exprAllocateGroup.USE_OFF_BUDGET_AMOUNT += !isReportBudget ? sumNewReportAmounts : decimal.Zero;
                                exprAllocateGroup.REMAIN_OFF_BUDGET_AMOUNT = exprAllocateGroup.ALLOCATE_OFF_BUDGET_AMOUNT - exprAllocateGroup.USE_OFF_BUDGET_AMOUNT;

                                // ใช้จ่ายภาพรวม (เงินงบ & เงินนอกงบ)
                                exprAllocateGroup.NET_USE_BUDGET_AMOUNT += sumNewReportAmounts;
                                exprAllocateGroup.NET_REMAIN_BUDGET_AMOUNT = exprAllocateGroup.NET_BUDGET_AMOUNT - exprAllocateGroup.NET_USE_BUDGET_AMOUNT;

                                exprAllocateGroup.UPDATED_DATETIME = DateTime.Now;
                                exprAllocateGroup.UPDATED_ID = userAuthorizeProfile.EmpId;
                            }
                        }

                        // เก็บประวัติการรายงานผลเดือน ต.ค.
                        if (expensesItem.NewReportOctAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 10,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeOct,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportOctAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน พ.ย.
                        if (expensesItem.NewReportNovAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 11,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeNov,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportNovAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน ธ.ค.
                        if (expensesItem.NewReportDecAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 12,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeDec,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportDecAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน ม.ค.
                        if (expensesItem.NewReportJanAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 1,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeJan,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportJanAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน ก.พ.
                        if (expensesItem.NewReportFebAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 2,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeFeb,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportFebAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน มี.ค
                        if (expensesItem.NewReportMarAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 3,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeMar,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportMarAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน เม.ย.
                        if (expensesItem.NewReportAprAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 4,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeApr,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportAprAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน พ.ค.
                        if (expensesItem.NewReportMayAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 5,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeMay,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportMayAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน มิ.ย.
                        if (expensesItem.NewReportJunAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 6,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeJun,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportJunAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน ก.ค.
                        if (expensesItem.NewReportJulAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 7,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeJul,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportJulAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน ส.ค.
                        if (expensesItem.NewReportAugAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 8,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeAug,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportAugAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                        // เก็บประวัติการรายงานผลเดือน ก.ย.
                        if (expensesItem.NewReportSepAmounts != null)
                            db.T_BUDGET_REPORTED_USE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_REPORTED_USE_EXPENSES_HISTORY()
                            {
                                AREA_ID = userAuthorizeProfile.AreaId,
                                DEP_ID = userAuthorizeProfile.DepId,
                                SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                                YR = model.FiscalYear,
                                MN = 9,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = expensesItem.PROJECT_ID,
                                REPORT_CODE = expensesItem.ReportCodeSep,
                                REPORT_BUDGET_AMOUNT = expensesItem.NewReportSepAmounts.Value,
                                BUDGET_TYPE = model.BudgetType.Value,
                                REMARK_TEXT = expensesItem.RemarkText,
                                REPORTED_DATETIME = DateTime.Now,
                                REPORTED_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            });
                    }
                });


                // สรุปภาพรวมงบประมาณของหน่วยงาน
                exprAllocateMas.NET_REMAIN_BUDGET_AMOUNT = exprAllocateMas.NET_BUDGET_AMOUNT - exprAllocateMas.NET_USE_BUDGET_AMOUNT;
                exprAllocateMas.REMAIN_BUDGET_AMOUNT = exprAllocateMas.ALLOCATE_BUDGET_AMOUNT - exprAllocateMas.USE_BUDGET_AMOUNT;
                exprAllocateMas.REMAIN_OFF_BUDGET_AMOUNT = exprAllocateMas.ALLOCATE_OFF_BUDGET_AMOUNT - exprAllocateMas.USE_OFF_BUDGET_AMOUNT;
                exprAllocateMas.LATEST_REPORT_DATETIME = DateTime.Now;
                exprAllocateMas.LATEST_REPORT_ID = userAuthorizeProfile.EmpId;


                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        private decimal? AddNewValue(decimal? sourceVal, decimal? newVal)
        {
            if (newVal != null)
            {
                if (sourceVal == null)
                    sourceVal = decimal.Zero;
                sourceVal += newVal;
            }

            return sourceVal;
        }

        public decimal GetDecimalNullableVal(decimal? value)
        {
            return value == null ? decimal.Zero : value.Value;
        }


        public class DepartmentBudgetAllocateProperty
        {
            public int SEQ_ID { get; set; }
            public decimal ALLOCATE_BUDGET_AMOUNT { get; set; }
            public decimal USE_BUDGET_AMOUNT { get; set; }

            public decimal ALLOCATE_OFF_BUDGET_AMOUNT { get; set; }
            public decimal USE_OFF_BUDGET_AMOUNT { get; set; }
            public decimal NET_BUDGET_AMOUNT { get; set; }
            public decimal NET_USE_BUDGET_AMOUNT { get; set; }
        }


        public class BudgetReportFormMapper
        {
            /// <summary>
            /// ปีงบประมาณ ที่ทำการบันทึกผลการใช้จ่ายงบประมาณ
            /// </summary>
            public short FiscalYear { get; set; }

            /// <summary>
            /// แหล่งเงินที่ใช้จ่าย งบประมาณ
            /// จำเป็นต้องระบุแหล่งเงินที่ใช้จ่าย เนื่องจากต้องนำไปหักออกจาก แหล่งเงิน
            /// 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ
            /// </summary>
            public short? BudgetType { get; set; }

            /// <summary>
            /// หน่วยงานที่บันทึกผลการใช้จ่าย
            /// </summary>
            public int DepId { get; set; }

            /// <summary>
            /// รายการผลการใช้จ่าย ที่บันทึกผลการใช้จ่ายงบประมาณ
            /// </summary>
            [Required(ErrorMessage = "ไม่พบรายการค่าใช้จ่ายที่รายงานผลการใช้จ่าย")]
            public List<ReportExpensesProperty> Expenses { get; set; }
        }

        public class ReportExpensesProperty
        {
            /// <summary>
            /// Primary key รายการค่าใช้จ่ายที่ได้รับจัดสรรจากกรมสรรพสามิต
            /// </summary>
            public int SEQ_ID { get; set; }

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
            /// งบรายจ่าย (งบลงทุน งบอุดหนุน ฯลฯ)
            /// </summary>
            public int BUDGET_TYPE_ID { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_GROUP_ID { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_ID { get; set; }

            /// <summary>
            /// โครงการที่อยู่ภายใต้ ค่าใช้จ่าย
            /// </summary>
            public int? PROJECT_ID { get; set; }

            /// <summary>
            /// หมายเหตุ
            /// </summary>
            public string RemarkText { get; set; }


            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนตุลาคม
            /// </summary>
            public decimal? NewReportOctAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ตุลาคม
            /// </summary>
            public string ReportCodeOct { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนพฤจิกายน
            /// </summary>
            public decimal? NewReportNovAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน พ.ย.
            /// </summary>
            public string ReportCodeNov { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนธันวาคม
            /// </summary>
            public decimal? NewReportDecAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ธ.ค.
            /// </summary>
            public string ReportCodeDec { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนมกราคม
            /// </summary>
            public decimal? NewReportJanAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ม.ค.
            /// </summary>
            public string ReportCodeJan { get; set; }


            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนกุมภาพันธ์
            /// </summary>
            public decimal? NewReportFebAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ก.พ.
            /// </summary>
            public string ReportCodeFeb { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนมีนาคม
            /// </summary>
            public decimal? NewReportMarAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ม.ค.
            /// </summary>
            public string ReportCodeMar { get; set; }


            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนเมษายน
            /// </summary>
            public decimal? NewReportAprAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน เม.ย.
            /// </summary>
            public string ReportCodeApr { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนพฤษาคม
            /// </summary>
            public decimal? NewReportMayAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน พ.ค.
            /// </summary>
            public string ReportCodeMay { get; set; }


            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนมิถุนายน
            /// </summary>
            public decimal? NewReportJunAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน มิ.ย.
            /// </summary>
            public string ReportCodeJun { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนกรกฏาคม
            /// </summary>
            public decimal? NewReportJulAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ก.ค.
            /// </summary>
            public string ReportCodeJul { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนสิงหาคม
            /// </summary>
            /// 
            public decimal? NewReportAugAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ส.ค.
            /// </summary>
            public string ReportCodeAug { get; set; }

            /// <summary>
            /// รายงานผลการใช้จ่ายเดือนกันยายน
            /// </summary>
            public decimal? NewReportSepAmounts { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก GFMIS ของเดือน ก.ย.
            /// </summary>
            public string ReportCodeSep { get; set; }
        }
    }
}