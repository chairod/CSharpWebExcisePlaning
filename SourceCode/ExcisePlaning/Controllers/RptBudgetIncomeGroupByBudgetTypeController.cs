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
    /// รายงาน สรุปการรับเงินประจำงวดในแต่ละปีงบประมาณ โดยจัดกลุ่มข้อมูลตาม งบรายจ่าย
    /// 
    /// Template: Report004_BudgetIncomeGroupByBudgetType_Template.xls
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptBudgetIncomeGroupByBudgetTypeController : Controller
    {
        // GET: RptBudgetIncomeGroupByBudgetType
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_REPORT_BUDGET_INCOME_GROUP_BY_BUDGET_TYPE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_REPORT_BUDGET_INCOME_GROUP_BY_BUDGET_TYPE;
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

            ViewBag.DepAuthorize = userAuthorizeProfile.DepAuthorize;
            ViewBag.DefaultYear = AppUtils.GetCurrYear();
            ViewBag.DefaultPeriodMN = DateTime.Now.Month.ToString("00");

            // กรณีเป็นหน่วยงานกลาง ไม่ต้อง Default เขตพื้นที่ และ หน่วยงาน
            if (userAuthorizeProfile.DepAuthorize.Equals(1))
            {
                ViewBag.DefaultAreaId = "empty";
                ViewBag.DefaultDepartmentId = "empty";
            }
            // หน่วยงานภูมิภาคที่คุมหน่วยงานอื่นๆ ให้ Default เฉพาะเขตพื้นที่
            else if (userAuthorizeProfile.DepAuthorize.Equals(2) && userAuthorizeProfile.AssignDepartmentIds.Count > 0)
                ViewBag.DefaultDepartmentId = "empty";

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

        /// <summary>
        /// สรุปประวัติการรับเงินประจำงวด ตามงบรายจ่าย โดยแสดงเฉพาะเงินงบประมาณ
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="fromDateStr"></param>
        /// <param name="toDateStr"></param>
        /// <param name="periodYr"></param>
        /// <param name="periodMn"></param>
        /// <param name="referDocNo">เลขที่เอกสาร ในการรับเงินประจำงวด</param>
        /// <param name="returnType">RETRIEVE = แสดงข้อมูลบนหน้า Grid, EXPORT = ส่งออกไปยัง Excel</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, string fromDateStr, string toDateStr, int? periodYr, int? periodMn, string referDocNo, string returnType, int? pageIndex, int? pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprBudgetIncome = db.V_GET_SUMMARY_BUDGET_INCOME_GROUP_BY_BUDGET_TYPEs.Where(e => e.YR.Equals(fiscalYear));
                // เฉพาะเงินงบประมาณ
                exprBudgetIncome = exprBudgetIncome.Where(e => e.BUDGET_TYPE.Equals(1));

                if (null != planId)
                    exprBudgetIncome = exprBudgetIncome.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprBudgetIncome = exprBudgetIncome.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprBudgetIncome = exprBudgetIncome.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprBudgetIncome = exprBudgetIncome.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprBudgetIncome = exprBudgetIncome.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));

                // ค้นหาตามช่วงวันที่
                var fromDate = AppUtils.TryValidUserDateStr(fromDateStr);
                var toDate = AppUtils.TryValidUserDateStr(toDateStr);
                if (fromDate != DateTime.MinValue && toDate != DateTime.MinValue)
                    exprBudgetIncome = exprBudgetIncome.Where(e => (e.CREATED_DATE >= fromDate && e.CREATED_DATE <= toDate));

                // ตามงวด
                if (null != periodYr && null != periodMn)
                    exprBudgetIncome = exprBudgetIncome.Where(e => e.PERIOD_YR.Equals(periodYr) && e.PERIOD_MN.Equals(periodMn));
                if (!string.IsNullOrEmpty(referDocNo))
                    exprBudgetIncome = exprBudgetIncome.Where(e => e.REFER_DOC_NO.Equals(referDocNo));

                decimal toPercentVal = decimal.Parse("100.00");

                // จัดรูปแบบข้อมูลเพื่อนำไปแสดงผล
                var finalExprBudgetIncome = exprBudgetIncome.AsEnumerable().GroupBy(e => new { e.BUDGET_TYPE_ID, e.BUDGET_TYPE_ORDER_SEQ, e.BUDGET_TYPE_NAME, e.NET_BUDGET_AMOUNT })
                        .OrderBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                        .Select(e => new
                        {
                            e.Key.BUDGET_TYPE_ID,
                            e.Key.BUDGET_TYPE_NAME,
                            e.Key.NET_BUDGET_AMOUNT,
                            NET_BUDGET_INCOME_AMOUNT = e.Sum(x => x.RECEIVE_BUDGET_AMOUNT == null ? decimal.Zero : x.RECEIVE_BUDGET_AMOUNT.Value),
                            Rows = e.GroupBy(x => new { x.PERIOD_MN, x.PERIOD_YR })
                                    .OrderBy(x => x.Key.PERIOD_YR)
                                    .ThenBy(x => x.Key.PERIOD_MN)
                                    .Select(x => new
                                    {
                                        x.Key.PERIOD_MN,
                                        x.Key.PERIOD_YR,
                                        TOTAL_BUDGET_INCOME_AMOUNT = x.Sum(sub => sub.RECEIVE_BUDGET_AMOUNT == null ? decimal.Zero : sub.RECEIVE_BUDGET_AMOUNT.Value),
                                        CUMULATIVE_BUDGET_INCOME_AMOUNT = e.Where(sub => sub.INCOME_ID <= x.Last().INCOME_ID).Sum(sub => sub.RECEIVE_BUDGET_AMOUNT == null ? decimal.Zero : sub.RECEIVE_BUDGET_AMOUNT.Value)
                                    }).ToList()
                        });

                // รายการค่าใช้จ่ายที่หน่วยงานได้รับจัดสรร
                if ("RETRIEVE".Equals(returnType))
                {
                    PaggingResultMapper pagging = new PaggingResultMapper()
                    {
                        totalPages = 0,
                        totalRecords = 0,
                        rows = null
                    };
                    pagging.totalRecords = finalExprBudgetIncome.Count();
                    pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                    int offset = pageIndex.Value * pageSize.Value - pageSize.Value;
                    pagging.rows = finalExprBudgetIncome.Skip(offset).Take(pageSize.Value).ToList();

                    return Json(pagging, JsonRequestBehavior.DenyGet);
                }
                else
                {
                    // นำข้อมูลส่งออกไปยังโปรแกรม XLS
                    Dictionary<string, string> res = new Dictionary<string, string>(2) {
                        { "errorText", null },
                        { "filename", "" }
                    };

                    if (finalExprBudgetIncome.Count() == 0)
                    {
                        res["errorText"] = "ไม่พบข้อมูล โปรดตรวจสอบเงื่อนไขการค้นหา";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                    var appSettings = AppSettingProperty.ParseXml();
                    string templateFile = string.Format("{0}/Report004_BudgetIncomeGroupByBudgetType_Template.xlsx", appSettings.ReportTemplatePath);
                    string filename = string.Format("{0}_สรุปเงินประจำงวดแยกตามงบรายจ่าย_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks);
                    using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
                    {
                        var currWorksheet = xlsApp.Workbook.Worksheets[1];
                        ExportUtils.CurrWorkSheet = currWorksheet;

                        // สร้างคอลัมล์ งวดที่รับเงินประจำงวด
                        Dictionary<string, string> periodColName = new Dictionary<string, string>();
                        var exprPeriods = exprBudgetIncome.GroupBy(e => new { e.PERIOD_MN, e.PERIOD_YR })
                            .Select(e => e.Key)
                            .OrderBy(e => e.PERIOD_YR).ThenBy(e => e.PERIOD_MN).ToList();
                        int columnIndex = 3;
                        string colName = "";
                        foreach (var exprPeriod in exprPeriods)
                        {
                            colName = ExportUtils.ColumnsName[columnIndex++];
                            currWorksheet.Column(columnIndex).Width = 22.56;
                            ExportUtils.SetCaption(string.Format("{0}3", colName), string.Format("{0}/{1}", exprPeriod.PERIOD_MN, exprPeriod.PERIOD_YR + 543));
                            periodColName.Add(string.Format("{0}_{1}", exprPeriod.PERIOD_MN, exprPeriod.PERIOD_YR), colName);
                        }
                        // คอลัมล์ เงินประจำงวดสะสม 
                        colName = ExportUtils.ColumnsName[columnIndex++];
                        currWorksheet.Column(columnIndex).Width = 25.56;
                        periodColName.Add("totalIncomeColName", colName);
                        ExportUtils.SetCaption(string.Format("{0}3", colName), "เงินประจำงวดสะสม (บาท)");
                        // คอลัมล์ คงเหลือ
                        colName = ExportUtils.ColumnsName[columnIndex++];
                        currWorksheet.Column(columnIndex).Width = 23.67;
                        periodColName.Add("remainColName", colName);
                        ExportUtils.SetCaption(string.Format("{0}3", colName), "คงเหลือ (บาท)");

                        // หัวรายงาน
                        string reportTitle = currWorksheet.Cells["A1"].Text;
                        string yearStr = (fiscalYear + 543).ToString();
                        currWorksheet.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);
                        currWorksheet.Select(string.Format("A1:{0}1", colName));
                        currWorksheet.SelectedRange.Merge = true;
                        currWorksheet.SelectedRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        // วันที่นำออกข้อมูล
                        string exportDateText = string.Format("ข้อมูล ณ วันที่ {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                        currWorksheet.Select(string.Format("{0}2:{1}2", ExportUtils.ColumnsName[columnIndex - 2], colName));
                        currWorksheet.SelectedRange.Value = exportDateText;
                        currWorksheet.SelectedRange.Merge = true;
                        currWorksheet.SelectedRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                        // เขียนค่าลงไฟล์ Excel
                        int rowIndex = 4;
                        Color muteColor = ColorTranslator.FromHtml("#6c757d");
                        Color primaryColor = ColorTranslator.FromHtml("#007bff");
                        foreach (var expensesItem in finalExprBudgetIncome)
                        {
                            ExportUtils.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), expensesItem.BUDGET_TYPE_NAME, true, ExportUtils.CaptionHtmlColorCode, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("C{0}", rowIndex), expensesItem.NET_BUDGET_AMOUNT, true);
                            ExportUtils.SelectedExcelRange.Style.Font.Color.SetColor(muteColor);
                            ExportUtils.SelectedExcelRange.Style.Font.Bold = true;
                            //ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), decimal.Zero, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            ExportUtils.SetCellCurrencyVal(string.Format("{0}{1}", periodColName["totalIncomeColName"], rowIndex), expensesItem.NET_BUDGET_INCOME_AMOUNT, true);
                            ExportUtils.SelectedExcelRange.Style.Font.Color.SetColor(primaryColor);
                            ExportUtils.SelectedExcelRange.Style.Font.Bold = true;
                            ExportUtils.SetCellCurrencyVal(string.Format("{0}{1}", periodColName["remainColName"], rowIndex), expensesItem.NET_BUDGET_AMOUNT - expensesItem.NET_BUDGET_INCOME_AMOUNT, true);

                            // นำจำนวนเงิน ไปลงในแต่ละคอลัมล์ให้ถูกงวด
                            foreach (var item in expensesItem.Rows)
                            {
                                string keyName = string.Format("{0}_{1}", item.PERIOD_MN, item.PERIOD_YR);
                                ExportUtils.SetCellCurrencyVal(string.Format("{0}{1}", periodColName[keyName], rowIndex), item.TOTAL_BUDGET_INCOME_AMOUNT, true);
                            }

                            rowIndex++;
                        }

                        res["filename"] = filename;
                        string saveToFile = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
                        xlsApp.SaveAs(new FileInfo(saveToFile));
                    }
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
            }
        }

    }
}