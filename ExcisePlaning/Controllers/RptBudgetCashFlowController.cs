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
    /// รายงานสรุปกระแสเงินงบประมาณของกรมสรรพสามิต ได้แก่ รับเงินประจำงวด จัดสรรงบประมาณให้หน่วยงานภูมิภาค กันเงินงบประมาณและเบิกจ่าย
    /// 
    /// Template: Report005_BudgetCashFlow_Template.xlsx
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptBudgetCashFlowController : Controller
    {
        // GET: RptBudgetCashFlow
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_REPORT_BUDGET_CASH_FLOW);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_REPORT_BUDGET_CASH_FLOW;
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
        /// ทะเบียนคุมเงินงบประมาณ ส่งออกไปยัง Excel
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="returnType">RETRIEVE = แสดงข้อมูลบนหน้า Grid, EXPORT = ส่งออกไปยัง Excel</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int budgetType, string returnType, int? pageIndex, int? pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprBudgetCashFlow = db.V_GET_BUDGET_CASH_FLOW_STATEMENTs.Where(e => e.YR.Equals(fiscalYear));

                if (null != planId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.EXPENSES_ID.Equals(expensesId));

                // จัดกลุ่มข้อมูล งบรายจ่าย เพื่อสร้างเป็น Sheet Name
                // ตามด้วย แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                var finalExprBudgetCashFlow = exprBudgetCashFlow.GroupBy(e => new
                {
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.BUDGET_TYPE_ORDER_SEQ
                }).OrderBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .Select(x => new
                {
                    BudgetTypeItem = x.Key,
                    Values = x.GroupBy(e => new
                    {
                        e.PLAN_ID,
                        e.PLAN_NAME,
                        e.PLAN_ORDER_SEQ,
                        e.PRODUCE_ID,
                        e.PRODUCE_NAME,
                        e.PRODUCE_ORDER_SEQ,
                        e.ACTIVITY_ID,
                        e.ACTIVITY_NAME,
                        e.ACTIVITY_SHORT_NAME,
                        e.ACTIVITY_ORDER_SEQ,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.BUDGET_TYPE_ORDER_SEQ,
                    }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                    .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                    .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                    .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                    .Select(e => new
                    {
                        GroupBy = e.Key,
                        Rows = e
                    })
                });

                // นำข้อมูลส่งออกไปยังโปรแกรม XLS
                Dictionary<string, string> res = new Dictionary<string, string>(2) {
                        { "errorText", null },
                        { "filename", "" }
                    };

                if (finalExprBudgetCashFlow.Count() == 0)
                {
                    res["errorText"] = "ไม่พบข้อมูล โปรดตรวจสอบเงื่อนไขการค้นหา";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var appSettings = AppSettingProperty.ParseXml();
                string templateFile = string.Format("{0}/Report005_BudgetCashFlow_Template.xlsx", appSettings.ReportTemplatePath);
                string filename = string.Format("{0}_ทะเบียนคุมเงินงบประมาณ_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks);

                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
                {
                    Color redColor = ColorTranslator.FromHtml("#FF0000");
                    Color blueColor = ColorTranslator.FromHtml("#1E00B3");
                    Color softPinkColor = ColorTranslator.FromHtml("#FDE9D9");
                    Color redPigColor = ColorTranslator.FromHtml("#A70900");
                    short defaultShortVal = 0;
                    string yearStr = (fiscalYear + 543).ToString();

                    // แยกออกเป็นแต่ละ Sheet ตามงบรายจ่าย
                    foreach (var budget in finalExprBudgetCashFlow)
                    {
                        ExportUtils.CurrWorkSheet = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", budget.BudgetTypeItem.BUDGET_TYPE_NAME);

                        string reportTitle = ExportUtils.CurrWorkSheet.Cells["A1"].Text;
                        ExportUtils.CurrWorkSheet.Cells["A1"].Value = reportTitle.Replace("[var_fiscal_year]", yearStr);

                        string exportDateText = ExportUtils.CurrWorkSheet.Cells["I2"].Text;
                        string exportDateVal = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                        ExportUtils.CurrWorkSheet.Cells["I2"].Value = exportDateText.Replace("[var_export_date]", exportDateVal);

                        ExportUtils.CurrWorkSheet.Row(3).Hidden = true;
                        ExportUtils.CurrWorkSheet.Row(4).Hidden = true;

                        // ข้อมูลภายใต้หมวด แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                        var rowIndex = 5;
                        foreach (var group in budget.Values)
                        {

                            // เขียนส่วนกลุ่มข้อมูล แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.PLAN_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.PRODUCE_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.ACTIVITY_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.BUDGET_TYPE_NAME, false, "", true);

                            // เขียนส่วนคอลัมล์
                            ExportUtils.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "วัน/เดือน/ปี");
                            ExportUtils.SetCaption(string.Format("B{0}:B{1}", rowIndex, rowIndex + 1), "เลขที่เอกสาร");
                            ExportUtils.SetCaption(string.Format("C{0}:C{1}", rowIndex, rowIndex + 1), "รายการ");
                            ExportUtils.SetCaption(string.Format("D{0}:F{0}", rowIndex), "งบประมาณรายจ่าย (บาท)");
                            ExportUtils.SetCaption(string.Format("D{0}", rowIndex + 1), "เพิ่ม");
                            ExportUtils.SetCaption(string.Format("E{0}", rowIndex + 1), "ลด");
                            ExportUtils.SetCaption(string.Format("F{0}", rowIndex + 1), "คงเหลือ");
                            ExportUtils.SetCaption(string.Format("G{0}:K{0}", rowIndex), "เงินประจำงวด (บาท)");
                            ExportUtils.SetCaption(string.Format("G{0}", rowIndex + 1), "เพิ่ม");
                            ExportUtils.SetCaption(string.Format("H{0}", rowIndex + 1), "ผูกพัน");
                            ExportUtils.SetCaption(string.Format("I{0}", rowIndex + 1), "กันไว้เบิก");
                            ExportUtils.SetCaption(string.Format("J{0}", rowIndex + 1), "เบิก");
                            ExportUtils.SetCaption(string.Format("K{0}", rowIndex + 1), "คงเหลือ");
                            rowIndex += 2;

                            // เขียนจำนวนเงินที่รัฐบาลจะจัดสรรให้
                            var exprTmp = group.Rows.GroupBy(e => new
                            {
                                e.PLAN_ID,
                                e.PRODUCE_ID,
                                e.ACTIVITY_ID,
                                e.BUDGET_TYPE_ID,
                                e.EXPENSES_GROUP_ID,
                                e.EXPENSES_ID,
                                e.ESTIMATE_BUDGET_AMOUNT,
                                e.ESTIMATE_OFF_BUDGET_AMOUNT
                            }).ToList();
                            //decimal estimateBudgetAmount = exprTmp.Sum(e => e.Key.ESTIMATE_BUDGET_AMOUNT);
                            //decimal estimateOffBudgetAmount = exprTmp.Sum(e => e.Key.ESTIMATE_OFF_BUDGET_AMOUNT);
                            //decimal estimateBudgetVal = budgetType.Equals(1) ? estimateBudgetAmount : estimateOffBudgetAmount;
                            //decimal estimateBudgetBalance = estimateBudgetVal;
                            decimal estimateBudgetBalance = exprTmp.Sum(e => e.Key.ESTIMATE_BUDGET_AMOUNT);

                            ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), string.Format("รับจัดสรรงบประมาณรายจ่ายประจำปี {0}", yearStr), true, "", true);
                            ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), estimateBudgetBalance, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), estimateBudgetBalance, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), 0, true);
                            ExcelRange range = ExportUtils.GetRange(string.Format("A{0}:K{0}", rowIndex));
                            range.Style.Font.Bold = true;
                            rowIndex++;

                            decimal budgetBalance = decimal.Zero; // เงินประจำงวดที่ได้รับ เพื่อนำไปคำนวณยอดคงเหลือ
                            decimal reserveBudgetBalance = decimal.Zero; // จำนวนที่กันเงิน ในใบกัน เพื่อนำไปลดยอดจากการเบิกจ่าย

                            // ข้อมูล group.Rows จะอยู่ภายใต้ แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                            // นำผลลัพธ์ที่ได้มาจัดเรียงลำดับใหม่ ตามวันที่เกิดเหตุการณ์ต่างๆ
                            // เริ่มจาก เงินประจำงวด, จัดสรรให้หน่วยงานภูมิภาค/กันเงิน

                            // เงินประจำงวด
                            var exprIncome = group.Rows.Where(e => null != e.INCOME_BUDGET_AMOUNT).GroupBy(e => new
                            {
                                e.PLAN_ID,
                                e.PRODUCE_ID,
                                e.ACTIVITY_ID,
                                e.BUDGET_TYPE_ID,
                                e.EXPENSES_GROUP_ID,
                                e.EXPENSES_ID,
                                e.INCOME_PERIOD_MN,
                                e.INCOME_PERIOD_YR,
                                e.INCOME_REFER_DOC_NO,
                                e.INCOME_DATETIME,
                                e.INCOME_BUDGET_AMOUNT
                            })
                            .Select(e => e.Key)
                            .GroupBy(e => new { e.INCOME_PERIOD_MN, e.INCOME_PERIOD_YR, e.INCOME_REFER_DOC_NO, e.INCOME_DATETIME })
                            .AsEnumerable().Select(e => new
                            {
                                DATA_TYPE = "INCOME",
                                CREATED_DATETIME = e.Key.INCOME_DATETIME.Value,
                                REFER_DOC_NO = e.Key.INCOME_REFER_DOC_NO,
                                REMARK_TEXT = string.Format("เงินประจำงวด - งวดที่ {0}/{1}", e.Key.INCOME_PERIOD_MN, e.Key.INCOME_PERIOD_YR + 543),
                                BUDGET_AMOUNT = e.Sum(x => null == x.INCOME_BUDGET_AMOUNT ? decimal.Zero : x.INCOME_BUDGET_AMOUNT.Value),
                                CASHBACK_AMOUNT = decimal.Zero,
                                RESERVE_TYPE = defaultShortVal,
                                WITHDRAWAL_DATETIME = DateTime.MinValue,
                                WITHDRAWAL_CODE = "",
                                WITHDRAWAL_SEQ_NO = defaultShortVal
                            });

                            // จัดสรรให้หน่วยงานภูมิภาค
                            var exprAllocate = group.Rows.Where(e => null != e.ALLOCATE_BUDGET_AMOUNT && e.ALLOCATE_BUDGET_AMOUNT.Value > 0).GroupBy(e => new
                            {
                                e.PLAN_ID,
                                e.PRODUCE_ID,
                                e.ACTIVITY_ID,
                                e.BUDGET_TYPE_ID,
                                e.EXPENSES_GROUP_ID,
                                e.EXPENSES_ID,
                                e.ALLOCATE_PERIOD_CODE,
                                e.ALLOCATE_REFER_DOC_NO,
                                e.ALLOCATE_DATETIME,
                                e.ALLOCATE_BUDGET_AMOUNT,
                                e.ALLOCATE_CASHBACK_BUDGET_AMOUNT
                            })
                            .Select(e => e.Key)
                            .GroupBy(e => new { e.ALLOCATE_PERIOD_CODE, e.ALLOCATE_REFER_DOC_NO, e.ALLOCATE_DATETIME })
                            .AsEnumerable().Select(e => new
                            {
                                DATA_TYPE = "ALLOCATE",
                                CREATED_DATETIME = e.Key.ALLOCATE_DATETIME.Value,
                                REFER_DOC_NO = e.Key.ALLOCATE_REFER_DOC_NO,
                                REMARK_TEXT = string.Format("จัดสรรงบประมาณ - งวดที่ {0}", e.Key.ALLOCATE_PERIOD_CODE),
                                BUDGET_AMOUNT = e.Sum(x => null == x.ALLOCATE_BUDGET_AMOUNT ? decimal.Zero : x.ALLOCATE_BUDGET_AMOUNT.Value),
                                CASHBACK_AMOUNT = e.Sum(x => null == x.ALLOCATE_CASHBACK_BUDGET_AMOUNT ? decimal.Zero : x.ALLOCATE_CASHBACK_BUDGET_AMOUNT.Value),
                                RESERVE_TYPE = defaultShortVal,
                                WITHDRAWAL_DATETIME = DateTime.MinValue,
                                WITHDRAWAL_CODE = "",
                                WITHDRAWAL_SEQ_NO = defaultShortVal
                            });

                            // กันเงินงบประมาณ
                            var exprAllReserve = group.Rows.Where(e => e.RESERVE_BUDGET_TYPE != null && e.RESERVE_BUDGET_TYPE.Value.Equals(1)).AsEnumerable();
                            var exprReserve = exprAllReserve.Select(e => new
                            {
                                DATA_TYPE = "RESERVE",
                                CREATED_DATETIME = e.RESERVE_DATE.Value,
                                REFER_DOC_NO = e.RESERVE_ID,
                                REMARK_TEXT = e.RESERVE_REMARK_TEXT, //string.Format("กันเงินงบประมาณ - เลขที่กันเงิน {0}", e.RESERVE_ID),
                                BUDGET_AMOUNT = e.RESERVE_BUDGET_AMOUNT.Value,
                                CASHBACK_AMOUNT = decimal.Zero,
                                RESERVE_TYPE = e.RESERVE_TYPE.Value,
                                WITHDRAWAL_DATETIME = DateTime.MinValue,
                                WITHDRAWAL_CODE = "",
                                WITHDRAWAL_SEQ_NO = defaultShortVal
                            });
                            var exprReserveCancel = exprAllReserve.Where(e => e.RESERVE_DELETED_DATETIME != null)
                                .Select(e => new
                                {
                                    DATA_TYPE = "RESERVE_CANCEL",
                                    CREATED_DATETIME = e.RESERVE_DELETED_DATETIME.Value,
                                    REFER_DOC_NO = e.RESERVE_ID,
                                    REMARK_TEXT = string.Format("ยกเลิกเลขที่กันเงิน {0} - {1}", e.RESERVE_ID, e.RESERVE_REJECT_REMARK_TEXT),
                                    BUDGET_AMOUNT = decimal.Zero,
                                    CASHBACK_AMOUNT = e.RESERVE_CASHBACK_BUDGET_AMOUNT.Value,
                                    RESERVE_TYPE = e.RESERVE_TYPE.Value,
                                    WITHDRAWAL_DATETIME = DateTime.MinValue,
                                    WITHDRAWAL_CODE = "",
                                    WITHDRAWAL_SEQ_NO = defaultShortVal
                                });
                            var exprReserveWithdrawal = exprAllReserve.Where(e => e.WITHDRAWAL_SEQ_NO != null)
                                .Select(e => new
                                {
                                    DATA_TYPE = "WITHDRAWAL",
                                    CREATED_DATETIME = e.RESERVE_DATE.Value,
                                    REFER_DOC_NO = e.RESERVE_ID,
                                    REMARK_TEXT = string.Format("{0} - {1}", e.RESERVE_ID, e.WITHDRAWAL_REMARK_TEXT), //string.Format("เบิกจ่าย - เลขที่เบิกจ่าย {0} ครั้งที่ {1}", e.WITHDRAWAL_CODE, e.WITHDRAWAL_SEQ_NO.Value),
                                    BUDGET_AMOUNT = e.WITHDRAWAL_AMOUNT.Value,
                                    CASHBACK_AMOUNT = decimal.Zero,
                                    RESERVE_TYPE = e.RESERVE_TYPE.Value,
                                    WITHDRAWAL_DATETIME = e.WITHDRAWAL_DATE.Value,
                                    e.WITHDRAWAL_CODE,
                                    WITHDRAWAL_SEQ_NO = e.WITHDRAWAL_SEQ_NO.Value
                                });


                            var rows = exprIncome.Union(exprAllocate).Union(exprReserve).Union(exprReserveWithdrawal).Union(exprReserveCancel)
                                .OrderBy(e => e.CREATED_DATETIME) // วันที่ => เงินประจำงวด, จัดสรรงบประมาณ, กันเงิน
                                .ThenBy(e => e.REFER_DOC_NO)
                                .ThenBy(e => e.WITHDRAWAL_SEQ_NO).ToList();
                            foreach (var row in rows)
                            {
                                // รายการรับเงินประจำงวด
                                if ("INCOME".Equals(row.DATA_TYPE))
                                {
                                    decimal totalIncomeBudgetAmount = row.BUDGET_AMOUNT;
                                    budgetBalance += totalIncomeBudgetAmount;
                                    estimateBudgetBalance -= totalIncomeBudgetAmount;

                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo), true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), totalIncomeBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), estimateBudgetBalance, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), totalIncomeBudgetAmount, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), budgetBalance, true);

                                    range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                                    range.Style.Font.Color.SetColor(blueColor);
                                    range.Style.Font.Bold = true;
                                    rowIndex++;
                                }


                                // จัดสรรงบประมาณให้กับหน่วยงานภูมิภาค
                                if ("ALLOCATE".Equals(row.DATA_TYPE))
                                {
                                    decimal totalAllocateBudgetAmount = row.BUDGET_AMOUNT;
                                    decimal totalAllocateCashbackBudgetAmount = row.CASHBACK_AMOUNT;

                                    // จัดสรรงบประมาณให้หน่วยงานภูมิภาค
                                    if (totalAllocateBudgetAmount.CompareTo(decimal.Zero) == 1)
                                    {
                                        budgetBalance -= totalAllocateBudgetAmount;
                                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo), true);
                                        ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                        ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), totalAllocateBudgetAmount * -1, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), budgetBalance, true);
                                        rowIndex++;
                                    }

                                    // ดึงงบประมาณคืนจากหน่วยงานภูมิภาค
                                    if (totalAllocateCashbackBudgetAmount.CompareTo(decimal.Zero) == 1)
                                    {
                                        budgetBalance += totalAllocateCashbackBudgetAmount;
                                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo), true);
                                        ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                        ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT.Replace("จัดสรรงบประมาณ", "ดึงงบประมาณคืน"), true, "", true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), totalAllocateCashbackBudgetAmount, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), budgetBalance, true);

                                        // เน้นสี รายการที่ดึงงบประมาณคืน
                                        range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                                        range.Style.Font.Color.SetColor(redColor);
                                        rowIndex++;
                                    }
                                }

                                // กันเงินงบประมาณ
                                if ("RESERVE".Equals(row.DATA_TYPE))
                                {
                                    reserveBudgetBalance = row.BUDGET_AMOUNT;
                                    budgetBalance -= reserveBudgetBalance;

                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo), true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), row.RESERVE_TYPE.Equals(1) ? reserveBudgetBalance : decimal.Zero, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), row.RESERVE_TYPE.Equals(2) ? reserveBudgetBalance : decimal.Zero, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), budgetBalance, true);
                                    rowIndex++;
                                }

                                // ยกเลิกกันเงิน
                                if ("RESERVE_CANCEL".Equals(row.DATA_TYPE))
                                {
                                    budgetBalance += row.CASHBACK_AMOUNT;

                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo), true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true, "", true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), row.CASHBACK_AMOUNT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), row.CASHBACK_AMOUNT * -1, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), budgetBalance, true);

                                    // เน้นข้อความรายการที่ยกเลิกกันเงิน
                                    range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                                    range.Style.Font.Color.SetColor(redColor);
                                    rowIndex++;
                                }

                                // เบิกจ่าย
                                if ("WITHDRAWAL".Equals(row.DATA_TYPE))
                                {
                                    reserveBudgetBalance -= row.BUDGET_AMOUNT;
                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), row.WITHDRAWAL_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo), true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.WITHDRAWAL_CODE, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), row.BUDGET_AMOUNT * -1, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), row.BUDGET_AMOUNT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), budgetBalance, true);
                                    //ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), reserveBudgetBalance, true);

                                    // เน้นข้อความรายการที่ยกเลิกกันเงิน
                                    range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                                    range.Style.Font.Color.SetColor(redPigColor);
                                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    range.Style.Fill.BackgroundColor.SetColor(softPinkColor);
                                    rowIndex++;
                                }
                            }

                            // เพิ่มแถวเพื่อเว้นระยะห่างระหว่างแต่ละกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย)
                            rowIndex += 2;
                        }
                    }

                    xlsApp.Workbook.Worksheets["TEMPLATE"].Hidden = eWorkSheetHidden.VeryHidden;
                    res["filename"] = filename;
                    string saveToFile = string.Format("{0}/{1}", appSettings.TemporaryPath, filename);
                    xlsApp.SaveAs(new FileInfo(saveToFile));
                }
                return Json(res, JsonRequestBehavior.DenyGet);
                //}
            }
        }

        /// <summary>
        /// ทะเบียนคุมเงินนอกงบประมาณ การรับเงินประจำงวดของเงินนอกงบประมาณ
        /// จะรับมาเป็นก้อนใหญ่ ที่ไม่แตกลงรายการ ค่าใช้จ่าย ทำให้ไม่สามารถแสดงกระแสเงินลงเป็นรายการได้
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="budgetType"></param>
        /// <param name="returnType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public ActionResult RetrieveOff(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int budgetType, string returnType, int? pageIndex, int? pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprBudgetCashFlow = db.V_GET_BUDGET_CASH_FLOW_STATEMENTs.Where(e => e.YR.Equals(fiscalYear));

                if (null != planId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    exprBudgetCashFlow = exprBudgetCashFlow.Where(e => e.EXPENSES_ID.Equals(expensesId));

                // จัดกลุ่มข้อมูล งบรายจ่าย เพื่อสร้างเป็น Sheet Name
                // ตามด้วย แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                var finalExprBudgetCashFlow = exprBudgetCashFlow.GroupBy(e => new
                {
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.BUDGET_TYPE_ORDER_SEQ
                }).OrderBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .Select(x => new
                {
                    BudgetTypeItem = x.Key,
                    Values = x.GroupBy(e => new
                    {
                        e.PLAN_ID,
                        e.PLAN_NAME,
                        e.PLAN_ORDER_SEQ,
                        e.PRODUCE_ID,
                        e.PRODUCE_NAME,
                        e.PRODUCE_ORDER_SEQ,
                        e.ACTIVITY_ID,
                        e.ACTIVITY_NAME,
                        e.ACTIVITY_SHORT_NAME,
                        e.ACTIVITY_ORDER_SEQ,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.BUDGET_TYPE_ORDER_SEQ,
                    }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                    .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                    .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                    .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                    .Select(e => new
                    {
                        GroupBy = e.Key,
                        Rows = e
                    })
                });

                // นำข้อมูลส่งออกไปยังโปรแกรม XLS
                Dictionary<string, string> res = new Dictionary<string, string>(2) {
                        { "errorText", null },
                        { "filename", "" }
                    };

                if (finalExprBudgetCashFlow.Count() == 0)
                {
                    res["errorText"] = "ไม่พบข้อมูล โปรดตรวจสอบเงื่อนไขการค้นหา";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var appSettings = AppSettingProperty.ParseXml();
                string templateFile = string.Format("{0}/Report005_BudgetCashFlow_Template.xlsx", appSettings.ReportTemplatePath);
                string filename = string.Format("{0}_ทะเบียนคุมเงินนอกงบประมาณ_{1}.xlsx", userAuthorizeProfile.EmpId, DateTime.Now.Ticks);
                using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(templateFile)))
                {
                    Color redColor = ColorTranslator.FromHtml("#FF0000");
                    Color blueColor = ColorTranslator.FromHtml("#1E00B3");
                    Color softPinkColor = ColorTranslator.FromHtml("#FDE9D9");
                    Color redPigColor = ColorTranslator.FromHtml("#A70900");
                    short defaultShortVal = 0;
                    decimal offBudgetBalance = decimal.Zero;
                    var exprOffIncome = db.T_OFF_BUDGET_MASTER_INCOME_HISTORies.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1));
                    if (exprOffIncome.Any())
                        offBudgetBalance = exprOffIncome.Sum(e => e.BUDGET_AMOUNT);

                    int overviewRowIndex = 5;
                    var exportOverviewHelper = new ExportHelper(xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "ภาพรวม"));
                    string reportTitle = exportOverviewHelper.CurrWorkSheet.Cells["A1"].Text;
                    string yearStr = (fiscalYear + 543).ToString();
                    reportTitle = reportTitle.Replace("[var_fiscal_year]", yearStr);

                    string exportDateText = exportOverviewHelper.CurrWorkSheet.Cells["I2"].Text;
                    string exportDateVal = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                    exportDateText = exportDateText.Replace("[var_export_date]", exportDateVal);

                    // Sheet ภาพรวม
                    exportOverviewHelper.CurrWorkSheet.Cells["A1"].Value = reportTitle;
                    exportOverviewHelper.CurrWorkSheet.Cells["I2"].Value = exportDateText;
                    exportOverviewHelper.CurrWorkSheet.Row(3).Hidden = true;
                    exportOverviewHelper.CurrWorkSheet.Row(4).Hidden = true;

                    foreach (var budget in finalExprBudgetCashFlow)
                    {
                        ExportUtils.CurrWorkSheet = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", budget.BudgetTypeItem.BUDGET_TYPE_NAME);
                        ExportUtils.CurrWorkSheet.Cells["A1"].Value = reportTitle;
                        ExportUtils.CurrWorkSheet.Cells["I2"].Value = exportDateText;

                        ExportUtils.CurrWorkSheet.Row(3).Hidden = true;
                        ExportUtils.CurrWorkSheet.Row(4).Hidden = true;

                        // ข้อมูลภายใต้ กิจกรรม
                        var rowIndex = 5;
                        foreach (var group in budget.Values)
                        {

                            // เขียนส่วนกลุ่มข้อมูล แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.PLAN_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.PRODUCE_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.ACTIVITY_NAME, false, "", true);
                            ExportUtils.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex++), group.GroupBy.BUDGET_TYPE_NAME, false, "", true);
                            // เขียนส่วนคอลัมล์
                            ExportUtils.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "วัน/เดือน/ปี");
                            ExportUtils.SetCaption(string.Format("B{0}:B{1}", rowIndex, rowIndex + 1), "เลขที่เอกสาร");
                            ExportUtils.SetCaption(string.Format("C{0}:C{1}", rowIndex, rowIndex + 1), "รายการ");
                            ExportUtils.SetCaption(string.Format("D{0}:F{0}", rowIndex), "งบประมาณรายจ่าย (บาท)");
                            ExportUtils.SetCaption(string.Format("D{0}", rowIndex + 1), "เพิ่ม");
                            ExportUtils.SetCaption(string.Format("E{0}", rowIndex + 1), "ลด");
                            ExportUtils.SetCaption(string.Format("F{0}", rowIndex + 1), "คงเหลือ");
                            ExportUtils.SetCaption(string.Format("G{0}:K{0}", rowIndex), "เงินประจำงวด (บาท)");
                            ExportUtils.SetCaption(string.Format("G{0}", rowIndex + 1), "เพิ่ม");
                            ExportUtils.SetCaption(string.Format("H{0}", rowIndex + 1), "ผูกพัน");
                            ExportUtils.SetCaption(string.Format("I{0}", rowIndex + 1), "กันไว้เบิก");
                            ExportUtils.SetCaption(string.Format("J{0}", rowIndex + 1), "เบิก");
                            ExportUtils.SetCaption(string.Format("K{0}", rowIndex + 1), "คงเหลือ");
                            rowIndex += 2;


                            // เขียนส่วนกลุ่มข้อมูล แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                            exportOverviewHelper.SetCellTextVal(string.Format("A{0}:G{0}", overviewRowIndex++), group.GroupBy.PLAN_NAME, false, "", true);
                            exportOverviewHelper.SetCellTextVal(string.Format("A{0}:G{0}", overviewRowIndex++), group.GroupBy.PRODUCE_NAME, false, "", true);
                            exportOverviewHelper.SetCellTextVal(string.Format("A{0}:G{0}", overviewRowIndex++), group.GroupBy.ACTIVITY_NAME, false, "", true);
                            exportOverviewHelper.SetCellTextVal(string.Format("A{0}:G{0}", overviewRowIndex++), group.GroupBy.BUDGET_TYPE_NAME, false, "", true);
                            // เขียนส่วนคอลัมล์
                            exportOverviewHelper.SetCaption(string.Format("A{0}:A{1}", overviewRowIndex, overviewRowIndex + 1), "วัน/เดือน/ปี");
                            exportOverviewHelper.SetCaption(string.Format("B{0}:B{1}", overviewRowIndex, overviewRowIndex + 1), "เลขที่เอกสาร");
                            exportOverviewHelper.SetCaption(string.Format("C{0}:C{1}", overviewRowIndex, overviewRowIndex + 1), "รายการ");
                            exportOverviewHelper.SetCaption(string.Format("D{0}:F{0}", overviewRowIndex), "งบประมาณรายจ่าย (บาท)");
                            exportOverviewHelper.SetCaption(string.Format("D{0}", overviewRowIndex + 1), "เพิ่ม");
                            exportOverviewHelper.SetCaption(string.Format("E{0}", overviewRowIndex + 1), "ลด");
                            exportOverviewHelper.SetCaption(string.Format("F{0}", overviewRowIndex + 1), "คงเหลือ");
                            exportOverviewHelper.SetCaption(string.Format("G{0}:K{0}", overviewRowIndex), "เงินประจำงวด (บาท)");
                            exportOverviewHelper.SetCaption(string.Format("G{0}", overviewRowIndex + 1), "เพิ่ม");
                            exportOverviewHelper.SetCaption(string.Format("H{0}", overviewRowIndex + 1), "ผูกพัน");
                            exportOverviewHelper.SetCaption(string.Format("I{0}", overviewRowIndex + 1), "กันไว้เบิก");
                            exportOverviewHelper.SetCaption(string.Format("J{0}", overviewRowIndex + 1), "เบิก");
                            exportOverviewHelper.SetCaption(string.Format("K{0}", overviewRowIndex + 1), "คงเหลือ");
                            overviewRowIndex += 2;

                            // เขียนจำนวนเงินที่รัฐบาลจะจัดสรรให้
                            var exprTmp = group.Rows.GroupBy(e => new
                            {
                                e.PLAN_ID,
                                e.PRODUCE_ID,
                                e.ACTIVITY_ID,
                                e.BUDGET_TYPE_ID,
                                e.EXPENSES_GROUP_ID,
                                e.EXPENSES_ID,
                                e.ESTIMATE_BUDGET_AMOUNT,
                                e.ESTIMATE_OFF_BUDGET_AMOUNT
                            }).ToList();
                            decimal estimateBudgetBalance = exprTmp.Sum(e => e.Key.ESTIMATE_BUDGET_AMOUNT);

                            ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), string.Format("รับจัดสรรงบประมาณรายจ่ายประจำปี {0}", yearStr), true, "", true);
                            ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), estimateBudgetBalance, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), 0, true);
                            ExcelRange range = ExportUtils.GetRange(string.Format("A{0}:K{0}", rowIndex));
                            range.Style.Font.Bold = true;
                            rowIndex++;
                            ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), string.Format("เงินประจำงวดยกมาสุทธิ ปี {0}", yearStr), true, "", true);
                            ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), decimal.Zero, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), decimal.Zero, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), decimal.Zero, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), offBudgetBalance, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), 0, true);
                            range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                            range.Style.Font.Color.SetColor(blueColor);
                            range.Style.Font.Bold = true;
                            rowIndex++;

                            // Sheet ภาพรวม
                            exportOverviewHelper.SetCellTextVal(string.Format("A{0}", overviewRowIndex), "", true);
                            exportOverviewHelper.SetCellTextVal(string.Format("B{0}", overviewRowIndex), "", true);
                            exportOverviewHelper.SetCellTextVal(string.Format("C{0}", overviewRowIndex), string.Format("รับจัดสรรงบประมาณรายจ่ายประจำปี {0}", yearStr), true, "", true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("D{0}", overviewRowIndex), estimateBudgetBalance, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("E{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("F{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("G{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("H{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("I{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("J{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), 0, true);
                            range = exportOverviewHelper.GetRange(string.Format("A{0}:K{0}", overviewRowIndex));
                            range.Style.Font.Bold = true;
                            overviewRowIndex++;
                            exportOverviewHelper.SetCellTextVal(string.Format("A{0}", overviewRowIndex), "", true);
                            exportOverviewHelper.SetCellTextVal(string.Format("B{0}", overviewRowIndex), "", true);
                            exportOverviewHelper.SetCellTextVal(string.Format("C{0}", overviewRowIndex), string.Format("เงินประจำงวดยกมาสุทธิ ปี {0}", yearStr), true, "", true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("D{0}", overviewRowIndex), decimal.Zero, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("E{0}", overviewRowIndex), decimal.Zero, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("F{0}", overviewRowIndex), decimal.Zero, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("G{0}", overviewRowIndex), offBudgetBalance, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("H{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("I{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("J{0}", overviewRowIndex), 0, true);
                            exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), 0, true);
                            range = exportOverviewHelper.GetRange(string.Format("C{0}:K{0}", overviewRowIndex));
                            range.Style.Font.Color.SetColor(blueColor);
                            range.Style.Font.Bold = true;
                            overviewRowIndex++;

                            decimal reserveBudgetBalance = decimal.Zero; // จำนวนที่กันเงิน ในใบกัน เพื่อนำไปลดยอดจากการเบิกจ่าย

                            // ข้อมูล group.Rows จะอยู่ภายใต้ แผนงาน ผลผลิต กิจกรรม งบรายจ่าย
                            // นำผลลัพธ์ที่ได้มาจัดเรียงลำดับใหม่ ตามวันที่เกิดเหตุการณ์ต่างๆ
                            // เริ่มจาก เงินประจำงวด, จัดสรรให้หน่วยงานภูมิภาค/กันเงิน

                            // จัดสรรให้หน่วยงานภูมิภาค
                            var exprAllocate = group.Rows.Where(e => null != e.ALLOCATE_OFF_BUDGET_AMOUNT && e.ALLOCATE_OFF_BUDGET_AMOUNT.Value > 0).GroupBy(e => new
                            {
                                e.PLAN_ID,
                                e.PRODUCE_ID,
                                e.ACTIVITY_ID,
                                e.BUDGET_TYPE_ID,
                                e.EXPENSES_GROUP_ID,
                                e.EXPENSES_ID,
                                e.ALLOCATE_PERIOD_CODE,
                                e.ALLOCATE_REFER_DOC_NO,
                                e.ALLOCATE_DATETIME,
                                e.ALLOCATE_OFF_BUDGET_AMOUNT,
                                e.ALLOCATE_CASHBACK_OFF_BUDGET_AMOUNT
                            })
                            .Select(e => e.Key)
                            .GroupBy(e => new { e.ALLOCATE_PERIOD_CODE, e.ALLOCATE_REFER_DOC_NO, e.ALLOCATE_DATETIME })
                            .AsEnumerable().Select(e => new
                            {
                                DATA_TYPE = "ALLOCATE",
                                CREATED_DATETIME = e.Key.ALLOCATE_DATETIME.Value,
                                REFER_DOC_NO = e.Key.ALLOCATE_REFER_DOC_NO,
                                REMARK_TEXT = string.Format("จัดสรรงบนอกประมาณ - งวดที่ {0}", e.Key.ALLOCATE_PERIOD_CODE),
                                BUDGET_AMOUNT = e.Sum(x => null == x.ALLOCATE_OFF_BUDGET_AMOUNT ? decimal.Zero : x.ALLOCATE_OFF_BUDGET_AMOUNT.Value),
                                CASHBACK_AMOUNT = e.Sum(x => null == x.ALLOCATE_CASHBACK_OFF_BUDGET_AMOUNT ? decimal.Zero : x.ALLOCATE_CASHBACK_OFF_BUDGET_AMOUNT.Value),
                                RESERVE_TYPE = defaultShortVal,
                                WITHDRAWAL_DATETIME = DateTime.MinValue,
                                WITHDRAWAL_CODE = "",
                                WITHDRAWAL_SEQ_NO = defaultShortVal
                            });

                            // กันเงินงบนอกประมาณ
                            var exprAllReserve = group.Rows.Where(e => e.RESERVE_BUDGET_TYPE != null && e.RESERVE_BUDGET_TYPE.Value.Equals(2)).AsEnumerable();
                            var exprReserve = exprAllReserve.Select(e => new
                            {
                                DATA_TYPE = "RESERVE",
                                CREATED_DATETIME = e.RESERVE_DATE.Value,
                                REFER_DOC_NO = e.RESERVE_ID,
                                REMARK_TEXT = e.RESERVE_REMARK_TEXT, //string.Format("กันเงินงบนอกประมาณ - เลขที่กันเงิน {0}", e.RESERVE_ID),
                                BUDGET_AMOUNT = e.RESERVE_OFF_BUDGET_AMOUNT.Value,
                                CASHBACK_AMOUNT = decimal.Zero,
                                RESERVE_TYPE = e.RESERVE_TYPE.Value,
                                WITHDRAWAL_DATETIME = DateTime.MinValue,
                                WITHDRAWAL_CODE = "",
                                WITHDRAWAL_SEQ_NO = defaultShortVal
                            });
                            var exprReserveCancel = exprAllReserve.Where(e => e.RESERVE_DELETED_DATETIME != null)
                                .Select(e => new
                                {
                                    DATA_TYPE = "RESERVE_CANCEL",
                                    CREATED_DATETIME = e.RESERVE_DELETED_DATETIME.Value,
                                    REFER_DOC_NO = e.RESERVE_ID,
                                    REMARK_TEXT = string.Format("ยกเลิกเลขที่กันเงิน {0} - {1}", e.RESERVE_ID, e.RESERVE_REJECT_REMARK_TEXT),
                                    BUDGET_AMOUNT = decimal.Zero,
                                    CASHBACK_AMOUNT = e.RESERVE_CASHBACK_OFF_BUDGET_AMOUNT.Value,
                                    RESERVE_TYPE = e.RESERVE_TYPE.Value,
                                    WITHDRAWAL_DATETIME = DateTime.MinValue,
                                    WITHDRAWAL_CODE = "",
                                    WITHDRAWAL_SEQ_NO = defaultShortVal
                                });
                            var exprReserveWithdrawal = exprAllReserve.Where(e => e.WITHDRAWAL_SEQ_NO != null)
                                .Select(e => new
                                {
                                    DATA_TYPE = "WITHDRAWAL",
                                    CREATED_DATETIME = e.RESERVE_DATE.Value,
                                    REFER_DOC_NO = e.RESERVE_ID,
                                    REMARK_TEXT = string.Format("{0} - {1}", e.RESERVE_ID, e.WITHDRAWAL_REMARK_TEXT), //string.Format("เบิกจ่าย - เลขที่เบิกจ่าย {0} ครั้งที่ {1}", e.WITHDRAWAL_CODE, e.WITHDRAWAL_SEQ_NO.Value),
                                    BUDGET_AMOUNT = e.WITHDRAWAL_AMOUNT.Value,
                                    CASHBACK_AMOUNT = decimal.Zero,
                                    RESERVE_TYPE = e.RESERVE_TYPE.Value,
                                    WITHDRAWAL_DATETIME = e.WITHDRAWAL_DATE.Value,
                                    e.WITHDRAWAL_CODE,
                                    WITHDRAWAL_SEQ_NO = e.WITHDRAWAL_SEQ_NO.Value
                                });


                            var rows = exprAllocate.Union(exprReserve).Union(exprReserveWithdrawal).Union(exprReserveCancel)
                                .OrderBy(e => e.CREATED_DATETIME) // วันที่ => เงินประจำงวด, จัดสรรงบประมาณ, กันเงิน, เบิกจ่าย
                                .ThenBy(e => e.REFER_DOC_NO)
                                .ThenBy(e => e.WITHDRAWAL_SEQ_NO).ToList();
                            foreach (var row in rows)
                            {

                                // จัดสรรงบประมาณให้กับหน่วยงานภูมิภาค
                                if ("ALLOCATE".Equals(row.DATA_TYPE))
                                {
                                    decimal totalAllocateBudgetAmount = row.BUDGET_AMOUNT;
                                    decimal totalAllocateCashbackBudgetAmount = row.CASHBACK_AMOUNT;

                                    // จัดสรรงบประมาณให้หน่วยงานภูมิภาค
                                    if (totalAllocateBudgetAmount.CompareTo(decimal.Zero) == 1)
                                    {
                                        string createdDateStr = row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                                        offBudgetBalance -= totalAllocateBudgetAmount;
                                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), createdDateStr, true);
                                        ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                        ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), totalAllocateBudgetAmount * -1, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), offBudgetBalance, true);
                                        rowIndex++;

                                        // ภาพรวม
                                        exportOverviewHelper.SetCellTextVal(string.Format("A{0}", overviewRowIndex), createdDateStr, true);
                                        exportOverviewHelper.SetCellTextVal(string.Format("B{0}", overviewRowIndex), row.REFER_DOC_NO, true);
                                        exportOverviewHelper.SetCellTextVal(string.Format("C{0}", overviewRowIndex), row.REMARK_TEXT, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("D{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("E{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("F{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("G{0}", overviewRowIndex), totalAllocateBudgetAmount * -1, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("H{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("I{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("J{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), offBudgetBalance, true);
                                        overviewRowIndex++;
                                    }

                                    // ดึงงบประมาณคืนจากหน่วยงานภูมิภาค
                                    if (totalAllocateCashbackBudgetAmount.CompareTo(decimal.Zero) == 1)
                                    {
                                        offBudgetBalance += totalAllocateCashbackBudgetAmount;
                                        string createdDateStr = row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), createdDateStr, true);
                                        ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                        ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT.Replace("จัดสรรงบประมาณ", "ดึงงบประมาณคืน"), true, "", true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), totalAllocateCashbackBudgetAmount, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                        ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), offBudgetBalance, true);
                                        // เน้นสี รายการที่ดึงงบประมาณคืน
                                        range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                                        range.Style.Font.Color.SetColor(redColor);
                                        rowIndex++;

                                        // ภาพรวม
                                        exportOverviewHelper.SetCellTextVal(string.Format("A{0}", overviewRowIndex), createdDateStr, true);
                                        exportOverviewHelper.SetCellTextVal(string.Format("B{0}", overviewRowIndex), row.REFER_DOC_NO, true);
                                        exportOverviewHelper.SetCellTextVal(string.Format("C{0}", overviewRowIndex), row.REMARK_TEXT.Replace("จัดสรรงบประมาณ", "ดึงงบประมาณคืน"), true, "", true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("D{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("E{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("F{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("G{0}", overviewRowIndex), totalAllocateCashbackBudgetAmount, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("H{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("I{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("J{0}", overviewRowIndex), 0, true);
                                        exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), offBudgetBalance, true);
                                        // เน้นสี รายการที่ดึงงบประมาณคืน
                                        range = exportOverviewHelper.GetRange(string.Format("C{0}:K{0}", overviewRowIndex));
                                        range.Style.Font.Color.SetColor(redColor);
                                        overviewRowIndex++;
                                    }
                                }

                                // กันเงินงบประมาณ
                                if ("RESERVE".Equals(row.DATA_TYPE))
                                {
                                    reserveBudgetBalance = row.BUDGET_AMOUNT;
                                    offBudgetBalance -= reserveBudgetBalance;
                                    string createdDateStr = row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);
                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), createdDateStr, true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), row.RESERVE_TYPE.Equals(1) ? reserveBudgetBalance : decimal.Zero, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), row.RESERVE_TYPE.Equals(2) ? reserveBudgetBalance : decimal.Zero, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), offBudgetBalance, true);
                                    rowIndex++;

                                    // ภาพรวม
                                    exportOverviewHelper.SetCellTextVal(string.Format("A{0}", overviewRowIndex), createdDateStr, true);
                                    exportOverviewHelper.SetCellTextVal(string.Format("B{0}", overviewRowIndex), row.REFER_DOC_NO, true);
                                    exportOverviewHelper.SetCellTextVal(string.Format("C{0}", overviewRowIndex), row.REMARK_TEXT, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("D{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("E{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("F{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("G{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("H{0}", overviewRowIndex), row.RESERVE_TYPE.Equals(1) ? reserveBudgetBalance : decimal.Zero, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("I{0}", overviewRowIndex), row.RESERVE_TYPE.Equals(2) ? reserveBudgetBalance : decimal.Zero, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("J{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), offBudgetBalance, true);
                                    overviewRowIndex++;
                                }

                                // ยกเลิกกันเงิน
                                if ("RESERVE_CANCEL".Equals(row.DATA_TYPE))
                                {
                                    offBudgetBalance += row.CASHBACK_AMOUNT;
                                    string createdDateStr = row.CREATED_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);

                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), createdDateStr, true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.REFER_DOC_NO, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true, "", true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), row.CASHBACK_AMOUNT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), row.CASHBACK_AMOUNT * -1, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), offBudgetBalance, true);
                                    // เน้นข้อความรายการที่ยกเลิกกันเงิน
                                    range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                                    range.Style.Font.Color.SetColor(redColor);
                                    rowIndex++;

                                    // ภาพรวม
                                    exportOverviewHelper.SetCellTextVal(string.Format("A{0}", overviewRowIndex), createdDateStr, true);
                                    exportOverviewHelper.SetCellTextVal(string.Format("B{0}", overviewRowIndex), row.REFER_DOC_NO, true);
                                    exportOverviewHelper.SetCellTextVal(string.Format("C{0}", overviewRowIndex), row.REMARK_TEXT, true, "", true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("D{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("E{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("F{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("G{0}", overviewRowIndex), row.CASHBACK_AMOUNT, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("H{0}", overviewRowIndex), row.CASHBACK_AMOUNT * -1, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("I{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("J{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), offBudgetBalance, true);
                                    // เน้นข้อความรายการที่ยกเลิกกันเงิน
                                    range = exportOverviewHelper.GetRange(string.Format("C{0}:K{0}", overviewRowIndex));
                                    range.Style.Font.Color.SetColor(redColor);
                                    overviewRowIndex++;
                                }

                                // เบิกจ่าย
                                if ("WITHDRAWAL".Equals(row.DATA_TYPE))
                                {
                                    reserveBudgetBalance -= row.BUDGET_AMOUNT;
                                    string createdDateStr = row.WITHDRAWAL_DATETIME.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);

                                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), createdDateStr, true);
                                    ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), row.WITHDRAWAL_CODE, true);
                                    ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), row.REMARK_TEXT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("D{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("F{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("G{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("H{0}", rowIndex), row.BUDGET_AMOUNT * -1, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("I{0}", rowIndex), 0, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("J{0}", rowIndex), row.BUDGET_AMOUNT, true);
                                    ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), offBudgetBalance, true);
                                    //ExportUtils.SetCellCurrencyVal(string.Format("K{0}", rowIndex), reserveBudgetBalance, true);
                                    // เน้นข้อความรายการที่เบิกจ่าย
                                    range = ExportUtils.GetRange(string.Format("C{0}:K{0}", rowIndex));
                                    range.Style.Font.Color.SetColor(redPigColor);
                                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    range.Style.Fill.BackgroundColor.SetColor(softPinkColor);
                                    rowIndex++;

                                    // ภวพรวม
                                    exportOverviewHelper.SetCellTextVal(string.Format("A{0}", overviewRowIndex), createdDateStr, true);
                                    exportOverviewHelper.SetCellTextVal(string.Format("B{0}", overviewRowIndex), row.WITHDRAWAL_CODE, true);
                                    exportOverviewHelper.SetCellTextVal(string.Format("C{0}", overviewRowIndex), row.REMARK_TEXT, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("D{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("E{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("F{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("G{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("H{0}", overviewRowIndex), row.BUDGET_AMOUNT * -1, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("I{0}", overviewRowIndex), 0, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("J{0}", overviewRowIndex), row.BUDGET_AMOUNT, true);
                                    exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), offBudgetBalance, true);
                                    //exportOverviewHelper.SetCellCurrencyVal(string.Format("K{0}", overviewRowIndex), reserveBudgetBalance, true);
                                    // เน้นข้อความรายการที่เบิกจ่าย
                                    range = exportOverviewHelper.GetRange(string.Format("C{0}:K{0}", overviewRowIndex));
                                    range.Style.Font.Color.SetColor(redPigColor);
                                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    range.Style.Fill.BackgroundColor.SetColor(softPinkColor);
                                    overviewRowIndex++;
                                }
                            }

                            // เพิ่มแถวเพื่อเว้นระยะห่างระหว่างแต่ละกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย)
                            rowIndex += 2;
                            overviewRowIndex += 2;
                        }
                    }

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