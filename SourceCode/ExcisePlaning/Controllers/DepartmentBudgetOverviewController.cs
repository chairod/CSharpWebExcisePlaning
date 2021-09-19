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
    /// สรุปภาพรวมการจัดสรรงบประมาณจาก กรมสรรพสามิต ลงมายังหน่วยงานภายนอก
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class DepartmentBudgetOverviewController : Controller
    {
        // GET: DepartmentBudgetOverview
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DEPARTMENT_BUDGET_OVERVIEW_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_DEPARTMENT_BUDGET_OVERVIEW_MENU;
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

            ViewBag.DefaultYear = AppUtils.GetCurrYear();
            ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            ViewBag.DefaultDepartmentId = userAuthorizeProfile.DepId;
            ViewBag.DepartmentAuthorize = userAuthorizeProfile.DepAuthorize;
            // หน่วยงานกลาง ไม่ต้อง Default หน่วยงาน
            if (userAuthorizeProfile.DepAuthorize.Equals(1))
            {
                ViewBag.DefaultAreaId = "empty";
                ViewBag.DefaultDepartmentId = "empty";
            }

            ViewBag.CanSelectDepartment = userAuthorizeProfile.CanSelectDepartment;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ข้อมูลเขตพื้นที่
                // ไม่ใช่หน่วยงานกลาง เห็นได้เฉพาะเขตพื้นที่ตนเอง
                var areaExpr = db.T_AREAs.Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = e.AREA_NAME,
                    AREA_CODE = e.AREA_CODE
                });
                if (userAuthorizeProfile.DepAuthorize.Equals(2))
                    areaExpr = areaExpr.Where(e => e.AREA_ID.Equals(userAuthorizeProfile.AreaId));
                ViewBag.Areas = areaExpr.ToList();

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
        /// สรุปข้อมูลภาพรวมการจัดสรรงบประมาณ จากกรมสรรพสามิต ลงไปให้กับหน่วยงาน
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="depId"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? areaId, int? depId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int pageIndex, int pageSize)
        {
            DepartmentAllocateBudgetOverview departmentBudgetOverview = new DepartmentAllocateBudgetOverview();
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null,
                responseOpts = departmentBudgetOverview
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprBudgetExpenses = db.V_GET_DEPARTMENT_EXPENSES_BUDGET_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1));

                // หน่วยงานกลาง
                if (userAuthorizeProfile.DepAuthorize.Equals(1))
                {
                    if (null != areaId)
                        exprBudgetExpenses = exprBudgetExpenses.Where(e => e.AREA_ID.Equals(areaId));
                    if (null != depId)
                        exprBudgetExpenses = exprBudgetExpenses.Where(e => e.DEP_ID.Equals(depId));
                }
                else // หน่วยงานทั่วไป
                {
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => e.AREA_ID.Equals(userAuthorizeProfile.AreaId));
                    var depAuthorize = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, userAuthorizeProfile.DepId);
                    exprBudgetExpenses = exprBudgetExpenses.Where(e => depAuthorize.AssignDepartmentIds.Contains(e.DEP_ID));
                    if (null != depId)
                        exprBudgetExpenses = exprBudgetExpenses.Where(e => e.DEP_ID.Equals(depId));
                }

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

                // สรุปยอดภาพรวม
                if (exprBudgetExpenses.Any())
                {
                    // ข้อมูลจัดสรรงบประมาณเป็นก้อน ตามหมวดค่าใช้จ่าย
                    var exprAllocateByGroup = exprBudgetExpenses.GroupBy(e => new
                    {
                        e.YR,
                        e.DEP_ID,
                        e.PLAN_ID,
                        e.PRODUCE_ID,
                        e.ACTIVITY_ID,
                        e.BUDGET_TYPE_ID,
                        e.EXPENSES_GROUP_ID,
                        e.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                        e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                        e.EX_GRP_NET_BUDGET_AMOUNT
                    });

                    // เงินงบ
                    departmentBudgetOverview.AllocateBudgetAmounts = exprBudgetExpenses.Sum(e => e.ALLOCATE_BUDGET_AMOUNT) + exprAllocateByGroup.Sum(e => e.Key.EX_GRP_ALLOCATE_BUDGET_AMOUNT);
                    departmentBudgetOverview.ReportBudgetAmounts = exprBudgetExpenses.Sum(e => e.USE_BUDGET_AMOUNT);
                    departmentBudgetOverview.BalanceBudgetAmounts = departmentBudgetOverview.AllocateBudgetAmounts - departmentBudgetOverview.ReportBudgetAmounts;// exprBudgetExpenses.Sum(e => e.REMAIN_BUDGET_AMOUNT);
                    // เงินนอก
                    departmentBudgetOverview.AllocateOffBudgetAmounts = exprBudgetExpenses.Sum(e => e.ALLOCATE_OFF_BUDGET_AMOUNT) + exprAllocateByGroup.Sum(e => e.Key.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT); ;
                    departmentBudgetOverview.ReportOffBudgetAmounts = exprBudgetExpenses.Sum(e => e.USE_OFF_BUDGET_AMOUNT);
                    departmentBudgetOverview.BalanceOffBudgetAmounts = departmentBudgetOverview.AllocateOffBudgetAmounts - departmentBudgetOverview.ReportOffBudgetAmounts;// exprBudgetExpenses.Sum(e => e.REMAIN_OFF_BUDGET_AMOUNT);
                    // ภาพรวม (เงินงบ + เงินนอก)
                    departmentBudgetOverview.NetBudgetAmounts = exprBudgetExpenses.Sum(e => e.NET_BUDGET_AMOUNT) + exprAllocateByGroup.Sum(e => e.Key.EX_GRP_NET_BUDGET_AMOUNT); ;
                    departmentBudgetOverview.NetReportedAmounts = exprBudgetExpenses.Sum(e => e.NET_USE_BUDGET_AMOUNT);
                    departmentBudgetOverview.NetBalanceAmounts = departmentBudgetOverview.NetBudgetAmounts - departmentBudgetOverview.NetReportedAmounts;//exprBudgetExpenses.Sum(e => e.NET_REMAIN_BUDGET_AMOUNT);

                    // กรณีระบุ รหัสหน่วยงาน ให้ใช้ข้อมูล
                    // การจัดสรรล่าสุดจาก Master
                    var exprBudgetExpensesLatest = exprBudgetExpenses.OrderByDescending(e => e.DEP_LATEST_ALLOCATE_DATETIME).FirstOrDefault();
                    departmentBudgetOverview.LatestAllocateDatetime = exprBudgetExpensesLatest.DEP_LATEST_ALLOCATE_DATETIME;
                    departmentBudgetOverview.LatestAllocateName = exprBudgetExpensesLatest.DEP_LATEST_ALLOCATE_NAME;
                }

                // จัดรูปแบบข้อมูลเพื่อนำไปแสดงผลบน Grid
                var finalExprBudgetExpenses = exprBudgetExpenses.GroupBy(e => new
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
                    // ยอดจัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย
                    e.ALLOCATE_EXPENSES_GROUP_ID,
                    e.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                    e.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                    e.EX_GRP_NET_BUDGET_AMOUNT
                }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.Key.EXPENSES_GROUP_ORDER_SEQ)
                .Select(e => new
                {
                    GroupBy = e.Key,
                    Rows = e.Select(x => new
                    {
                        x.DEP_SORT_INDEX,
                        x.EXPENSES_ORDER_SEQ,
                        x.EXPENSES_NAME,
                        x.PROJECT_NAME,
                        x.DEP_NAME,
                        // ได้รับจัดสรรจากส่วนกลาง
                        x.NET_BUDGET_AMOUNT,
                        x.ALLOCATE_BUDGET_AMOUNT,
                        x.ALLOCATE_OFF_BUDGET_AMOUNT,
                        // รายงานผลการใช้จ่ายสุทธิ
                        x.NET_USE_BUDGET_AMOUNT,
                        x.USE_BUDGET_AMOUNT,
                        x.USE_OFF_BUDGET_AMOUNT,
                        // คงเหลือสุทธิ
                        x.NET_REMAIN_BUDGET_AMOUNT,
                        x.REMAIN_BUDGET_AMOUNT,
                        x.REMAIN_OFF_BUDGET_AMOUNT
                    }).OrderBy(x => x.EXPENSES_ORDER_SEQ).ThenBy(x => x.DEP_SORT_INDEX).ToList()
                }).ToList();

                // รายการค่าใช้จ่ายที่หน่วยงานได้รับจัดสรร
                pagging.totalRecords = finalExprBudgetExpenses.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = finalExprBudgetExpenses.Skip(offset).Take(pageSize).ToList();
                //.OrderBy(e => e.DEP_SORT_INDEX)
                //.ThenBy(e => e.PLAN_ORDER_SEQ)
                //.ThenBy(e => e.PRODUCE_ORDER_SEQ)
                //.ThenBy(e => e.ACTIVITY_ORDER_SEQ)
                //.ThenBy(e => e.BUDGET_TYPE_ORDER_SEQ)
                //.ThenBy(e => e.EXPENSES_GROUP_ORDER_SEQ)
                //.ThenBy(e => e.EXPENSES_ORDER_SEQ).Skip(offset).Take(pageSize).Select(e => new
                //{
                //    // ภาพรวมหน่วยงาน
                //    e.AREA_ID,
                //    e.DEP_ID,
                //    e.DEP_NAME,
                //    // ภาพรวมเงินงบประมาณ ของหน่วยงาน
                //    e.DEP_ALLOCATE_BUDGET_AMOUNT,
                //    e.DEP_USE_BUDGET_AMOUNT,
                //    e.DEP_REMAIN_BUDGET_AMOUNT,
                //    // ภาพรวมเงินนอกงบประมาณ ของหน่วยงาน
                //    e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT,
                //    e.DEP_USE_OFF_BUDGET_AMOUNT,
                //    e.DEP_REMAIN_OFF_BUDGET_AMOUNT,
                //    // ภาพรวม เงินงบ และ เงินนอกงบประมาณ ของหน่วยงาน
                //    e.DEP_NET_BUDGET_AMOUNT,
                //    e.DEP_NET_USE_BUDGET_AMOUNT,
                //    e.DEP_NET_REMAIN_BUDGET_AMOUNT,


                //    // ภาพรวมแต่ละรายการค่าใช้จ่าย
                //    e.PLAN_ID,
                //    e.PLAN_NAME,
                //    e.PRODUCE_ID,
                //    e.PRODUCE_NAME,
                //    e.ACTIVITY_ID,
                //    e.ACTIVITY_NAME,
                //    e.BUDGET_TYPE_ID,
                //    e.BUDGET_TYPE_NAME,
                //    e.EXPENSES_GROUP_ID,
                //    e.EXPENSES_GROUP_NAME,
                //    e.EXPENSES_ID,
                //    e.EXPENSES_NAME,
                //    e.PROJECT_ID,
                //    e.PROJECT_NAME,

                //    e.ALLOCATE_BUDGET_AMOUNT, // เงินงบ ที่ได้รับจัดสรรสุทธิ
                //    e.USE_BUDGET_AMOUNT, // รายงานผล
                //    e.REMAIN_BUDGET_AMOUNT, // งบประมาณคงเหลือสุทธิ

                //    e.ALLOCATE_OFF_BUDGET_AMOUNT, // เงินนอกงบ ที่ได้รับจัดสรรสุทธิ
                //    e.USE_OFF_BUDGET_AMOUNT,
                //    e.REMAIN_OFF_BUDGET_AMOUNT,

                //    e.NET_BUDGET_AMOUNT, // เงินงบ + เงินนอกงบ สุทธิ
                //    e.NET_USE_BUDGET_AMOUNT,
                //    e.NET_REMAIN_BUDGET_AMOUNT,

                //    e.LAST_ALLOCATE_DATETIME,
                //    LAST_ALLOCATE_NAME = db.T_PERSONNELs.Where(p => p.PERSON_ID.Equals(e.LAST_ALLOCATE_ID))
                //        .Select(p => string.Concat(p.PREFIX_NAME, p.FIRST_NAME, " ", p.LAST_NAME)).FirstOrDefault(),
                //    e.LATEST_REPORT_DATETIME,
                //    e.LATEST_REPORT_NAME
                //}).ToList();
            }
            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ภาพรวม การจัดสรรงบประมาณจากส่วนกลาง ลงมาให้หน่วยงาน และ การรายงานผล
        /// </summary>
        public class DepartmentAllocateBudgetOverview
        {
            public DepartmentAllocateBudgetOverview()
            {
                AllocateBudgetAmounts = decimal.Zero;
                ReportBudgetAmounts = decimal.Zero;
                BalanceBudgetAmounts = decimal.Zero;

                AllocateOffBudgetAmounts = decimal.Zero;
                ReportOffBudgetAmounts = decimal.Zero;
                BalanceOffBudgetAmounts = decimal.Zero;

                NetBudgetAmounts = decimal.Zero;
                NetReportedAmounts = decimal.Zero;
                NetBalanceAmounts = decimal.Zero;

                LatestAllocateName = "";
                LatestAllocateDatetime = null;
            }

            /// <summary>
            /// เงินงบประมาณ ที่จัดสรรให้หน่วยงาน
            /// </summary>
            public decimal AllocateBudgetAmounts { get; set; }

            /// <summary>
            /// รายงานผลเงินงบประมาณ สุทธิ
            /// </summary>
            public decimal ReportBudgetAmounts { get; set; }

            /// <summary>
            /// เงินงบประมาณคงเหลือ สุทธิ
            /// </summary>
            public decimal BalanceBudgetAmounts { get; set; }

            /// <summary>
            /// เงินนอกงบประมาณ ที่จัดสรรให้หน่วยงาน
            /// </summary>
            public decimal AllocateOffBudgetAmounts { get; set; }

            /// <summary>
            /// ยอดสะสมใช้จ่ายเงินงบนอกประมาณ
            /// </summary>
            public decimal ReportOffBudgetAmounts { get; set; }

            /// <summary>
            /// เงินนอกงบประมาณ คงเหลือสุทธิ
            /// </summary>
            public decimal BalanceOffBudgetAmounts { get; set; }

            /// <summary>
            /// งบประมาณสิทธิที่ได้รับจัดสรร (งบประมาณ + นอกงบประมาณ)
            /// </summary>
            public decimal NetBudgetAmounts { get; set; }

            /// <summary>
            /// ผู้จัดสรรงบประมาณ ลงให้หน่วยงานา คนล่าสุด
            /// </summary>
            public string LatestAllocateName { get; set; }

            /// <summary>
            /// วันที่จัดสรรงบประมาณลงให้หน่วยงานล่าสุด
            /// </summary>
            public DateTime? LatestAllocateDatetime { get; set; }

            /// <summary>
            /// ยอด รายงานผลการใช้จ่ายงบประมาณสุทธิ
            /// </summary>
            public decimal NetReportedAmounts { get; set; }

            /// <summary>
            /// เงินงบประมาณคงเหลือสุทธิ หลังจากหักจำนวนที่รายงานผล
            /// </summary>
            public decimal NetBalanceAmounts { get; set; }
        }
    }
}