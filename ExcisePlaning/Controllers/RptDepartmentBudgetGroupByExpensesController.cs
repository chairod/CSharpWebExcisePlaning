using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// รายงานสรุปเงินงบประมาณตามประเภทรายจ่ายของหน่วยงาน
    /// เช่น งบประมาณที่ได้รับจัดสรร (เงินงบ เงินนอก) ผลการใช้จ่าย คงเหลือ เป็นต้น
    /// 
    /// Template: Report001_RptDepartmentBudgetGroupByExpenses_Template.xls
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptDepartmentBudgetGroupByExpensesController : Controller
    {
        // GET: RptDepartmentBudgetGroupByExpenses
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_REPORT_DEPARTMENT_BUDGET_GROUP_BY_EXPENSES);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_REPORT_DEPARTMENT_BUDGET_GROUP_BY_EXPENSES;
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
            ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            ViewBag.DefaultDepartmentId = userAuthorizeProfile.DepId;
            ViewBag.CanSelectDepartment = userAuthorizeProfile.DepAuthorize.Equals(1) ||
                (userAuthorizeProfile.DepAuthorize.Equals(2) && userAuthorizeProfile.AssignDepartmentIds.Count > 0);

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
                // เขตพื้นที่
                ViewBag.Areas = db.T_AREAs.Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_CODE = e.AREA_CODE,
                    AREA_NAME = e.AREA_NAME
                }).ToList();
                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();
                // ผลผลิต
                ViewBag.Produces = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new ProduceShortFieldProperty()
                {
                    PRODUCE_ID = e.PRODUCE_ID,
                    PRODUCE_NAME = e.PRODUCE_NAME
                }).ToList();
                // กิจกรรม
                ViewBag.Activities = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new ActivityShortFieldProperty()
                {
                    ACTIVITY_ID = e.ACTIVITY_ID,
                    ACTIVITY_NAME = e.ACTIVITY_NAME
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
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="areaId"></param>
        /// <param name="depId"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="budgetTypeFlag">1 = เงินงบ, 2 = เงินนอกงบ</param>
        /// <param name="fromDateStr">วันที่ในรูปแบบ dd/MM/yyyy ปี ค.ศ.</param>
        /// <param name="toDateStr">วันที่ในรูปแบบ dd/MM/yyyy ปี ค.ศ.</param>
        /// <param name="returnType">RETRIEVE = แสดงผลบนหน้าเว็บ, EXPORT = ส่งออกไปยัง Excel</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? areaId, int? depId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? budgetTypeFlag, string fromDateStr, string toDateStr, string returnType, int? pageIndex, int? pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // View V_GET_DEPARTMENT_BUDGET_CASH_FLOW_STATEMENT จะดึงข้อมูลจากตารางประวัติการจัดสรร และ ประวัติการเบิกจ่าย
                // ดังนั้นจะมีข้อมูลมากกว่า 1 รายการในชุดของ แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย ค่าใช้จ่าย และ โครงการ
                // จึงต้อง Group เพื่อรวมยอดเงินสุทธิ
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprDepCashflow = db.V_GET_DEPARTMENT_BUDGET_CASH_FLOW_STATEMENTs.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1));

                // ตรวจสอบการเข้าถึงข้อมูลของหน่วยงาน
                // 1. กรณีไม่เลือกหน่วยงาน ให้ใช้ข้อมูล Profile กรองข้อมูลตามสิทธิ์
                // 2. กรณีเลือกหน่วยงาน ให้ดูสิทธิ์การเข้าถึงข้อมูลของหน่วยงาน ที่เลือก
                var depFilterAuthorize = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, depId);
                if (depFilterAuthorize.Authorize.Equals(2))
                    exprDepCashflow = exprDepCashflow.Where(e => depFilterAuthorize.AssignDepartmentIds.Contains(e.DEP_ID));
                if (null != areaId && null == depId)
                    exprDepCashflow = exprDepCashflow.Where(e => e.AREA_ID.Equals(areaId));

                if (null != planId)
                    exprDepCashflow = exprDepCashflow.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprDepCashflow = exprDepCashflow.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprDepCashflow = exprDepCashflow.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprDepCashflow = exprDepCashflow.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprDepCashflow = exprDepCashflow.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));

                // ค้นหาตามช่วงวันที่
                var fromDate = AppUtils.TryValidUserDateStr(fromDateStr);
                var toDate = AppUtils.TryValidUserDateStr(toDateStr);
                if (fromDate != DateTime.MinValue && toDate != DateTime.MinValue)
                    exprDepCashflow = exprDepCashflow.Where(e => (e.HIS_ALLOCATE_DATE >= fromDate && e.HIS_ALLOCATE_DATE <= toDate)
                        || (e.REPORTED_DATE >= fromDate && e.REPORTED_DATE <= toDate));

                // ประเภทงบประมาณ(1 = เงินงบ, 2 = เงินนอกงบ)
                if (null != budgetTypeFlag)
                    exprDepCashflow = exprDepCashflow.Where(e => e.HIS_BUDGET_TYPE.Equals(budgetTypeFlag) || e.REPORT_BUDGET_TYPE.Equals(budgetTypeFlag));

                var finalGroupExpenses = exprDepCashflow.AsEnumerable().GroupBy(e => new
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
                    e.ALLOCATE_EXPENSES_GROUP_ID
                }).Select(e => new
                {
                    GroupBy = e.Key,
                    // จำนวนเงินจัดสรรเป็นก้อนตาม หมวดค่าใช้จ่าย
                    ExpDepAllocateGroup = e.Where(x => x.HIS_ALLOCATE_SOURCE_TYPE.Equals("GROU"))
                        .GroupBy(x => new { x.DEP_ID, x.HIS_BUDGET_TYPE, x.HIS_ALLOCATE_DATE, x.HIS_ALLOCATE_PERIOD, x.HIS_ALLOCATE_BUDGET_AMOUNT })
                        .Select(x => x.Key)
                        .GroupBy(x => new { x.DEP_ID })
                        .Select(x => new
                        {
                            x.Key.DEP_ID,
                            EX_GRP_ALLOCATE_BUDGET_AMOUNT = x.Sum(g => g.HIS_BUDGET_TYPE.Equals(1) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero),
                            EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT = x.Sum(g => g.HIS_BUDGET_TYPE.Equals(2) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero)
                        }).ToList(),

                    // จำนวนเงินที่หน่วยงานได้รับจัดสรรทั้งหมด
                    ExprDepAllocate = e.GroupBy(x => new { x.DEP_ID, x.DEP_NAME, x.DEP_SORT_INDEX, x.EXPENSES_ID, x.EXPENSES_NAME, x.EXPENSES_ORDER_SEQ, x.PROJECT_ID, x.PROJECT_NAME, x.HIS_ALLOCATE_SOURCE_TYPE, x.HIS_BUDGET_TYPE, x.HIS_ALLOCATE_DATE, x.HIS_ALLOCATE_PERIOD, x.HIS_ALLOCATE_BUDGET_AMOUNT })
                        .Select(x => x.Key)
                        // จำนวนเงินที่หน่วยงานภูมิภาคได้รับจัดสรร ตามรายการ คชจ. สุทธิ
                        .GroupBy(x => new
                        {
                            x.DEP_ID,
                            x.DEP_NAME,
                            x.DEP_SORT_INDEX,
                            x.EXPENSES_ID,
                            x.EXPENSES_NAME,
                            x.EXPENSES_ORDER_SEQ,
                            x.PROJECT_ID,
                            x.PROJECT_NAME
                        }).Select(x => new
                        {
                            x.Key,
                            ALLOCATE_BUDGET_AMOUNT = x.Sum(g => g.HIS_ALLOCATE_SOURCE_TYPE.Equals("EXPEN") && g.HIS_BUDGET_TYPE.Equals(1) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero),
                            ALLOCATE_OFF_BUDGET_AMOUNT = x.Sum(g => g.HIS_ALLOCATE_SOURCE_TYPE.Equals("EXPEN") && g.HIS_BUDGET_TYPE.Equals(2) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero),
                        })
                        // รวมรายการค่าใช้จ่าย ให้อยู่ภายใต้หน่วยงาน
                        .GroupBy(x => new { x.Key.DEP_ID, x.Key.DEP_NAME, x.Key.DEP_SORT_INDEX })
                        .Select(x => new
                        {
                            x.Key.DEP_ID,
                            x.Key.DEP_NAME,
                            x.Key.DEP_SORT_INDEX,
                            Expenses = x.OrderBy(g => g.Key.EXPENSES_ORDER_SEQ).ToList()
                        }).OrderBy(x => x.DEP_SORT_INDEX).ToList(),

                    // รายการ คชจ. ที่หน่วยงานภูมิภาค เบิกจ่าย
                    ExprDepReported = e.Where(x => x.REPORT_BUDGET_AMOUNT != null)
                        // Group ข้อมูลเพื่อให้เป็นเฉพาะข้อมูลของ การเบิกจ่าย
                        .GroupBy(x => new { x.DEP_ID, x.EXPENSES_ID, x.PROJECT_ID, x.REPORT_BUDGET_TYPE, x.REPORT_BUDGET_AMOUNT, x.REPORT_CODE })
                        .Select(x => x.Key)
                        .GroupBy(x => new { x.DEP_ID, x.EXPENSES_ID, x.PROJECT_ID })
                        .Select(x => new
                        {
                            x.Key.DEP_ID,
                            x.Key.EXPENSES_ID,
                            x.Key.PROJECT_ID,
                            REPORTED_BUDGET_AMOUNT = x.Sum(g => g.REPORT_BUDGET_TYPE.Value.Equals(1) ? g.REPORT_BUDGET_AMOUNT.Value : decimal.Zero),
                            REPORTED_OFF_BUDGET_AMOUNT = x.Sum(g => g.REPORT_BUDGET_TYPE.Value.Equals(2) ? g.REPORT_BUDGET_AMOUNT.Value : decimal.Zero)
                        }).ToList()
                }).OrderBy(e => e.GroupBy.PLAN_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.EXPENSES_GROUP_ORDER_SEQ).ToList();

                // นำข้อมูลส่งออกไปยังโปรแกรม XLS
                Dictionary<string, string> res = new Dictionary<string, string>(2) {
                    { "errorText", null },
                    { "filename", "" }
                };

                if (finalGroupExpenses.Count() == 0)
                {
                    res["errorText"] = "ไม่พบข้อมูล โปรดตรวจสอบเงื่อนไขการค้นหา";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var appSettings = AppSettingProperty.ParseXml();
                string templateFile = string.Format("{0}/Report001_RptDepartmentBudgetGroupByExpenses_Template.xlsx", appSettings.ReportTemplatePath);
                string filename = string.Format("{0}_รายงานสรุปเงินงบประมาณตามประเภทรายจ่ายของหน่วยงาน_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
                {
                    var multiplierPercentVal = decimal.Parse("100.00");

                    var exportAll = new ExportHelper(xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "ภาพรวม")); // ภาพรวม
                    var exportBudget = new ExportHelper(xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "เงินงบประมาณ")); // เงินงบประมาณ
                    var exportOffBudget = new ExportHelper(xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "เงินนอกงบประมาณ")); // เงินนอกงบประมาณ

                    string reportTitle = exportAll.CurrWorkSheet.Cells["A1"].Text;
                    string yearStr = (fiscalYear + 543).ToString();
                    exportAll.CurrWorkSheet.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);
                    exportBudget.CurrWorkSheet.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);
                    exportOffBudget.CurrWorkSheet.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);

                    string exportDateText = exportAll.CurrWorkSheet.Cells["D2"].Text;
                    string exportDateVal = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                    exportAll.CurrWorkSheet.Cells["D2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);
                    exportBudget.CurrWorkSheet.Cells["D2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);
                    exportOffBudget.CurrWorkSheet.Cells["D2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);

                    exportAll.CurrWorkSheet.Row(3).Hidden = true;
                    exportBudget.CurrWorkSheet.Row(3).Hidden = true;
                    exportOffBudget.CurrWorkSheet.Row(3).Hidden = true;


                    // เขียนค่าลงไฟล์ Excel
                    int rowIndex = 4;
                    foreach (var group in finalGroupExpenses)
                    {
                        // เขียนส่วนกลุ่มข้อมูล แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย
                        exportAll.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), group.GroupBy.PLAN_NAME, false, "", true);
                        exportAll.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 1), group.GroupBy.PRODUCE_NAME, false, "", true);
                        exportAll.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 2), group.GroupBy.ACTIVITY_NAME, false, "", true);
                        exportAll.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 3), group.GroupBy.BUDGET_TYPE_NAME, false, "", true);
                        exportAll.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 4), group.GroupBy.EXPENSES_GROUP_NAME, false, "", true);
                        exportAll.SetCaption(string.Format("A{0}:B{0}", rowIndex + 5), "หน่วยงาน/รายการค่าใช้จ่าย");
                        exportAll.SetCaption(string.Format("C{0}", rowIndex + 5), "ได้รับจัดสรร (บาท)");
                        exportAll.SetCaption(string.Format("D{0}", rowIndex + 5), "เบิกจ่าย (บาท)");
                        exportAll.SetCaption(string.Format("E{0}", rowIndex + 5), "คงเหลือ (บาท)");
                        exportAll.SetCaption(string.Format("F{0}", rowIndex + 5), "ร้อยละ");

                        // เขียนส่วนกลุ่มข้อมูล แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย
                        exportBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), group.GroupBy.PLAN_NAME, false, "", true);
                        exportBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 1), group.GroupBy.PRODUCE_NAME, false, "", true);
                        exportBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 2), group.GroupBy.ACTIVITY_NAME, false, "", true);
                        exportBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 3), group.GroupBy.BUDGET_TYPE_NAME, false, "", true);
                        exportBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 4), group.GroupBy.EXPENSES_GROUP_NAME, false, "", true);
                        exportBudget.SetCaption(string.Format("A{0}:B{0}", rowIndex + 5), "หน่วยงาน/รายการค่าใช้จ่าย");
                        exportBudget.SetCaption(string.Format("C{0}", rowIndex + 5), "ได้รับจัดสรร (บาท)");
                        exportBudget.SetCaption(string.Format("D{0}", rowIndex + 5), "เบิกจ่าย (บาท)");
                        exportBudget.SetCaption(string.Format("E{0}", rowIndex + 5), "คงเหลือ (บาท)");
                        exportBudget.SetCaption(string.Format("F{0}", rowIndex + 5), "ร้อยละ");

                        // เขียนส่วนกลุ่มข้อมูล แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย
                        exportOffBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), group.GroupBy.PLAN_NAME, false, "", true);
                        exportOffBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 1), group.GroupBy.PRODUCE_NAME, false, "", true);
                        exportOffBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 2), group.GroupBy.ACTIVITY_NAME, false, "", true);
                        exportOffBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 3), group.GroupBy.BUDGET_TYPE_NAME, false, "", true);
                        exportOffBudget.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 4), group.GroupBy.EXPENSES_GROUP_NAME, false, "", true);
                        exportOffBudget.SetCaption(string.Format("A{0}:B{0}", rowIndex + 5), "หน่วยงาน/รายการค่าใช้จ่าย");
                        exportOffBudget.SetCaption(string.Format("C{0}", rowIndex + 5), "ได้รับจัดสรร (บาท)");
                        exportOffBudget.SetCaption(string.Format("D{0}", rowIndex + 5), "เบิกจ่าย (บาท)");
                        exportOffBudget.SetCaption(string.Format("E{0}", rowIndex + 5), "คงเหลือ (บาท)");
                        exportOffBudget.SetCaption(string.Format("F{0}", rowIndex + 5), "ร้อยละ");

                        rowIndex += 6;


                        group.ExprDepAllocate.ForEach(exprDepItem =>
                        {
                            bool isAllocateByGroup = !string.IsNullOrEmpty(group.GroupBy.ALLOCATE_EXPENSES_GROUP_ID);
                            var depName = exprDepItem.DEP_NAME;

                            // สรุปภาพรวม การจัดสรร เบิกจ่าย คงเหลือ ของหน่วยงาน
                            var totalAllocateBudgetAmount = exprDepItem.Expenses.Sum(x => x.ALLOCATE_BUDGET_AMOUNT);
                            var totalAllocateOffBudgetAmount = exprDepItem.Expenses.Sum(x => x.ALLOCATE_OFF_BUDGET_AMOUNT);

                            // ข้อมูลการเบิกจ่าย ของหน่วยงาน
                            var exprDepReported = group.ExprDepReported.Where(x => x.DEP_ID.Equals(exprDepItem.DEP_ID));
                            var totalPayBudgetAmount = decimal.Zero;
                            var totalPayOffBudgetAmount = decimal.Zero;
                            if (exprDepReported.Any())
                            {
                                totalPayBudgetAmount = exprDepReported.Sum(x => x.REPORTED_BUDGET_AMOUNT);
                                totalPayOffBudgetAmount = exprDepReported.Sum(x => x.REPORTED_OFF_BUDGET_AMOUNT);
                            }

                            // กรณีจัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย
                            if (isAllocateByGroup)
                            {
                                var exprAllocateGroup = group.ExpDepAllocateGroup.Where(x => x.DEP_ID.Equals(exprDepItem.DEP_ID)).FirstOrDefault();
                                totalAllocateBudgetAmount += exprAllocateGroup.EX_GRP_ALLOCATE_BUDGET_AMOUNT;
                                totalAllocateOffBudgetAmount += exprAllocateGroup.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT;
                                depName = string.Format("{0} [จัดสรรเป็นก้อน]", depName);
                            }


                            var netAllocateAmounts = totalAllocateBudgetAmount + totalAllocateOffBudgetAmount;
                            var netPayAmounts = totalPayBudgetAmount + totalPayOffBudgetAmount;
                            exportAll.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), depName, true, ExportUtils.CaptionHtmlColorCode);
                            exportAll.SetCellCurrencyVal(string.Format("C{0}", rowIndex), netAllocateAmounts, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportAll.SetCellCurrencyVal(string.Format("D{0}", rowIndex), netPayAmounts, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportAll.SetCellCurrencyVal(string.Format("E{0}", rowIndex), netAllocateAmounts - netPayAmounts, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportAll.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netAllocateAmounts.CompareTo(decimal.Zero) != 0 ? Math.Round(netPayAmounts / netAllocateAmounts * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);

                            exportBudget.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), depName, true, ExportUtils.CaptionHtmlColorCode);
                            exportBudget.SetCellCurrencyVal(string.Format("C{0}", rowIndex), totalAllocateBudgetAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportBudget.SetCellCurrencyVal(string.Format("D{0}", rowIndex), totalPayBudgetAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportBudget.SetCellCurrencyVal(string.Format("E{0}", rowIndex), totalAllocateBudgetAmount - totalPayBudgetAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportBudget.SetCellCurrencyVal(string.Format("F{0}", rowIndex), totalAllocateBudgetAmount.CompareTo(decimal.Zero) != 0 ? Math.Round(totalPayBudgetAmount / totalAllocateBudgetAmount * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);

                            exportOffBudget.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), depName, true, ExportUtils.CaptionHtmlColorCode);
                            exportOffBudget.SetCellCurrencyVal(string.Format("C{0}", rowIndex), totalAllocateOffBudgetAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportOffBudget.SetCellCurrencyVal(string.Format("D{0}", rowIndex), totalPayOffBudgetAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportOffBudget.SetCellCurrencyVal(string.Format("E{0}", rowIndex), totalAllocateOffBudgetAmount - totalPayOffBudgetAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            exportOffBudget.SetCellCurrencyVal(string.Format("F{0}", rowIndex), totalAllocateOffBudgetAmount.CompareTo(decimal.Zero) != 0 ? Math.Round(totalPayOffBudgetAmount / totalAllocateOffBudgetAmount * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);

                            exportAll.GetRange(string.Format("A{0}:F{0}", rowIndex)).Style.Font.Bold = true;
                            exportBudget.GetRange(string.Format("A{0}:F{0}", rowIndex)).Style.Font.Bold = true;
                            exportOffBudget.GetRange(string.Format("A{0}:F{0}", rowIndex)).Style.Font.Bold = true;
                            rowIndex++;

                            // รายการค่าใช้จ่ายของแต่ละหน่วยงาน
                            var itemIndex = 1;
                            exprDepItem.Expenses.ForEach(expensesItem =>
                            {
                                string expensesName = string.Format("   {0}. {1}", itemIndex++, expensesItem.Key.EXPENSES_NAME);
                                if (!string.IsNullOrEmpty(expensesItem.Key.PROJECT_NAME))
                                    expensesName = string.Format("{0} ({1})", expensesName, expensesItem.Key.PROJECT_NAME);


                                var exprDepExpensesReported = exprDepReported.Where(x => x.EXPENSES_ID.Equals(expensesItem.Key.EXPENSES_ID)
                                     && x.PROJECT_ID.Equals(expensesItem.Key.PROJECT_ID)).FirstOrDefault();
                                var reportedBudgetAmount = decimal.Zero;
                                var reportedOffBudgetAmount = decimal.Zero;
                                if (null != exprDepExpensesReported)
                                {
                                    reportedBudgetAmount = exprDepExpensesReported.REPORTED_BUDGET_AMOUNT;
                                    reportedOffBudgetAmount = exprDepExpensesReported.REPORTED_OFF_BUDGET_AMOUNT;
                                }
                                var totalAllocateAmounts = expensesItem.ALLOCATE_BUDGET_AMOUNT + expensesItem.ALLOCATE_OFF_BUDGET_AMOUNT;
                                var totalPayAmounts = reportedBudgetAmount + reportedOffBudgetAmount;
                                exportAll.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), expensesName, true);
                                exportAll.SetCellCurrencyVal(string.Format("C{0}", rowIndex), totalAllocateAmounts, true);
                                exportAll.SetCellCurrencyVal(string.Format("D{0}", rowIndex), totalPayAmounts, true);

                                exportBudget.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), expensesName, true);
                                exportBudget.SetCellCurrencyVal(string.Format("C{0}", rowIndex), expensesItem.ALLOCATE_BUDGET_AMOUNT, true);
                                exportBudget.SetCellCurrencyVal(string.Format("D{0}", rowIndex), reportedBudgetAmount, true);

                                exportOffBudget.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), expensesName, true);
                                exportOffBudget.SetCellCurrencyVal(string.Format("C{0}", rowIndex), expensesItem.ALLOCATE_OFF_BUDGET_AMOUNT, true);
                                exportOffBudget.SetCellCurrencyVal(string.Format("D{0}", rowIndex), reportedOffBudgetAmount, true);

                                if (isAllocateByGroup)
                                {
                                    exportAll.SetCellCurrencyVal(string.Format("E{0}", rowIndex), decimal.Zero, true);
                                    exportAll.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netAllocateAmounts.CompareTo(decimal.Zero) != 0 ? Math.Round(totalPayAmounts / netAllocateAmounts * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true);

                                    exportBudget.SetCellCurrencyVal(string.Format("E{0}", rowIndex), decimal.Zero, true);
                                    exportBudget.SetCellCurrencyVal(string.Format("F{0}", rowIndex), totalAllocateBudgetAmount.CompareTo(decimal.Zero) != 0 ? Math.Round(reportedBudgetAmount / totalAllocateBudgetAmount * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true);

                                    exportOffBudget.SetCellCurrencyVal(string.Format("E{0}", rowIndex), decimal.Zero, true);
                                    exportOffBudget.SetCellCurrencyVal(string.Format("F{0}", rowIndex), totalAllocateOffBudgetAmount.CompareTo(decimal.Zero) != 0 ? Math.Round(reportedOffBudgetAmount / totalAllocateOffBudgetAmount * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true);
                                }
                                else
                                {
                                    exportAll.SetCellCurrencyVal(string.Format("E{0}", rowIndex), totalAllocateAmounts - totalPayAmounts, true);
                                    exportAll.SetCellCurrencyVal(string.Format("F{0}", rowIndex), totalAllocateAmounts.CompareTo(decimal.Zero) != 0 ? Math.Round(totalPayAmounts / totalAllocateAmounts * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true);

                                    exportBudget.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expensesItem.ALLOCATE_BUDGET_AMOUNT - reportedBudgetAmount, true);
                                    exportBudget.SetCellCurrencyVal(string.Format("F{0}", rowIndex), expensesItem.ALLOCATE_BUDGET_AMOUNT.CompareTo(decimal.Zero) != 0 ? Math.Round(reportedBudgetAmount / expensesItem.ALLOCATE_BUDGET_AMOUNT * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true);

                                    exportOffBudget.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expensesItem.ALLOCATE_OFF_BUDGET_AMOUNT - reportedOffBudgetAmount, true);
                                    exportOffBudget.SetCellCurrencyVal(string.Format("F{0}", rowIndex), expensesItem.ALLOCATE_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) != 0 ? Math.Round(reportedOffBudgetAmount / expensesItem.ALLOCATE_OFF_BUDGET_AMOUNT * multiplierPercentVal, 2, MidpointRounding.AwayFromZero) : decimal.Zero, true);
                                }
                                rowIndex++;
                            });
                        });

                        rowIndex += 2;
                    }

                    // ซ่อน WorkSheet "เงินงบประมาณ" หรือ "เงินนอกงบประมาณ" กรณีที่เลือกประเภทงบ
                    xlsApp.Workbook.Worksheets[1].Hidden = eWorkSheetHidden.VeryHidden; // ซ่อน Sheet "TEMPLATE"
                    if (budgetTypeFlag != null)
                    {
                        xlsApp.Workbook.Worksheets[2].Hidden = eWorkSheetHidden.VeryHidden; // ภาพรวม
                        if (budgetTypeFlag.Value.Equals(1))
                            xlsApp.Workbook.Worksheets[4].Hidden = eWorkSheetHidden.VeryHidden; // เงินนอกงบประมาณ
                        else if (budgetTypeFlag.Value.Equals(2))
                            xlsApp.Workbook.Worksheets[3].Hidden = eWorkSheetHidden.VeryHidden; // เงินงบประมาณ
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