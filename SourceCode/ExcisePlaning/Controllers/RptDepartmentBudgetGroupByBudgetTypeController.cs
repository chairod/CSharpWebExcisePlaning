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
    /// รายงานสรุปเงินงบประมาณตามประเภทงบของหน่วยงาน
    /// เช่น งบประมาณที่ได้รับจัดสรร (เงินงบ เงินนอก) ผลการใช้จ่าย คงเหลือ เป็นต้น
    /// 
    /// Template: Report003_RptDepartmentBudgetGroupByBudgetType_Template.xls
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptDepartmentBudgetGroupByBudgetTypeController : Controller
    {
        // GET: RptDepartmentBudgetGroupByBudgetType
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_REPORT_DEPARTMENT_BUDGET_GROUP_BY_BUDGET_TYPE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_REPORT_DEPARTMENT_BUDGET_GROUP_BY_BUDGET_TYPE;
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
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprDepCashflow = db.V_GET_DEPARTMENT_BUDGET_CASH_FLOW_STATEMENTs.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1));

                // ส่วนกลาง
                if (userAuthorizeProfile.DepAuthorize.Equals(1))
                {
                    if(null !=areaId)
                        exprDepCashflow = exprDepCashflow.Where(e => e.AREA_ID.Equals(areaId));
                    if(null != depId)
                        exprDepCashflow = exprDepCashflow.Where(e => e.DEP_ID.Equals(depId));
                }
                else // หน่วยงานทั่วไป
                {
                    exprDepCashflow = exprDepCashflow.Where(e => e.AREA_ID.Equals(userAuthorizeProfile.AreaId));
                    var depAuthorize = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, userAuthorizeProfile.DepId);
                    exprDepCashflow = exprDepCashflow.Where(e => depAuthorize.AssignDepartmentIds.Contains(e.DEP_ID));
                    if (null != depId)
                        exprDepCashflow = exprDepCashflow.Where(e => e.DEP_ID.Equals(depId));
                }

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

                decimal multiplierPercentVal = decimal.Parse("100.00");

                // จัดรูปแบบข้อมูลเพื่อนำไปแสดงผล
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
                            x.DEP_SORT_INDEX
                        }).Select(x => new
                        {
                            x.Key.DEP_ID,
                            x.Key.DEP_NAME,
                            x.Key.DEP_SORT_INDEX,
                            ALLOCATE_BUDGET_AMOUNT = x.Sum(g => g.HIS_ALLOCATE_SOURCE_TYPE.Equals("EXPEN") && g.HIS_BUDGET_TYPE.Equals(1) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero),
                            ALLOCATE_OFF_BUDGET_AMOUNT = x.Sum(g => g.HIS_ALLOCATE_SOURCE_TYPE.Equals("EXPEN") && g.HIS_BUDGET_TYPE.Equals(2) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero),
                        }).OrderBy(x => x.DEP_SORT_INDEX).ToList(),

                    // รายการ คชจ. ที่หน่วยงานภูมิภาค เบิกจ่าย
                    ExprDepReported = e.Where(x => x.REPORT_BUDGET_AMOUNT != null)
                        // Group ข้อมูลเพื่อให้เป็นเฉพาะข้อมูลของ การเบิกจ่าย
                        .GroupBy(x => new { x.DEP_ID, x.EXPENSES_ID, x.PROJECT_ID, x.REPORT_BUDGET_TYPE, x.REPORT_BUDGET_AMOUNT, x.REPORT_CODE })
                        .Select(x => x.Key)
                        .GroupBy(x => new { x.DEP_ID })
                        .Select(x => new
                        {
                            x.Key.DEP_ID,
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


                // จัดกลุ่มข้อมูลตาม งบรายจ่าย
                // สำหรับนำไปสร้างเป็น Sheet Name
                var exprGroupByBudgetType = finalGroupExpenses.GroupBy(e => new
                {
                    e.GroupBy.BUDGET_TYPE_ID,
                    e.GroupBy.BUDGET_TYPE_NAME,
                    e.GroupBy.BUDGET_TYPE_ORDER_SEQ
                }).OrderBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .Select(e => new
                {
                    e.Key.BUDGET_TYPE_ID,
                    e.Key.BUDGET_TYPE_NAME,
                    e.Key.BUDGET_TYPE_ORDER_SEQ,
                    // แผนงาน ผลผลิต กิจกรรม ภายในแต่ละงบ
                    Group = e.GroupBy(x => new
                    {
                        x.GroupBy.PLAN_ID,
                        x.GroupBy.PLAN_NAME,
                        x.GroupBy.PLAN_ORDER_SEQ,
                        x.GroupBy.PRODUCE_ID,
                        x.GroupBy.PRODUCE_NAME,
                        x.GroupBy.PRODUCE_ORDER_SEQ,
                        x.GroupBy.ACTIVITY_ID,
                        x.GroupBy.ACTIVITY_NAME,
                        x.GroupBy.ACTIVITY_ORDER_SEQ
                    }).OrderBy(x => x.Key.PLAN_ORDER_SEQ)
                    .ThenBy(x => x.Key.PRODUCE_ORDER_SEQ)
                    .ThenBy(x => x.Key.ACTIVITY_ORDER_SEQ)
                    .Select(x => new
                    {
                        x.Key.PLAN_NAME,
                        x.Key.PRODUCE_NAME,
                        x.Key.ACTIVITY_NAME,
                        // หน่วยงานที่อยู่ภายใต้ แผนงาน ผลผลิต กิจกรรม
                        Departments = new Func<List<DepartmentBudgetProperty>>(() =>
                        {
                            List<DepartmentBudgetProperty> ret = new List<DepartmentBudgetProperty>();
                            x.ToList().ForEach(g =>
                            {
                                foreach (var depItem in g.ExprDepAllocate)
                                {
                                    var newItem = ret.Where(m => m.DepId.Equals(depItem.DEP_ID)).FirstOrDefault();
                                    if (null == newItem)
                                    {
                                        newItem = new DepartmentBudgetProperty()
                                        {
                                            DepId = depItem.DEP_ID,
                                            DepName = depItem.DEP_NAME,
                                            AllocateBudgetAmount = decimal.Zero,
                                            AllocateOffBudgetAmount = decimal.Zero,
                                            ReportBudgetAmount = decimal.Zero,
                                            ReportOffBudgetAmount = decimal.Zero
                                        };
                                        ret.Add(newItem);
                                    }

                                    var exprAllocateGroup = g.ExpDepAllocateGroup.Where(m => m.DEP_ID.Equals(depItem.DEP_ID)).FirstOrDefault();

                                    newItem.AllocateBudgetAmount += depItem.ALLOCATE_BUDGET_AMOUNT;
                                    newItem.AllocateBudgetAmount += null == exprAllocateGroup ? decimal.Zero : exprAllocateGroup.EX_GRP_ALLOCATE_BUDGET_AMOUNT;
                                    newItem.ReportBudgetAmount += g.ExprDepReported.Sum(m => m.DEP_ID.Equals(depItem.DEP_ID) ? m.REPORTED_BUDGET_AMOUNT : decimal.Zero);

                                    newItem.AllocateOffBudgetAmount += depItem.ALLOCATE_OFF_BUDGET_AMOUNT;
                                    newItem.AllocateOffBudgetAmount += null == exprAllocateGroup ? decimal.Zero : exprAllocateGroup.EX_GRP_ALLOCATE_OFF_BUDGET_AMOUNT;
                                    newItem.ReportOffBudgetAmount += g.ExprDepReported.Sum(m => m.DEP_ID.Equals(depItem.DEP_ID) ? m.REPORTED_OFF_BUDGET_AMOUNT : decimal.Zero);
                                }
                            });

                            return ret;
                        })()
                    }).ToList()
                });



                var appSettings = AppSettingProperty.ParseXml();
                string templateFile = string.Format("{0}/Report003_RptDepartmentBudgetGroupByBudgetType_Template.xlsx", appSettings.ReportTemplatePath);
                string filename = string.Format("{0}_รายงานสรุปเงินงบประมาณตามประเภทงบของหน่วยงาน_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
                {
                    var workSheetSummary = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "ภาพรวม"); // ภาพรวม

                    string reportTitle = workSheetSummary.Cells["A1"].Text;
                    string yearStr = (fiscalYear + 543).ToString();
                    workSheetSummary.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);

                    string exportDateText = workSheetSummary.Cells["D2"].Text;
                    string exportDateVal = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                    workSheetSummary.Cells["D2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);
                    workSheetSummary.Row(3).Hidden = true;


                    bool isSheetBudget = null == budgetTypeFlag || (budgetTypeFlag != null && budgetTypeFlag.Value.Equals(1));
                    bool isSheetOffBudget = null == budgetTypeFlag || (budgetTypeFlag != null && budgetTypeFlag.Value.Equals(2));

                    // เขียนค่าลงไฟล์ Excel
                    int rowIndex = 4;
                    foreach (var budgetTypeItem in exprGroupByBudgetType)
                    {
                        ExcelWorksheet workSheetBudget = null;
                        ExcelWorksheet workSheetOffBudget = null;

                        // เงินงบประมาณ
                        if (isSheetBudget)
                        {
                            workSheetBudget = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", string.Format("{0} (ง)", budgetTypeItem.BUDGET_TYPE_NAME));
                            workSheetBudget.Row(3).Hidden = true;

                            ExportUtils.CurrWorkSheet = workSheetBudget;
                            reportTitle = workSheetBudget.Cells["A1"].Text;
                            workSheetBudget.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);
                            workSheetBudget.Cells["D2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);
                        }
                        // เงินนอกงบประมาณ
                        if (isSheetOffBudget)
                        {
                            workSheetOffBudget = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", string.Format("{0} (งน)", budgetTypeItem.BUDGET_TYPE_NAME));
                            workSheetOffBudget.Row(3).Hidden = true;

                            ExportUtils.CurrWorkSheet = workSheetOffBudget;
                            reportTitle = workSheetOffBudget.Cells["A1"].Text;
                            workSheetOffBudget.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);
                            workSheetOffBudget.Cells["D2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);
                        }

                        int budgetTypeRowIndex = 4;
                        foreach (var item in budgetTypeItem.Group)
                        {
                            // ภาพรวม
                            ExportUtils.CurrWorkSheet = workSheetSummary;
                            ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), item.PLAN_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 1), item.PRODUCE_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 2), item.ACTIVITY_NAME, false, "", true);
                            string budgetTypeName = budgetTypeItem.BUDGET_TYPE_NAME;
                            if (isSheetBudget && isSheetOffBudget)
                                budgetTypeName = string.Format("{0} (เงินงบประมาณ + เงินนอกงบประมาณ)", budgetTypeName);
                            else
                                budgetTypeName = string.Format("{0} ({1})", budgetTypeName, isSheetBudget ? "เงินงบประมาณ" : "เงินนอกงบประมาณ");
                            ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 3), budgetTypeName, false, "", true);

                            // เขียนส่วนหัวคอลัมล์
                            rowIndex += 4;
                            ExportUtils.SetCaption(string.Format("A{0}:B{0}", rowIndex), "หน่วยงาน");
                            ExportUtils.SetCaption(string.Format("C{0}", rowIndex), "ได้รับจัดสรร (บาท)");
                            ExportUtils.SetCaption(string.Format("D{0}", rowIndex), "เบิกจ่าย (บาท)");
                            ExportUtils.SetCaption(string.Format("E{0}", rowIndex), "คงเหลือ (บาท)");
                            ExportUtils.SetCaption(string.Format("F{0}", rowIndex), "ร้อยละ");

                            // เงินงบประมาณ
                            if (isSheetBudget)
                            {
                                ExportUtils.CurrWorkSheet = workSheetBudget;
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex), item.PLAN_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex + 1), item.PRODUCE_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex + 2), item.ACTIVITY_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex + 3), budgetTypeItem.BUDGET_TYPE_NAME, false, "", true);

                                // เขียนส่วนหัวคอลัมล์
                                ExportUtils.SetCaption(string.Format("A{0}:B{0}", budgetTypeRowIndex + 4), "หน่วยงาน");
                                ExportUtils.SetCaption(string.Format("C{0}", budgetTypeRowIndex + 4), "ได้รับจัดสรร (บาท)");
                                ExportUtils.SetCaption(string.Format("D{0}", budgetTypeRowIndex + 4), "เบิกจ่าย (บาท)");
                                ExportUtils.SetCaption(string.Format("E{0}", budgetTypeRowIndex + 4), "คงเหลือ (บาท)");
                                ExportUtils.SetCaption(string.Format("F{0}", budgetTypeRowIndex + 4), "ร้อยละ");
                            }

                            // เงินนอกงบประมาณ
                            if (isSheetOffBudget)
                            {
                                ExportUtils.CurrWorkSheet = workSheetOffBudget;
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex), item.PLAN_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex + 1), item.PRODUCE_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex + 2), item.ACTIVITY_NAME, false, "", true);
                                ExportUtils.SetCellTextVal(string.Format("A{0}:F{0}", budgetTypeRowIndex + 3), budgetTypeItem.BUDGET_TYPE_NAME, false, "", true);

                                // เขียนส่วนหัวคอลัมล์
                                ExportUtils.SetCaption(string.Format("A{0}:B{0}", budgetTypeRowIndex + 4), "หน่วยงาน");
                                ExportUtils.SetCaption(string.Format("C{0}", budgetTypeRowIndex + 4), "ได้รับจัดสรร (บาท)");
                                ExportUtils.SetCaption(string.Format("D{0}", budgetTypeRowIndex + 4), "เบิกจ่าย (บาท)");
                                ExportUtils.SetCaption(string.Format("E{0}", budgetTypeRowIndex + 4), "คงเหลือ (บาท)");
                                ExportUtils.SetCaption(string.Format("F{0}", budgetTypeRowIndex + 4), "ร้อยละ");
                            }

                            // แสดงข้อมูลรายหน่วยงาน
                            rowIndex++;
                            budgetTypeRowIndex += 5;
                            foreach (var depItem in item.Departments)
                            {
                                var totalAllocateAmounts = decimal.Zero;
                                var totalPayAmounts = decimal.Zero;
                                if (isSheetBudget && isSheetOffBudget)
                                {
                                    totalAllocateAmounts = depItem.AllocateBudgetAmount + depItem.AllocateOffBudgetAmount;
                                    totalPayAmounts = depItem.ReportBudgetAmount + depItem.ReportOffBudgetAmount;
                                }
                                else
                                {
                                    totalAllocateAmounts = isSheetBudget ? depItem.AllocateBudgetAmount : depItem.AllocateOffBudgetAmount;
                                    totalPayAmounts = isSheetBudget ? depItem.ReportBudgetAmount : depItem.ReportOffBudgetAmount;
                                }
                                // ภาพรวม
                                ExportUtils.CurrWorkSheet = workSheetSummary;
                                ExportUtils.SetCellTextVal(string.Format("A{0}:B{0}", rowIndex), depItem.DepName, true);
                                ExportUtils.SetCellCurrencyVal(string.Format("C{0}", rowIndex), totalAllocateAmounts, true);
                                ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), totalPayAmounts, true);
                                ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), totalAllocateAmounts - totalPayAmounts, true);
                                ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), totalAllocateAmounts.CompareTo(decimal.Zero) == 0 ? decimal.Zero : Math.Round(totalPayAmounts / totalAllocateAmounts * multiplierPercentVal, 2, MidpointRounding.AwayFromZero), true);

                                // เงินงบประมาณ
                                if (isSheetBudget)
                                {
                                    ExportUtils.CurrWorkSheet = workSheetBudget;
                                    ExportUtils.SetCellTextVal(string.Format("A{0}:B{0}", budgetTypeRowIndex), depItem.DepName, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("C{0}", budgetTypeRowIndex), depItem.AllocateBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", budgetTypeRowIndex), depItem.ReportBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", budgetTypeRowIndex), depItem.AllocateBudgetAmount - depItem.ReportBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", budgetTypeRowIndex), depItem.AllocateBudgetAmount.CompareTo(decimal.Zero) == 0 ? decimal.Zero : Math.Round(depItem.ReportBudgetAmount / depItem.AllocateBudgetAmount * multiplierPercentVal, 2, MidpointRounding.AwayFromZero), true);
                                }

                                // เงินนอกงบประมาณ
                                if (isSheetOffBudget)
                                {
                                    ExportUtils.CurrWorkSheet = workSheetOffBudget;
                                    ExportUtils.SetCellTextVal(string.Format("A{0}:B{0}", budgetTypeRowIndex), depItem.DepName, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("C{0}", budgetTypeRowIndex), depItem.AllocateOffBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", budgetTypeRowIndex), depItem.ReportOffBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", budgetTypeRowIndex), depItem.AllocateOffBudgetAmount - depItem.ReportOffBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", budgetTypeRowIndex), depItem.AllocateOffBudgetAmount.CompareTo(decimal.Zero) == 0 ? decimal.Zero : Math.Round(depItem.ReportOffBudgetAmount / depItem.AllocateOffBudgetAmount * multiplierPercentVal, 2, MidpointRounding.AwayFromZero), true);
                                }

                                rowIndex++;
                                budgetTypeRowIndex++;
                            }

                            // สรุปยอดรวมกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย)
                            decimal totalAllocateAmount = decimal.Zero;
                            decimal totalUseAmount = decimal.Zero;
                            if (isSheetBudget && isSheetOffBudget)
                            {
                                totalAllocateAmount = item.Departments.Sum(e => e.AllocateBudgetAmount + e.AllocateOffBudgetAmount);
                                totalUseAmount = item.Departments.Sum(e => e.ReportBudgetAmount + e.ReportOffBudgetAmount);
                            }
                            else
                            {
                                totalAllocateAmount = item.Departments.Sum(e => isSheetBudget ? e.AllocateBudgetAmount : e.AllocateOffBudgetAmount);
                                totalUseAmount = item.Departments.Sum(e => isSheetBudget ? e.ReportBudgetAmount : e.ReportOffBudgetAmount);
                            }
                            ExportUtils.CurrWorkSheet = workSheetSummary;
                            ExportUtils.SetCaption(string.Format("A{0}:B{0}", rowIndex), "รวมทั้งสิ้น (บาท)");
                            ExportUtils.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                            ExportUtils.SetCellCurrencyVal(string.Format("C{0}", rowIndex), totalAllocateAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), totalUseAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), totalAllocateAmount - totalUseAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                            ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), totalAllocateAmount.CompareTo(decimal.Zero) == 1 ? Math.Round(totalUseAmount / totalAllocateAmount * multiplierPercentVal, 4) : 0, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);

                            //ExportUtils.GetRange(string.Format("A{0}:F{0}", rowIndex));
                            //ExportUtils.SelectedExcelRange.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#FF0000"));
                            //ExportUtils.SelectedExcelRange.Style.Font.Bold = true;

                            workSheetSummary.Select(string.Format("A{0}:F{0}", rowIndex));
                            workSheetSummary.SelectedRange.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#FF0000"));
                            workSheetSummary.SelectedRange.Style.Font.Bold = true;
                            rowIndex += 2; // เว้น 1 แถวแล้วเริ่มกลุ่มใหม่

                            if (isSheetBudget)
                            {
                                totalAllocateAmount = item.Departments.Sum(e => e.AllocateBudgetAmount);
                                totalUseAmount = item.Departments.Sum(e => e.ReportBudgetAmount);
                                ExportUtils.CurrWorkSheet = workSheetBudget;
                                ExportUtils.SetCaption(string.Format("A{0}:B{0}", budgetTypeRowIndex), "รวมทั้งสิ้น (บาท)");
                                ExportUtils.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                                ExportUtils.SetCellCurrencyVal(string.Format("C{0}", budgetTypeRowIndex), totalAllocateAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("D{0}", budgetTypeRowIndex), totalUseAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("E{0}", budgetTypeRowIndex), totalAllocateAmount - totalUseAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("F{0}", budgetTypeRowIndex), totalAllocateAmount.CompareTo(decimal.Zero) == 1 ? Math.Round(totalUseAmount / totalAllocateAmount * multiplierPercentVal, 4) : 0, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);

                                workSheetBudget.Select(string.Format("A{0}:F{0}", budgetTypeRowIndex));
                                workSheetBudget.SelectedRange.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#FF0000"));
                                workSheetBudget.SelectedRange.Style.Font.Bold = true;
                            }

                            if (isSheetOffBudget)
                            {
                                totalAllocateAmount = item.Departments.Sum(e => e.AllocateOffBudgetAmount);
                                totalUseAmount = item.Departments.Sum(e => e.ReportOffBudgetAmount);
                                ExportUtils.CurrWorkSheet = workSheetOffBudget;
                                ExportUtils.SetCaption(string.Format("A{0}:B{0}", budgetTypeRowIndex), "รวมทั้งสิ้น (บาท)");
                                ExportUtils.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                                ExportUtils.SetCellCurrencyVal(string.Format("C{0}", budgetTypeRowIndex), totalAllocateAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("D{0}", budgetTypeRowIndex), totalUseAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("E{0}", budgetTypeRowIndex), totalAllocateAmount - totalUseAmount, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);
                                ExportUtils.SetCellCurrencyVal(string.Format("F{0}", budgetTypeRowIndex), totalAllocateAmount.CompareTo(decimal.Zero) == 1 ? Math.Round(totalUseAmount / totalAllocateAmount * multiplierPercentVal, 4) : 0, true, ExportUtils.CurrencyNumberFormat, ExportUtils.CaptionHtmlColorCode);

                                workSheetOffBudget.Select(string.Format("A{0}:F{0}", budgetTypeRowIndex));
                                workSheetOffBudget.SelectedRange.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#FF0000"));
                                workSheetOffBudget.SelectedRange.Style.Font.Bold = true;
                            }

                            budgetTypeRowIndex += 2; // เว้น 1 แถวแล้วเริ่มกลุ่มใหม่
                        }
                    }

                    // ซ่อน WorkSheet "เงินงบประมาณ" หรือ "เงินนอกงบประมาณ" กรณีที่เลือกประเภทงบ
                    xlsApp.Workbook.Worksheets["TEMPLATE"].Hidden = eWorkSheetHidden.VeryHidden;

                    res["filename"] = filename;
                    string saveToFile = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
                    xlsApp.SaveAs(new FileInfo(saveToFile));
                }
                return Json(res, JsonRequestBehavior.DenyGet);
            }
        }
    }


    public class DepartmentBudgetProperty
    {
        /// <summary>
        /// รหัสหน่วยงาน (ที่ระบบสร้างขึ้น)
        /// </summary>
        public int DepId { get; set; }

        /// <summary>
        /// ชื่อหน่วยงาน
        /// </summary>
        public string DepName { get; set; }

        /// <summary>
        /// เงินงบประมาณ ที่ได้รับจัดสรรสุทธิ
        /// </summary>
        public decimal AllocateBudgetAmount { get; set; }

        /// <summary>
        /// เงินนอกงบประมาณ ที่ได้รับจัดสรรสุทธิ
        /// </summary>
        public decimal AllocateOffBudgetAmount { get; set; }

        /// <summary>
        /// จำนวนที่ เบิกจ่ายเงินงบประมาณ
        /// </summary>
        public decimal ReportBudgetAmount { get; set; }

        /// <summary>
        /// จำนวนที่ เบิกจ่ายเงินนอกงบประมาณ
        /// </summary>
        public decimal ReportOffBudgetAmount { get; set; }
    }
}