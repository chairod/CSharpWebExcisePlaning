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
    /// สรุปภาพรวมการกันเงินงบประมาณของหน่วยงานภายใน
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class DepartmentBudgetReserveOverviewController : Controller
    {
        // GET: DepartmentBudgetReserveOverview
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DEPARTMENT_BUDGET_RESERVE_OVERVIEW_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_DEPARTMENT_BUDGET_RESERVE_OVERVIEW_MENU;
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
            ViewBag.DefaultSubDepartmentId = userAuthorizeProfile.SubDepId;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var appSettings = AppSettingProperty.ParseXml();
                var areaIdsCanReserveBudget = appSettings.GetAreaIdsCanReserveBudgetToList();
                // หน่วยงานภายในกรมสรรพสามิต
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => areaIdsCanReserveBudget.Contains(e.AREA_ID.Value))
                    .Select(e => new DepartmentShortFieldProperty()
                    {
                        DEP_ID = e.DEP_ID,
                        DEP_NAME = e.DEP_NAME
                    }).ToList();
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
        /// สรุปภาพรวมการกันเงินงบประมาณ และ รายการกันเงิน
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="depId"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="reserveType">1 = ผูกพัน, 2 = กันไว้เบิก</param>
        /// <param name="budgetType">1 = เงินงบ, 2 = นอกงบ</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? depId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int reserveType, int budgetType, int pageIndex, int pageSize)
        {
            DepartmentBudgetReserveOverview reserveBudgetOverview = new DepartmentBudgetReserveOverview();
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null,
                responseOpts = reserveBudgetOverview
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ไม่ต้องใช้เงื่อนไข Active = 1
                // ใบกันเงินบางรายการที่ถูกยกเลิก (Active = -1) อาจมีการเบิกจ่ายไปแล้ว ทำให้ยอด
                // กันเงินงบประมาณไม่ถูกต้อง
                var exprReserveBudget = db.V_GET_BUDGET_RESERVE_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear));

                if (null != depId)
                    exprReserveBudget = exprReserveBudget.Where(e => e.DEP_ID.Equals(depId));
                if (null != planId)
                    exprReserveBudget = exprReserveBudget.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprReserveBudget = exprReserveBudget.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprReserveBudget = exprReserveBudget.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprReserveBudget = exprReserveBudget.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprReserveBudget = exprReserveBudget.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    exprReserveBudget = exprReserveBudget.Where(e => e.EXPENSES_ID.Equals(expensesId));
                if (reserveType != 0)
                    exprReserveBudget = exprReserveBudget.Where(e => e.RESERVE_TYPE.Equals(reserveType));
                if (budgetType != 0)
                    exprReserveBudget = exprReserveBudget.Where(e => e.BUDGET_TYPE.Equals(budgetType));

                // สรุปยอดภาพรวม
                if (exprReserveBudget.Any())
                {
                    reserveBudgetOverview.ReserveBudgetAmounts = exprReserveBudget.Sum(e => e.BUDGET_TYPE.Equals(1) ? e.RESERVE_BUDGET_AMOUNT : decimal.Zero);
                    reserveBudgetOverview.ReserveOffBudgetAmounts = exprReserveBudget.Sum(e => e.BUDGET_TYPE.Equals(2) ? e.RESERVE_BUDGET_AMOUNT : decimal.Zero);

                    // กันเงิน
                    reserveBudgetOverview.ReserveBudgetType1Amounts = exprReserveBudget.Sum(e => e.RESERVE_TYPE.Equals(2) ? e.RESERVE_BUDGET_AMOUNT : decimal.Zero);
                    reserveBudgetOverview.ReserveBudgetType2Amounts = exprReserveBudget.Sum(e => e.RESERVE_TYPE.Equals(1) ? e.RESERVE_BUDGET_AMOUNT : decimal.Zero);
                    reserveBudgetOverview.NetReserveBudgetAmounts = exprReserveBudget.Sum(e => e.RESERVE_BUDGET_AMOUNT);

                    // เบิกจ่าย
                    reserveBudgetOverview.WithdrawalType1Amounts = exprReserveBudget.Sum(e => e.RESERVE_TYPE.Equals(2) ? e.USE_AMOUNT : decimal.Zero);
                    reserveBudgetOverview.WithdrawalType2Amounts = exprReserveBudget.Sum(e => e.RESERVE_TYPE.Equals(1) ? e.USE_AMOUNT : decimal.Zero);
                    reserveBudgetOverview.WithdrawalAmounts = exprReserveBudget.Sum(e => e.USE_AMOUNT);

                    // คงเหลือ
                    reserveBudgetOverview.BalanceType1Amounts = exprReserveBudget.Sum(e => e.RESERVE_TYPE.Equals(2) ? e.REMAIN_AMOUNT : decimal.Zero);
                    reserveBudgetOverview.BalanceType2Amounts = exprReserveBudget.Sum(e => e.RESERVE_TYPE.Equals(1) ? e.REMAIN_AMOUNT : decimal.Zero);
                    reserveBudgetOverview.BalanceAmounts = exprReserveBudget.Sum(e => e.REMAIN_AMOUNT);
                }

                // จัดรูปแบบข้อมูลเพื่อนำไปแสดงผลบน Grid
                var finalReserveBudget = exprReserveBudget.GroupBy(e => new
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
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.PROJECT_ID,
                    e.PROJECT_NAME
                }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.Key.EXPENSES_GROUP_ORDER_SEQ)
                .ThenBy(e => e.Key.EXPENSES_ORDER_SEQ)
                .ThenBy(e => e.Key.PROJECT_ID)
                .Select(e => new
                {
                    GroupBy = e.Key,
                    Rows = e.OrderBy(x => x.DEP_ORDER_SEQ).ToList()
                });

                // รายการค่าใช้จ่ายที่หน่วยงานได้รับจัดสรร
                pagging.totalRecords = finalReserveBudget.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = finalReserveBudget.Skip(offset).Take(pageSize).ToList();
                //    .Select(e => new
                //{
                //    // ภาพรวมหน่วยงาน
                //    e.RESERVE_ID,
                //    e.SUB_DEP_ID,
                //    e.SUB_DEP_NAME,
                //    e.DEP_ID,

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

                //    e.RESERVE_TYPE, // 1 = ผูกพัน, 2 = กันไว้เบิก
                //    e.BUDGET_TYPE, // 1 = เงินงบ, 2 = นอกงบ
                //    e.RESERVE_BUDGET_AMOUNT,
                //    e.USE_AMOUNT,
                //    e.REMAIN_AMOUNT,
                //    e.CASHBACK_AMOUNT, // ยอดเบิกเกินส่งคืน

                //    e.CREATED_DATETIME,
                //    e.RESERVE_NAME,
                //    e.LATEST_WITHDRAWAL_DATETIME,
                //    e.LATEST_WITHDRAWAL_NAME
                //}).ToList();
            }
            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ภาพรวมการกันเงินงบประมาณของหน่วยงานภายในกรมสรรพสามิต
        /// </summary>
        public class DepartmentBudgetReserveOverview
        {
            public DepartmentBudgetReserveOverview()
            {
                ReserveBudgetAmounts = decimal.Zero;
                ReserveOffBudgetAmounts = decimal.Zero;

                ReserveBudgetType1Amounts = decimal.Zero;
                ReserveBudgetType2Amounts = decimal.Zero;
                NetReserveBudgetAmounts = decimal.Zero;

                WithdrawalType1Amounts = decimal.Zero;
                WithdrawalType2Amounts = decimal.Zero;
                WithdrawalAmounts = decimal.Zero;

                BalanceType1Amounts = decimal.Zero;
                BalanceType2Amounts = decimal.Zero;
                BalanceAmounts = decimal.Zero;
            }

            /// <summary>
            /// เงินงบประมาณ ที่หน่วยงานภายในกันเงิน
            /// </summary>
            public decimal ReserveBudgetAmounts { get; set; }

            /// <summary>
            /// เงินนอกงบประมาณ ที่หน่วยงานภายในกันเงิน
            /// </summary>
            public decimal ReserveOffBudgetAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินสุทธิที่กันไว้เบิก
            /// </summary>
            public decimal ReserveBudgetType1Amounts { get; set; }

            /// <summary>
            /// จำนวนเงินสุทธิที่ ผูกพัน
            /// </summary>
            public decimal ReserveBudgetType2Amounts { get; set; }

            /// <summary>
            /// กันเงินสุทธิ (กันไว้เบิก + ผู้พัน)
            /// </summary>
            public decimal NetReserveBudgetAmounts { get; set; }

            /// <summary>
            /// ยอดเบิกจ่าย (กันไว้เบิก)
            /// </summary>
            public decimal WithdrawalType1Amounts { get; set; }

            /// <summary>
            /// ยอดเบิกจ่าย (ผูกพัน)
            /// </summary>
            public decimal WithdrawalType2Amounts { get; set; }

            /// <summary>
            /// ยอด ที่เบิกจ่ายสุทธิ
            /// </summary>
            public decimal WithdrawalAmounts { get; set; }

            /// <summary>
            /// ยอดคงเหลือ กันไว้เบิก
            /// </summary>
            public decimal BalanceType1Amounts { get; set; }

            /// <summary>
            /// ยอดคงเหลือ ผูกพัน
            /// </summary>
            public decimal BalanceType2Amounts { get; set; }

            /// <summary>
            /// เงินงบประมาณคงเหลือสุทธิ หลังจากหักจำนวนที่รายงานผล
            /// </summary>
            public decimal BalanceAmounts { get; set; }
        }
    }
}