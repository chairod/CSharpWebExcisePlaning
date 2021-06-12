using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{

    /// <summary>
    /// R001-รายงานผลการใช้จ่ายงบประมาณภาพรวมกรมสรรพสามิต
    /// สรุปผลการใช้จ่ายงบประมาณ ได้แก่ เงินประจำงวด ผูกพัน เบิกจ่าย คงเหลือ มองในส่วน ทั้งกรมสรรพสามิตฯ ภูมิภาค และแยกเป็นเงินงบและเงินนอกงบ
    /// 
    /// Template: RptSummaryBudgetUsed.xlsx
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptSummaryBudgetUsedController : Controller
    {
        // GET: RptSummaryBudgetUsed
        public ActionResult RptSummaryBudgetUsedForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_RptSummaryBudgetUsed);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_RptSummaryBudgetUsed;
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
                Text = menuReportItem.GroupName,
                CssIcon = menuReportItem.GroupIcon,
                ControllerName = "ReportMainPage",
                ActionName = "GetForm"
            });
            breadcrumps.Add(new Breadcrump()
            {
                Text = menuItem.MenuName,
                CssIcon = menuItem.MenuIcon,
                ControllerName = menuItem.RouteName,
                ActionName = menuItem.ActionName
            });
            ViewBag.Breadcrumps = breadcrumps;


            ViewBag.DeafultFiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            ViewBag.DefaultDepId = userAuthorizeProfile.DepId;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                //ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).ToList();
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new PlanShortFieldProperty()
                    {
                        PLAN_ID = e.PLAN_ID,
                        PLAN_NAME = e.PLAN_NAME
                    }).ToList();
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
        /// ค้นหาสรุปภาพรวม งบประมาณของกรมสรรพสามิตฯ
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="type">export = ส่งออกไปยัง Xls</param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, string type, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(4) {
                { "rows", null },
                { "filename", null },
                { "resultFilename", null },
                { "errorText", null }
            };
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_SUMMARY_OVERALL_BUDGETs.Where(e => e.YR.Equals(fiscalYear));
                if (null != planId)
                    expr = expr.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    expr = expr.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    expr = expr.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    expr = expr.Where(e => e.EXPENSES_ID.Equals(expensesId));


                if (!expr.Any())
                {
                    res["errorText"] = "ไม่พบข้อมูล";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var rows = new List<RptSummaryBudgetUseProperty>();

                // ข้อมูลการกันเงิน และ เบิกจ่าย (จะมีเฉพาะส่วนกลาง)
                var exprReserve = expr.GroupBy(e => new
                {
                    e.PLAN_ID,
                    e.PRODUCE_ID,
                    e.ACTIVITY_ID,
                    e.BUDGET_TYPE_ID,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_ID,
                    e.PROJECT_ID,
                    e.RESERVE_BUDGET_AMOUNT,
                    e.RESERVE_OFF_BUDGET_AMOUNT,
                    e.RESERVE_REMAIN_BUDGET_AMOUNT,
                    e.RESERVE_REMAIN_OFF_BUDGET_AMOUNT,
                    e.RESERVE_USE_BUDGET_AMOUNT,
                    e.RESERVE_USE_OFF_BUDGET_AMOUNT
                }).Select(e => e.Key);
                decimal reserveBudgetAmount = exprReserve.Sum(e => null == e.RESERVE_BUDGET_AMOUNT ? decimal.Zero : e.RESERVE_BUDGET_AMOUNT.Value),
                    reservePayBudgetAmount = exprReserve.Sum(e => null == e.RESERVE_USE_BUDGET_AMOUNT ? decimal.Zero : e.RESERVE_USE_BUDGET_AMOUNT.Value),
                    reserveOffBudgetAmount = exprReserve.Sum(e => null == e.RESERVE_OFF_BUDGET_AMOUNT ? decimal.Zero : e.RESERVE_OFF_BUDGET_AMOUNT.Value),
                    reservePayOffBudgetAmount = exprReserve.Sum(e => null == e.RESERVE_USE_OFF_BUDGET_AMOUNT ? decimal.Zero : e.RESERVE_USE_OFF_BUDGET_AMOUNT.Value);

                // ข้อมูลการจัดสรร และ เบิกจ่าย (เฉพาะหน่วยงานภูมิภาค)
                var exprAllocateGrp = expr.GroupBy(e => new
                {
                    e.PLAN_ID,
                    e.PRODUCE_ID,
                    e.ACTIVITY_ID,
                    e.BUDGET_TYPE_ID,
                    e.EXPENSES_GROUP_ID,
                    e.DEP_GRP_ALLOCATE_BUDGET_AMOUNT,
                    e.DEP_GRP_ALLOCATE_OFF_BUDGET_AMOUNT
                }).Select(e => e.Key);
                var exprAllocateExpenses = expr.GroupBy(e => new
                {
                    e.PLAN_ID,
                    e.PRODUCE_ID,
                    e.ACTIVITY_ID,
                    e.BUDGET_TYPE_ID,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_ID,
                    e.PROJECT_ID,
                    e.DEP_ALLOCATE_BUDGET_AMOUNT,
                    e.DEP_USE_BUDGET_AMOUNT,
                    e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT,
                    e.DEP_USE_OFF_BUDGET_AMOUNT
                }).Select(e => e.Key);
                decimal allocateBudgetAmount = exprAllocateExpenses.Sum(e => e.DEP_ALLOCATE_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_ALLOCATE_BUDGET_AMOUNT.Value),
                    useBudgetAmount = exprAllocateExpenses.Sum(e => e.DEP_USE_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_USE_BUDGET_AMOUNT.Value),
                    allocateOffBudgetAmount = exprAllocateExpenses.Sum(e => e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT.Value),
                    useOffBudgetAmount = exprAllocateExpenses.Sum(e => e.DEP_USE_OFF_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_USE_OFF_BUDGET_AMOUNT.Value);
                if (exprAllocateGrp.Any())
                {
                    allocateBudgetAmount += exprAllocateGrp.Sum(e => e.DEP_GRP_ALLOCATE_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_GRP_ALLOCATE_BUDGET_AMOUNT.Value);
                    allocateOffBudgetAmount += exprAllocateGrp.Sum(e => e.DEP_GRP_ALLOCATE_OFF_BUDGET_AMOUNT == null ? decimal.Zero : e.DEP_GRP_ALLOCATE_OFF_BUDGET_AMOUNT.Value);
                }


                // ภาพรวมทั้งกรม
                var firstItem = expr.First();
                var exprActualBudget = expr.GroupBy(e => new
                {
                    e.PLAN_ID,
                    e.PRODUCE_ID,
                    e.ACTIVITY_ID,
                    e.BUDGET_TYPE_ID,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_ID,
                    e.ACTUAL_BUDGET_AMOUNT,
                    e.ACTUAL_OFF_BUDGET_AMOUNT
                }).Select(e => e.Key);
                decimal totalBudgetAmount = exprActualBudget.Sum(e => e.ACTUAL_BUDGET_AMOUNT) + (firstItem.OFF_BUDGET_SPREAD_TO_EXPENSES ? exprActualBudget.Sum(e => e.ACTUAL_OFF_BUDGET_AMOUNT) : firstItem.MAS_ACTUAL_OFF_BUDGET_AMOUNT)
                    , totalAllocateAmount = reserveBudgetAmount + allocateBudgetAmount + reserveOffBudgetAmount + allocateOffBudgetAmount
                    , totalPayAmount = reservePayBudgetAmount + useBudgetAmount + reservePayOffBudgetAmount + useOffBudgetAmount
                    , totalBalance = totalBudgetAmount - totalAllocateAmount - totalPayAmount;
                rows.Add(new RptSummaryBudgetUseProperty(1, "กรมสรรพสามิต", totalBudgetAmount, totalAllocateAmount, totalPayAmount, totalBalance, totalBudgetAmount.CompareTo(decimal.Zero) == 0 ? 0 : Math.Round(totalPayAmount / totalBudgetAmount * 100, 2)));

                // แยกเป็นเงินงบประมาณ
                totalBudgetAmount = exprActualBudget.Sum(e => e.ACTUAL_BUDGET_AMOUNT);
                totalAllocateAmount = reserveBudgetAmount + allocateBudgetAmount;
                totalPayAmount = reservePayBudgetAmount + useBudgetAmount;
                totalBalance = totalBudgetAmount - totalAllocateAmount - totalPayAmount;
                rows.Add(new RptSummaryBudgetUseProperty(2, "เงินงบประมาณ", totalBudgetAmount, totalAllocateAmount, totalPayAmount, totalBalance, totalBudgetAmount.CompareTo(decimal.Zero) == 0 ? 0 : Math.Round(totalPayAmount / totalBudgetAmount * 100, 2)));

                // เงินงบประมาณ (ส่วนกลาง)
                totalBudgetAmount = decimal.Zero;
                totalAllocateAmount = reserveBudgetAmount;
                totalPayAmount = reservePayBudgetAmount;
                totalBalance = totalAllocateAmount - totalPayAmount;
                rows.Add(new RptSummaryBudgetUseProperty(3, "ส่วนกลาง", totalBudgetAmount, totalAllocateAmount, totalPayAmount, totalBalance, totalAllocateAmount.CompareTo(decimal.Zero) == 0 ? 0 : Math.Round(totalPayAmount / totalAllocateAmount * 100, 2)));

                // เงินงบประมาณ (ภูมิภาค)
                totalBudgetAmount = allocateBudgetAmount;
                totalAllocateAmount = decimal.Zero;
                totalPayAmount = useBudgetAmount;
                totalBalance = totalBudgetAmount - totalPayAmount;
                rows.Add(new RptSummaryBudgetUseProperty(4, "ภูมิภาค", totalBudgetAmount, totalAllocateAmount, totalPayAmount, totalBalance, totalBudgetAmount.CompareTo(decimal.Zero) == 0 ? 0 : Math.Round(totalPayAmount / totalBudgetAmount * 100, 2)));



                // แยกเป็นเงินนอกงบประมาณ
                totalBudgetAmount = (firstItem.OFF_BUDGET_SPREAD_TO_EXPENSES ? exprActualBudget.Sum(e => e.ACTUAL_OFF_BUDGET_AMOUNT) : firstItem.MAS_ACTUAL_OFF_BUDGET_AMOUNT);
                totalAllocateAmount = reserveOffBudgetAmount + allocateOffBudgetAmount;
                totalPayAmount = reservePayOffBudgetAmount + useOffBudgetAmount;
                totalBalance = totalBudgetAmount - totalAllocateAmount - totalPayAmount;
                rows.Add(new RptSummaryBudgetUseProperty(5, "เงินนอกงบประมาณ", totalBudgetAmount, totalAllocateAmount, totalPayAmount, totalBalance, totalBudgetAmount.CompareTo(decimal.Zero) == 0 ? 0 : Math.Round(totalPayAmount / totalBudgetAmount * 100, 2)));


                // เงินนอกงบประมาณ (ส่วนกลาง)
                totalBudgetAmount = decimal.Zero;
                totalAllocateAmount = reserveOffBudgetAmount;
                totalPayAmount = reservePayOffBudgetAmount;
                totalBalance = totalAllocateAmount - totalPayAmount;
                rows.Add(new RptSummaryBudgetUseProperty(6, "ส่วนกลาง", totalBudgetAmount, totalAllocateAmount, totalPayAmount, totalBalance, totalAllocateAmount.CompareTo(decimal.Zero) == 0 ? 0 : Math.Round(totalPayAmount / totalAllocateAmount * 100, 2)));

                // เงินนอกงบประมาณ (ภูมิภาค)
                totalBudgetAmount = allocateOffBudgetAmount;
                totalAllocateAmount = decimal.Zero;
                totalPayAmount = useOffBudgetAmount;
                totalBalance = totalBudgetAmount - totalPayAmount;
                rows.Add(new RptSummaryBudgetUseProperty(7, "ภูมิภาค", totalBudgetAmount, totalAllocateAmount, totalPayAmount, totalBalance, totalBudgetAmount.CompareTo(decimal.Zero) == 0 ? 0 : Math.Round(totalPayAmount / totalBudgetAmount * 100, 2)));

                res["rows"] = rows;



                // เขียนค่าลงใน XLS
                if ("export".Equals(type))
                {
                    var appSetting = AppSettingProperty.ParseXml();
                    var usrAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                    string filename = string.Format("{0}_รายงานผลการใช้จ่ายภาพรวมของกรมสรรพสามิต.xls", usrAuthorizeProfile.EmpId);
                    string template = string.Format("{0}/RptSummaryBudgetUsed.xlsx", appSetting.ReportTemplatePath, filename);
                    using (ExcelPackage xlsApp = new ExcelPackage(new System.IO.FileInfo(template)))
                    {
                        ExportHelper exportor = new ExportHelper(xlsApp.Workbook.Worksheets[1]);
                        int rowIndex = 4;
                        rows.ForEach(row =>
                        {
                            string spacebar = "";
                            if (rowIndex == 6 || rowIndex == 7 || rowIndex == 9 || rowIndex == 10)
                                spacebar = "    ";
                            exportor.SetCellTextVal(string.Format("A{0}", rowIndex), spacebar + row.ItemText, true);
                            exportor.SetCellCurrencyVal(string.Format("B{0}", rowIndex), row.ActualBudgetAmount, true);
                            exportor.SetCellCurrencyVal(string.Format("C{0}", rowIndex), row.ReserveBudgetAmount, true);
                            exportor.SetCellCurrencyVal(string.Format("D{0}", rowIndex), row.PayBudgetAmount, true);
                            exportor.SetCellCurrencyVal(string.Format("E{0}", rowIndex), row.BalanceAmount, true);
                            exportor.SetCellCurrencyVal(string.Format("F{0}", rowIndex), row.UsePercent, true);

                            if (rowIndex == 4 || rowIndex == 5 || rowIndex == 8)
                            {
                                ExcelRange range = exportor.GetRange(string.Format("A{0}:F{0}", rowIndex));
                                range.Style.Font.Bold = true;
                            }

                            rowIndex++;
                        });

                        res["filename"] = filename;
                        res["resultFilename"] = filename;
                        string filePath = string.Format("{0}/{1}", appSetting.TemporaryPath, filename);
                        xlsApp.SaveAs(new System.IO.FileInfo(filePath));
                    }
                }
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class RptSummaryBudgetUseProperty
        {
            public RptSummaryBudgetUseProperty(short sortIndex, string itemText, decimal actualBudgetAmount, decimal reserveBudgetAmount, decimal payBudgetAmount, decimal balanceAmount, decimal usePercent)
            {
                SortIndex = sortIndex;
                ItemText = itemText;
                ActualBudgetAmount = actualBudgetAmount;
                ReserveBudgetAmount = reserveBudgetAmount;
                PayBudgetAmount = payBudgetAmount;
                BalanceAmount = balanceAmount;
                UsePercent = usePercent;
            }

            /// <summary>
            /// ลำดับการจัดเรียงรายการ
            /// </summary>
            public short SortIndex { get; set; }

            /// <summary>
            /// รายละเอียดสำหรับแสดงผล
            /// </summary>
            public string ItemText { get; set; }

            /// <summary>
            /// เงินประจำงวด หรือ งบประมาณที่ได้รับจัดสรร
            /// </summary>
            public decimal? ActualBudgetAmount { get; set; }

            /// <summary>
            /// กันเงินงบประมาณ (เฉพาะส่วนกลาง)
            /// </summary>
            public decimal? ReserveBudgetAmount { get; set; }

            /// <summary>
            /// จำนวนเงินที่ใช้จ่าย จากการกันเงิน หรือ ได้รับจัดสรร
            /// </summary>
            public decimal? PayBudgetAmount { get; set; }

            /// <summary>
            /// ยอดคงเหลือ ActualBudgetAmount - (ReserveBudgetAmount + PayBudgetAmount)
            /// </summary>
            public decimal? BalanceAmount { get; set; }

            /// <summary>
            /// % ผลการใช้จ่าย
            /// </summary>
            public decimal UsePercent { get; set; }
        }
    }
}