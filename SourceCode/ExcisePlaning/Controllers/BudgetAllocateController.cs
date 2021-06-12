using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Compilation;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// จัดสรรเงินงบประมาณที่มีอยู่ ลงไปให้กับหน่วยงานต่างๆในพื้นที่ โดยใช้ข้อมูลคำของบประมาณ
    /// เป็นตัวอ้างอิงรายการค่าใช้จ่าย ที่จะจัดสรรไปลงไปให้กับหน่วยงาน
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetAllocateController : Controller
    {
        // GET: BudgetAllocate
        public ActionResult GetForm()
        {

            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_ALLOCATE_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_ALLOCATE_MENU;
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
                ActionName = menuItem.ActionName
            });
            ViewBag.Breadcrumps = breadcrumps;

            ViewBag.DefaultYear = AppUtils.GetCurrYear();
            ViewBag.DefaultAreaId = userAuthorizeProfile.AreaId;
            ViewBag.DefaultDepartmentId = userAuthorizeProfile.DepId;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // เขตพื้นที่
                ViewBag.Areas = db.T_AREAs.Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_CODE = e.AREA_CODE,
                    AREA_NAME = e.AREA_NAME
                }).ToList();
            }

            return View();
        }


        /// <summary>
        /// โหลดหน้าฟอร์มสำหรับจัดสรรงบประมาณไปยังหน่วยงาน
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetAllocateModalForm()
        {
            return View();
        }


        /// <summary>
        /// ค้นหาคำของบประมาณ เพื่อจัดสรร
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="depId"></param>
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="budgetTypeFlag">1 = คำขอต้นปี, 2 = คำขอเพิ่มเติม</param>
        /// <param name="searchType">1 = ค้นหาเพื่อแก้ไขรายการที่อนุมัติไปแล้ว</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int? areaId, int? depId, int fiscalYear, short? budgetTypeFlag, short? searchType, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_BUDGET_REQUEST_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear));

                // คำขอต้นปีต้องผ่านการ SignOff มาก่อน
                expr = expr.Where(e => (e.REQ_TYPE.Equals(1) && e.SIGNOFF_FLAG == true) || e.REQ_TYPE.Equals(2));

                // เขตพื้นที่
                if (null != areaId)
                    expr = expr.Where(e => e.AREA_ID.Equals(areaId));

                // หน่วยงาน
                if (null != depId)
                    expr = expr.Where(e => e.DEP_ID.Equals(depId));

                // ประเภทคำขอ (คำขอต้นปี คำขอเพิ่มเติม)
                if (null != budgetTypeFlag)
                    expr = expr.Where(e => e.REQ_TYPE.Equals(budgetTypeFlag.Value));

                // ค้นหาเฉพาะรายการที่จัดสรรแล้ว เพื่อนำมาแก้ไข
                if (null != searchType && searchType == 1)
                    expr = expr.Where(e => e.PROCESS_STATUS.Equals(1)); // จัดสรรแล้ว
                else
                    expr = expr.Where(e => e.PROCESS_STATUS.Equals(0)); // รอจัดสรร

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.REQ_ID,
                    e.REQ_TYPE,
                    e.REQ_COUNT, // ครั้งที่ส่งคำขอ งบเพิ่มเติม
                    e.AREA_ID,
                    e.AREA_NAME,
                    e.DEP_ID,
                    e.DEP_NAME,
                    e.TOTAL_REQUEST_BUDGET,
                    e.CREATED_NAME,
                    e.CREATED_DATETIME,
                    e.REMARK_TEXT,
                    e.BUDGET_TYPE,
                    e.PROCESS_STATUS
                }).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ค้นหาข้อมูลรายการค่าใช้จ่ายภายใต้คำขอ งปม. เพื่อจัดสรรงบประมาณไปยังหน่วยงาน
        /// ตามรายการ คชจ. ที่ทำคำขอเข้ามา 
        /// หมายเหตุ: แสดงเฉพาะรายการค่าใช้จ่ายที่ยอดคำขอมากกว่า 0 บาท
        /// </summary>
        /// <param name="reqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveRequestBudgetExpenses(string reqId)
        {
            if (string.IsNullOrEmpty(reqId))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_DEPARTMENT_REQUEST_EXPENSES_BUDGET_AND_ALLOCATE_INFORMATIONs.Where(e => e.REQ_ID.Equals(reqId) && e.REQUEST_BUDGET_AMOUNT > 0)
                    .Select(e => new
                    {
                        // ข้อมูลคำขอ งปม.
                        e.REQ_ID,
                        e.YR,
                        e.AREA_ID,
                        e.AREA_NAME,
                        e.DEP_ID,
                        e.DEP_NAME,
                        e.BUDGET_TYPE, // 1 = เงินงบประมาณ, 2 = เงินนอก งปม.
                        e.REQ_TYPE, // 1 = คำขอต้นปี, 2 = คำขอเพิ่มเติม
                        e.NET_REQUEST_BUDGET_AMOUNT, // ยอดสุทธิในใบคำขอ
                        e.REMARK_TEXT, // หมายเหตุของคำขอ

                        e.PLAN_ID,
                        e.PLAN_NAME,
                        e.PRODUCE_ID,
                        e.PRODUCE_NAME,
                        e.ACTIVITY_ID,
                        e.ACTIVITY_NAME,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.BUDGET_TYPE_GOVERNMENT_REFER_CODE,
                        e.BUDGET_TYPE_SHARED_BUDGET, // true = ค่าใช้จ่าย ภายใต้ งบรายจ่ายนี้ สามารถใช้จ่ายแบบถัวเฉลี่ยได้
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_GROUP_GOVERNMENT_REFER_CODE,
                        e.EXPENSES_ID,
                        e.EXPENSES_NAME,
                        e.CAN_ADD_PROJECT,
                        e.REQUEST_BUDGET_AMOUNT, // จำนวนเงินต่อรายการค่าใช้จ่ายที่ส่งคำขอ
                        e.REQ_COUNT, // ขอเพิ่มเติม ครั้งที่?

                        // งบประมาณคงเหลือสุทธิ ของรายการค่าใช้จ่าย
                        e.BUDGET_FLAG, // 1 = ปกติ, 2 = พลางก่อน
                        e.TEMPORARY_YR, // พลางก่อนของปี งปม. ใด
                        e.BALANCE_BUDGET_AMOUNT, // เงินงบประมาณคงเหลือสุทธิ
                        e.BALANCE_OFF_BUDGET_AMOUNT, // เงินนอก งปม. คงเหลือสุทธิ

                        // ข้อมูลงบประมาณของรายการค่าใช้จ่าย ของหน่วยงาน
                        e.DEP_ALLOCATE_BUDGET_AMOUNT,
                        e.DEP_ALLOCATE_OFF_BUDGET_AMOUNT,
                        e.DEP_NET_BUDGET_AMOUNT,
                        e.DEP_USE_BUDGET_AMOUNT,
                        e.DEP_REMAIN_BUDGET_AMOUNT
                    }).GroupBy(e => new
                    {
                        e.PLAN_ID,
                        e.PLAN_NAME,
                        e.PRODUCE_ID,
                        e.PRODUCE_NAME,
                        e.ACTIVITY_ID,
                        e.ACTIVITY_NAME,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_GOVERNMENT_REFER_CODE,
                        e.BUDGET_TYPE_NAME,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_GOVERNMENT_REFER_CODE,
                        e.EXPENSES_GROUP_NAME
                    }).Select(e => new
                    {
                        GroupBy = e.Key,
                        Expenses = e
                    }).ToList();


                return Json(expr, JsonRequestBehavior.DenyGet);
            };
        }


        /// <summary>
        /// แบบฟอร์มสำหรับ จัดสรรงบประมาณ รายโครงการลงไปให้กับหน่วยงาน
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetAllocateModalProjectForm()
        {
            return View();
        }

        /// <summary>
        /// รายชื่อโครงการ ที่จัดสรรเงินงบประมาณ ลงไปให้กับหน่วยงาน
        /// </summary>
        /// <param name="budgetType">1 = งบประมาณ, 2 = นอกงบประมาณ</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetAllocateExpensesProjectBudgetToDepartment(int depId, int fiscalYear, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, int budgetType)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.proc_GetAllocateProjectBudgetToDepartment(fiscalYear, depId, planId, produceId, activityId, budgetTypeId, expensesGroupId, expensesId, budgetType).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ปฏิเสธการจัดสรรคำขอ งปม. จากหน่วยงาน
        /// </summary>
        /// <param name="reqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitDecline(string reqId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) {
                { "errorText", null }
            };

            if (string.IsNullOrEmpty(reqId))
            {
                res["errorText"] = "โปรดระบุเลขที่คำของบประมาณ";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.REQ_ID.Equals(reqId)).FirstOrDefault();
                if (expr == null)
                {
                    res["errorText"] = string.Format("ไม่พบเลขที่คำของบประมาณ {0} ในระบบ", reqId);
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ยกเลิกคำขอ
                if (expr.ACTIVE.Equals(-1))
                {
                    res["errorText"] = string.Format("เลขที่คำขอ {0} ถูกยกเลิกไปแล้วเวลา {1}", reqId, expr.DELETED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ไม่จัดสรร
                if (expr.PROCESS_STATUS.Equals(-1))
                {
                    res["errorText"] = string.Format("เลขที่คำขอ {0} ถูกปฏิเสธคำขอไปแล้ว เวลา {1}", reqId, expr.UPDATED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // จัดสรร
                if (expr.PROCESS_STATUS.Equals(1))
                {
                    res["errorText"] = string.Format("เลขที่คำขอ {0} จัดสรรไปแล้ว เวลา {1}", reqId, expr.APPROVE_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                expr.PROCESS_STATUS = -1; // ไม่จัดสรร
                expr.UPDATED_DATETIME = DateTime.Now;
                expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                db.SubmitChanges();
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// จัดสรรงบประมาณตามคำขอของหน่วยงาน
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitSave(BudgetAllocateFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errorText", null },
                { "errors", null }
            };

            // ตรวจสอบความถูกต้องของค่าที่ส่งจากฟอร์ม
            if (model.Expenses == null)
            {
                res["errorText"] = "ไม่มีรายการค่าใช้จ่ายที่จะจัดสรร";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบความถูกต้องของข้อมูล ในแต่ละรายการค่าใช้จ่าย
            List<Dictionary<string, ModelValidateErrorProperty>> expensesErrors = new List<Dictionary<string, ModelValidateErrorProperty>>();
            model.Expenses.ForEach(expensesItem =>
            {
                expensesErrors.Add(ModelValidateErrorProperty.TryOneValidate(expensesItem));
            });
            if (expensesErrors.Any(e => null != e))
            {
                res["errors"] = new Dictionary<string, object>(1) { { "Expenses", expensesErrors } };
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprReq = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.ACTIVE.Equals(1) && e.REQ_ID.Equals(model.ReqId)).FirstOrDefault();
                if (null == exprReq)
                {
                    res["errorText"] = string.Format("ไม่พบข้อมูลคำขอ (เลขที่คำขอ {0})", model.ReqId);
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                // คำขอถูกจัดสรรไปแล้ว
                if (exprReq.PROCESS_STATUS.Equals(1))
                {
                    res["errorText"] = string.Format("คำขอนี้ จัดสรรงบประมาณไปแล้ว เมื่อ {0}", exprReq.APPROVE_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                // คำขอถูกปฏิเสธการจัดสรร
                if (exprReq.PROCESS_STATUS.Equals(-1))
                {
                    res["errorText"] = string.Format("คำขอนี้ ปฏิเสธการจัดสรร เมื่อ {0}", exprReq.APPROVE_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                // เป็นคำของบประมาณปีก่อนหน้า
                if (exprReq.YR < AppUtils.GetCurrYear())
                {
                    res["errorText"] = "คำขอนี้ เป็นคำขอของปีงบประมาณก่อนหน้า ไม่สามารถจัดสรรได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                // งบประมาณคงคลัง
                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(exprReq.YR)).FirstOrDefault();
                if (null == exprBudgetMas)
                {
                    res["errorText"] = "กรุณาบันทึกข้อมูลเงินงบประมาณที่ได้รับจากรัฐบาลลงระบบก่อน (จัดสรรงบประมาณ => จัดการเงินงบประมาณ)";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (model.AllocateBudgetTypeFlag.Equals(1) && !exprBudgetMas.RELEASE_BUDGET)
                {
                    res["errorText"] = "กรุณาเปิดใช้งาน เงินงบประมาณกรมสรรพสามิตก่อน";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (model.AllocateBudgetTypeFlag.Equals(2) && !exprBudgetMas.RELEASE_OFF_BUDGET)
                {
                    res["errorText"] = "กรุณาเปิดใช้งาน เงินนอกงบประมาณกรมสรรพสามิตก่อน";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                // ตรวจสอบงบประมาณคงคลังของแต่ละรายการค่าใช้จ่าย หรือ โครงการ เพียงพอต่อการจัดสรรหรือไม่
                Dictionary<string, Dictionary<string, object>> expensesAllocateErrors = new Dictionary<string, Dictionary<string, object>>();
                Dictionary<string, decimal> actualBudgetAmountsAllGroups = new Dictionary<string, decimal>(); // จำนวนเงินงบที่สามารถจัดสรรได้ ในแต่ละกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวด คชจ.)
                model.Expenses.ForEach(expensesItem =>
                {
                    // ค้นหายอดรวม งบประมาณรายกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย) เพื่อนำไปใช้ตรวจสอบการจัดสรรเกินเงื่อนไข
                    string groupKeyName = string.Format("{0}_{1}_{2}"
                            , expensesItem.PLAN_ID
                            , expensesItem.PRODUCE_ID
                            , expensesItem.BUDGET_TYPE_ID);
                    if (!actualBudgetAmountsAllGroups.ContainsKey(groupKeyName))
                        actualBudgetAmountsAllGroups.Add(groupKeyName, BudgetUtils.GetTotalActualBudgetOrOffBudgetBalanceByGroup(exprReq.YR
                                , expensesItem.PLAN_ID, expensesItem.PRODUCE_ID
                                , expensesItem.BUDGET_TYPE_ID
                                , model.AllocateBudgetTypeFlag));
                    decimal netActualBudgetAmountsByGroup = actualBudgetAmountsAllGroups[groupKeyName];

                    // ข้อผิดพลาดในแต่ละ รายการ คชจ. 
                    // เนื่องจากใน 1 คำขอมีหลายกลุ่ม ทำให้ต้องแสดงข้อผิดพลาดรายการ คชจ. ให้ตรงตามกลุ่ม
                    // กลุ่ม: แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย
                    string errorKeyName = string.Format("{0}_{1}_{2}_{3}_{4}_{5}"
                            , expensesItem.PLAN_ID
                            , expensesItem.PRODUCE_ID
                            , expensesItem.ACTIVITY_ID
                            , expensesItem.BUDGET_TYPE_ID
                            , expensesItem.EXPENSES_GROUP_ID
                            , expensesItem.EXPENSES_ID);
                    expensesAllocateErrors.Add(errorKeyName, null);

                    if (expensesItem.NewAllocateAmounts != null)
                        if (null != expensesItem.Projects)
                        {
                            // ตรวจสอบงบประมาณคงเหลือสุทธิ รายโครงการ
                            List<Dictionary<string, ModelValidateErrorProperty>> projectErrors = new List<Dictionary<string, ModelValidateErrorProperty>>();
                            foreach (var projectItem in expensesItem.Projects)
                            {
                                if (projectItem.NewAllocateAmounts == null)
                                {
                                    projectErrors.Add(null);
                                    continue;
                                }

                                var result = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprReq.YR
                                        , projectItem.PLAN_ID, projectItem.PRODUCE_ID
                                        , projectItem.ACTIVITY_ID, projectItem.BUDGET_TYPE_ID
                                        , projectItem.EXPENSES_GROUP_ID, projectItem.EXPENSES_ID
                                        , projectItem.PROJECT_ID, model.AllocateBudgetTypeFlag, BudgetUtils.ADJUSTMENT_PAY
                                        , projectItem.NewAllocateAmounts.Value, ref netActualBudgetAmountsByGroup);
                                if (!result.Completed)
                                    projectErrors.Add(new Dictionary<string, ModelValidateErrorProperty>(1) {
                                        { "NewAllocateAmounts", new ModelValidateErrorProperty("NewAllocateAmounts", new List<string>(1){ result.CauseErrorMessage }) }
                                    });
                                else
                                    projectErrors.Add(null);
                            }

                            // เก็บ Error ของโครงการ
                            if (projectErrors.Any(e => e != null))
                                expensesAllocateErrors[errorKeyName] = new Dictionary<string, object>(2) {
                                    { "NewAllocateAmounts", new ModelValidateErrorProperty("NewAllocateAmounts", new List<string>(1){ "จัดสรรงบ โครงการไม่ถูกต้อง" }) },
                                    { "Projects", projectErrors }
                                };
                        }
                        else
                        {
                            // ตรวจสอบงบประมาณคงเหลือสุทธิ รายการค่าใช้จ่าย
                            var adjustmentResult = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, exprReq.YR
                                    , expensesItem.PLAN_ID, expensesItem.PRODUCE_ID
                                    , expensesItem.ACTIVITY_ID, expensesItem.BUDGET_TYPE_ID
                                    , expensesItem.EXPENSES_GROUP_ID, expensesItem.EXPENSES_ID
                                    , null, model.AllocateBudgetTypeFlag, BudgetUtils.ADJUSTMENT_PAY
                                    , expensesItem.NewAllocateAmounts.Value, ref netActualBudgetAmountsByGroup);
                            if (!adjustmentResult.Completed)
                                expensesAllocateErrors[errorKeyName] = new Dictionary<string, object>(2) {
                                    { "NewAllocateAmounts", new ModelValidateErrorProperty("NewAllocateAmounts", new List<string>(1){ adjustmentResult.CauseErrorMessage}) },
                                    { "Projects", null }
                                };
                        }

                    // นำค่าที่เปลี่ยนแปลง ยอดรวมงบประมาณรายกลุ่มมาปรับปรุง เพื่อใช้ในรอบถัดไป
                    actualBudgetAmountsAllGroups[groupKeyName] = netActualBudgetAmountsByGroup;
                });

                // ยกเลิกรายการที่รออัพเดตทั้งหมดทิ้งไป จากการตรวจสอบ งบประมาณคงคลังคงเหลือ
                db.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, db.GetChangeSet().Updates);
                if (expensesAllocateErrors.Any(e => null != e.Value))
                {
                    res["errors"] = new Dictionary<string, object>(1) { { "Expenses", expensesAllocateErrors } };
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                // ปรับปรุงสถานะ และ ผู้ทำรายการจัดสรร ในคำขอ
                exprReq.PROCESS_STATUS = 1;
                exprReq.APPROVE_DATETIME = DateTime.Now;
                exprReq.APPROVE_ID = userAuthorizeProfile.EmpId;

                // ข้อมูลงบประมาณที่หน่วยงานได้รับ
                var exprAllocateMas = db.T_BUDGET_ALLOCATEs.Where(e => e.YR.Equals(exprReq.YR) && e.DEP_ID.Equals(exprReq.DEP_ID)).FirstOrDefault();
                if (null == exprAllocateMas)
                {
                    var fiscalYear2Digits = (exprReq.YR + 543).ToString().Substring(2);
                    string allocateId = AppUtils.GetNextKey("BUDGET_ALLOCATE.ALLOCATE_ID", string.Format("A{0}", fiscalYear2Digits), 8, false, true);
                    exprAllocateMas = new T_BUDGET_ALLOCATE()
                    {
                        ALLOCATE_ID = allocateId,
                        DEP_ID = exprReq.DEP_ID,
                        SUB_DEP_ID = exprReq.SUB_DEP_ID,
                        YR = exprReq.YR,
                        ALLOCATE_BUDGET_AMOUNT = decimal.Zero,
                        ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero,
                        NET_BUDGET_AMOUNT = decimal.Zero,
                        USE_BUDGET_AMOUNT = decimal.Zero,
                        REMAIN_BUDGET_AMOUNT = decimal.Zero,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        // ในปีงบประมาณ ยังไม่มีรายการจัดสรร ให้ถือว่าคำขอแรก
                        // เป็นข้อมูลครั้งล่าสุดที่ของบประมาณ
                        LATEST_REQUEST_DATETIME = exprReq.CREATED_DATETIME,
                        LATEST_REQUEST_ID = exprReq.USER_ID
                    };
                    db.T_BUDGET_ALLOCATEs.InsertOnSubmit(exprAllocateMas);
                }

                decimal allocateBudgetAmounts = decimal.Zero;
                decimal allocateOffBudgetAmounts = decimal.Zero;
                if (model.AllocateBudgetTypeFlag == 1)
                    allocateBudgetAmounts = model.Expenses.Sum(e => null == e.NewAllocateAmounts ? decimal.Zero : e.NewAllocateAmounts.Value);
                else
                    allocateOffBudgetAmounts = model.Expenses.Sum(e => null == e.NewAllocateAmounts ? decimal.Zero : e.NewAllocateAmounts.Value);

                exprAllocateMas.ALLOCATE_BUDGET_AMOUNT += allocateBudgetAmounts;
                exprAllocateMas.ALLOCATE_OFF_BUDGET_AMOUNT += allocateOffBudgetAmounts;
                exprAllocateMas.NET_BUDGET_AMOUNT = exprAllocateMas.ALLOCATE_BUDGET_AMOUNT + exprAllocateMas.ALLOCATE_OFF_BUDGET_AMOUNT;
                exprAllocateMas.REMAIN_BUDGET_AMOUNT = exprAllocateMas.NET_BUDGET_AMOUNT - exprAllocateMas.USE_BUDGET_AMOUNT;

                exprAllocateMas.LATEST_ALLOCATE_DATETIME = DateTime.Now;
                exprAllocateMas.LATEST_ALLOCATE_ID = userAuthorizeProfile.EmpId;

                // ปรับปรุงยอดคงเหลือ และ เพิ่มยอดการใช้จ่ายงบประมาณ ในคลัง (ส่วน Master)
                exprBudgetMas.USE_BUDGET_AMOUNT += allocateBudgetAmounts;
                exprBudgetMas.REMAIN_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_BUDGET_AMOUNT - exprBudgetMas.USE_BUDGET_AMOUNT;
                exprBudgetMas.USE_OFF_BUDGET_AMOUNT += allocateOffBudgetAmounts;
                exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;


                // งบประมาณที่หน่วยงานได้รับ แยกลงแต่ละรายการค่าใช้จ่าย 
                // กรณีไม่มีโครงการภายใต้ คชจ.
                model.Expenses.Where(e => e.NewAllocateAmounts != null && e.Projects == null).ToList().ForEach(expensesItem =>
                {
                    allocateBudgetAmounts = decimal.Zero;
                    allocateOffBudgetAmounts = decimal.Zero;
                    if (model.AllocateBudgetTypeFlag == 1)
                        allocateBudgetAmounts = expensesItem.NewAllocateAmounts.Value;
                    else
                        allocateOffBudgetAmounts = expensesItem.NewAllocateAmounts.Value;

                    var exprAllocateExpenses = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                        && e.YR.Equals(exprReq.YR)
                        && e.DEP_ID.Equals(exprAllocateMas.DEP_ID)
                        && e.PLAN_ID == expensesItem.PLAN_ID
                        && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                        && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                        && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                        && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                        && e.EXPENSES_ID == expensesItem.EXPENSES_ID).FirstOrDefault();
                    if (null == exprAllocateExpenses)
                    {
                        exprAllocateExpenses = new T_BUDGET_ALLOCATE_EXPENSE()
                        {
                            ALLOCATE_ID = exprAllocateMas.ALLOCATE_ID,
                            DEP_ID = exprAllocateMas.DEP_ID,
                            SUB_DEP_ID = exprAllocateMas.SUB_DEP_ID,
                            YR = exprAllocateMas.YR,
                            PLAN_ID = expensesItem.PLAN_ID,
                            PRODUCE_ID = expensesItem.PRODUCE_ID,
                            ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                            BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                            EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                            EXPENSES_ID = expensesItem.EXPENSES_ID,
                            PROJECT_ID = null,
                            ALLOCATE_COUNT = 1,
                            ALLOCATE_BUDGET_AMOUNT = decimal.Zero,
                            ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero,
                            NET_BUDGET_AMOUNT = decimal.Zero,
                            USE_BUDGET_AMOUNT = decimal.Zero,
                            REMAIN_BUDGET_AMOUNT = decimal.Zero,
                            CREATED_DATETIME = DateTime.Now,
                            USER_ID = userAuthorizeProfile.EmpId,
                            ACTIVE = 1
                        };
                        db.T_BUDGET_ALLOCATE_EXPENSEs.InsertOnSubmit(exprAllocateExpenses);
                    }
                    else
                    {
                        exprAllocateExpenses.UPDATED_DATETIME = DateTime.Now;
                        exprAllocateExpenses.UPDATED_ID = userAuthorizeProfile.EmpId;
                        exprAllocateExpenses.ALLOCATE_COUNT += 1;
                    }

                    exprAllocateExpenses.ALLOCATE_BUDGET_AMOUNT += allocateBudgetAmounts;
                    exprAllocateExpenses.ALLOCATE_OFF_BUDGET_AMOUNT += allocateOffBudgetAmounts;
                    exprAllocateExpenses.NET_BUDGET_AMOUNT = exprAllocateExpenses.ALLOCATE_BUDGET_AMOUNT + exprAllocateExpenses.ALLOCATE_OFF_BUDGET_AMOUNT;
                    exprAllocateExpenses.REMAIN_BUDGET_AMOUNT = exprAllocateExpenses.NET_BUDGET_AMOUNT - exprAllocateExpenses.USE_BUDGET_AMOUNT;

                    // บันทึกประวัติการจัดสรรงบประมาณ
                    //short allocateExpensesCount = 0; // ครั้งที่จัดสรรเงิน งปม. ให้กับรายการ คชจ.
                    //allocateExpensesCount = Convert.ToInt16(db.T_BUDGET_ALLOCATE_EXPENSES_HISTORies.Where(e => e.DEP_ID.Equals(exprAllocateMas.DEP_ID)
                    //        && e.YR.Equals(exprAllocateMas.YR)
                    //        && e.PLAN_ID == expensesItem.PLAN_ID
                    //        && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                    //        && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                    //        && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                    //        && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                    //        && e.EXPENSES_ID == expensesItem.EXPENSES_ID).Count());
                    ////&& db.T_BUDGET_REQUEST_MASTERs.Any(mas => mas.REQ_ID.Equals(e.REQ_ID) && mas.REQ_TYPE.Equals(2))).Count();
                    //allocateExpensesCount++;
                    //exprAllocateExpenses.ALLOCATE_COUNT = allocateExpensesCount;

                    db.T_BUDGET_ALLOCATE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_ALLOCATE_EXPENSES_HISTORY()
                    {
                        DEP_ID = exprAllocateMas.DEP_ID,
                        SUB_DEP_ID = exprAllocateMas.SUB_DEP_ID,
                        YR = exprAllocateMas.YR,
                        MN = Convert.ToInt16(DateTime.Now.Month),
                        REQ_ID = exprReq.REQ_ID,
                        PLAN_ID = expensesItem.PLAN_ID,
                        PRODUCE_ID = expensesItem.PRODUCE_ID,
                        ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                        BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                        EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                        EXPENSES_ID = expensesItem.EXPENSES_ID,
                        PROJECT_ID = null,
                        ALLOCATE_TYPE = model.AllocateBudgetTypeFlag, // จัดสรรจาก เงินงบประมาณ หรือ เงินนอก งปม.
                        REMARK_TEXT = expensesItem.RemarkText,
                        BUDGET_TYPE = exprReq.BUDGET_TYPE, // คำขอขอจาก เงินงบประมาณ หรือ เงินนอก งปม.
                        ALLOCATE_BUDGET_AMOUNT = allocateBudgetAmounts.CompareTo(decimal.Zero) == 1 ? allocateBudgetAmounts : allocateOffBudgetAmounts,
                        ALLOCATE_DATETIME = DateTime.Now,
                        ALLOCATE_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1,
                        SEQ_NO = exprAllocateExpenses.ALLOCATE_COUNT.Value // จัดสรรเพิ่มเติมครั้งที่
                    });


                    // ปรับปรุงยอดคงเหลือ และ เพื่อยอดใช้จ่ายงบประมาณ ของรายการค่าใช้จ่ายในคลัง
                    var exprBudgetExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                        && e.YR.Equals(exprReq.YR)
                        && e.PLAN_ID == expensesItem.PLAN_ID
                        && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                        && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                        && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                        && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                        && e.EXPENSES_ID == expensesItem.EXPENSES_ID).FirstOrDefault();
                    if (null != exprBudgetExpenses)
                    {
                        exprBudgetExpenses.USE_BUDGET_AMOUNT += allocateBudgetAmounts;
                        exprBudgetExpenses.REMAIN_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_BUDGET_AMOUNT - exprBudgetExpenses.USE_BUDGET_AMOUNT;

                        exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT += allocateOffBudgetAmounts;
                        exprBudgetExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT;

                        exprBudgetExpenses.LATEST_USE_BUDGET = DateTime.Now;
                    }
                });



                // งบประมาณที่หน่วยงานได้รับ แยกลงแต่ละรายการค่าใช้จ่าย 
                // กรณีมีโครงการภายใต้ คชจ.
                model.Expenses.Where(e => e.NewAllocateAmounts != null && e.Projects != null).ToList().ForEach(expensesItem =>
                {
                    expensesItem.Projects.ForEach(projectItem =>
                    {
                        allocateBudgetAmounts = decimal.Zero;
                        allocateOffBudgetAmounts = decimal.Zero;
                        if (model.AllocateBudgetTypeFlag == 1)
                            allocateBudgetAmounts = projectItem.NewAllocateAmounts == null ? decimal.Zero : projectItem.NewAllocateAmounts.Value;
                        else
                            allocateOffBudgetAmounts = projectItem.NewAllocateAmounts == null ? decimal.Zero : projectItem.NewAllocateAmounts.Value;

                        var exprAllocateExpenses = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                            && e.YR.Equals(exprReq.YR)
                            && e.DEP_ID.Equals(exprAllocateMas.DEP_ID)
                            && e.PLAN_ID == expensesItem.PLAN_ID
                            && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                            && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                            && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                            && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                            && e.EXPENSES_ID == expensesItem.EXPENSES_ID
                            && e.PROJECT_ID == projectItem.PROJECT_ID).FirstOrDefault();
                        if (null == exprAllocateExpenses)
                        {
                            exprAllocateExpenses = new T_BUDGET_ALLOCATE_EXPENSE()
                            {
                                ALLOCATE_ID = exprAllocateMas.ALLOCATE_ID,
                                DEP_ID = exprAllocateMas.DEP_ID,
                                SUB_DEP_ID = exprAllocateMas.SUB_DEP_ID,
                                YR = exprAllocateMas.YR,
                                PLAN_ID = expensesItem.PLAN_ID,
                                PRODUCE_ID = expensesItem.PRODUCE_ID,
                                ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                                BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                                EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                                EXPENSES_ID = expensesItem.EXPENSES_ID,
                                PROJECT_ID = projectItem.PROJECT_ID,
                                ALLOCATE_COUNT = 1,
                                ALLOCATE_BUDGET_AMOUNT = decimal.Zero,
                                ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero,
                                NET_BUDGET_AMOUNT = decimal.Zero,
                                USE_BUDGET_AMOUNT = decimal.Zero,
                                REMAIN_BUDGET_AMOUNT = decimal.Zero,
                                CREATED_DATETIME = DateTime.Now,
                                USER_ID = userAuthorizeProfile.EmpId,
                                ACTIVE = 1
                            };
                            db.T_BUDGET_ALLOCATE_EXPENSEs.InsertOnSubmit(exprAllocateExpenses);
                        }
                        else
                        {
                            exprAllocateExpenses.UPDATED_DATETIME = DateTime.Now;
                            exprAllocateExpenses.UPDATED_ID = userAuthorizeProfile.EmpId;
                            exprAllocateExpenses.ALLOCATE_COUNT += 1;
                        }

                        exprAllocateExpenses.ALLOCATE_BUDGET_AMOUNT += allocateBudgetAmounts;
                        exprAllocateExpenses.ALLOCATE_OFF_BUDGET_AMOUNT += allocateOffBudgetAmounts;
                        exprAllocateExpenses.NET_BUDGET_AMOUNT = exprAllocateExpenses.ALLOCATE_BUDGET_AMOUNT + exprAllocateExpenses.ALLOCATE_OFF_BUDGET_AMOUNT;
                        exprAllocateExpenses.REMAIN_BUDGET_AMOUNT = exprAllocateExpenses.NET_BUDGET_AMOUNT - exprAllocateExpenses.USE_BUDGET_AMOUNT;

                        // บันทึกประวัติการจัดสรรงบประมาณ
                        //short allocateExpensesCount = 0; // ครั้งที่จัดสรรเงิน งปม. ให้กับรายการ คชจ.
                        //allocateExpensesCount = Convert.ToInt16(db.T_BUDGET_ALLOCATE_EXPENSES_HISTORies.Where(e => e.DEP_ID.Equals(exprAllocateMas.DEP_ID)
                        //        && e.YR.Equals(exprAllocateMas.YR)
                        //        && e.PLAN_ID == expensesItem.PLAN_ID
                        //        && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                        //        && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                        //        && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                        //        && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                        //        && e.EXPENSES_ID == expensesItem.EXPENSES_ID).Count());
                        ////&& db.T_BUDGET_REQUEST_MASTERs.Any(mas => mas.REQ_ID.Equals(e.REQ_ID) && mas.REQ_TYPE.Equals(2))).Count();
                        //allocateExpensesCount++;
                        //exprAllocateExpenses.ALLOCATE_COUNT = allocateExpensesCount;

                        db.T_BUDGET_ALLOCATE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_ALLOCATE_EXPENSES_HISTORY()
                        {
                            DEP_ID = exprAllocateMas.DEP_ID,
                            SUB_DEP_ID = exprAllocateMas.SUB_DEP_ID,
                            YR = exprAllocateMas.YR,
                            MN = Convert.ToInt16(DateTime.Now.Month),
                            REQ_ID = exprReq.REQ_ID,
                            PLAN_ID = expensesItem.PLAN_ID,
                            PRODUCE_ID = expensesItem.PRODUCE_ID,
                            ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                            BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                            EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                            EXPENSES_ID = expensesItem.EXPENSES_ID,
                            PROJECT_ID = projectItem.PROJECT_ID,
                            ALLOCATE_TYPE = model.AllocateBudgetTypeFlag, // จัดสรรจาก เงินงบประมาณ หรือ เงินนอก งปม.
                            REMARK_TEXT = expensesItem.RemarkText,
                            BUDGET_TYPE = exprReq.BUDGET_TYPE, // คำขอขอจาก เงินงบประมาณ หรือ เงินนอก งปม.
                            ALLOCATE_BUDGET_AMOUNT = allocateBudgetAmounts.CompareTo(decimal.Zero) == 1 ? allocateBudgetAmounts : allocateOffBudgetAmounts,
                            ALLOCATE_DATETIME = DateTime.Now,
                            ALLOCATE_ID = userAuthorizeProfile.EmpId,
                            ACTIVE = 1,
                            SEQ_NO = exprAllocateExpenses.ALLOCATE_COUNT.Value // จัดสรรเพิ่มเติมครั้งที่
                        });


                        // ปรับปรุงยอดคงเหลือ และ เพื่อยอดใช้จ่ายงบประมาณ ของรายการค่าใช้จ่ายในคลัง
                        var exprBudgetExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                            && e.YR.Equals(exprReq.YR)
                            && e.PLAN_ID == expensesItem.PLAN_ID
                            && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                            && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                            && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                            && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                            && e.EXPENSES_ID == expensesItem.EXPENSES_ID).FirstOrDefault();
                        if (null != exprBudgetExpenses)
                        {
                            exprBudgetExpenses.USE_BUDGET_AMOUNT += allocateBudgetAmounts;
                            exprBudgetExpenses.REMAIN_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_BUDGET_AMOUNT - exprBudgetExpenses.USE_BUDGET_AMOUNT;

                            exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT += allocateOffBudgetAmounts;
                            exprBudgetExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetExpenses.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetExpenses.USE_OFF_BUDGET_AMOUNT;

                            exprBudgetExpenses.LATEST_USE_BUDGET = DateTime.Now;
                        }

                        // ปรังปรุงยอดคงเหลือ และ เพิ่มยอดใช้จ่ายงบประมาณ ของโครงการ
                        var exprBudgetExpensesProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.ACTIVE.Equals(1)
                            && e.YR.Equals(exprReq.YR)
                            && e.PLAN_ID == expensesItem.PLAN_ID
                            && e.PRODUCE_ID == expensesItem.PRODUCE_ID
                            && e.ACTIVITY_ID == expensesItem.ACTIVITY_ID
                            && e.BUDGET_TYPE_ID == expensesItem.BUDGET_TYPE_ID
                            && e.EXPENSES_GROUP_ID == expensesItem.EXPENSES_GROUP_ID
                            && e.EXPENSES_ID == expensesItem.EXPENSES_ID
                            && e.PROJECT_ID == projectItem.PROJECT_ID).FirstOrDefault();
                        if (null != exprBudgetExpensesProject)
                        {
                            exprBudgetExpensesProject.USE_BUDGET_AMOUNT += allocateBudgetAmounts;
                            exprBudgetExpensesProject.REMAIN_BUDGET_AMOUNT = exprBudgetExpensesProject.ACTUAL_BUDGET_AMOUNT - exprBudgetExpensesProject.USE_BUDGET_AMOUNT;

                            exprBudgetExpensesProject.USE_OFF_BUDGET_AMOUNT += allocateOffBudgetAmounts;
                            exprBudgetExpensesProject.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetExpensesProject.USE_OFF_BUDGET_AMOUNT;
                        }
                    });
                });

                db.SubmitChanges();
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetAllocateFormMapper
        {
            /// <summary>
            /// เลขที่คำขอ งปม. ที่ต้องการจัดสรรงบประมาณ
            /// </summary>
            public string ReqId { get; set; }

            /// <summary>
            /// ประเภทงบประมาณที่จะใช้เงินงบประมาณในการจัดสรร
            /// 1 = เงินงบประมาณ, 2 = เงินนอก งปม.
            /// </summary>
            public short AllocateBudgetTypeFlag { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายตามคำของบประมาณ ที่จัดสรรให้กับหน่วยงาน
            /// </summary>
            [Required(ErrorMessage = "ไม่มีรายการค่าใช้จ่ายที่จะจัดสรร")]
            public List<BudgetAllocateExpensesProperty> Expenses { get; set; }
        }

        public class BudgetAllocateExpensesProperty
        {
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
            /// งบรายจ่าย (งบลงทุน งบอุดหนุน ฯลฯ)
            /// </summary>
            public int BUDGET_TYPE_ID { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_GROUP_ID { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่าย
            /// </summary>
            public int EXPENSES_ID { get; set; }

            /// <summary>
            /// รายชื่อโครงการ ของรายการค่าใช้จ่ายที่จัดสรรลงให้กับหน่วยงาน
            /// </summary>
            public List<BudgetAllocateProjectProperty> Projects { get; set; }

            /// <summary>
            /// จำนวนเงินงบประมาณที่จัดสรร ของรายการค่าใช้จ่าย
            /// </summary>
            public decimal? NewAllocateAmounts { get; set; }


            [MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }

        public class BudgetAllocateProjectProperty
        {
            /// <summary>
            /// เลขที่โครงการ ของหน่วยงาน ที่จัดสรรงบประมาณไปแล้ว
            /// </summary>
            public int? SEQ_ID { get; set; }

            public int? PLAN_ID { get; set; }

            public int? PRODUCE_ID { get; set; }

            public int? ACTIVITY_ID { get; set; }

            public int BUDGET_TYPE_ID { get; set; }

            public int EXPENSES_GROUP_ID { get; set; }

            public int EXPENSES_ID { get; set; }

            public int PROJECT_ID { get; set; }

            public string PROJECT_NAME { get; set; }

            /// <summary>
            /// จำนวนเงินที่จัดสรรเพิ่ม
            /// </summary>
            public decimal? NewAllocateAmounts { get; set; }

            public string RemarkText { get; set; }
        }
    }
}