using ExcisePlaning.Classes;
using ExcisePlaning.Classes.ExpensesInfra;
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
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// รายงานรายละเอียดของรายการค่าใช้จ่าย ที่หน่วยงานระบุ
    /// เพื่อส่งคำขอ งบประมาณ
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptBudgetRequestExpensesDetailController : Controller
    {
        // GET: RptBudgetRequestDetail
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_REPORT_BUDGET_REQUEST_EXPENSES_DETAIL);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_REPORT_BUDGET_REQUEST_EXPENSES_DETAIL;
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


            ViewBag.FiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.DepartmentId = userAuthorizeProfile.DepId;
            ViewBag.AreaId = userAuthorizeProfile.AreaId;
            ViewBag.DepAuthorize = userAuthorizeProfile.DepAuthorize;
            ViewBag.CanSelectDepartment = userAuthorizeProfile.CanSelectDepartment;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ข้อมูลเขตพื้นที่
                // ไม่ใช่หน่วยงานกลาง เห็นได้เฉพาะเขตพื้นที่ตนเอง
                var areaExpr = db.T_AREAs.Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = e.AREA_NAME,
                    AREA_CODE = e.AREA_CODE
                });
                if (userAuthorizeProfile.DepAuthorize.Equals(2))
                    areaExpr = areaExpr.Where(e => e.AREA_ID.Equals(userAuthorizeProfile.AreaId));
                ViewBag.Areas = areaExpr.ToList();


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
        /// ส่งข้อมูล รายละเอียดคำของบประมาณ ของหน่วยงานออกไปยัง Excel
        /// </summary>
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="areaId"></param>
        /// <param name="depId"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesIds"></param>
        /// <param name="budgetTypeFlag">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="requestTypeFlag">1 = คำขอต้นปี, 2 = คำขอเพิ่มเติม</param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitExport(int fiscalYear, int? areaId, int? depId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, string expensesIds, int budgetTypeFlag, int? requestTypeFlag, string requestId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>() {
                { "errorText", null },
                { "filename", "" },
                { "groupType", "Temporary" } // อ้างอิงจาก ResourceController.GetFile
            };
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var usrProps = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var reqDetailExpr = db.V_GET_BUDGET_REQUEST_DETAIL_ONLY_DESCRIBEs.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1)
                    && e.BUDGET_TYPE.Equals(budgetTypeFlag));

                if (null != requestTypeFlag)
                    reqDetailExpr = reqDetailExpr.Where(e => e.REQ_TYPE.Equals(requestTypeFlag));
                if (!string.IsNullOrEmpty(requestId))
                    reqDetailExpr = reqDetailExpr.Where(e => e.REQ_ID.Equals(requestId));

                // หน่วยงานกลาง
                if (usrProps.DepAuthorize.Equals(1))
                {
                    if (null != areaId)
                        reqDetailExpr = reqDetailExpr.Where(e => db.T_BUDGET_REQUEST_MASTERs.Any(m => m.REQ_ID.Equals(e.REQ_ID) && m.AREA_ID.Equals(areaId)));
                    if (null != depId)
                        reqDetailExpr = reqDetailExpr.Where(e => e.DEP_ID.Equals(depId));
                }
                else // หน่วยงานทั่วไป
                {
                    reqDetailExpr = reqDetailExpr.Where(e => db.T_BUDGET_REQUEST_MASTERs.Any(m => m.REQ_ID.Equals(e.REQ_ID) && m.AREA_ID.Equals(usrProps.AreaId)));
                    var depAuthorize = DepartmentAuthorizeFilterProperty.Verfity(usrProps, usrProps.DepId);
                    reqDetailExpr = reqDetailExpr.Where(e => depAuthorize.AssignDepartmentIds.Contains(e.DEP_ID));
                    if (null != depId)
                        reqDetailExpr = reqDetailExpr.Where(e => e.DEP_ID.Equals(depId));
                }

                if (null != planId)
                    reqDetailExpr = reqDetailExpr.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    reqDetailExpr = reqDetailExpr.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    reqDetailExpr = reqDetailExpr.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    reqDetailExpr = reqDetailExpr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    reqDetailExpr = reqDetailExpr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));

                int[] ids = expensesIds.Split(new char[] { ',' }).Where(str => !string.IsNullOrEmpty(str)).Select(str => Convert.ToInt32(str)).ToArray();
                if (ids.Length > 0)
                    reqDetailExpr = reqDetailExpr.Where(e => ids.Contains(e.EXPENSES_ID));


                if (!reqDetailExpr.Any())
                {
                    res["errorText"] = "ไม่พบข้อมูล";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var finalGroupByExpenses = reqDetailExpr.GroupBy(e => new
                {
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.FORM_TEMPLATE_NAME // ไว้สำหรับ Matching Class เพื่ออ่านข้อมูลใน EXPENSES_XML_DESSCRIBE
                }).Select(e => new
                {
                    e.Key,
                    Departments = e.GroupBy(x => new
                    {
                        x.AREA_NAME,
                        x.DEP_NAME,
                        x.DEP_ORDER_SEQ,
                        x.PLAN_NAME,
                        x.PLAN_ORDER_SEQ,
                        x.PRODUCE_NAME,
                        x.PRODUCE_ORDER_SEQ,
                        x.ACTIVITY_NAME,
                        x.ACTIVITY_ORDER_SEQ,
                        x.BUDGET_TYPE_NAME,
                        x.BUDGET_TYPE_ORDER_SEQ,

                        x.EXPENSES_MASTER_NAME,
                        x.EXPENSES_GROUP_NAME,
                        x.EXPENSES_GROUP_ORDER_SEQ,
                    }).Select(x => new
                    {
                        x.Key,
                        Describes = x.Select(m => new
                        {
                            m.TOTAL_REQUEST_BUDGET,
                            m.EXPENSES_XML_DESCRIBE
                        }).ToList()
                    }).OrderBy(x => x.Key.DEP_ORDER_SEQ)
                    .ThenBy(x => x.Key.PLAN_ORDER_SEQ)
                    .ThenBy(x => x.Key.PRODUCE_ORDER_SEQ)
                    .ThenBy(x => x.Key.ACTIVITY_ORDER_SEQ)
                    .ThenBy(x => x.Key.BUDGET_TYPE_ORDER_SEQ)
                    .ThenBy(x => x.Key.EXPENSES_GROUP_ORDER_SEQ).ToList()
                }).OrderBy(e => e.Key.EXPENSES_ORDER_SEQ).ToList();


                var appSettings = AppSettingProperty.ParseXml();
                FileInfo source = new FileInfo(string.Format("{0}/Report008_RptBudgetRequestExpensesDetail_Template.xlsx", appSettings.ReportTemplatePath));
                using (ExcelPackage xlsApp = new ExcelPackage(source))
                {
                    Color redColor = ColorTranslator.FromHtml("#ff0000");
                    string year = Convert.ToString(fiscalYear + 543);
                    string exportDateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo);


                    // รายการ คชจ. ยกเว้น ค่าสาธารณูปโภค & ปิโตรเลียม
                    foreach (var item in finalGroupByExpenses)
                    {
                        string formTemplateName = Regex.Replace(item.Key.FORM_TEMPLATE_NAME, @"\#.+$", "", RegexOptions.IgnoreCase);
                        // กรณีค่าสาธารณูปโภคไม่ต้องใช้ ตรรกะ นี้ในการเขียนลง Xls
                        if ("PublicUtilitiesForm".Equals(formTemplateName) || "ChargePetoleumForm".Equals(formTemplateName))
                            continue;

                        var currWorkSheet = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", item.Key.EXPENSES_ID.ToString());
                        var helper = new ExportHelper(currWorkSheet);
                        helper.GetRange("A1").Value = helper.GetRange("A1").Text.Replace("[var_fiscal_year]", year);
                        helper.GetRange("A2").Value = helper.GetRange("A2").Text.Replace("[var_export_date]", exportDateStr);

                        int rowIndex = 4;
                        foreach (var depItem in item.Departments)
                        {
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "หน่วยงาน: " + depItem.Key.DEP_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "แผนงาน: " + depItem.Key.PLAN_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "ผลผลิต: " + depItem.Key.PRODUCE_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "กิจกรรม: " + depItem.Key.ACTIVITY_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), depItem.Key.BUDGET_TYPE_NAME, false, "", true);
                            if (!string.IsNullOrEmpty(depItem.Key.EXPENSES_MASTER_NAME))
                                helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), depItem.Key.EXPENSES_MASTER_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), depItem.Key.EXPENSES_GROUP_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), item.Key.EXPENSES_NAME, false, "", true);
                            helper.SelectedExcelRange.Style.Font.Color.SetColor(redColor);

                            // รายละเอียดทั้งหมด ของรายการค่าใช้จ่าย (Expenses) ที่หน่วยงานทำคำขอ
                            // ซึ่งกระจายอยู่ในแต่ละเลขที่คำขอ
                            var expensesDescribe = depItem.Describes.Select(e => new ExpensesDescribeProperty()
                            {
                                TotalBudget = e.TOTAL_REQUEST_BUDGET,
                                // รายละเอียดของค่าใช้จ่ายในแต่ละเลขที่คำขอ จะถูก Convert จาก XML -> Object Class เพื่อให้ง่ายต่อการเข้าถึงข้อมูล
                                ExpensesDescribe = e.EXPENSES_XML_DESCRIBE
                            }).ToList();

                            if ("RentHouseForm".Equals(formTemplateName))
                                DoRentHouse(helper, expensesDescribe, ref rowIndex);
                            else if ("MonthlyCompensationExtraForm".Equals(formTemplateName))
                                DoMonthlyCompensationExtra(helper, expensesDescribe, ref rowIndex);
                            else if ("OvertimeCompensationForm".Equals(formTemplateName))
                                DoOvertimeCompensation(helper, expensesDescribe, ref rowIndex);
                            else if ("InsteadCarForPositionCompensationForm".Equals(formTemplateName))
                                DoInsteadCarForPositionCompensation(helper, expensesDescribe, ref rowIndex);
                            else if ("SocialSecurityForm".Equals(formTemplateName))
                                DoSocialSecurity(helper, expensesDescribe, ref rowIndex);
                            else if ("CompensationFundForm".Equals(formTemplateName))
                                DoCompensationFund(helper, expensesDescribe, ref rowIndex);
                            else if ("AllowanceForm".Equals(formTemplateName))
                                DoAllowance(helper, expensesDescribe, ref rowIndex);
                            else if ("RepairVehicleAndTransportForm".Equals(formTemplateName))
                                DoRepairVehicleAndTransport(helper, expensesDescribe, ref rowIndex);
                            else if ("RepairEquipmentForm".Equals(formTemplateName))
                                DoRepairEquipment(helper, expensesDescribe, ref rowIndex);
                            else if ("RepairBuildingForm".Equals(formTemplateName))
                                DoRepairBuilding(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForRentBuildingForm".Equals(formTemplateName))
                                DoChargeForRentBuilding(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForHireServiceForm".Equals(formTemplateName))
                                DoChargeForHireService(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForKillTermiteForm".Equals(formTemplateName))
                                DoChargeForKillTermite(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForSoftwareMAForm".Equals(formTemplateName))
                                DoChargeForSoftwareMA(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForReprintStampForm".Equals(formTemplateName))
                                DoChargeForReprintStamp(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForRentServiceForm".Equals(formTemplateName))
                                DoChargeForRentService(helper, expensesDescribe, ref rowIndex);
                            else if ("TraningAndSeminorsForm".Equals(formTemplateName))
                                DoTraningAndSeminors(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForAdvertiseAndReleaseForm".Equals(formTemplateName))
                                DoChargeForAdvertiseAndRelease(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForMaintainAirConditionerForm".Equals(formTemplateName))
                                DoChargeForMaintainAirConditioner(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForOtherForm".Equals(formTemplateName))
                                DoChargeForOther(helper, expensesDescribe, ref rowIndex);
                            else if ("CourtFeeForm".Equals(formTemplateName))
                                DoCourtFee(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForMaterialOfficialForm".Equals(formTemplateName))
                                DoChargeForMaterialOfficial(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForOilAndLubricateForm".Equals(formTemplateName))
                                DoChargeForOilAndLubricate(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForElectricalAndRadioForm".Equals(formTemplateName))
                                DoChargeForElectricalAndRadio(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForScienceAndMedicalForm".Equals(formTemplateName))
                                DoChargeForScienceAndMedical(helper, expensesDescribe, ref rowIndex);
                            else if ("ComputerMaterialForm".Equals(formTemplateName))
                                DoComputerMaterial(helper, expensesDescribe, ref rowIndex);
                            else if ("VehicleEquipmentAndTransport".Equals(formTemplateName))
                                DoVehicleEquipmentAndTransport(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForMaterialOtherForm".Equals(formTemplateName))
                                DoChargeForMaterialOther(helper, expensesDescribe, ref rowIndex);
                            else if ("OfficialEquipmentForm".Equals(formTemplateName))
                                DoOfficialEquipment(helper, expensesDescribe, ref rowIndex);
                            else if ("ComputerEquipmentForm".Equals(formTemplateName))
                                DoComputerEquipment(helper, expensesDescribe, ref rowIndex);
                            else if ("VehicleAndTransportEquipmentForm".Equals(formTemplateName))
                                DoVehicleAndTransportEquipment(helper, expensesDescribe, ref rowIndex);
                            else if ("ScienceEquipmentForm".Equals(formTemplateName))
                                DoScienceEquipment(helper, expensesDescribe, ref rowIndex);
                            else if ("OtherEquipmentForm".Equals(formTemplateName))
                                DoOtherEquipment(helper, expensesDescribe, ref rowIndex);
                            else if ("LandAndBuildingForm".Equals(formTemplateName))
                                DoLandAndBuilding(helper, expensesDescribe, ref rowIndex);
                            else if ("SubsidyForm".Equals(formTemplateName))
                                DoSubsidy(helper, expensesDescribe, ref rowIndex);
                            else if ("ChargeForPrintStampForm".Equals(formTemplateName))
                                DoChargeForPrintStamp(helper, expensesDescribe, ref rowIndex);
                            else if ("ExpensesConferenceForeignForm".Equals(formTemplateName))
                                DoExpensesConferenceForeign(helper, expensesDescribe, ref rowIndex);
                            else if ("ExpensesSeminaForeignForm".Equals(formTemplateName))
                                DoExpensesSeminaForeign(helper, expensesDescribe, ref rowIndex);
                            else if ("HireAdvisorForm".Equals(formTemplateName))
                                DoHireAdvisor(helper, expensesDescribe, ref rowIndex);
                            else if ("EducationFundForm".Equals(formTemplateName))
                                DoEducationFund(helper, expensesDescribe, ref rowIndex);
                            else if ("HealthCheckProjectForm".Equals(formTemplateName))
                                DoHealthCheckProject(helper, expensesDescribe, ref rowIndex);
                            else if ("GovernmentIncomeForm".Equals(formTemplateName))
                                DoGovernmentIncome(helper, expensesDescribe, ref rowIndex);
                            else if ("TranferToMinistryOfFinancialForm".Equals(formTemplateName))
                                DoTranferToMinistryOfFinancial(helper, expensesDescribe, ref rowIndex);
                            else
                            {
                                // ไม่มี รายงานใน Requestment ให้ซ่อนไว้
                                helper.CurrWorkSheet.Hidden = eWorkSheetHidden.VeryHidden;
                            }


                            // เว้นระยะห่างระหว่าง แต่ละ หน่วยงาน 2 บรรทัด
                            rowIndex += 2;
                        }
                    }


                    // ค่าสาธารณูปโภค
                    // ในระบบจะแยก คชจ. ค่าน้ำ ค่าไฟ ค่าประปา ออกเป็นแต่ละรายการ ทำให้ตอนออกรายงานจะต้อง
                    // จัดกลุ่ม คชจ. มารวมเป็น ค่าสาธารณูปโภค
                    var finalPublicUtilities = finalGroupByExpenses.Where(e => e.Key.FORM_TEMPLATE_NAME.Contains("PublicUtilitiesForm")).ToList();
                    if (finalPublicUtilities.Any())
                    {
                        var finalReGroup = (from a in finalPublicUtilities
                                            from inner in a.Departments
                                            select new
                                            {
                                                inner.Key.DEP_NAME,
                                                inner.Key.PLAN_NAME,
                                                inner.Key.PRODUCE_NAME,
                                                inner.Key.ACTIVITY_NAME,
                                                inner.Key.BUDGET_TYPE_NAME,
                                                inner.Key.EXPENSES_MASTER_NAME,
                                                inner.Key.EXPENSES_GROUP_NAME,
                                                a.Key.EXPENSES_NAME,
                                                EXPENSES_XML_DESCRIBEs = inner.Describes.Select(m => m.EXPENSES_XML_DESCRIBE).ToList()
                                            }).GroupBy(e => new
                                            {
                                                e.DEP_NAME,
                                                e.PLAN_NAME,
                                                e.PRODUCE_NAME,
                                                e.ACTIVITY_NAME,
                                                e.BUDGET_TYPE_NAME,
                                                e.EXPENSES_MASTER_NAME,
                                                e.EXPENSES_GROUP_NAME
                                            }).Select(e => new
                                            {
                                                e.Key,
                                                // ในปี งปม. จะมีการทำคำขอ > 1 รายการ
                                                Expenses = e.GroupBy(m => m.EXPENSES_NAME).Select(m => new
                                                {
                                                    EXPENSES_NAME = m.Key,
                                                    EXPENSES_XML_DESCRIBEs = (from s in m
                                                                              from xmlDescribe in s.EXPENSES_XML_DESCRIBEs
                                                                              select xmlDescribe).ToList()
                                                }).ToList()
                                            }).ToList();


                        var currWorkSheet = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "ค่าสาธารณูปโภค");
                        var helper = new ExportHelper(currWorkSheet);
                        helper.GetRange("A1").Value = helper.GetRange("A1").Text.Replace("[var_fiscal_year]", year);
                        helper.GetRange("A2").Value = helper.GetRange("A2").Text.Replace("[var_export_date]", exportDateStr);
                        int rowIndex = 4;
                        foreach (var depItem in finalReGroup)
                        {
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "หน่วยงาน: " + depItem.Key.DEP_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "แผนงาน: " + depItem.Key.PLAN_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "ผลผลิต: " + depItem.Key.PRODUCE_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "กิจกรรม: " + depItem.Key.ACTIVITY_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), depItem.Key.BUDGET_TYPE_NAME, false, "", true);
                            if (!string.IsNullOrEmpty(depItem.Key.EXPENSES_MASTER_NAME))
                                helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), depItem.Key.EXPENSES_MASTER_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), depItem.Key.EXPENSES_GROUP_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "ค่าไฟฟ้า ประปา โทรศัพท์ ไปรษณีย์ ค่าบริการสื่อสารและโทรคมนาคม", false, "", true);
                            helper.SelectedExcelRange.Style.Font.Color.SetColor(redColor);

                            // คอลัมล์
                            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
                            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
                            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), string.Format("จ่ายจริงปีก่อนหน้า ปี {0} (บาท)", fiscalYear - 1 + 543));
                            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "คำของบประมาณ (บาท)");
                            rowIndex++;

                            decimal? netPrevBudget = decimal.Zero, netReqBudget = decimal.Zero;
                            int itemIndex = 1;
                            foreach (var expensesItem in depItem.Expenses)
                            {
                                helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                                helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), expensesItem.EXPENSES_NAME, true);
                                decimal? totalPrevBudget = decimal.Zero,
                                    totalReqBudget = decimal.Zero;
                                expensesItem.EXPENSES_XML_DESCRIBEs.ForEach(xmlDescribeVal =>
                                {
                                    var describeItems = AppUtils.ConvertXElementTo<List<ExpensesPublicUtilities>>(xmlDescribeVal);
                                    totalPrevBudget += describeItems.First().ActualPayPreviousYearAmounts;
                                    totalReqBudget += describeItems.First().RequestBudgetAmounts;
                                });
                                netPrevBudget += totalPrevBudget;
                                netReqBudget += totalReqBudget;

                                helper.SetCellCurrencyVal(string.Format("F{0}:G{0}", rowIndex), totalPrevBudget, true);
                                helper.SetCellCurrencyVal(string.Format("H{0}:I{0}", rowIndex), totalReqBudget, true);

                                rowIndex++;
                                itemIndex++;
                            }
                            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
                            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                            helper.SetCellCurrencyVal(string.Format("F{0}:G{0}", rowIndex), netPrevBudget, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
                            helper.SetCellCurrencyVal(string.Format("H{0}:I{0}", rowIndex), netReqBudget, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);


                            // เว้นระยะห่างแถวแต่ละหน่วยงาน 2 บรรทัด
                            rowIndex += 2;
                        }
                    }


                    // ค่าปิโตรเลียม
                    // "กระทำความผิดเกี่ยวกับปิโตรเลียม"
                    // "ป้องกันและปราบปรามการกระทำผิดเกี่ยวกับปิโตรเลียม"
                    // "ค่าเช่ารถยนต์สำหรับใช่ในการป้องกันและปราบปรามการกระทำความผิดเกี่ยวกับปิโตรเลียม"
                    var finalPetoleum = finalGroupByExpenses.Where(e => e.Key.FORM_TEMPLATE_NAME.Contains("ChargePetoleumForm")).ToList();
                    if (finalPetoleum.Any())
                    {
                        var restructureExpr = (from a in finalPetoleum
                                               from inner in a.Departments
                                               select new
                                               {
                                                   inner.Key.DEP_NAME,
                                                   inner.Key.PLAN_NAME,
                                                   inner.Key.PRODUCE_NAME,
                                                   inner.Key.ACTIVITY_NAME,
                                                   inner.Key.BUDGET_TYPE_NAME,
                                                   inner.Key.EXPENSES_MASTER_NAME,
                                                   inner.Key.EXPENSES_GROUP_NAME,
                                                   a.Key.EXPENSES_NAME,
                                                   EXPENSES_XML_DESCRIBEs = inner.Describes.Select(m => m.EXPENSES_XML_DESCRIBE).ToList()
                                               }).GroupBy(e => new
                                               {
                                                   e.DEP_NAME,
                                                   e.PLAN_NAME,
                                                   e.PRODUCE_NAME,
                                                   e.ACTIVITY_NAME,
                                                   e.BUDGET_TYPE_NAME,
                                                   e.EXPENSES_MASTER_NAME,
                                                   e.EXPENSES_GROUP_NAME
                                               }).Select(e => new
                                               {
                                                   e.Key,
                                                   // ในปี งปม. จะมีการทำคำขอ > 1 รายการ หรือ ทำคำขอ ค่าปิโตรเลียม มากกว่า 1 ครั้ง
                                                   Expenses = e.GroupBy(m => m.EXPENSES_NAME).Select(m => new
                                                   {
                                                       EXPENSES_NAME = m.Key,
                                                       Organizations = // ข้อมูลแต่ละ รายการ คชจ. ในแต่ละ องค์กร
                                                                       from s in m
                                                                       from xmlDescribe in s.EXPENSES_XML_DESCRIBEs
                                                                       from item in AppUtils.ConvertXElementTo<List<ExpensesChargePetoleum>>(xmlDescribe)
                                                                       where item.TotalPrice != null && item.TotalPrice.Value.CompareTo(decimal.Zero) == 1
                                                                       from sub in item.Expenses
                                                                       select new
                                                                       {
                                                                           item.OrgId,
                                                                           item.OrgName,
                                                                           item.TotalPrice,
                                                                           sub.BUDGET_TYPE_ID,
                                                                           sub.BUDGET_TYPE_NAME,
                                                                           sub.EXPENSES_MASTER_NAME,
                                                                           sub.EXPENSES_GROUP_ID,
                                                                           sub.EXPENSES_GROUP_NAME,
                                                                           sub.EXPENSES_ID,
                                                                           sub.EXPENSES_NAME,
                                                                           sub.TOTAL_REQUEST_BUDGET
                                                                       }
                                                   }).ToList()
                                               }).ToList();


                        var currWorkSheet = xlsApp.Workbook.Worksheets.Copy("TEMPLATE", "ปิโตรเลียม");
                        var helper = new ExportHelper(currWorkSheet);
                        helper.GetRange("A1").Value = helper.GetRange("A1").Text.Replace("[var_fiscal_year]", year);
                        helper.GetRange("A2").Value = helper.GetRange("A2").Text.Replace("[var_export_date]", exportDateStr);

                        Color yellowGray = ColorTranslator.FromHtml("#FFC000"),
                            yellow = ColorTranslator.FromHtml("#FFFF00");
                        int rowIndex = 4;
                        foreach (var groupItem in restructureExpr)
                        {
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "หน่วยงาน: " + groupItem.Key.DEP_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "แผนงาน: " + groupItem.Key.PLAN_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "ผลผลิต: " + groupItem.Key.PRODUCE_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), "กิจกรรม: " + groupItem.Key.ACTIVITY_NAME, false, "", true);
                            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), groupItem.Key.BUDGET_TYPE_NAME, false, "", true);
                            //if (!string.IsNullOrEmpty(groupItem.Key.EXPENSES_MASTER_NAME))
                            //    helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), groupItem.Key.EXPENSES_MASTER_NAME, false, "", true);
                            //helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex++), groupItem.Key.EXPENSES_GROUP_NAME, false, "", true);

                            // คอลัมล์
                            helper.SetCaption(string.Format("A{0}:F{1}", rowIndex, rowIndex + 1), "หมวด/รายการ");
                            helper.SetCaption(string.Format("G{0}:G{1}", rowIndex, rowIndex + 1), "กรมสรรพสามิต");
                            helper.SetCaption(string.Format("H{0}:H{1}", rowIndex, rowIndex + 1), "กรมศุลกากร");
                            helper.SetCaption(string.Format("I{0}:I{1}", rowIndex, rowIndex + 1), "กรมธุรกิจพลังงาน");
                            helper.SetCaption(string.Format("J{0}:J{1}", rowIndex, rowIndex + 1), "สำนักนโยบายและแผน");
                            helper.SetCaption(string.Format("K{0}:L{0}", rowIndex), "สำนักงานตำรวจแห่งชาติ");
                            helper.SetCaption(string.Format("M{0}:M{1}", rowIndex, rowIndex + 1), "จำนวน (บาท)");
                            rowIndex++;

                            helper.SetCaption(string.Format("K{0}", rowIndex), "ทางบก");
                            helper.SetCaption(string.Format("L{0}", rowIndex), "ทางน้ำ");
                            rowIndex++;

                            decimal totalOrg1 = decimal.Zero, totalOrg2 = decimal.Zero,
                                totalOrg3 = decimal.Zero, totalOrg4 = decimal.Zero,
                                totalOrg5 = decimal.Zero, totalOrg6 = decimal.Zero,
                                totalAll = decimal.Zero;
                            Dictionary<string, decimal?> netBudgets = new Dictionary<string, decimal?>() {
                                { "NetOrg1", decimal.Zero },
                                { "NetOrg2", decimal.Zero },
                                { "NetOrg3", decimal.Zero },
                                { "NetOrg4", decimal.Zero },
                                { "NetOrg5", decimal.Zero },
                                { "NetOrg6", decimal.Zero },
                                { "NetAll", decimal.Zero }
                            };
                            foreach (var petoleumItem in groupItem.Expenses)
                            {
                                var organizations = petoleumItem.Organizations.GroupBy(m => new { m.OrgId, m.OrgName, m.TotalPrice }).Select(m => m.Key);
                                var exprOrg1 = organizations.Where(m => m.OrgId.Equals("1")); // กรมสรรพสามิต
                                var exprOrg2 = organizations.Where(m => m.OrgId.Equals("2")); // กรมศุลกากร
                                var exprOrg3 = organizations.Where(m => m.OrgId.Equals("3")); // กรมธุรกิจพลังงาน
                                var exprOrg4 = organizations.Where(m => m.OrgId.Equals("4")); // สำนักนโยบายและแผน
                                var exprOrg5 = organizations.Where(m => m.OrgId.Equals("5")); // สำนักงานตำรวจแห่งชาติ ทางบก
                                var exprOrg6 = organizations.Where(m => m.OrgId.Equals("6")); // สำนักงานตำรวจแห่งชาติ ทางน้ำ


                                totalOrg1 = exprOrg1.Any() ? exprOrg1.Sum(m => null != m.TotalPrice ? m.TotalPrice.Value : decimal.Zero) : decimal.Zero;
                                totalOrg2 = exprOrg2.Any() ? exprOrg2.Sum(m => null != m.TotalPrice ? m.TotalPrice.Value : decimal.Zero) : decimal.Zero;
                                totalOrg3 = exprOrg3.Any() ? exprOrg3.Sum(m => null != m.TotalPrice ? m.TotalPrice.Value : decimal.Zero) : decimal.Zero;
                                totalOrg4 = exprOrg4.Any() ? exprOrg4.Sum(m => null != m.TotalPrice ? m.TotalPrice.Value : decimal.Zero) : decimal.Zero;
                                totalOrg5 = exprOrg5.Any() ? exprOrg5.Sum(m => null != m.TotalPrice ? m.TotalPrice.Value : decimal.Zero) : decimal.Zero;
                                totalOrg6 = exprOrg6.Any() ? exprOrg6.Sum(m => null != m.TotalPrice ? m.TotalPrice.Value : decimal.Zero) : decimal.Zero;
                                totalAll = organizations.Sum(m => null != m.TotalPrice ? m.TotalPrice.Value : decimal.Zero);
                                netBudgets["NetOrg1"] += totalOrg1;
                                netBudgets["NetOrg2"] += totalOrg2;
                                netBudgets["NetOrg3"] += totalOrg3;
                                netBudgets["NetOrg4"] += totalOrg4;
                                netBudgets["NetOrg5"] += totalOrg5;
                                netBudgets["NetOrg6"] += totalOrg6;
                                netBudgets["NetAll"] += totalAll;

                                // รวมรายการ ของแต่ละองค์กร กรรมสรรพสามิต....สำนักตำรวจแห่งชาติ ทางน้ำ
                                helper.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), petoleumItem.EXPENSES_NAME, true);
                                helper.SelectedExcelRange.Style.Font.Bold = true;
                                helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), totalOrg1, true);
                                helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), totalOrg2, true);
                                helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), totalOrg3, true);
                                helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), totalOrg4, true);
                                helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), totalOrg5, true);
                                helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), totalOrg6, true);
                                helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), totalAll, true);
                                var range = helper.GetRange(string.Format("G{0}:M{0}", rowIndex));
                                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(yellowGray);
                                rowIndex++;


                                var budgetTypesExpr = petoleumItem.Organizations.GroupBy(e => new { e.BUDGET_TYPE_ID, e.BUDGET_TYPE_NAME })
                                        .Select(e => new
                                        {
                                            e.Key,
                                            TotalPrice = e.Sum(g => g.TOTAL_REQUEST_BUDGET == null ? decimal.Zero : g.TOTAL_REQUEST_BUDGET.Value),
                                            Org1Budget = e.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("1") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                            Org2Budget = e.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("2") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                            Org3Budget = e.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("3") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                            Org4Budget = e.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("4") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                            Org5Budget = e.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("5") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                            Org6Budget = e.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("6") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                            OrgTotalBudget = e.Sum(g => null != g.TOTAL_REQUEST_BUDGET ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                            ExpensesGroups = e.GroupBy(m => new { m.EXPENSES_GROUP_ID, m.EXPENSES_GROUP_NAME, m.EXPENSES_MASTER_NAME })
                                                .Select(m => new
                                                {
                                                    m.Key,
                                                    TotalPrice = m.Sum(g => g.TOTAL_REQUEST_BUDGET == null ? decimal.Zero : g.TOTAL_REQUEST_BUDGET.Value),
                                                    Org1Budget = m.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("1") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                    Org2Budget = m.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("2") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                    Org3Budget = m.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("3") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                    Org4Budget = m.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("4") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                    Org5Budget = m.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("5") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                    Org6Budget = m.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("6") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                    OrgTotalBudget = m.Sum(g => null != g.TOTAL_REQUEST_BUDGET ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                    Expenses = m.GroupBy(x => new { x.EXPENSES_ID, x.EXPENSES_NAME })
                                                        .Select(x => new
                                                        {
                                                            x.Key,
                                                            Org1Budget = x.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("1") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                            Org2Budget = x.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("2") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                            Org3Budget = x.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("3") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                            Org4Budget = x.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("4") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                            Org5Budget = x.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("5") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                            Org6Budget = x.Sum(g => null != g.TOTAL_REQUEST_BUDGET && g.OrgId.Equals("6") ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero),
                                                            OrgTotalBudget = x.Sum(g => null != g.TOTAL_REQUEST_BUDGET ? g.TOTAL_REQUEST_BUDGET.Value : decimal.Zero)
                                                        }).ToList()
                                                }).ToList()
                                        }).ToList();

                                foreach (var budgetTypeItem in budgetTypesExpr)
                                {
                                    helper.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), string.Format("    {0}", budgetTypeItem.Key.BUDGET_TYPE_NAME), true);
                                    helper.SelectedExcelRange.Style.Font.Bold = true;
                                    helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), budgetTypeItem.Org1Budget, true);
                                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), budgetTypeItem.Org2Budget, true);
                                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), budgetTypeItem.Org3Budget, true);
                                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), budgetTypeItem.Org4Budget, true);
                                    helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), budgetTypeItem.Org5Budget, true);
                                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), budgetTypeItem.Org6Budget, true);
                                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), budgetTypeItem.OrgTotalBudget, true);
                                    range = helper.GetRange(string.Format("G{0}:M{0}", rowIndex));
                                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                    range.Style.Fill.BackgroundColor.SetColor(yellow);
                                    rowIndex++;

                                    foreach (var expensesGroupItem in budgetTypeItem.ExpensesGroups)
                                    {
                                        if (!string.IsNullOrEmpty(expensesGroupItem.Key.EXPENSES_MASTER_NAME))
                                        {
                                            helper.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), string.Format("        {0}", expensesGroupItem.Key.EXPENSES_MASTER_NAME), true);
                                            helper.SelectedExcelRange.Style.Font.Bold = true;
                                            helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), null, true);
                                            helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), null, true);
                                            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), null, true);
                                            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), null, true);
                                            helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), null, true);
                                            helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), null, true);
                                            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), null, true);
                                            rowIndex++;
                                        }
                                        helper.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), string.Format("        {0}", expensesGroupItem.Key.EXPENSES_GROUP_NAME), true);
                                        helper.SelectedExcelRange.Style.Font.Bold = true;
                                        helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), expensesGroupItem.Org1Budget, true);
                                        helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), expensesGroupItem.Org2Budget, true);
                                        helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), expensesGroupItem.Org3Budget, true);
                                        helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), expensesGroupItem.Org4Budget, true);
                                        helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), expensesGroupItem.Org5Budget, true);
                                        helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), expensesGroupItem.Org6Budget, true);
                                        helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), expensesGroupItem.OrgTotalBudget, true);
                                        rowIndex++;

                                        foreach (var expensesItem in expensesGroupItem.Expenses)
                                        {
                                            helper.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), string.Format("            {0}", expensesItem.Key.EXPENSES_NAME), true);
                                            helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), expensesItem.Org1Budget, true);
                                            helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), expensesItem.Org2Budget, true);
                                            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), expensesItem.Org3Budget, true);
                                            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), expensesItem.Org4Budget, true);
                                            helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), expensesItem.Org5Budget, true);
                                            helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), expensesItem.Org6Budget, true);
                                            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), expensesItem.OrgTotalBudget, true);
                                            rowIndex++;
                                        }
                                    }
                                }
                            }
                            helper.SetCellTextVal(string.Format("A{0}:F{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
                            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                            helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), netBudgets["NetOrg1"], true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
                            helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), netBudgets["NetOrg2"], true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
                            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netBudgets["NetOrg3"], true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
                            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), netBudgets["NetOrg4"], true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
                            helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), netBudgets["NetOrg5"], true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
                            helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), netBudgets["NetOrg6"], true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
                            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), netBudgets["NetAll"], true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);


                            // เว้นระยะห่างแถวแต่ละหน่วยงาน 2 บรรทัด
                            rowIndex += 2;
                        }
                    }


                    xlsApp.Workbook.Worksheets["TEMPLATE"].Hidden = eWorkSheetHidden.VeryHidden;

                    string filename = string.Format("รายงานรายละเอียด แผนรายรับ-รายจ่าย_{0}_{1}.xls", usrProps.EmpId, DateTime.Now.Ticks);
                    res["filename"] = filename;
                    xlsApp.SaveAs(new FileInfo(string.Format("{0}/{1}", appSettings.TemporaryPath, filename)));
                }
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        private class ExpensesDescribeProperty
        {
            public decimal TotalBudget { get; set; }
            public XElement ExpensesDescribe { get; set; }
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "โอนให้กระทรวงการคลังฯ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoTranferToMinistryOfFinancial(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "การคลัง";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesTranferToMinistryOfFinancial>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.TotalPrice, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าถอนคืนรายได้แผ่นดิน"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoGovernmentIncome(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ถอนคืนค่าปรับ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");
            helper.SetCaption(string.Format("G{0}:J{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesGovernmentIncome>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("G{0}:J{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:J{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "โครงการตรวจสุขภาพ (ลูกจ้างชั่วคราว)"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoHealthCheckProject(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ตรวจสุขภาพ (ลูกจ้างชั่วคราว)";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวนราย");
            helper.SetCaption(string.Format("G{0}", rowIndex), "รายละ (บาท)");
            helper.SetCaption(string.Format("H{0}", rowIndex), "จำนวนเงิน (บาท)");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesHealthCheckProject>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.TotalPrice, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าทุนการศึกษาต่อ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoEducationFund(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ทุนการศึกษาต่อ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesEducationFund>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.TotalPrice, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าจ้างที่ปรึกษา"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoHireAdvisor(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "จ้างที่ปรึกษา";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesHireAdvisor>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.TotalPrice, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าใช้จ่ายในการอบรมต่างประเทศ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoExpensesSeminaForeign(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "อบรมต่างประเทศประเทศ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesSeminaForeign>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.TotalPrice, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าใช้จ่ายในการประชุมต่างประเทศ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoExpensesConferenceForeign(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ประชุมต่างประเทศ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesConferenceForeign>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.TotalPrice, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าใช้จ่ายในการจัดพิมพ์แสตมป์สรรพสามิต"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForPrintStamp(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "จัดพิมพ์แสตมป์";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ (ประเภทแสตมป์)");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (ดวง)");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("H{0}", rowIndex), "รวม (บาท)");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForPrintStamp>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.TotalPrice, true);

                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "เงินอุดหนุนสำหรับค่าสินบนและเงินรางวัล สุรา ยาสูบ ไพ่ แสตมป์สรรพสามิตและ พ.ร.บ. ภาษีสรรพสามิต พ.ศ. 2527"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoSubsidy(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "อุดหนุน";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");
            helper.SetCaption(string.Format("G{0}:K{0}", rowIndex), "หมายเหตุ");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesSubsidy>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("G{0}:K{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:K{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ที่ดินและสิ่งก่อสร้าง"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoLandAndBuilding(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ที่ดินและสิ่งก่อสร้าง";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}:I{0}", rowIndex), "โครงการ");
            helper.SetCaption(string.Format("J{0}", rowIndex), "จำนวน (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesLandAndBuilding>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellTextVal(string.Format("F{0}:I{0}", rowIndex), describeItem.ProjectName, true);
                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), describeItem.TotalPrice, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:I{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ครุภัณฑ์อื่นๆ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoOtherEquipment(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ครุภัณฑ์อื่นๆ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "ชื่อรายการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "คุณลักษณะของครุภัณฑ์ (Spec)");
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "กรอบครุภัณฑ์");
            helper.SetCaption(string.Format("J{0}:K{0}", rowIndex), "วัตถุประสงค์ในการขอ");
            helper.SetCaption(string.Format("L{0}:L{1}", rowIndex, rowIndex + 1), "ราคาต่อหน่วย/ชุด (บาท)");
            helper.SetCaption(string.Format("M{0}:M{1}", rowIndex, rowIndex + 1), "ราคารวม (บาท)");
            helper.SetCaption(string.Format("N{0}:Q{1}", rowIndex, rowIndex + 1), "ปัญหา อุปสรรค เหตุผลจำเป็นที่ต้องจัดหาครุภัณฑ์นี้");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ราคามาตรฐานครุภัณฑ์");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ไม่กำหนดราคามาตรฐานครุภัณฑ์");

            helper.SetCaption(string.Format("H{0}", rowIndex), "ความต้องการ");
            helper.SetCaption(string.Format("I{0}", rowIndex), "จำนวนที่มีอยู่");

            helper.SetCaption(string.Format("J{0}", rowIndex), "เพิ่มเติม");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ทดแทนของเดิม");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesOtherEquipment>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellTextValByFontName(string.Format("F{0}", rowIndex), "1".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellTextValByFontName(string.Format("G{0}", rowIndex), "0".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), describeItem.RequiredAmounts, true);
                    helper.SetCellIntVal(string.Format("I{0}", rowIndex), describeItem.StockAmounts, true);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), describeItem.RequestAmounts, true);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), describeItem.ReplaceAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:L{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ครุภัณฑ์วิทยาศาสตร์"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoScienceEquipment(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ครุภัณฑ์วิทยาศาสตร์";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "ชื่อรายการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "คุณลักษณะของครุภัณฑ์ (Spec)");
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "กรอบครุภัณฑ์");
            helper.SetCaption(string.Format("J{0}:K{0}", rowIndex), "วัตถุประสงค์ในการขอ");
            helper.SetCaption(string.Format("L{0}:L{1}", rowIndex, rowIndex + 1), "ราคาต่อหน่วย/ชุด (บาท)");
            helper.SetCaption(string.Format("M{0}:M{1}", rowIndex, rowIndex + 1), "ราคารวม (บาท)");
            helper.SetCaption(string.Format("N{0}:Q{1}", rowIndex, rowIndex + 1), "ปัญหา อุปสรรค เหตุผลจำเป็นที่ต้องจัดหาครุภัณฑ์นี้");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ราคามาตรฐานครุภัณฑ์");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ไม่กำหนดราคามาตรฐานครุภัณฑ์");

            helper.SetCaption(string.Format("H{0}", rowIndex), "ความต้องการ");
            helper.SetCaption(string.Format("I{0}", rowIndex), "จำนวนที่มีอยู่");

            helper.SetCaption(string.Format("J{0}", rowIndex), "เพิ่มเติม");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ทดแทนของเดิม");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesScienceEquipment>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellTextValByFontName(string.Format("F{0}", rowIndex), "1".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellTextValByFontName(string.Format("G{0}", rowIndex), "0".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), describeItem.RequiredAmounts, true);
                    helper.SetCellIntVal(string.Format("I{0}", rowIndex), describeItem.StockAmounts, true);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), describeItem.RequestAmounts, true);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), describeItem.ReplaceAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:L{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ครุภัณฑ์ยานพาหนะและขนส่ง"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoVehicleAndTransportEquipment(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ครุภัณฑ์ยานพาหนะ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "ชื่อรายการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "คุณลักษณะของครุภัณฑ์ (Spec)");
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "กรอบครุภัณฑ์");
            helper.SetCaption(string.Format("J{0}:K{0}", rowIndex), "วัตถุประสงค์ในการขอ");
            helper.SetCaption(string.Format("L{0}:L{1}", rowIndex, rowIndex + 1), "ราคาต่อหน่วย/ชุด (บาท)");
            helper.SetCaption(string.Format("M{0}:M{1}", rowIndex, rowIndex + 1), "ราคารวม (บาท)");
            helper.SetCaption(string.Format("N{0}:Q{1}", rowIndex, rowIndex + 1), "ปัญหา อุปสรรค เหตุผลจำเป็นที่ต้องจัดหาครุภัณฑ์นี้");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ราคามาตรฐานครุภัณฑ์");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ไม่กำหนดราคามาตรฐานครุภัณฑ์");

            helper.SetCaption(string.Format("H{0}", rowIndex), "ความต้องการ");
            helper.SetCaption(string.Format("I{0}", rowIndex), "จำนวนที่มีอยู่");

            helper.SetCaption(string.Format("J{0}", rowIndex), "เพิ่มเติม");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ทดแทนของเดิม");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesVehicleAndTransportEquipment>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellTextValByFontName(string.Format("F{0}", rowIndex), "1".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellTextValByFontName(string.Format("G{0}", rowIndex), "0".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), describeItem.RequiredAmounts, true);
                    helper.SetCellIntVal(string.Format("I{0}", rowIndex), describeItem.StockAmounts, true);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), describeItem.RequestAmounts, true);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), describeItem.ReplaceAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:L{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ครุภัณฑ์คอมพิวเตอร์"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoComputerEquipment(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ครุภัณฑ์คอมพิวเตอร์";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "ชื่อรายการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "คุณลักษณะของครุภัณฑ์ (Spec)");
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "กรอบครุภัณฑ์");
            helper.SetCaption(string.Format("J{0}:K{0}", rowIndex), "วัตถุประสงค์ในการขอ");
            helper.SetCaption(string.Format("L{0}:L{1}", rowIndex, rowIndex + 1), "ราคาต่อหน่วย/ชุด (บาท)");
            helper.SetCaption(string.Format("M{0}:M{1}", rowIndex, rowIndex + 1), "ราคารวม (บาท)");
            helper.SetCaption(string.Format("N{0}:Q{1}", rowIndex, rowIndex + 1), "ปัญหา อุปสรรค เหตุผลจำเป็นที่ต้องจัดหาครุภัณฑ์นี้");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ราคามาตรฐานครุภัณฑ์");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ไม่กำหนดราคามาตรฐานครุภัณฑ์");

            helper.SetCaption(string.Format("H{0}", rowIndex), "ความต้องการ");
            helper.SetCaption(string.Format("I{0}", rowIndex), "จำนวนที่มีอยู่");

            helper.SetCaption(string.Format("J{0}", rowIndex), "เพิ่มเติม");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ทดแทนของเดิม");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesComputerEquipment>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellTextValByFontName(string.Format("F{0}", rowIndex), "1".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellTextValByFontName(string.Format("G{0}", rowIndex), "0".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), describeItem.RequiredAmounts, true);
                    helper.SetCellIntVal(string.Format("I{0}", rowIndex), describeItem.StockAmounts, true);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), describeItem.RequestAmounts, true);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), describeItem.ReplaceAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:L{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ครุภัณฑ์สำนักงาน"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoOfficialEquipment(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ครุภัณฑ์สำนักงาน";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "ชื่อรายการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "คุณลักษณะของครุภัณฑ์ (Spec)");
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "กรอบครุภัณฑ์");
            helper.SetCaption(string.Format("J{0}:K{0}", rowIndex), "วัตถุประสงค์ในการขอ");
            helper.SetCaption(string.Format("L{0}:L{1}", rowIndex, rowIndex + 1), "ราคาต่อหน่วย/ชุด (บาท)");
            helper.SetCaption(string.Format("M{0}:M{1}", rowIndex, rowIndex + 1), "ราคารวม (บาท)");
            helper.SetCaption(string.Format("N{0}:Q{1}", rowIndex, rowIndex + 1), "ปัญหา อุปสรรค เหตุผลจำเป็นที่ต้องจัดหาครุภัณฑ์นี้");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ราคามาตรฐานครุภัณฑ์");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ไม่กำหนดราคามาตรฐานครุภัณฑ์");

            helper.SetCaption(string.Format("H{0}", rowIndex), "ความต้องการ");
            helper.SetCaption(string.Format("I{0}", rowIndex), "จำนวนที่มีอยู่");

            helper.SetCaption(string.Format("J{0}", rowIndex), "เพิ่มเติม");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ทดแทนของเดิม");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesOfficialEquipment>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellTextValByFontName(string.Format("F{0}", rowIndex), "1".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellTextValByFontName(string.Format("G{0}", rowIndex), "0".Equals(describeItem.PriceType) ? "ü" : "", true, "Wingdings");
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), describeItem.RequiredAmounts, true);
                    helper.SetCellIntVal(string.Format("I{0}", rowIndex), describeItem.StockAmounts, true);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), describeItem.RequestAmounts, true);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), describeItem.ReplaceAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:L{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("N{0}:Q{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าสาธารณูปโภค"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoPublicUtilities(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, int fiscalYear, ref int rowIndex)
        {
            //helper.CurrWorkSheet.Name = "ค่าสาธารณูปโภค";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), string.Format("จ่ายจริงปีก่อนหน้า ปี {0} (บาท)", fiscalYear - 1 + 543));
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "คำของบประมาณ (บาท)");

            decimal? netPrevBudget = decimal.Zero, netReqBudget = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesPublicUtilities>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}:G{0}", rowIndex), describeItem.ActualPayPreviousYearAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}:I{0}", rowIndex), describeItem.RequestBudgetAmounts, true);

                    netPrevBudget += describeItem.ActualPayPreviousYearAmounts;
                    netReqBudget += describeItem.RequestBudgetAmounts;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}:G{0}", rowIndex), netPrevBudget, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("H{0}:I{0}", rowIndex), netReqBudget, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าวัสดุอื่นๆ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForMaterialOther(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "วัสดุอื่นๆ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "หน่วยนับ");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("I{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("J{0}:N{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForMaterialOther>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellTextVal(string.Format("G{0}", rowIndex), describeItem.UnitName, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), describeItem.RemarkText, true);


                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าวัสดุยานพาหนะและขนส่ง"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoVehicleEquipmentAndTransport(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "วัสดุยานพาหนะ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "หน่วยนับ");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("I{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("J{0}:N{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesVehicleEquipmentAndTransport>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellTextVal(string.Format("G{0}", rowIndex), describeItem.UnitName, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), describeItem.RemarkText, true);


                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าวัสดุคอมพิวเตอร์"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoComputerMaterial(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "วัสดุคอมพิวเตอร์";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "หน่วยนับ");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("I{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("J{0}:N{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesComputerMaterial>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellTextVal(string.Format("G{0}", rowIndex), describeItem.UnitName, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), describeItem.RemarkText, true);


                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าวัสดุวิทยาศาสตร์หรือการแพทย์"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForScienceAndMedical(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "วัสดุวิทยาศาสตร์";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "หน่วยนับ");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("I{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("J{0}:N{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForScienceAndMedical>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellTextVal(string.Format("G{0}", rowIndex), describeItem.UnitName, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), describeItem.RemarkText, true);


                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าวัสดุไฟฟ้าและวิทยุ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForElectricalAndRadio(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "วัสดุไฟฟ้า";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "หน่วยนับ");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("I{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("J{0}:N{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForElectricalAndRadio>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellTextVal(string.Format("G{0}", rowIndex), describeItem.UnitName, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), describeItem.RemarkText, true);


                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าวัสดุเชื้อเพลิงและหล่อลื่น"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForOilAndLubricate(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "วัสดุเชื้อเพลิง";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "ประเภทยานพาหนะ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (คัน)");
            helper.SetCaption(string.Format("G{0}", rowIndex), "จำนวนวัน");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ลิตร/คัน/เดือน");
            helper.SetCaption(string.Format("I{0}", rowIndex), "ราคา/ลิตร (บาท)");
            helper.SetCaption(string.Format("J{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ต่อปี (บาท)");

            int? netVehicleAmount = 0, netDays = 0, netLiter = 0;
            decimal? netPrice = decimal.Zero, netYearlyPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForOilAndLubricate>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.VehicleTypeName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.VehicleAmounts, true);
                    helper.SetCellIntVal(string.Format("G{0}", rowIndex), describeItem.AmountDays, true);
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), describeItem.LiterAmountsPerMonth, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.PricePerLiter, true);
                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), describeItem.TotalYearlyPrice, true);

                    netVehicleAmount += describeItem.VehicleAmounts;
                    netDays += describeItem.AmountDays;
                    netLiter += describeItem.LiterAmountsPerMonth;
                    netPrice += describeItem.TotalPrice;
                    netYearlyPrice += describeItem.TotalYearlyPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netVehicleAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("G{0}", rowIndex), netDays, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("H{0}", rowIndex), netLiter, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("I{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), netYearlyPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าวัสดุสำนักงาน"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForMaterialOfficial(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าวัสดุสำนักงาน";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "หน่วยนับ");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("I{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("J{0}:N{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForMaterialOfficial>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellTextVal(string.Format("G{0}", rowIndex), describeItem.UnitName, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), describeItem.RemarkText, true);

                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("J{0}:N{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าธรรมเนียมศาล"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoCourtFee(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าธรรมเนียมศาล";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "ชื่อคดี");
            helper.SetCaption(string.Format("F{0}", rowIndex), "ค่าธรรมเนียม (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesCourtFee>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ItemText, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.FeeAmount, true);

                    netPrice += describeItem.FeeAmount;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าใช้สอยอื่น"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForOther(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าใช้สอยอื่น";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}:I{0}", rowIndex), "โครงการ");
            helper.SetCaption(string.Format("J{0}", rowIndex), "จำนวน (หน่วย)");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("L{0}", rowIndex), "รวมทั้งสิ้น (บาท)");
            helper.SetCaption(string.Format("M{0}:Q{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForOther>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellTextVal(string.Format("F{0}:I{0}", rowIndex), describeItem.ProjectName, true);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("M{0}:Q{0}", rowIndex), describeItem.RemarkText, true);

                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:I{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("J{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("K{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("M{0}:Q{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าใช้จ่ายในการบำรุงรักษาเครื่องปรับอากาศ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForMaintainAirConditioner(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "เครื่องปรับอากาศ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (เครื่อง)");
            helper.SetCaption(string.Format("G{0}", rowIndex), "อัตรา (บาท)");
            helper.SetCaption(string.Format("H{0}", rowIndex), "รวมทั้งสิ้น (บาท)");
            helper.SetCaption(string.Format("I{0}:N{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForMaintainAirConditioner>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ServiceName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("I{0}:N{0}", rowIndex), describeItem.RemarkText, true);

                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("I{0}:N{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าโฆษณาและเผยแพร่"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForAdvertiseAndRelease(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าโฆษณาและเผยแพร่";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "หน่วยนับ");
            helper.SetCaption(string.Format("H{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("I{0}", rowIndex), "ราคา/หน่วย (บาท)");
            helper.SetCaption(string.Format("J{0}", rowIndex), "รวม (บาท)");
            helper.SetCaption(string.Format("K{0}:O{0}", rowIndex), "หมายเหตุ");

            int? netAmount = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForAdvertiseAndRelease>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetName, true);
                    helper.SetCellTextVal(string.Format("F{0}:G{0}", rowIndex), describeItem.UnitName, true);
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), describeItem.Amounts, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.PricePerUnit, true);
                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), describeItem.TotalPrice, true);
                    helper.SetCellTextVal(string.Format("K{0}:O{0}", rowIndex), describeItem.RemarkText, true);

                    netAmount += describeItem.Amounts;
                    netPrice += describeItem.TotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("H{0}", rowIndex), netAmount, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("I{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("K{0}:O{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าใช้จ่ายในการสัมมนาและฝึกอบรม"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoTraningAndSeminors(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "อบรมสัมมนา";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "โครงการ/หลักสูตร/รายการ-กิจกรรม");
            helper.SetCaption(string.Format("F{0}:F{1}", rowIndex, rowIndex + 1), "ครั้ง/รุ่น");
            helper.SetCaption(string.Format("G{0}:K{0}", rowIndex), "จำนวนคน");
            helper.SetCaption(string.Format("L{0}:M{0}", rowIndex), "จำนวน/หน่วยนับ");
            helper.SetCaption(string.Format("N{0}:O{0}", rowIndex), "อัตราค่าใช้จ่ายที่ตั้ง");
            helper.SetCaption(string.Format("P{0}:P{1}", rowIndex, rowIndex + 1), "รวมเงิน (บาท)");
            helper.SetCaption(string.Format("Q{0}:R{0}", rowIndex), "สถานที่ดำเนินการ");
            helper.SetCaption(string.Format("S{0}:Z{1}", rowIndex, rowIndex + 1), "คำชี้แจง (เหตุผลความจำเป็น และผลประโยชน์ที่จะได้รับ)");

            rowIndex++;
            helper.SetCaption(string.Format("G{0}", rowIndex), "ประเภท ก");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ประเภท ข");
            helper.SetCaption(string.Format("I{0}", rowIndex), "บุคคลภายนอก");
            helper.SetCaption(string.Format("J{0}", rowIndex), "คณะผู้จัด");
            helper.SetCaption(string.Format("K{0}", rowIndex), "รวม");

            helper.SetCaption(string.Format("L{0}", rowIndex), "จำนวน");
            helper.SetCaption(string.Format("M{0}", rowIndex), "หน่วยนับ");

            helper.SetCaption(string.Format("N{0}", rowIndex), "อัตราค่าใช้จ่าย (บาท)");
            helper.SetCaption(string.Format("O{0}", rowIndex), "หน่วยนับ");

            helper.SetCaption(string.Format("Q{0}", rowIndex), "ราชการ");
            helper.SetCaption(string.Format("R{0}", rowIndex), "เอกชน");

            int? netTime = 0, netTypeA = 0, netTypeB = 0, netGuest = 0, netStaff = 0, netPersons = 0, netUnitAmounts = 0;
            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesTraningAndSeminors>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    int? totalTime = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.TimeAmounts)),
                        totalTypeA = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.TypeAPersonAmounts)),
                        totalTypeB = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.TypeBPersonAmounts)),
                        totalGuest = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.GuestAmounts)),
                        totalStaff = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.StaffAmounts)),
                        totalPersons = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.TotalPersonAmounts)),
                        totalUnitAmount = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.UnitAmounts));
                    decimal? totalPrice = describeItem.Activities.Sum(x => x.Expenses.Sum(s => s.TotalPrice));

                    netTime += totalTime;
                    netTypeA += totalTypeA;
                    netTypeB += totalTypeB;
                    netGuest += totalGuest;
                    netStaff += totalStaff;
                    netPersons += totalPersons;
                    netUnitAmounts += totalUnitAmount;
                    netPrice += totalPrice;

                    int itemCounts = describeItem.Activities.Sum(x => x.Expenses.Count() + 1);
                    helper.SetCellTextVal(string.Format("A{0}:A{1}", rowIndex, rowIndex + itemCounts), itemIndex.ToString(), true, "", true);
                    helper.SelectedExcelRange.Style.Font.Size = 18;
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ProjectName, true, "", true);
                    helper.SelectedExcelRange.Style.Font.Size = 18;
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), totalTime, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("G{0}", rowIndex), totalTypeA, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("H{0}", rowIndex), totalTypeB, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("I{0}", rowIndex), totalGuest, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), totalStaff, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), totalPersons, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("L{0}", rowIndex), totalUnitAmount, true, helper.GroupHtmlColorCode);
                    helper.SetCellTextVal(string.Format("M{0}:O{0}", rowIndex), string.Empty, true, helper.GroupHtmlColorCode);
                    helper.SetCellCurrencyVal(string.Format("P{0}", rowIndex), totalPrice, true, helper.CurrencyNumberFormat, helper.GroupHtmlColorCode);
                    helper.SetCellTextValByFontName(string.Format("Q{0}:Q{1}", rowIndex, rowIndex + itemCounts), "1".Equals(describeItem.PlaceTypeFlag) ? "ü" : string.Empty, true, "Wingdings");
                    helper.SetCellTextValByFontName(string.Format("R{0}:R{1}", rowIndex, rowIndex + itemCounts), "2".Equals(describeItem.PlaceTypeFlag) ? "ü" : string.Empty, true, "Wingdings");
                    helper.SetCellTextVal(string.Format("S{0}:Z{1}", rowIndex, rowIndex + itemCounts), describeItem.RemarkText, true);

                    rowIndex++;
                    int activityIndex = 1;
                    foreach (var activityItem in describeItem.Activities)
                    {
                        totalTime = activityItem.Expenses.Sum(s => s.TimeAmounts);
                        totalTypeA = activityItem.Expenses.Sum(s => s.TypeAPersonAmounts);
                        totalTypeB = activityItem.Expenses.Sum(s => s.TypeBPersonAmounts);
                        totalGuest = activityItem.Expenses.Sum(s => s.GuestAmounts);
                        totalStaff = activityItem.Expenses.Sum(s => s.StaffAmounts);
                        totalPersons = activityItem.Expenses.Sum(s => s.TotalPersonAmounts);
                        totalUnitAmount = activityItem.Expenses.Sum(s => s.UnitAmounts);
                        totalPrice = activityItem.Expenses.Sum(s => s.TotalPrice);

                        helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), string.Format("    {0}. {1}", activityIndex++, activityItem.ActivityName), true, "", true);
                        helper.SetCellIntVal(string.Format("F{0}", rowIndex), totalTime, true, helper.OddHtmlColorCode);
                        helper.SetCellIntVal(string.Format("G{0}", rowIndex), totalTypeA, true, helper.OddHtmlColorCode);
                        helper.SetCellIntVal(string.Format("H{0}", rowIndex), totalTypeB, true, helper.OddHtmlColorCode);
                        helper.SetCellIntVal(string.Format("I{0}", rowIndex), totalGuest, true, helper.OddHtmlColorCode);
                        helper.SetCellIntVal(string.Format("J{0}", rowIndex), totalStaff, true, helper.OddHtmlColorCode);
                        helper.SetCellIntVal(string.Format("K{0}", rowIndex), totalPersons, true, helper.OddHtmlColorCode);
                        helper.SetCellIntVal(string.Format("L{0}", rowIndex), totalUnitAmount, true, helper.OddHtmlColorCode);
                        helper.SetCellTextVal(string.Format("M{0}:O{0}", rowIndex), string.Empty, true, helper.OddHtmlColorCode);
                        helper.SetCellCurrencyVal(string.Format("P{0}", rowIndex), totalPrice, true, helper.CurrencyNumberFormat, helper.OddHtmlColorCode);
                        helper.GetRange(string.Format("B{0}:P{0}", rowIndex)).Style.Font.Bold = true;

                        rowIndex++;
                        foreach (var expensesItem in activityItem.Expenses)
                        {
                            helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), string.Format("        {0}", expensesItem.TraningAndSeminorsName), true, "", true);
                            helper.SetCellIntVal(string.Format("F{0}", rowIndex), expensesItem.TimeAmounts, true);
                            helper.SetCellIntVal(string.Format("G{0}", rowIndex), expensesItem.TypeAPersonAmounts, true);
                            helper.SetCellIntVal(string.Format("H{0}", rowIndex), expensesItem.TypeBPersonAmounts, true);
                            helper.SetCellIntVal(string.Format("I{0}", rowIndex), expensesItem.GuestAmounts, true);
                            helper.SetCellIntVal(string.Format("J{0}", rowIndex), expensesItem.StaffAmounts, true);
                            helper.SetCellIntVal(string.Format("K{0}", rowIndex), expensesItem.TotalPersonAmounts, true);
                            helper.SetCellIntVal(string.Format("L{0}", rowIndex), expensesItem.UnitAmounts, true);
                            helper.SetCellTextVal(string.Format("M{0}", rowIndex), expensesItem.UnitName, true);
                            helper.SetCellCurrencyVal(string.Format("N{0}", rowIndex), expensesItem.CompensationPrice, true);
                            helper.SetCellTextVal(string.Format("O{0}", rowIndex), expensesItem.CompensationUnitName, true);
                            helper.SetCellCurrencyVal(string.Format("P{0}", rowIndex), expensesItem.TotalPrice, true, helper.CurrencyNumberFormat);
                            rowIndex++;
                        }

                    }

                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netTime, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("G{0}", rowIndex), netTypeA, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("H{0}", rowIndex), netTypeB, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("I{0}", rowIndex), netGuest, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("J{0}", rowIndex), netStaff, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("K{0}", rowIndex), netPersons, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("L{0}", rowIndex), netUnitAmounts, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("M{0}:O{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("P{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("Q{0}:Z{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าจ้างเหมาบริการอื่นๆ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForRentService(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = " ค่าจ้างเหมาบริการอื่นๆ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForRentService>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ServiceName, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.ServicePrice, true);

                    netPrice += describeItem.ServicePrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าจ้างพิมพ์แก้ไขแสตมป์ (ค่าจ้างเหมา)"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForReprintStamp(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "พิมพ์แก้ไขแสตมป์";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForReprintStamp>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ServiceName, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.ServicePrice, true);

                    netPrice += describeItem.ServicePrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าจ้างเหมาบริการ บำรุงรักษาระบบ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForSoftwareMA(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "MA";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "รายการ");
            helper.SetCaption(string.Format("D{0}:D{1}", rowIndex, rowIndex + 1), "เลขที่สัญญา");
            helper.SetCaption(string.Format("E{0}:E{1}", rowIndex, rowIndex + 1), "วันที่ของสัญญา");
            helper.SetCaption(string.Format("F{0}:F{1}", rowIndex, rowIndex + 1), "วันที่รับมอบงาน");
            helper.SetCaption(string.Format("G{0}:G{1}", rowIndex, rowIndex + 1), "วงเงินตามสัญญา");
            helper.SetCaption(string.Format("H{0}:H{1}", rowIndex, rowIndex + 1), "ปีที่บำรุงรักษา");
            helper.SetCaption(string.Format("I{0}:I{1}", rowIndex, rowIndex + 1), "จำนวน (บาท)");
            helper.SetCaption(string.Format("J{0}:L{0}", rowIndex), "ประเภทค่าจ้างเหมา)");
            rowIndex++;
            helper.SetCaption(string.Format("J{0}", rowIndex), "ค่าบำรุงรักษาระบบคอมพิวเตอร์");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ค่าบำรุงรักษาอาคาร");
            helper.SetCaption(string.Format("L{0}", rowIndex), "ค่าบำรุงรักษาอื่นๆ");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItem = AppUtils.ConvertXElementTo<ExpensesChargeForSoftwareMA>(item.ExpensesDescribe);

                // ค่าบำรุงรักษาโปรแกรมคอมพิวเตอร์
                if (null != describeItem.MASoftware)
                    foreach (var subItem in describeItem.MASoftware.Items)
                    {
                        helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                        helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), subItem.ServiceName, true);
                        helper.SetCellTextVal(string.Format("D{0}", rowIndex), subItem.ContractNumber, true);
                        helper.SetCellTextVal(string.Format("E{0}", rowIndex), subItem.ContractDateStr, true);
                        helper.SetCellTextVal(string.Format("F{0}", rowIndex), subItem.VaranteeExpireDateStr, true);
                        helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), subItem.ContractPrice, true);
                        helper.SetCellIntVal(string.Format("H{0}", rowIndex), subItem.ServiceBeginYear, true);
                        helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), subItem.ServicePrice, true);

                        helper.SetCellTextValByFontName(string.Format("J{0}", rowIndex), "ü", true, "Wingdings");
                        helper.SetCellTextVal(string.Format("K{0}", rowIndex), string.Empty, true);
                        helper.SetCellTextVal(string.Format("L{0}", rowIndex), string.Empty, true);

                        netPrice += subItem.ServicePrice;
                        itemIndex++;
                        rowIndex++;
                    }


                // ค่าบำรุงรักษาอาคาร
                if (null != describeItem.MABuilding)
                    foreach (var subItem in describeItem.MABuilding.Items)
                    {
                        helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                        helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), subItem.ServiceName, true);
                        helper.SetCellTextVal(string.Format("D{0}", rowIndex), subItem.ContractNumber, true);
                        helper.SetCellTextVal(string.Format("E{0}", rowIndex), subItem.ContractDateStr, true);
                        helper.SetCellTextVal(string.Format("F{0}", rowIndex), subItem.VaranteeExpireDateStr, true);
                        helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), subItem.ContractPrice, true);
                        helper.SetCellIntVal(string.Format("H{0}", rowIndex), subItem.ServiceBeginYear, true);
                        helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), subItem.ServicePrice, true);

                        helper.SetCellTextVal(string.Format("J{0}", rowIndex), string.Empty, true);
                        helper.SetCellTextValByFontName(string.Format("K{0}", rowIndex), "ü", true, "Wingdings");
                        helper.SetCellTextVal(string.Format("L{0}", rowIndex), string.Empty, true);

                        netPrice += subItem.ServicePrice;
                        itemIndex++;
                        rowIndex++;
                    }

                // ค่าบำรุงรักษาอื่นๆ
                if (null != describeItem.MAOther)
                    foreach (var subItem in describeItem.MAOther.Items)
                    {
                        helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                        helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), subItem.ServiceName, true);
                        helper.SetCellTextVal(string.Format("D{0}", rowIndex), subItem.ContractNumber, true);
                        helper.SetCellTextVal(string.Format("E{0}", rowIndex), subItem.ContractDateStr, true);
                        helper.SetCellTextVal(string.Format("F{0}", rowIndex), subItem.VaranteeExpireDateStr, true);
                        helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), subItem.ContractPrice, true);
                        helper.SetCellIntVal(string.Format("H{0}", rowIndex), subItem.ServiceBeginYear, true);
                        helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), subItem.ServicePrice, true);

                        helper.SetCellTextVal(string.Format("J{0}", rowIndex), string.Empty, true);
                        helper.SetCellTextVal(string.Format("K{0}", rowIndex), string.Empty, true);
                        helper.SetCellTextValByFontName(string.Format("L{0}", rowIndex), "ü", true, "Wingdings");

                        netPrice += subItem.ServicePrice;
                        itemIndex++;
                        rowIndex++;
                    }
            }
            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("J{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellTextVal(string.Format("K{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellTextVal(string.Format("L{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่ากำจัดปลวก (เหมาบริการ)"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForKillTermite(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่ากำจัดปลวก";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวน (บาท)");
            helper.SetCaption(string.Format("G{0}:H{0}", rowIndex), "หมายเหตุ");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForKillTermite>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.ServiceName, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.ServicePrice, true);
                    helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), describeItem.RemarkText, true);

                    netPrice += describeItem.ServicePrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("G{0}:H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าจ้างเหมาบริการ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForHireService(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าจ้างเหมาบริการ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "รายการ");
            helper.SetCaption(string.Format("D{0}:D{1}", rowIndex, rowIndex + 1), "จำนวน (คน)");
            helper.SetCaption(string.Format("E{0}:E{1}", rowIndex, rowIndex + 1), "อัตรา/คน/เดือน (บาท) ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "อัตรา (บาท)");
            helper.SetCaption(string.Format("H{0}:I{1}", rowIndex, rowIndex + 1), "หมายเหตุ");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ต่อเดือน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ต่อปี");

            int? netPersonAmounts = 0;
            decimal? monthlyVal = decimal.Zero, yearlyVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForHireService>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), describeItem.ServiceName, true);
                    helper.SetCellIntVal(string.Format("D{0}", rowIndex), describeItem.PersonAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("E{0}", rowIndex), describeItem.ServicePrice, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.ServiceMonthlyPrice, true);
                    helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), describeItem.ServiceYearlyPrice, true);
                    helper.SetCellTextVal(string.Format("H{0}:I{0}", rowIndex), describeItem.RemarkText, true);

                    netPersonAmounts += describeItem.PersonAmounts;
                    monthlyVal += describeItem.ServiceMonthlyPrice;
                    yearlyVal += describeItem.ServiceYearlyPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:C{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("D{0}", rowIndex), netPersonAmounts, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("E{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), monthlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), yearlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("H{0}:I{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าเช่าทรัพย์สิน"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoChargeForRentBuilding(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าเช่าทรัพย์สิน";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "รายการค่าเช่าทรัพย์สิน");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "อัตราค่าเช่า (บาท)");
            helper.SetCaption(string.Format("H{0}:I{1}", rowIndex, rowIndex + 1), "หมายเหตุ");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ต่อเดือน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ต่อปี");

            decimal? monthlyVal = decimal.Zero, yearlyVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesChargeForRentBuilding>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.AssetOtherName ?? describeItem.AssetName, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.RentMonthlyPrice, true);
                    helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), describeItem.RentYearlyPrice, true);
                    helper.SetCellTextVal(string.Format("H{0}:I{0}", rowIndex), describeItem.RemarkText, true);

                    monthlyVal += describeItem.RentMonthlyPrice;
                    yearlyVal += describeItem.RentYearlyPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), monthlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), yearlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("H{0}:I{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode, true);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าซ่อมแซมสิ่งก่อสร้าง"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoRepairBuilding(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ซ่อมแซมสิ่งก่อสร้าง";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการสิ่งก่อสร้าง");
            helper.SetCaption(string.Format("F{0}", rowIndex), "ค่าซ่อมแซม (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesRepairBuilding>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.BuildingOtherName ?? describeItem.BuildingName, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.RepairPrice, true);

                    netPrice += describeItem.RepairPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าซ่อมแซมครุภัณฑ์"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoRepairEquipment(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ซ่อมแซมครุภัณฑ์";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{0}", rowIndex), "รายการ");
            helper.SetCaption(string.Format("F{0}", rowIndex), "จำนวนทั้งหมดที่มีอยู่");
            helper.SetCaption(string.Format("G{0}", rowIndex), "จำนวนที่ขอซ่อม");
            helper.SetCaption(string.Format("H{0}", rowIndex), "ราคาซ่อมโดยเฉลี่ย/เครื่อง");
            helper.SetCaption(string.Format("I{0}", rowIndex), "รวมทั้งสิ้น (บาท)");

            decimal? netPrice = decimal.Zero;
            int? netStockAmounts = 0, netRepairAmounts = 0;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesRepairEquipment>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{0}", rowIndex), describeItem.EquipmentOtherName ?? describeItem.EquipmentName, true);
                    helper.SetCellIntVal(string.Format("F{0}", rowIndex), describeItem.StockAmounts, true);
                    helper.SetCellIntVal(string.Format("G{0}", rowIndex), describeItem.RepairAmounts, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.RepairPricePerItem, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.RepairTotalPrice, true);

                    netStockAmounts += describeItem.StockAmounts;
                    netRepairAmounts += describeItem.RepairAmounts;
                    netPrice = describeItem.RepairTotalPrice;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("F{0}", rowIndex), netStockAmounts, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("G{0}", rowIndex), netRepairAmounts, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("H{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าซ่อมแซมยานพาหนะและขนส่ง"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoRepairVehicleAndTransport(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ซ่อมแซมยานพาหนะ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}", rowIndex), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:D{0}", rowIndex), "ประเภทยานพาหนะ");
            helper.SetCaption(string.Format("E{0}", rowIndex), "ทะเบียนยานพาหนะราชการ");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "วัน/เดือน/ปี จดทะเบียน");
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "อายุการใช้งาน (ปี/เดือน)");
            helper.SetCaption(string.Format("J{0}", rowIndex), "ราคาซ่อมโดยเฉลี่ย (บาท)");

            decimal? netPrice = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesRepairVehicleAndTransport>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    var countItems = describeItem.Items.Count;
                    decimal? totalPrice = describeItem.Items.Sum(e => e.ExpensesRepairPrice);
                    netPrice += totalPrice;

                    helper.SetCellTextVal(string.Format("A{0}:A{1}", rowIndex, rowIndex + countItems), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:D{1}", rowIndex, rowIndex + countItems), describeItem.VehicleTypeName, true);
                    helper.SetCellTextVal(string.Format("E{0}:I{0}", rowIndex), string.Empty, true);
                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), totalPrice, true, helper.CurrencyNumberFormat, helper.GroupHtmlColorCode);
                    rowIndex++;

                    foreach (var subItem in describeItem.Items)
                    {
                        helper.SetCellTextVal(string.Format("E{0}", rowIndex), subItem.LicenseNo, true);
                        helper.SetCellTextVal(string.Format("F{0}:G{0}", rowIndex), subItem.RegisterDateText, true);
                        helper.SetCellTextVal(string.Format("H{0}:I{0}", rowIndex), subItem.VehicleAgeValueText, true);
                        helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), subItem.ExpensesRepairPrice, true);
                        rowIndex++;
                    }

                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:I{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), netPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าเบี้ยเลี้ยง ค่าเช่าที่พัก และค่าพาหนะ"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoAllowance(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าเบี้ยเลี้ยง";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:E{1}", rowIndex, rowIndex + 1), "รายการ");
            helper.SetCaption(string.Format("F{0}:G{1}", rowIndex, rowIndex + 1), "ระดับ");
            helper.SetCaption(string.Format("H{0}:H{1}", rowIndex, rowIndex + 1), "ชม.");
            helper.SetCaption(string.Format("I{0}:I{1}", rowIndex, rowIndex + 1), "ครั้ง");
            helper.SetCaption(string.Format("J{0}:M{0}", rowIndex), "ค่าเบี้ยเลี้ยง");
            helper.SetCaption(string.Format("N{0}:Q{0}", rowIndex), "ค่าเช่าที่พัก");
            helper.SetCaption(string.Format("R{0}:R{1}", rowIndex, rowIndex + 1), "ค่าพาหนะและค่าน้ำมัน");
            helper.SetCaption(string.Format("S{0}:S{1}", rowIndex, rowIndex + 1), "รวมทั้งสิ้น");
            rowIndex++;
            helper.SetCaption(string.Format("J{0}", rowIndex), "คน");
            helper.SetCaption(string.Format("K{0}", rowIndex), "วัน");
            helper.SetCaption(string.Format("L{0}", rowIndex), "อัตรา");
            helper.SetCaption(string.Format("M{0}", rowIndex), "จำนวน (บาท)");
            helper.SetCaption(string.Format("N{0}", rowIndex), "คน");
            helper.SetCaption(string.Format("O{0}", rowIndex), "คืน");
            helper.SetCaption(string.Format("P{0}", rowIndex), "อัตรา");
            helper.SetCaption(string.Format("Q{0}", rowIndex), "จำนวน (บาท)");

            int? netAllowancePersons = 0, netAllowanceDays = 0, netRentRoomPersons = 0, netRentRoomDays = 0,
                netTimeAmounts = 0;
            decimal? netAllowancePrice = decimal.Zero, netRentRoomPrice = decimal.Zero
                , netVihicleAndOil = decimal.Zero, netExpenses = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesAllowance>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    int? allowancePersons = describeItem.Items.Sum(x => x.AllowancePersonAmounts),
                        allowanceDays = describeItem.Items.Sum(x => x.AllowanceAmountDays),
                        rentRoomPerons = describeItem.Items.Sum(x => x.RentRoomPersonAmounts),
                        rentRoomDays = describeItem.Items.Sum(x => x.RentRoomAmountDays),
                        timeAmounts = describeItem.Items.Sum(x => x.TimeAmounts);
                    decimal? allowancePrice = describeItem.Items.Sum(x => x.AllowanceTotalCompensationPrice),
                        rentRoomPrice = describeItem.Items.Sum(x => x.RentRoomTotalCompensationPrice),
                        vihicleAndOil = describeItem.Items.Sum(x => x.VehicleAndOilPrice),
                        totalExpenses = describeItem.Items.Sum(x => x.NetExpensesPrice);

                    netTimeAmounts += timeAmounts;
                    netAllowancePersons += allowancePersons;
                    netAllowanceDays += allowanceDays;
                    netAllowancePrice += allowancePrice;
                    netRentRoomPersons += rentRoomPerons;
                    netRentRoomDays += rentRoomDays;
                    netRentRoomPrice += rentRoomPrice;
                    netVihicleAndOil += vihicleAndOil;
                    netExpenses += totalExpenses;

                    int itemCount = describeItem.Items.Count;
                    helper.SetCellTextVal(string.Format("A{0}:A{1}", rowIndex, rowIndex + itemCount), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:E{1}", rowIndex, rowIndex + itemCount), describeItem.ItemText, true);
                    helper.SetCellIntVal(string.Format("I{0}", rowIndex), timeAmounts, true, helper.GroupHtmlColorCode);
                    // รวมค่าเบี้ยเลี้ยง
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), allowancePersons, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), allowanceDays, true, helper.GroupHtmlColorCode);
                    helper.SetCellTextVal(string.Format("L{0}", rowIndex), string.Empty, true, helper.GroupHtmlColorCode);
                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), allowancePrice, true, helper.CurrencyNumberFormat, helper.GroupHtmlColorCode);
                    // รวมค่าเช่าที่พัก
                    helper.SetCellIntVal(string.Format("N{0}", rowIndex), rentRoomPerons, true, helper.GroupHtmlColorCode);
                    helper.SetCellIntVal(string.Format("O{0}", rowIndex), rentRoomDays, true, helper.GroupHtmlColorCode);
                    helper.SetCellTextVal(string.Format("P{0}", rowIndex), string.Empty, true, helper.GroupHtmlColorCode);
                    helper.SetCellCurrencyVal(string.Format("Q{0}", rowIndex), rentRoomPrice, true, helper.CurrencyNumberFormat, helper.GroupHtmlColorCode);
                    // รวมค่าพาหนะและน้ำมัน & รวมทั้งสิ้นของรายการ
                    helper.SetCellCurrencyVal(string.Format("R{0}", rowIndex), vihicleAndOil, true, helper.CurrencyNumberFormat, helper.GroupHtmlColorCode);
                    helper.SetCellCurrencyVal(string.Format("S{0}", rowIndex), totalExpenses, true, helper.CurrencyNumberFormat, helper.GroupHtmlColorCode);

                    rowIndex++;
                    foreach (var subItem in describeItem.Items)
                    {
                        bool isMoreThanHalfDay = subItem.MoreThanHalfDay.Equals(1);
                        helper.SetCellTextVal(string.Format("F{0}:G{0}", rowIndex), subItem.LevelName, true);
                        helper.SetCellTextVal(string.Format("H{0}", rowIndex), isMoreThanHalfDay ? "> 12 ชม." : "< 12 ชม.", true);
                        helper.SetCellIntVal(string.Format("I{0}", rowIndex), subItem.TimeAmounts, true);
                        // ค่าเบี้ยเลี้ยง
                        helper.SetCellIntVal(string.Format("J{0}", rowIndex), subItem.AllowancePersonAmounts, true);
                        helper.SetCellIntVal(string.Format("K{0}", rowIndex), subItem.AllowanceAmountDays, true);
                        helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), isMoreThanHalfDay ? subItem.AllowanceCompensationRatePrice : subItem.AllowanceCompensationRateHalfPrice, true);
                        helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), subItem.AllowanceTotalCompensationPrice, true);
                        // เค่าเช่าที่พัก
                        helper.SetCellIntVal(string.Format("N{0}", rowIndex), subItem.RentRoomPersonAmounts, true);
                        helper.SetCellIntVal(string.Format("O{0}", rowIndex), subItem.RentRoomAmountDays, true);
                        helper.SetCellCurrencyVal(string.Format("P{0}", rowIndex), isMoreThanHalfDay ? subItem.RentRoomCompensationRatePrice : subItem.RentRoomCompensationRateHalfPrice, true);
                        helper.SetCellCurrencyVal(string.Format("Q{0}", rowIndex), subItem.RentRoomTotalCompensationPrice, true);
                        // ค่ายานพาหนะและน้ำมัน & รวม
                        helper.SetCellCurrencyVal(string.Format("R{0}", rowIndex), subItem.VehicleAndOilPrice, true);
                        helper.SetCellCurrencyVal(string.Format("S{0}", rowIndex), subItem.NetExpensesPrice, true);

                        rowIndex++;
                    }

                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellIntVal(string.Format("I{0}", rowIndex), netTimeAmounts, true, helper.CaptionHtmlColorCode);
            // รวมค่าเบี้ยเลี้ยง
            helper.SetCellIntVal(string.Format("J{0}", rowIndex), netAllowancePersons, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("K{0}", rowIndex), netAllowanceDays, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("L{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), netAllowancePrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            // รวมค่าเช่าที่พัก
            helper.SetCellIntVal(string.Format("N{0}", rowIndex), netRentRoomPersons, true, helper.CaptionHtmlColorCode);
            helper.SetCellIntVal(string.Format("O{0}", rowIndex), netRentRoomDays, true, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("P{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("Q{0}", rowIndex), netRentRoomPrice, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            // รวมค่าพาหนะและน้ำมัน & รวมทั้งสิ้นของรายการ
            helper.SetCellCurrencyVal(string.Format("R{0}", rowIndex), netVihicleAndOil, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("S{0}", rowIndex), netExpenses, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "เงินสมทบกองทุนเงินทดแทน"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoCompensationFund(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "เงินสมทบกองทุนเงินทดแทน";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "ชื่อ-สกุล");
            helper.SetCaption(string.Format("D{0}:E{1}", rowIndex, rowIndex + 1), "ตำแหน่ง");
            helper.SetCaption(string.Format("F{0}:G{1}", rowIndex, rowIndex + 1), "ประเภทบุคลากร");
            helper.SetCaption(string.Format("H{0}:I{1}", rowIndex, rowIndex + 1), "อัตราเงินเดือน");
            helper.SetCaption(string.Format("J{0}:K{0}", rowIndex), "อัตรา (บาท)");
            rowIndex++;
            helper.SetCaption(string.Format("J{0}", rowIndex), "ต่อเดือน");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ต่อปี");

            decimal monthlyVal = decimal.Zero, yearlyVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesCompensationFund>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), describeItem.PersonName, true);
                    helper.SetCellTextVal(string.Format("D{0}:E{0}", rowIndex), describeItem.PositionName, true);
                    helper.SetCellTextVal(string.Format("F{0}:G{0}", rowIndex), "", true);
                    helper.SetCellCurrencyVal(string.Format("H{0}:I{0}", rowIndex), describeItem.Salary, true);
                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), describeItem.MonthlyRateVal, true);
                    helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), describeItem.YearlyRateVal, true);

                    monthlyVal += describeItem.MonthlyRateVal.HasValue ? describeItem.MonthlyRateVal.Value : decimal.Zero;
                    yearlyVal += describeItem.YearlyRateVal.HasValue ? describeItem.YearlyRateVal.Value : decimal.Zero;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:I{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), monthlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), yearlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "เงินสมทบกองทุนประกันสังคม"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoSocialSecurity(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "เงินสมทบกองทุนประกันสังคม";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "ชื่อ-สกุล");
            helper.SetCaption(string.Format("D{0}:E{1}", rowIndex, rowIndex + 1), "ตำแหน่ง");
            helper.SetCaption(string.Format("F{0}:G{1}", rowIndex, rowIndex + 1), "อัตราเงินเดือน");
            helper.SetCaption(string.Format("H{0}:I{0}", rowIndex), "อัตรา (บาท)");
            rowIndex++;
            helper.SetCaption(string.Format("H{0}", rowIndex), "ต่อเดือน");
            helper.SetCaption(string.Format("I{0}", rowIndex), "ต่อปี");

            decimal monthlyVal = decimal.Zero, yearlyVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesSocialSecurity>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), describeItem.PersonName, true);
                    helper.SetCellTextVal(string.Format("D{0}:E{0}", rowIndex), describeItem.PositionName, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}:G{0}", rowIndex), describeItem.Salary, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.MonthlyRateVal, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.YearlyRateVal, true);

                    monthlyVal += describeItem.MonthlyRateVal.HasValue ? describeItem.MonthlyRateVal.Value : decimal.Zero;
                    yearlyVal += describeItem.YearlyRateVal.HasValue ? describeItem.YearlyRateVal.Value : decimal.Zero;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:G{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), monthlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), yearlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าตอบแทนแหมาจ่ายแทนการจัดหารถประจำตำแหน่ง"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoInsteadCarForPositionCompensation(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "รถประจำตำแหน่ง";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "ชื่อ-สกุล");
            helper.SetCaption(string.Format("D{0}:E{1}", rowIndex, rowIndex + 1), "ตำแหน่ง");
            helper.SetCaption(string.Format("F{0}:G{0}", rowIndex), "ค่าตอบแทน (บาท)");
            rowIndex++;
            helper.SetCaption(string.Format("F{0}", rowIndex), "ต่อเดือน");
            helper.SetCaption(string.Format("G{0}", rowIndex), "ต่อปี");

            decimal monthlyVal = decimal.Zero, yearlyVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesInsteadCarForPositionCompensation>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), describeItem.PersonName, true);
                    helper.SetCellTextVal(string.Format("D{0}:E{0}", rowIndex), describeItem.PositionName, true);
                    helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), describeItem.CompensationMonthlyRatePrice, true);
                    helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), describeItem.CompensationYearlyPrice, true);

                    monthlyVal += describeItem.CompensationMonthlyRatePrice.HasValue ? describeItem.CompensationMonthlyRatePrice.Value : decimal.Zero;
                    yearlyVal += describeItem.CompensationYearlyPrice.HasValue ? describeItem.CompensationYearlyPrice.Value : decimal.Zero;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:E{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("F{0}", rowIndex), monthlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("G{0}", rowIndex), yearlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าเช่าบ้าน"
        /// </summary>
        /// <param name="rowIndex"></param>
        private void DoRentHouse(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ค่าเช่าบ้าน";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "ชื่อ-สกุล");
            helper.SetCaption(string.Format("D{0}:E{1}", rowIndex, rowIndex + 1), "ตำแหน่ง");
            helper.SetCaption(string.Format("F{0}:G{1}", rowIndex, rowIndex + 1), "ระดับ");
            helper.SetCaption(string.Format("H{0}:H{1}", rowIndex, rowIndex + 1), "อัตราเงินเดือน");
            helper.SetCaption(string.Format("I{0}:J{0}", rowIndex), "อัตรา (บาท)");
            rowIndex++;
            helper.SetCaption(string.Format("I{0}", rowIndex), "ต่อเดือน");
            helper.SetCaption(string.Format("J{0}", rowIndex), "ต่อปี");

            decimal monthlyVal = decimal.Zero, yearlyVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesRentHouse>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), describeItem.PersonName, true);
                    helper.SetCellTextVal(string.Format("D{0}:E{0}", rowIndex), describeItem.PositionName, true);
                    helper.SetCellTextVal(string.Format("F{0}:G{0}", rowIndex), describeItem.LevelName, true);
                    helper.SetCellCurrencyVal(string.Format("H{0}", rowIndex), describeItem.Salary, true);
                    helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), describeItem.RentMonthlyRateVal, true);
                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), describeItem.RentYearlyRateVal, true);

                    monthlyVal += describeItem.RentMonthlyRateVal.HasValue ? describeItem.RentMonthlyRateVal.Value : decimal.Zero;
                    yearlyVal += describeItem.RentYearlyRateVal.HasValue ? describeItem.RentYearlyRateVal.Value : decimal.Zero;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:H{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("I{0}", rowIndex), monthlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), yearlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }


        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าตอบแทนพิเศษรายเดือนสำหรับผู้ปฏิบัติงานในเขตพื้นที่พิเศษ (ส.ป.พ.)"
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="expensesDescribe"></param>
        /// <param name="rowIndex"></param>
        private void DoMonthlyCompensationExtra(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ส.ป.พ.";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "ชื่อ-สกุล");
            helper.SetCaption(string.Format("D{0}:E{1}", rowIndex, rowIndex + 1), "ประเภทบุคลากร");
            helper.SetCaption(string.Format("F{0}:G{1}", rowIndex, rowIndex + 1), "ตำแหน่ง");
            helper.SetCaption(string.Format("H{0}:I{1}", rowIndex, rowIndex + 1), "ระดับ");
            helper.SetCaption(string.Format("J{0}:K{0}", rowIndex), "อัตรา (บาท)");
            rowIndex++;
            helper.SetCaption(string.Format("J{0}", rowIndex), "ต่อเดือน");
            helper.SetCaption(string.Format("K{0}", rowIndex), "ต่อปี");

            decimal monthlyVal = decimal.Zero, yearlyVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesMonthlyCompensationExtra>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), describeItem.PersonName, true);
                    helper.SetCellTextVal(string.Format("D{0}:E{0}", rowIndex), describeItem.PersonTypeName, true);
                    helper.SetCellTextVal(string.Format("F{0}:G{0}", rowIndex), describeItem.PositionName, true);
                    helper.SetCellTextVal(string.Format("H{0}:I{0}", rowIndex), describeItem.LevelName, true);
                    helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), describeItem.CompensationMonthlyVal, true);
                    helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), describeItem.CompensationYearlyVal, true);

                    monthlyVal += describeItem.CompensationMonthlyVal.HasValue ? describeItem.CompensationMonthlyVal.Value : decimal.Zero;
                    yearlyVal += describeItem.CompensationYearlyVal.HasValue ? describeItem.CompensationYearlyVal.Value : decimal.Zero;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:I{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("J{0}", rowIndex), monthlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("K{0}", rowIndex), yearlyVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }

        /// <summary>
        /// เขียนข้อมูลลง Excel
        /// "ค่าตอบแทนการปฏิบัติงานนอกเวลาราชการ"
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="expensesDescribe"></param>
        /// <param name="rowIndex"></param>
        private void DoOvertimeCompensation(ExportHelper helper, List<ExpensesDescribeProperty> expensesDescribe, ref int rowIndex)
        {
            helper.CurrWorkSheet.Name = "ปฏิบัติงานนอกเวลาราชการ";

            // คอลัมล์
            helper.SetCaption(string.Format("A{0}:A{1}", rowIndex, rowIndex + 1), "ลำดับ");
            helper.SetCaption(string.Format("B{0}:C{1}", rowIndex, rowIndex + 1), "ชื่อ-สกุล");
            helper.SetCaption(string.Format("D{0}:E{1}", rowIndex, rowIndex + 1), "ตำแหน่ง");
            helper.SetCaption(string.Format("F{0}:G{1}", rowIndex, rowIndex + 1), "ระดับ");
            helper.SetCaption(string.Format("H{0}:I{1}", rowIndex, rowIndex + 1), "ประเภทบุคลากร");

            helper.SetCaption(string.Format("J{0}:M{0}", rowIndex), "วันทำการ");
            helper.SetCaption(string.Format("N{0}:Q{0}", rowIndex), "วันหยุด");
            helper.SetCaption(string.Format("R{0}:S{1}", rowIndex, rowIndex + 1), "รวมทั้งสิ้น (บาท)");
            rowIndex++;
            helper.SetCaption(string.Format("J{0}", rowIndex), "จำนวนวัน");
            helper.SetCaption(string.Format("K{0}", rowIndex), "จำนวนชั่วโมง/วัน");
            helper.SetCaption(string.Format("L{0}", rowIndex), "ค่าตอบแทน/ชั่วโมง");
            helper.SetCaption(string.Format("M{0}", rowIndex), "จำนวน (บาท)");
            helper.SetCaption(string.Format("N{0}", rowIndex), "จำนวนวัน");
            helper.SetCaption(string.Format("O{0}", rowIndex), "จำนวนชั่วโมง/วัน");
            helper.SetCaption(string.Format("P{0}", rowIndex), "ค่าตอบแทน/ชั่วโมง");
            helper.SetCaption(string.Format("Q{0}", rowIndex), "จำนวน (บาท)");

            decimal workingVal = decimal.Zero, holidayVal = decimal.Zero, netVal = decimal.Zero;
            int itemIndex = 1;
            rowIndex++;
            foreach (var item in expensesDescribe)
            {
                var describeItems = AppUtils.ConvertXElementTo<List<ExpensesOvertimeCompensation>>(item.ExpensesDescribe);
                foreach (var describeItem in describeItems)
                {
                    helper.SetCellTextVal(string.Format("A{0}", rowIndex), itemIndex.ToString(), true);
                    helper.SetCellTextVal(string.Format("B{0}:C{0}", rowIndex), describeItem.PersonName, true);
                    helper.SetCellTextVal(string.Format("D{0}:E{0}", rowIndex), describeItem.PositionName, true);
                    helper.SetCellTextVal(string.Format("F{0}:G{0}", rowIndex), describeItem.LevelName, true);
                    helper.SetCellTextVal(string.Format("H{0}:I{0}", rowIndex), describeItem.PersonTypeName, true);
                    helper.SetCellIntVal(string.Format("J{0}", rowIndex), describeItem.WorkingAmountDays, true);
                    helper.SetCellIntVal(string.Format("K{0}", rowIndex), describeItem.WorkingAmountHoursPerDay, true);
                    helper.SetCellCurrencyVal(string.Format("L{0}", rowIndex), describeItem.WorkingCompensationPerHour, true);
                    helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), describeItem.TotalCompensationWorkingPrice, true);

                    helper.SetCellIntVal(string.Format("N{0}", rowIndex), describeItem.HolidayAmountDays, true);
                    helper.SetCellIntVal(string.Format("O{0}", rowIndex), describeItem.HolidayAmountHoursPerDay, true);
                    helper.SetCellCurrencyVal(string.Format("P{0}", rowIndex), describeItem.HolidayCompensationPerHour, true);
                    helper.SetCellCurrencyVal(string.Format("Q{0}", rowIndex), describeItem.TotalCompensationHolidayPrice, true);
                    helper.SetCellCurrencyVal(string.Format("R{0}:S{0}", rowIndex), describeItem.NetCompensationPrice, true);

                    workingVal += describeItem.TotalCompensationWorkingPrice.HasValue ? describeItem.TotalCompensationWorkingPrice.Value : decimal.Zero;
                    holidayVal += describeItem.TotalCompensationHolidayPrice.HasValue ? describeItem.TotalCompensationHolidayPrice.Value : decimal.Zero;
                    netVal += describeItem.NetCompensationPrice.HasValue ? describeItem.NetCompensationPrice.Value : decimal.Zero;

                    rowIndex++;
                    itemIndex++;
                }
            }
            helper.SetCellTextVal(string.Format("A{0}:L{0}", rowIndex), "รวมทั้งสิ้น (บาท)", true, helper.CaptionHtmlColorCode, true);
            helper.SelectedExcelRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            helper.SetCellCurrencyVal(string.Format("M{0}", rowIndex), workingVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellTextVal(string.Format("N{0}:P{0}", rowIndex), string.Empty, true, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("Q{0}", rowIndex), holidayVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
            helper.SetCellCurrencyVal(string.Format("R{0}:S{0}", rowIndex), netVal, true, helper.CurrencyNumberFormat, helper.CaptionHtmlColorCode);
        }
    }
}