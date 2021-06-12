using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.ModelBinding;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class RptPlansForReceivingAndPlayingExpensesController : Controller
    {
        // GET: PlansForReceivingAndPlayingExpenses
        public ActionResult RptPlansForReceivingAndPlayingExpenses()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_PlansForReceivingAndPlayingExpenses);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);
            UserMenuGroupProperty menuReportItem = userAuthorizeProfile.MenuGroups.Where(e => "รายงาน".Equals(e.GroupName)).FirstOrDefault();

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_PlansForReceivingAndPlayingExpenses;
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
            ViewBag.FiscalYearThai = userAuthorizeProfile.DefaultFiscalYear + 543;

          
            return View();
        }

        [HttpPost, Route("Yr:int")]
        public ActionResult ShowData(int Yr)
        {
            Yr = Yr - 543;

            Dictionary<string, object> res = new Dictionary<string, object>(1) {
                //2
         
                { "TotalCompensationCost" ,0},//ยอดรวม 2 ค่าตอบแทน ใช้สอยและวัสดุ
                { "Compensation", null }, //2.1 ค่าตอบแทน
                { "CompensationDetail", null }, //2.1 ค่าตอบแทน

                //3
                { "TotalEquipment",0}, //ยอดรวม 3 ค่าครุภัณฑ์
                { "EquipmentOffice",null}, //3.1 ค่าครุภัณฑ์สำนักงาน


                //4
                { "TotalLandAndBuilding",0}, //ยอดรวม 4.ค่าที่ดินและสิ่งก่อสร้าง
                { "LandAndBuilding",null},//4.1ค่าปรับปรุงสิ่งก่อสร้าง

                //5
                { "TotalOtherExpense",0}, //ยอดรวม 5.หมวดรายจ่ายอื่น
                { "OtherExpense",null}, 

                //6
                { "TotalMinistryofFinance",0},//ยอดรวม 6.โอนให้กระทรวงการคลังและหน่วยงานในสังกัดกระทรวงการคลัง		

                { "PermanentSecretary",null},//6.1 โอนให้สำนักงานปลัดกระทรวงการคลัง

            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                //2 ค่าตอบแทน ใช้สอยและวัสดุ
                var expr2 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.Compensation_Master_ID) 
                                        && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.Compensation_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();
                res["TotalCompensationCost"] = expr2.Sum(e => e.OFF_BUDGET_AMOUNT); 
              
                var finalExpr2 = expr2.Select(e => new
                {
                    ExpensesItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(e.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(e.EXPENSES_ID) 
                                            && e.BUDGET_ID.Equals(e.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).ToList(),

                    EXPENSES_NAME = e.EXPENSES_NAME,
                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                }).ToList();
                res["Compensation"] = finalExpr2;

                //3 ค่าครุภัณฑ์
                var expr3 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.EquipmentOffice_Master_ID) 
                                && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.EquipmentOffice_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();
                res["TotalEquipment"] = expr3.Sum(e => e.OFF_BUDGET_AMOUNT);

                var finalExpr3 = expr3.Select(e => new
                {
                    EquipmentOfficeItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(e.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(e.EXPENSES_ID)
                                            && e.BUDGET_ID.Equals(e.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).ToList(),

                    EXPENSES_NAME = e.EXPENSES_NAME,
                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                }).ToList();
                res["EquipmentOffice"] = finalExpr3;


                //4 ค่าที่ดินและสิ่งก่อสร้าง
                var expr4 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.LandAndBuilding_Master_ID) 
                                    && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.LandAndBuilding_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();
                res["TotalLandAndBuilding"] = expr4.Sum(e => e.OFF_BUDGET_AMOUNT);

                var finalExpr4 = expr4.Select(e => new
                {
                    LandAndBuildingItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(e.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(e.EXPENSES_ID)
                                            && e.BUDGET_ID.Equals(e.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).ToList(),

                    EXPENSES_NAME = e.EXPENSES_NAME,
                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                }).ToList();
                res["LandAndBuilding"] = finalExpr4;

                //5 หมวดรายจ่ายอื่น
                var expr5 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.OtherExpense_Master_ID) 
                                && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.OtherExpense_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();
                res["TotalOtherExpense"] = expr5.Sum(e => e.OFF_BUDGET_AMOUNT);

                var finalExpr5 = expr5.Select(e => new
                {
                    OtherExpenseItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(e.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(e.EXPENSES_ID)
                                            && e.BUDGET_ID.Equals(e.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).ToList(),

                    EXPENSES_NAME = e.EXPENSES_NAME,
                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                }).ToList();
                res["OtherExpense"] = finalExpr5;

            };


            return Json(res, JsonRequestBehavior.AllowGet);

            // return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpGet, Route("filename:string, resultFilename:string, deleteFlag: string")]
        public ActionResult GetFile(string filename, string resultFilename, string deleteFlag)
        {
            string file = filename;


            // กรณีไม่พบไฟล์
            if (!System.IO.File.Exists(file))
            {
                byte[] buffer = Encoding.UTF8.GetBytes("<center><h1 style=\"color:red\">FILE NOT FOUND</h1></center>");
                return base.File(buffer, "text/html");
            }

            // อ่านไฟล์ลง stream และตอบกลับ
            using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string fileExt = Regex.Replace(filename, @"^.+\.", "", RegexOptions.IgnoreCase);
                resultFilename = string.IsNullOrEmpty(resultFilename) ? filename : string.Format("{0}.{1}", resultFilename, fileExt);

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                stream.Close();

                var fileContentResult = base.File(buffer, "application/octet-stream", resultFilename);
                if ("Y".Equals(deleteFlag))
                    System.IO.File.Delete(file);

                return fileContentResult;
            }
        }

        [HttpPost]
        public ActionResult ExportExcel(int Yr)
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            Dictionary<string, string> res = new Dictionary<string, string>(2) {
                { "filename", null },
                { "resultFilename", null }
            };

            Yr = Yr - 543;

            // เขียนข้อมูลลงไฟล์ Excel
            decimal rTotal = 0;

            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            string reportTemplateFile = string.Format("{0}/RptPlansForReceivingAndPlayingExpenses.xlsx", appSetting.ReportTemplatePath);
            using (ExcelPackage xlsApp = new ExcelPackage(new FileInfo(reportTemplateFile)))
            {
                int rowIndex = 3;

                ExportUtils.CurrWorkSheet = xlsApp.Workbook.Worksheets["Sheet1"];

                int YearThai = userAuthorizeProfile.DefaultFiscalYear + 543;

                ExportUtils.SetCellTextVal("E2", "ปี "+ YearThai.ToString(), false);

                // 1.ค่าาจ้างลูกจ้างชั่วคราว	
                ExportUtils.SetCellCurrencyVal(string.Format("A{0}", rowIndex), 1, true,"", "#AED6F1");
                ExportUtils.SetCellTextVal(string.Format("B{0}:D{0}", rowIndex), "ค่าาจ้างลูกจ้างชั่วคราว", true, "#AED6F1");
                ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true,"", "#AED6F1");

                rowIndex++;
                ExportUtils.SetCellTextVal(string.Format("A{0}:D{0}", rowIndex), "รวม", true);
                ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true);

                // 2.ค่าตอบแทน ใช้สอยและวัสดุ
                using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                {
                    var expr2 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.Compensation_Master_ID)
                                        && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.Compensation_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();

                    var finalExpr2 = expr2.Select(e => new
                    {
                        EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                        EXPENSES_ID = e.EXPENSES_ID,
                        BUDGET_ID = e.BUDGET_ID,
                        EXPENSES_NAME = e.EXPENSES_NAME,
                        OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                    }).ToList();

                    rowIndex++;
                    ExportUtils.SetCellCurrencyVal(string.Format("A{0}", rowIndex), 2, true,"", "#AED6F1");
                    ExportUtils.SetCellTextVal(string.Format("B{0}:D{0}", rowIndex), "ค่าตอบแทน ใช้สอยและวัสดุ", true, "#AED6F1");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr2.Sum(e => e.OFF_BUDGET_AMOUNT), true,"#,##0.00" , "#AED6F1");

                    decimal irecno = 2;
                    int irecnoDetail = 0;
                    foreach (var item in finalExpr2)
                    {
                        rowIndex++;
                        irecno =  irecno + Convert.ToDecimal( 0.1 );
                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                        ExportUtils.SetCellCurrencyVal(string.Format("B{0}", rowIndex), irecno, true, "0.0");
                        ExportUtils.SetCellTextVal(string.Format("C{0}:D{0}", rowIndex), item.EXPENSES_NAME, true);
                        ExportUtils.SetCellTextVal(string.Format("E{0}", rowIndex), "", true);

                        var ExpensesItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(item.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(item.EXPENSES_ID)
                                                && sub.BUDGET_ID.Equals(item.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).Select( e => new 
                                                {
                                                    EXPENSES_NAME = e.PROJECT_NAME,
                                                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT
                                                }).ToList();
                        
                        irecnoDetail = 0;
                        foreach (var ItemDetail in ExpensesItems)
                        {
                            rowIndex++;

                            irecnoDetail++;
                            ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), irecno.ToString()+"."+ irecnoDetail.ToString(), true);
                            ExportUtils.SetCellTextVal(string.Format("D{0}", rowIndex), ItemDetail.EXPENSES_NAME, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), ItemDetail.OFF_BUDGET_AMOUNT, true, "#,##0.00");

                        }
                        if (ExpensesItems.Count == 0)
                        {
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), item.OFF_BUDGET_AMOUNT, true,  "#,##0.00");
                        }
                    }
                    rowIndex++;
                    ExportUtils.SetCaption(string.Format("A{0}:D{0}", rowIndex), "รวม", "");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr2.Sum(e => e.OFF_BUDGET_AMOUNT), true, "#,##0.00");
                   
                    rTotal = rTotal + expr2.Sum(e => e.OFF_BUDGET_AMOUNT);

                    //3.ค่าครุภัณฑ์
                    var expr3 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.EquipmentOffice_Master_ID)
                                && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.EquipmentOffice_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();
                    

                    var finalExpr3 = expr3.Select(e => new
                    {
                        EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                        EXPENSES_ID = e.EXPENSES_ID,
                        BUDGET_ID = e.BUDGET_ID,
                        EXPENSES_NAME = e.EXPENSES_NAME,
                        OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                    }).ToList();

                    rowIndex++;
                    ExportUtils.SetCellCurrencyVal(string.Format("A{0}", rowIndex), 3, true,"", "#AED6F1");
                    ExportUtils.SetCellTextVal(string.Format("B{0}:D{0}", rowIndex), "ค่าครุภัณฑ์", true, "#AED6F1");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr3.Sum(e => e.OFF_BUDGET_AMOUNT), true, "#,##0.00", "#AED6F1");

                    irecno = 3;
                    irecnoDetail = 0;
                    foreach (var item in finalExpr3)
                    {
                        rowIndex++;
                        irecno = irecno + Convert.ToDecimal(0.1);
                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                        ExportUtils.SetCellCurrencyVal(string.Format("B{0}", rowIndex), irecno, true, "0.0");
                        ExportUtils.SetCellTextVal(string.Format("C{0}:D{0}", rowIndex), item.EXPENSES_NAME, true);
                        ExportUtils.SetCellTextVal(string.Format("E{0}", rowIndex), "", true);

                        var ExpensesItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(item.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(item.EXPENSES_ID)
                                                && sub.BUDGET_ID.Equals(item.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).Select(e => new
                                                {
                                                    EXPENSES_NAME = e.PROJECT_NAME,
                                                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT
                                                }).ToList();

                        irecnoDetail = 0;
                        foreach (var ItemDetail in ExpensesItems)
                        {
                            rowIndex++;
                            irecnoDetail++;
                            ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), irecno.ToString() + "." + irecnoDetail.ToString(), true);
                            ExportUtils.SetCellTextVal(string.Format("D{0}", rowIndex), ItemDetail.EXPENSES_NAME, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), ItemDetail.OFF_BUDGET_AMOUNT, true, "#,##0.00");

                        }
                        if (ExpensesItems.Count == 0)
                        {
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), item.OFF_BUDGET_AMOUNT, true,  "#,##0.00");
                        }
                    }
                    rowIndex++;
                    ExportUtils.SetCaption(string.Format("A{0}:D{0}", rowIndex), "รวม", "");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr3.Sum(e => e.OFF_BUDGET_AMOUNT), true, "#,##0.00");

                    rTotal = rTotal + expr3.Sum(e => e.OFF_BUDGET_AMOUNT);

                    //4.ค่าที่ดินและสิ่งก่อสร้าง
                    var expr4 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.LandAndBuilding_Master_ID)
                                    && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.LandAndBuilding_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();
                  

                    var finalExpr4 = expr4.Select(e => new
                    {
                        EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                        EXPENSES_ID = e.EXPENSES_ID,
                        BUDGET_ID = e.BUDGET_ID,
                        EXPENSES_NAME = e.EXPENSES_NAME,
                        OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                    }).ToList();

                    rowIndex++;
                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "4", true, "#AED6F1");
                    ExportUtils.SetCellTextVal(string.Format("B{0}:D{0}", rowIndex), "ค่าที่ดินและสิ่งก่อสร้าง", true, "#AED6F1");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr4.Sum(e => e.OFF_BUDGET_AMOUNT), true, "#,##0.00", "#AED6F1");

                    irecno = 4;
                    irecnoDetail = 0;
                    foreach (var item in finalExpr4)
                    {
                        rowIndex++;
                        irecno = irecno + Convert.ToDecimal(0.1);
                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                        ExportUtils.SetCellCurrencyVal(string.Format("B{0}", rowIndex), irecno, true, "0.0");
                        ExportUtils.SetCellTextVal(string.Format("C{0}:D{0}", rowIndex), item.EXPENSES_NAME, true);
                        ExportUtils.SetCellTextVal(string.Format("E{0}", rowIndex), "", true);

                        var ExpensesItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(item.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(item.EXPENSES_ID)
                                                && sub.BUDGET_ID.Equals(item.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).Select(e => new
                                                {
                                                    EXPENSES_NAME = e.PROJECT_NAME,
                                                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT
                                                }).ToList();

                        irecnoDetail = 0;
                        foreach (var ItemDetail in ExpensesItems)
                        {
                            rowIndex++;
                            irecnoDetail++;
                            ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), irecno.ToString() + "." + irecnoDetail.ToString(), true);
                            ExportUtils.SetCellTextVal(string.Format("D{0}", rowIndex), ItemDetail.EXPENSES_NAME, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), ItemDetail.OFF_BUDGET_AMOUNT, true, "#,##0.00");

                        }
                        if (ExpensesItems.Count == 0)
                        {
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), item.OFF_BUDGET_AMOUNT, true, "#,##0.00");
                        }
                    }
                    rowIndex++;
                    ExportUtils.SetCaption(string.Format("A{0}:D{0}", rowIndex), "รวม", "");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr4.Sum(e => e.OFF_BUDGET_AMOUNT), true, "#,##0.00");

                    rTotal = rTotal + expr4.Sum(e => e.OFF_BUDGET_AMOUNT);

                    //5.หมวดรายจ่ายอื่น
                    var expr5 = db.proc_RptPlansForReceivingAndPlayingExpenses(Convert.ToInt16(Yr)).Where(e => e.EXPENSES_MASTER_ID.Equals(AppConfigConst.OtherExpense_Master_ID)
                                && e.EXPENSES_GROUP_ID.Equals(AppConfigConst.OtherExpense_EXPENSES_GROUP_ID) && !e.OFF_BUDGET_AMOUNT.Equals(0)).ToList();
                    

                    var finalExpr5 = expr5.Select(e => new
                    {
                        EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                        EXPENSES_ID = e.EXPENSES_ID,
                        BUDGET_ID = e.BUDGET_ID,
                        EXPENSES_NAME = e.EXPENSES_NAME,
                        OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT

                    }).ToList();

                    rowIndex++;
                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "5", true, "#AED6F1");
                    ExportUtils.SetCellTextVal(string.Format("B{0}:D{0}", rowIndex), "หมวดรายจ่ายอื่น", true, "#AED6F1");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr5.Sum(e => e.OFF_BUDGET_AMOUNT), true, "#,##0.00", "#AED6F1");

                    irecno = 5;
                    irecnoDetail = 0;
                    foreach (var item in finalExpr5)
                    {
                        rowIndex++;
                        irecno = irecno + Convert.ToDecimal(0.1);
                        ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                        ExportUtils.SetCellCurrencyVal(string.Format("B{0}", rowIndex), irecno, true, "0.0");
                        ExportUtils.SetCellTextVal(string.Format("C{0}:D{0}", rowIndex), item.EXPENSES_NAME, true);
                        ExportUtils.SetCellTextVal(string.Format("E{0}", rowIndex), "", true);

                        var ExpensesItems = db.T_BUDGET_EXPENSES_PROJECTs.Where(sub => sub.EXPENSES_GROUP_ID.Equals(item.EXPENSES_GROUP_ID) && sub.EXPENSES_ID.Equals(item.EXPENSES_ID)
                                                && sub.BUDGET_ID.Equals(item.BUDGET_ID) && sub.ACTIVE.Equals(1) && !sub.OFF_BUDGET_AMOUNT.Equals(0)).Select(e => new
                                                {
                                                    EXPENSES_NAME = e.PROJECT_NAME,
                                                    OFF_BUDGET_AMOUNT = e.OFF_BUDGET_AMOUNT
                                                }).ToList();

                        irecnoDetail = 0;
                        foreach (var ItemDetail in ExpensesItems)
                        {
                            rowIndex++;
                            irecnoDetail++;
                            ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("B{0}", rowIndex), "", true);
                            ExportUtils.SetCellTextVal(string.Format("C{0}", rowIndex), irecno.ToString() + "." + irecnoDetail.ToString(), true);
                            ExportUtils.SetCellTextVal(string.Format("D{0}", rowIndex), ItemDetail.EXPENSES_NAME, true);
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), ItemDetail.OFF_BUDGET_AMOUNT, true, "#,##0.00");

                        }
                        if (ExpensesItems.Count == 0)
                        {
                            ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), item.OFF_BUDGET_AMOUNT, true, "#,##0.00");
                        }
                    }
                    rowIndex++;
                    ExportUtils.SetCaption(string.Format("A{0}:D{0}", rowIndex), "รวม", "");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), expr5.Sum(e => e.OFF_BUDGET_AMOUNT), true, "#,##0.00");

                    rTotal = rTotal + expr5.Sum(e => e.OFF_BUDGET_AMOUNT);

                    //6.โอนให้กระทรวงการคลังและหน่วยงานในสังกัดกระทรวงการคลัง
                    rowIndex++;
                    ExportUtils.SetCellTextVal(string.Format("A{0}", rowIndex), "6", true, "#AED6F1");
                    ExportUtils.SetCellTextVal(string.Format("B{0}:D{0}", rowIndex), "โอนให้กระทรวงการคลังและหน่วยงานในสังกัดกระทรวงการคลัง", true, "#AED6F1");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true, "#,##0.00", "#AED6F1");

                    rowIndex++;
                    ExportUtils.SetCaption(string.Format("A{0}:D{0}", rowIndex), "รวม", "");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), 0, true, "#,##0.00");


                    rowIndex++;
                    ExportUtils.SetCaption(string.Format("A{0}:D{0}", rowIndex), "รวมทั้งสิ้น", "");
                    ExportUtils.SetCellCurrencyVal(string.Format("E{0}", rowIndex), rTotal, true, "#,##0.00");

                   

                    // บันทึกข้อมูลเป็นไฟล์ Excel ที่ temporary path

                    string outFile = string.Format("{0}/RptPlansForReceivingAndPlayingExpenses{1}.xlsx", appSetting.TemporaryPath, Yr.ToString());
                    FileInfo outFileInfo = new FileInfo(outFile);
                    xlsApp.SaveAs(outFileInfo);

                    res["filename"] = outFile;
                    res["resultFilename"] = outFile;

                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                    


                

            }



        }

   


    }
}