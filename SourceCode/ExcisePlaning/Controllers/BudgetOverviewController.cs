using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// สรุปภาพรวม กิจกรรมการได้มาและการใช้จ่ายของเงินงบประมาณในแต่ละปีงบประมาณ
    /// เช่น ได้รับจัดสรรจากรัฐบาล เงินประจำงวด จัดสรร กันเงิน คงเหลือ และ สามารถค้นหาแยกย่อยลงรายละเอียดส่วน แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย ค่าใช้จ่าย เป็นต้น
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetOverviewController : Controller
    {
        // GET: BudgetOverview
        public ActionResult GetForm(string pageType)
        {
            if (!"all".Equals(pageType) && !"budget".Equals(pageType) && !"off_budget".Equals(pageType))
                return RedirectToAction("Index", "Dashboard");
            string currMenuConst = AppConfigConst.MENU_CONST_SUMMARY_OVERALL_BOTH_BUDGET_MENU;
            if ("budget".Equals(pageType))
                currMenuConst = AppConfigConst.MENU_CONST_SUMMARY_OVERALL_BUDGET_MENU;
            else if ("off_budget".Equals(pageType))
                currMenuConst = AppConfigConst.MENU_CONST_SUMMARY_OVERALL_OFF_BUDGET_MENU;

            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(currMenuConst);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = currMenuConst;
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
                QueryString = menuItem.QueryString,
                ActionName = menuItem.ActionName
            });
            ViewBag.Breadcrumps = breadcrumps;

            ViewBag.PageType = pageType; // all, budget, off_budget
            ViewBag.DefaultBudgetType = 1;
            if ("off_budget".Equals(pageType))
                ViewBag.DefaultBudgetType = 2;

            ViewBag.DefaultYear = AppUtils.GetCurrYear();
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new PlanShortFieldProperty()
                    {
                        PLAN_ID = e.PLAN_ID,
                        PLAN_NAME = e.PLAN_NAME
                    }).ToList();
                // ผลผลิต
                ViewBag.Produces = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new ProduceShortFieldProperty()
                    {
                        PRODUCE_ID = e.PRODUCE_ID,
                        PRODUCE_NAME = e.PRODUCE_NAME
                    }).ToList();
                // กิจกรรม
                ViewBag.Activities = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new ActivityShortFieldProperty()
                    {
                        ACTIVITY_ID = e.ACTIVITY_ID,
                        ACTIVITY_NAME = e.ACTIVITY_NAME
                    }).ToList();
                // งบรายจ่าย
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new BudgetTypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();
            }

            return View();
        }

        /// <summary>
        /// สรุปงบประมาณในแต่ละปีของกรมสรรพสามิต โดยแยกสรุปเป็น เงินงบประมาณ หรือ เงินนอกงบประมาณ
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int budgetType, int pageIndex, int pageSize)
        {
            BudgetOverviewProperty budgetOverview = new BudgetOverviewProperty();
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null,
                responseOpts = budgetOverview
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprBudgetExpenses = db.V_GET_SUMMARY_OVERALL_BUDGETs.Where(e => e.YR.Equals(fiscalYear));
                exprBudgetExpenses = exprBudgetExpenses.Where(e => (e.PROJECT_ID == null || (e.PROJECT_ID != null && e.PROJECT_FOR_TYPE.Equals(budgetType))));

                if (null != planId)
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => e.EXPENSES_ID.Equals(expensesId));
                exprBudgetExpenses = exprBudgetExpenses.OrderBy(e => e.PLAN_ORDER_SEQ)
                    .ThenBy(e => e.PRODUCE_ORDER_SEQ)
                    .ThenBy(e => e.ACTIVITY_ORDER_SEQ)
                    .ThenBy(e => e.BUDGET_TYPE_ORDER_SEQ)
                    .ThenBy(e => e.EXPENSES_GROUP_ORDER_SEQ)
                    .ThenBy(e => e.EXPENSES_ORDER_SEQ);


                // สรุปยอดภาพรวม ใช้ข้อมูลแต่ในรายการค่าใช้จ่ายสรุปเพราะในหน้าจอมีให้ค้นหารายการ แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย ...
                if (exprBudgetExpenses.Any())
                {
                    // จัดสรรงบประมาณเป็นก้อน ตามหมวดค่าใช้จ่าย
                    var exprAllocateBudgetGroup = exprBudgetExpenses.GroupBy(e => new
                    {
                        e.PLAN_ID,
                        e.PRODUCE_ID,
                        e.ACTIVITY_ID,
                        e.BUDGET_TYPE_ID,
                        e.EXPENSES_GROUP_ID,
                        e.DEP_GRP_ALLOCATE_BUDGET_AMOUNT,
                        e.DEP_GRP_ALLOCATE_OFF_BUDGET_AMOUNT
                    }).Select(e => e.Key).ToList();

                    // จัดกลุ่มข้อมูล ก่อนนำไปสรุปยอดของกรมสรรพสามิต
                    // เนื่องจากผลลัพธ์การค้นหา Join โครงการของแต่ละรายการค่าใช้จ่ายมาด้วย (ทำให้รายการมีมากขึ้น)
                    var exprBudgetGroup = exprBudgetExpenses.GroupBy(e => new
                    {
                        e.REQUEST_BUDGET_START_YEAR_AMOUNT,
                        e.TEMPORARY_YR,
                        e.BUDGET_FLAG,
                        e.PLAN_ID,
                        e.PRODUCE_ID,
                        e.ACTIVITY_ID,
                        e.BUDGET_TYPE_ID,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_ID,

                        e.BUDGET_AMOUNT,
                        e.ACTUAL_BUDGET_AMOUNT,
                        e.USE_BUDGET_AMOUNT,
                        e.REMAIN_BUDGET_AMOUNT,

                        e.OFF_BUDGET_AMOUNT,
                        e.ACTUAL_OFF_BUDGET_AMOUNT,
                        e.USE_OFF_BUDGET_AMOUNT,
                        e.REMAIN_OFF_BUDGET_AMOUNT,

                        e.MAS_ACTUAL_OFF_BUDGET_AMOUNT,
                        e.MAS_REMAIN_OFF_BUDGET_AMOUNT,
                        e.MAS_USE_OFF_BUDGET_AMOUNT,
                        e.OFF_BUDGET_SPREAD_TO_EXPENSES
                    }).Select(e => e.Key).ToList();
                    budgetOverview.SpreadBudgetToExpenses = exprBudgetGroup.First().OFF_BUDGET_SPREAD_TO_EXPENSES; // เงินนอกงบประมาณ กระจายงบลงรายการค่าใช้จ่ายหรือไม่
                    if (exprBudgetGroup.First().BUDGET_FLAG.Equals(2))
                        budgetOverview.TemporaryYear = exprBudgetGroup.First().TEMPORARY_YR; // ปีงบประมาณพลางก่อน

                    budgetOverview.BudgetRequestStartYearAmounts = exprBudgetGroup.First().REQUEST_BUDGET_START_YEAR_AMOUNT.Value; // คำของบประมาณต้นปี
                    if (budgetType.Equals(1))
                    {
                        budgetOverview.BudgetAmounts = exprBudgetGroup.Sum(e => e.BUDGET_AMOUNT);
                        budgetOverview.BudgetActualAmounts = exprBudgetGroup.Sum(e => e.ACTUAL_BUDGET_AMOUNT);
                        budgetOverview.BudgetAllocateToDepartmentAmounts = exprBudgetExpenses.Sum(e => e.DEP_ALLOCATE_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_ALLOCATE_BUDGET_AMOUNT.Value);
                        budgetOverview.BudgetReserveAmounts = exprBudgetExpenses.Sum(e => e.RESERVE_BUDGET_AMOUNT == null ? decimal.Zero : e.RESERVE_BUDGET_AMOUNT.Value);
                        budgetOverview.BudgetUseAmounts = exprBudgetGroup.Sum(e => e.USE_BUDGET_AMOUNT);
                        budgetOverview.BudgetActualBalance = exprBudgetGroup.Sum(e => e.REMAIN_BUDGET_AMOUNT);

                        if (exprAllocateBudgetGroup.Any())
                            budgetOverview.BudgetAllocateToDepartmentAmounts += exprAllocateBudgetGroup.Sum(e => e.DEP_GRP_ALLOCATE_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_GRP_ALLOCATE_BUDGET_AMOUNT.Value);
                    }
                    else
                    {
                        budgetOverview.BudgetAmounts = exprBudgetGroup.Sum(e => e.OFF_BUDGET_AMOUNT);
                        budgetOverview.BudgetUseAmounts = exprBudgetGroup.Sum(e => e.USE_OFF_BUDGET_AMOUNT);
                        budgetOverview.BudgetAllocateToDepartmentAmounts = exprBudgetExpenses.Sum(e => e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT.Value);
                        budgetOverview.BudgetReserveAmounts = exprBudgetExpenses.Sum(e => e.RESERVE_OFF_BUDGET_AMOUNT == null ? decimal.Zero : e.RESERVE_OFF_BUDGET_AMOUNT.Value);

                        if (exprAllocateBudgetGroup.Any())
                            budgetOverview.BudgetAllocateToDepartmentAmounts += exprAllocateBudgetGroup.Sum(e => e.DEP_GRP_ALLOCATE_OFF_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_GRP_ALLOCATE_OFF_BUDGET_AMOUNT.Value);

                        // เงินนอกงบประมาณ ในปีงบประมาณใดกระจายเงินประจำงวดลงในแต่ละรายการค่าใช้จ่าย
                        if (budgetOverview.SpreadBudgetToExpenses)
                            budgetOverview.BudgetActualAmounts = exprBudgetGroup.Sum(e => e.ACTUAL_OFF_BUDGET_AMOUNT);
                        else
                            budgetOverview.BudgetActualAmounts = exprBudgetGroup.First().MAS_ACTUAL_OFF_BUDGET_AMOUNT;
                        budgetOverview.BudgetActualBalance = budgetOverview.BudgetActualAmounts - budgetOverview.BudgetUseAmounts;
                    }
                }


                // Group ข้อมูลสำหรับจัดรูปแบบการแสดงผลในหน้าเว็บ
                var finalExprBudgetExpenses = exprBudgetExpenses.AsEnumerable().Select(e => new
                {
                    // ภาพรวมแต่ละรายการค่าใช้จ่าย
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
                    e.BUDGET_TYPE_SHARED_BUDGET,
                    e.BUDGET_TYPE_GOVERNMENT_REFER_CODE,
                    e.BUDGET_TYPE_ORDER_SEQ,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_GOVERNMENT_REFER_CODE,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.PROJECT_ID,
                    e.PROJECT_NAME,
                    e.PROJECT_FOR_TYPE, // 1 = โครงการของเงินงบประมาณ, 2 = โครงการของเงินนอกงบประมาณ

                    // ข้อมูลงบประมาณของ คชจ.
                    BUDGET_AMOUNT = (budgetType.Equals(1) ? e.BUDGET_AMOUNT : e.OFF_BUDGET_AMOUNT),
                    ACTUAL_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.ACTUAL_BUDGET_AMOUNT : e.ACTUAL_OFF_BUDGET_AMOUNT),
                    USE_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.USE_BUDGET_AMOUNT : e.USE_OFF_BUDGET_AMOUNT),
                    REMAIN_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.REMAIN_BUDGET_AMOUNT : e.REMAIN_OFF_BUDGET_AMOUNT),

                    // ข้อมูลงบประมาณ โครงการ
                    PRO_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.PRO_BUDGET_AMOUNT : e.PRO_OFF_BUDGET_AMOUNT),
                    PRO_ACTUAL_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.PRO_ACTUAL_BUDGET_AMOUNT : e.PRO_ACTUAL_OFF_BUDGET_AMOUNT),
                    PRO_USE_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.PRO_USE_BUDGET_AMOUNT : e.PRO_USE_OFF_BUDGET_AMOUNT),
                    PRO_REMAIN_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.PRO_REMAIN_BUDGET_AMOUNT : e.PRO_REMAIN_OFF_BUDGET_AMOUNT),

                    // จัดสรรให้หน่วยงานภูมิภาค
                    DEP_ALLOCATE_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.DEP_ALLOCATE_BUDGET_AMOUNT : e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT),
                    GRP_ALLOCATE_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.DEP_GRP_ALLOCATE_BUDGET_AMOUNT : e.DEP_GRP_ALLOCATE_OFF_BUDGET_AMOUNT),

                    // กันเงิน
                    RESERVE_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.RESERVE_BUDGET_AMOUNT : e.RESERVE_OFF_BUDGET_AMOUNT),
                    RESERVE_USE_BUDGET_AMOUNT = (budgetType.Equals(1) ? e.RESERVE_USE_BUDGET_AMOUNT : e.RESERVE_USE_OFF_BUDGET_AMOUNT),
                    RESERVE_REMAIN_AMOUNT = (budgetType.Equals(1) ? e.RESERVE_REMAIN_BUDGET_AMOUNT : e.RESERVE_REMAIN_OFF_BUDGET_AMOUNT)
                }).GroupBy(e => new
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
                    e.BUDGET_TYPE_SHARED_BUDGET,
                    e.BUDGET_TYPE_ORDER_SEQ,
                    e.BUDGET_TYPE_GOVERNMENT_REFER_CODE,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_GOVERNMENT_REFER_CODE,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.GRP_ALLOCATE_BUDGET_AMOUNT
                }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.Key.EXPENSES_GROUP_ORDER_SEQ)
                .Select(e => new
                {
                    GroupBy = e.Key,
                    Rows = e.OrderBy(x => x.EXPENSES_ORDER_SEQ).ToList()
                }).ToList();

                // รายการค่าใช้จ่ายที่หน่วยงานได้รับจัดสรร
                pagging.totalRecords = finalExprBudgetExpenses.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = finalExprBudgetExpenses.Skip(offset).Take(pageSize).ToList();
            }
            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        public class BudgetOverviewProperty
        {
            /// <summary>
            /// เงินประจำงวด ระบุจำนวนเงินงบประมาณในแต่ละรายการค่าใช้จ่ายหรือไม่ (true = ลงในแต่ละรายการค่าใช้จ่าย)
            /// </summary>
            public bool SpreadBudgetToExpenses { get; set; }

            /// <summary>
            /// ปีงบประมาณพลางก่อน ในบางปีงบประมาณยังไม่มีการจัดสรรงบประมาณจากรัฐบาล
            /// กรมสรรพสามิตจะใช้ข้อมูลพลางก่อนของปีก่อนหน้ามาตั้งงบ
            /// </summary>
            public int? TemporaryYear { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณสุทธิ ของคำของบประมาณต้นปี ของหน่วยงานภายนอก
            /// </summary>
            public decimal BudgetRequestStartYearAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณสุทธิ ที่ได้รับจัดสรรจากรัฐบาลในปี งบประมาณ
            /// </summary>
            public decimal BudgetAmounts { get; set; }

            /// <summary>
            /// เงินประจำงวดสุทธิ รัฐบาลจะจ่ายเงินงบประมาณที่จัดสรร มาเป็นแต่ละงวด
            /// </summary>
            public decimal BudgetActualAmounts { get; set; }

            /// <summary>
            /// ยอดใช้จ่ายงบประมาณสุทธิ (จัดสรร และ กันเงิน)
            /// </summary>
            public decimal BudgetUseAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณสุทธิ ที่จัดสรรให้หน่วยงานภายนอก
            /// </summary>
            public decimal BudgetAllocateToDepartmentAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณสุทธิ ที่กันเงินของหน่วยงานภายในกรมสรรพสามิต
            /// </summary>
            public decimal BudgetReserveAmounts { get; set; }

            /// <summary>
            /// เงินประจำงวดคงเหลือสุทธิ
            /// </summary>
            public decimal BudgetActualBalance { get; set; }
        }
    }
}