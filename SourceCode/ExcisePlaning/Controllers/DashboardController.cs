using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class DashboardController : Controller
    {
        // GET: Dashboard
        public ActionResult Index()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_DASHBOARD;
            ViewBag.Title = menuItem.MenuName;
            ViewBag.MenuGroups = userAuthorizeProfile.MenuGroups;
            ViewBag.PageName = "";// menuItem.MenuName;
            ViewBag.PageDescription = menuItem.MenuDescription;
            ViewBag.LoginName = userAuthorizeProfile.EmpFullname;

            // กำหนดค่า Breadcrump
            List<Breadcrump> breadcrumps = new List<Breadcrump>(1);
            breadcrumps.Add(new Breadcrump()
            {
                Text = menuItem.MenuName,
                CssIcon = menuItem.MenuIcon,
                ControllerName = menuItem.RouteName,
                ActionName = menuItem.ActionName
            });
            ViewBag.breadrumps = breadcrumps;


            ViewBag.FiscalYear = AppUtils.GetCurrYear();
            ViewBag.AreaId = userAuthorizeProfile.AreaId;
            ViewBag.DepartmentId = userAuthorizeProfile.DepId;
            ViewBag.DepAuthorize = userAuthorizeProfile.DepAuthorize;
            ViewBag.CanSelectDepartment = userAuthorizeProfile.CanSelectDepartment;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Areas = db.T_AREAs.Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = e.AREA_NAME
                }).ToList();
            }

            return View();
        }


        /// <summary>
        /// ค่า % เป้าหมายการรายงานผลการใช้จ่าย (จัดสรร กันเงิน)
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="quarterNo"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetReportPercentTarget(int fiscalYear, int quarterNo)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(3) {
                { "Overall", null },
                { "Expenses", null },
                { "Investment", null }
            };

            string dateStr = string.Format("{0}-10-01", fiscalYear - 1);
            if (quarterNo.Equals(2))
                dateStr = string.Format("{0}-01-01", fiscalYear + 1);
            if (quarterNo.Equals(3))
                dateStr = string.Format("{0}-04-01", fiscalYear + 1);
            if (quarterNo.Equals(4))
                dateStr = string.Format("{0}-07-01", fiscalYear + 1);

            DateTime activeDate = DateTime.Parse(dateStr);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_REPORTED_BUDGET_TARGET_PERCENTs.Where(e => activeDate >= e.START_DATE && activeDate <= e.END_DATE)
                    .Select(e => new
                    {
                        e.TARGET_TYPE,
                        e.TARGET_PERCENT_QTR1,
                        e.TARGET_PERCENT_QTR2,
                        e.TARGET_PERCENT_QTR3,
                        e.TARGET_PERCENT_QTR4
                    });
                res["Overall"] = expr.Where(e => e.TARGET_TYPE.Equals(1)).FirstOrDefault();
                res["Expenses"] = expr.Where(e => e.TARGET_TYPE.Equals(2)).FirstOrDefault();
                res["Investment"] = expr.Where(e => e.TARGET_TYPE.Equals(3)).FirstOrDefault();
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// สรุปข้อมูล งบประมาณ และ กันเงินเพื่อนำไปแสดงในหน้า Dashboard
        /// 1. เป้าหมายภาพรวม, สัดส่วนงบประมาณในภาพรวม แยกตามงบรายจ่าย (งบลงทุน งบอุดหนุน งบบุคลากร ...), สรุปกราฟแท่ง
        /// 2. เป้าหมายรายจ่ายประจำ (ไม่รวมงบลงทุน), สรุปกราฟแท่ง
        /// 3. เป้าหมายงบลงทุน, สรุปกราฟแท่ง
        /// 4. กราฟแท่ง แยกตามงบรายจ่าย
        /// </summary>
        /// <param name="fiscalYear">ปีงบประมาณ ค.ศ.</param>
        /// <param name="areaId">รหัสเขตพื้นที่</param>
        /// <param name="depId">หน่วยงาน</param>
        /// <param name="subDepId">หน่วยงานภายใน</param>
        /// <param name="budgetType">1 = เงินงบ, 2 = เงินนอกงบ</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(short fiscalYear, int? areaId, int? depId, short? budgetType)
        {
            DashboardDataProperty ret = new DashboardDataProperty();
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "errorText", null }, { "data", ret } };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var depFilterAuthorize = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, depId);

                // ข้อมลการจัดสรร
                var exprAllocate = db.V_GET_DEPARTMENT_EXPENSES_BUDGET_INFORMATIONs.Select(e => new
                {
                    e.YR,
                    e.AREA_ID,
                    e.DEP_ID,
                    e.PLAN_ID,
                    e.PRODUCE_ID,
                    e.ACTIVITY_ID,
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_ID,
                    e.PROJECT_ID,

                    // เงินงบ
                    e.ALLOCATE_BUDGET_AMOUNT,
                    e.USE_BUDGET_AMOUNT,
                    e.REMAIN_BUDGET_AMOUNT,

                    // เงินนอกงบ
                    e.ALLOCATE_OFF_BUDGET_AMOUNT,
                    e.USE_OFF_BUDGET_AMOUNT,
                    e.REMAIN_OFF_BUDGET_AMOUNT,

                    // เงินงบ + เงินนอก
                    e.NET_BUDGET_AMOUNT,
                    e.NET_USE_BUDGET_AMOUNT,
                    e.NET_REMAIN_BUDGET_AMOUNT,

                    // จัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย
                    e.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                    e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT
                }).Where(e => e.YR.Equals(fiscalYear));
                if (depFilterAuthorize.Authorize.Equals(2))
                    exprAllocate = exprAllocate.Where(e => depFilterAuthorize.AssignDepartmentIds.Contains(e.DEP_ID));
                if (null != areaId)
                    exprAllocate = exprAllocate.Where(e => e.AREA_ID.Equals(areaId));

                // จัดสรร ตามรายการค่าใช้จ่าย
                var finalExprAllocate = exprAllocate.GroupBy(e => new { e.YR, e.BUDGET_TYPE_ID, e.BUDGET_TYPE_NAME })
                    .Select(e => new
                    {
                        e.Key.YR,
                        e.Key.BUDGET_TYPE_ID,
                        e.Key.BUDGET_TYPE_NAME,
                        // เงินงบ
                        ALLOCATE_BUDGET_AMOUNT = e.Sum(x => x.ALLOCATE_BUDGET_AMOUNT),
                        USE_BUDGET_AMOUNT = e.Sum(x => x.USE_BUDGET_AMOUNT),
                        REMAIN_BUDGET_AMOUNT = e.Sum(x => x.REMAIN_BUDGET_AMOUNT),

                        // เงินนอก
                        ALLOCATE_OFF_BUDGET_AMOUNT = e.Sum(x => x.ALLOCATE_OFF_BUDGET_AMOUNT),
                        USE_OFF_BUDGET_AMOUNT = e.Sum(x => x.USE_OFF_BUDGET_AMOUNT),
                        REMAIN_OFF_BUDGET_AMOUNT = e.Sum(x => x.REMAIN_OFF_BUDGET_AMOUNT),

                        // เงินงบ + เงินนอก
                        NET_ALLOCATE_BUDGET_AMOUNT = e.Sum(x => x.NET_BUDGET_AMOUNT),
                        NET_USE_BUDGET_AMOUNT = e.Sum(x => x.NET_USE_BUDGET_AMOUNT),
                        NET_REMAIN_BUDGET_AMOUNT = e.Sum(x => x.NET_REMAIN_BUDGET_AMOUNT)
                    }).AsEnumerable()
                    .Select(e => new
                    {
                        e.YR,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        ALLOCATE_BUDGET_AMOUNT = budgetType == null ? e.NET_ALLOCATE_BUDGET_AMOUNT : (budgetType.Value.Equals(1) ? e.ALLOCATE_BUDGET_AMOUNT : e.ALLOCATE_OFF_BUDGET_AMOUNT),
                        USE_BUDGET_AMOUNT = budgetType == null ? e.NET_USE_BUDGET_AMOUNT : (budgetType.Value.Equals(1) ? e.USE_BUDGET_AMOUNT : e.USE_OFF_BUDGET_AMOUNT),
                        REMAIN_BUDGET_AMOUNT = budgetType == null ? e.NET_REMAIN_BUDGET_AMOUNT : (budgetType.Value.Equals(1) ? e.REMAIN_BUDGET_AMOUNT : e.REMAIN_OFF_BUDGET_AMOUNT),
                    }).ToList();

                // จัดสรร เป็นก้อนตามหมวดค่าใช้จ่าย
                var finalExprAllocateGroup = exprAllocate.GroupBy(e => new
                {
                    e.YR,
                    e.DEP_ID,
                    e.PLAN_ID,
                    e.PRODUCE_ID,
                    e.ACTIVITY_ID,
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.EXPENSES_GROUP_ID,
                    e.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                    e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT
                }).Select(e => e.Key)
                .GroupBy(e => new { e.YR, e.BUDGET_TYPE_ID, e.BUDGET_TYPE_NAME })
                .Select(e => new
                {
                    e.Key.YR,
                    e.Key.BUDGET_TYPE_ID,
                    e.Key.BUDGET_TYPE_NAME,
                    EX_GRP_ALLOCATE_BUDGET_AMOUNT = e.Sum(x => x.EX_GRP_ALLOCATE_BUDGET_AMOUNT),
                    EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT = e.Sum(x => x.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT),
                    NET_EX_GRP_ALLOCATE_BUDGET_AMOUNT = e.Sum(x => x.EX_GRP_ALLOCATE_BUDGET_AMOUNT) + e.Sum(x => x.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT)
                }).AsEnumerable()
                .Select(e => new
                {
                    e.YR,
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    EX_GRP_BUDGET_AMOUNT = budgetType == null ? e.NET_EX_GRP_ALLOCATE_BUDGET_AMOUNT : (budgetType.Value.Equals(1) ? e.EX_GRP_ALLOCATE_BUDGET_AMOUNT : e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT)
                }).ToList();

                // ข้อมูลการกันเงิน
                var exprReserve = db.V_GET_BUDGET_RESERVE_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear));
                if (depFilterAuthorize.Authorize.Equals(2))
                    exprReserve = exprReserve.Where(e => depFilterAuthorize.AssignDepartmentIds.Contains(e.DEP_ID.Value));
                if (null != budgetType)
                    exprReserve = exprReserve.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                var finalExprReserve = exprReserve.GroupBy(e => new { e.YR, e.BUDGET_TYPE_ID, e.BUDGET_TYPE_NAME })
                    .Select(e => new
                    {
                        e.Key.YR,
                        e.Key.BUDGET_TYPE_ID,
                        e.Key.BUDGET_TYPE_NAME,
                        RESERVE_BUDGET_AMOUNT = e.Sum(x => x.RESERVE_BUDGET_AMOUNT),
                        USE_RESERVE_BUDGET_AMOUNT = e.Sum(x => x.USE_AMOUNT),
                        REMAIN_RESERVE_BUDGET_AMOUNT = e.Sum(x => x.REMAIN_AMOUNT)
                    }).ToList();



                var isAllocateData = exprAllocate.Any();
                var isReserveData = exprReserve.Any();
                if (!isAllocateData && !isReserveData)
                {
                    res["errorText"] = "ไม่พบข้อมูล";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var chartKeyValuesBudgetType = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new ChartKeyValueProperty()
                {
                    KeyId = e.BUDGET_TYPE_ID,
                    label = e.BUDGET_TYPE_NAME,
                    value = decimal.Zero
                }).AsEnumerable();


                decimal multiplierPercentVal = decimal.Parse("100.00");
                decimal netPaymentBudgetAmounts = decimal.Zero; // งบประมาณ จัดสรร และ กันเงินทั้งหมด
                decimal paymentBudgetAmounts = decimal.Zero;
                decimal useBudgetAmounts = decimal.Zero;

                // สัดส่วนการใช้จ่ายงบประมาณ (จัดสรร, กันเงิน)
                ret.BudgetProportionPercent = chartKeyValuesBudgetType.ToList();


                // กราฟแท่งแสดงการใช้จ่ายในภาพรวม (ภาพรวม เป้าหมายรายจ่ายประจำ เป้าหมายงบลงทุน)
                int defaultInitSize = 3;
                ret.ChartColumnsOverall = new Dictionary<string, List<ChartSeriesProperty>>(3) {
                    // แท่งได้รับจัดสรร & กันเงิน
                    { "Receive",  new List<ChartSeriesProperty>(2){ new ChartSeriesProperty("จัดสรร", defaultInitSize), new ChartSeriesProperty("กันเงิน", defaultInitSize) } },
                    // แท่งใช้จ่าย หรือ เบิกจ่าย
                    { "Payment",  new List<ChartSeriesProperty>(2){ new ChartSeriesProperty("จัดสรร", defaultInitSize), new ChartSeriesProperty("กันเงิน", defaultInitSize) } },
                    // แท่งยอดคงเหลือ
                    { "Balance",  new List<ChartSeriesProperty>(2){ new ChartSeriesProperty("จัดสรร", defaultInitSize), new ChartSeriesProperty("กันเงิน", defaultInitSize) } }
                };


                // กราฟแท่งแสดงการใช้จ่าย แยกตามงบรายจ่าย (งบลงทุน งบอุดหนุน งบบุคลากร, ...)
                defaultInitSize = chartKeyValuesBudgetType.Count();
                ret.ChartColumnsBudgetType = new Dictionary<string, List<ChartSeriesProperty>>(3) {
                    // แท่งได้รับจัดสรร & กันเงิน
                    { "Receive",  new List<ChartSeriesProperty>(2){ new ChartSeriesProperty("จัดสรร", defaultInitSize), new ChartSeriesProperty("กันเงิน", defaultInitSize) } },
                    // แท่งใช้จ่าย หรือ เบิกจ่าย
                    { "Payment",  new List<ChartSeriesProperty>(2){ new ChartSeriesProperty("จัดสรร", defaultInitSize), new ChartSeriesProperty("กันเงิน", defaultInitSize) } },
                    // แท่งยอดคงเหลือ
                    { "Balance",  new List<ChartSeriesProperty>(2){ new ChartSeriesProperty("จัดสรร", defaultInitSize), new ChartSeriesProperty("กันเงิน", defaultInitSize) } }
                };
                ret.ChartColumnsBudgetType["Receive"][0].Values = chartKeyValuesBudgetType.ToList();
                ret.ChartColumnsBudgetType["Receive"][1].Values = chartKeyValuesBudgetType.ToList();
                ret.ChartColumnsBudgetType["Payment"][0].Values = chartKeyValuesBudgetType.ToList();
                ret.ChartColumnsBudgetType["Payment"][1].Values = chartKeyValuesBudgetType.ToList();
                ret.ChartColumnsBudgetType["Balance"][0].Values = chartKeyValuesBudgetType.ToList();
                ret.ChartColumnsBudgetType["Balance"][1].Values = chartKeyValuesBudgetType.ToList();


                // % เป้าหมายภาพรวม
                paymentBudgetAmounts = decimal.Zero;
                useBudgetAmounts = decimal.Zero;
                if (isAllocateData)
                {
                    paymentBudgetAmounts += finalExprAllocate.Sum(e => e.ALLOCATE_BUDGET_AMOUNT);
                    paymentBudgetAmounts += finalExprAllocateGroup.Sum(e => e.EX_GRP_BUDGET_AMOUNT);
                    useBudgetAmounts += finalExprAllocate.Sum(e => e.USE_BUDGET_AMOUNT);

                    // กราฟแท่งภาพรวม
                    ret.ChartColumnsOverall["Receive"][0].Values[0].value = paymentBudgetAmounts;
                    ret.ChartColumnsOverall["Payment"][0].Values[0].value = useBudgetAmounts;
                    ret.ChartColumnsOverall["Balance"][0].Values[0].value = paymentBudgetAmounts - useBudgetAmounts;
                }
                if (isReserveData)
                {
                    var reserveAmounts = finalExprReserve.Sum(e => e.RESERVE_BUDGET_AMOUNT);
                    var useReserveAmounts = finalExprReserve.Sum(e => e.USE_RESERVE_BUDGET_AMOUNT);

                    paymentBudgetAmounts += reserveAmounts;
                    useBudgetAmounts += useReserveAmounts;

                    // กราฟแท่งภาพรวม
                    ret.ChartColumnsOverall["Receive"][1].Values[0].value = reserveAmounts;
                    ret.ChartColumnsOverall["Payment"][1].Values[0].value = useReserveAmounts;
                    ret.ChartColumnsOverall["Balance"][1].Values[0].value = reserveAmounts - useReserveAmounts;
                }
                netPaymentBudgetAmounts = paymentBudgetAmounts;
                ret.ReportOverallPercentTargetVal = paymentBudgetAmounts.CompareTo(decimal.Zero) == 0 ? decimal.Zero : Math.Round(useBudgetAmounts / paymentBudgetAmounts * multiplierPercentVal, 2, MidpointRounding.AwayFromZero);


                // % เป้าหมายรายจ่ายประจำ (ไม่รวมงบลงทุน)
                paymentBudgetAmounts = decimal.Zero;
                useBudgetAmounts = decimal.Zero;
                if (isAllocateData)
                {
                    paymentBudgetAmounts += finalExprAllocate.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? decimal.Zero : e.ALLOCATE_BUDGET_AMOUNT);
                    paymentBudgetAmounts += finalExprAllocateGroup.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? decimal.Zero : e.EX_GRP_BUDGET_AMOUNT);
                    useBudgetAmounts += finalExprAllocate.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? decimal.Zero : e.USE_BUDGET_AMOUNT);

                    // กราฟแท่งภาพรวม
                    ret.ChartColumnsOverall["Receive"][0].Values[1].value = paymentBudgetAmounts;
                    ret.ChartColumnsOverall["Payment"][0].Values[1].value = useBudgetAmounts;
                    ret.ChartColumnsOverall["Balance"][0].Values[1].value = paymentBudgetAmounts - useBudgetAmounts;
                }
                if (isReserveData)
                {
                    var reserveAmounts = finalExprReserve.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? decimal.Zero : e.RESERVE_BUDGET_AMOUNT);
                    var useReserveAmounts = finalExprReserve.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? decimal.Zero : e.USE_RESERVE_BUDGET_AMOUNT);

                    paymentBudgetAmounts += reserveAmounts;
                    useBudgetAmounts += useReserveAmounts;

                    // กราฟแท่งภาพรวม
                    ret.ChartColumnsOverall["Receive"][1].Values[1].value = reserveAmounts;
                    ret.ChartColumnsOverall["Payment"][1].Values[1].value = useReserveAmounts;
                    ret.ChartColumnsOverall["Balance"][1].Values[1].value = reserveAmounts - useReserveAmounts;
                }
                ret.ReportExpensesPercentTargetVal = paymentBudgetAmounts.CompareTo(decimal.Zero) == 0 ? decimal.Zero : Math.Round(useBudgetAmounts / paymentBudgetAmounts * multiplierPercentVal, 2, MidpointRounding.AwayFromZero);


                // % เป้าหมายงบลงทุน
                paymentBudgetAmounts = decimal.Zero;
                useBudgetAmounts = decimal.Zero;
                if (isAllocateData)
                {
                    paymentBudgetAmounts += finalExprAllocate.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? e.ALLOCATE_BUDGET_AMOUNT : decimal.Zero);
                    paymentBudgetAmounts += finalExprAllocateGroup.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? e.EX_GRP_BUDGET_AMOUNT : decimal.Zero);
                    useBudgetAmounts += finalExprAllocate.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? e.USE_BUDGET_AMOUNT : decimal.Zero);

                    // กราฟแท่งภาพรวม
                    ret.ChartColumnsOverall["Receive"][0].Values[2].value = paymentBudgetAmounts;
                    ret.ChartColumnsOverall["Payment"][0].Values[2].value = useBudgetAmounts;
                    ret.ChartColumnsOverall["Balance"][0].Values[2].value = paymentBudgetAmounts - useBudgetAmounts;
                }
                if (isReserveData)
                {
                    var reserveAmounts = finalExprReserve.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? e.RESERVE_BUDGET_AMOUNT : decimal.Zero);
                    var useReserveAmounts = finalExprReserve.Sum(e => e.BUDGET_TYPE_ID.Equals(2) ? e.USE_RESERVE_BUDGET_AMOUNT : decimal.Zero);

                    paymentBudgetAmounts += reserveAmounts;
                    useBudgetAmounts += useReserveAmounts;

                    // กราฟแท่งภาพรวม
                    ret.ChartColumnsOverall["Receive"][1].Values[2].value = reserveAmounts;
                    ret.ChartColumnsOverall["Payment"][1].Values[2].value = useReserveAmounts;
                    ret.ChartColumnsOverall["Balance"][1].Values[2].value = reserveAmounts - useReserveAmounts;
                }
                ret.ReportInvestmentPercentTargetVal = paymentBudgetAmounts.CompareTo(decimal.Zero) == 0 ? decimal.Zero : Math.Round(useBudgetAmounts / paymentBudgetAmounts * multiplierPercentVal, 2, MidpointRounding.AwayFromZero);



                finalExprAllocate.ForEach(e =>
                {
                    // สัดส่วนการใช้จ่ายงบประมาณ (จัดสรร, กันเงิน)
                    var currItem = ret.BudgetProportionPercent.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    currItem.value += e.ALLOCATE_BUDGET_AMOUNT;

                    // ได้รับจัดสรร
                    var receiveItem = ret.ChartColumnsBudgetType["Receive"][0].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    receiveItem.value += e.ALLOCATE_BUDGET_AMOUNT;
                    // ใช้จ่าย
                    var paymentItem = ret.ChartColumnsBudgetType["Payment"][0].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    paymentItem.value += e.USE_BUDGET_AMOUNT;
                    // คงเหลือ
                    var balanceItem = ret.ChartColumnsBudgetType["Balance"][0].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    balanceItem.value = receiveItem.value - paymentItem.value;
                });
                finalExprAllocateGroup.ForEach(e =>
                {
                    var currItem = ret.BudgetProportionPercent.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    currItem.value += e.EX_GRP_BUDGET_AMOUNT;

                    // ได้รับจัดสรร
                    var receiveItem = ret.ChartColumnsBudgetType["Receive"][0].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    receiveItem.value += e.EX_GRP_BUDGET_AMOUNT;
                    // ใช้จ่าย
                    var paymentItem = ret.ChartColumnsBudgetType["Payment"][0].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    // คงเหลือ
                    var balanceItem = ret.ChartColumnsBudgetType["Balance"][0].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    balanceItem.value = receiveItem.value - paymentItem.value;
                });
                finalExprReserve.ForEach(e =>
                {
                    var currItem = ret.BudgetProportionPercent.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    currItem.value += e.RESERVE_BUDGET_AMOUNT;

                    // กันเงิน
                    var receiveItem = ret.ChartColumnsBudgetType["Receive"][1].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    receiveItem.value += e.RESERVE_BUDGET_AMOUNT;
                    // ใช้จ่าย
                    var paymentItem = ret.ChartColumnsBudgetType["Payment"][1].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    paymentItem.value += e.USE_RESERVE_BUDGET_AMOUNT;
                    // คงเหลือ
                    var balanceItem = ret.ChartColumnsBudgetType["Balance"][1].Values.Where(x => x.KeyId.Equals(e.BUDGET_TYPE_ID)).FirstOrDefault();
                    balanceItem.value = receiveItem.value - paymentItem.value;
                });

                // คำนวณ % สัดส่วนงบที่จัดสรร & กันเงิน โดยแยกตามงบรายจ่าย (งบดำเนินงาน งบลงทุน งบอุดหนุน, ...)
                //if (netPaymentBudgetAmounts.CompareTo(decimal.Zero) == 1)
                //    ret.BudgetProportionPercent.ForEach(item => {
                //        item.value = Math.Floor(item.value / netPaymentBudgetAmounts * multiplierPercentVal);
                //    });
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class DashboardDataProperty
        {
            /// <summary>
            /// ภาพรวมผลการเบิกจ่ายงบประมาณ (กันเงิน, จัดสรร) เทียบกับ เงินที่ได้รับจัดสรร & กันเงิน
            /// </summary>
            public decimal ReportOverallPercentTargetVal { get; set; }

            /// <summary>
            /// เป้าหมายรายจ่ายประจำ (กันเงิน, จัดสรร) เทียบกับ เงินที่ได้รับจัดสรร & กันเงิน
            /// </summary>
            public decimal ReportExpensesPercentTargetVal { get; set; }

            /// <summary>
            /// เป้าหมายงบลงทุน (กันเงิน, จัดสรร) เทียบกับ เงินที่ได้รับจัดสรร & กันเงิน
            /// </summary>
            public decimal ReportInvestmentPercentTargetVal { get; set; }

            /// <summary>
            /// % สัดส่วนของงบประมาณ โดยแยกตามงบรายจ่าย (งบดำเนินงาน งบลงทุน งบบุคลากร ...)
            /// </summary>
            public List<ChartKeyValueProperty> BudgetProportionPercent { get; set; }

            /// <summary>
            /// แสดงแบบ Column Stack
            /// จะมีข้อมูล 3 กลุ่มได้แก่ ภาพรวม รายจ่ายประจำ งบลงทุน ในแต่ละกลุ่มจะใช้ข้อมูล 3 ประเภทในการเปรียบเทียบคือ
            /// 1. จัดสรร หรือ กันเงิน
            /// 2. ผลการใช้จ่าย
            /// 3. ยอดคงเหลือ
            /// </summary>
            public Dictionary<string, List<ChartSeriesProperty>> ChartColumnsOverall { get; set; }

            /// <summary>
            /// แสดงแบบ Column Stack
            /// จะแยกข้อมูลตามงบรายจ่าย งบลงทุน งบอุดหนุน งบบุคลากร ... ในแต่ละกลุ่มจะใช้ข้อมูล 3 ประเภทในการเปรียบเทียบคือ
            /// 1. จัดสรร หรือ กันเงิน
            /// 2. ผลการใช้จ่าย
            /// 3. ยอดคงเหลือ
            /// </summary>
            public Dictionary<string, List<ChartSeriesProperty>> ChartColumnsBudgetType { get; set; }
        }



        public class ChartKeyValueProperty
        {
            /// <summary>
            /// ค่าใช้สำหรับเป็นคีย์ในการตรวจสอบ เงื่อนไขบางอย่าง
            /// </summary>
            public int KeyId { get; set; }

            /// <summary>
            /// Label บอกประเภทของข้อมูล
            /// </summary>
            public string label { get; set; }

            /// <summary>
            /// ค่าที่แสดงในกราฟ
            /// </summary>
            public decimal value { get; set; }
        }


        public class ChartSeriesProperty
        {
            public ChartSeriesProperty(string seriesName, int initValueCapacitySize = 0)
            {
                SerialName = seriesName;
                Values = new List<ChartKeyValueProperty>(initValueCapacitySize);
                for (int i = 1; i <= initValueCapacitySize; i++)
                    Values.Add(new ChartKeyValueProperty());
            }

            /// <summary>
            /// ชื่อแท่งของกราฟ
            /// </summary>
            public string SerialName { get; set; }

            /// <summary>
            /// ข้อมูลของแต่ละแท่งกราฟ ซึ่งจำนวนข้อมูลใน Array จะสัมพันธ์กับ Categories
            /// </summary>
            public List<ChartKeyValueProperty> Values { get; set; }
        }

        //public class LastUserData
        //{
        //    public string LastRequestUser { get; set; }
        //    public DateTime? LastRequestDate { get; set; }
        //    public string LastAllowcateUser { get; set; }
        //    public DateTime? LastAllowcateDate { get; set; }
        //    public string LastReportUser { get; set; }
        //    public DateTime? LastReportDate { get; set; }
        //}




        ///// <summary>
        ///// สำหรับสร้างโครงสร้างข้อมูลเพื่อนำไปวาดเป็นกราฟแท่ง ประวัติการมาทำงาน 
        ///// โดยกราฟจะสรุปเป็นรายปี จำนวน 12 เดือน
        ///// </summary>
        //public class WorkingChartColumnProperty
        //{
        //    /// <summary>
        //    /// เตรียมโครงสร้างข้อมูลจำนวน 12 เดือน
        //    /// </summary>
        //    /// <returns></returns>
        //    public static List<WorkingChartColumnProperty> PrepareWorkingChartColumnData()
        //    {
        //        List<WorkingChartColumnProperty> result = new List<WorkingChartColumnProperty>(12) {
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null),
        //            new WorkingChartColumnProperty(null)
        //        };

        //        return result;
        //    }

        //    public WorkingChartColumnProperty(decimal? defaultValue)
        //    {
        //        value = defaultValue;
        //    }

        //    /// <summary>
        //    /// ชื่อ property ของกราฟที่จะนำไปวาดเป็นกราฟแท่ง
        //    /// </summary>
        //    public decimal? value { get; set; }
        //}


        ///// <summary>
        ///// ในแต่ละเดือนจะถูกสร้าง จันทร์ - อาทิตย์ ไว้ทั้งหมด 6 สัปดาห์/เดือน (Weeks)
        ///// </summary>
        //public class CalendarMonthProperty
        //{
        //    public static CalendarMonthProperty GetCalendar(int year, int month, List<string> holidayDates, List<LeaveDayProperty> leavesInMonth)
        //    {
        //        DateTime sourceDate = new DateTime(year, month, 1);
        //        int weekIndex = 1;

        //        CalendarMonthProperty calendar = new CalendarMonthProperty()
        //        {
        //            MonthNo = month,
        //            YearNo = year,
        //            Label = sourceDate.ToString("MMM, yyyy", AppUtils.ThaiCultureInfo)
        //        };

        //        do
        //        {
        //            string weekKey = string.Format("Week{0}", weekIndex);
        //            var dayProp = calendar.Weeks[weekKey].Where(day => day.DayOfWeek == sourceDate.DayOfWeek).FirstOrDefault();
        //            dayProp.DateStr = sourceDate.ToString("dd/MM/yyyy");
        //            dayProp.IsWeekend = (sourceDate.DayOfWeek == DayOfWeek.Sunday || sourceDate.DayOfWeek == DayOfWeek.Saturday);
        //            dayProp.DayOfMonth = sourceDate.Day;
        //            dayProp.IsHolidayDate = holidayDates.IndexOf(dayProp.DateStr) > -1;
        //            dayProp.IsCurrentDay = sourceDate.Equals(DateTime.Now.Date);
        //            dayProp.LeaveInfo = leavesInMonth.Where(e => e.DateStr.Equals(dayProp.DateStr)).FirstOrDefault();

        //            sourceDate = sourceDate.AddDays(1);
        //            if (sourceDate.DayOfWeek == DayOfWeek.Sunday)
        //                weekIndex++;
        //        } while (sourceDate.Month.Equals(month));

        //        return calendar;
        //    }
        //    public CalendarMonthProperty()
        //    {
        //        Weeks = new Dictionary<string, List<CalendarDayProperty>>();
        //        Weeks.Add("Week1", new List<CalendarDayProperty>() {
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
        //        });

        //        Weeks.Add("Week2", new List<CalendarDayProperty>() {
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
        //        });

        //        Weeks.Add("Week3", new List<CalendarDayProperty>() {
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
        //        });

        //        Weeks.Add("Week4", new List<CalendarDayProperty>() {
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
        //        });

        //        Weeks.Add("Week5", new List<CalendarDayProperty>() {
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
        //        });
        //        Weeks.Add("Week6", new List<CalendarDayProperty>() {
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
        //            new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
        //        });
        //    }

        //    public int MonthNo { get; set; }
        //    public int YearNo { get; set; }

        //    /// <summary>
        //    /// ข้อความสำหรับแสดงบน ปฏิทินของเดือน เช่น มี.ค., 63 เป็นต้น
        //    /// </summary>
        //    public string Label { get; set; }

        //    /// <summary>
        //    /// ข้อมูลสัปดาห์ ของแต่ละเดือน <para/>
        //    /// ประกอบไปด้วย 5 รายการ "Week1" ... "Week5" <para/>
        //    /// ในแต่ละรายการ (Week1 ... Week5) จะมีทั้งหมด 7 วัน
        //    /// </summary>
        //    public Dictionary<string, List<CalendarDayProperty>> Weeks { get; set; }
        //}

        //[HttpPost]//,Route("Yr:int,DepID:int")
        //public ActionResult ShowDataDashboard(int iYear, int DepID,int quarter)//
        //{


        //    iYear = iYear - 543;

        //    Dictionary<string, Object> res = new Dictionary<string, object>(8)
        //    {
        //        {"LastRequestUser","" },
        //        {"LastRequestDate","" },
        //        {"LastAllowcateUser","" },
        //        {"LastAllowcateDate","" },
        //        {"LastReportUser","" },
        //        {"LastReportDate","" },

        //        {"RequestDataTotal",0},//คำของบประมาณ
        //        {"RequestDataBudget",0},//เงิน งปม.
        //        {"RequestDataOffBudget",0},//เงินนอก งปม.

        //        {"AllowcateDataTotal",0 },//จัดสรรงบประมาณ
        //        {"AllowcateDataBudget",0 },//เงิน งปม.
        //        {"AllowcateDataOffBudget",0 },//เงินนอก งปม.

        //        //กราฟ 
        //        {"GraphRequestData",null},// graph คำของบประมาณ
        //        {"GraphAllowcateData",null },// graph จัดสรรงบประมาณ

        //        //กราฟ gauge
        //        {"gauge1Data",0 },
        //        {"gauge2Data",0 },
        //        {"gauge3Data",0 }

        //    };

        //    if (quarter == 1)
        //    {
        //        res["gauge1Data"] = 32 ;
        //        res["gauge2Data"] = 36;
        //        res["gauge3Data"] = 20;
        //    }
        //    else if (quarter == 2)
        //    {
        //        res["gauge1Data"] = 54;
        //        res["gauge2Data"] = 57;
        //        res["gauge3Data"] = 45;
        //    }
        //    else if (quarter == 3)
        //    {
        //        res["gauge1Data"] = 77;
        //        res["gauge2Data"] = 80;
        //        res["gauge3Data"] = 65;
        //    }
        //    else if (quarter == 4)
        //    {
        //        res["gauge1Data"] = 100;
        //        res["gauge2Data"] = 100;
        //        res["gauge3Data"] = 100;
        //    }

        //    using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
        //    {               

        //        var exprReq = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.ACTIVE.Equals(1)
        //                        && e.DEP_ID.Equals(DepID)
        //                        && new[] { 1,2}.Contains(e.BUDGET_TYPE)
        //                        && e.YR.Equals(iYear));
        //        if (exprReq.Any())
        //        {
        //            res["RequestDataTotal"] = exprReq.Sum(e => e.TOTAL_REQUEST_BUDGET);
        //            res["RequestDataBudget"] = exprReq.Where(e => e.BUDGET_TYPE.Equals(1)).Sum(e => (decimal?)e.TOTAL_REQUEST_BUDGET) ?? 0;
        //            res["RequestDataOffBudget"] = exprReq.Where(e => e.BUDGET_TYPE.Equals(2)).Sum(e => (decimal?)e.TOTAL_REQUEST_BUDGET) ?? 0;
        //        }

        //        var exprAllowcate = db.T_BUDGET_ALLOCATEs.Where(e =>  e.YR.Equals(iYear)
        //                            && e.DEP_ID.Equals(DepID));
        //        if (exprAllowcate.Any())
        //        {
        //            res["AllowcateDataTotal"] = exprAllowcate.Sum(e => e.ALLOCATE_BUDGET_AMOUNT + e.ALLOCATE_OFF_BUDGET_AMOUNT);
        //            res["AllowcateDataBudget"] = exprAllowcate.Sum(e => (decimal?)e.ALLOCATE_BUDGET_AMOUNT) ?? 0;
        //            res["AllowcateDataOffBudget"] = exprAllowcate.Sum(e => (decimal?)e.ALLOCATE_OFF_BUDGET_AMOUNT) ?? 0;
        //        }

        //        // graph คำของบประมาณ

        //        string sSql = "Select Label = B.BUDGET_TYPE_NAME, " +
        //                 "   value = (Select IsNull(sum(REQ.TOTAL_REQUEST_BUDGET), 0)" +
        //                  "           from T_BUDGET_REQUEST_DETAIL REQ" +
        //                 "            Where REQ.BUDGET_TYPE_ID = B.BUDGET_TYPE_ID And  REQ.ACTIVE = 1 " +
        //                 "            And REQ.DEP_ID = " + DepID.ToString() +
        //                 "            and REQ.YR = " + iYear.ToString() + ")" +
        //                 " ,ORDER_SEQ from T_BUDGET_TYPE B" +
        //                 " where ACTIVE = 1" +
        //                 " order by ORDER_SEQ";

        //        var exprGraphRequestData = db.ExecuteQuery<Graph>(sSql);
        //        var finalGraphRequestData = exprGraphRequestData.Select(e => new
        //        {
        //            label = e.label,
        //            value = e.value,
        //            ORDER_SEQ = e.ORDER_SEQ
        //        }).ToList();

        //        res["GraphRequestData"] = finalGraphRequestData.OrderBy(e => e.ORDER_SEQ).ToList();

        //        // graph จัดสรรงบประมาณ

        //        sSql = "Select Label = B.BUDGET_TYPE_NAME, "+
        //                "   value =  isnull( (Select sum(NET_BUDGET_AMOUNT) from V_GET_DEPARTMENT_EXPENSES_BUDGET_INFORMATION V " +
        //                 "           where v.ACTIVE = 1 and v.BUDGET_TYPE_ID = b.BUDGET_TYPE_ID and v.YR = "+ iYear.ToString() + ") , 0) " +
        //                " from T_BUDGET_TYPE B" +
        //                " where ACTIVE = 1" +
        //                " order by ORDER_SEQ";

        //        var exprGraphAllowcateData = db.ExecuteQuery<Graph>(sSql);
        //        var finalGraphAllowcateData = exprGraphAllowcateData.Select(e => new
        //        {
        //            label = e.label,
        //            value = e.value,
        //            ORDER_SEQ = e.ORDER_SEQ
        //        }).ToList();

        //        res["GraphAllowcateData"] = finalGraphAllowcateData.OrderBy(e => e.ORDER_SEQ).ToList();

        //        if ( Convert.ToDecimal( res["AllowcateDataTotal"].ToString() ) + Convert.ToDecimal( res["RequestDataTotal"].ToString() ) > 0 )
        //        {
        //            sSql = " Select top 1 " +
        //       " LastRequestUser = (Select top 1 PREFIX_NAME + ' ' + FIRST_NAME + ' ' + LAST_NAME from T_PERSONNEL P where P.PERSON_ID = B.LATEST_REQUEST_ID  ) ," +
        //       " LastRequestDate = B.LATEST_REQUEST_DATETIME," +
        //       " LastAllowcateUser = (Select top 1 PREFIX_NAME + ' ' + FIRST_NAME + ' ' + LAST_NAME from T_PERSONNEL P where P.PERSON_ID = B.LATEST_ALLOCATE_ID  )," +
        //       " LastAllowcateDate = B.LATEST_ALLOCATE_DATETIME," +
        //       " LastReportUser = (Select top 1 PREFIX_NAME + ' ' + FIRST_NAME + ' ' + LAST_NAME from T_PERSONNEL P where P.PERSON_ID = B.LATEST_REPORT_ID  )," +
        //       " LastReportDate = B.LATEST_REPORT_DATETIME" +
        //       " from T_BUDGET_ALLOCATE B" +
        //       " where DEP_ID = " + DepID.ToString() +
        //       //" and YR = " + AppUtils.GetCurrYear().ToString() +
        //       " order by CREATED_DATETIME desc";

        //            var exprUser = db.ExecuteQuery<LastUserData>(sSql);

        //            ViewBag.UserData = exprUser.Select(e => new
        //            {
        //                LastRequestUser = e.LastRequestUser,
        //                LastRequestDate = e.LastRequestDate,
        //                LastAllowcateUser = e.LastAllowcateUser,
        //                LastAllowcateDate = e.LastAllowcateDate,
        //                LastReportUser = e.LastReportUser,
        //                LastReportDate = e.LastReportDate
        //            }).ToList();

        //            Thread.CurrentThread.CurrentCulture = new CultureInfo("th-TH");
        //            if (ViewBag.UserData.Count > 0)
        //            {
        //                var item = ViewBag.UserData[0];
        //                int year = 0;

        //                res["LastRequestUser"] = item.LastRequestUser;
        //                if (item.LastRequestDate != null)
        //                {
        //                    year = item.LastRequestDate.Year + 543;
        //                    res["LastRequestDate"] = item.LastRequestDate.ToString("dd /MM/") + year.ToString();
        //                }


        //                res["LastAllowcateUser"] = item.LastAllowcateUser;
        //                if (item.LastAllowcateDate != null)
        //                {
        //                    year = item.LastAllowcateDate.Year + 543;
        //                    res["LastAllowcateDate"] = item.LastAllowcateDate.ToString("dd /MM/") + year.ToString();
        //                }


        //                res["LastReportUser"] = item.LastReportUser;
        //                if (item.LastReportDate != null)
        //                {
        //                    year = item.LastReportDate.Year + 543;
        //                    res["LastReportDate"] = item.LastReportDate.ToString("dd /MM/") + year.ToString();
        //                }

        //            }
        //        }




        //        return Json(res, JsonRequestBehavior.AllowGet);

        //    }
        //}

        //public class Graph
        //{
        //    public decimal value { get; set; }
        //    public string label { get; set; }

        //    public int ORDER_SEQ { get; set; }

        //}

        //public class CalendarDayProperty
        //{
        //    public CalendarDayProperty()
        //    {
        //        DateStr = "";
        //        DayOfMonth = null;
        //        IsWeekend = false;
        //        IsHolidayDate = false;
        //        IsCurrentDay = false;
        //        LeaveInfo = null;
        //    }

        //    /// <summary>
        //    /// วันที่ในรูปแบบ dd/MM/yyyy
        //    /// </summary>
        //    public string DateStr { get; set; }

        //    /// <summary>
        //    /// วันในสัปดาห์ 0..6 (Sun ... Sat)
        //    /// </summary>
        //    public DayOfWeek DayOfWeek { get; set; }

        //    /// <summary>
        //    /// วันที่ของเดือน 1..30/31
        //    /// </summary>
        //    public int? DayOfMonth { get; set; }

        //    /// <summary>
        //    /// กำหนดเป็นวันหยุดแล้วใช่หรือไม่
        //    /// </summary>
        //    public bool IsHolidayDate { get; set; }

        //    /// <summary>
        //    /// เป็นวันเสาร์ อาทิตย์ ใช่หรือไม่
        //    /// </summary>
        //    public bool IsWeekend { get; set; }

        //    /// <summary>
        //    /// เป็นวันปัจจุบันใช่หรือไม่
        //    /// </summary>
        //    public bool IsCurrentDay { get; set; }

        //    /// <summary>
        //    /// กรณีเป็นวันนั้นๆเป็นวันลา
        //    /// </summary>
        //    public LeaveDayProperty LeaveInfo { get; set; }
        //}

        //public class LeaveDayProperty
        //{
        //    /// <summary>
        //    /// วันลาที่อยู่ในรูปแบบ dd/MM/yyyy
        //    /// </summary>
        //    public string DateStr { get; set; }

        //    /// <summary>
        //    /// ชื่อ class สำหรับแสดงสีของวันลาแต่ล่ะประเภท จะได้จาก third_parties/assets/css/style.css
        //    /// โดยให้เรียก AppUtils.GetLeaveClassName(leaveTypeId)
        //    /// </summary>
        //    public string ClassName { get; set; }

        //    /// <summary>
        //    /// ประเภทการลา (ลากิจ ลาพักผ่อน ลาป่วย)
        //    /// </summary>
        //    public string LeaveTypeName { get; set; }

        //    /// <summary>
        //    /// จำนวนวันที่ขอลา (1 = เต็มวัน, 0.5 = ครึ่งวัน)
        //    /// </summary>
        //    public decimal AmountDays { get; set; }
        //}
    }
}