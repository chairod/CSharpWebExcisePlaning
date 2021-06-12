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
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// จัดสรรเงินงบประมาณ ลงไปให้กับหน่วยงานภายนอก แบบรวม
    /// คือรวมทุกหน่วยงาน มาจัดสรรตามรายการ คชจ. ที่กรมสรรพสามิตมีอยู่ (เฉพาะคำขอต้นปี ที่หน่วยงาน SignOff และยังไม่จัดสรรงบ)
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetAllocateDepartmentGroupController : Controller
    {
        // GET: BudgetAllocateDepartmentGroup
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            // ตรวจสอบการเข้าทำงานของจอ
            var fiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            var verifyBudget = BudgetUtils.VerifyBudget(fiscalYear, null);
            if (!verifyBudget.IsComplete)
                return RedirectToAction("GetPageWarning", "BudgetAllocateDepartmentGroup");

            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_ALLOCATE_GROUP_BY_DEAPRTMENT_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_ALLOCATE_GROUP_BY_DEAPRTMENT_MENU;
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

            ViewBag.FiscalYear = fiscalYear;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // เฉพาะหน่วยงานที่ ทำคำขอเงินงบประมาณได้
                // ในระบบจะมีทั้ง หน่วยงานที่คุมหน่วยงานภูมิภาค ซึ่งหน่วยงานเหล่านั้นไม่สามารถทำคำขอ และ ไม่ได้รับเงิบงบประมาณ
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && e.CAN_REQUEST_BUDGET).Select(e => new DepartmentShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = db.T_AREAs.Where(a => a.AREA_ID.Equals(e.AREA_ID)).Select(a => a.AREA_NAME).FirstOrDefault(),
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME,
                    DEP_SHORT_NAME = e.DEP_SHORT_NAME
                }).OrderBy(e => e.AREA_ID).ThenBy(e => e.DEP_NAME).ToList();

                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).ToList();
            }

            return View();
        }

        /// <summary>
        /// แสดงข้อความ แจ้งเตือนก่อนเข้าสู่หน้าจอ
        /// ซึ่ง ต้องทำอะไรก่อน ถึงจะสามารถทำรายการในหน้าจอนี้ได้
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetPageWarning()
        {
            // ตรวจสอบการเข้าทำงานของจอ
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            var fiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            var verifyBudget = BudgetUtils.VerifyBudget(fiscalYear, null);
            ViewBag.CauseMessages = verifyBudget.CauseMessage;
            return View();
        }


        /// <summary>
        /// สร้างแบบฟอร์มสำหรับจัดสรรงบประมาณลงไปให้หน่วยงาน
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="requestType">1=คำขอต้นปี, 2 = คำขอเพิ่มเติม</param>
        /// <param name="allocateType">จัดสรรจาก 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetDownloadAllocateForm(int fiscalYear, int requestType, int allocateType, List<int> depIds)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(2) {
                { "errorText", null },
                { "filename", null }
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var verifyBudget = BudgetUtils.VerifyBudget(fiscalYear, allocateType);
                if (!verifyBudget.IsComplete)
                {
                    res["errorText"] = verifyBudget.FormatCauseMessageToUser();
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // รหัสหน่วยงานที่ไม่สามารถส่งคำขอเงินงบประมาณได้
                // ให้ยกเว้น เพราะถือว่าเป็นหน่วยงานที่คุมหน่วยงานอื่นๆ
                var depIdsIgnoreToAllocate = AppUtils.GetAllDepartmentIdsCannotRequestBudget();

                // Procedure จะใช้ข้อมูลรายการ คชจ./โครงการภายใต้ คชจ. ตั้งต้นแล้ว Left join กับคำขอของแต่ล่ะหน่วยงาน
                var exprReqExpenses = db.proc_GetDepartmentRequestBudgetForAllocate(fiscalYear, allocateType, requestType).ToList();
                if (depIds != null && depIds.Count > 0)
                    exprReqExpenses = exprReqExpenses.Where(e => e.DEP_ID != null && !depIdsIgnoreToAllocate.Contains(e.DEP_ID.Value) && depIds.Contains(e.DEP_ID.Value)).ToList();
                else
                    exprReqExpenses = exprReqExpenses.Where(e => e.DEP_ID != null && !depIdsIgnoreToAllocate.Contains(e.DEP_ID.Value)).ToList();
                if (!exprReqExpenses.Any())
                {
                    res["errorText"] = "ยังไม่มีคำของบประมาณ หรือคำขอยังไม่ SignOff จากหน่วยงานภูมิภาค";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var appSettings = AppSettingProperty.ParseXml();
                string templateFile = string.Format("{0}/Allocate_Budget_To_Department_Template.xlsx", appSettings.ReportTemplatePath);
                if (!System.IO.File.Exists(templateFile))
                {
                    res["errorText"] = "ไม่พบแบบฟอร์มคำขอ โปรดแจ้งผู้ดูแลระบบให้ตรวจสอบแบบฟอร์ม BudgetAllocateTemplate.xlsx";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // หน่วยงานที่ไม่ทำคำขอมาในรายการ คชจ. ใดๆ ให้เพิ่มหน่วยงานเข้าไป
                // เพื่อให้รายการ คชจ. มีรายชื่อหน่วยงานที่เท่ากับ จำนวนหน่วยงานที่อยู่ในผลลัพธ์ที่จะจัดสรรคำขอ
                var exprGroupForAddDepartment = exprReqExpenses.GroupBy(e => new
                {
                    e.YR,
                    e.PLAN_ID,
                    e.PLAN_NAME,
                    e.PLAN_ORDER_SEQ,
                    e.PRODUCE_ID,
                    e.PRODUCE_NAME,
                    e.PRODUCE_ORDER_SEQ,
                    e.ACTIVITY_ID,
                    e.ACTIVITY_NAME,
                    e.ACTIVITY_ORDER_SEQ,
                    e.ACTIVITY_SHORT_NAME,
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.BUDGET_TYPE_ORDER_SEQ,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG, // 1 = จัดสรรงบประมาณตามหมวดค่าใช้จ่าย
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.PROJECT_ID,
                    e.PROJECT_NAME,
                    e.GLCODEs,
                    e.REQUIRED_ALLOCATE_TYPE,
                    e.REQUIRED_REQUEST_TYPE
                }).Select(e => new
                {
                    groupBy = e.Key,
                    Departments = e.GroupBy(x => new { x.DEP_ID, x.REQ_ID }).Select(x => x.Key).ToList()
                }).ToList();
                var allRequestDepartments = exprReqExpenses.GroupBy(e => new { e.DEP_ID, e.DEP_CODE, e.DEP_SORT_INDEX, e.DEP_NAME, e.AREA_ID, e.AREA_NAME, e.REQ_ID })
                        .Select(e => e.Key).ToList();
                foreach (var exprGroup in exprGroupForAddDepartment)
                {
                    // มีหน่วยงานใดบ้าง ที่จัดทำคำขอ แล้วไม่มีรายการ คชจ. (ให้เพิ่มหน่วยงานนั้นเข้าไปในผลลัพธ์)
                    var exprDepartmentNotFound = allRequestDepartments.Where(expr => !exprGroup.Departments.Any(x => x.DEP_ID.Equals(expr.DEP_ID) && x.REQ_ID.Equals(expr.REQ_ID))).ToList();
                    exprDepartmentNotFound.ForEach(expr =>
                    {
                        // ค้นหางบประมาณที่เคยจัดสรรให้กับหน่วยงาน
                        decimal allocateBudgetAmounts = db.fn_GetAllocateBudgetAmountsToDepartmentBy(fiscalYear, expr.DEP_ID
                            , exprGroup.groupBy.PLAN_ID, exprGroup.groupBy.PRODUCE_ID
                            , exprGroup.groupBy.ACTIVITY_ID, exprGroup.groupBy.BUDGET_TYPE_ID
                            , exprGroup.groupBy.EXPENSES_GROUP_ID, exprGroup.groupBy.EXPENSES_ID
                            , exprGroup.groupBy.PROJECT_ID, Convert.ToInt16(allocateType)).GetValueOrDefault(decimal.Zero);
                        decimal? depAllocateBudgetAmounts = allocateType.Equals(1) ? allocateBudgetAmounts : decimal.Zero;
                        decimal? depAllocateOffBudgetAmounts = allocateType.Equals(2) ? allocateBudgetAmounts : decimal.Zero;

                        exprReqExpenses.Add(new proc_GetDepartmentRequestBudgetForAllocateResult()
                        {
                            YR = exprGroup.groupBy.YR,
                            PLAN_ID = exprGroup.groupBy.PLAN_ID,
                            PLAN_NAME = exprGroup.groupBy.PLAN_NAME,
                            PLAN_ORDER_SEQ = exprGroup.groupBy.PLAN_ORDER_SEQ,
                            PRODUCE_ID = exprGroup.groupBy.PRODUCE_ID,
                            PRODUCE_NAME = exprGroup.groupBy.PRODUCE_NAME,
                            PRODUCE_ORDER_SEQ = exprGroup.groupBy.PRODUCE_ORDER_SEQ,
                            ACTIVITY_ID = exprGroup.groupBy.ACTIVITY_ID,
                            ACTIVITY_NAME = exprGroup.groupBy.ACTIVITY_NAME,
                            ACTIVITY_SHORT_NAME = exprGroup.groupBy.ACTIVITY_SHORT_NAME,
                            ACTIVITY_ORDER_SEQ = exprGroup.groupBy.ACTIVITY_ORDER_SEQ,
                            BUDGET_TYPE_ID = exprGroup.groupBy.BUDGET_TYPE_ID,
                            BUDGET_TYPE_NAME = exprGroup.groupBy.BUDGET_TYPE_NAME,
                            BUDGET_TYPE_ORDER_SEQ = exprGroup.groupBy.BUDGET_TYPE_ORDER_SEQ,
                            EXPENSES_GROUP_ID = exprGroup.groupBy.EXPENSES_GROUP_ID,
                            EXPENSES_GROUP_NAME = exprGroup.groupBy.EXPENSES_GROUP_NAME,
                            EXPENSES_GROUP_ALLOCATE_GROUP_FLAG = exprGroup.groupBy.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG,
                            EXPENSES_GROUP_ORDER_SEQ = exprGroup.groupBy.EXPENSES_GROUP_ORDER_SEQ,
                            EXPENSES_ID = exprGroup.groupBy.EXPENSES_ID,
                            EXPENSES_NAME = exprGroup.groupBy.EXPENSES_NAME,
                            EXPENSES_ORDER_SEQ = exprGroup.groupBy.EXPENSES_ORDER_SEQ,
                            PROJECT_ID = exprGroup.groupBy.PROJECT_ID,
                            PROJECT_NAME = exprGroup.groupBy.PROJECT_NAME,
                            GLCODEs = exprGroup.groupBy.GLCODEs,

                            REQUIRED_ALLOCATE_TYPE = exprGroup.groupBy.REQUIRED_ALLOCATE_TYPE,
                            REQUIRED_REQUEST_TYPE = exprGroup.groupBy.REQUIRED_REQUEST_TYPE,

                            EXPENSES_ACTUAL_BUDGET_AMOUNT = decimal.Zero,
                            EXPENSES_REMAIN_BUDGET_AMOUNT = decimal.Zero,
                            EXPENSES_ACTUAL_OFF_BUDGET_AMOUNT = decimal.Zero,
                            EXPENSES_REMAIN_OFF_BUDGET_AMOUNT = decimal.Zero,

                            // จำนวนเงินที่เคยจัดสรรให้กับหน่วยงาน
                            DEP_ALLOCATE_EXPENSES_BUDGET_AMOUNT = depAllocateBudgetAmounts,
                            DEP_ALLOCATE_EXPENSES_OFF_BUDGET_AMOUNT = depAllocateOffBudgetAmounts,

                            DEP_ID = expr.DEP_ID,
                            DEP_NAME = expr.DEP_NAME,
                            DEP_CODE = expr.DEP_CODE,
                            DEP_SORT_INDEX = expr.DEP_SORT_INDEX,
                            AREA_ID = expr.AREA_ID,
                            AREA_NAME = expr.AREA_NAME,
                            REQ_ID = expr.REQ_ID
                        });
                    });
                }

                // สร้างแบบฟอร์มจัดสรรงบประมาณ เป็นไฟล์ Excel
                // สำหรับให้ผู้ใช้งานระบุ จำนวนเงิน งปม. ที่จะจัดสรรลงไปให้กับหน่วยงานภูมิภาค
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                res["filename"] = GenerateAllocateTemplateFile(exprReqExpenses, appSettings, userAuthorizeProfile, requestType, allocateType);
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// สร้างแบบฟอร์ม Excel File สำหรับจัดสรรงบประมาณ ให้กับหน่วยงานภูมิภาค
        /// 1. บางหมวดค่าใช้จ่าย จะจัดสรรเงินเป็นก้อนตามหมวดค่าใช้จ่าย เช่น ค่าสาธารณูปโภค เป็นต้น
        /// </summary>
        /// <param name="exprReqExpenses"></param>
        /// <param name="appSettings"></param>
        /// <param name="userAuthorizeProfile"></param>
        /// <param name="requestType">1 = คำขอต้นปี, 2 = คำขอเพิ่มเติม, null = จัดสรรเพิ่มเติมโดยไม่มีคำขอ</param>
        /// <param name="allocateType">1 = จัดสรรจากเงินงบประมาณ, 2 = จัดสรรจากเงินนอกงบประมาณ</param>
        /// <returns></returns>
        public string GenerateAllocateTemplateFile(List<proc_GetDepartmentRequestBudgetForAllocateResult> exprReqExpenses, AppSettingProperty appSettings, UserAuthorizeProperty userAuthorizeProfile, int? requestType, int allocateType)
        {
            if (null == exprReqExpenses || null == appSettings || null == userAuthorizeProfile)
                return "";

            string templateFile = string.Format("{0}/Allocate_Budget_To_Department_Template.xlsx", appSettings.ReportTemplatePath);
            string filename = string.Format("{0}_{1}_แบบฟอร์มจัดสรรงบประมาณ.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks.ToString());

            // จัดกลุ่มข้อมูล เพื่อเตรียมเขียนลง XLS 
            // 1. แยก Sheet ตามกิจกรรม
            // 2. ภายใน Sheet ประกอบด้วย หน่วยงาน งบรายจ่าย หมวดค่าใช้จ่าย และ ค่าใช้จ่าย
            var exprGroupByActivity = exprReqExpenses.OrderBy(e => e.ACTIVITY_ORDER_SEQ)
                .GroupBy(e => new { e.ACTIVITY_ID, e.ACTIVITY_NAME, e.ACTIVITY_SHORT_NAME })
                .Select(e => new
                {
                    GroupBy = e.Key,
                    Items = e
                }).ToList();
            using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
            {
                // สีเทา สำหรับรายการที่ไม่เคยจัดสรรมาก่อน
                Color firstTimeAllocateColor = ColorTranslator.FromHtml("#E9E9E9");
                Color moreTimeAllocateColor = ColorTranslator.FromHtml("#216021");

                // สร้าง Worksheet โดยแยกตามกิจกรรม
                foreach (var exprActivity in exprGroupByActivity)
                {
                    string sheetName = exprActivity.GroupBy.ACTIVITY_SHORT_NAME;
                    if (string.IsNullOrEmpty(sheetName))
                        sheetName = exprActivity.GroupBy.ACTIVITY_NAME.Substring(0, 20);

                    ExportUtils.CurrWorkSheet = xlsApp.Workbook.Worksheets.Copy("TEMPLATE_SHEET", sheetName);
                    double defaultRowHeight = 21.00;

                    // กำหนดคุณสมบัติของ Sheet
                    ExportUtils.CurrWorkSheet.View.ZoomScale = 80;
                    ExportUtils.CurrWorkSheet.View.ShowGridLines = false;
                    ExportUtils.CurrWorkSheet.Cells.Style.Font.Name = appSettings.ReportDefaultFontName;
                    //ExportUtils.CurrWorkSheet.Cells.Style.Font.Size = appSettings.ReportDefaultFontSize;
                    ExportUtils.CurrWorkSheet.Row(2).Height = defaultRowHeight;
                    ExportUtils.CurrWorkSheet.Row(3).Height = defaultRowHeight;
                    ExportUtils.CurrWorkSheet.Row(4).Height = defaultRowHeight;
                    ExportUtils.CurrWorkSheet.Row(5).Height = defaultRowHeight;
                    ExportUtils.CurrWorkSheet.Row(6).Height = defaultRowHeight;

                    var cell = ExportUtils.GetRange("A6:G6");
                    string allocateText = "ใช้เงินงบประมาณกรมสรรพสามิตในการจัดสรร";
                    if (allocateType.Equals(2))
                        allocateText = "ใช้เงินนอกงบประมาณกรมสรรพสามิตในการจัดสรร";
                    if (null == requestType)
                        allocateText = string.Format("{0} [จัดสรรเพิ่มเติม โดยไม่มีคำขอ]", allocateText);
                    else
                        allocateText = string.Format("{0} [{1}]", allocateText, requestType.Equals(1) ? "คำขอต้นปี" : "คำขอเพิ่มเติม");
                    cell.Value = allocateText;
                    //cell.Style.Font.Size = 16;

                    // Group ข้อมูลตาม งบรายจ่าย หมวดค่าใช้จ่าย รายการ คชจ.
                    var exprGroupByBudgetType = exprActivity.Items.GroupBy(e => new { e.BUDGET_TYPE_ID, e.BUDGET_TYPE_NAME })
                            .Select(e => new
                            {
                                GroupByBudgetType = e.Key,
                                ExpensesGroups = e.GroupBy(sub => new { sub.EXPENSES_GROUP_ID, sub.EXPENSES_GROUP_NAME, sub.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG })
                                    .Select(sub => new
                                    {
                                        GroupByExpensesGroup = sub.Key,
                                        ExpensesItems = sub.GroupBy(x => new { x.EXPENSES_ID, x.EXPENSES_NAME, x.GLCODEs, x.PROJECT_ID, x.PROJECT_NAME })
                                            .Select(x => new
                                            {
                                                GroupByExpensse = x.Key,
                                                Departments = x.OrderBy(x1 => x1.PLAN_ORDER_SEQ)
                                                    .ThenBy(x1 => x1.PRODUCE_ORDER_SEQ)
                                                    .ThenBy(x1 => x1.ACTIVITY_ORDER_SEQ)
                                                    .ThenBy(x1 => x1.DEP_SORT_INDEX)
                                                    .ThenBy(x1 => x1.REQ_ID).ToList()
                                            }).ToList()
                                    }).ToList()
                            }).ToList();

                    int columnIndex = 7; // H ค่าตำแหน่งคอลัมล์ที่ 7 ลบค่าออกไป 1 (-1) แล้ว เพราะจะใช้อ้างอิงตำแหน่งคอลัมล์ใน ExportUtils.ColumnsName
                    int rowIndex = 7;
                    string startCell = "";
                    string endCell = "";
                    foreach (var exprBudgetType in exprGroupByBudgetType)
                    {
                        int budgetTypeColumnMergeCount = 0;
                        int budgetTypeColumnIndex = columnIndex;
                        foreach (var exprExpensesGroup in exprBudgetType.ExpensesGroups)
                        {
                            int expensesGroupColumnMergeCount = 0;
                            int expensesGroupColumnIndex = columnIndex;
                            bool isFirstExpenses = true;
                            bool isAllocateByExpensesGroup = exprExpensesGroup.GroupByExpensesGroup.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG.HasValue
                                && exprExpensesGroup.GroupByExpensesGroup.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG.Value.Equals(1);
                            foreach (var exprExpenses in exprExpensesGroup.ExpensesItems)
                            {
                                //  เขียนคอลัมล์ รายการ คชจ.
                                rowIndex = 9;
                                string expensesText = "จัดสรรเป็นก้อน ตามหมวดค่าใช้จ่าย";// (รายการค่าใช้จ่ายทุกตัวจะถูกจัดสรรไปด้วย)";
                                if (!isAllocateByExpensesGroup)
                                {
                                    expensesText = exprExpenses.GroupByExpensse.EXPENSES_NAME;
                                    if (!string.IsNullOrEmpty(exprExpenses.GroupByExpensse.PROJECT_NAME))
                                        expensesText = string.Format("{0} ({1})", exprExpenses.GroupByExpensse.EXPENSES_NAME, exprExpenses.GroupByExpensse.PROJECT_NAME);
                                    if (!string.IsNullOrEmpty(exprExpenses.GroupByExpensse.GLCODEs))
                                        expensesText = string.Format("{0}  {1}", expensesText, exprExpenses.GroupByExpensse.GLCODEs);
                                }

                                ExportUtils.SetCaption(string.Format("{0}{1}", ExportUtils.ColumnsName[columnIndex], rowIndex), expensesText);
                                if (isAllocateByExpensesGroup)
                                    ExportUtils.SelectedExcelRange.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#FF0000"));
                                ExportUtils.CurrWorkSheet.Column(columnIndex + 1).Width = 13.33;

                                rowIndex++;
                                int sumColumnIndex = columnIndex + 2;
                                foreach (var department in exprExpenses.Departments)
                                {
                                    ExportUtils.CurrWorkSheet.Row(rowIndex).Height = defaultRowHeight;
                                    if (isFirstExpenses)
                                    {
                                        // ชื่อแผนงาน
                                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), department.PLAN_NAME, true);
                                        // ชื่อผลผลิต
                                        ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), department.PRODUCE_NAME, true);
                                        // ชื่อกิจกรรม
                                        ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), department.ACTIVITY_NAME, true);
                                        // ชื่อหน่วยงาน
                                        string departmentText = department.DEP_NAME;
                                        //if (requestType.Equals(2)) // คำขอเพิ่มเติม ให้แสดงเลขที่คำขอด้วย
                                        departmentText = string.Format("{0} [เลขที่คำขอ: {1}]", departmentText, department.REQ_ID);
                                        ExportUtils.SetCellTextVal(string.Format("D{0}", rowIndex), departmentText, true);
                                        // รหัสหน่วยรับ งปม. (รหัสหน่วยงานของ ลค.)
                                        ExportUtils.SetCellTextVal(string.Format("E{0}", rowIndex), department.DEP_CODE, true);
                                        // จัดสรรครั้งที่
                                        ExportUtils.SetCellTextVal(string.Format("F{0}", rowIndex), string.Empty, true);
                                        // เลขที่เอกสาร
                                        ExportUtils.SetCellTextVal(string.Format("G{0}", rowIndex), string.Empty, true);
                                    }

                                    // สร้างสูตรภาพรวมจัดสรรงบประมาณของหน่วยงาน ในแต่ละรายการค่าใช้จ่าย (แนวนอน)
                                    // เริ่มใช้ค่าจากคอลัมล์ H
                                    ExportUtils.SetCellFormulaVal(rowIndex, sumColumnIndex, string.Format("SUM(H{0}:{1}{0})", rowIndex, ExportUtils.ColumnsName[sumColumnIndex - 2]), true, ExportUtils.OddHtmlColorCode);
                                    ExportUtils.SelectedExcelRange.Style.Numberformat.Format = ExportUtils.CurrencyNumberFormat;

                                    // เขียน Cell สำหรับให้ผู้ใช้ระบุงบประมาณที่ต้องการจัดสรร
                                    string columnName = string.Format("{0}{1}", ExportUtils.ColumnsName[columnIndex], rowIndex);
                                    ExportUtils.SetCellCurrencyVal(columnName, decimal.Zero, true);
                                    ExportUtils.SelectedExcelRange.AddComment(AppUtils.ToJson(department), "");

                                    ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    decimal? allocatedAmounts = allocateType.Equals(1) ? department.DEP_ALLOCATE_EXPENSES_BUDGET_AMOUNT : department.DEP_ALLOCATE_EXPENSES_OFF_BUDGET_AMOUNT;
                                    if (department.REQ_BUDGET_AMOUNT == null) // ไม่ได้มาจากคำขอ
                                    {
                                        if (allocatedAmounts == null || allocatedAmounts.Value.CompareTo(decimal.Zero) == 0) // ไม่เคยจัดสรรมาก่อน (สีเทา)
                                            ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(firstTimeAllocateColor);
                                        else // มีการจัดสรรมาแล้ว (สีเขียว)
                                            ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(moreTimeAllocateColor);
                                    }
                                    else
                                    {
                                        if (allocatedAmounts == null || allocatedAmounts.Value.CompareTo(decimal.Zero) == 0)
                                            ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.None;
                                        else // มีการจัดสรรมาแล้ว (สีเขียว)
                                            ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(moreTimeAllocateColor);
                                    }

                                    rowIndex++;
                                }

                                // สร้างสูตรรวมภาพรวมการจัดสรรงบของรายการค่าใช้จ่าย ให้กับแต่ละหน่วยงาน (แนวตั้ง)
                                // เริ่มเขียนตั้งแต่คอลัมล์ H
                                ExportUtils.SetCellFormulaVal(rowIndex, columnIndex + 1, string.Format("SUM({0}10:{0}{1})", ExportUtils.ColumnsName[columnIndex], rowIndex - 1), true, ExportUtils.GroupHtmlColorCode);
                                ExportUtils.SelectedExcelRange.Style.Numberformat.Format = ExportUtils.CurrencyNumberFormat;

                                // สร้างสูตรภาพรวมจัดสรรงบประมาณของกิจกรรม
                                // เริ่มใช้ค่าจากคอลัมล์ H
                                ExportUtils.SetCellFormulaVal(rowIndex, sumColumnIndex, string.Format("SUM(H{0}:{1}{0})", rowIndex, ExportUtils.ColumnsName[sumColumnIndex - 2]), true, ExportUtils.OddHtmlColorCode);
                                ExportUtils.SelectedExcelRange.Style.Numberformat.Format = ExportUtils.CurrencyNumberFormat;

                                columnIndex++;
                                expensesGroupColumnMergeCount++;
                                budgetTypeColumnMergeCount++;
                                isFirstExpenses = false;

                                // กรณีจัดสรรงบประมาณตามหมวดค่าใช้จ่าย
                                // ให้เขียนคอลัมล์เดียวภายใต้หมวดค่าใช้จ่าย ชื่อว่า  "จัดสรรตามหมวดค่าใช้จ่าย"
                                if (isAllocateByExpensesGroup)
                                    break;
                            }

                            // เขียนคอลัมล์ หมวดค่าใช้จ่าย
                            rowIndex = 8;
                            startCell = string.Format("{0}{1}", ExportUtils.ColumnsName[expensesGroupColumnIndex], rowIndex);
                            endCell = string.Format("{0}{1}", ExportUtils.ColumnsName[expensesGroupColumnIndex + expensesGroupColumnMergeCount - 1], rowIndex);
                            ExportUtils.SetCaption(string.Format("{0}:{1}", startCell, endCell), exprExpensesGroup.GroupByExpensesGroup.EXPENSES_GROUP_NAME);
                        }

                        // เขียนคอลัมล์ งบรายจ่าย
                        rowIndex = 7;
                        startCell = string.Format("{0}{1}", ExportUtils.ColumnsName[budgetTypeColumnIndex], rowIndex);
                        endCell = string.Format("{0}{1}", ExportUtils.ColumnsName[budgetTypeColumnIndex + budgetTypeColumnMergeCount - 1], rowIndex);
                        ExportUtils.SetCaption(string.Format("{0}:{1}", startCell, endCell), exprBudgetType.GroupByBudgetType.BUDGET_TYPE_NAME);
                    }
                }

                // ซ่อน Worksheet ที่เป็น Template ออกแบบ
                xlsApp.Workbook.Worksheets["TEMPLATE_SHEET"].Hidden = eWorkSheetHidden.VeryHidden;
                string saveToFile = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
                xlsApp.SaveAs(new FileInfo(saveToFile));
            }

            return filename;
        }

        /// <summary>
        /// โหลดแบบฟอร์มสำหรับยืนยันการจัดสรรงบประมาณให้กับหน่วยงาน
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetVerifyModalForm()
        {
            return View();
        }

        /// <summary>
        /// แสดงภาพรวมของคำขอต้นปี
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetRequestStartYearSummary(short? fiscalYear)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() {
                { "signoffAmounts", null },
                { "signoffBudgetAmounts", null },

                { "unsignoffAmounts", null },
                { "unsignoffBudgetAmounts", null },

                { "noTransactionAmounts", null }
            };
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                if (null == fiscalYear)
                    fiscalYear = AppUtils.GetCurrYear();
                var expr = db.proc_GetTrackingBudgetRequestStartYear(fiscalYear.Value).ToList();
                if (expr.Any())
                {
                    // จำนวนที่ยืนยันยอดคำของบประมาณ (ยอดงบประมาณ, หน่วยงาน)
                    var exprSignOff = expr.Where(e => e.REQ_ID != null && e.PROCESS_STATUS != null && e.PROCESS_STATUS.Value.Equals(0) && e.SIGNOFF_FLAG != null && e.SIGNOFF_FLAG.Value == true);
                    res["signoffBudgetAmounts"] = exprSignOff.Sum(e => e.TOTAL_REQUEST_BUDGET);
                    res["signoffAmounts"] = exprSignOff.Count();

                    // จำนวนที่รอยืนยันคำของบประมาณ (ยอดงบประมาณ, หน่วยงาน)
                    var exprUnSignOff = expr.Where(e => e.REQ_ID != null && (e.SIGNOFF_FLAG == null || e.SIGNOFF_FLAG.Value == false));
                    res["unsignoffBudgetAmounts"] = exprUnSignOff.Sum(e => e.TOTAL_REQUEST_BUDGET);
                    res["unsignoffAmounts"] = exprUnSignOff.Count();

                    // จำนวนที่ยังไม่ทำคำขอ (หน่วยงาน)
                    res["noTransactionAmounts"] = expr.Where(e => e.REQ_ID == null).Count();
                }
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ลบไฟล์เอกสารที่อัพโหลดไว้แล้ว กรณีที่ผู้ใช้งานกดยกเลิก ไม่นำเข้า
        /// ข้อมูลจัดสรรงบประมาณ
        /// </summary>
        /// <param name="filename"></param>
        public void DeleteUploadFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return;

            var appSettings = AppSettingProperty.ParseXml();
            var fileUpload = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
            if (System.IO.File.Exists(fileUpload))
                System.IO.File.Delete(fileUpload);
        }


        /// <summary>
        /// ตรวจสอบฟอร์มจัดสรรงบประมาณ และ นำเข้าข้อมูล
        /// แบ่งหมวดการทำงานออกเป็น 2 ประเภทได้แก่
        /// 1. VERIFY   อ่านข้อมูลในไฟล์ XLS เพื่อสรุปข้อมูลการจัดสรรให้หน่วยงาน และนำไปแสดงบนหน้าจอเว็บ (ยังไม่มีการบันทึกใดๆลงระบบ)
        /// 2. COMMIT   อ่านข้อมูลในไฟล์ XLS และนำเข้าข้อมูลการจัดสรร (บันทึกลงระบบ)
        /// </summary>
        /// <param name="filename">ไฟล์ Template จัดสรรงบประมาณที่อัพโหลดไว้แล้ว</param>
        /// <param name="referDocNo">เลขที่อ้างอิงของเอกสาร ในการจัดสรรงบประมาณในแต่ล่ะครั้ง Validate ค่าเฉพาะ actionType = COMMIT</param>
        /// <param name="actionType">VERIFY = อ่านข้อมูลในไฟล์เพื่อสรุปข้อมูลการจัดสรรให้หน่วยงาน, COMMIT = อ่านข้อมูลในไฟล์และนำเข้าข้อมูลการจัดสรร</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult VerifyDocument(string filename, string actionType)
        {
            string periodCodeVal = "";
            Dictionary<string, object> res = new Dictionary<string, object>(4) {
                { "errors", null }, // บังคับให้ระบุ เลขที่เอกสาร
                { "errorText", null },
                { "uploadFilename", filename }, // ชื่อไฟล์ Template จัดสรรงบประมาณที่อัพโหลด
                { "filename", null }, // ผลลัพธ์การนำเข้าข้อมูลจัดสรรงบประมาณ
                { "periodCode", periodCodeVal }, // ครั้งที่จัดสรร
                { "verifyDepartments", null } // ข้อมูลสรุปยอดการจัดสรรเงินงบประมาณให้แต่ล่ะหน่วยงาน
            };

            // ตรวจสอบการผ่านค่า actionType
            if (!"VERIFY".Equals(actionType) && !"COMMIT".Equals(actionType))
            {
                res["errorText"] = "actionType ไม่ถูกต้อง";
                return Json(res, JsonRequestBehavior.DenyGet);
            }
            else if ("COMMIT".Equals(actionType))
            {
                // ตรวจสอบการระบุเลขที่อ้างอิงเอกสาร เฉพาะ actionType = COMMIT
                //if (string.IsNullOrEmpty(referDocNo))
                //{
                //    res["errors"] = new Dictionary<string, ModelValidateErrorProperty>(1)
                //    { { "ReferDocNo", new ModelValidateErrorProperty("ReferDocNo", new List<string>(1){ "โปรดระบุเลขที่เอกสารก่อน" })} };
                //    return Json(res, JsonRequestBehavior.DenyGet);
                //}
                //else if (Regex.Replace(referDocNo, @"[^\d]", "", RegexOptions.IgnoreCase).Length != 10)
                //{
                //    res["errors"] = new Dictionary<string, ModelValidateErrorProperty>(1)
                //    { { "ReferDocNo", new ModelValidateErrorProperty("ReferDocNo", new List<string>(1){ "ระบุเป็นตัวเลขและความยาว 10 ตัวอักษร" })} };
                //    return Json(res, JsonRequestBehavior.DenyGet);
                //}
            }

            // ตรวจสอบไฟล์ Template การจัดสรรเงินงบประมาณมีอยู่จริงหรือไม่
            var appSettings = AppSettingProperty.ParseXml();
            string uploadFile = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
            if (!System.IO.File.Exists(uploadFile))
            {
                res["errorText"] = "ไม่พบแบบฟอร์มจัดสรรงบประมาณที่ต้องการ จัดสรรให้กับหน่วยงานภูมิภาค";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            Dictionary<string, decimal> netBudgetBalanceByGroups = null;// งบประมาณในภาพรวมของแต่ละกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)
            List<AllocateBudgetDepartmentProperty> verifyDepartments = null;
            if ("VERIFY".Equals(actionType))
                verifyDepartments = new List<AllocateBudgetDepartmentProperty>();
            else
                netBudgetBalanceByGroups = new Dictionary<string, decimal>();

            var uploadFileinfo = new FileInfo(uploadFile);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                bool isRequiredSaveChange = false;
                bool isRequiredToRollback = false;

                // 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ
                // ในไฟล์ Template จะระบุประเภทของเงินงบประมาณที่จะจัดสรร ให้เปลี่ยนแปลงค่านี้ตามค่าที่อ่านได้จาก Template
                int allocateType = 1;

                // รายการคำขอเงินงบประมาณ ที่ได้รับจัดสรร
                // สำหรับนำไป ปรับปรุงสถานะของ คำขอ
                List<string> allocateRequestIds = new List<string>();

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                using (ExcelPackage xlsApp = new ExcelPackage(uploadFileinfo))
                {
                    int sheetIndex = 1;
                    Color errorBgColor = ColorTranslator.FromHtml("#990000");
                    Color warningColor = ColorTranslator.FromHtml("#FFC204");
                    Color successBgColor = ColorTranslator.FromHtml("#009900");
                    do // อ่านข้อมูลในแต่ละ Worksheet
                    {
                        ExportUtils.CurrWorkSheet = xlsApp.Workbook.Worksheets[sheetIndex];
                        sheetIndex++;
                        if (ExportUtils.CurrWorkSheet.Hidden == eWorkSheetHidden.Hidden)
                        {
                            if (sheetIndex > xlsApp.Workbook.Worksheets.Count)
                                break;
                            continue;
                        }

                        int rowIndex = 10;
                        do // อ่านข้อมูลแต่ละแถว
                        {
                            // ถ้าคอลัมล์ A ไม่มีค่าให้หยุดการอ่านข้อมูล
                            if (ExportUtils.GetCellByIndex(rowIndex, 1).Value == null)
                                break;

                            // เก็บ Cell ที่เป็นครั้งที่จัดสรรเอาไว้ สำหรับตรวจสอบ
                            // ให้ระบุค่าครั้งที่จัดสรร ในกรณีที่ในแถว (หน่วยงาน) มีการระบุจำนวนเงินที่จัดสรร
                            ExportUtils.GetCellByIndex(rowIndex, 6); // F ครั้งที่จัดสรร
                            var periodExcelRange = ExportUtils.SelectedExcelRange;
                            ExportUtils.GetCellByIndex(rowIndex, 7); // G เลขที่เอกสาร
                            var refDocExcelRange = ExportUtils.SelectedExcelRange;
                            string referDocNo = Regex.Replace(refDocExcelRange.Text, @"[^\d]", "", RegexOptions.IgnoreCase);

                            // รับค่าเฉพาะตัวเลข
                            // ง = เงินงบประมาณ, งน = เงินนอกงบประมาณให้ และจัดการตัวเลขให้อยู่ในรูปแบบ 3 หลัก
                            string allocatePeriodText = Regex.Replace(periodExcelRange.Text, @"[^\d]", "");

                            string commentText = "";
                            int columnIndex = 8; // H
                            do // อ่านข้อมูลแต่ละคอลัมล์
                            {
                                ExportUtils.GetCellByIndex(rowIndex, columnIndex);
                                if (ExportUtils.SelectedExcelRange.Value == null)
                                    break;

                                commentText = "";
                                if (null != ExportUtils.SelectedExcelRange.Comment)
                                {
                                    commentText = ExportUtils.SelectedExcelRange.Comment.Text;
                                    ExportUtils.CurrWorkSheet.Comments.Remove(ExportUtils.SelectedExcelRange.Comment);
                                }
                                columnIndex++;
                                if (string.IsNullOrEmpty(commentText))
                                {
                                    // ยกเว้น Cell ที่เป็นสูตรรวม ไม่ต้องถือว่าเป็น Error
                                    string formulaValue = ExportUtils.SelectedExcelRange.Formula;
                                    if (string.IsNullOrEmpty(formulaValue))
                                    {
                                        ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(warningColor);
                                        ExportUtils.SelectedExcelRange.AddComment("ไม่พบข้อมูลที่ระบบสร้างไว้", "");

                                        // ทำสีแดงของ Tab Sheet  เพื่อบอกว่ามีรายการที่ไม่สามารถนำเข้าฐานข้อมูลได้
                                        ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                    }

                                    continue;
                                }

                                // จำนวนเงินที่ระบุ สำหรับจัดสรร หรือ ดึงเงินคืน (-)
                                object allocateAmountsVal = ExportUtils.SelectedExcelRange.Value;
                                decimal allocateAmounts = decimal.Zero;
                                if (null != allocateAmountsVal)
                                {
                                    if (Regex.IsMatch(allocateAmountsVal.ToString(), @"[^\d\-]", RegexOptions.IgnoreCase))
                                    {
                                        ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(warningColor);
                                        ExportUtils.SelectedExcelRange.AddComment(string.Format("ระบุจำนวนเงินไม่ถูกต้อง {0}", allocateAmountsVal), "");

                                        // ทำสีแดงของ Tab Sheet  เพื่อบอกว่ามีรายการที่ไม่สามารถนำเข้าฐานข้อมูลได้
                                        ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                        continue;
                                    }
                                    decimal.TryParse(allocateAmountsVal.ToString(), out allocateAmounts);


                                    if (allocateAmounts.CompareTo(decimal.Zero) == 1)
                                    {
                                        // บังคับให้ระบุครั้งที่จัดสรร ในกรณีที่ระบุจำนวนเงิน
                                        if (string.IsNullOrEmpty(allocatePeriodText) && periodExcelRange.Comment == null)
                                        {
                                            isRequiredToRollback = true;
                                            periodExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                            periodExcelRange.Style.Fill.BackgroundColor.SetColor(errorBgColor);
                                            periodExcelRange.AddComment("ให้ระบุค่าเฉพาะตัวเลข เช่น จัดสรรครั้งที่ 1 ระบุค่า 1 ลงไป ระบบจะใส่ข้อมูล ง241/001, งน001 ให้", "");

                                            // ทำสีแดงของ Tab Sheet  เพื่อบอกว่ามีรายการที่ไม่สามารถนำเข้าฐานข้อมูลได้
                                            ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                        }

                                        // บังคับให้ระบุเลขที่เอกสาร ความยาว 10 หลัก ในกรณีที่ระบุจำนวนเงิน
                                        if (string.IsNullOrEmpty(referDocNo) && refDocExcelRange.Comment == null)
                                        {
                                            isRequiredToRollback = true;
                                            refDocExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                            refDocExcelRange.Style.Fill.BackgroundColor.SetColor(errorBgColor);
                                            refDocExcelRange.AddComment("ให้ระบุเลขที่เอกสารก่อน", "");

                                            // ทำสีแดงของ Tab Sheet  เพื่อบอกว่ามีรายการที่ไม่สามารถนำเข้าฐานข้อมูลได้
                                            ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                        }
                                        else if (referDocNo.Length != 10 && refDocExcelRange.Comment == null)
                                        {
                                            isRequiredToRollback = true;
                                            refDocExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                            refDocExcelRange.Style.Fill.BackgroundColor.SetColor(errorBgColor);
                                            refDocExcelRange.AddComment("ระบุเป็นตัวเลขและความยาว 10 หลัก", "");

                                            // ทำสีแดงของ Tab Sheet  เพื่อบอกว่ามีรายการที่ไม่สามารถนำเข้าฐานข้อมูลได้
                                            ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                        }
                                    }
                                }

                                // แปลงข้อมูลที่ระบบสร้างขึ้นใน Comment ของ Cell เพื่ออ้างอิงรายการจัดสรรลงไปให้หน่วยงานภายนอก
                                var exprAllocateInfo = AppUtils.ParseJson<proc_GetDepartmentRequestBudgetForAllocateResult>(commentText);
                                if (null == exprAllocateInfo)
                                {
                                    ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(warningColor);
                                    ExportUtils.SelectedExcelRange.AddComment(string.Format("โครงสร้างข้อมูล Json ที่ระบบสร้างไว้ไม่ถูกต้อง {0}", commentText), "");

                                    // ทำสีแดงของ Tab Sheet  เพื่อบอกว่ามีรายการที่ไม่สามารถนำเข้าฐานข้อมูลได้
                                    ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                    continue;
                                }

                                if (string.IsNullOrEmpty(periodCodeVal) && !string.IsNullOrEmpty(allocatePeriodText))
                                    periodCodeVal = BudgetUtils.FormatBudgetPeriod(exprAllocateInfo.REQUIRED_ALLOCATE_TYPE.Value, Convert.ToInt32(allocatePeriodText));// string.Format("{0}{1}", exprAllocateInfo.REQUIRED_ALLOCATE_TYPE.Value.Equals(1) ? "ง241/" : "งน", Convert.ToInt32(allocatePeriodText).ToString("000"));

                                // กรณีเป็นการส่งคำขอ ให้ตรวจสอบความสมบูรณ์ของคำขอก่อน
                                string reqId = null;
                                if (exprAllocateInfo.REQ_BUDGET_AMOUNT != null)
                                {
                                    reqId = exprAllocateInfo.REQ_ID;

                                    // เนื่องจากเป็นการอ่านข้อมูลใน Cell อาจจะมีบาง Cell ของ XLS
                                    // มีเลขที่คำขอซ้ำกับที่เคยตรวจสอบไปแล้ว และ สามารถนำไปทำรายการจัดสรรได้
                                    // ดังนั้นไม่ต้องตรวจสอบซ้ำ เพื่อลดการ Select ข้อมูลจาก DB
                                    if (allocateRequestIds.IndexOf(reqId) == -1)
                                    {
                                        var verifyReqErrorText = "";
                                        var exprReqBudget = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.REQ_ID.Equals(reqId)).Select(e => new
                                        {
                                            e.PROCESS_STATUS,
                                            e.ACTIVE,
                                            e.REQ_TYPE,
                                            e.SIGNOFF_FLAG
                                        }).FirstOrDefault();
                                        if (null == exprReqBudget)
                                            verifyReqErrorText = string.Format("ไม่พบคำของบประมาณ: {0}", reqId);
                                        else if (exprReqBudget.ACTIVE.Equals(-1))
                                            verifyReqErrorText = string.Format("คำขอถูกยกเลิกไปแล้ว: {0}", reqId);
                                        else if (exprReqBudget.PROCESS_STATUS.Equals(1))
                                            verifyReqErrorText = string.Format("คำขอถูกจัดสรรไปแล้ว: {0}", reqId);
                                        else if (exprReqBudget.PROCESS_STATUS.Equals(-1))
                                            verifyReqErrorText = string.Format("คำขอถูกปฏิเสธจัดสรรไปแล้ว: {0}", reqId);
                                        else if (exprReqBudget.REQ_TYPE.Equals(1) && !exprReqBudget.SIGNOFF_FLAG)
                                            verifyReqErrorText = string.Format("คำขอต้นปี ยังไม่ผ่านการ SignOff: {0}", reqId);
                                        if (!string.IsNullOrEmpty(verifyReqErrorText))
                                        {
                                            isRequiredToRollback = true;

                                            ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                            ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(warningColor);
                                            ExportUtils.SelectedExcelRange.AddComment(verifyReqErrorText, "");

                                            // ทำสีแดงของ Tab Sheet  เพื่อบอกว่ามีรายการที่ไม่สามารถนำเข้าฐานข้อมูลได้
                                            ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                            continue;
                                        }

                                        allocateRequestIds.Add(reqId);
                                    }
                                }


                                // กรณีจัดสรรเป็นศูนย์บาท ไม่ต้องลงรายการค่าใช้จ่ายให้หน่วยงาน และ ไม่ต้องปรับปรุงงบประมาณของกรมสรรพสามิต
                                if (allocateAmounts.CompareTo(decimal.Zero) == 0)
                                    continue;

                                // ให้บันทึกรายการจัดสรรงบประมาณเฉพาะ actionType = "COMMIT"
                                if ("COMMIT".Equals(actionType))
                                {
                                    allocateType = exprAllocateInfo.REQUIRED_ALLOCATE_TYPE.Value;

                                    // งบประมาณคงเหลือสุทธิ (เงินงบ/เงินนอก) สำหรับใช้ตรวจสอบกรณี งบรายจ่าย บางประเภท
                                    // จะใช้วิธีการถั่วเฉลี่ยเงินงบประมาณของรายการ คชจ. ภายใต้ตัวเอง แต่ไม่เกินภาพรวมภายใน งบรายจ่ายนั้นๆ
                                    string budgetBalanceGroupKey = string.Format("{0}_{1}_{2}"
                                            , exprAllocateInfo.PLAN_ID, exprAllocateInfo.PRODUCE_ID
                                            , exprAllocateInfo.BUDGET_TYPE_ID);
                                    if (!netBudgetBalanceByGroups.ContainsKey(budgetBalanceGroupKey))
                                        netBudgetBalanceByGroups.Add(budgetBalanceGroupKey, BudgetUtils.GetTotalActualBudgetOrOffBudgetBalanceByGroup(exprAllocateInfo.YR
                                                , exprAllocateInfo.PLAN_ID, exprAllocateInfo.PRODUCE_ID
                                                , exprAllocateInfo.BUDGET_TYPE_ID
                                                , exprAllocateInfo.REQUIRED_ALLOCATE_TYPE.Value));
                                    decimal netBudgetBalanceByGroup = netBudgetBalanceByGroups[budgetBalanceGroupKey];

                                    // ปรับปรุงยอดงบประมาณคงเหลือสุทธิ ของกรมสรรพสามิต
                                    var adjustmentResult = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprAllocateInfo.YR
                                            , exprAllocateInfo.PLAN_ID, exprAllocateInfo.PRODUCE_ID
                                            , exprAllocateInfo.ACTIVITY_ID, exprAllocateInfo.BUDGET_TYPE_ID
                                            , exprAllocateInfo.EXPENSES_GROUP_ID, exprAllocateInfo.EXPENSES_ID
                                            , exprAllocateInfo.PROJECT_ID, exprAllocateInfo.REQUIRED_ALLOCATE_TYPE.Value
                                            , allocateAmounts.CompareTo(decimal.Zero) == -1 ? BudgetUtils.ADJUSTMENT_CASHBACK : BudgetUtils.ADJUSTMENT_PAY
                                            , Math.Abs(allocateAmounts), ref netBudgetBalanceByGroup);
                                    netBudgetBalanceByGroups[budgetBalanceGroupKey] = netBudgetBalanceByGroup;
                                    if (!adjustmentResult.Completed)
                                    {
                                        ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(errorBgColor);
                                        ExportUtils.SelectedExcelRange.AddComment(adjustmentResult.CauseErrorMessage, "");
                                        isRequiredToRollback = true;

                                        // ทำสีแดงของ Tab Sheet กรณีไม่สามารถปรับปรุงยอดงบประมาณของส่วนกลางได้
                                        ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                        continue;
                                    }


                                    // จัดสรร หรือ ดึงเงินคืน จากหน่วยงานภายนอก
                                    adjustmentResult = BudgetUtils.AdjustmentDepartmentBudgetBalance(db
                                        , exprAllocateInfo.AREA_ID.Value, exprAllocateInfo.DEP_ID.Value, exprAllocateInfo.YR
                                        , exprAllocateInfo.PLAN_ID, exprAllocateInfo.PRODUCE_ID
                                        , exprAllocateInfo.ACTIVITY_ID, exprAllocateInfo.BUDGET_TYPE_ID
                                        , exprAllocateInfo.EXPENSES_GROUP_ID, exprAllocateInfo.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG.Value
                                        , exprAllocateInfo.EXPENSES_ID
                                        , exprAllocateInfo.PROJECT_ID
                                        , reqId
                                        , exprAllocateInfo.REQUIRED_REQUEST_TYPE.Value // 1 = คำขอต้นปี หรือ 2 = เพิ่มเติม, 0 = จัดสรรโดยไม่มีคำขอ
                                        , exprAllocateInfo.REQUIRED_ALLOCATE_TYPE.Value // 1 = จัดสรรจาก เงินงบ หรือ 2 = เงินนอกงบ
                                        , allocateAmounts.CompareTo(decimal.Zero) == -1 ? BudgetUtils.ADJUSTMENT_CASHBACK : BudgetUtils.ADJUSTMENT_ALLOCATE
                                        , allocateAmounts, allocatePeriodText, referDocNo, userAuthorizeProfile);
                                    if (!adjustmentResult.Completed)
                                    {
                                        ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                        ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(errorBgColor);
                                        ExportUtils.SelectedExcelRange.AddComment(adjustmentResult.CauseErrorMessage, "");
                                        isRequiredToRollback = true;

                                        // ทำสีแดงของ Tab Sheet กรณีไม่สามารถปรับปรุงเงินงบประมาณของหน่วยงานภูมิภาคได้
                                        ExportUtils.CurrWorkSheet.TabColor = errorBgColor;
                                        continue;
                                    }

                                    // แสดงสีเขียว กรณีตัดเงินส่วนกลาง และจัดสรรให้หน่วยงานภายนอก สำเร็จ
                                    ExportUtils.SelectedExcelRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    ExportUtils.SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(successBgColor);
                                    isRequiredSaveChange = true;
                                }
                                else
                                {
                                    // สรุปเงินงบประมาณที่จัดสรรให้กับหน่วยงานภูมิภาค
                                    var exprAllocateItem = verifyDepartments.Where(e => e.DEP_ID.Equals(exprAllocateInfo.DEP_ID.Value)).FirstOrDefault();
                                    //&& e.PLAN_ID.Equals(exprAllocateInfo.PLAN_ID)
                                    //&& e.PRODUCE_ID.Equals(exprAllocateInfo.PRODUCE_ID)
                                    //&& e.ACTIVITY_ID.Equals(exprAllocateInfo.ACTIVITY_ID)
                                    //&& e.BUDGET_TYPE_ID.Equals(exprAllocateInfo.BUDGET_TYPE_ID)
                                    //&& e.EXPENSES_GROUP_ID.Equals(exprAllocateInfo.EXPENSES_GROUP_ID)
                                    //&& e.EXPENSES_ID.Equals(exprAllocateInfo.EXPENSES_ID)
                                    //&& e.PROJECT_ID == exprAllocateInfo.PROJECT_ID).FirstOrDefault();
                                    if (null == exprAllocateItem)
                                    {
                                        exprAllocateItem = new AllocateBudgetDepartmentProperty()
                                        {
                                            DEP_ID = exprAllocateInfo.DEP_ID.Value,
                                            DEP_NAME = exprAllocateInfo.DEP_NAME,
                                            AREA_NAME = exprAllocateInfo.AREA_NAME,
                                            //PLAN_ID = exprAllocateInfo.PLAN_ID,
                                            //PLAN_NAME = exprAllocateInfo.PLAN_NAME,
                                            //PRODUCE_ID = exprAllocateInfo.PRODUCE_ID,
                                            //PRODUCE_NAME = exprAllocateInfo.PRODUCE_NAME,
                                            //ACTIVITY_ID = exprAllocateInfo.ACTIVITY_ID,
                                            //ACTIVITY_NAME = exprAllocateInfo.ACTIVITY_NAME,
                                            //BUDGET_TYPE_ID = exprAllocateInfo.BUDGET_TYPE_ID,
                                            //BUDGET_TYPE_NAME = exprAllocateInfo.BUDGET_TYPE_NAME,
                                            //EXPENSES_GROUP_ID = exprAllocateInfo.EXPENSES_GROUP_ID,
                                            //EXPENSES_GROUP_NAME = exprAllocateInfo.EXPENSES_GROUP_NAME,
                                            //EXPENSES_ID = exprAllocateInfo.EXPENSES_ID,
                                            //EXPENSES_NAME = exprAllocateInfo.EXPENSES_NAME,
                                            //PROJECT_ID = exprAllocateInfo.PROJECT_ID,
                                            //PROJECT_NAME = exprAllocateInfo.PROJECT_NAME,
                                            AllocateAmounts = decimal.Zero,
                                            CashbackAmounts = decimal.Zero,
                                            REQ_ID = reqId
                                        };
                                        verifyDepartments.Add(exprAllocateItem);
                                    }

                                    if (allocateAmounts.CompareTo(decimal.Zero) == 1)
                                        exprAllocateItem.AllocateAmounts = exprAllocateItem.AllocateAmounts.Value + allocateAmounts;
                                    else
                                        exprAllocateItem.CashbackAmounts = exprAllocateItem.CashbackAmounts.Value + Math.Abs(allocateAmounts);
                                }
                            } while (true); // column

                            rowIndex++;
                        } while (true); // row


                        if (sheetIndex > xlsApp.Workbook.Worksheets.Count)
                            break;
                    } while (true); // WorkSheet


                    res["periodCode"] = periodCodeVal;
                    res["filename"] = string.Format("ผลการประมวลผลแบบฟอร์มจัดสรรงบประมาณ_{0}_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks.ToString());
                    string saveToFile = string.Format("{0}/{1}", appSettings.TemporaryPath, res["filename"]);
                    if ("COMMIT".Equals(actionType))
                    {
                        // บันทึกผลลัพธ์ของการประมวลผลไฟล์ แบบฟอร์มจัดสรร
                        xlsApp.SaveAs(new FileInfo(saveToFile));
                        // หลังจากอ่านข้อมูลในไฟล์แบบฟอร์มจัดสรรงบประมาณเสร็จให้ลบไฟล์ทิ้ง
                        uploadFileinfo.Delete();
                    }
                    else
                    {
                        // บันทึกผลลัพธ์ของการประมวลผลไฟล์ กรณีที่ Verify ไม่ผ่าน
                        if (isRequiredToRollback)
                        {
                            xlsApp.SaveAs(new FileInfo(saveToFile));
                            uploadFileinfo.Delete();
                        }

                        res["verifyDepartments"] = verifyDepartments.OrderBy(e => e.AREA_NAME).ThenBy(e => e.DEP_NAME);
                    }
                }


                //if (isRequiredToRollback) // ยกเลิกการทำรายการทั้งหมด
                //db.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, db.GetChangeSet().Updates);
                //else if (isRequiredSaveChange) 
                //   db.SubmitChanges();
                if (!isRequiredToRollback && isRequiredSaveChange)
                {
                    // ปรัปปรุงสถานะของคำขอเงินงบประมาณ
                    allocateRequestIds.ForEach(reqId =>
                    {
                        var exprReqBudgetMas = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.ACTIVE.Equals(1) && e.PROCESS_STATUS.Equals(0) && e.REQ_ID.Equals(reqId)).FirstOrDefault();
                        if (null != exprReqBudgetMas)
                        {
                            if (allocateType.Equals(1))
                                exprReqBudgetMas.BUDGET_ALLOCATE_FLAG = true;
                            else if (allocateType.Equals(2))
                                exprReqBudgetMas.OFF_BUDGET_ALLOCATE_FLAG = true;

                            if (exprReqBudgetMas.BUDGET_ALLOCATE_FLAG == true && exprReqBudgetMas.OFF_BUDGET_ALLOCATE_FLAG == true)
                                exprReqBudgetMas.PROCESS_STATUS = 1;

                            exprReqBudgetMas.APPROVE_DATETIME = DateTime.Now;
                            exprReqBudgetMas.APPROVE_ID = userAuthorizeProfile.EmpId;
                        }
                    });

                    db.SubmitChanges();
                }
                else if (isRequiredToRollback)
                    res["errorText"] = "งบประมาณที่จัดสรรในบางรายการค่าใช้จ่าย ไม่สามารถจัดสรรได้ โปรดตรวจสอบไฟล์ที่ดาวน์โหลด";
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        ///// <summary>
        ///// ค้นหาหน่วยงาน ที่ทำคำขอต้นปีและ SignOff แล้วและยังไม่จัดสรร
        ///// มาให้หน่วยงานกลาง จัดสรรงบประมาณ
        ///// 1. ใช้ข้อมูลรายการ คชจ. และ โครงการ ของกรมสรรพสามิต เป็นหลัก
        ///// 2. ค้นหาคำขอต้นปีของแต่ละหน่วยงาน และ นำมา Map กับแต่ละรายการ คชจ. และ โครงการ
        ///// 3. นำไป Render แบบฟอร์มให้หน้าเว็บเพื่อให้ ระบุยอดจัดสรร
        ///// </summary>
        ///// <param name="budgetType">1 = เงินงบประมาณ, 2 = นอกงบประมาณ</param>
        ///// <returns></returns>
        //[HttpPost]
        //public ActionResult RetrieveDepartmentForAllocate(int budgetType)
        //{
        //    using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
        //    {
        //        var fiscalYear = AppUtils.GetCurrYear();
        //        var expr = db.proc_GetDepartmentRequestBudgetStartYearForAllocate(fiscalYear, budgetType).ToList();

        //        // จัดเรียงข้อมูล
        //        var exprOrderBy = expr.OrderBy(e => e.PLAN_ID)
        //            .ThenBy(e => e.PRODUCE_ID)
        //            .ThenBy(e => e.ACTIVITY_ID)
        //            .ThenBy(e => e.BUDGET_TYPE_ID)
        //            .ThenBy(e => e.EXPENSES_GROUP_ID)
        //            .ThenBy(e => e.EXPENSES_ID)
        //            .ThenBy(e => e.DEP_ID).ToList();

        //        // รายชื่อหน่วยงานที่รอการจัดสรร
        //        // ไม่มีหน่วยงานที่รอจัดสรร งบต้นปีให้ตอบกลับ โดยไม่ต้องำอะไรต่อ
        //        var exprDepartments = exprOrderBy.Where(e => e.DEP_NAME != null)
        //            .GroupBy(e => new { e.DEP_ID, e.DEP_NAME }).Select(e => new
        //            {
        //                e.Key.DEP_ID,
        //                e.Key.DEP_NAME
        //            }).ToList();
        //        if (exprDepartments.Count == 0)
        //        {
        //            return Json(new Dictionary<string, object>(2) {
        //                { "expenses", null },
        //                { "departments", exprDepartments }
        //            }, JsonRequestBehavior.DenyGet);
        //        }

        //        // จัดรูปแบบโครงสร้างข้อมูล
        //        var exprData = exprOrderBy.GroupBy(e => new
        //        {
        //            e.PLAN_ID,
        //            e.PLAN_NAME,
        //            e.PRODUCE_ID,
        //            e.PRODUCE_NAME,
        //            e.ACTIVITY_ID,
        //            e.ACTIVITY_NAME,
        //            e.BUDGET_TYPE_ID,
        //            e.BUDGET_TYPE_NAME,
        //            e.EXPENSES_GROUP_ID,
        //            e.EXPENSES_GROUP_NAME
        //        }).Select(e => new
        //        {
        //            GroupBy = e.Key,
        //            Expenses = e.GroupBy(x => new { x.EXPENSES_ID, x.EXPENSES_NAME, x.PROJECT_ID, x.PROJECT_NAME }).Select(final => new
        //            {
        //                GroupBy = final.Key,
        //                Departments = final
        //            }).ToList()
        //        }).ToList();

        //        // ตอบกลับค่า
        //        return Json(new Dictionary<string, object>(2) {
        //            { "expenses", exprData },
        //            { "departments", exprDepartments }
        //        }, JsonRequestBehavior.DenyGet);
        //    }
        //}


        //[HttpPost]
        //public ActionResult SubmitSave(AllocateBudgetGroupByDepartmentFormMapper model)
        //{
        //    Dictionary<string, object> res = new Dictionary<string, object>(2)
        //    {
        //        { "errors", null },
        //        { "errorText", null }
        //    };


        //    using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
        //    {
        //        // ปีงบประมาณที่จัดสรร
        //        var fiscalYear = AppUtils.GetCurrYear();

        //        // ข้อมูลงบประมาณในภาพรวมแต่ละกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)
        //        Dictionary<string, decimal> actualBudgetByGroups = new Dictionary<string, decimal>();
        //        // ตรวจสอบงบประมาณคงคลัง ในแต่ละ คชจ. หรือ โครงการเพียงพอจัดสรรหรือไม่
        //        Dictionary<string, ModelValidateErrorProperty> expensesErrors = new Dictionary<string, ModelValidateErrorProperty>();

        //        foreach(AllocateBudgetDepartentProperty department in model.Departments)
        //        {
        //            string groupName = string.Format("{0}_{1}_{2}_{3}_{4}"
        //                    , department.PLAN_ID, department.PRODUCE_ID
        //                    , department.ACTIVITY_ID, department.BUDGET_TYPE_ID
        //                    , department.EXPENSES_GROUP_ID);
        //            string errorKey = string.Format("{0}_{1}_{2}_{3}"
        //                    , groupName, department.EXPENSES_ID
        //                    , department.PROJECT_ID, department.DEP_ID);

        //            // งบประมาณภาพรวมในแต่ละกลุ่ม
        //            if (!actualBudgetByGroups.ContainsKey(groupName))
        //                actualBudgetByGroups[groupName] = BudgetUtils.GetTotalActualBudgetOrOffBudgetBalanceByGroup(fiscalYear
        //                    , department.PLAN_ID, department.PRODUCE_ID
        //                    , department.ACTIVITY_ID, department.BUDGET_TYPE_ID
        //                    , department.EXPENSES_GROUP_ID, model.BudgetType);
        //            var netCurrActualBudgetGroup = actualBudgetByGroups[groupName];




        //            // ปรับปรุงเงินงบประมาณในแต่ละกลุ่ม หลังจากคำนวณการใช้จ่าย ณ ปัจจุบันแล้ว
        //            // เพื่อนำยอดคงเหลือ ไปตรวจสอบ
        //            actualBudgetByGroups[groupName] = netCurrActualBudgetGroup;
        //        }
        //    }

        //    return Json(res, JsonRequestBehavior.DenyGet);
        //}

        //public class AllocateBudgetGroupByDepartmentFormMapper
        //{
        //    /// <summary>
        //    /// ปีที่จัดสรรงบประมาณ
        //    /// </summary>
        //    public int FiscalYear { get; set; }

        //    /// <summary>
        //    /// เงินที่จัดสรร
        //    /// 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ
        //    /// </summary>
        //    public int BudgetType { get; set; }

        //    /// <summary>
        //    /// รายชื่อหน่วยงานที่จัดสรรงบประมาณ ในแต่ละกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย รายการค่าใช้จ่าย โครงการ)
        //    /// </summary>
        //    public List<AllocateBudgetDepartentProperty> Departments { get; set; }
        //}

        public class AllocateBudgetDepartmentProperty
        {
            /// <summary>
            /// เลขที่คำขอ งบประมาณต้นปีของหน่วยงาน
            /// </summary>
            public string REQ_ID { get; set; }

            /// <summary>
            /// เขตพื้นที่ ที่หน่วยงานสังกัด
            /// </summary>
            public string AREA_NAME { get; set; }

            /// <summary>
            /// รหัสหน่วยงาน
            /// </summary>
            public int DEP_ID { get; set; }

            /// <summary>
            /// ชื่อหน่วยงาน
            /// </summary>
            public string DEP_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับ แผนงาน
            /// </summary>
            public int? PLAN_ID { get; set; }
            public string PLAN_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับ ผลผลิต
            /// </summary>
            public int? PRODUCE_ID { get; set; }
            public string PRODUCE_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับ กิจกรรม
            /// </summary>
            public int? ACTIVITY_ID { get; set; }
            public string ACTIVITY_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับ งบรายจ่าย
            /// </summary>
            public int BUDGET_TYPE_ID { get; set; }
            public string BUDGET_TYPE_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับ หมวดค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_GROUP_ID { get; set; }
            public string EXPENSES_GROUP_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับ รายการค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_ID { get; set; }
            public string EXPENSES_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับ โครงการ
            /// </summary>
            public int? PROJECT_ID { get; set; }
            public string PROJECT_NAME { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณที่จัดสรรให้หน่วยงาน ในแต่ละ คชจ. หรือ โครงการ
            /// </summary>
            public decimal? AllocateAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณที่ดึงคืนจากหน่วยงาน
            /// </summary>
            public decimal? CashbackAmounts { get; set; }
        }
    }
}