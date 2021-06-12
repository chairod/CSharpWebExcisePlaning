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
    /// หน่วยงานภูมิภาคบันทึกผลการใช้จ่ายเงินงบประมาณ
    /// ที่ได้รับจัดสรรจากส่วนกลาง (กรมสรรพสามิต)
    /// ซึ่งเป็นการนำออกข้อมูลจากระบบ แล้วคีย์ลงไฟล์
    /// และนำมา Import ให้ระบบอ่านข้อมูลลงไปยัง Db
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReportImportController : Controller
    {
        // GET: BudgetReportImport
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            // ตรวจสอบการเข้าทำงานของจอ
            var fiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            var verifyBudget = BudgetUtils.VerifyBudget(fiscalYear, null);
            if (!verifyBudget.IsComplete)
                return RedirectToAction("GetPageWarning", "BudgetAllocateDepartmentGroup");

            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_REPORT_IMPORT_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_REPORT_IMPORT_MENU;
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
                ActionName = menuItem.ActionName,
                QueryString = menuItem.QueryString
            });
            ViewBag.Breadcrumps = breadcrumps;

            ViewBag.FiscalYear = AppUtils.GetCurrYear();
            ViewBag.DefaultDepId = userAuthorizeProfile.DepId;
            ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            ViewBag.DepAuhtorize = userAuthorizeProfile.DepAuthorize;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).ToList();
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
        /// นำข้อมูลรายการ คชจ. ของหน่วยงานภูมิภาคที่ได้รับจัดสรรจากกรมสรรพสามิต
        /// ส่งออกไปยัง Excel Template เพื่อให้ใส่ข้อมูลการรายงานผลลงในไฟล์ Xls และรายงานผลในไฟล์
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="areaId"></param>
        /// <param name="depId"></param>
        /// <param name="budgetType">1 = เงินงบ, 2 = เงินนอกงบ</param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitExport(int fiscalYear, int? areaId, int? depId, short budgetType, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>() {
                { "errorText", null },
                { "filename", null },
                { "downloadFilename", null }
            };
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ตรวจสอบสิทธิ์การเข้าถึงข้อมูล
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var depAuthorizeFilter = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, userAuthorizeProfile.DepId);
                if (depAuthorizeFilter.Authorize.Equals(2) && depId != null && depAuthorizeFilter.AssignDepartmentIds.IndexOf(depId.Value) == -1)
                {
                    res["errorText"] = "ท่านไม่ได้รับอนุญาตให้เข้าถึงข้อมูลของหน่วยงานนี้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var exprDepBudget = db.V_GET_DEPARTMENT_EXPENSES_BUDGET_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear));
                if (null != areaId)
                    exprDepBudget = exprDepBudget.Where(e => e.AREA_ID.Equals(areaId));

                if (depAuthorizeFilter.Authorize.Equals(2))
                    exprDepBudget = exprDepBudget.Where(e => depAuthorizeFilter.AssignDepartmentIds.Contains(e.DEP_ID));
                if (null != depId)
                    exprDepBudget = exprDepBudget.Where(e => e.DEP_ID.Equals(depId));

                if (null != planId)
                    exprDepBudget = exprDepBudget.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprDepBudget = exprDepBudget.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprDepBudget = exprDepBudget.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprDepBudget = exprDepBudget.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprDepBudget = exprDepBudget.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    exprDepBudget = exprDepBudget.Where(e => e.EXPENSES_ID.Equals(expensesId));


                var finalExprDepBudget = exprDepBudget.AsEnumerable().GroupBy(e => new { e.DEP_ID, e.DEP_CODE, e.DEP_NAME, e.DEP_SORT_INDEX })
                    .OrderBy(e => e.Key.DEP_SORT_INDEX)
                    .Select(e => new
                    {
                        e.Key.DEP_ID,
                        e.Key.DEP_CODE,
                        e.Key.DEP_NAME,
                        GroupBy = e.GroupBy(x => new
                        {
                            x.PLAN_ID,
                            x.PLAN_NAME,
                            x.PLAN_ORDER_SEQ,
                            x.PRODUCE_ID,
                            x.PRODUCE_NAME,
                            x.PRODUCE_ORDER_SEQ,
                            x.ACTIVITY_ID,
                            x.ACTIVITY_NAME,
                            x.ACTIVITY_ORDER_SEQ,
                            x.BUDGET_TYPE_ID,
                            x.BUDGET_TYPE_NAME,
                            x.BUDGET_TYPE_ORDER_SEQ,
                            x.EXPENSES_GROUP_ID,
                            x.EXPENSES_GROUP_NAME,
                            x.EXPENSES_GROUP_ORDER_SEQ,
                            x.ALLOCATE_EXPENSES_GROUP_ID,
                            x.EX_GRP_ALLOCATE_BUDGET_AMOUNT,
                            x.EX_GRP_USE_BUDGET_AMOUNT,
                            x.EX_GRP_REMAIN_BUDGET_AMOUNT,

                            x.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT,
                            x.EX_GRP_USE_OFF_BUDGET_AMOUNT,
                            x.EX_GRP_REMAIN_OFF_BUDGET_AMOUNT
                        }).OrderBy(x => x.Key.PLAN_ORDER_SEQ)
                        .ThenBy(x => x.Key.PRODUCE_ORDER_SEQ)
                        .ThenBy(x => x.Key.ACTIVITY_ORDER_SEQ)
                        .ThenBy(x => x.Key.BUDGET_TYPE_ORDER_SEQ)
                        .ThenBy(x => x.Key.EXPENSES_GROUP_ORDER_SEQ)
                        .Select(x => new
                        {
                            GroupBy = x.Key,
                            Expenses = x.OrderBy(m => m.EXPENSES_ORDER_SEQ).Select(m => new
                            {
                                m.SEQ_ID,
                                m.YR,
                                m.DEP_ID,

                                m.PLAN_ID,
                                m.PRODUCE_ID,
                                m.ACTIVITY_ID,
                                m.BUDGET_TYPE_ID,
                                m.EXPENSES_GROUP_ID,
                                m.EXPENSES_ID,
                                m.EXPENSES_NAME,
                                m.PROJECT_ID,
                                m.PROJECT_NAME,
                                BUDGET_TYPE = budgetType,
                                // เงินงบประมาณ
                                m.ALLOCATE_BUDGET_AMOUNT,
                                m.USE_BUDGET_AMOUNT,
                                m.REMAIN_BUDGET_AMOUNT,
                                // เงินนอกงบประมาณ
                                m.ALLOCATE_OFF_BUDGET_AMOUNT,
                                m.USE_OFF_BUDGET_AMOUNT,
                                m.REMAIN_OFF_BUDGET_AMOUNT
                            }).ToList()
                        }).ToList()
                    }).ToList();
                if (!finalExprDepBudget.Any())
                {
                    res["errorText"] = "ไม่พบข้อมูล โปรดตรวจสอบเงื่อนไขที่เลือก";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                string filename = string.Format("{0}_รายงานผลการใช้จ่ายภูมิภาค_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks.ToString());
                var appSettings = AppSettingProperty.ParseXml();
                string filePath = string.Format("{0}/Report007_Department_Report_Payment_Template.xlsx", appSettings.ReportTemplatePath);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(filePath)))
                {
                    string grayWhiteHtmlColor = "#F2F2F2";
                    string fiscalYearBuddhist = (fiscalYear + 543).ToString();
                    string exportDateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                    foreach (var exprDep in finalExprDepBudget)
                    {
                        var exportor = new ExportHelper(xlsApp.Workbook.Worksheets.Copy("TEMPLATE", exprDep.DEP_CODE));
                        string titleText = exportor.GetCellByIndex(1, 1).Text;
                        titleText = titleText.Replace("[var_fiscal_year]", fiscalYearBuddhist);
                        titleText = titleText.Replace("[var_department_name]", exprDep.DEP_NAME);
                        exportor.GetCellByIndex(1, 1).Value = titleText;

                        string exportDateText = exportor.GetCellByIndex(2, 4).Text;
                        exportDateText = exportDateText.Replace("[var_export_date]", exportDateStr);
                        exportor.GetCellByIndex(2, 4).Value = exportDateText;

                        int rowIndex = 6;
                        exprDep.GroupBy.ForEach(itemGroup =>
                        {
                            exportor.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), itemGroup.GroupBy.PLAN_NAME, false, "", true);
                            exportor.SetCellTextVal(string.Format("G{0}:AD{0}", rowIndex), "", false, grayWhiteHtmlColor, true);
                            rowIndex++;

                            exportor.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), itemGroup.GroupBy.PRODUCE_NAME, false, "", true);
                            exportor.SetCellTextVal(string.Format("G{0}:AD{0}", rowIndex), "", false, grayWhiteHtmlColor, true);
                            rowIndex++;

                            exportor.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), itemGroup.GroupBy.ACTIVITY_NAME, false, "", true);
                            exportor.SetCellTextVal(string.Format("G{0}:AD{0}", rowIndex), "", false, grayWhiteHtmlColor, true);
                            rowIndex++;

                            exportor.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), itemGroup.GroupBy.BUDGET_TYPE_NAME, false, "", true);
                            exportor.SetCellTextVal(string.Format("G{0}:AD{0}", rowIndex), "", false, grayWhiteHtmlColor, true);
                            rowIndex++;

                            exportor.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), itemGroup.GroupBy.EXPENSES_GROUP_NAME, false, "", true);
                            exportor.SetCellTextVal(string.Format("G{0}:AD{0}", rowIndex), "", false, grayWhiteHtmlColor, true);
                            rowIndex++;

                            var budgetAmounts = budgetType.Equals(1) ? itemGroup.GroupBy.EX_GRP_ALLOCATE_BUDGET_AMOUNT : itemGroup.GroupBy.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT;
                            var remainAmounts = budgetType.Equals(1) ? itemGroup.GroupBy.EX_GRP_REMAIN_BUDGET_AMOUNT : itemGroup.GroupBy.EX_GRP_REMAIN_OFF_BUDGET_AMOUNT;

                            // กรณีจัดสรรเป็นก้อน
                            if (!string.IsNullOrEmpty(itemGroup.GroupBy.ALLOCATE_EXPENSES_GROUP_ID))
                            {
                                int expensesCount = itemGroup.Expenses.Count() - 1;
                                exportor.SetCellCurrencyVal(string.Format("C{0}:C{1}", rowIndex, rowIndex + expensesCount), budgetAmounts, true);
                                exportor.SetCellCurrencyVal(string.Format("E{0}:E{1}", rowIndex, rowIndex + expensesCount), remainAmounts, true);
                            }


                            // รายการค่าใช้จ่าย หรือ โครงการ
                            short itemIndex = 1;
                            itemGroup.Expenses.ForEach(expensesItem =>
                            {
                                string expensesName = expensesItem.EXPENSES_NAME;
                                if (!string.IsNullOrEmpty(expensesItem.PROJECT_NAME))
                                    expensesName = string.Format("{0} ({1})", expensesName, expensesItem.PROJECT_NAME);

                                exportor.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                                exportor.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                exportor.SetCellTextVal(string.Format("B{0}", rowIndex), expensesName, true);
                                budgetAmounts = budgetType.Equals(1) ? expensesItem.ALLOCATE_BUDGET_AMOUNT : expensesItem.ALLOCATE_OFF_BUDGET_AMOUNT;
                                var useBudgetAmounts = budgetType.Equals(1) ? expensesItem.USE_BUDGET_AMOUNT : expensesItem.USE_OFF_BUDGET_AMOUNT;
                                remainAmounts = budgetType.Equals(1) ? expensesItem.REMAIN_BUDGET_AMOUNT : expensesItem.REMAIN_OFF_BUDGET_AMOUNT;

                                if (string.IsNullOrEmpty(itemGroup.GroupBy.ALLOCATE_EXPENSES_GROUP_ID))
                                {
                                    exportor.SetCellCurrencyVal(string.Format("C{0}", rowIndex), budgetAmounts, true);
                                    exportor.SetCellCurrencyVal(string.Format("E{0}", rowIndex), remainAmounts, true);
                                }
                                exportor.SetCellCurrencyVal(string.Format("D{0}", rowIndex), useBudgetAmounts, true);

                                // สร้างข้อมูลสำหรับเป็นค่าเวลานำมา Upload
                                var selectedRange = exportor.GetCellByIndex(rowIndex, 6);
                                selectedRange.AddComment(AppUtils.ToJson(expensesItem), "");

                                for (int colIndex = 6; colIndex <= 28; colIndex++)
                                    exportor.SetCellTextVal(string.Format("{0}{1}:{2}{1}", ExportUtils.ColumnsName[colIndex], rowIndex, ExportUtils.ColumnsName[++colIndex]), "", true);


                                rowIndex++;
                                itemIndex++;
                            });
                        });
                    }

                    xlsApp.Workbook.Worksheets["TEMPLATE"].Hidden = eWorkSheetHidden.VeryHidden;

                    res["filename"] = filename;
                    res["downloadFilename"] = string.Format("ฟอร์มบันทึกผลการใช้จ่าย_{0}", finalExprDepBudget.First().DEP_NAME);
                    xlsApp.SaveAs(new FileInfo(string.Format("{0}/{1}", appSettings.TemporaryPath, filename)));
                }
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ตรวจสอบข้อมูลการรายงานผลการเบิกจ่าย งบประมาณของหน่วยงานภูมิภาค และ นำเข้าไปยังฐานข้อมูล
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="type">verify = ตรวจสอบและตอบกลับ, commit = นำเข้าข้อมูล</param>
        /// <returns></returns>
        //[HttpPost]
        //public ActionResult VerifyDocument(string filename, string type)
        //{

        //}
    }
}