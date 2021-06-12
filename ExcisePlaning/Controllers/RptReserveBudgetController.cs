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
    /// รายงานการกันเงินงบประมาณ
    /// 
    /// Template: Report002_RptReserveBudget_Template.xlsx
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptReserveBudgetController : Controller
    {
        // GET: RptReserveBudget
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_REPORT_BUDGET_RESERVE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_REPORT_BUDGET_RESERVE;
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

        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? depId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int? budgetType, int? reserveType, string fromDateStr, string toDateStr, string reserveId, string returnType, int? pageIndex, int? pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var exprBudgetReserve = db.V_GET_SUMMARY_BUDGET_RESERVE_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1));

                if (null != planId)
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.EXPENSES_ID.Equals(expensesId));

                if (null != depId)
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.DEP_ID.Equals(depId));

                // ประเภทงบประมาณ(1 = เงินงบ, 2 = เงินนอกงบ)
                if (null != budgetType && !budgetType.Value.Equals(0))
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                // ประเภทการกันเงิน 1 = ผูกพัน, 2 = กันไว้เบิก
                if (null != reserveType && !reserveType.Value.Equals(0))
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.RESERVE_TYPE.Equals(reserveType));

                // ค้นหาตามช่วงวันที่กันเงิน
                var fromDate = AppUtils.TryValidUserDateStr(fromDateStr);
                var toDate = AppUtils.TryValidUserDateStr(toDateStr);
                if (fromDate != DateTime.MinValue && toDate != DateTime.MinValue)
                    exprBudgetReserve = exprBudgetReserve.Where(e => (e.RESERVE_DATE >= fromDate && e.RESERVE_DATE <= toDate));

                if (!string.IsNullOrEmpty(reserveId))
                    exprBudgetReserve = exprBudgetReserve.Where(e => e.RESERVE_ID.Equals(reserveId));

                // จัดรูปแบบข้อมูลเพื่อนำไปแสดงผล
                var finalExprBudgetExpenses = exprBudgetReserve.GroupBy(e => new
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
                    e.BUDGET_TYPE_ORDER_SEQ
                }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .AsEnumerable()
                .Select(e => new
                {
                    GroupBy = e.Key,
                    Rows = e.OrderBy(x => x.EXPENSES_GROUP_ORDER_SEQ)
                    .ThenBy(x => x.EXPENSES_ORDER_SEQ)
                    .ThenBy(x => x.PRODUCE_ID)
                    .ThenBy(x => x.DEP_ORDER_SEQ)
                    .ThenBy(x => x.CREATED_DATETIME).GroupBy(x => x.RESERVE_ID)
                    .Select(x => new
                    {
                        RESERVE_ID = x.Key,
                        x.First().REMARK_TEXT,
                        x.First().EXPENSES_GROUP_NAME,
                        x.First().EXPENSES_NAME,
                        x.First().PROJECT_NAME,
                        x.First().DEP_NAME,
                        x.First().RESERVE_TYPE,
                        x.First().BUDGET_TYPE,
                        BUDGET_AMOUNT = x.First().RESERVE_BUDGET_AMOUNT,
                        RESERVE_BUDGET_AMOUNT = x.First().BUDGET_TYPE.Equals(1) ? x.First().RESERVE_BUDGET_AMOUNT : decimal.Zero,
                        RESERVE_OFF_BUDGET_AMOUNT = x.First().BUDGET_TYPE.Equals(2) ? x.First().RESERVE_BUDGET_AMOUNT : decimal.Zero,
                        x.First().USE_AMOUNT,
                        x.First().REMAIN_AMOUNT
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
                    pagging.totalRecords = finalExprBudgetExpenses.Count();
                    pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                    int offset = pageIndex.Value * pageSize.Value - pageSize.Value;
                    pagging.rows = finalExprBudgetExpenses.Skip(offset).Take(pageSize.Value).ToList();

                    return Json(pagging, JsonRequestBehavior.DenyGet);
                }
                else
                {
                    // นำข้อมูลส่งออกไปยังโปรแกรม XLS
                    Dictionary<string, string> res = new Dictionary<string, string>(2) {
                        { "errorText", null },
                        { "filename", "" }
                    };

                    if (finalExprBudgetExpenses.Count() == 0)
                    {
                        res["errorText"] = "ไม่พบข้อมูล โปรดตรวจสอบเงื่อนไขการค้นหา";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    var appSettings = AppSettingProperty.ParseXml();
                    string templateFile = string.Format("{0}/Report002_RptReserveBudget_Template.xlsx", appSettings.ReportTemplatePath);
                    string filename = string.Format("{0}_รายงานกันเงินงบประมาณ_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks);
                    using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
                    {

                        // จัดกลุ่มข้อมูลตาม งบรายจ่าย เพื่อแยกออกเป็นแต่ละ Worksheet
                        var exprExport = finalExprBudgetExpenses.GroupBy(e => new { e.GroupBy.BUDGET_TYPE_ID, e.GroupBy.BUDGET_TYPE_NAME, e.GroupBy.BUDGET_TYPE_ORDER_SEQ })
                            .OrderBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                            .Select(e => new
                            {
                                SHEET_NAME = e.Key.BUDGET_TYPE_NAME,
                                Data = e.ToList()
                            }).ToList();

                        // นำออกไปยัง Excel โดยแยก Sheet ตามงบรายจ่าย
                        foreach (var budgetItem in exprExport)
                        {
                            var currWorkSheet = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", budgetItem.SHEET_NAME);
                            ExportUtils.CurrWorkSheet = currWorkSheet;

                            string reportTitle = ExportUtils.CurrWorkSheet.Cells["A1"].Text;
                            string yearStr = (fiscalYear + 543).ToString();
                            ExportUtils.CurrWorkSheet.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);

                            string exportDateText = ExportUtils.CurrWorkSheet.Cells["G2"].Text;
                            string exportDateVal = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                            ExportUtils.CurrWorkSheet.Cells["H2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);
                            ExportUtils.CurrWorkSheet.Row(3).Hidden = true;

                            // เขียนค่าลงไฟล์ Excel
                            int rowIndex = 4;
                            bool isOffBudget = budgetType.Value.Equals(2);
                            foreach (var expensesItem in budgetItem.Data)
                            {
                                // เขียนกลุ่มข้อมูล (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย)
                                ExportUtils.SetCellTextVal(string.Format("A{0}:J{0}", rowIndex), expensesItem.GroupBy.PLAN_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:J{0}", rowIndex + 1), expensesItem.GroupBy.PRODUCE_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:J{0}", rowIndex + 2), expensesItem.GroupBy.ACTIVITY_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:J{0}", rowIndex + 3), expensesItem.GroupBy.BUDGET_TYPE_NAME, false, "", true);

                                // เขียนหัวคอลัมล์
                                rowIndex += 4;
                                ExportUtils.SetCaption(string.Format("A{0}", rowIndex), "เลขที่ใบกัน");
                                ExportUtils.SetCaption(string.Format("B{0}", rowIndex), "หมวดรายจ่าย");
                                ExportUtils.SetCaption(string.Format("C{0}", rowIndex), "รายการค่าใช้จ่าย");
                                ExportUtils.SetCaption(string.Format("D{0}", rowIndex), "รายละเอียด");
                                ExportUtils.SetCaption(string.Format("E{0}:F{0}", rowIndex), "หน่วยงาน");
                                ExportUtils.SetCaption(string.Format("G{0}", rowIndex), "เงินงบประมาณ (บาท)");
                                ExportUtils.SetCaption(string.Format("H{0}", rowIndex), "เงินนอกงบประมาณ (บาท)");
                                ExportUtils.SetCaption(string.Format("I{0}", rowIndex), "เบิกจ่ายสะสม (บาท)");
                                ExportUtils.SetCaption(string.Format("J{0}", rowIndex), "คงเหลือ (บาท)");

                                // แสดงข้อมูลรายหน่วยงาน
                                rowIndex++;
                                foreach (var depItem in expensesItem.Rows)
                                {
                                    string expensesName = depItem.EXPENSES_NAME;
                                    if (!string.IsNullOrEmpty(depItem.PROJECT_NAME))
                                        expensesName = string.Format("{0} [{1}]", expensesName, depItem.PROJECT_NAME);

                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), depItem.RESERVE_ID, true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), depItem.EXPENSES_GROUP_NAME, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), expensesName, true);
                                    ExportUtils.SetCellTextVal(string.Format("D{0}", rowIndex), depItem.REMARK_TEXT, true);
                                    ExportUtils.SetCellTextVal(string.Format("E{0}:F{0}", rowIndex), depItem.DEP_NAME, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), depItem.RESERVE_BUDGET_AMOUNT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), depItem.RESERVE_OFF_BUDGET_AMOUNT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), depItem.USE_AMOUNT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), depItem.REMAIN_AMOUNT, true);

                                    rowIndex++;
                                }

                                // แสดงยอดรวมของกลุ่มข้อมูล
                                ExportUtils.SetCaption(string.Format("A{0}:F{0}", rowIndex), "รวมทั้งสิ้น");
                                ExportUtils.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                                ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), expensesItem.Rows.Sum(e => e.RESERVE_BUDGET_AMOUNT), true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), expensesItem.Rows.Sum(e => e.RESERVE_OFF_BUDGET_AMOUNT), true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), expensesItem.Rows.Sum(e => e.USE_AMOUNT), true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), expensesItem.Rows.Sum(e => e.REMAIN_AMOUNT), true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                currWorkSheet.Select(string.Format("A{0}:J{0}", rowIndex));
                                currWorkSheet.SelectedRange.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#FF0000"));
                                currWorkSheet.SelectedRange.Style.Font.Bold = true;

                                rowIndex += 2; // เว้นไปอีก 1 บรรทัดแล้วขึ้นกลุ่มใหม่
                            }

                            if (isOffBudget) // เงินนอกงบประมาณ ซ่อนคอลัมล์เงินงบ
                                currWorkSheet.Column(7).Hidden = true;
                            else // เงินงบประมาณ ซ่อนคอลัมล์เงินนอก
                                currWorkSheet.Column(8).Hidden = true;
                        }


                        // ซ่อน Worksheet ที่เป็น Template เอาไว้
                        xlsApp.Workbook.Worksheets["TEMPLATE"].Hidden = eWorkSheetHidden.VeryHidden;

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