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
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// สรุปผลการจัดการเงินงบประมาณ และ เงินนอกงบประมาณ
    /// 
    /// R014-รายงานจัดการเงินงบประมาณ
    /// R015-รายงานจัดการเงินนอกงบประมาณ
    /// 
    /// Template:  RptBudgetReceive.xlsx
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptBudgetReceiveController : Controller
    {
        // GET: RptBudgetReceive
        public ActionResult GetForm(string pageType)
        {
            string currentMenuConst = AppConfigConst.MENU_CONST_REPORT_SUMMARY_RECEIVE_BUDGET_MENU;
            if ("off_budget".Equals(pageType))
                currentMenuConst = AppConfigConst.MENU_CONST_REPORT_SUMMARY_RECEIVE_OFF_BUDGET_MENU;

            UserAuthorizeProperty usrProps = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = usrProps.FindUserMenu(currentMenuConst);
            UserAuthorizeMenuProperty menuIndexItem = usrProps.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = usrProps.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กรณีไม่ผ่านค่า type เข้าไปให้เด้งกลับไปหน้า Dashboard/หน้าแรก
            List<string> acceptPageTypes = new List<string>() { "budget", "off_budget" };
            if (string.IsNullOrEmpty(pageType) || acceptPageTypes.IndexOf(pageType) == -1)
                return RedirectToAction(menuIndexItem.ActionName, menuIndexItem.RouteName);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = currentMenuConst;
            ViewBag.Title = menuItem.MenuName;
            ViewBag.MenuGroups = usrProps.MenuGroups;
            ViewBag.PageName = menuItem.MenuName;
            ViewBag.PageDescription = menuItem.MenuDescription;
            ViewBag.LoginName = usrProps.EmpFullname;


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

            ViewBag.PageType = pageType;
            ViewBag.DefaultFiscalYear = usrProps.DefaultFiscalYear;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
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

        public ActionResult Retrieve(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int budgetType)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(3) {
                { "filename", null },
                { "resultFilename", null },
                { "errorText", null }
            };

            var usrAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = from exp_budget in db.V_GET_BUDGET_EXPENSES_INFORMATIONs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(fiscalYear) && (e.PROJECT_FOR_TYPE == null || (e.PROJECT_FOR_TYPE != null && e.PROJECT_FOR_TYPE.Equals(budgetType))))
                           join exp in db.V_GET_EXPENSES_INFORMATIONs on exp_budget.EXPENSES_ID equals exp.EXPENSES_ID
                           join plan in db.T_PLAN_CONFIGUREs on exp_budget.PLAN_ID equals plan.PLAN_ID
                           join produce in db.T_PRODUCE_CONFIGUREs on exp_budget.PRODUCE_ID equals produce.PRODUCE_ID
                           join activity in db.T_ACTIVITY_CONFIGUREs on exp_budget.ACTIVITY_ID equals activity.ACTIVITY_ID
                           select new
                           {
                               exp_budget.YR,
                               exp_budget.PLAN_ID,
                               exp_budget.PRODUCE_ID,
                               exp_budget.ACTIVITY_ID,
                               exp_budget.BUDGET_TYPE_ID,
                               exp_budget.EXPENSES_GROUP_ID,
                               exp_budget.EXPENSES_ID,
                               exp_budget.CAN_ADD_PROJECT, // คชจ. นั้นมีโครงการภายใต้อีกหรือไม่
                               exp_budget.PROJECT_ID,
                               exp_budget.PROJECT_NAME,
                               exp_budget.PROJECT_FOR_TYPE,

                               plan.PLAN_NAME,
                               PLAN_ORDER_SEQ = plan.ORDER_SEQ,
                               produce.PRODUCE_NAME,
                               PRODUCE_ORDER_SEQ = produce.ORDER_SEQ,
                               activity.ACTIVITY_NAME,
                               ACTIVITY_ORDER_SEQ = activity.ORDER_SEQ,

                               exp.BUDGET_TYPE_NAME,
                               exp.BUDGET_TYPE_ORDER_SEQ,
                               exp.EXPENSES_MASTER_ID,
                               exp.EXPENSES_MASTER_NAME,
                               exp.EXPENSES_GROUP_NAME,
                               exp.EXPENSES_GROUP_ORDER_SEQ,
                               exp.EXPENSES_NAME,
                               EXPENSES_ORDER_SEQ = exp.ORDER_SEQ,

                               // ส่วนของรายการค่าใช้จ่าย
                               BUDGET_AMOUNT = budgetType.Equals(1) ? exp_budget.BUDGET_AMOUNT : exp_budget.OFF_BUDGET_AMOUNT,
                               ACTUAL_BUDGET_AMOUNT = budgetType.Equals(1) ? exp_budget.ACTUAL_BUDGET_AMOUNT : exp_budget.ACTUAL_OFF_BUDGET_AMOUNT,
                               REMAIN_BUDGET_AMOUNT = budgetType.Equals(1) ? exp_budget.REMAIN_BUDGET_AMOUNT : exp_budget.REMAIN_OFF_BUDGET_AMOUNT,

                               // ส่วนของโครงการ
                               PRO_BUDGET_AMOUNT = budgetType.Equals(1) ? exp_budget.PRO_BUDGET_AMOUNT : exp_budget.PRO_OFF_BUDGET_AMOUNT,
                               PRO_ACTUAL_BUDGET_AMOUNT = budgetType.Equals(1) ? exp_budget.PRO_ACTUAL_BUDGET_AMOUNT : exp_budget.PRO_ACTUAL_OFF_BUDGET_AMOUNT,
                               PRO_REMAIN_BUDGET_AMOUNT = budgetType.Equals(1) ? exp_budget.PRO_REMAIN_BUDGET_AMOUNT : exp_budget.PRO_REMAIN_OFF_BUDGET_AMOUNT,
                           };

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


                // ค้นหาการจัดเก็บรายได้ภาษีของเงินนอกงบประมาณ
                // ซึ่งเป็นภาพรวมในแต่ละปีงบประมาณ
                decimal netOffBudgetAmounts = decimal.Zero;
                if (budgetType.Equals(2))
                {
                    var exprMaster = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear));
                    netOffBudgetAmounts = exprMaster.Any() ? exprMaster.Sum(e => e.ACTUAL_OFF_BUDGET_AMOUNT) : decimal.Zero;
                }

                var primaryExpr = expr.AsEnumerable();
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
                                    g.EXPENSES_GROUP_ORDER_SEQ
                                }).OrderBy(g => g.Key.EXPENSES_GROUP_ORDER_SEQ)
                                .Select(g => new
                                {
                                    g.Key.EXPENSES_GROUP_ID,
                                    g.Key.EXPENSES_GROUP_NAME,
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
                                                        m.PLAN_ID,
                                                        m.PRODUCE_ID,
                                                        m.ACTIVITY_ID,
                                                        m.BUDGET_TYPE_ID,
                                                        m.EXPENSES_MASTER_ID,
                                                        m.EXPENSES_GROUP_ID,
                                                        m.EXPENSES_ID,
                                                        m.PROJECT_ID,
                                                        m.BUDGET_AMOUNT,
                                                        m.ACTUAL_BUDGET_AMOUNT,
                                                        m.REMAIN_BUDGET_AMOUNT,
                                                        m.PRO_BUDGET_AMOUNT,
                                                        m.PRO_ACTUAL_BUDGET_AMOUNT,
                                                        m.PRO_REMAIN_BUDGET_AMOUNT
                                                    }).Select(m => m.Key).ToList();
                                                bool isBudgetType = budgetTypeItemExpr.Any();
                                                decimal allocateAmount = decimal.Zero, actualAmount = decimal.Zero, remainAmount = decimal.Zero;
                                                if (isBudgetType)
                                                {
                                                    allocateAmount = budgetTypeItemExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.BUDGET_AMOUNT);
                                                    actualAmount = budgetTypeItemExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.ACTUAL_BUDGET_AMOUNT);
                                                    remainAmount = allocateAmount - actualAmount;
                                                }
                                                newItems.Add(new ItemProperty(budgetTypeItem.BUDGET_TYPE_NAME, allocateAmount, actualAmount, remainAmount, true, "#DAEEF3", true));

                                                // กลุ่มหมวดค่าใช้จ่าย
                                                budgetTypeItem.GroupMasters.ForEach(groupMasterItem =>
                                                {
                                                    if (null != groupMasterItem.EXPENSES_MASTER_ID)
                                                    {
                                                        var expensesGroupMasExpr = budgetTypeItemExpr.Where(m => m.EXPENSES_MASTER_ID.Equals(groupMasterItem.EXPENSES_MASTER_ID)).ToList();
                                                        allocateAmount = decimal.Zero;
                                                        actualAmount = decimal.Zero;
                                                        remainAmount = decimal.Zero;
                                                        if (expensesGroupMasExpr.Any())
                                                        {
                                                            allocateAmount = expensesGroupMasExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.BUDGET_AMOUNT);
                                                            actualAmount = expensesGroupMasExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.ACTUAL_BUDGET_AMOUNT);
                                                        }
                                                        remainAmount = allocateAmount - actualAmount;
                                                        newItems.Add(new ItemProperty(groupMasterItem.EXPENSES_MASTER_NAME, allocateAmount, actualAmount, remainAmount, true, string.Empty, false));
                                                    }

                                                    // หมวดค่าใช้จ่าย
                                                    groupMasterItem.ExpensesGroups.ForEach(expensesGroupItem =>
                                                    {
                                                        var expensesGroupExpr = budgetTypeItemExpr.Where(m => m.EXPENSES_GROUP_ID.Equals(expensesGroupItem.EXPENSES_GROUP_ID)).ToList();
                                                        bool isExpensesGroup = expensesGroupExpr.Any();
                                                        string expensesGroupName = string.Format("    {0}", expensesGroupItem.EXPENSES_GROUP_NAME);
                                                        allocateAmount = decimal.Zero;
                                                        actualAmount = decimal.Zero;
                                                        remainAmount = decimal.Zero;
                                                        if (isExpensesGroup)
                                                        {
                                                            allocateAmount = expensesGroupExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.BUDGET_AMOUNT);
                                                            actualAmount = expensesGroupExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.ACTUAL_BUDGET_AMOUNT);
                                                            remainAmount = allocateAmount - actualAmount;
                                                        }
                                                        newItems.Add(new ItemProperty(expensesGroupName, allocateAmount, actualAmount, remainAmount, true, string.Empty, false));

                                                        // ค่าใช้จ่าย
                                                        expensesGroupItem.Expenses.ForEach(expensesItem =>
                                                        {
                                                            string expensesName = string.Format("        {0}", expensesItem.EXPENSES_NAME);
                                                            var expensesExpr = expensesGroupExpr.Where(m => m.EXPENSES_ID.Equals(expensesItem.EXPENSES_ID)).ToList();
                                                            var isExpenses = expensesExpr.Any();

                                                            if (!isExpensesGroup)
                                                                newItems.Add(new ItemProperty(expensesName, null, null, null, false, string.Empty, false));
                                                            else
                                                            {
                                                                allocateAmount = decimal.Zero;
                                                                actualAmount = decimal.Zero;
                                                                remainAmount = decimal.Zero;
                                                                if (isExpenses)
                                                                {
                                                                    allocateAmount = expensesExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.BUDGET_AMOUNT);
                                                                    actualAmount = expensesExpr.Sum(m => m.PROJECT_ID != null ? decimal.Zero : m.ACTUAL_BUDGET_AMOUNT);
                                                                    remainAmount = allocateAmount - actualAmount;
                                                                }
                                                                newItems.Add(new ItemProperty(expensesName, allocateAmount, actualAmount, remainAmount, false, string.Empty, false));
                                                            }


                                                            // โครงการ
                                                            expensesItem.Projects.ForEach(projectItem =>
                                                            {
                                                                string projectName = string.Format("            {0}", projectItem.PROJECT_NAME);
                                                                if (!isExpenses)
                                                                    newItems.Add(new ItemProperty(projectName, null, null, null, false, string.Empty, false));
                                                                else
                                                                {
                                                                    var projectExpr = expensesExpr.Where(m => m.PROJECT_ID.Equals(projectItem.PROJECT_ID)).FirstOrDefault();
                                                                    allocateAmount = decimal.Zero;
                                                                    actualAmount = decimal.Zero;
                                                                    remainAmount = decimal.Zero;
                                                                    if (projectExpr != null && projectExpr.PRO_BUDGET_AMOUNT != null)
                                                                    {
                                                                        allocateAmount = projectExpr.PRO_BUDGET_AMOUNT.Value;
                                                                        actualAmount = projectExpr.PRO_ACTUAL_BUDGET_AMOUNT.Value;
                                                                        remainAmount = allocateAmount - actualAmount;
                                                                    }
                                                                    newItems.Add(new ItemProperty(projectName, allocateAmount, actualAmount, remainAmount, false, string.Empty, false));
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
                string filename = string.Format("{0}_รายงานจัดการเงินงบประมาณ.xls", usrAuthorizeProfile.EmpId);
                if (budgetType.Equals(2))
                    filename = string.Format("{0}_รายงานจัดการเงินนอกงบประมาณ.xls", usrAuthorizeProfile.EmpId);

                var appSettings = AppSettingProperty.ParseXml();
                string template = string.Format("{0}/RptBudgetReceive.xlsx", appSettings.ReportTemplatePath);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(template)))
                {
                    ExportHelper export = new ExportHelper(xlsApp.Workbook.Worksheets[1]);

                    string reportTitleStr = "รายงานจัดการเงินงบประมาณประจำปี พ.ศ. [var_fiscal_year]";
                    if (budgetType.Equals(2))
                        reportTitleStr = "รายงานจัดการเงินนอกงบประมาณประจำปี พ.ศ. [var_fiscal_year]";
                    export.GetCellByIndex(1, 1).Value = reportTitleStr.Replace("[var_fiscal_year]", (fiscalYear + 543).ToString());
                    string reportDateStr = export.GetCellByIndex(2, 1).Text;
                    export.GetCellByIndex(2, 1).Value = reportDateStr.Replace("[var_export_date]", DateTime.Now.ToString("dd/MM/yyy HH:mm:ss", AppUtils.ThaiCultureInfo));

                    // กรณีเงินนอกงบประมาณ ให้รวมเงินรายได้ที่จัดเก็บภาษี เข้าไปในรวมทั้งสิ้น
                    if (budgetType.Equals(2))
                        export.SetCellFormulaVal("C7", string.Format("=SUM(E7+G7+I7+K7) + {0}", netOffBudgetAmounts), true);

                    Color grayLightColor = ColorTranslator.FromHtml(ExportUtils.CaptionHtmlColorCode);

                    int planColumnIndex = 3; // D ลบ 1 ออกแล้ว ใช้อ้างอิงตำแหน่งใน ExportUtils.ColumnsName
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
                                int rowIndex = 8;
                                string activityAllocatedColumn = ExportUtils.ColumnsName[activityColumnIndex];
                                string activityReportedColumn = ExportUtils.ColumnsName[activityColumnIndex + 1];
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

                                    ExcelRange range1, range2, range3, range4;

                                    // รวมทุกรายการค่าใช้จ่าย ของกิจกรรม (แนวนอน)
                                    export.SetCellFormulaVal(string.Format("B{0}", rowIndex), string.Format("SUM(D{0}+F{0}+H{0}+J{0})", rowIndex), true);
                                    range1 = export.SelectedExcelRange;
                                    export.SetCellFormulaVal(string.Format("C{0}", rowIndex), string.Format("SUM(E{0}+G{0}+I{0}+K{0})", rowIndex), true);
                                    range2 = export.SelectedExcelRange;

                                    // ได้รับจัดสรร
                                    export.SetCellCurrencyVal(string.Format("{0}{1}", activityAllocatedColumn, rowIndex), item.BudgetAmount, true);
                                    range3 = export.SelectedExcelRange;
                                    // ผลการใช้จ่าย
                                    export.SetCellCurrencyVal(string.Format("{0}{1}", activityReportedColumn, rowIndex), item.ReportedAmount, true);
                                    range4 = export.SelectedExcelRange;

                                    range1.Style.Font.Bold = item.IsFontWeight;
                                    range2.Style.Font.Bold = item.IsFontWeight;
                                    range3.Style.Font.Bold = item.IsFontWeight;
                                    range4.Style.Font.Bold = item.IsFontWeight;
                                    if (!string.IsNullOrEmpty(item.HtmlColor))
                                    {
                                        range1.Style.Font.Bold = item.IsFontWeight;
                                        range1.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        range1.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(item.HtmlColor));

                                        range2.Style.Font.Bold = item.IsFontWeight;
                                        range2.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        range2.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(item.HtmlColor));

                                        range3.Style.Font.Bold = item.IsFontWeight;
                                        range3.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        range3.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(item.HtmlColor));

                                        range4.Style.Font.Bold = item.IsFontWeight;
                                        range4.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        range4.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(item.HtmlColor));
                                    }

                                    rowIndex++;
                                });

                                // เขียนคอลัมล์กิจกรรม
                                export.SetCaption(string.Format("{0}5:{1}5", activityAllocatedColumn, activityReportedColumn), activityItem.ACTIVITY_NAME);
                                export.SetCaption(string.Format("{0}6", activityAllocatedColumn), "งบประมาณ");
                                export.SetCaption(string.Format("{0}6", activityReportedColumn), "เงินประจำงวด");

                                // รวมทุกกิจกรรม ของรายการค่าใช้จ่าย (แนวตั้ง)
                                // รวมเฉพาะรายการที่เป็น งบรายจ่าย
                                export.SetCellCurrencyVal(string.Format("{0}7", activityAllocatedColumn), activityItem.Items.Sum(e => e.IsBudgetType ? e.BudgetAmount : decimal.Zero), true);
                                export.SetCellCurrencyVal(string.Format("{0}7", activityReportedColumn), activityItem.Items.Sum(e => e.IsBudgetType ? e.ReportedAmount : decimal.Zero), true);

                                activityColumnIndex += 2;
                                planColumnMergeCount += 2;
                                produceColumnMergeCount += 2;
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
            public ItemProperty(string itemText, decimal? budgetAmount, decimal? reportedAmount, decimal? remainAmount, bool isFontWeight, string htmlColor, bool isBudgetType)
            {
                ItemText = itemText;
                BudgetAmount = budgetAmount;
                ReportedAmount = reportedAmount;
                RemainAmount = remainAmount;
                IsFontWeight = isFontWeight;
                HtmlColor = htmlColor;
                IsBudgetType = isBudgetType;
            }

            public string ItemText { get; set; }

            /// <summary>
            /// งบประมาณ ที่ได้รับจัดสรร
            /// </summary>
            public decimal? BudgetAmount { get; set; }

            /// <summary>
            /// ยอดสะสมที่รายงานผลการใช้จ่าย
            /// </summary>
            public decimal? ReportedAmount { get; set; }

            /// <summary>
            /// ยอดคงเหลือฆ
            /// </summary>
            public decimal? RemainAmount { get; set; }

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