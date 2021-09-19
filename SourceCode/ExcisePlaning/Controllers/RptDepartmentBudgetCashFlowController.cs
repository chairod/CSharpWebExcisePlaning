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
    /// รายงานทะเบียนคุมเงินงบประมาณของหน่วยงานภูมิภาค
    /// 
    /// Template: Report006_DepartmentBudgetCashFlow_Template.xls
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptDepartmentBudgetCashFlowController : Controller
    {
        // GET: RptDepartmentBudgetCashFlow
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_REPORT_DEPARTMENT_BUDGET_CASH_FLOW);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_REPORT_DEPARTMENT_BUDGET_CASH_FLOW;
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
        public ActionResult Retrieve(int fiscalYear, int? areaId, int? depId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int budgetTypeFlag, string fromDateStr, string toDateStr, string returnType, int? pageIndex, int? pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // View V_GET_DEPARTMENT_BUDGET_CASH_FLOW_STATEMENT จะดึงข้อมูลจากตารางประวัติการจัดสรร และ ประวัติการเบิกจ่าย
                // ดังนั้นจะมีข้อมูลมากกว่า 1 รายการในชุดของ แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย ค่าใช้จ่าย และ โครงการ
                // จึงต้อง Group เพื่อรวมยอดเงินสุทธิ
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprDepCashflow = db.V_GET_DEPARTMENT_BUDGET_CASH_FLOW_STATEMENTs.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1)
                    && (e.HIS_BUDGET_TYPE.Equals(budgetTypeFlag) || e.REPORT_BUDGET_TYPE.Equals(budgetTypeFlag)));

                // หน่วยงานกลาง
                if (userAuthorizeProfile.DepAuthorize.Equals(1))
                {
                    if(null!=areaId)
                        exprDepCashflow = exprDepCashflow.Where(e => e.AREA_ID.Equals(areaId));
                    if(null!=depId)
                        exprDepCashflow = exprDepCashflow.Where(e => e.DEP_ID.Equals(depId));
                }
                else // หน่วยงานทั่วไป
                {
                    
                    var depAuthorize = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, userAuthorizeProfile.DepId);
                    exprDepCashflow = exprDepCashflow.Where(e => e.AREA_ID.Equals(userAuthorizeProfile.AreaId));
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
                        .GroupBy(x => new { x.DEP_ID, x.DEP_NAME, x.DEP_SORT_INDEX, x.HIS_BUDGET_TYPE, x.HIS_ALLOCATE_DATE, x.HIS_ALLOCATE_PERIOD, x.HIS_REFER_DOC_NO, x.HIS_ALLOCATE_BUDGET_AMOUNT })
                        .Select(x => x.Key)
                        .GroupBy(x => new { x.DEP_ID, x.DEP_NAME, x.DEP_SORT_INDEX, x.HIS_REFER_DOC_NO, x.HIS_ALLOCATE_PERIOD, x.HIS_ALLOCATE_DATE })
                        .Select(x => new
                        {
                            x.Key,
                            ALLOCATE_BUDGET_AMOUNT = x.Sum(g => g.HIS_BUDGET_TYPE.Equals(1) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero),
                            ALLOCATE_OFF_BUDGET_AMOUNT = x.Sum(g => g.HIS_BUDGET_TYPE.Equals(2) ? g.HIS_ALLOCATE_BUDGET_AMOUNT.Value : decimal.Zero)
                        }).ToList(),

                    // Group ข้อมูลให้เป็นเฉพาะการจัดสรร (เนื่องจากดึงจากประวัติ)
                    ExprDepAllocate = e.GroupBy(x => new { x.DEP_ID, x.DEP_NAME, x.DEP_SORT_INDEX, x.EXPENSES_ID, x.EXPENSES_NAME, x.EXPENSES_ORDER_SEQ, x.PROJECT_ID, x.PROJECT_NAME, x.HIS_ALLOCATE_SOURCE_TYPE, x.HIS_BUDGET_TYPE, x.HIS_ALLOCATE_DATE, x.HIS_REFER_DOC_NO, x.HIS_ALLOCATE_PERIOD, x.HIS_ALLOCATE_BUDGET_AMOUNT })
                        .Select(x => x.Key)
                        // จำนวนเงินที่หน่วยงานภูมิภาคได้รับจัดสรร ตามรายการ คชจ. สุทธิ
                        .GroupBy(x => new
                        {
                            x.DEP_ID,
                            x.DEP_NAME,
                            x.DEP_SORT_INDEX,
                            x.HIS_REFER_DOC_NO,
                            x.HIS_ALLOCATE_PERIOD,
                            x.HIS_ALLOCATE_DATE,
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
                        }).ToList(),

                    // รายการ คชจ. ที่หน่วยงานภูมิภาค เบิกจ่าย
                    ExprDepReported = e.Where(x => x.REPORT_BUDGET_AMOUNT != null)
                        // Group ข้อมูลเพื่อให้เป็นเฉพาะข้อมูลของ การเบิกจ่าย (เนื่องจากดึงจากประวัติ)
                        .GroupBy(x => new { x.DEP_ID, x.DEP_NAME, x.DEP_SORT_INDEX, x.EXPENSES_ID, x.EXPENSES_NAME, x.EXPENSES_ORDER_SEQ, x.PROJECT_ID, x.REPORT_BUDGET_TYPE, x.REPORT_BUDGET_AMOUNT, x.REPORT_CODE, x.REPORTED_DATE })
                        .Select(x => x.Key)
                        .GroupBy(x => new { x.DEP_ID, x.DEP_NAME, x.DEP_SORT_INDEX, x.EXPENSES_ID, x.EXPENSES_NAME, x.EXPENSES_ORDER_SEQ, x.PROJECT_ID, x.REPORT_CODE, x.REPORTED_DATE })
                        .Select(x => new
                        {
                            x.Key,
                            REPORTED_BUDGET_AMOUNT = x.Sum(g => g.REPORT_BUDGET_TYPE.Value.Equals(1) ? g.REPORT_BUDGET_AMOUNT.Value : decimal.Zero),
                            REPORTED_OFF_BUDGET_AMOUNT = x.Sum(g => g.REPORT_BUDGET_TYPE.Value.Equals(2) ? g.REPORT_BUDGET_AMOUNT.Value : decimal.Zero)
                        }).ToList()
                })
                // นำข้อมูล จัดสรรเป็นก้อน, จัดสรรตามรายการ คชจ., รายงานผล/เบิกจ่าย
                // มารวมกัน เพื่อแยกย่อยลงตามหน่วยงาน
                .Select(e => new
                {
                    e.GroupBy,
                    Departments = new Func<List<DepartmentCashProperty>>(() =>
                    {
                        var ret = new List<DepartmentCashProperty>();

                        // จัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย
                        e.ExpDepAllocateGroup.ForEach(item =>
                        {
                            var currDepItem = ret.Where(x => x.DepId.Equals(item.Key.DEP_ID)).FirstOrDefault();
                            if (null == currDepItem)
                            {
                                currDepItem = new DepartmentCashProperty(item.Key.DEP_ID, item.Key.DEP_NAME, item.Key.DEP_SORT_INDEX);
                                ret.Add(currDepItem);
                            }

                            var allocateAmounts = budgetTypeFlag.Equals(1) ? item.ALLOCATE_BUDGET_AMOUNT : item.ALLOCATE_OFF_BUDGET_AMOUNT;
                            currDepItem.AllocateBudgetAmounts += allocateAmounts;
                            currDepItem.BalanceAmounts = currDepItem.AllocateBudgetAmounts - currDepItem.PayBudgetAmounts;

                            currDepItem.ExpensesCashFlow.Add(new DepartmentExpensesCashFlowProperty()
                            {
                                ExpensesId = null,
                                ProjectId = null,
                                ExpensesOrderSeq = -1,
                                ProjectOrderSeq = -1,
                                TranDate = item.Key.HIS_ALLOCATE_DATE.Value,
                                DocNo = item.Key.HIS_REFER_DOC_NO,
                                Description = string.Format("รับโอนจากกรมฯ งวดที่ {0} (จัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย)", item.Key.HIS_ALLOCATE_PERIOD),
                                AllocateBudgetAmounts = allocateAmounts,
                                PayBudgetAmounts = decimal.Zero
                            });
                        });

                        // จัดสรรตามรายการ ค่าใช้จ่าย หรือ โครงการ
                        e.ExprDepAllocate.ForEach(item =>
                        {
                            var currDepItem = ret.Where(x => x.DepId.Equals(item.Key.DEP_ID)).FirstOrDefault();
                            if (null == currDepItem)
                            {
                                currDepItem = new DepartmentCashProperty(item.Key.DEP_ID, item.Key.DEP_NAME, item.Key.DEP_SORT_INDEX);
                                ret.Add(currDepItem);
                            }

                            var allocateAmounts = budgetTypeFlag.Equals(1) ? item.ALLOCATE_BUDGET_AMOUNT : item.ALLOCATE_OFF_BUDGET_AMOUNT;
                            if(allocateAmounts.CompareTo(decimal.Zero) != 0)
                            {
                                currDepItem.AllocateBudgetAmounts += allocateAmounts;
                                currDepItem.BalanceAmounts = currDepItem.AllocateBudgetAmounts - currDepItem.PayBudgetAmounts;

                                string expensesName = item.Key.EXPENSES_NAME;
                                if (null != item.Key.PROJECT_ID)
                                    expensesName = string.Format("{0} ({1})", expensesName, item.Key.PROJECT_NAME);
                                currDepItem.ExpensesCashFlow.Add(new DepartmentExpensesCashFlowProperty()
                                {
                                    ExpensesId = item.Key.EXPENSES_ID,
                                    ProjectId = item.Key.PROJECT_ID,
                                    ExpensesOrderSeq = item.Key.EXPENSES_ORDER_SEQ.Value,
                                    ProjectOrderSeq = item.Key.PROJECT_ID == null ? -1 : item.Key.PROJECT_ID.Value,
                                    TranDate = item.Key.HIS_ALLOCATE_DATE.Value,
                                    DocNo = item.Key.HIS_REFER_DOC_NO,
                                    Description = string.Format("รับโอนจากกรมฯ งวดที่ {0} - {1}", item.Key.HIS_ALLOCATE_PERIOD, expensesName),
                                    AllocateBudgetAmounts = allocateAmounts,
                                    PayBudgetAmounts = decimal.Zero
                                });
                            }
                        });

                        // รายงานผลการใช้จ่าย
                        e.ExprDepReported.ForEach(item =>
                        {
                            var currDepItem = ret.Where(x => x.DepId.Equals(item.Key.DEP_ID)).FirstOrDefault();
                            if (null == currDepItem)
                            {
                                currDepItem = new DepartmentCashProperty(item.Key.DEP_ID, item.Key.DEP_NAME, item.Key.DEP_SORT_INDEX);
                                ret.Add(currDepItem);
                            }

                            var payAmounts = budgetTypeFlag.Equals(1) ? item.REPORTED_BUDGET_AMOUNT : item.REPORTED_OFF_BUDGET_AMOUNT;
                            currDepItem.PayBudgetAmounts += payAmounts;
                            currDepItem.BalanceAmounts = currDepItem.AllocateBudgetAmounts - currDepItem.PayBudgetAmounts;

                            currDepItem.ExpensesCashFlow.Add(new DepartmentExpensesCashFlowProperty()
                            {
                                ExpensesId = item.Key.EXPENSES_ID,
                                ProjectId = item.Key.PROJECT_ID,
                                ExpensesOrderSeq = item.Key.EXPENSES_ORDER_SEQ.Value,
                                ProjectOrderSeq = item.Key.PROJECT_ID == null ? -1 : item.Key.PROJECT_ID.Value,
                                TranDate = item.Key.REPORTED_DATE.Value,
                                DocNo = item.Key.REPORT_CODE,
                                Description = "รายงานผลการใช้จ่าย",
                                AllocateBudgetAmounts = decimal.Zero,
                                PayBudgetAmounts = payAmounts
                            });
                        });


                        ret = ret.OrderBy(x => x.DepOrderSeq).ToList();
                        return ret;
                    })()
                }).OrderBy(e => e.GroupBy.PLAN_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.GroupBy.EXPENSES_GROUP_ORDER_SEQ)
                .ToList();

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
                string templateFile = string.Format("{0}/Report006_DepartmentBudgetCashFlow_Template.xlsx", appSettings.ReportTemplatePath);
                string filename = string.Format("{0}_รายงานทะเบียนคุมเงินงบประมาณของหน่วยงานภูมิภาค_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
                {
                    ExportHelper exporter = new ExportHelper(xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "ทะเบียนคุม"));
                    exporter.CurrWorkSheet.View.ZoomScale = 80;
                    Color depColor = ColorTranslator.FromHtml("#C9E4FF");
                    Color redColor = ColorTranslator.FromHtml("#FF0000");

                    string reportTitle = exporter.CurrWorkSheet.Cells["A1"].Text;
                    string yearStr = (fiscalYear + 543).ToString();
                    exporter.CurrWorkSheet.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);

                    string exportDateText = exporter.CurrWorkSheet.Cells["E2"].Text;
                    string exportDateVal = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                    exporter.CurrWorkSheet.Cells["E2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);

                    exporter.CurrWorkSheet.Row(3).Hidden = true;


                    // เขียนค่าลงไฟล์ Excel
                    int rowIndex = 4;
                    foreach (var group in finalGroupExpenses)
                    {
                        // เขียนส่วนกลุ่มข้อมูล แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย
                        exporter.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), group.GroupBy.PLAN_NAME, false, "", true);
                        exporter.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 1), group.GroupBy.PRODUCE_NAME, false, "", true);
                        exporter.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 2), group.GroupBy.ACTIVITY_NAME, false, "", true);
                        exporter.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 3), group.GroupBy.BUDGET_TYPE_NAME, false, "", true);
                        exporter.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex + 4), group.GroupBy.EXPENSES_GROUP_NAME, false, "", true);
                        exporter.SetCaption(string.Format("A{0}", rowIndex + 5), "วัน/เดือน/ปี");
                        exporter.SetCaption(string.Format("B{0}", rowIndex + 5), "เลขที่เอกสาร GFMIS");
                        exporter.SetCaption(string.Format("C{0}", rowIndex + 5), "รายการ");
                        exporter.SetCaption(string.Format("D{0}", rowIndex + 5), "รับ (บาท)");
                        exporter.SetCaption(string.Format("E{0}", rowIndex + 5), "จ่าย (บาท)");
                        exporter.SetCaption(string.Format("F{0}", rowIndex + 5), "คงเหลือ (บาท)");

                        rowIndex += 6;

                        group.Departments.ForEach(exprDepItem =>
                        {
                            exporter.SetCellTextVal(string.Format("A{0}:C{0}", rowIndex), exprDepItem.DepName, true);
                            exporter.SetCellCurrencyVal(string.Format("D{0}", rowIndex), exprDepItem.AllocateBudgetAmounts, true);
                            exporter.SetCellCurrencyVal(string.Format("E{0}", rowIndex), exprDepItem.PayBudgetAmounts, true);
                            exporter.SetCellCurrencyVal(string.Format("F{0}", rowIndex), exprDepItem.AllocateBudgetAmounts - exprDepItem.PayBudgetAmounts, true);
                            var range = exporter.GetRange(string.Format("A{0}:F{0}", rowIndex));
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(depColor);
                            rowIndex++;

                            var exprExpenses = exprDepItem.ExpensesCashFlow.OrderBy(e => e.ExpensesOrderSeq)
                                .ThenBy(e => e.ProjectOrderSeq)
                                .ThenBy(e => e.TranDate).ToList();
                            string oldExpensesVal = string.Empty;
                            decimal expensesBudgetBalance = decimal.Zero;
                            bool isAllocateGroup = false;
                            foreach(var expensesItem in exprExpenses)
                            {
                                if (null == expensesItem.ExpensesId)
                                    isAllocateGroup = true;

                                string keyId = string.Format("{0}_{1}", expensesItem.ExpensesId, expensesItem.ProjectId);
                                if (!oldExpensesVal.Equals(keyId) && !isAllocateGroup)
                                    expensesBudgetBalance = decimal.Zero;

                                expensesBudgetBalance += expensesItem.AllocateBudgetAmounts;
                                expensesBudgetBalance -= expensesItem.PayBudgetAmounts;
                                exporter.SetCellTextVal(string.Format("A{0}", rowIndex), expensesItem.TranDate.ToString("dd/MM/yyyy", AppUtils.ThaiCultureInfo), true);
                                exporter.SetCellTextVal(string.Format("B{0}", rowIndex), expensesItem.DocNo, true);
                                exporter.SetCellTextVal(string.Format("C{0}", rowIndex), expensesItem.Description, true);
                                exporter.SetCellCurrencyVal(string.Format("D{0}", rowIndex), expensesItem.AllocateBudgetAmounts, true);
                                exporter.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expensesItem.PayBudgetAmounts, true);
                                exporter.SetCellCurrencyVal(string.Format("F{0}", rowIndex), expensesBudgetBalance, true);

                                var allocateCompareVal = expensesItem.AllocateBudgetAmounts.CompareTo(decimal.Zero);
                                // กรณีได้รับจัดสรร ให้ทำเป็นตัวหนา
                                if (allocateCompareVal != 0)
                                    exporter.GetRange(string.Format("A{0}:F{0}", rowIndex)).Style.Font.Bold = true;

                                // ยอดจัดสรรเป็นลบ (ดึงคืนงบประมาณ) ให้แสดงตัวเลขสีแดง
                                if(allocateCompareVal == -1)
                                    exporter.GetRange(string.Format("B{0}:D{0}", rowIndex)).Style.Font.Color.SetColor(redColor);

                                rowIndex++;
                                oldExpensesVal = keyId;
                            }
                        });

                        rowIndex += 2;
                    }


                    xlsApp.Workbook.Worksheets[1].Hidden = eWorkSheetHidden.VeryHidden; // ซ่อน Sheet "TEMPLATE"
                    res["filename"] = filename;
                    string saveToFile = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
                    xlsApp.SaveAs(new FileInfo(saveToFile));
                }

                return Json(res, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ข้อมูลกระแสเงินสดของหน่วยงาน ซึ่งประกอบไปด้วย รายการค่าใช้จ่ายต่างๆ
        /// ที่ได้รับจัดสรรจากส่วนกลาง (กรมสรรพสามิต)
        /// </summary>
        private class DepartmentCashProperty
        {
            public DepartmentCashProperty(int depIdVal, string depNameVal, int depOrderSeqVal)
            {
                DepId = depIdVal;
                DepName = depNameVal;
                DepOrderSeq = depOrderSeqVal;
                ExpensesCashFlow = new List<DepartmentExpensesCashFlowProperty>();
            }

            public int DepId { get; set; }
            public string DepName { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียง ของหน่วยงาน
            /// </summary>
            public int DepOrderSeq { get; set; }

            /// <summary>
            /// ยอดเงินที่ได้รับจัดสรรสุทธิของหน่วยงาน (เงินงบ หรือ เงินนอกงบ), บาท
            /// </summary>
            public decimal AllocateBudgetAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินที่เบิกจ่ายสุทธิของหน่วยงาน (บาท)
            /// </summary>
            public decimal PayBudgetAmounts { get; set; }

            /// <summary>
            /// ยอดเงินคงเหลือสุทธิของหน่วยงาน (บาท)
            /// </summary>
            public decimal BalanceAmounts { get; set; }

            /// <summary>
            /// รายการเคลื่อนไหวของกระแสเงิน ในแต่ละรายการค่าใช้จ่าย หรือ โครงการ
            /// ของหน่วยงาน
            /// <para>ก่อนการใช้ให้จัดเรียงข้อมูลก่อน ด้วย ExpensesOrderSeq, ProjectOrderSeq, TranDate ตามลำดับ</para>
            /// </summary>
            public List<DepartmentExpensesCashFlowProperty> ExpensesCashFlow { get; set; }
        }


        /// <summary>
        /// กระแสเงินสด ของรายการค่าใช้จ่าย เช่น ได้รับจัดสรรเมื่อไหร่ ใช้จ่ายไปเมื่อไหร่ เป็นต้น
        /// </summary>
        private class DepartmentExpensesCashFlowProperty
        {
            /// <summary>
            /// รหัสรายการค่าใช้จ่าย ที่ระบบสร้างขึ้น
            /// </summary>
            public int? ExpensesId { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียง รายการค่าใช้จ่าย
            /// </summary>
            public int ExpensesOrderSeq { get; set; }


            /// <summary>
            /// รหัสโครงการภายใต้ รายการค่าใช้จ่าย ที่ระบบสร้างขึ้น
            /// </summary>
            public int? ProjectId { get; set; }

            // ลำดับการจัดเรียง รายละเอียดภายใต้ค่าใช้จ่าย
            public int ProjectOrderSeq { get; set; }

            /// <summary>
            /// วันที่เกิดรายการ (วัน)
            /// </summary>
            public DateTime TranDate { get; set; }

            /// <summary>
            /// เลขที่เอกสาร เช่น เลขที่เอกสารการจัดสรรงบประมาณ หรือ เลขที่เอกสารการเบิกจ่าย เป็นต้น
            /// </summary>
            public string DocNo { get; set; }

            /// <summary>
            /// รายละเอียดของรายการ
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// ยอดเงินที่ได้รับจัดสรร (เงินงบ หรือ เงินนอกงบ), บาท
            /// </summary>
            public decimal AllocateBudgetAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินที่เบิกจ่าย (บาท)
            /// </summary>
            public decimal PayBudgetAmounts { get; set; }
        }
    }

}