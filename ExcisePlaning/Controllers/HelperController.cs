using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3,General")]
    public class HelperController : Controller
    {
        /// <summary>
        /// โหลด Modal form สำหรับค้นหาข้อมูลพนักงานโดยให้ระบุหน่วยงาน (หรือไม่ระบุ) เข้ามาเพื่อกรองข้อมูลเฉพาะหน่วยงานนั้นๆ
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("depId:string")]
        public ActionResult GetHelperPersonSearchForm(string depId)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.DepartmentId = depId; // อาจเป็น Null

                // ประเภทพนักงาน
                ViewBag.PersonTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PersonnelTypeShortFieldProperty()
                    {
                        PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                        PERSON_TYPE_NAME = e.ITEM_TEXT
                    }).ToList();


                // ตำแหน่งงาน
                ViewBag.Positions = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PositionShortFieldProperty()
                    {
                        POSITION_ID = e.POSITION_ID,
                        POSITION_NAME = e.POSITION_NAME
                    }).ToList();
            }
            return View("Helper_Person_Information_Form");
        }

        [HttpPost, Route("depId:int?, positionId:short?, personTypeId:short?, personCode:string, personName:string, periodId:short?, pageIndex:int, pageSize:int")]
        public ActionResult RetrievePersonnel(int? depId, short? positionId, short? personTypeId, string personCode, string personName, short? periodId, int pageIndex, int pageSize)
        {
            // จัดเตรียมข้อมูลเพื่อตอบกลับ
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };


            // ตรวจสอบสิทธิ์การเข้าถึงข้อมูลของบุคลากร โดยตรวจสอบจากหน่วยงาน
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            if (!userAuthorizeProfile.AccountType.Value.Equals(1))
            {
                if (depId == null)
                    return Json(pagging, JsonRequestBehavior.DenyGet);
                //if (depId != null && userAuthorizeProfile.AssignDepartmentIds.IndexOf(depId.Value) == -1)
                //    return Json(pagging, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_PERSONNEL_INFORMATIONs.Where(e => e.ACTIVE.Equals(1));

                // ค้นหาตามหน่วยงาน
                if (null != depId)
                    expr = expr.Where(e => e.DEP_ID.Equals(depId.Value));

                // ค้นหาตามตำแหน่งงาน
                if (null != positionId)
                    expr = expr.Where(e => e.POSITION_ID.Equals(positionId.Value));

                // ค้นหาตามประเภทบุคลากร
                if (null != personTypeId)
                    expr = expr.Where(e => e.PERSON_TYPE_ID.Equals(personTypeId.Value));

                // ค้นหาตามรหัสบุคลากร (บางส่วน)
                if (!string.IsNullOrEmpty(personCode))
                    expr = expr.Where(e => e.PERSON_CODE.Contains(personCode));

                // ค้นหาตามชื่อ-นามสกุล (บางส่วน)
                if (!string.IsNullOrEmpty(personName))
                    expr = expr.Where(e => e.FIRST_NAME.Contains(personName) || e.LAST_NAME.Contains(personName));

                var finalExpr = expr.Select(e => new
                {
                    e.PERSON_ID,
                    e.PERSON_CODE,
                    e.PREFIX_NAME,
                    e.FIRST_NAME,
                    e.LAST_NAME,
                    e.POSITION_ID,
                    e.POSITION_NAME,
                    e.PERSON_TYPE_ID,
                    e.PERSON_TYPE_NAME,
                    e.DEP_ID,
                    e.DEP_NAME,
                    e.SUB_DEP_ID,
                    e.SUB_DEP_NAME,
                    e.LEVEL_NAME // ระดับ C ของบุคลากร
                }).OrderBy(e => Convert.ToInt32(e.PERSON_CODE));

                pagging.totalRecords = finalExpr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));

                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = finalExpr.Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// Modal ค้นหาบุคลากร ที่สามารถเลือกได้มากกว่าคน
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetHelperPersonSearchMultiSelectForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // หน่วยงาน
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.SORT_INDEX)
                    .Select(e => new DepartmentShortFieldProperty()
                    {
                        DEP_ID = e.DEP_ID,
                        DEP_NAME = e.DEP_NAME
                    }).ToList();

                // ประเภทพนักงาน
                ViewBag.PersonTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PersonnelTypeShortFieldProperty()
                    {
                        PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                        PERSON_TYPE_NAME = e.ITEM_TEXT
                    }).ToList();

                // ตำแหน่งงาน
                ViewBag.Positions = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PositionShortFieldProperty()
                    {
                        POSITION_ID = e.POSITION_ID,
                        POSITION_NAME = e.POSITION_NAME
                    }).ToList();
            }
            return View("Helper_Person_Multiple_Select_Search_Form");
        }


        [HttpGet]
        public ActionResult GetHelperMenuSelectMultiple()
        {
            return View("Helper_Menu_Multiple_Select_Search_Form");
        }
        [HttpPost, Route("menuName:string, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveMenu(string menuName, int pageSize, int pageIndex)
        {
            // จัดเตรียมข้อมูลเพื่อตอบกลับ
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_MENUs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(menuName))
                    expr = expr.Where(e => e.MENU_NAME.Contains(menuName));
                var finalExpr = expr.OrderBy(e => e.MENU_ID);

                pagging.totalRecords = finalExpr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = finalExpr.Select(e => new
                {
                    MENU_ID = e.MENU_ID,
                    MENU_NAME = e.MENU_NAME
                }).Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// แสดงแบบฟอร์มการค้นหา รายการค่าใช้จ่าย/โครงการของกรมสรรพสามิต
        /// สำหรับให้เลือกรายการ (เลือกได้มากกว่า 1)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetHelperBudgetExpensesSelectMultiple()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
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
        /// ค้นหารายการค่าใช้จ่าย/โครงการ ของกรมสรรพสามิต ในปีงบประมาณ
        /// สำหรับนำไปแสดงผลให้เลือกรายการ
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="projectName"></param>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveBudgetExpenses(int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, string projectName, int? budgetType, int pageIndex, int pageSize)
        {
            // จัดเตรียมข้อมูลเพื่อตอบกลับ
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_SUMMARY_OVERALL_BUDGETs.Where(e => e.YR.Equals(fiscalYear));

                if (null != planId)
                    expr = expr.Where(e => e.PLAN_ID.Equals(planId));
                if (null != produceId)
                    expr = expr.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (null != activityId)
                    expr = expr.Where(e => e.ACTIVITY_ID.Equals(activityId));
                if (null != budgetTypeId)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (null != expensesGroupId)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != expensesId)
                    expr = expr.Where(e => e.EXPENSES_ID.Equals(expensesId));

                // project_for_type = โครงการใช้กับ เงินงบประมาณ หรือ เงินนอกงบประมาณ
                if (null != budgetType)
                    expr = expr.Where(e => e.PROJECT_ID == null || (e.PROJECT_ID != null && e.PROJECT_FOR_TYPE.Equals(budgetType)));
                if (!string.IsNullOrEmpty(projectName))
                    expr = expr.Where(e => e.PROJECT_NAME.Contains(projectName));

                var finalExpr = expr.OrderBy(e => e.PLAN_ORDER_SEQ)
                        .ThenBy(e => e.PRODUCE_ORDER_SEQ)
                        .ThenBy(e => e.ACTIVITY_ORDER_SEQ)
                        .ThenBy(e => e.BUDGET_TYPE_ORDER_SEQ)
                        .ThenBy(e => e.EXPENSES_GROUP_ORDER_SEQ)
                        .ThenBy(e => e.EXPENSES_ORDER_SEQ)
                        .ThenBy(e => e.PROJECT_ID);

                pagging.totalRecords = finalExpr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = finalExpr.Select(e => new
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
                    e.BUDGET_TYPE_ID,
                    e.BUDGET_TYPE_NAME,
                    e.BUDGET_TYPE_ORDER_SEQ,
                    e.BUDGET_TYPE_SHARED_BUDGET,
                    e.BUDGET_TYPE_GOVERNMENT_REFER_CODE,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_GROUP_ORDER_SEQ,
                    e.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG, // 1 = จัดสรรงบประมาณ ตามหมวดค่าใช้จ่าย
                    e.EXPENSES_GROUP_GOVERNMENT_REFER_CODE,
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_ORDER_SEQ,
                    e.GLCODEs,
                    e.PROJECT_ID,
                    e.PROJECT_NAME,
                    e.PROJECT_FOR_TYPE,

                    // งบประมาณ, เงินประจำงวด, ใช้จ่าย, คงเหลือสุทธิ (รายการค่าใช้จ่าย)
                    e.BUDGET_AMOUNT,
                    e.OFF_BUDGET_AMOUNT,
                    e.ACTUAL_BUDGET_AMOUNT,
                    e.ACTUAL_OFF_BUDGET_AMOUNT,
                    e.USE_BUDGET_AMOUNT,
                    e.USE_OFF_BUDGET_AMOUNT,
                    e.REMAIN_BUDGET_AMOUNT,
                    e.REMAIN_OFF_BUDGET_AMOUNT,

                    // เงินงบประมาณ, เงินประจำงวด, ใช้จ่าย, คงเหลือสุทธิ (โครงการ)
                    e.PRO_BUDGET_AMOUNT,
                    e.PRO_OFF_BUDGET_AMOUNT,
                    e.PRO_ACTUAL_BUDGET_AMOUNT,
                    e.PRO_ACTUAL_OFF_BUDGET_AMOUNT,
                    e.PRO_USE_BUDGET_AMOUNT,
                    e.PRO_USE_OFF_BUDGET_AMOUNT,
                    e.PRO_REMAIN_BUDGET_AMOUNT,
                    e.PRO_REMAIN_OFF_BUDGET_AMOUNT
                }).Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpGet]
        public ActionResult GetHelperSearchBudgetTemplate()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.PLAN_ID).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();

                // ผลผลิต
                ViewBag.Produces = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.PRODUCE_ID).Select(e => new ProduceShortFieldProperty()
                {
                    PRODUCE_ID = e.PRODUCE_ID,
                    PRODUCE_NAME = e.PRODUCE_NAME
                }).ToList();

                // กิจกรรม
                ViewBag.Activities = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ACTIVITY_ID).Select(e => new ActivityShortFieldProperty()
                {
                    ACTIVITY_ID = e.ACTIVITY_ID,
                    ACTIVITY_NAME = e.ACTIVITY_NAME
                }).ToList();

                // งบรายจ่าย (งบดำเนินงาน งบลงทุน)
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.BUDGET_TYPE_ID).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();
            };
            return View();
        }

        [HttpGet]
        public ActionResult GetHelperSearchBudgetSelectMultipleTemplate()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
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
            };
            return View();
        }


        /// <summary>
        /// โหลดฟอร์มแสดงผลประวัติการปรับปรุงรายการเบิกจ่าย
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetHelperReserveWithdrawalHistoryForm()
        {
            return View();
        }
        [HttpPost]
        public ActionResult RetrieveReserveWithdrawalHistory(string reserveId, int seqNo)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.V_GET_BUDGET_RESERVE_WITHDRAWAL_HISTORY_INFORMATIONs.Where(e => e.RESERVE_ID.Equals(reserveId) && e.WITHDRAWAL_SEQ_NO.Equals(seqNo))
                        .Select(e => new
                        {
                            e.RESERVE_ID,
                            e.WITHDRAWAL_SEQ_NO,
                            e.WITHDRAWAL_CODE,
                            e.REFER_DOC_CODE,
                            e.SEQ_NO,
                            e.CURR_WITHDRAWAL_AMOUNT,
                            e.ADJUSTMENT_AMOUNT,
                            e.CASHBACK_AMOUNT,
                            e.BALANCE_AMOUNT,
                            e.REMARK_TEXT,
                            e.TRAN_TYPE,
                            e.CREATED_DATETIME,
                            e.CREATED_NAME
                        }).OrderByDescending(e => e.SEQ_NO).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ค้นหาข้อมูล Template ที่สร้างไว้เพื่อใช้ในการส่งคำขอเงิน งบประมาณ
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId">งบรายจ่าย (งบดำเนินงาน งบอุดหนุน งบลงทุน ฯลฯ)</param>
        /// <param name="expensesGroupId">หมวดค่าใช้จ่าย</param>
        /// <param name="budgetTypeFlag">1 = งบประมาณ, 2 = เงินนอก งปม.</param>
        /// <param name="budgetTargetFlag">1 = คำงบต้นปี, 2 = คำของบเพิ่มเติม</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost, Route("templateName:string, planId:int?, produceId:int?, activityId:int?, budgetTypeId:int?, expensesGroupId:int?, budgetTypeFlag:string, budgetTargetFlag:string, pageIndex:int, pageSize:int")]
        public ActionResult RetrieveBudgetTemplate(string templateName, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, string budgetTypeFlag, string budgetTargetFlag, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            // จะต้องกรองข้อมูลตามสิทธิ์ที่ระบุไว้ใน Template
            // ดูได้เฉพาะบางหน่วยงาน หรือ แสดงเฉพาะบางปีงบประมาณ
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                int currYear = AppUtils.GetCurrYear();

                var expr = db.V_GET_BUDGET_TEMPLATE_INFORMATIONs.Where(e =>
                    // ใช้ได้ทุกปีงบประมาณ หรือ เฉพาะปีงบประมาณที่ระบุสิทธิ์ไว้
                    (e.SHARED_YR_TEMPLATE.Equals(1) || (e.SHARED_YR_TEMPLATE.Equals(2) && db.T_BUDGET_REQUEST_TEMPLATE_YR_AUTHORIZEs.Any(authorize => authorize.TEMPLATE_ID.Equals(e.TEMPLATE_ID) && authorize.YR.Equals(currYear))))
                    &&
                    // ใช้ได้ทุกหน่วยงาน หรือ เฉพาะบางหน่วยงาน
                    (e.SHARED_DEP_TEMPLATE.Equals(1) || (e.SHARED_DEP_TEMPLATE.Equals(2) && db.T_BUDGET_REQUEST_TEMPLATE_DEPARTMENT_AUTHORIZEs.Any(authorize => authorize.TEMPLATE_ID.Equals(e.TEMPLATE_ID) && authorize.DEP_ID.Equals(userAuthorizeProfile.DepId))))
                    );

                // แสดงเฉพาะเงิน งปม.
                if ("1".Equals(budgetTypeFlag))
                    expr = expr.Where(e => e.FOR_BUDGET);
                // แสดงเฉพาะเงินนอก งปม.
                if ("2".Equals(budgetTypeFlag))
                    expr = expr.Where(e => e.FOR_OFF_BUDGET);

                // แสดงเฉพาะใช้กับงบประมาณต้นปี
                if ("1".Equals(budgetTargetFlag))
                    expr = expr.Where(e => e.FOR_SOURCE_BUDGET_BEGIN);
                // แสดงเฉพาะใช้กับคำขอเงินงบประมาณเพิ่ม
                if ("2".Equals(budgetTargetFlag))
                    expr = expr.Where(e => e.FOR_SOURCE_BUDGET_ADJUNCT);

                // ชื่อ Template
                if (!string.IsNullOrEmpty(templateName))
                    expr = expr.Where(e => e.TEMPLATE_NAME.Contains(templateName));

                // แผนงาน
                if (planId.HasValue)
                    expr = expr.Where(e => e.PLAN_ID.Equals(planId.Value));
                // ผลผลิต
                if (produceId.HasValue)
                    expr = expr.Where(e => e.PRODUCE_ID.Equals(produceId.Value));
                // กิจกรรม
                if (activityId.HasValue)
                    expr = expr.Where(e => e.ACTIVITY_ID.Equals(activityId.Value));
                // งบรายจ่าย
                if (budgetTypeId.HasValue)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId.Value));
                // หมวด คชจ.
                if (expensesGroupId.HasValue)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId.Value));


                var finalExpr = expr
                    .OrderBy(e => e.PLAN_ORDER_SEQ)
                    .ThenBy(e => e.PRODUCE_ORDER_SEQ)
                    .ThenBy(e => e.ACTIVITY_ORDER_SEQ)
                    .ThenBy(e => e.BUDGET_TYPE_ORDER_SEQ)
                    .ThenBy(e => e.EXPENSES_GROUP_ORDER_SEQ)
                    .Select(e => new
                    {
                        e.TEMPLATE_ID,
                        e.TEMPLATE_NAME,
                        e.CREATE_TYPE,
                        e.PLAN_ID,
                        e.PLAN_NAME,
                        e.PRODUCE_ID,
                        e.PRODUCE_NAME,
                        e.ACTIVITY_ID,
                        e.ACTIVITY_NAME,
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.FOR_BUDGET,
                        e.FOR_OFF_BUDGET,
                        e.FOR_SOURCE_BUDGET_ADJUNCT,
                        e.FOR_SOURCE_BUDGET_BEGIN,
                        e.SHARED_DEP_TEMPLATE,
                        e.SHARED_YR_TEMPLATE,
                        e.CREATED_DATETIME
                    }).OrderBy(e => e.TEMPLATE_NAME);

                var offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = finalExpr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = finalExpr.Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ค้นหารายละเอียดของ Template ได้แก่ รายการค่าใช้จ่าย สิทธิ์การเข้าถึง (หน่วยงาน/ปี งปม.)
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveBudgetTemplateDetail(int? templateId)
        {
            if (!templateId.HasValue)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var res = new Dictionary<string, object>(3) {
                    { "Expenses", null },
                    { "DepartmentAuthorize", null },
                    { "YearAuthorize", null }
                };

                // รายการค่าใช้จ่ายที่กำหนดไว้ใน Template
                res["Expenses"] = db.T_BUDGET_REQUEST_TEMPLATE_EXPENSEs.Where(e => e.TEMPLATE_ID.Equals(templateId.Value)).Select(e => e.EXPENSES_ID).ToList();

                // สิทธิ์การเข้าถึง Template (หน่วยงาน)
                res["DepartmentAuthorize"] = db.T_BUDGET_REQUEST_TEMPLATE_DEPARTMENT_AUTHORIZEs.Where(e => e.TEMPLATE_ID.Equals(templateId)).Select(e => new
                {
                    e.DEP_ID,
                    DEP_NAME = db.T_DEPARTMENTs.Where(dep => dep.DEP_ID.Equals(e.DEP_ID)).Select(dep => dep.DEP_NAME).FirstOrDefault()
                }).ToList();

                // สิทธิ์การเข้าถึง Template (ปี งปม.)
                res["YearAuthorize"] = db.T_BUDGET_REQUEST_TEMPLATE_YR_AUTHORIZEs.Where(e => e.TEMPLATE_ID.Equals(templateId)).Select(e => e.YR).ToList();

                return Json(res, JsonRequestBehavior.DenyGet);
            };
        }


        /// <summary>
        /// ค้นหาข้อมูลหน่วยงานภายใต้ เขตพื้นที่
        /// </summary>
        /// <param name="areaId"></param>
        /// <returns></returns>
        [HttpPost, AllowAnonymous]
        public ActionResult GetDepartmentBy(int? areaId)
        {
            if (null == areaId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && e.AREA_ID.Equals(areaId.Value));
                return Json(expr.OrderBy(e => e.SORT_INDEX).Select(e => new DepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME
                }).ToList(), JsonRequestBehavior.DenyGet);
            };
        }


        /// <summary>
        /// ค้นหาผลผลิต ที่อยู่ภายใต้ แผนงาน
        /// </summary>
        /// <param name="planId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetProduceBy(int? planId)
        {
            if (null == planId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1) && e.PLAN_ID.Equals(planId));
                return Json(expr.OrderBy(e => e.ORDER_SEQ).Select(e => new ProduceShortFieldProperty()
                {
                    PRODUCE_ID = e.PRODUCE_ID,
                    PRODUCE_NAME = e.PRODUCE_NAME
                }).ToList(), JsonRequestBehavior.DenyGet);
            };
        }

        /// <summary>
        /// ค้นหากิจกรรมต่างๆ ที่อยู่ภายใต้ผลผลิต
        /// </summary>
        /// <param name="produceId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetActivityBy(int? produceId)
        {
            if (null == produceId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1) && e.PRODUCE_ID.Equals(produceId));
                return Json(expr.OrderBy(e => e.ORDER_SEQ).Select(e => new ActivityShortFieldProperty()
                {
                    ACTIVITY_ID = e.ACTIVITY_ID,
                    ACTIVITY_NAME = e.ACTIVITY_NAME
                }).ToList(), JsonRequestBehavior.DenyGet);
            };
        }

        /// <summary>
        /// ค้นหาข้อมูลหน่วยงานย่อยภายใต้ หน่วยงานหลัก
        /// </summary>
        /// <param name="depId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetSubDepartmentBy(int? depId)
        {
            if (null == depId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_SUB_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && e.DEP_ID.Equals(depId));
                return Json(expr.OrderBy(e => e.SUB_DEP_ID).Select(e => new SubDepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    SUB_DEP_ID = e.SUB_DEP_ID,
                    SUB_DEP_NAME = e.SUB_DEP_NAME
                }).ToList(), JsonRequestBehavior.DenyGet);
            };
        }


        /// <summary>
        /// ค้นหาข้อมูลหน่วยงานภายใต้ เขตพื้นที่ ซึ่งเป็นหน่วยงานที่ตนเองสังกัด หรือได้รับมอบหมายให้รับผิดชอบ
        /// </summary>
        /// <param name="areaId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetDepartmentAuthorizeBy(int? areaId)
        {
            if (null == areaId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && e.AREA_ID.Equals(areaId.Value));

                // หน่วยงานทั่วไป หรือ หน่วยงานที่มอบหมายให้ดูแลหน่วยงานอื่นๆ
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                if (userAuthorizeProfile.DepAuthorize.Equals(2))
                {
                    List<int> authorizeDepIds = userAuthorizeProfile.AssignDepartmentIds;
                    authorizeDepIds.Add(userAuthorizeProfile.DepId);
                    expr = expr.Where(e => authorizeDepIds.Contains(e.DEP_ID));
                }

                return Json(expr.OrderBy(e => e.SORT_INDEX).Select(e => new DepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME
                }).ToList(), JsonRequestBehavior.DenyGet);
            };
        }


        /// <summary>
        /// assetTypeId ดูได้จากตาราง T_ASSET_TYPE
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetAssetByAssetType(int? assetTypeId)
        {
            if (null == assetTypeId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ASSETs.Where(e => e.ACTIVE.Equals(1) && e.ASSET_TYPE.Equals(assetTypeId)).OrderBy(e => e.ORDER_SEQ).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME,
                    e.ASSET_OTHER_FLAG,
                    ASSET_TYPE_NAME = db.T_ASSET_TYPEs.Where(s => s.ASSET_TYPE_ID.Equals(e.ASSET_TYPE)).Select(s => s.ASSET_TYPE_NAME).FirstOrDefault()
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            };
        }


        // <summary>
        /// ค้นหารายละเอียดค่าใช้จ่าย โดยอ้างอิงจาก Template หรือ คำขอ งปม.
        /// </summary>
        /// <param name="referenceId">TemplateId (กรณีเป็น Templete), REQ_ID (กรณีเป็นคำของบประมาณ)</param>
        /// <param name="type">TEMPLATE = ให้ค้นหาจาก Template, REQUEST_BUDGET = ให้ค้นหาและอ้างอิงรายการจาก คำขอเงิน งปม.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetExpensesDetailBy(string reqId, List<int> templateIds)
        {
            if ((templateIds == null || templateIds.Count == 0) && string.IsNullOrEmpty(reqId))
                return Json(null, JsonRequestBehavior.DenyGet);

            // กรณีไม่ผ่านค่า TemplateIds มาให้ Init เป็น list empty
            // มีการนำค่าไปใช้ ในกรณีที่ดึงข้อมูลรายการค่าใช้จ่าย จากคำขอ
            templateIds = null == templateIds ? new List<int>() : templateIds;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // รายการค่าใช้จ่ายจาก Template
                var exprExpensesTemplate = (from exprExpenses in db.V_GET_EXPENSES_INFORMATIONs
                                            join exprTemplate in db.V_GET_BUDGET_TEMPLATE_EXPENSES_INFORMATIONs.Where(e => templateIds.Contains(e.TEMPLATE_ID))
                                            on exprExpenses.EXPENSES_ID equals exprTemplate.EXPENSES_ID
                                            select new
                                            {
                                                exprTemplate.TEMPLATE_ID,
                                                exprTemplate.TEMPLATE_NAME,
                                                exprTemplate.PLAN_ID,
                                                exprTemplate.PLAN_NAME,
                                                exprTemplate.PLAN_ORDER_SEQ,

                                                exprTemplate.PRODUCE_ID,
                                                exprTemplate.PRODUCE_NAME,
                                                exprTemplate.PRODUCE_ORDER_SEQ,

                                                exprTemplate.ACTIVITY_ID,
                                                exprTemplate.ACTIVITY_NAME,
                                                exprTemplate.ACTIVITY_ORDER_SEQ,

                                                exprTemplate.BUDGET_TYPE_ID,
                                                exprTemplate.BUDGET_TYPE_NAME,
                                                exprTemplate.BUDGET_TYPE_ORDER_SEQ,

                                                exprTemplate.EXPENSES_GROUP_ID,
                                                exprTemplate.EXPENSES_GROUP_NAME,
                                                exprTemplate.EXPENSES_GROUP_ORDER_SEQ,

                                                exprExpenses.EXPENSES_ID,
                                                exprExpenses.EXPENSES_NAME,
                                                exprTemplate.EXPENSES_ORDER_SEQ,

                                                exprExpenses.FORM_TEMPLATE_NAME
                                            }).GroupBy(e => new
                                            {
                                                e.PLAN_ID,
                                                e.PRODUCE_ID,
                                                e.ACTIVITY_ID,
                                                e.BUDGET_TYPE_ID,
                                                e.EXPENSES_GROUP_ID,
                                                e.EXPENSES_ID
                                            })
                                // เผื่อกรณีที่มีการกำหนด รายการค่าใช้จ่าย ซ้ำกับ Template อื่นให้เลือกมา Template ใด Template หนึ่ง
                                .Select(e => new
                                {
                                    GroupBy = e.Key,
                                    items = e
                                }).OrderBy(e => e.GroupBy.BUDGET_TYPE_ID)
                                .ThenBy(e => e.GroupBy.EXPENSES_GROUP_ID)
                                .ThenBy(e => e.GroupBy.EXPENSES_ID).ToList();

                var exprExpensesFromTemplate = exprExpensesTemplate.Select(e => new
                {
                    e.items.First().TEMPLATE_ID,
                    e.items.First().TEMPLATE_NAME,

                    e.items.First().PLAN_ID,
                    e.items.First().PLAN_NAME,
                    e.items.First().PLAN_ORDER_SEQ,

                    e.items.First().PRODUCE_ID,
                    e.items.First().PRODUCE_NAME,
                    e.items.First().PRODUCE_ORDER_SEQ,

                    e.items.First().ACTIVITY_ID,
                    e.items.First().ACTIVITY_NAME,
                    e.items.First().ACTIVITY_ORDER_SEQ,

                    e.items.First().BUDGET_TYPE_ID,
                    e.items.First().BUDGET_TYPE_NAME,
                    e.items.First().BUDGET_TYPE_ORDER_SEQ,

                    e.items.First().EXPENSES_GROUP_ID,
                    e.items.First().EXPENSES_GROUP_NAME,
                    e.items.First().EXPENSES_GROUP_ORDER_SEQ,

                    e.items.First().EXPENSES_ID,
                    e.items.First().EXPENSES_NAME,
                    e.items.First().EXPENSES_ORDER_SEQ,

                    e.items.First().FORM_TEMPLATE_NAME,
                    EXPENSES_DESCRIBEs = "",
                    TOTAL_REQUEST_BUDGET = decimal.Zero
                }).ToList();

                // ค้นหาจาก คำขอที่ทำรายการไปแล้ว
                if (!string.IsNullOrEmpty(reqId))
                {
                    var exprRequest = (from expenses in db.V_GET_EXPENSES_INFORMATIONs
                                       join expensesReq in db.V_GET_BUDGET_REQUEST_DETAIL_INFORMATIONs.Where(e => e.REQ_ID.Equals(reqId))
                                       on expenses.EXPENSES_ID equals expensesReq.EXPENSES_ID
                                       select new
                                       {
                                           expensesReq.TEMPLATE_ID,
                                           expensesReq.TEMPLATE_NAME,
                                           expensesReq.PLAN_ID,
                                           expensesReq.PLAN_NAME,
                                           expensesReq.PLAN_ORDER_SEQ,

                                           expensesReq.PRODUCE_ID,
                                           expensesReq.PRODUCE_NAME,
                                           expensesReq.PRODUCE_ORDER_SEQ,

                                           expensesReq.ACTIVITY_ID,
                                           expensesReq.ACTIVITY_NAME,
                                           expensesReq.ACTIVITY_ORDER_SEQ,

                                           expensesReq.BUDGET_TYPE_ID,
                                           expensesReq.BUDGET_TYPE_NAME,
                                           expensesReq.BUDGET_TYPE_ORDER_SEQ,

                                           expenses.EXPENSES_GROUP_ID,
                                           expenses.EXPENSES_GROUP_NAME,
                                           expenses.EXPENSES_GROUP_ORDER_SEQ,

                                           expenses.EXPENSES_ID,
                                           expenses.EXPENSES_NAME,
                                           EXPENSES_ORDER_SEQ = expenses.ORDER_SEQ,

                                           expenses.FORM_TEMPLATE_NAME,
                                           // ค้นหารายละเอียดรายการ คชจ. ที่ผู้ใช้งานระบุไว้
                                           expensesReq.EXPENSES_XML_DESCRIBE,
                                           // ยอดใช้จ่ายที่ต้องการ ส่งคำขอ งปม.
                                           expensesReq.TOTAL_REQUEST_BUDGET
                                       }).AsEnumerable().Select(e => new
                                       {
                                           e.TEMPLATE_ID,
                                           e.TEMPLATE_NAME,
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

                                           e.EXPENSES_ID,
                                           e.EXPENSES_NAME,
                                           e.EXPENSES_ORDER_SEQ,

                                           e.FORM_TEMPLATE_NAME,
                                           // แปลง คชจ. จาก XML เป็น Json string
                                           EXPENSES_DESCRIBEs = AppUtils.LinqXmlToJson(e.EXPENSES_XML_DESCRIBE),
                                           // ยอดใช้จ่ายที่ต้องการ ส่งคำขอ งปม.
                                           e.TOTAL_REQUEST_BUDGET
                                       }).ToList();
                    if (templateIds.Count > 0) // แสดงข้อมูลตาม Template ที่เลือก
                        exprRequest = exprRequest.Where(e => templateIds.Contains(e.TEMPLATE_ID)).ToList();

                    exprExpensesFromTemplate = exprExpensesFromTemplate.Where(e => !exprRequest.Any(req => req.TEMPLATE_ID == e.TEMPLATE_ID
                        && req.EXPENSES_ID == e.EXPENSES_ID)).ToList();
                    exprRequest.AddRange(exprExpensesFromTemplate);
                    return Json(exprRequest
                            .GroupBy(e => new
                            {
                                e.PLAN_ID,
                                e.PLAN_ORDER_SEQ,
                                e.PLAN_NAME,
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
                                e.EXPENSES_GROUP_ORDER_SEQ
                            }).OrderBy(e => e.Key.PLAN_ORDER_SEQ)
                            .ThenBy(e => e.Key.PRODUCE_ORDER_SEQ)
                            .ThenBy(e => e.Key.ACTIVITY_ORDER_SEQ)
                            .ThenBy(e => e.Key.BUDGET_TYPE_ORDER_SEQ)
                            .ThenBy(e => e.Key.EXPENSES_GROUP_ORDER_SEQ)
                            .Select(e => new
                            {
                                GroupBy = e.Key,
                                Expenses = e.OrderBy(x => x.EXPENSES_ORDER_SEQ).ToList()
                            }).ToList(), JsonRequestBehavior.DenyGet);
                }

                return Json(exprExpensesFromTemplate.OrderBy(e => e.PLAN_ORDER_SEQ)
                            .ThenBy(e => e.PRODUCE_ORDER_SEQ)
                            .ThenBy(e => e.ACTIVITY_ORDER_SEQ)
                            .ThenBy(e => e.BUDGET_TYPE_ORDER_SEQ)
                            .ThenBy(e => e.EXPENSES_GROUP_ORDER_SEQ)
                            .ThenBy(e => e.EXPENSES_ORDER_SEQ).GroupBy(e => new
                            {
                                e.PLAN_ID,
                                e.PLAN_NAME,
                                e.PRODUCE_ID,
                                e.PRODUCE_NAME,
                                e.ACTIVITY_ID,
                                e.ACTIVITY_NAME,
                                e.BUDGET_TYPE_ID,
                                e.BUDGET_TYPE_NAME,
                                e.EXPENSES_GROUP_ID,
                                e.EXPENSES_GROUP_NAME
                            }).Select(e => new
                            {
                                GroupBy = e.Key,
                                Expenses = e.ToList()
                            }).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ค้นหารายละเอียดค่าใช้จ่าย โดยอ้างอิงจาก Template หรือ คำขอ งปม.
        /// </summary>
        /// <param name="referenceId">TemplateId (กรณีเป็น Templete), REQ_ID (กรณีเป็นคำของบประมาณ)</param>
        /// <param name="type">TEMPLATE = ให้ค้นหาจาก Template, REQUEST_BUDGET = ให้ค้นหาและอ้างอิงรายการจาก คำขอเงิน งปม.</param>
        /// <returns></returns>
        //[HttpPost]
        //public ActionResult GetExpensesDetailBy(string referenceId, string type)
        //{
        //    if (string.IsNullOrEmpty(referenceId) || string.IsNullOrEmpty(type))
        //        return Json(null, JsonRequestBehavior.DenyGet);

        //    using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
        //    {
        //        if ("TEMPLATE".Equals(type))
        //        {
        //            var expr = db.V_GET_EXPENSES_INFORMATIONs.Where(e => db.T_BUDGET_REQUEST_TEMPLATE_EXPENSEs.Any(expenses => expenses.TEMPLATE_ID.Equals(referenceId) && expenses.EXPENSES_ID.Equals(e.EXPENSES_ID)))
        //                .Select(e => new
        //                {
        //                    e.EXPENSES_ID,
        //                    e.EXPENSES_NAME,
        //                    e.EXPENSES_GROUP_ID,
        //                    e.EXPENSES_GROUP_NAME,
        //                    e.FORM_TEMPLATE_NAME,
        //                    EXPENSES_DESCRIBEs = "",
        //                    TOTAL_REQUEST_BUDGET = 0,
        //                }).ToList();
        //            return Json(expr, JsonRequestBehavior.DenyGet);
        //        }
        //        else if ("REQUEST_BUDGET".Equals(type))
        //        {
        //            // รายการค่าใช้จ่ายที่อยู่ในคำขอ งบประมาณ
        //            var expr = (from expenses in db.V_GET_EXPENSES_INFORMATIONs
        //                        join expensesReq in db.T_BUDGET_REQUEST_DETAILs.Where(e => e.REQ_ID.Equals(referenceId))
        //                        on expenses.EXPENSES_ID equals expensesReq.EXPENSES_ID
        //                        select new
        //                        {
        //                            expenses.EXPENSES_ID,
        //                            expenses.EXPENSES_NAME,
        //                            expenses.EXPENSES_GROUP_ID,
        //                            expenses.EXPENSES_GROUP_NAME,
        //                            expenses.FORM_TEMPLATE_NAME,
        //                            // ค้นหารายละเอียดรายการ คชจ. ที่ผู้ใช้งานระบุไว้
        //                            expensesReq.EXPENSES_XML_DESCRIBE,
        //                            // ยอดใช้จ่ายที่ต้องการ ส่งคำขอ งปม.
        //                            expensesReq.TOTAL_REQUEST_BUDGET
        //                        }).AsEnumerable().Select(e => new
        //                        {
        //                            e.EXPENSES_ID,
        //                            e.EXPENSES_NAME,
        //                            e.EXPENSES_GROUP_ID,
        //                            e.EXPENSES_GROUP_NAME,
        //                            e.FORM_TEMPLATE_NAME,
        //                            // แปลง คชจ. จาก XML เป็น Object
        //                            EXPENSES_DESCRIBEs = AppUtils.LinqXmlToJson(e.EXPENSES_XML_DESCRIBE),
        //                            // ยอดใช้จ่ายที่ต้องการ ส่งคำขอ งปม.
        //                            e.TOTAL_REQUEST_BUDGET
        //                        }).ToList();


        //            // เผื่อกรณีมีการแก้ไข template ต้นทางและมีการเพิ่มรายการ คชจ.
        //            // ให้นำมาแสดงผลในรายการคำขอ งปม. ด้วย
        //            // ยกเว้น  คำของบประมาณนั้น ถูกยกเลิก หรือ ไม่จัดสรร/จัดสรร แล้ว
        //            var requestExpr = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.REQ_ID.Equals(referenceId) && e.ACTIVE.Equals(1)).Select(e => new { e.TEMPLATE_ID, e.PROCESS_STATUS }).FirstOrDefault();
        //            if (null != requestExpr && requestExpr.PROCESS_STATUS.Equals(0))
        //            {
        //                List<int> requestExpensesIds = expr.Select(e => e.EXPENSES_ID).ToList();

        //                var newExpensesExpr = db.V_GET_EXPENSES_INFORMATIONs.Where(e => db.T_BUDGET_REQUEST_TEMPLATE_EXPENSEs.Any(expenses => expenses.EXPENSES_ID.Equals(e.EXPENSES_ID) && expenses.TEMPLATE_ID.Equals(requestExpr.TEMPLATE_ID)));
        //                newExpensesExpr = newExpensesExpr.Where(e => !requestExpensesIds.Contains(e.EXPENSES_ID));
        //                var finalNewExpenses = newExpensesExpr.AsEnumerable().Select(e => new
        //                {
        //                    e.EXPENSES_ID,
        //                    e.EXPENSES_NAME,
        //                    e.EXPENSES_GROUP_ID,
        //                    e.EXPENSES_GROUP_NAME,
        //                    e.FORM_TEMPLATE_NAME,
        //                    EXPENSES_DESCRIBEs = "",
        //                    TOTAL_REQUEST_BUDGET = decimal.Zero,
        //                }).ToList();
        //                expr.AddRange(finalNewExpenses);
        //            }

        //            return Json(expr, JsonRequestBehavior.DenyGet);
        //        }
        //    }

        //    return Json(null, JsonRequestBehavior.DenyGet);
        //}


        [HttpGet]
        public ActionResult GetHelperDepartmentSearchMultiSelectForm()
        {
            return View();
        }
        [HttpPost, Route("depName:string, pageIndex:int, pageSize:int")]
        public ActionResult RetrieveDepartment(string depName, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalPages = 0,
                totalRecords = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(depName))
                    expr = expr.Where(e => e.DEP_NAME.Contains(depName));

                var finalExpr = expr.Select(e => new { e.DEP_ID, e.DEP_NAME, e.DEP_SHORT_NAME, e.DEP_AUTHORIZE });
                var offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = finalExpr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = finalExpr.Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpGet]
        public ActionResult GetHelperExpensesSearchMultiSelectForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new BudgetTypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();
            }

            return View();
        }
        [HttpGet]
        public ActionResult GetHelperExpensesSearchOneSelectForm()
        {
            return View();
        }
        [HttpPost]
        public ActionResult RetrieveExpenses(int? budgetTypeId, int? expensesGroupId, string expensesName, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalPages = 0,
                totalRecords = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_EXPENSES_ITEMs.AsQueryable();
                if (!string.IsNullOrEmpty(expensesName))
                    expr = expr.Where(e => e.EXPENSES_NAME.Contains(expensesName));
                if (null != expensesGroupId)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (null != budgetTypeId)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));


                var finalExpr = expr
                    .OrderBy(e => e.BUDGET_TYPE_ORDER_SEQ)
                    .ThenBy(e => e.EXPENSES_GROUP_ORDER_SEQ)
                    .ThenBy(e => e.EXPENSES_ORDER_SEQ)
                    .Select(e => new
                    {
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_MASTER_NAME,
                        e.EXPENSES_ID,
                        e.EXPENSES_NAME,
                        e.CAN_ADD_PROJECT,
                        e.CREATED_DATETIME,
                        e.FORM_TEMPLATE_NAME
                    });
                var offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = finalExpr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = finalExpr.Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ค้นหารายละเอียดของ ค่าใช้จ่ายจากรหัส
        /// </summary>
        /// <param name="exepensesId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveExpensesById(int? expensesId)
        {
            if (null == expensesId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_EXPENSES_ITEMs.Where(e => e.EXPENSES_ID.Equals(expensesId))
                    .Select(e => new
                    {
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_MASTER_NAME,
                        e.EXPENSES_ID,
                        e.EXPENSES_NAME,
                        e.CAN_ADD_PROJECT,
                        e.CREATED_DATETIME,
                        e.FORM_TEMPLATE_NAME
                    }).FirstOrDefault();
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// แบบฟอร์มการค้นหา กันเงินงบประมาณ
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetHelperReserveBudgetSearchSelectOneForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var appSettings = AppSettingProperty.ParseXml();
                var areaIdsCanReserveBudget = appSettings.GetAreaIdsCanReserveBudgetToList();
                // หน่วยงานภายในกรมสรรพสามิต ที่จะกันเงิน
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && areaIdsCanReserveBudget.Contains(e.AREA_ID.Value)).Select(e => new DepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME
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
                // งบรายจ่าย
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();
            }

            return View();
        }
        /// <summary>
        /// แบบฟอร์มการค้นหา กันเงินงบประมาณ
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetHelperReserveBudgetSearchSelectMultipleForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var appSettings = AppSettingProperty.ParseXml();
                var areaIdsCanReserveBudget = appSettings.GetAreaIdsCanReserveBudgetToList();
                // หน่วยงานภายในกรมสรรพสามิต ที่จะกันเงิน
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && areaIdsCanReserveBudget.Contains(e.AREA_ID.Value)).Select(e => new DepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME
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
                // งบรายจ่าย
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();
            }

            return View();
        }
        [HttpPost]
        public ActionResult RetrieveReserveBudget(int? fiscalYear, int? subDepId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, short? reserveType, short? budgetType, string reserveId, short? active, short? isRemain, int pageIndex, int pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                PaggingResultMapper pagging = new PaggingResultMapper()
                {
                    totalRecords = 0,
                    totalPages = 0,
                    rows = null
                };

                var expr = db.V_GET_BUDGET_RESERVE_INFORMATIONs.AsQueryable();
                if (fiscalYear != null)
                    expr = expr.Where(e => e.YR.Equals(fiscalYear));
                if (subDepId != null)
                    expr = expr.Where(e => e.DEP_ID == subDepId);
                if (planId != null)
                    expr = expr.Where(e => e.PLAN_ID == planId);
                if (produceId != null)
                    expr = expr.Where(e => e.PRODUCE_ID == produceId);
                if (activityId != null)
                    expr = expr.Where(e => e.ACTIVITY_ID == activityId);
                if (budgetTypeId != null)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (expensesGroupId != null)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (expensesId != null)
                    expr = expr.Where(e => e.EXPENSES_ID.Equals(expensesId));
                if (reserveType != null)
                    expr = expr.Where(e => e.RESERVE_TYPE.Equals(reserveType));
                if (budgetType != null)
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                if (!string.IsNullOrEmpty(reserveId))
                    expr = expr.Where(e => e.RESERVE_ID.Equals(reserveId));
                if (null != active)
                    expr = expr.Where(e => e.ACTIVE.Equals(active));
                if (null != isRemain && isRemain.Value == 1)
                    expr = expr.Where(e => e.REMAIN_AMOUNT > 0);

                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = expr.OrderBy(e => e.DEP_ORDER_SEQ).OrderBy(e => e.CREATED_DATETIME).Skip(offset).Take(pageSize).Select(e => new
                {
                    e.RESERVE_ID,
                    e.DEP_NAME,
                    e.YR,
                    e.RESERVE_DATE,
                    e.RESERVE_BUDGET_AMOUNT,
                    e.BUDGET_TYPE,
                    e.USE_AMOUNT,
                    e.REMAIN_AMOUNT,
                    e.RESERVE_NAME,
                    e.CREATED_DATETIME,
                    e.REMARK_TEXT,
                    e.CASHBACK_AMOUNT,
                    // ข้อมูลการเบิกจ่าย
                    e.LATEST_WITHDRAWAL_DATETIME,
                    e.LATEST_WITHDRAWAL_NAME,
                    e.ACTIVE
                }).ToList();

                return Json(pagging, JsonRequestBehavior.DenyGet);
            }
        }



        /// <summary>
        /// แบบฟอร์มการค้นหา ข้อมูลเบิกจ่าย
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetHelperWithdrawalReserveBudgetSearchSelectOneForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var appSettings = AppSettingProperty.ParseXml();
                var areaIdsCanReserveBudget = appSettings.GetAreaIdsCanReserveBudgetToList();
                // หน่วยงานภายในกรมสรรพสามิต ที่จะกันเงิน
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && areaIdsCanReserveBudget.Contains(e.AREA_ID.Value)).Select(e => new DepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME
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
                // งบรายจ่าย
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();
            }

            return View();
        }
        [HttpPost]
        public ActionResult RetrieveWithdrawalReserveBudget(int? fiscalYear, int? subDepId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, short? reserveType, short? budgetType, short? active, int pageIndex, int pageSize)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                PaggingResultMapper pagging = new PaggingResultMapper()
                {
                    totalRecords = 0,
                    totalPages = 0,
                    rows = null
                };

                var expr = db.V_GET_BUDGET_RESERVE_WITHDRAWAL_INFORMATIONs.AsQueryable();
                if (fiscalYear != null)
                    expr = expr.Where(e => e.YR.Equals(fiscalYear));
                if (subDepId != null)
                    expr = expr.Where(e => e.DEP_ID == subDepId);
                if (planId != null)
                    expr = expr.Where(e => e.PLAN_ID == planId);
                if (produceId != null)
                    expr = expr.Where(e => e.PRODUCE_ID == produceId);
                if (activityId != null)
                    expr = expr.Where(e => e.ACTIVITY_ID == activityId);
                if (budgetTypeId != null)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (expensesGroupId != null)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                if (expensesId != null)
                    expr = expr.Where(e => e.EXPENSES_ID.Equals(expensesId));
                if (reserveType != null)
                    expr = expr.Where(e => e.RESERVE_TYPE.Equals(reserveType));
                if (budgetType != null)
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                if (null != active)
                    expr = expr.Where(e => e.ACTIVE.Equals(active));

                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = expr.OrderBy(e => e.DEP_ORDER_SEQ).OrderBy(e => e.CREATED_DATETIME).Skip(offset).Take(pageSize).Select(e => new
                {
                    e.RESERVE_ID,
                    e.SEQ_NO, // ลำดับการเบิกจ่าย
                    e.DEP_NAME,
                    e.YR,

                    e.WITHDRAWAL_AMOUNT,
                    e.WITHDRAWAL_CODE, // เลขที่เบิกจ่าย ที่รับค่าจากผู้ใช้งาน 10 หลัก
                    e.WITHDRAWAL_DATE,
                    e.CREATED_NAME,
                    e.CREATED_DATETIME,
                    e.REMARK_TEXT,
                    // ข้อมูลการปรับปรุงบัญชี
                    e.WITHDRAWAL_REFER_RESERVE_ID,
                    e.WITHDRAWAL_REFER_SEQ_NO,

                    e.ACTIVE
                }).ToList();

                return Json(pagging, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลหมวดค่าใช้จ่ายทั้งหมด
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllExpensesGroup()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_EXPENSES_GROUP_INFORMATIONs.Where(e => e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                    e.EXPENSES_MASTER_ID,
                    e.EXPENSES_MASTER_NAME,
                    e.BUDGET_TYPE_NAME,
                }).ToList();
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลรายการ คชจ. ทั้งหมดที่ยังใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllExpenses()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_EXPENSES_INFORMATIONs.Where(e => e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.EXPENSES_ID,
                    e.EXPENSES_NAME,
                    e.EXPENSES_GROUP_ID,
                    e.EXPENSES_GROUP_NAME,
                }).OrderBy(e => e.EXPENSES_GROUP_ID).ThenBy(e => e.EXPENSES_NAME).ToList();
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ค้นหาข้อมูลหมวด คชจ. โดยใช้ งบรายจ่าย (งบดำเนินงาน งบอุดหนุน, ...) เป็นเงื่อนไขในการค้นหา
        /// เนื่องจากหมวด คชจ. จะอยู่ภายใต้แต่ละงบรายจ่าย
        /// </summary>
        /// <param name="budgetTypeId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveExpensesGroupByBudgetType(int? budgetTypeId)
        {
            if (null == budgetTypeId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                return Json(db.V_GET_EXPENSES_GROUP_INFORMATIONs.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId.Value))
                    .OrderBy(e => e.BUDGET_TYPE_ORDER_SEQ)
                    .ThenBy(e => e.ORDER_SEQ)
                    .Select(e => new
                    {
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_MASTER_ID,
                        e.EXPENSES_MASTER_NAME
                    }).ToList(), JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public ActionResult GetOrganizationByExpenses(int? expensesId)
        {
            if (null == expensesId)
                return Json(null, JsonRequestBehavior.DenyGet);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = (from org in db.T_ORGANIZATIONs.Where(e => e.ACTIVE.Equals(1))
                            join orgExpenses in db.T_ORGANIZATION_RELATION_EXPENSEs.Where(e => e.EXPENSES_ID.Equals(expensesId))
                            on org.ORG_ID equals orgExpenses.ORG_ID
                            orderby org.SORT_INDEX ascending
                            select new
                            {
                                org.ORG_ID,
                                org.ORG_NAME
                            }
                           ).ToList();
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ค้นหาข้อมูลรายการ คชจ. โดยใช้ หมวด คชจ. เป็นเงื่อนไขในการค้นหา
        /// เนื่องจากรายการ คชจ. จะอยู่ภายใต้ หมวด คชจ.
        /// </summary>
        /// <param name="expensesGroupId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveExpensesByExpensesGroup(int? expensesGroupId)
        {
            if (null == expensesGroupId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                return Json(db.V_GET_EXPENSES_INFORMATIONs.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId.Value))
                    .OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new
                    {
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_ID,
                        e.EXPENSES_NAME
                    }).ToList(), JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ค้นหาข้อมูลบุคลากร โดยใช้รหัสพนักงานเป็นคีย์ ในการค้นหา
        /// </summary>
        /// <param name="personCode"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrievePersonnelByPersonCode(string personCode)
        {
            if (string.IsNullOrEmpty(personCode))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_PERSONNEL_INFORMATIONs.Where(e => e.PERSON_CODE.Equals(personCode))
                        .Select(e => new
                        {
                            e.PERSON_CODE,
                            e.PERSON_ID,
                            e.PREFIX_NAME,
                            e.FIRST_NAME,
                            e.LAST_NAME,
                            e.POSITION_NAME,
                            e.PERSON_TYPE_NAME,
                            e.DEP_ID,
                            e.DEP_NAME,
                            e.SUB_DEP_ID,
                            e.SUB_DEP_NAME,
                            e.SALARY, // เงินเดือน ณ ปัจจุบัน
                            e.LEVEL_NAME // ระดับ C ของบุคลากร
                        }).FirstOrDefault();
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลประเภทยานะาหนะทั้งหมดที่ ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllVehicleType()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_VEHICLE_TYPEs.Where(e => e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.VEHICLE_TYPE_ID,
                    e.VEHICLE_TYPE_NAME,
                    e.COMPENSATION_PRICE
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }
        /// <summary>
        /// ค้นหาข้อมูลประเภทยานพาหนะ โดยใช้ Id เป็นเงื่อนไขในการค้นหา 
        /// ซึ่งจะได้ข้อมูล Id,Name,Compensation Price (เงินทดแทนของแต่ละประเภทยานพาหนะกรณีปฏิบัติงานนอกสถานที่)
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveVehicleTypeById(int? vehicleId)
        {
            if (null == vehicleId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_VEHICLE_TYPEs.Where(e => e.ACTIVE.Equals(1) && e.VEHICLE_TYPE_ID.Equals(vehicleId.Value)).Select(e => new
                {
                    e.VEHICLE_TYPE_ID,
                    e.VEHICLE_TYPE_NAME,
                    e.COMPENSATION_PRICE
                }).FirstOrDefault();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ค้นหาค่าตอบแทน ตามระดับของบุคลากร และ ประเภทค่าตอบแทน (ค่าเบี้ยเลี้ยง ค่าเช่าที่พัก เป็นต้น)
        /// เช่น ระดับชำนาญการพิเศษ ได้ค่าเบี้ยเลี้ยงเท่าไหร่ เป็นต้น
        /// </summary>
        /// <param name="personLevelId">ระดับบุคลากร</param>
        /// <param name="compensationTypeCode">ประเภทค่าตอบแทน เช่น ค่าเบี้ยเลี้ยง ค่าเช่าที่พัก เป็นต้น</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetPersonnelLevelCompensationRateBy(int? personLevelId, string compensationTypeCode)
        {
            if (personLevelId == null)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ระดับบุคลากร
                var expr = db.V_GET_PERSONNEL_LEVEL_COMPENSATION_RATE_INFOMATIONs.Where(e => e.LEVEL_ID.Equals(personLevelId.Value));

                // ค่าคงที่ ประเภทค่าตอบแทน (T_COMPENSATION_TYPE.COMPENSATION_TYPE_CODE)
                if (!string.IsNullOrEmpty(compensationTypeCode))
                    expr = expr.Where(e => e.COMPENSATION_TYPE_CODE.Equals(compensationTypeCode));

                var finalExpr = expr.Select(e => new
                {
                    e.LEVEL_ID,
                    e.PERSONNEL_LEVEL_NAME,
                    e.COMPENSATION_TYPE_ID,
                    e.COMPENSATION_TYPE_NAME,
                    e.COMPENSATION_TYPE_CODE,
                    e.RATE_AMOUNT,
                    e.RATE_TYPE
                }).ToList();

                return Json(finalExpr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลสิ่งก่อสร้างทั้งหมด ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllBuilding()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(1) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลสินทรัพที่เป็น โฆษณาและเผยแพร่ทั้งหมดที่มีอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllAdvertiseAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // AssetType = 2 โฆษณาและเผยแพร่
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(2) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลสินทรัพที่เป็น วัสดุสำนักงาน ที่ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllOfficialEquipmentAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // AssetType = 3 วัสดุสำนักงาน
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(3) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ดึงข้อมูลสินทรัพที่เป็น ครุภัณฑ์คอมพิวเตอร์ ที่ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllComputerEquipmentAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // AssetType = 6 ครุภัณฑ์คอมพิวเตอร์
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(6) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลสินทรัพที่เป็น อุปกรณ์ไฟฟ้าและวิทยุ ที่ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllElectricalAndRadioAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // AssetType = 4 อุปกรณ์ไฟฟ้าและวิทยุ
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(4) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลสินทรัพที่เป็น  ค่าวัสดุวิทยาศาสตร์หรือการแพทย์ ที่ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllScienceAndMedicalAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // AssetType = 5 วัสดุวิทยาศาสตร์หรือการแพทย์
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(5) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ดึงข้อมูลสินทรัพที่เป็น สินทรัพท์ประเภทอื่นๆ ที่ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllOtherAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // AssetType = 9 สินทรัพย์อื่นๆ
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(9) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ดึงข้อมูลสินทรัพที่เป็น วัสดุยานพาหนะและขนส่ง ที่ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllVehicleEquipmentAndTransportAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // AssetType = 7 วัสดุยานพาหนะและขนส่ง
                var expr = db.T_ASSETs.Where(e => e.ASSET_TYPE.Equals(7) && e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลหน่วยนับ ที่ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllUnit()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_UNITs.Where(e => e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.UNIT_ID,
                    e.UNIT_TEXT
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ดึงข้อมูลสิ่งทรัพสินทั้งหมด ใช้งานอยู่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveAllAsset()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ASSETs.Where(e => e.ACTIVE.Equals(1)).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME
                }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ค้นหาตำแหน่งงานที่สามารถใช้กับรายการค่าใช้จ่ายได้
        /// </summary>
        /// <param name="expensesId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrievePositionAuthorizeByExpensesId(int? expensesId)
        {
            if (null == expensesId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1));

                // ตำแหน่งงานที่ใช้ได้กับทุก รายการ คชจ.
                //var exprPositionAll = expr.Where(e => e.EXPENSES_FLAG.Equals(1)).Select(e => new
                //{
                //    e.POSITION_ID,
                //    e.POSITION_NAME,
                //    e.POSITION_CODE
                //}).ToList();

                // ตำแหน่งงานที่ระบุให้ใช้ได้เฉพาะ บางรายการ คชจ.
                var exprPositionAuthorize = expr.Where(e => e.EXPENSES_FLAG.Equals(2) && db.T_POSITION_AUTHORIZE_EXPENSEs.Where(ex => ex.EXPENSES_ID.Equals(expensesId) && ex.POSITION_ID.Equals(e.POSITION_ID)).Any())
                        .Select(e => new
                        {
                            e.POSITION_ID,
                            e.POSITION_NAME,
                            e.POSITION_CODE
                        }).ToList();

                return Json(exprPositionAuthorize, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ค้นหาระดับ C ที่สามารถใช้กับรายการค่าใช้จ่ายได้
        /// </summary>
        /// <param name="expensesId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrievePersonnelLeveAuthorizeByExpensesId(int? expensesId)
        {
            if (null == expensesId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1));

                //// ระดับ C ที่ใช้ได้กับทุก รายการ คชจ.
                //var exprPersonLevelAll = expr.Where(e => e.EXPENSES_FLAG.Equals(1)).Select(e => new
                //{
                //    e.LEVEL_ID,
                //    e.LEVEL_NAME
                //}).ToList();

                // ระดับ C ที่ระบุให้ใช้ได้เฉพาะ บางรายการ คชจ.
                var exprPersonLevelAuthorize = expr.Where(e => e.EXPENSES_FLAG.Equals(2) && db.T_PERSONNEL_LEVEL_AUTHORIZE_EXPENSEs.Where(ex => ex.EXPENSES_ID.Equals(expensesId) && ex.LEVEL_ID.Equals(e.LEVEL_ID)).Any())
                        .Select(e => new
                        {
                            e.LEVEL_ID,
                            e.LEVEL_NAME
                        }).ToList();

                return Json(exprPersonLevelAuthorize, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ข้อมูลค่าฝึกอบรมและสัมนา
        /// </summary>
        /// <param name="expensesId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetriveAllTraningAndSeminors()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.T_TRANING_AND_SEMINORs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SEQ_ID)
                    .Select(e => new
                    {
                        e.SEQ_ID,
                        e.ITEM_TEXT,
                        e.COMPENSATION_PRICE
                    }).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// แบบฟอร์มสรุปผลการจัดการเงินงบประมาณประจำปีของ กรมสรรพสามิต 
        /// เช่น จัดสรรงบประมาณให้หน่วยงานภูมิภาพ หรือ การกันเงินงบประมาณ เป็นต้น
        /// </summary>
        /// <returns></returns>
        public ActionResult GetModalSummaryBudgetCashFlow()
        {
            return View();
        }
        /// <summary>
        /// ข้อมูลสรุปผลการจัดการเงินงบประมาณในแต่ละปีของ กรมสรรพสามิต
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="retrieveType">BUDGET_ALLOCATE = จัดสรรงบประมาณให้หน่วยงานภูมิภาค, BUDGET_RESERVE = กันเงินงบประมาณ</param>
        /// <param name="budgetType">1 = เงินงบ, 2= เงินนอก, null = ไม่ระบุ</param>
        /// <returns></returns>
        public ActionResult RetrieveSummaryBudgetCashFlow(int fiscalYear, string retrieveType, int? budgetType, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, int? expensesId, int? projectId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "BudgetTypes", null }, { "Departments", null } };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ข้อมูลงบประมาณ
                var budgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new
                    {
                        e.BUDGET_TYPE_ID,
                        e.BUDGET_TYPE_NAME
                    }).ToList();
                res["BudgetTypes"] = budgetTypes;

                // สรุปภาพรวม การจัดสรรงบประมาณลงให้แต่ละหน่วยงานภูมิภาค
                if ("BUDGET_ALLOCATE".Equals(retrieveType))
                {
                    // จัดสรรงบประมาณให้หน่วยงานภูมิภาค - เป็นก้อนตามหมวดค่าใช้จ่าย
                    var exprAllocateGroup = db.T_BUDGET_ALLOCATE_EXPENSES_GROUP_HISTORies.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(fiscalYear));
                    var exprAllocateExpenses = db.T_BUDGET_ALLOCATE_EXPENSES_HISTORies.Where(e => e.ACTIVE.Equals(1) && e.YR.Equals(fiscalYear));
                    if (null != budgetType)
                    {
                        exprAllocateGroup = exprAllocateGroup.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                    }
                    if (null != planId)
                    {
                        exprAllocateGroup = exprAllocateGroup.Where(e => e.PLAN_ID.Equals(planId));
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.PLAN_ID.Equals(planId));
                    }
                    if (null != produceId)
                    {
                        exprAllocateGroup = exprAllocateGroup.Where(e => e.PRODUCE_ID.Equals(produceId));
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.PRODUCE_ID.Equals(produceId));
                    }
                    if (null != activityId)
                    {
                        exprAllocateGroup = exprAllocateGroup.Where(e => e.ACTIVITY_ID.Equals(activityId));
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.ACTIVITY_ID.Equals(activityId));
                    }
                    if (null != budgetTypeId)
                    {
                        exprAllocateGroup = exprAllocateGroup.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                    }
                    if (null != expensesGroupId)
                    {
                        exprAllocateGroup = exprAllocateGroup.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                    }
                    if (null != expensesId)
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.EXPENSES_ID.Equals(expensesId));
                    if (null != projectId)
                        exprAllocateExpenses = exprAllocateExpenses.Where(e => e.PROJECT_ID.Equals(projectId));



                    // จัดสรรงบประมาณให้หน่วยงานภูมิภาค - ตามรายการค่าใช้จ่ายหรือรายละเอียด
                    var finalExprAllocateExpenses = (from exprAllocateHis in exprAllocateExpenses
                                                     join exprDep in db.V_GET_DEPARTMENT_INFORMATIONs
                                                     on exprAllocateHis.DEP_ID equals exprDep.DEP_ID
                                                     select new
                                                     {
                                                         exprDep.AREA_ID,
                                                         exprDep.AREA_NAME,
                                                         exprDep.DEP_ID,
                                                         exprDep.DEP_NAME,
                                                         DEP_ORDER_SEQ = exprDep.SORT_INDEX,
                                                         exprAllocateHis.ALLOCATE_ID,
                                                         exprAllocateHis.ADJUSTMENT_TYPE, // 1 = จัดสรรงบให้หน่วยงาน, 2 = ดึงงบประมาณคืนจากหน่วยงาน
                                                         exprAllocateHis.ALLOCATE_TYPE, // 0 = จัดสรรโดยไม่มีคำขอ, 1=คำขอต้นปี, 2 = คำขอเพิ่มเติม
                                                         exprAllocateHis.PERIOD_CODE,
                                                         exprAllocateHis.BUDGET_TYPE, // 1 = เงินงบ, 2 = เงินนอกงบ
                                                         exprAllocateHis.BUDGET_TYPE_ID, // จัดสรรด้วยงบรายจ่ายอะไร (งบอุดหนุน งบดำเนินงาน งบบุคลากร ...)
                                                         exprAllocateHis.ALLOCATE_BUDGET_AMOUNT
                                                     }).GroupBy(e => new { e.AREA_ID, e.AREA_NAME, e.DEP_ID, e.DEP_NAME, e.DEP_ORDER_SEQ, e.BUDGET_TYPE_ID })
                                                     .Select(e => new
                                                     {
                                                         e.Key.AREA_ID,
                                                         e.Key.AREA_NAME,
                                                         e.Key.DEP_ID,
                                                         e.Key.DEP_NAME,
                                                         e.Key.DEP_ORDER_SEQ,
                                                         e.Key.BUDGET_TYPE_ID,
                                                         ALLOCATE_BUDGET_AMOUNT = e.Sum(x => x.ADJUSTMENT_TYPE.Equals(1) ? x.ALLOCATE_BUDGET_AMOUNT : decimal.Zero)
                                                                - e.Sum(x => x.ADJUSTMENT_TYPE.Equals(2) ? x.ALLOCATE_BUDGET_AMOUNT : decimal.Zero)
                                                     }).AsEnumerable();

                    // สรุปข้อมูลการจัดสรรงบประมาณเป็นก้อน
                    // ตาม เขตพื้นที่ หน่วยงาน งบรายจ่าย และจำนวนเงินที่ได้รับจัดสรร
                    var finalExprAllocateGroup = exprAllocateGroup.Select(e => new
                    {
                        e.AREA_ID,
                        e.DEP_ID,
                        e.BUDGET_TYPE,// 1 = เงินงบ, 2 = เงินนอกงบ
                        e.BUDGET_TYPE_ID,// จัดสรรด้วยงบรายจ่ายอะไร (งบอุดหนุน งบดำเนินงาน งบบุคลากร ...)
                        e.ADJUSTMENT_TYPE, // 1 = จัดสรรงบให้หน่วยงาน, 2 = ดึงงบประมาณคืนจากหน่วยงาน
                        e.ALLOCATE_BUDGET_AMOUNT
                    }).GroupBy(e => new
                    {
                        e.AREA_ID,
                        e.DEP_ID,
                        e.BUDGET_TYPE_ID
                    }).Select(e => new
                    {
                        e.Key.AREA_ID,
                        AREA_NAME = db.T_AREAs.Where(x => x.AREA_ID.Equals(e.Key.AREA_ID)).Select(x => x.AREA_NAME).FirstOrDefault(),
                        e.Key.DEP_ID,
                        DEP_NAME = db.T_DEPARTMENTs.Where(x => x.DEP_ID.Equals(e.Key.DEP_ID)).Select(x => x.DEP_NAME).FirstOrDefault(),
                        DEP_ORDER_SEQ = db.T_DEPARTMENTs.Where(x => x.DEP_ID.Equals(e.Key.DEP_ID)).Select(x => x.SORT_INDEX).FirstOrDefault(),
                        e.Key.BUDGET_TYPE_ID,
                        ALLOCATE_BUDGET_AMOUNT = e.Sum(x => x.ADJUSTMENT_TYPE.Equals(1) ? x.ALLOCATE_BUDGET_AMOUNT : decimal.Zero)
                            - e.Sum(x => x.ADJUSTMENT_TYPE.Equals(2) ? x.ALLOCATE_BUDGET_AMOUNT : decimal.Zero)
                    }).AsEnumerable();


                    var finalExpr = finalExprAllocateGroup.Concat(finalExprAllocateExpenses)
                            .GroupBy(e => new { e.AREA_ID, e.AREA_NAME, e.DEP_ID, e.DEP_NAME, e.DEP_ORDER_SEQ })
                            .Select(e => new
                            {
                                e.Key.AREA_ID,
                                e.Key.AREA_NAME,
                                e.Key.DEP_ID,
                                e.Key.DEP_NAME,
                                e.Key.DEP_ORDER_SEQ,
                                NET_BUDGET_AMOUNT = e.Sum(x => x.ALLOCATE_BUDGET_AMOUNT),
                                BudgetTypes = new Func<List<object>>(() =>
                                {
                                    List<object> ret = new List<object>();
                                    budgetTypes.ForEach(budgetItem =>
                                    {
                                        decimal totalAllocateBudgetAmounts = e.Sum(x => x.BUDGET_TYPE_ID.Equals(budgetItem.BUDGET_TYPE_ID) ? x.ALLOCATE_BUDGET_AMOUNT : decimal.Zero);
                                        ret.Add(new Dictionary<string, object>(3) {
                                            { "BUDGET_TYPE_ID", budgetItem.BUDGET_TYPE_ID },
                                            { "BUDGET_TYPE_NAME", budgetItem.BUDGET_TYPE_NAME },
                                            { "BUDGET_AMOUNT", totalAllocateBudgetAmounts }
                                        });
                                    });
                                    return ret;
                                })()
                            }).OrderBy(e => e.AREA_ID).ThenBy(e => e.DEP_ORDER_SEQ).ToList();

                    res["Departments"] = finalExpr;
                }
                else // สรุปภาพรวม การกันเงินงบประมาณ ของหน่วยงานภายใน
                {
                    var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.YR.Equals(fiscalYear));
                    if (null != budgetType)
                        exprReserve = exprReserve.Where(e => e.BUDGET_TYPE.Equals(budgetType));
                    if (null != planId)
                        exprReserve = exprReserve.Where(e => e.PLAN_ID.Equals(planId));
                    if (null != produceId)
                        exprReserve = exprReserve.Where(e => e.PRODUCE_ID.Equals(produceId));
                    if (null != activityId)
                        exprReserve = exprReserve.Where(e => e.ACTIVITY_ID.Equals(activityId));
                    if (null != budgetTypeId)
                        exprReserve = exprReserve.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                    if (null != expensesGroupId)
                        exprReserve = exprReserve.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId));
                    if (null != expensesId)
                        exprReserve = exprReserve.Where(e => e.EXPENSES_ID.Equals(expensesId));
                    if (null != projectId)
                        exprReserve = exprReserve.Where(e => e.PROJECT_ID.Equals(projectId));

                    var expr = from exprBudgetReserve in exprReserve
                               join exprDep in db.V_GET_DEPARTMENT_INFORMATIONs
                               on exprBudgetReserve.DEP_ID equals exprDep.DEP_ID
                               select new
                               {
                                   exprDep.AREA_ID,
                                   exprDep.AREA_NAME,
                                   exprDep.DEP_ID,
                                   exprDep.DEP_NAME,
                                   DEP_ORDER_SEQ = exprDep.SORT_INDEX,
                                   exprBudgetReserve.RESERVE_ID,
                                   exprBudgetReserve.BUDGET_TYPE_ID, // จัดสรรด้วยงบรายจ่ายอะไร (งบอุดหนุน งบดำเนินงาน งบบุคลากร ...)
                                   exprBudgetReserve.BUDGET_TYPE, // 1 = เงินงบ, 2 = เงินนอกงบ
                                   exprBudgetReserve.RESERVE_TYPE, // 1 = ผูกพัน, 2 = กันไว้เบิก
                                   exprBudgetReserve.RESERVE_BUDGET_AMOUNT
                               };

                    var finalExpr = expr.GroupBy(e => new { e.AREA_ID, e.AREA_NAME, e.DEP_ID, e.DEP_NAME, e.DEP_ORDER_SEQ })
                            .OrderBy(e => e.Key.DEP_ORDER_SEQ).AsEnumerable()
                            .Select(e => new
                            {
                                e.Key.AREA_ID,
                                e.Key.AREA_NAME,
                                e.Key.DEP_ID,
                                e.Key.DEP_NAME,
                                // งบที่หน่วยงานได้รับจัดสรรทั้งหมด
                                NET_BUDGET_AMOUNT = e.Sum(x => x.RESERVE_BUDGET_AMOUNT),
                                // งบที่หน่วยงานได้รับจัดสรร แยกตาม งบรายจ่าย (งบอุดหนุน งบลงทุน งบบุคลากร ...)
                                BudgetTypes = new Func<List<object>>(() =>
                                {
                                    List<object> ret = new List<object>();
                                    budgetTypes.ForEach(budgetItem =>
                                    {
                                        ret.Add(new Dictionary<string, object>(3) {
                                            {"BUDGET_TYPE_ID", budgetItem.BUDGET_TYPE_ID},
                                            {"BUDGET_TYPE_NAME", budgetItem.BUDGET_TYPE_NAME},
                                            {"BUDGET_AMOUNT", e.Sum(x => x.BUDGET_TYPE_ID.Equals(budgetItem.BUDGET_TYPE_ID) ? x.RESERVE_BUDGET_AMOUNT : decimal.Zero) }
                                        });
                                    });
                                    return ret;
                                })()
                            }).ToList();

                    res["Departments"] = finalExpr;
                }


                res["BudgetTypes"] = budgetTypes;
            };
            return Json(res, JsonRequestBehavior.DenyGet);
        }

    }
}