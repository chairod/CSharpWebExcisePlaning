using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// งบประมาณที่จัดสรรลงให้กับหน่วยงานภูมิภาค และ รายการกันเงินงบประมาณ
    /// 
    /// R003-รายงานงบประมาณรายจ่ายประจำปีงบประมาณ
    /// R004-รายงานแผนรายรับรายจ่ายเงินนอกงบประมาณประจำปี
    /// 
    /// Template: RptPlansIncomeOfYear.xlsx
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptPlansIncomeOfYearController : Controller
    {
        // GET: RptPlansIncomeOfYear
        public ActionResult RptPlansIncomeOfYearForm(string pageType)
        {
            string currentMenuConst = AppConfigConst.MENU_CONST_REPORT_SUMMARY_ALLOCATE_BUDGET_MENU;
            if ("off_budget".Equals(pageType))
                currentMenuConst = AppConfigConst.MENU_CONST_REPORT_SUMMARY_ALLOCATE_OFF_BUDGET_MENU;

            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(currentMenuConst);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

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
                ActionName = menuItem.ActionName,
                QueryString = string.Format("pageType={0}", pageType)
            });
            ViewBag.Breadcrumps = breadcrumps;

            ViewBag.PageType = pageType;
            ViewBag.DefaultFiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            ViewBag.DefaultDepId = userAuthorizeProfile.DepId;
            ViewBag.CanSelectDepartment = userAuthorizeProfile.CanSelectDepartment;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // เขตพื้นที่
                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = e.AREA_NAME
                }).ToList();

                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();

                // งบรายจ่าย (งบดำเนินงาน งบลงทุน)
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();
            }

            return View();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="areaId"></param>
        /// <param name="depId"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="budgetType">1 = เงินงบ, 2 = เงินนอกงบ</param>
        /// <param name="excludeReserveFlag">Y = ค้นหาเฉพาะที่จัดสรรให้ภูมิภาค</param>
        /// <returns></returns>
        public ActionResult Retrieve(int fiscalYear, int? areaId, int? depId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int budgetType, string excludeReserveFlag)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(3) {
                { "filename", null },
                { "resultFilename", null },
                { "errorText", null }
            };

            var usrAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_SUMMARY_BUDGET_ALLOCATEs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(fiscalYear));

                var appSettings = AppSettingProperty.ParseXml();
                var exprReserve = db.V_GET_BUDGET_RESERVE_INFORMATIONs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(fiscalYear));
                if (null != areaId && appSettings.GetAreaIdsCanReserveBudgetToList().IndexOf(areaId.Value) == -1)
                    exprReserve = exprReserve.Where(e => 1 == 2);

                // หน่วยงานกลาง
                if (usrAuthorizeProfile.DepAuthorize.Equals(1))
                {
                    if (null != areaId)
                        expr = expr.Where(e => e.AREA_ID == null || (e.AREA_ID != null && e.AREA_ID.Equals(areaId)));
                    if (null != depId)
                    {
                        expr = expr.Where(e => e.DEP_ID == null || (e.DEP_ID != null && e.DEP_ID.Equals(depId)));
                        exprReserve = exprReserve.Where(e => e.DEP_ID.Equals(depId));
                    }
                }
                else // หน่วยงานทั่วไป
                {
                    expr = expr.Where(e => e.AREA_ID == null || (e.AREA_ID != null && e.AREA_ID.Equals(usrAuthorizeProfile.AreaId)));
                    var depAuthorize = DepartmentAuthorizeFilterProperty.Verfity(usrAuthorizeProfile, usrAuthorizeProfile.DepId);
                    expr = expr.Where(e => e.DEP_ID == null || (e.DEP_ID != null && depAuthorize.AssignDepartmentIds.Contains(e.DEP_ID.Value)));
                    if (null != depId)
                    {
                        expr = expr.Where(e => e.DEP_ID == null || (e.DEP_ID != null && e.DEP_ID.Equals(depId)));
                        exprReserve = exprReserve.Where(e => e.DEP_ID.Equals(depId));
                    }
                }

                if (null != planId)
                {
                    expr = expr.Where(e => e.PLAN_ID.Equals(planId));
                    exprReserve = exprReserve.Where(e => e.PLAN_ID.Equals(planId));
                }
                if (null != produceId)
                {
                    expr = expr.Where(e => e.PRODUCE_ID.Equals(produceId));
                    exprReserve = exprReserve.Where(e => e.PRODUCE_ID.Equals(produceId));
                }
                if (null != activityId)
                {
                    expr = expr.Where(e => e.ACTIVITY_ID.Equals(activityId));
                    exprReserve = exprReserve.Where(e => e.ACTIVITY_ID.Equals(activityId));
                }
                if (null != budgetTypeId)
                {
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                    exprReserve = exprReserve.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                }
                if (null != expensesGroupId)
                {
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                    exprReserve = exprReserve.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                }
                if (null != expensesId)
                {
                    expr = expr.Where(e => e.EXPENSES_ID.Equals(expensesId));
                    exprReserve = exprReserve.Where(e => e.EXPENSES_ID.Equals(expensesId));
                }

                if (!expr.Any() && !exprReserve.Any())
                {
                    res["errorText"] = "ไม่พบข้อมูล";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var primaryExpr = expr.Select(e => new
                {
                    e.DEP_ID,
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
                    e.EXPENSES_MASTER_ID,
                    e.EXPENSES_MASTER_NAME,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG,
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.PROJECT_ID,
                    e.PROJECT_NAME,
                    e.ALLOCATE_PROJECT_ID, // รหัสโครงการที่จัดสรรลงให้กับหน่วยงานภูมิภาค ใช้ในกรณีที่จัดสรรแล้วมีการเพิ่มโครงการในภายหลัง
                    e.ALLOCATE_BUDGET_AMOUNT,
                    e.ALLOCATE_OFF_BUDGET_AMOUNT,
                    e.ALLOCATE_EXPENSES_GROUP_ID,
                    e.ALLOCATE_GRP_BUDGET_AMOUNT,
                    e.ALLOCATE_GRP_OFF_BUDGET_AMOUNT
                }).AsEnumerable();

                // Join ข้อมูลการกันเงินงบประมาณ
                if ("N".Equals(excludeReserveFlag))
                {
                    short? defaultAllowGroupFlag = 0;
                    decimal? defaultDecimal = 0;
                    primaryExpr = primaryExpr.Concat(exprReserve.Select(e => new
                    {
                        e.DEP_ID,
                        e.PLAN_ID,
                        e.PLAN_NAME,
                        PLAN_ORDER_SEQ = e.PLAN_ORDER_SEQ.Value,
                        e.PRODUCE_ID,
                        e.PRODUCE_NAME,
                        PRODUCE_ORDER_SEQ = e.PRODUCE_ORDER_SEQ.Value,
                        e.ACTIVITY_ID,
                        e.ACTIVITY_NAME,
                        ACTIVITY_ORDER_SEQ = e.ACTIVITY_ORDER_SEQ.Value,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.BUDGET_TYPE_ORDER_SEQ,
                        e.EXPENSES_MASTER_ID,
                        e.EXPENSES_MASTER_NAME,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_GROUP_ORDER_SEQ,
                        EXPENSES_GROUP_ALLOCATE_GROUP_FLAG = defaultAllowGroupFlag,
                        e.EXPENSES_ID,
                        e.EXPENSES_NAME,
                        EXPENSES_ORDER_SEQ = e.EXPENSES_ORDER_SEQ.Value,
                        e.PROJECT_ID,
                        e.PROJECT_NAME,
                        ALLOCATE_PROJECT_ID = e.PROJECT_ID, // รหัสโครงการที่กันเงิน ใช้ในกรณีที่จัดสรรแล้วมีการเพิ่มโครงการในภายหลัง
                        ALLOCATE_BUDGET_AMOUNT = e.BUDGET_TYPE.Equals(1) ? e.RESERVE_BUDGET_AMOUNT : defaultDecimal,
                        ALLOCATE_OFF_BUDGET_AMOUNT = e.BUDGET_TYPE.Equals(2) ? e.RESERVE_BUDGET_AMOUNT : defaultDecimal,
                        ALLOCATE_EXPENSES_GROUP_ID = e.RESERVE_ID,
                        ALLOCATE_GRP_BUDGET_AMOUNT = defaultDecimal,
                        ALLOCATE_GRP_OFF_BUDGET_AMOUNT = defaultDecimal
                    }).AsEnumerable());
                }

                // จัดกลุ่ม งบรายจ่าย กลุ่มหมวดแผนงาน หมวดแผนงาน ค่าใช้จ่าย โครงการ
                // สำหรับเตรียมเขียนค่าลงไฟล์ XLS
                var exprExpenses = primaryExpr.GroupBy(e => new { e.BUDGET_TYPE_ID, e.BUDGET_TYPE_NAME, e.BUDGET_TYPE_ORDER_SEQ })
                        .OrderBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                        .Select(e => new
                        {
                            e.Key.BUDGET_TYPE_ID,
                            e.Key.BUDGET_TYPE_NAME,
                            GroupMasters = e.GroupBy(x => new
                            {
                                x.EXPENSES_MASTER_NAME,
                                x.EXPENSES_MASTER_ID
                            }).OrderBy(x => x.Key.EXPENSES_MASTER_NAME)
                            .Select(y => new
                            {
                                y.Key.EXPENSES_MASTER_ID,
                                y.Key.EXPENSES_MASTER_NAME,
                                ExpensesGroups = y.GroupBy(g => new
                                {
                                    g.EXPENSES_GROUP_ID,
                                    g.EXPENSES_GROUP_NAME,
                                    g.EXPENSES_GROUP_ORDER_SEQ,
                                    g.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG
                                }).OrderBy(g => g.Key.EXPENSES_GROUP_ORDER_SEQ)
                                .Select(g => new
                                {
                                    g.Key.EXPENSES_GROUP_ID,
                                    g.Key.EXPENSES_GROUP_NAME,
                                    g.Key.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG,
                                    Expenses = g.GroupBy(m => new
                                    {
                                        m.EXPENSES_ID,
                                        m.EXPENSES_NAME,
                                        m.EXPENSES_ORDER_SEQ
                                    }).OrderBy(m => m.Key.EXPENSES_ORDER_SEQ)
                                    .Select(m => new
                                    {
                                        m.Key.EXPENSES_ID,
                                        m.Key.EXPENSES_NAME,
                                        Projects = m.Where(z => z.PROJECT_ID != null).GroupBy(z => new
                                        {
                                            z.PROJECT_ID,
                                            z.PROJECT_NAME
                                        }).Select(z => z.Key).ToList()
                                    }).ToList()
                                }).ToList()
                            }).ToList()
                        }).ToList();


                // จัดกลุ่มข้อมูลเพื่อเตรียม นำออก Excel
                var finalExpr = primaryExpr.GroupBy(e => new { e.PLAN_ID, e.PLAN_NAME, e.PLAN_ORDER_SEQ })
                    .OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                    .Select(e => new
                    {
                        e.Key.PLAN_ID,
                        e.Key.PLAN_NAME,
                        Produces = e.GroupBy(x => new { x.PRODUCE_ID, x.PRODUCE_NAME, x.PRODUCE_ORDER_SEQ })
                            .OrderBy(x => x.Key.PRODUCE_ORDER_SEQ)
                            .Select(x => new
                            {
                                x.Key.PRODUCE_ID,
                                x.Key.PRODUCE_NAME,
                                Activities = x.GroupBy(y => new { y.ACTIVITY_ID, y.ACTIVITY_NAME, y.ACTIVITY_ORDER_SEQ })
                                    .OrderBy(y => y.Key.ACTIVITY_ORDER_SEQ)
                                    .Select(y => new
                                    {
                                        y.Key.ACTIVITY_ID,
                                        y.Key.ACTIVITY_NAME,
                                        Items = new Func<List<ItemProperty>>(() =>
                                        {
                                            List<ItemProperty> newItems = new List<ItemProperty>();
                                            // งบรายจ่าย
                                            exprExpenses.ForEach(budgetTypeItem =>
                                            {
                                                var budgetTypeItemExpr = primaryExpr.Where(m => m.PLAN_ID.Equals(e.Key.PLAN_ID)
                                                            && m.PRODUCE_ID.Equals(x.Key.PRODUCE_ID)
                                                            && m.ACTIVITY_ID.Equals(y.Key.ACTIVITY_ID)
                                                            && m.BUDGET_TYPE_ID.Equals(budgetTypeItem.BUDGET_TYPE_ID))
                                                    .GroupBy(m => new
                                                    {
                                                        m.DEP_ID,
                                                        m.PLAN_ID,
                                                        m.PRODUCE_ID,
                                                        m.ACTIVITY_ID,
                                                        m.BUDGET_TYPE_ID,
                                                        m.EXPENSES_GROUP_ID,
                                                        m.EXPENSES_ID,
                                                        m.ALLOCATE_PROJECT_ID,
                                                        ALLOCATE_AMOUNT = budgetType.Equals(1) ? m.ALLOCATE_BUDGET_AMOUNT : m.ALLOCATE_OFF_BUDGET_AMOUNT,
                                                        ALLOCATE_GRP_AMOUNT = budgetType.Equals(1) ? m.ALLOCATE_GRP_BUDGET_AMOUNT : m.ALLOCATE_GRP_OFF_BUDGET_AMOUNT,
                                                        m.ALLOCATE_EXPENSES_GROUP_ID
                                                    }).Select(m => m.Key).ToList();
                                                bool isBudgetType = budgetTypeItemExpr.Any();
                                                decimal allocateAmount = decimal.Zero;
                                                if (isBudgetType)
                                                {
                                                    var budgetTypeItemGrpExpr = budgetTypeItemExpr.GroupBy(m => new
                                                    {
                                                        m.ALLOCATE_EXPENSES_GROUP_ID,
                                                        m.PLAN_ID,
                                                        m.PRODUCE_ID,
                                                        m.ACTIVITY_ID,
                                                        m.BUDGET_TYPE_ID,
                                                        m.EXPENSES_GROUP_ID,
                                                        m.ALLOCATE_GRP_AMOUNT
                                                    }).Select(m => m.Key).ToList();
                                                    allocateAmount = budgetTypeItemExpr.Sum(m => m.ALLOCATE_AMOUNT == null ? decimal.Zero : m.ALLOCATE_AMOUNT.Value)
                                                        + budgetTypeItemGrpExpr.Sum(m => m.ALLOCATE_GRP_AMOUNT == null ? decimal.Zero : m.ALLOCATE_GRP_AMOUNT.Value);
                                                }
                                                newItems.Add(new ItemProperty(budgetTypeItem.BUDGET_TYPE_NAME, allocateAmount, true, "#DAEEF3", true));

                                                // กลุ่มหมวดค่าใช้จ่าย
                                                budgetTypeItem.GroupMasters.ForEach(groupMasterItem =>
                                                {
                                                    if (null != groupMasterItem.EXPENSES_MASTER_ID)
                                                        newItems.Add(new ItemProperty(groupMasterItem.EXPENSES_MASTER_NAME, null, true, string.Empty, false));

                                                    // หมวดค่าใช้จ่าย
                                                    groupMasterItem.ExpensesGroups.ForEach(expensesGroupItem =>
                                                    {
                                                        var expensesGroupExpr = budgetTypeItemExpr.Where(m => m.EXPENSES_GROUP_ID.Equals(expensesGroupItem.EXPENSES_GROUP_ID)).ToList();
                                                        bool isExpensesGroup = expensesGroupExpr.Any();
                                                        string expensesGroupName = string.Format("    {0} {1}", expensesGroupItem.EXPENSES_GROUP_NAME, expensesGroupItem.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG == 1 ? "[จัดสรรเป็นก้อน]" : "");
                                                        allocateAmount = decimal.Zero;
                                                        if (isExpensesGroup)
                                                            allocateAmount = expensesGroupExpr.Sum(m => m.ALLOCATE_AMOUNT == null ? decimal.Zero : m.ALLOCATE_AMOUNT.Value)
                                                                + expensesGroupExpr.Sum(m => m.ALLOCATE_GRP_AMOUNT == null ? decimal.Zero : m.ALLOCATE_GRP_AMOUNT.Value);
                                                        newItems.Add(new ItemProperty(expensesGroupName, allocateAmount, true, string.Empty, false));

                                                        // ค่าใช้จ่าย
                                                        expensesGroupItem.Expenses.ForEach(expensesItem =>
                                                        {
                                                            string expensesName = string.Format("        {0}", expensesItem.EXPENSES_NAME);
                                                            var expensesExpr = expensesGroupExpr.Where(m => m.EXPENSES_ID.Equals(expensesItem.EXPENSES_ID)).ToList();
                                                            var isExpenses = expensesExpr.Any();

                                                            if (!isExpensesGroup)
                                                                newItems.Add(new ItemProperty(expensesName, null, false, string.Empty, false));
                                                            else
                                                            {
                                                                allocateAmount = decimal.Zero;
                                                                if (isExpenses)
                                                                    allocateAmount = expensesExpr.Sum(m => m.ALLOCATE_AMOUNT == null ? decimal.Zero : m.ALLOCATE_AMOUNT.Value);
                                                                newItems.Add(new ItemProperty(expensesName, allocateAmount, false, string.Empty, false));
                                                            }


                                                            // โครงการ
                                                            expensesItem.Projects.ForEach(projectItem =>
                                                            {
                                                                string projectName = string.Format("            {0}", projectItem.PROJECT_NAME);
                                                                if (!isExpenses)
                                                                    newItems.Add(new ItemProperty(projectName, null, false, string.Empty, false));
                                                                else
                                                                {
                                                                    var projectExpr = expensesExpr.Where(m => m.ALLOCATE_PROJECT_ID.Equals(projectItem.PROJECT_ID));
                                                                    allocateAmount = decimal.Zero;
                                                                    if (projectExpr.Any() && null != projectExpr.First().ALLOCATE_AMOUNT)
                                                                        allocateAmount = projectExpr.First().ALLOCATE_AMOUNT.Value;
                                                                    newItems.Add(new ItemProperty(projectName, allocateAmount, false, string.Empty, false));
                                                                }
                                                            });
                                                        });
                                                    });
                                                });
                                            });

                                            return newItems;
                                        })()
                                    }).ToList()
                            }).ToList()
                    }).ToList();


                // เขียนข้อมูลลงไฟล์ Excel
                string filename = string.Format("{0}_รายงานงบประมาณรายจ่ายประจำปีงบประมาณ.xls", usrAuthorizeProfile.EmpId);
                if (budgetType.Equals(2))
                    filename = string.Format("{0}_รายงานแผนรายรับรายจ่ายเงินนอกงบประมาณประจำปี.xls", usrAuthorizeProfile.EmpId);

                string template = string.Format("{0}/RptPlansIncomeOfYear.xlsx", appSettings.ReportTemplatePath);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(template)))
                {
                    ExportHelper export = new ExportHelper(xlsApp.Workbook.Worksheets[1]);

                    string reportTitleStr = "รายงานงบประมาณรายจ่ายประจำปีงบประมาณ พ.ศ. [var_fiscal_year]";
                    if (budgetType.Equals(2))
                        reportTitleStr = "รายงานแผนรายรับรายจ่ายเงินนอกงบประมาณประจำปี พ.ศ. [var_fiscal_year]";
                    export.GetCellByIndex(1, 1).Value = reportTitleStr.Replace("[var_fiscal_year]", (fiscalYear + 543).ToString());
                    string reportDateStr = export.GetCellByIndex(2, 1).Text;
                    export.GetCellByIndex(2, 1).Value = reportDateStr.Replace("[var_export_date]", DateTime.Now.ToString("dd/MM/yyy HH:mm:ss", AppUtils.ThaiCultureInfo));

                    Color grayLightColor = ColorTranslator.FromHtml(ExportUtils.CaptionHtmlColorCode);

                    int planColumnIndex = 2; // C ลบ 1 ออกแล้ว ใช้อ้างอิงตำแหน่งใน ExportUtils.ColumnsName
                    int planColumnMergeCount = 0,
                        produceColumnMergeCount = 0;
                    bool isFirst = true;
                    finalExpr.ForEach(planItem =>
                    {
                        int produceColumnIndex = planColumnIndex;
                        planItem.Produces.ForEach(produceItem =>
                        {
                            int activityColumnIndex = planColumnIndex;
                            produceItem.Activities.ForEach(activityItem =>
                            {
                                int rowIndex = 7;
                                string activityColumn = ExportUtils.ColumnsName[activityColumnIndex];
                                activityItem.Items.ForEach(item =>
                                {
                                    if (isFirst)
                                    {
                                        export.SetCellTextVal(string.Format("A{0}", rowIndex), item.ItemText, true);
                                        export.SelectedExcelRange.Style.Font.Bold = item.IsFontWeight;
                                        export.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        if (string.IsNullOrEmpty(item.HtmlColor))
                                            export.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(grayLightColor);
                                        else
                                            export.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(item.HtmlColor));
                                    }
                                    // รวมทุกรายการค่าใช้จ่าย ของกิจกรรม (แนวนอน)
                                    export.SetCellFormulaVal(string.Format("B{0}", rowIndex), string.Format("SUM(C{0}:Z{0})", rowIndex), true);
                                    export.SelectedExcelRange.Style.Font.Bold = item.IsFontWeight;
                                    if (!string.IsNullOrEmpty(item.HtmlColor))
                                    {
                                        export.SelectedExcelRange.Style.Font.Bold = item.IsFontWeight;
                                        export.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        export.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(item.HtmlColor));
                                    }

                                    export.SetCellCurrencyVal(string.Format("{0}{1}", activityColumn, rowIndex), item.ItemValue, true);
                                    export.SelectedExcelRange.Style.Font.Bold = item.IsFontWeight;
                                    if (!string.IsNullOrEmpty(item.HtmlColor))
                                    {
                                        export.SelectedExcelRange.Style.Font.Bold = item.IsFontWeight;
                                        export.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        export.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(item.HtmlColor));
                                    }
                                    rowIndex++;
                                });

                                // รวมทุกกิจกรรม ของรายการค่าใช้จ่าย (แนวตั้ง)
                                // รวมเฉพาะรายการที่เป็น งบรายจ่าย
                                export.SetCellCurrencyVal(string.Format("{0}6", activityColumn), activityItem.Items.Sum(e => e.IsBudgetType ? e.ItemValue : decimal.Zero), true);

                                // เขียนคอลัมล์กิจกรรม
                                export.SetCaption(string.Format("{0}5", activityColumn), activityItem.ACTIVITY_NAME);
                                activityColumnIndex++;
                                planColumnMergeCount++;
                                produceColumnMergeCount++;
                                isFirst = false;
                            });
                            // เขียนคอลัมล์ผลผลิต
                            export.SetCaption(string.Format("{0}4:{1}4", ExportUtils.ColumnsName[produceColumnIndex], ExportUtils.ColumnsName[produceColumnIndex + produceColumnMergeCount - 1]), produceItem.PRODUCE_NAME);
                            produceColumnIndex += produceColumnMergeCount;
                            produceColumnMergeCount = 0;
                        });
                        // เขียนคอลัมล์แผนงาน
                        export.SetCaption(string.Format("{0}3:{1}3", ExportUtils.ColumnsName[planColumnIndex], ExportUtils.ColumnsName[planColumnIndex + planColumnMergeCount - 1]), planItem.PLAN_NAME);
                        planColumnIndex += planColumnMergeCount;
                        planColumnMergeCount = 0;
                    });

                    res["filename"] = filename;
                    res["resultFilename"] = filename;
                    string filePath = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
                    xlsApp.SaveAs(new FileInfo(filePath));
                }
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class ItemProperty
        {
            public ItemProperty(string itemText, decimal? itemValue, bool isFontWeight, string htmlColor, bool isBudgetType)
            {
                ItemText = itemText;
                ItemValue = itemValue;
                IsFontWeight = isFontWeight;
                HtmlColor = htmlColor;
                IsBudgetType = isBudgetType;
            }

            public string ItemText { get; set; }

            public decimal? ItemValue { get; set; }

            /// <summary>
            /// ให้แสดงเป็นฟอร์นตัวหนาหรือไม่ เมื่อเขียนลง Xls
            /// </summary>
            public bool IsFontWeight { get; set; }

            /// <summary>
            /// สีสำหรับแสดงผลพื้นหลัง เมื่อนำไปเขียนลง Xls
            /// สำหรับทำ Highlight ส่วนหัว ส่วนกลุ่ม ...
            /// </summary>
            public string HtmlColor { get; set; }

            /// <summary>
            /// ItemText นี้เป็นชื่อของ งบรายจ่ายใช่หรือไม่
            /// ใช้ประกอบเงื่อนไขในการ Sum ภาพรวม
            /// </summary>
            public bool IsBudgetType { get; set; }
        }
    }
}