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
    /// R002-รายงานคำของบประมาณรายจ่ายประจำปี
    /// สรุปรายการคำขอเงินงบประมาณในแต่ละปี
    /// 
    /// Template: RptRequestBudgetOfYear.xlsx
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptRequestBudgetOfYearController : Controller
    {
        // GET: RptRequestBudgetOfYearForm
        public ActionResult RptRequestBudgetOfYearForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_RptRequestBudgetOfYear);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_RptRequestBudgetOfYear;
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


            ViewBag.DefaultFiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            ViewBag.DefaultDepId = userAuthorizeProfile.DepId;
            ViewBag.CanSelectDepartment = userAuthorizeProfile.CanSelectDepartment;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID)
                    .Select(e => new AreaShortFieldProperty()
                    {
                        AREA_ID = e.AREA_ID,
                        AREA_NAME = e.AREA_NAME
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
        /// 
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="depId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="requestType">1 = คำขอต้นปี, 2 = คำขอเพิ่มเติม</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? areaId, int? depId, int? budgetTypeId, int? budgetType, int? requestType)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(3) {
                { "filename", null },
                { "resultFilename", null },
                { "errorText", null }
            };

            var usrAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            var depFilterAuthorize = DepartmentAuthorizeFilterProperty.Verfity(usrAuthorizeProfile, depId);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_SUMMARY_BUDGET_REQUESTs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(fiscalYear));
                if (depFilterAuthorize.Authorize.Equals(2))
                    expr = expr.Where(e => (e.DEP_ID != null && depFilterAuthorize.AssignDepartmentIds.Contains(e.DEP_ID.Value)) || e.DEP_ID == null);

                if (null != areaId && depId == null)
                    expr = expr.Where(e => e.AREA_ID.Equals(areaId));
                if (null != budgetType)
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                if (null != requestType)
                    expr = expr.Where(e => e.REQ_TYPE.Equals(requestType));
                if (null != budgetTypeId)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));

                if (!expr.Any())
                {
                    res["errorText"] = "ไม่พบข้อมูล";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var primaryExpr = expr.Select(e => new
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
                    e.EXPENSES_MASTER_ID,
                    e.EXPENSES_MASTER_NAME,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.TOTAL_REQUEST_BUDGET
                }).AsEnumerable();

                // จัดกลุ่ม งบรายจ่าย กลุ่มหมวดแผนงาน หมวดแผนงาน ค่าใช้จ่าย
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
                                        m.Key.EXPENSES_NAME
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
                                                            && m.BUDGET_TYPE_ID.Equals(budgetTypeItem.BUDGET_TYPE_ID)).ToList();
                                                bool isBudgetType = budgetTypeItemExpr.Any();
                                                newItems.Add(new ItemProperty(budgetTypeItem.BUDGET_TYPE_NAME, isBudgetType ? budgetTypeItemExpr.Sum(m => m.TOTAL_REQUEST_BUDGET == null ? decimal.Zero : m.TOTAL_REQUEST_BUDGET.Value) : decimal.Zero, true, "#DAEEF3", true));

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
                                                        string expensesGroupName = string.Format("    {0}", expensesGroupItem.EXPENSES_GROUP_NAME);
                                                        if (!isBudgetType)
                                                            newItems.Add(new ItemProperty(expensesGroupName, null, true, string.Empty, false));
                                                        else
                                                            newItems.Add(new ItemProperty(expensesGroupName, isExpensesGroup ? expensesGroupExpr.Sum(m => null == m.TOTAL_REQUEST_BUDGET ? decimal.Zero : m.TOTAL_REQUEST_BUDGET.Value) : decimal.Zero, true, string.Empty, false));

                                                        // ค่าใช้จ่าย
                                                        expensesGroupItem.Expenses.ForEach(expensesItem =>
                                                        {
                                                            string expensesName = string.Format("        {0}", expensesItem.EXPENSES_NAME);
                                                            if (!isExpensesGroup)
                                                                newItems.Add(new ItemProperty(expensesName, null, false, string.Empty, false));
                                                            else
                                                            {
                                                                var expensesExpr = expensesGroupExpr.Where(m => m.EXPENSES_ID.Equals(expensesItem.EXPENSES_ID)).ToList();
                                                                newItems.Add(new ItemProperty(expensesName, expensesExpr.Any() ? expensesExpr.Sum(m => null == m.TOTAL_REQUEST_BUDGET ? decimal.Zero : m.TOTAL_REQUEST_BUDGET.Value) : decimal.Zero, false, string.Empty, false));
                                                            }
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
                var appSettings = AppSettingProperty.ParseXml();
                string filename = string.Format("{0}_รายงานคำของบประมาณรายจ่ายประจำปี.xls", usrAuthorizeProfile.EmpId);
                string template = string.Format("{0}/RptRequestBudgetOfYear.xlsx", appSettings.ReportTemplatePath);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(template)))
                {
                    ExportHelper export = new ExportHelper(xlsApp.Workbook.Worksheets[1]);

                    string reportTitleStr = export.GetCellByIndex(1, 1).Text;
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