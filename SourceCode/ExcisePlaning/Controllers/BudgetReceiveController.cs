using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// รับเงินที่ได้รับจัดสรรงบประมาณจากรัฐบาล
    /// จะประกอบไปด้วย รายการค่าใช้จ่ายต่างๆ
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReceiveController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageType">การแสดงผลข้อมูลการจัดสรรงบประมาณ, budget = แสดงเฉพาะงบประมาณ, off_budget=แสดงเฉพาะเงินนอกงบประมาณ</param>
        /// <returns></returns>
        // GET: BudgetReceive
        public ActionResult GetForm(string pageType)
        {
            string currentMenuConst = AppConfigConst.MENU_CONST_ALL_BUDGET_RECEIVE_GORNVERMENT_MENU;
            if ("budget".Equals(pageType))
                currentMenuConst = AppConfigConst.MENU_CONST_BUDGET_RECEIVE_GORNVERMENT_MENU;
            else if ("off_budget".Equals(pageType))
                currentMenuConst = AppConfigConst.MENU_CONST_OFF_BUDGET_RECEIVE_GORNVERMENT_MENU;

            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(currentMenuConst);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กรณีไม่ผ่านค่า type เข้าไปให้เด้งกลับไปหน้า Dashboard/หน้าแรก
            List<string> acceptPageTypes = new List<string>() { "all", "budget", "off_budget" };
            if (string.IsNullOrEmpty(pageType) || acceptPageTypes.IndexOf(pageType) == -1)
                return RedirectToAction(menuIndexItem.ActionName, menuIndexItem.RouteName);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = currentMenuConst;
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


            ViewBag.FiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.PageType = pageType;
            return View();
        }


        /// <summary>
        /// รายการค่าใช้จ่าย ที่ได้รับจัดสรรจากรัฐบาลในแต่ละปี หรือ กำลังจะทำรายการจัดสรรใหม่ในปีงบประมาณ (โดยอ้างอิงรายการจากคำขอต้นปี หรือ พลางก่อน)
        ///
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="getType">1 = รวมจำนวนเงินคำขอ งปม. ต้นปี และ รายการค่าใช้จ่าย, 2 = เฉพาะรายการค่าใช้จ่าย</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(short? fiscalYear, string getType)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() {
                { "netBudgetRequestStartYearAmounts", 0 }, // เงินงบประมาณต้นปี ที่ทำคำขอ
                { "netBudgetAmounts", 0 }, // จัดสรรจากรัฐบาลสุทธิ (งบประมาณ
                { "netActualBudgetAmounts", 0 }, // ได้รับจัดสรรจริง
                { "netUseBudgetAmounts", 0 }, // ยอดเบิกจ่าย เงินงบประมาณ
                { "netAllocateBudgetToDepartmentAmounts", 0 }, // จัดสรรให้หน่วยงาน
                { "netReserveBudgetAmounts", 0 }, // กันเงินงบประมาณ
                { "netReserveWithdrawalBudgetAmounts", 0 }, // เบิกจ่ายเงินงบประมาณ
                { "netRemainBudgetAmounts", 0 }, // เงินงบประมาณคงเหลือสุทธิ

                { "netOffBudgetAmounts", 0 }, // จัดสรรจากรัฐบาลสุทธิ (เงินนอกงบประมาณ)
                { "netOffActualBudgetAmounts", 0 }, // จัดเก็บรายได้สุทธิ
                { "netOffUseBudgetAmounts", 0 }, // ยอดเบิกจ่าย เงินนอกงบประมาณ
                { "netAllocateOffBudgetToDepartmentAmounts", 0 }, // จัดสรรให้หน่วยงาน
                { "netReserveOffBudgetAmounts", 0 }, // กันเงินนอกงบประมาณ
                { "netReserveWithdrawalOffBudgetAmounts", 0 }, // เบิกจ่าย เงินนอกงบประมาณ
                { "netOffRemainBudgetAmounts", 0 }, // เงินนอกงบประมาณ คงเหลือสุทธิ

                { "editAble", false }, // รายการจัดสรรเงิน งปม. จากรัฐบาลสามารถแก้ไขได้หรือไม่ (น้อยกว่าปี งปม. ปัจจุบันแก้ไขไม่ได้)
                { "budgetId", null }, // เลขที่กำกับของปีงบประมาณ ใช้ตรวจสอบเวลาบันทึกข้อมูล
                { "TemporaryBudgetFlag", null }, // พลางก่อน
                { "TemporaryYear", null }, // พลางก่อนปี ใด
                { "ReleaseBudgetFlag", null }, // เปิดใช้งบประมาณ
                { "ReleaseOffBudgetFlag", null }, // เปิดใช้เงินนอกงบประมาณ
                { "expenses", null } // รายการค่าใช้จ่ายที่ได้รับจัดสรร
            };

            if (null == fiscalYear)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // รายการค่าใช้จ่าย (ยังไม่มีบันทึกดึงจาก คำขอ, ดึงจาก T_BUDGET_EXPENSES)
                var expr = db.proc_GetExpensesAllocateBudgetFromGovernmentByYear(fiscalYear).ToList();
                var finalExpr = expr.GroupBy(e => new
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
                    e.BUDGET_TYPE_GOVERNMENT_REFER_CODE,
                    e.BUDGET_TYPE_ORDER_SEQ,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.EXPENSES_GROUP_GOVERNMENT_REFER_CODE
                }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                .ThenBy(e => e.Key.EXPENSES_GROUP_ORDER_SEQ)
                .Select(e => new
                {
                    GroupBy = e.Key,
                    ExpensesItems = e.OrderBy(x => x.EXPENSES_ORDER_SEQ).ToList()
                }).ToList();

                res["expenses"] = finalExpr;

                // สรุปข้อมูลการจัดสรร งปม. จากรัฐบาลตามปีที่ขอแสดงผล ข้อมูล
                if (expr.Any())
                {
                    var exprDepartmentBudget = db.T_BUDGET_ALLOCATEs.Where(e => e.YR.Equals(fiscalYear));
                    // รายการกันเงินงบประมาณ บางรายการ ที่ยกเลิกมีการกันเบิกจ่ายไปแล้ว
                    // จะต้องนำมาด้วย
                    var exprReserve = db.T_BUDGET_RESERVEs.Where(e => (e.ACTIVE.Equals(1) || (e.ACTIVE.Equals(-1) && e.USE_AMOUNT > 0)) && e.YR.Equals(fiscalYear));
                    var exprBudgetReserve = exprReserve.Where(e => e.BUDGET_TYPE.Equals(1)); // กันเงิน งบประมาณ
                    var exprOffBudgetReserve = exprReserve.Where(e => e.BUDGET_TYPE.Equals(2)); // กันเงิน นอกงบประมาณ

                    res["netBudgetAmounts"] = expr.Sum(e => e.BUDGET_AMOUNT);
                    res["netActualBudgetAmounts"] = expr.Sum(e => e.ACTUAL_BUDGET_AMOUNT);
                    res["netUseBudgetAmounts"] = expr.Sum(e => e.USE_BUDGET_AMOUNT);
                    res["netAllocateBudgetToDepartmentAmounts"] = exprDepartmentBudget.Any() ? exprDepartmentBudget.Sum(e => e.ALLOCATE_BUDGET_AMOUNT) : decimal.Zero;

                    if (exprBudgetReserve.Any())
                    {
                        var reserveAmount = exprBudgetReserve.Sum(e => e.RESERVE_BUDGET_AMOUNT);
                        var withdrawalAmount = exprBudgetReserve.Sum(e => e.USE_AMOUNT);
                        res["netReserveBudgetAmounts"] = reserveAmount - withdrawalAmount;
                        res["netReserveWithdrawalBudgetAmounts"] = withdrawalAmount;
                    }
                    res["netRemainBudgetAmounts"] = expr.Sum(e => e.REMAIN_BUDGET_AMOUNT);

                    // ให้แก้ไขข้อมูลได้เฉพาะข้อมูลปีงบประมาณ มากกว่า หรือ เท่ากับปีงบประมาณปัจจุบัน
                    res["editAble"] = expr.First().YR.Value.CompareTo(AppUtils.GetCurrYear()) != -1;

                    // มีการทำรายการรับเงิน งปม. จากรัฐบาลมาแล้ว
                    int? budgetId = expr.First().BUDGET_ID;
                    res["budgetId"] = budgetId;
                    if (null != budgetId)
                    {
                        var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear)).FirstOrDefault();
                        res["netOffBudgetAmounts"] = exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT; //expr.Sum(e => e.OFF_BUDGET_AMOUNT);
                        res["netOffActualBudgetAmounts"] = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT; //expr.Sum(e => e.ACTUAL_OFF_BUDGET_AMOUNT);
                        res["netOffUseBudgetAmounts"] = exprBudgetMas.USE_OFF_BUDGET_AMOUNT; //expr.Sum(e => e.USE_OFF_BUDGET_AMOUNT);
                        res["netAllocateOffBudgetToDepartmentAmounts"] = exprDepartmentBudget.Any() ? exprDepartmentBudget.Sum(e => e.ALLOCATE_OFF_BUDGET_AMOUNT) : decimal.Zero;

                        if (exprOffBudgetReserve.Any())
                        {
                            var reserveAmount = exprOffBudgetReserve.Sum(e => e.RESERVE_BUDGET_AMOUNT);
                            var withdrawalAmount = exprOffBudgetReserve.Sum(e => e.USE_AMOUNT);
                            res["netReserveOffBudgetAmounts"] = reserveAmount - withdrawalAmount;
                            res["netReserveWithdrawalOffBudgetAmounts"] = withdrawalAmount;
                        }
                        res["netOffRemainBudgetAmounts"] = exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT; //expr.Sum(e => e.REMAIN_OFF_BUDGET_AMOUNT);

                        //var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.BUDGET_ID.Equals(budgetId)).FirstOrDefault();
                        res["TemporaryBudgetFlag"] = exprBudgetMas.BUDGET_FLAG; // 1 = งบประมาณปกติ, 2 = พลางก่อน
                        res["TemporaryYear"] = exprBudgetMas.TEMPORARY_YR; // พลางก่อนปีใด
                        res["ReleaseBudgetFlag"] = exprBudgetMas.RELEASE_BUDGET ? 1 : 0;
                        res["ReleaseOffBudgetFlag"] = exprBudgetMas.RELEASE_OFF_BUDGET ? 1 : 0;
                    }
                }


                // สรุปยอดคำขอ งปม. ของปีที่ขอแสดงผลข้อมูล
                // และเป็นเฉพาะคำขอต้นปี ที่ผ่านการ SignOff มาแล้วเท่านั้น
                // ดึงเฉพาะกรณี getType = 1
                if ("1".Equals(getType))
                {
                    var exprReqMas = db.T_BUDGET_REQUEST_MASTERs
                           .Where(e => e.ACTIVE.Equals(1) && e.REQ_TYPE.Equals(1) && e.SIGNOFF_FLAG == true && e.YR.Equals(fiscalYear));
                    if (exprReqMas.Any())
                        res["netBudgetRequestStartYearAmounts"] = exprReqMas.Sum(e => e.TOTAL_REQUEST_BUDGET);
                }
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// บันทึกรายการจัดสรรงบประมาณจากรัฐบาล
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitSave(BudgetReceiveFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errorText", null },
                { "errors", null }
            };

            // ตรวจสอบความถูกต้องของข้อมูลที่ส่งจากหน้าฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (model.TemporaryBudgetFlag == 2)
            {
                if (model.TemporaryYear == null)
                    modelErrors.Add("TemporaryYear", new ModelValidateErrorProperty("TemporaryYear", new List<string>(1) { "โปรดระบุปีพลางก่อน" }));
                else if (model.TemporaryYear.Value.Equals(model.FiscalYear))
                    modelErrors.Add("TemporaryYear", new ModelValidateErrorProperty("TemporaryYear", new List<string>(1) { "ระบุปีงบประมาณพลางก่อนไม่ถูกต้อง" }));
            }

            // บันทึกเป็นครั้งแรกบังคับจะต้องมีรายการ ค่าใช้จ่าย
            if (model.BudgetId == null && (model.Expenses == null || model.Expenses.Count == 0))
                modelErrors.Add("Expenses", new ModelValidateErrorProperty("Expenses", new List<string>(1) { "โปรดระบุรายการค่าใช้จ่ายที่ได้รับจัดสรรจากรัฐบาล อย่างน้อย 1 รายการ" }));

            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            if (!AppUtils.CanChangeDataByIntervalYear(model.FiscalYear, AppUtils.GetCurrYear()))
            {
                res["errorText"] = "ไม่สามารถแก้ไขข้อมูลของปีงบประมาณก่อนหน้าได้";
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.BUDGET_ID.Equals(model.BudgetId)).FirstOrDefault();
                if (null == exprBudgetMas)
                {
                    exprBudgetMas = new T_BUDGET_MASTER
                    {
                        YR = model.FiscalYear,
                        ALLOCATE_BUDGET_AMOUNT = 0, // เงินงบประมาณสุทธิ ที่จัดสรรจากรัฐบาล
                        ACTUAL_BUDGET_AMOUNT = 0, // เงินงบประมาณ ที่สามารถนำไปจัดสรร กันเงิน
                        USE_BUDGET_AMOUNT = 0,
                        REMAIN_BUDGET_AMOUNT = 0,

                        ALLOCATE_OFF_BUDGET_AMOUNT = 0, // เงินนอกงบประมาณสุทธิ ที่จัดสรรจากรัฐบาล
                        ACTUAL_OFF_BUDGET_AMOUNT = 0, // เงินนอกงบประมาณ ที่สามารถนำไปจัดสรร กันเงิน
                        USE_OFF_BUDGET_AMOUNT = 0,
                        REMAIN_OFF_BUDGET_AMOUNT = 0,

                        OFF_BUDGET_SPREAD_TO_EXPENSES = false // true = จัดเก็บรายได้ และ กระจายลงเป็นรายการ คชจ.
                    };

                    db.T_BUDGET_MASTERs.InsertOnSubmit(exprBudgetMas);
                }
                // งบประมาณปกติ หรือ พลางก่อน
                exprBudgetMas.BUDGET_FLAG = 1; // งบประมาณปกติ
                if (null != model.TemporaryBudgetFlag && model.TemporaryBudgetFlag.Value == 2)
                    exprBudgetMas.BUDGET_FLAG = 2; // งบประมาณพลางก่อน

                // เปิดใช้งบประมาณหรือยัง
                exprBudgetMas.RELEASE_BUDGET = model.ReleaseBudgetFlag.HasValue && model.ReleaseBudgetFlag.Value == 1;
                exprBudgetMas.RELEASE_OFF_BUDGET = model.ReleaseOffBudgetFlag.HasValue && model.ReleaseOffBudgetFlag.Value == 1;

                // ปรับปรุงปีงบประมาณพลางก่อน เฉพาะที่เลือกรายการเป็นพลางก่อน
                // เผื่อกรณีผู้ใช้ยกเลิกพลางก่อน และลบปีออก จะทำให้ไม่ทราบ ที่มาของข้อมูล
                if (exprBudgetMas.BUDGET_FLAG == 2)
                    exprBudgetMas.TEMPORARY_YR = model.TemporaryYear;

                // ใส่วันที่ได้รับจัดสรรงบล่าสุดจากรัฐบาลมาเมื่อไหร่
                exprBudgetMas.LATEST_ALLOCATE_BUDGET = DateTime.Now;
                exprBudgetMas.LATEST_ALLOCATE_OFF_BUDGET = DateTime.Now;

                // พลางก่อน เป็นการใช้ข้อมูลจากปีงบประมาณอื่นๆมาตั้งต้นข้อมูล
                // ดังนั้นจึงต้อง ใช้ยอด สะสม ของปีงบประมาณนั้นมาตั้งต้นยอดก่อน
                // *** เฉพาะกรณีบันทึกครั้งแรก หรือ เปลี่ยนแปลงปีพลางก่อนเป็นปีอื่นๆ ***
                if (exprBudgetMas.BUDGET_FLAG == 2 && (model.BudgetId == null || model.TemporaryYear != model.OldTemporaryYear))
                {
                    exprBudgetMas.ALLOCATE_BUDGET_AMOUNT = null == model.Expenses ? decimal.Zero : model.Expenses.Sum(e => e.BUDGET_AMOUNT);
                    exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT = null == model.Expenses ? decimal.Zero : model.Expenses.Sum(e => e.OFF_BUDGET_AMOUNT);

                    // ถ้าเปลี่ยนแปลง ปีพลางก่อน เพราะถือว่า เริ่มทำข้อมูลใหม่
                    if (model.BudgetId != null && model.TemporaryYear != model.OldTemporaryYear)
                    {
                        // ยกเลิกประวัติการรับเงินงบประมาณ และโครงการ
                        var exprBudgetExpensesReceive = db.T_BUDGET_EXPENSES_RECEIVEs.Where(e => e.YR.Equals(exprBudgetMas.YR) && e.ACTIVE.Equals(1)).ToList();
                        exprBudgetExpensesReceive.ForEach(exprExpensesReceiveItem =>
                        {
                            exprExpensesReceiveItem.ACTIVE = -1;
                            exprExpensesReceiveItem.DELETED_DATETIME = DateTime.Now;
                            exprExpensesReceiveItem.DELETED_ID = userAuthorizeProfile.EmpId;
                            exprExpensesReceiveItem.DELETE_REMARK_TEXT = string.Format("เปลี่ยนแปลงปีงบประมาณพลางก่อน {0} เป็น {1}", model.OldTemporaryYear, model.TemporaryYear);
                        });

                        // ยกเลิกโครงการ
                        var exprProjects = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.YR.Equals(exprBudgetMas.YR) && e.ACTIVE.Equals(1)).ToList();
                        exprProjects.ForEach(exprProject =>
                        {
                            exprProject.ACTIVE = -1;
                            exprProject.DELETED_DATETIME = DateTime.Now;
                            exprProject.DELETED_ID = userAuthorizeProfile.EmpId;
                        });

                        // ยกเลิกค่าใช้จ่าย
                        var exprExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(exprBudgetMas.YR)).ToList();
                        exprExpenses.ForEach(expensesItem =>
                        {
                            expensesItem.ACTIVE = -1;
                            expensesItem.DELETED_DATETIME = DateTime.Now;
                            expensesItem.DELETED_ID = userAuthorizeProfile.EmpId;
                        });
                    }
                }

                //exprBudgetMas.ALLOCATE_BUDGET_AMOUNT += null == model.Expenses ? decimal.Zero : model.Expenses.Sum(e => null == e.NewAllocateBudgetAmounts ? decimal.Zero : e.NewAllocateBudgetAmounts.Value);
                //exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT += null == model.Expenses ? decimal.Zero : model.Expenses.Sum(e => null == e.NewAllocateOffBudgetAmounts ? decimal.Zero : e.NewAllocateOffBudgetAmounts.Value);



                // บันทึกส่วน Master ก่อนเพื่อให้ได้ ID
                db.SubmitChanges();


                short? hisTemporaryYear = null;
                if (exprBudgetMas.BUDGET_FLAG.Equals(2) && (model.BudgetId == null || (model.BudgetId != null && model.OldTemporaryYear != model.TemporaryYear)))
                    hisTemporaryYear = model.TemporaryYear;

                if (null != model.Expenses)
                    model.Expenses.ForEach(currExpensesItem =>
                    {
                        // บันทึกรายการค่าใช้จ่าย
                        // ใช้ YR เพิ่มเพราะ กรณีพลางก่อน จะเป็นการใช้ข้อมูลของปี งปม. อื่นมาตั้งต้น
                        // ดังนั้นจะได้ SEQ_ID ของปีอื่นมาด้วย ทำให้ไม่ถูกสร้างเป็นรายการใหม่
                        var exprExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(model.FiscalYear) && e.SEQ_ID.Equals(currExpensesItem.SEQ_ID)).FirstOrDefault();
                        bool isNewItem = false;
                        if (null == exprExpenses)
                        {
                            isNewItem = true;
                            exprExpenses = new T_BUDGET_EXPENSE()
                            {
                                YR = exprBudgetMas.YR,
                                BUDGET_ID = exprBudgetMas.BUDGET_ID,
                                PLAN_ID = currExpensesItem.PLAN_ID,
                                PRODUCE_ID = currExpensesItem.PRODUCE_ID,
                                ACTIVITY_ID = currExpensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = currExpensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = currExpensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = currExpensesItem.EXPENSES_ID,
                                ACTIVE = 1,
                                CAN_ADD_PROJECT = currExpensesItem.CAN_ADD_PROJECT,
                                LATEST_RECEIVE_BUDGET = null,
                                LATEST_USE_BUDGET = null,

                                // เงินงบประมาณ
                                BUDGET_AMOUNT = 0,
                                ACTUAL_BUDGET_AMOUNT = 0,
                                USE_BUDGET_AMOUNT = 0,
                                REMAIN_BUDGET_AMOUNT = 0,

                                // เงินนอก งปม.
                                OFF_BUDGET_AMOUNT = 0,
                                ACTUAL_OFF_BUDGET_AMOUNT = 0,
                                USE_OFF_BUDGET_AMOUNT = 0,
                                REMAIN_OFF_BUDGET_AMOUNT = 0
                            };
                            db.T_BUDGET_EXPENSEs.InsertOnSubmit(exprExpenses);
                        }

                        decimal expensesAllocateBudgetAmounts = decimal.Zero;
                        decimal expensesAllocateOffBudgetAmounts = decimal.Zero;

                        // รายการค่าใช้จ่ายที่มีรายละเอียดย่อย
                        if (currExpensesItem.CAN_ADD_PROJECT)
                        {
                            // เป็นปีงบประมาณพลางก่อน ที่
                            // 1. บันทึกรับงบประมาณจากรัฐบาล ครั้งแรก
                            // 2. บันทึกรับงบประมาณจากรัฐบาลไปแล้ว และ มีการเปลี่ยนแปลงปีงบประมาณพลางก่อน
                            if (exprBudgetMas.BUDGET_FLAG == 2 && ((isNewItem && model.BudgetId == null) || model.TemporaryYear != model.OldTemporaryYear))
                            {
                                if (currExpensesItem.Projects == null)
                                    currExpensesItem.Projects = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.YR.Equals(model.TemporaryYear)
                                        && e.PLAN_ID == currExpensesItem.PLAN_ID
                                        && e.PRODUCE_ID == currExpensesItem.PRODUCE_ID
                                        && e.ACTIVITY_ID == currExpensesItem.ACTIVITY_ID
                                        && e.BUDGET_TYPE_ID == currExpensesItem.BUDGET_TYPE_ID
                                        && e.EXPENSES_GROUP_ID == currExpensesItem.EXPENSES_GROUP_ID
                                        && e.EXPENSES_ID == currExpensesItem.EXPENSES_ID
                                        && e.ACTIVE.Equals(1)).Select(e => new ExpensesProjectProperty()
                                        {
                                            // พลางก่อน ไม่ต้องระบุ ProjectId เพราะเป็นการคัดลอกข้อมูล
                                            // มาใช้ ในปีงบประมาณใหม่
                                            ProjectId = null,
                                            ProjectName = e.PROJECT_NAME,
                                            AllocateBudgetAmounts = e.BUDGET_AMOUNT,
                                            AllocateOffBudgetAmounts = e.OFF_BUDGET_AMOUNT
                                        }).ToList();
                            }
                            if (currExpensesItem.Projects == null)
                                currExpensesItem.Projects = new List<ExpensesProjectProperty>();

                            // รวมเงินงบประมาณ หรือ เงินนอกงบประมาณ รายโครงการภายใต้ค่าใช้จ่าย
                            // เพื่อสรุปเป็นยอดรวมทั้งสิ้น ไว้ในค่าใช้จ่าย
                            expensesAllocateBudgetAmounts = currExpensesItem.Projects.Sum(e => e.AllocateBudgetAmounts == null ? decimal.Zero : e.AllocateBudgetAmounts.Value);
                            expensesAllocateOffBudgetAmounts = currExpensesItem.Projects.Sum(e => e.AllocateOffBudgetAmounts == null ? decimal.Zero : e.AllocateOffBudgetAmounts.Value);
                            if ("budget".Equals(model.PageType))
                                exprExpenses.BUDGET_AMOUNT = expensesAllocateBudgetAmounts;
                            else if ("off_budget".Equals(model.PageType))
                                exprExpenses.OFF_BUDGET_AMOUNT = expensesAllocateOffBudgetAmounts;

                            // บันทึกโครงการภายใต้ คชจ.
                            currExpensesItem.Projects.ForEach(projectItem =>
                            {
                                var exprExpensesProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1)
                                    && e.PROJECT_ID.Equals(projectItem.ProjectId)
                                    && e.YR.Equals(exprBudgetMas.YR)).FirstOrDefault();
                                if (null == exprExpensesProject)
                                {
                                    exprExpensesProject = new T_BUDGET_EXPENSES_PROJECT()
                                    {
                                        YR = exprBudgetMas.YR,
                                        BUDGET_ID = exprBudgetMas.BUDGET_ID,
                                        PLAN_ID = currExpensesItem.PLAN_ID,
                                        PRODUCE_ID = currExpensesItem.PRODUCE_ID,
                                        ACTIVITY_ID = currExpensesItem.ACTIVITY_ID,
                                        BUDGET_TYPE_ID = currExpensesItem.BUDGET_TYPE_ID,
                                        EXPENSES_GROUP_ID = currExpensesItem.EXPENSES_GROUP_ID,
                                        EXPENSES_ID = currExpensesItem.EXPENSES_ID,
                                        PROJECT_FOR_TYPE = Convert.ToInt16("budget".Equals(model.PageType) ? 1 : 2),

                                        // เงิน งปม.
                                        BUDGET_AMOUNT = 0,
                                        ACTUAL_BUDGET_AMOUNT = 0,
                                        USE_BUDGET_AMOUNT = 0,
                                        REMAIN_BUDGET_AMOUNT = 0,

                                        // เงินนอก งปม.
                                        OFF_BUDGET_AMOUNT = 0,
                                        ACTUAL_OFF_BUDGET_AMOUNT = 0,
                                        USE_OFF_BUDGET_AMOUNT = 0,
                                        REMAIN_OFF_BUDGET_AMOUNT = 0,

                                        LATEST_RECEIVE_BUDGET_DATETIME = null,
                                        ACTIVE = 1,
                                        CREATED_DATETIME = DateTime.Now,
                                        USER_ID = userAuthorizeProfile.EmpId
                                    };
                                    db.T_BUDGET_EXPENSES_PROJECTs.InsertOnSubmit(exprExpensesProject);
                                }

                                // หากชื่อโครงการยาวมากกว่า 200 ตัวอักษร ให้ตัดส่วนเกินออก
                                exprExpensesProject.PROJECT_NAME = string.IsNullOrEmpty(projectItem.ProjectName) ? "-" : projectItem.ProjectName;
                                if (!string.IsNullOrEmpty(projectItem.ProjectName) && projectItem.ProjectName.Length > 200)
                                    exprExpensesProject.PROJECT_NAME = projectItem.ProjectName.Substring(0, 200);

                                // ยอดจัดสรรจากรัฐบาลสุทธิ ต้องไม่น้อยกว่า ยอดรับจัดสรรจริง
                                var allocateBudgetAmounts = projectItem.AllocateBudgetAmounts == null ? decimal.Zero : projectItem.AllocateBudgetAmounts.Value;
                                var allocateOffBudgetAmounts = projectItem.AllocateOffBudgetAmounts == null ? decimal.Zero : projectItem.AllocateOffBudgetAmounts.Value;
                                if (allocateBudgetAmounts.CompareTo(exprExpensesProject.ACTUAL_BUDGET_AMOUNT) != -1)
                                    exprExpensesProject.BUDGET_AMOUNT = allocateBudgetAmounts;
                                if (allocateOffBudgetAmounts.CompareTo(exprExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT) != -1)
                                    exprExpensesProject.OFF_BUDGET_AMOUNT = allocateOffBudgetAmounts;
                            });
                        }
                        else
                        {
                            // รายการค่าใช้จ่ายที่ ไม่มีรายละเอียดย่อย

                            bool isNewValue = true;
                            if (exprBudgetMas.BUDGET_FLAG == 2 && ((isNewItem && model.BudgetId == null) || model.TemporaryYear != model.OldTemporaryYear))
                            {
                                // ถ้าไม่มีการระบุจำนวนเงินใหม่ ให้ใช้จำนวนเงินจากปีพลางก่อน
                                if (null == currExpensesItem.NewAllocateBudgetAmounts && null == currExpensesItem.NewAllocateOffBudgetAmounts)
                                {
                                    isNewValue = false;
                                    var exprTempExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(model.TemporaryYear) && e.SEQ_ID.Equals(currExpensesItem.SEQ_ID)).FirstOrDefault();
                                    expensesAllocateBudgetAmounts = exprTempExpenses.BUDGET_AMOUNT;
                                    expensesAllocateOffBudgetAmounts = exprTempExpenses.OFF_BUDGET_AMOUNT;
                                }
                                else
                                {
                                    expensesAllocateBudgetAmounts = null == currExpensesItem.NewAllocateBudgetAmounts ? decimal.Zero : currExpensesItem.NewAllocateBudgetAmounts.Value;
                                    expensesAllocateOffBudgetAmounts = null == currExpensesItem.NewAllocateOffBudgetAmounts ? decimal.Zero : currExpensesItem.NewAllocateOffBudgetAmounts.Value;
                                }
                            }
                            else
                            {
                                expensesAllocateBudgetAmounts = null == currExpensesItem.NewAllocateBudgetAmounts ? decimal.Zero : currExpensesItem.NewAllocateBudgetAmounts.Value;
                                expensesAllocateOffBudgetAmounts = null == currExpensesItem.NewAllocateOffBudgetAmounts ? decimal.Zero : currExpensesItem.NewAllocateOffBudgetAmounts.Value;
                            }
                            exprExpenses.BUDGET_AMOUNT += expensesAllocateBudgetAmounts;
                            exprExpenses.OFF_BUDGET_AMOUNT += expensesAllocateOffBudgetAmounts;

                            // เงินงบประมาณ เก็บประวัติรายการค่าใช้จ่ายที่ได้รับจัดสรรเงินจากรัฐบาล
                            if (expensesAllocateBudgetAmounts.CompareTo(decimal.Zero) == 1)
                                db.T_BUDGET_EXPENSES_RECEIVEs.InsertOnSubmit(new T_BUDGET_EXPENSES_RECEIVE()
                                {
                                    YR = exprBudgetMas.YR,
                                    MN = Convert.ToInt16(DateTime.Now.Month),
                                    PLAN_ID = currExpensesItem.PLAN_ID,
                                    PRODUCE_ID = currExpensesItem.PRODUCE_ID,
                                    ACTIVITY_ID = currExpensesItem.ACTIVITY_ID,
                                    BUDGET_TYPE_ID = currExpensesItem.BUDGET_TYPE_ID,
                                    EXPENSES_GROUP_ID = currExpensesItem.EXPENSES_GROUP_ID,
                                    EXPENSES_ID = currExpensesItem.EXPENSES_ID,
                                    BUDGET_TYPE = 1,
                                    RECEIVE_BUDGET_AMOUNT = expensesAllocateBudgetAmounts,
                                    RECEIVE_DATETIME = DateTime.Now,
                                    USER_ID = userAuthorizeProfile.EmpId,
                                    REMARK_TEXT = currExpensesItem.RemarkText,
                                    TEMPORARY_YR = isNewValue ? null : hisTemporaryYear,
                                    ACTIVE = 1
                                });

                            // เงินนอกงบประมาณ เก็บประวัติรายการค่าใช้จ่ายที่ได้รับจัดสรรจากรัฐบาล
                            if (expensesAllocateOffBudgetAmounts.CompareTo(decimal.Zero) == 1)
                                db.T_BUDGET_EXPENSES_RECEIVEs.InsertOnSubmit(new T_BUDGET_EXPENSES_RECEIVE()
                                {
                                    YR = exprBudgetMas.YR,
                                    MN = Convert.ToInt16(DateTime.Now.Month),
                                    PLAN_ID = currExpensesItem.PLAN_ID,
                                    PRODUCE_ID = currExpensesItem.PRODUCE_ID,
                                    ACTIVITY_ID = currExpensesItem.ACTIVITY_ID,
                                    BUDGET_TYPE_ID = currExpensesItem.BUDGET_TYPE_ID,
                                    EXPENSES_GROUP_ID = currExpensesItem.EXPENSES_GROUP_ID,
                                    EXPENSES_ID = currExpensesItem.EXPENSES_ID,
                                    BUDGET_TYPE = 2,
                                    RECEIVE_BUDGET_AMOUNT = expensesAllocateOffBudgetAmounts,
                                    RECEIVE_DATETIME = DateTime.Now,
                                    USER_ID = userAuthorizeProfile.EmpId,
                                    REMARK_TEXT = currExpensesItem.RemarkText,
                                    TEMPORARY_YR = isNewValue ? null : hisTemporaryYear,
                                    ACTIVE = 1
                                });
                        }
                    });


                // บันทึกการเปลี่ยนแปลง รายการค่าใช้จ่าย และ รายละเอียดภายใต้รายการค่าใช้จ่าย
                db.SubmitChanges();


                // ปรับปรุงยอดงบประมาณภาพรวมของปี
                var exprTemp = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(model.FiscalYear)).Select(e => new { e.BUDGET_AMOUNT, e.OFF_BUDGET_AMOUNT });
                exprBudgetMas.ALLOCATE_BUDGET_AMOUNT = decimal.Zero;
                exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero;
                if (exprTemp.Any())
                {
                    exprBudgetMas.ALLOCATE_BUDGET_AMOUNT = exprTemp.Sum(e => e.BUDGET_AMOUNT);
                    exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT = exprTemp.Sum(e => e.OFF_BUDGET_AMOUNT);
                }
                db.SubmitChanges();
            };
            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ยกเลิกรายการค่าใช้จ่าย ที่ได้รับจัดสรรจากรัฐบาล
        /// 1.) ปรับปรุง เงินงบประมาณ หรือ เงินนอก งปม. ส่วน Master
        /// 2.) ยกเลิกประวัติการรับเงิน
        /// </summary>
        /// <param name="seqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitCancelExpenses(int? seqId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) { { "errorText", null } };
            if (null == seqId)
            {
                res["errorText"] = "รหัสอ้างอิง รายการค่าใช้จ่ายที่ต้องการยกเลิกไม่ถูกต้อง (เป็นค่า null)";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.SEQ_ID.Equals(seqId)).FirstOrDefault();
                if (null == exprExpenses || !exprExpenses.ACTIVE.Equals(1))
                    return Json(res, JsonRequestBehavior.DenyGet);


                // ไม่สามารถยกเลิกรายการค่าใช้จ่ายของ ปี งปม. อื่นได้
                if (AppUtils.GetCurrYear().CompareTo(exprExpenses.YR) == 1)
                {
                    res["errorText"] = "ไม่สามารถยกเลิกรายการค่าใช้จ่ายของปี งปม. ย้อนหลังได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                exprExpenses.ACTIVE = -1;
                exprExpenses.DELETED_DATETIME = DateTime.Now;
                exprExpenses.DELETED_ID = userAuthorizeProfile.EmpId;

                // ปรับปรุงยอดงบประมาณส่วน Master
                var exprMaster = db.T_BUDGET_MASTERs.Where(e => e.BUDGET_ID.Equals(exprExpenses.BUDGET_ID)).FirstOrDefault();
                if (null != exprMaster)
                {
                    // เงินงบประมาณ
                    exprMaster.ALLOCATE_BUDGET_AMOUNT -= exprExpenses.BUDGET_AMOUNT;
                    exprMaster.ACTUAL_BUDGET_AMOUNT -= exprExpenses.ACTUAL_BUDGET_AMOUNT;
                    exprMaster.USE_BUDGET_AMOUNT -= exprExpenses.USE_BUDGET_AMOUNT;
                    exprMaster.REMAIN_BUDGET_AMOUNT = exprMaster.ACTUAL_BUDGET_AMOUNT - exprMaster.USE_BUDGET_AMOUNT;
                    if (exprMaster.ALLOCATE_BUDGET_AMOUNT.CompareTo(exprMaster.ACTUAL_BUDGET_AMOUNT) == -1)
                    {
                        res["errorText"] = "ยอดจัดสรรเงินงบประมาณ น้อยกว่า เงินประจำงวด ไม่สามารถยกเลิกรายการค่าใช้จ่ายนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    else if (exprMaster.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "งบประมาณคงเหลือสุทธิ น้อยกว่า ศูนย์ ไม่สามารถยกเลิกรายการค่าใช้จ่ายนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    // เงินนอก งปม.
                    exprMaster.ALLOCATE_OFF_BUDGET_AMOUNT -= exprExpenses.OFF_BUDGET_AMOUNT;
                    exprMaster.ACTUAL_OFF_BUDGET_AMOUNT -= exprExpenses.ACTUAL_OFF_BUDGET_AMOUNT;
                    exprMaster.USE_OFF_BUDGET_AMOUNT -= exprExpenses.USE_OFF_BUDGET_AMOUNT;
                    exprMaster.REMAIN_OFF_BUDGET_AMOUNT = exprMaster.ACTUAL_OFF_BUDGET_AMOUNT - exprMaster.USE_OFF_BUDGET_AMOUNT;
                    if (exprMaster.ALLOCATE_OFF_BUDGET_AMOUNT.CompareTo(exprMaster.ACTUAL_OFF_BUDGET_AMOUNT) == -1)
                    {
                        res["errorText"] = "ยอดจัดสรรเงินนอกงบประมาณ น้อยกว่า จัดเก็บรายได้ ไม่สามารถยกเลิกรายการค่าใช้จ่ายนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    else if (exprMaster.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "เงินนอกงบประมาณคงเหลือสุทธิ น้อยกว่า ศูนย์ ไม่สามารถยกเลิกรายการค่าใช้จ่ายนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                }

                // ยกเลิกประวัติการรับเงิน
                var exprBudgetReceiveHis = db.T_BUDGET_EXPENSES_RECEIVEs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR == exprExpenses.YR
                    && e.EXPENSES_ID == exprExpenses.EXPENSES_ID
                    && e.EXPENSES_GROUP_ID == exprExpenses.EXPENSES_GROUP_ID
                    && e.BUDGET_TYPE_ID == exprExpenses.BUDGET_TYPE_ID
                    && e.ACTIVITY_ID == exprExpenses.ACTIVITY_ID
                    && e.PRODUCE_ID == exprExpenses.PRODUCE_ID
                    && e.PLAN_ID == exprExpenses.PLAN_ID).ToList();
                exprBudgetReceiveHis.ForEach(entity =>
                {
                    entity.ACTIVE = -1;
                    entity.DELETED_DATETIME = DateTime.Now;
                    entity.DELETED_ID = userAuthorizeProfile.EmpId;
                    entity.DELETE_REMARK_TEXT = "ยกเลิกเนื่องจาก ยกเลิกรายการค่าใช้จ่าย (โดยระบบ)";
                });

                // ยกเลิกโครงการทั้งหมด
                var exprExpensesProjects = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR == exprExpenses.YR
                    && e.PLAN_ID == exprExpenses.PLAN_ID
                    && e.PRODUCE_ID == exprExpenses.PRODUCE_ID
                    && e.ACTIVITY_ID == exprExpenses.ACTIVITY_ID
                    && e.BUDGET_TYPE_ID == exprExpenses.BUDGET_TYPE_ID
                    && e.EXPENSES_GROUP_ID == exprExpenses.EXPENSES_GROUP_ID
                    && e.EXPENSES_ID == exprExpenses.EXPENSES_ID
                    ).ToList();
                exprExpensesProjects.ForEach(entity =>
                {
                    entity.ACTIVE = -1;
                    entity.DELETED_DATETIME = DateTime.Now;
                    entity.DELETED_ID = userAuthorizeProfile.EmpId;
                });

                // ยกเลิกประวัติการรับเงินงวด
                var exprExpensesIncomes = db.T_BUDGET_EXPENSES_INCOMEs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR == exprExpenses.YR
                    && e.PLAN_ID == exprExpenses.PLAN_ID
                    && e.PRODUCE_ID == exprExpenses.PRODUCE_ID
                    && e.ACTIVITY_ID == exprExpenses.ACTIVITY_ID
                    && e.BUDGET_TYPE_ID == exprExpenses.BUDGET_TYPE_ID
                    && e.EXPENSES_GROUP_ID == exprExpenses.EXPENSES_GROUP_ID
                    && e.EXPENSES_ID == exprExpenses.EXPENSES_ID).ToList();
                exprExpensesIncomes.ForEach(entity =>
                {
                    entity.ACTIVE = -1;
                    entity.DELETED_DATETIME = DateTime.Now;
                    entity.DELETED_ID = userAuthorizeProfile.EmpId;
                    entity.DELETED_REMARK_TEXT = "ยกเลิกเนื่องจาก ยกเลิกรายการค่าใช้จ่าย (โดยระบบ)";
                });

                db.SubmitChanges();
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// แบบฟอร์มการแสดงประวัติรายการค่าใช้จ่าย ที่การรับเงินจากรัฐบาล
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetExpensesAllocateHistoryView()
        {
            return View();
        }

        /// <summary>
        /// แบบฟอร์มสำหรับบันทึกโครงการ ภายใต้รายการค่าใช้จ่ายในแต่ละปีงบประมาณ
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalExpensesProjectForm()
        {
            return View();
        }

        /// <summary>
        /// แสดงข้อมูลโครงการ ที่อยู่ภายใต้รายการค่าใช้จ่าย ในแต่ละปีงบประมาณ
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="pageType">budget = งบประมาณ, off_budget = เงินนอกงบประมาณ</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveExpensesProject(int fiscalYear, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, string pageType)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                int projectForType = 1;
                if ("off_budget".Equals(pageType))
                    projectForType = 2;
                return Json(db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR == fiscalYear
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId
                    && e.PROJECT_FOR_TYPE.Equals(projectForType))
                    .Select(e => new
                    {
                        e.PROJECT_ID,
                        e.PROJECT_NAME,

                        // ข้อมูลงบประมาณ
                        e.BUDGET_AMOUNT,
                        e.ACTUAL_BUDGET_AMOUNT,
                        e.USE_BUDGET_AMOUNT,
                        e.REMAIN_BUDGET_AMOUNT,

                        // ข้อมูลเงินนอกงบประมาณ
                        e.OFF_BUDGET_AMOUNT,
                        e.ACTUAL_OFF_BUDGET_AMOUNT,
                        e.USE_OFF_BUDGET_AMOUNT,
                        e.REMAIN_OFF_BUDGET_AMOUNT
                    }).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ยกเลิกรายชื่อโครงการภายใต้รายการค่าใช้จ่าย ในแต่ละปีงบประมาณ
        /// </summary>
        /// <param name="projectId"></param>
        [HttpPost]
        public ActionResult DeleteExpensesProject(int? projectId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) { { "errorText", null } };
            if (null == projectId)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprExpensesProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1) && e.PROJECT_ID.Equals(projectId)).FirstOrDefault();
                if (null == exprExpensesProject)
                    return Json(res, JsonRequestBehavior.DenyGet);

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                exprExpensesProject.ACTIVE = -1;
                exprExpensesProject.DELETED_DATETIME = DateTime.Now;
                exprExpensesProject.DELETED_ID = userAuthorizeProfile.EmpId;

                // ปรับปรุงรายยอดงบประมาณ/นอก งบประมาณ รายการค่าใช้จ่าย
                var exprExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR.Equals(exprExpensesProject.YR)
                    && e.PLAN_ID == exprExpensesProject.PLAN_ID
                    && e.PRODUCE_ID == exprExpensesProject.PRODUCE_ID
                    && e.ACTIVITY_ID == exprExpensesProject.ACTIVITY_ID
                    && e.BUDGET_TYPE_ID == exprExpensesProject.BUDGET_TYPE_ID
                    && e.EXPENSES_GROUP_ID == exprExpensesProject.EXPENSES_GROUP_ID
                    && e.EXPENSES_ID == exprExpensesProject.EXPENSES_ID).FirstOrDefault();
                if (null != exprExpenses)
                {
                    exprExpenses.BUDGET_AMOUNT -= exprExpensesProject.BUDGET_AMOUNT;
                    exprExpenses.ACTUAL_BUDGET_AMOUNT -= exprExpensesProject.ACTUAL_BUDGET_AMOUNT;
                    exprExpenses.USE_BUDGET_AMOUNT -= exprExpensesProject.USE_BUDGET_AMOUNT;
                    exprExpenses.REMAIN_BUDGET_AMOUNT = exprExpenses.ACTUAL_BUDGET_AMOUNT - exprExpenses.USE_BUDGET_AMOUNT;
                    if (exprExpenses.BUDGET_AMOUNT.CompareTo(exprExpenses.ACTUAL_BUDGET_AMOUNT) == -1)
                    {
                        res["errorText"] = "ยอดจัดสรรงบประมาณ น้อยกว่า เงินงบประมาณ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    else if (exprExpenses.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "งบประมาณคงเหลือสุทธิ น้อยกว่า ศูนย์ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    exprExpenses.OFF_BUDGET_AMOUNT -= exprExpensesProject.OFF_BUDGET_AMOUNT;
                    exprExpenses.ACTUAL_OFF_BUDGET_AMOUNT -= exprExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT;
                    exprExpenses.USE_OFF_BUDGET_AMOUNT -= exprExpensesProject.USE_OFF_BUDGET_AMOUNT;
                    exprExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprExpenses.ACTUAL_OFF_BUDGET_AMOUNT - exprExpenses.USE_OFF_BUDGET_AMOUNT;
                    if (exprExpenses.OFF_BUDGET_AMOUNT.CompareTo(exprExpenses.ACTUAL_OFF_BUDGET_AMOUNT) == -1)
                    {
                        res["errorText"] = "ยอดจัดสรรเงินนอกงบประมาณ น้อยกว่า จัดเก็บรายได้ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    else if (exprExpenses.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "เงินนอกงบประมาณคงเหลือสุทธิ น้อยกว่า ศูนย์ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                }


                // ปรับปรุงยอดงบประมาณ/นอก งบประมาณ ภาพรวมของกรมสรรพสามิต
                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(exprExpensesProject.YR)).FirstOrDefault();
                if (null != exprBudgetMas)
                {
                    exprBudgetMas.ALLOCATE_BUDGET_AMOUNT -= exprExpensesProject.BUDGET_AMOUNT;
                    exprBudgetMas.ACTUAL_BUDGET_AMOUNT -= exprExpensesProject.ACTUAL_BUDGET_AMOUNT;
                    exprBudgetMas.USE_BUDGET_AMOUNT -= exprExpensesProject.USE_BUDGET_AMOUNT;
                    exprBudgetMas.REMAIN_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_BUDGET_AMOUNT - exprBudgetMas.USE_BUDGET_AMOUNT;
                    if (exprBudgetMas.ALLOCATE_BUDGET_AMOUNT.CompareTo(exprBudgetMas.ACTUAL_BUDGET_AMOUNT) == -1)
                    {
                        res["errorText"] = "ยอดจัดสรรงบประมาณ น้อยกว่า เงินงบประมาณ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    else if (exprBudgetMas.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "งบประมาณคงเหลือสุทธิ น้อยกว่า ศูนย์ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT -= exprExpensesProject.OFF_BUDGET_AMOUNT;
                    exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT -= exprExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT;
                    exprBudgetMas.USE_OFF_BUDGET_AMOUNT -= exprExpensesProject.USE_OFF_BUDGET_AMOUNT;
                    exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                    if (exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT.CompareTo(exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT) == -1)
                    {
                        res["errorText"] = "ยอดจัดสรรเงินนอกงบประมาณ น้อยกว่า จัดเก็บรายได้ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    else if (exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "เงินนอกงบประมาณคงเหลือสุทธิ น้อยกว่า ศูนย์ ไม่สามารถยกเลิกโครงการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                }

                // ยกเลิกประวัติการรับเงินประจำงวดทั้งหมดของ โครงการที่ยกเลิก
                var exprProjectBudgetIncome = db.T_BUDGET_EXPENSES_INCOMEs.Where(e => e.ACTIVE.Equals(1) && e.PROJECT_ID.Equals(exprExpensesProject.PROJECT_ID)).ToList();
                exprProjectBudgetIncome.ForEach(expr =>
                {
                    expr.ACTIVE = -1;
                    expr.DELETED_ID = userAuthorizeProfile.EmpId;
                    expr.DELETED_DATETIME = DateTime.Now;
                });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ค้นหาประวัติการรับเงินที่จัดสรรจากรัฐบาล ของรายการค่าใช้จ่าย
        /// </summary>
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="pageType">all = เงินงบประมาณ และ เงินนอก งบประมาณ, budget = เงินงบประมาณ, off_budget=เงินนอกงบประมาณ</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveExpensesReceiveHistory(short fiscalYear, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, string pageType)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_BUDGET_EXPENSES_RECEIVE_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear)
                    && e.EXPENSES_ID.Equals(expensesId)
                    && e.EXPENSES_GROUP_ID.Equals(expensesGroupId)
                    && e.BUDGET_TYPE_ID.Equals(budgetTypeId)
                    && e.ACTIVITY_ID == activityId
                    && e.PRODUCE_ID == produceId
                    && e.PLAN_ID == planId);
                if ("budget".Equals(pageType))
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(1));
                else if ("off_budget".Equals(pageType))
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(2));

                var finalExpr = expr.OrderBy(e => e.RECEIVE_DATETIME).Select(e => new
                {
                    e.RECEIVE_ID,
                    e.BUDGET_TYPE,
                    e.RECEIVE_NAME,
                    e.RECEIVE_DATETIME,
                    e.RECEIVE_BUDGET_AMOUNT,
                    e.TEMPORARY_YR,
                    e.REMARK_TEXT
                }).ToList();
                return Json(finalExpr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ยกเลิกรายการรับเงินจัดสรรจากรัฐบาล ของรายการค่าใช้จ่าย
        /// </summary>
        /// <param name="receiveId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitRejectExpensesReceiveBudget(int receiveId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) {
                { "errorText", null }
            };
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_BUDGET_EXPENSES_RECEIVEs.Where(e => e.ACTIVE.Equals(1) && e.RECEIVE_ID.Equals(receiveId)).FirstOrDefault();
                if (null == expr)
                    return Json(res, JsonRequestBehavior.DenyGet);

                // ไม่สามารถยกเลิกรายการรับเงิน ปีงบประมาณก่อนหน้าได้
                if (!AppUtils.CanChangeDataByIntervalYear(expr.YR, AppUtils.GetCurrYear()))
                {
                    res["errorText"] = "ไม่สามารถยกเลิกรายการรับเงินของปีงบประมาณย้อนหลังได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                // ปรับปรุงยอดงบประมาณ
                var exprBudgetExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR.Equals(expr.YR)
                    && e.EXPENSES_ID == expr.EXPENSES_ID
                    && e.EXPENSES_GROUP_ID == expr.EXPENSES_GROUP_ID
                    && e.BUDGET_TYPE_ID == expr.BUDGET_TYPE_ID
                    && e.ACTIVITY_ID == expr.ACTIVITY_ID
                    && e.PRODUCE_ID == expr.PRODUCE_ID
                    && e.PLAN_ID == expr.PLAN_ID).FirstOrDefault();
                if (null != exprBudgetExpenses)
                {
                    decimal cancelBudgetAmounts = expr.BUDGET_TYPE.Equals(1) ? expr.RECEIVE_BUDGET_AMOUNT : decimal.Zero;
                    decimal cancelOffBudgetAmounts = expr.BUDGET_TYPE.Equals(2) ? expr.RECEIVE_BUDGET_AMOUNT : decimal.Zero;

                    // ปรับปรุงงบประมาณส่วนรายการค่าใช้จ่าย
                    exprBudgetExpenses.BUDGET_AMOUNT -= cancelBudgetAmounts;
                    //exprBudgetExpenses.REMAIN_BUDGET_AMOUNT = exprBudgetExpenses.BUDGET_AMOUNT - exprBudgetExpenses.USE_BUDGET_AMOUNT;

                    exprBudgetExpenses.OFF_BUDGET_AMOUNT -= cancelOffBudgetAmounts;
                    //exprBudgetExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetExpenses.OFF_BUDGET_AMOUNT - exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT;

                    // กรณียกเลิกรับเงินงบประมาณ แล้วทำให้ยอดคงเหลือติดลบ
                    // ให้ฟ้อง error
                    if (expr.BUDGET_TYPE.Equals(1) && exprBudgetExpenses.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                    {
                        res["errorText"] = "เงินงบประมาณคงเหลือติดลบ ไม่สามารถยกเลิกรายการนี้ได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }


                    // ปรับปรุงงบประมาณส่วน Header
                    var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.BUDGET_ID.Equals(exprBudgetExpenses.BUDGET_ID)).FirstOrDefault();
                    exprBudgetMas.ALLOCATE_BUDGET_AMOUNT -= cancelBudgetAmounts;
                    //exprBudgetMas.REMAIN_BUDGET_AMOUNT = exprBudgetMas.ALLOCATE_BUDGET_AMOUNT - exprBudgetMas.USE_BUDGET_AMOUNT;

                    exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT -= cancelOffBudgetAmounts;
                    //exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                }


                // ยกเลิกรายการรับเงิน
                expr.ACTIVE = -1;
                expr.DELETED_DATETIME = DateTime.Now;
                expr.DELETED_ID = userAuthorizeProfile.EmpId;

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetReceiveFormMapper
        {
            /// <summary>
            /// เลขกำกับ ปีงบประมาณ ค่านี้จะไม่ซ้ำกันในแต่ละปี 
            /// สามารถใช้เป็นคีย์ในการค้นหา
            /// </summary>
            public int? BudgetId { get; set; }

            /// <summary>
            /// ปีงบประมาณ ที่ได้รับเงินจากรัฐบาล
            /// </summary>
            public short FiscalYear { get; set; }

            /// <summary>
            /// ปีงบประมาณนี้ เป็นงบประมาณพลางก่อนหรือไม่
            /// 2 = พลางก่อน
            /// </summary>
            public short? TemporaryBudgetFlag { get; set; }

            /// <summary>
            /// งบประมาณพลางก่อน ของปีใด
            /// </summary>
            public short? TemporaryYear { get; set; }

            /// <summary>
            /// all = งบประมาณ และ นอกงบประมาณ, budget = งบประมาณ, off_budget = นอกงบประมาณ
            /// ข้อมูลนี้จะใช้เป็นเงื่อนไขในการอัพเดตข้อมูล เช่น การเปิดใช้ งบประมาณ หรือ เงินนอก งบประมาณ เป็นต้น
            /// </summary>
            public string PageType { get; set; }

            /// <summary>
            /// ปีงบประมาณพลางก่อน ก่อนที่ผู้ใช้งานจะแก้ไขเป็นปีอื่นๆ
            /// เนื่องจากจะส่งผลต่อการนำข้อมูล จำนวนเงิน งปม. มาตั้งต้นให้กับปีนั้น
            /// เช่น หากทำเป็นพลางก่อนไว้แล้ว และ แก้ไขปีเป็นพลางก่อนปีอื่น จะต้องใช้ข้อมูลปีที่แก้ไขไปล่าสุดมาตั้งต้นให้ใหม่ เป็นต้น
            /// </summary>
            public short? OldTemporaryYear { get; set; }

            /// <summary>
            /// เงินงบประมาณ เปิดให้เริ่มจัดสรรได้หรือยัง
            /// 1 = เปิดให้เริ่มจัดสรรได้
            /// </summary>
            public short? ReleaseBudgetFlag { get; set; }

            /// <summary>
            /// เงินนอก งปม. เปิดให้เริ่มจัดสรรได้หรือยัง
            /// 1 = เปิดให้เริ่มจัดสรรได้
            /// </summary>
            public short? ReleaseOffBudgetFlag { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่ได้รับจัดสรรจากรัฐบาล
            /// </summary>
            //[Required(ErrorMessage = "โปรดระบุรายการค่าใช้จ่ายที่ได้รับจัดสรรจากรัฐบาล อย่างน้อย 1 รายการ")]
            public List<ExpensesAllocateBudgetProperty> Expenses { get; set; }
        }

        public class ExpensesAllocateBudgetProperty
        {
            /// <summary>
            /// เลขที่รายการของ รายการค่าใช้จ่าย ที่ถูกบันทึกไว้ก่อนหน้าแล้ว
            /// </summary>
            public int? SEQ_ID { get; set; }

            /// <summary>
            /// แผนงาน
            /// </summary>
            public int? PLAN_ID { get; set; }

            /// <summary>
            /// ผลผลิต
            /// </summary>
            public int? PRODUCE_ID { get; set; }

            /// <summary>
            /// กิจกรรม
            /// </summary>
            public int? ACTIVITY_ID { get; set; }

            /// <summary>
            /// งบรายจ่าย (งบดำเนินงาน งบอุดหนุน งบอื่นๆ ฯลฯ)
            /// </summary>
            public int BUDGET_TYPE_ID { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_GROUP_ID { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่ได้รับเงิน งปม. รายรัฐบาล
            /// </summary>
            public int EXPENSES_ID { get; set; }

            /// <summary>
            /// รายการ คชจ. นี้สามารถเพิ่มโครงการได้หรือไม่
            /// </summary>
            public bool CAN_ADD_PROJECT { get; set; }

            /// <summary>
            /// เงินงบประมาณจัดสรร สะสม
            /// </summary>
            public decimal BUDGET_AMOUNT { get; set; }

            /// <summary>
            /// จำนวนเงิน งปม. ที่จัดสรรจากรัฐบาล
            /// </summary>
            public decimal? NewAllocateBudgetAmounts { get; set; }

            /// <summary>
            /// เงินนอก งปม. จัดสรรสะสม
            /// </summary>
            public decimal OFF_BUDGET_AMOUNT { get; set; }

            /// <summary>
            /// จำนวนเงินนอก งปม. ที่จัดสรรจากรัฐบาล
            /// </summary>
            public decimal? NewAllocateOffBudgetAmounts { get; set; }

            [MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }

            /// <summary>
            /// รายชื่อโครงการทั้งหมดที่อยู่ภายใต้ ค่าใช้จ่าย ในแต่ละปีงบประมาณ
            /// </summary>
            public List<ExpensesProjectProperty> Projects { get; set; }
        }


        public class ExpensesProjectProperty
        {
            /// <summary>
            /// รหัสโครงการที่สร้างโดยระบบ
            /// </summary>
            public int? ProjectId { get; set; }

            /// <summary>
            /// ชื่อโครงการที่อยู่ภายใต้รายการค่าใช้จ่าย ในแต่ล่ะปีงบประมาณ
            /// </summary>
            public string ProjectName { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณสุทธิ ที่ได้รับจัดสรรจากรัฐิบาย
            /// </summary>
            public decimal? AllocateBudgetAmounts { get; set; }

            /// <summary>
            /// จำนวนเงินนอก งปม. สุทธิ ที่ได้รับจัดสรรจากรัฐบาล
            /// </summary>
            public decimal? AllocateOffBudgetAmounts { get; set; }
        }
    }
}