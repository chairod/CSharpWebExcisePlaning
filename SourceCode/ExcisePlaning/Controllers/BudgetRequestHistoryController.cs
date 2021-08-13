using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// ค้นหาประวัติคำขอเงินงบประมาณ
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3,General")]
    public class BudgetRequestHistoryController : Controller
    {
        // GET: BudgetRequestHistory
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_REQUEST_HISTORY_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_REQUEST_HISTORY_MENU;
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
        /// <param name="areaId"></param>
        /// <param name="depId"></param>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="budgetTypeFlag">1 = เงิน งปม. , 2 = เงินนอก งปม.</param>
        /// <param name="requestTypeFlag">1 = คำขอต้นปี, 2 = คำขอเพิ่มเติม</param>
        /// <param name="status">0 = รอพิจารณา, 1 = จัดสรร, -1 = ไม่จัดสรร</param>
        /// <param name="requestId"></param>
        /// <param name="refCode">เลขที่หนังสืออ้างอิง (เฉพาะกรณีคำขอเพิ่มเติม)</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(int? areaId, int? depId, int fiscalYear, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, short? budgetTypeFlag, short? requestTypeFlag, short? status, string requestId, string refCode, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var expr = db.V_GET_BUDGET_REQUEST_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear));

                // ตรวจสอบการเข้าถึงข้อมูลของหน่วยงาน
                // 1. กรณีไม่เลือกหน่วยงาน ให้ใช้ข้อมูล Profile กรองข้อมูลตามสิทธิ์
                // 2. กรณีเลือกหน่วยงาน ให้ดูสิทธิ์การเข้าถึงข้อมูลของหน่วยงาน ที่เลือก
                var depFilterAuthorize = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, depId);
                if (depFilterAuthorize.Authorize.Equals(2))
                    expr = expr.Where(e => depFilterAuthorize.AssignDepartmentIds.Contains(e.DEP_ID));
                if (null != areaId && depId == null)
                    expr = expr.Where(e => e.AREA_ID.Equals(areaId));

                // หน่วยงานกลาง เข้าถึงข้อมูลของทุกหน่วยงาน
                // หน่วยงานทั่วไป หรือ หน่วยงานหลักของภูมิภาค เข้าถึงข้อมูลได้เฉพาะหน่วยงานตนเองหรือที่ได้รับมอบหมาย
                //if (userAuthorizeProfile.DepAuthorize.Equals(2))
                //{
                //    var authorizeDepIds = userAuthorizeProfile.AssignDepartmentIds;
                //    authorizeDepIds.Add(userAuthorizeProfile.DepId);
                //    if (null != depId && authorizeDepIds.IndexOf(depId.Value) > -1)
                //    {
                //        authorizeDepIds.Clear();
                //        authorizeDepIds.Add(depId.Value);
                //    }
                //    expr = expr.Where(e => authorizeDepIds.Contains(e.DEP_ID));
                //}
                //else
                //{
                //    // เขตพื้นที่
                //    if (null != areaId)
                //        expr = expr.Where(e => e.AREA_ID.Equals(areaId));
                //    if (null != depId)
                //        expr = expr.Where(e => e.DEP_ID.Equals(depId));
                //}

                // แผนงาน
                if (null != planId)
                    expr = expr.Where(e => db.T_BUDGET_REQUEST_DETAILs.Any(reqDetail => reqDetail.ACTIVE.Equals(1) && reqDetail.REQ_ID.Equals(e.REQ_ID) && reqDetail.PLAN_ID.Equals(planId)));

                // ผลผลิต
                if (null != produceId)
                    expr = expr.Where(e => db.T_BUDGET_REQUEST_DETAILs.Any(reqDetail => reqDetail.ACTIVE.Equals(1) && reqDetail.REQ_ID.Equals(e.REQ_ID) && reqDetail.PRODUCE_ID.Equals(produceId)));

                // กิจกรรม
                if (null != activityId)
                    expr = expr.Where(e => db.T_BUDGET_REQUEST_DETAILs.Any(reqDetail => reqDetail.ACTIVE.Equals(1) && reqDetail.REQ_ID.Equals(e.REQ_ID) && reqDetail.ACTIVITY_ID.Equals(activityId)));

                // งบรายจ่าย
                if (null != budgetTypeId)
                    expr = expr.Where(e => db.T_BUDGET_REQUEST_DETAILs.Any(reqDetail => reqDetail.ACTIVE.Equals(1) && reqDetail.REQ_ID.Equals(e.REQ_ID) && reqDetail.BUDGET_TYPE_ID.Equals(budgetTypeId)));

                // หมวดค่าใช้จ่าย
                if (null != expensesGroupId)
                    expr = expr.Where(e => db.T_BUDGET_REQUEST_DETAILs.Any(reqDetail => reqDetail.ACTIVE.Equals(1) && reqDetail.REQ_ID.Equals(e.REQ_ID) && reqDetail.EXPENSES_GROUP_ID.Equals(expensesGroupId)));

                // เงินงบประมาณ, เงินนอก งปม.
                if (null != budgetTypeFlag)
                    expr = expr.Where(e => e.BUDGET_TYPE.Equals(budgetTypeFlag));

                // ประเภทคำขอ (1 = คำต้นปี, 2 = คำขอเพิ่มเติม)
                if (null != requestTypeFlag)
                    expr = expr.Where(e => e.REQ_TYPE.Equals(requestTypeFlag));

                // สถานะรายการ (รอจัดสรร จัดสรร ไม่จัดสรร)
                if (null != status)
                    expr = expr.Where(e => e.PROCESS_STATUS.Equals(status));

                // เลขที่รายการ
                if (!string.IsNullOrEmpty(requestId))
                    expr = expr.Where(e => e.REQ_ID.Equals(requestId));

                // เลขที่หนังสืออ้างอิง (กรณีคำขอเพิ่มเติม)
                if (!string.IsNullOrEmpty(refCode))
                    expr = expr.Where(e => e.REFER_REQ_ID.Equals(refCode));

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.OrderBy(e => e.AREA_ID).ThenBy(e => e.DEP_SORT_INDEX)
                    .Skip(offset).Take(pageSize)
                    .Select(e => new
                    {
                        e.AREA_ID,
                        e.AREA_NAME,
                        e.DEP_ID,
                        e.DEP_NAME,
                        e.REQ_ID,
                        e.REFER_REQ_ID, // เลขที่หนังสืออ้างอิง (กรณีคำขอเพิ่มเติม)
                        e.YR,

                        e.REQ_TYPE, // 1 = งปม. ต้นปี, 2 = ของบ เพิ่มเติม
                        e.BUDGET_TYPE, // 1 = เงิน งปม., 2 = เงินนอก งปม.
                        e.TOTAL_REQUEST_BUDGET, // จำนวนสุทธิที่ขอ งปม.

                        e.NET_ALLOCATE_BUDGET_AMOUNT, // เงินงบประมาณสุทธิ ที่จัดสรร
                        e.NET_ALLOCATE_OFF_BUDGET_AMOUNT, // เงินนอกงบประมาณสุทธิ ที่จัดสรร
                        e.NET_CASHBACK_BUDGET_AMOUNT, // เงินงบประมาณสุทธิ ที่ดึงคืน
                        e.NET_CASHBACK_OFF_BUDGET_AMOUNT, // เงินนอกงบประมาณสุทธิ ที่ดึงคืน

                        e.CREATED_DATETIME,
                        e.CREATED_NAME,
                        e.REMARK_TEXT,
                        e.PROCESS_STATUS, // 1 = จัดสรรสมบูรณ์แล้ว (เงินงบ และ เงินนอกงบ)
                        e.BUDGET_ALLOCATE_FLAG, // สถานะการจัดสรรเงินงบประมาณ ของคำขอ (true/false)
                        e.OFF_BUDGET_ALLOCATE_FLAG, // สถานะการจัดสรรเงินนอกงบประมาณ ของคำขอ (true/false)
                        e.SIGNOFF_FLAG,
                        e.REQ_COUNT // ครั้งที่ขอเพิ่มเติม คำขอเพิ่มเติมระบบจะนับจำนวนครั้งที่ขอเพิ่มเติม
                    }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ยกเลิกคำขอเงินงบประมาณ
        /// </summary>
        /// <param name="reqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitReject(string reqId)
        {
            var controller = DependencyResolver.Current.GetService<BudgetRequestController>();
            controller.ControllerContext = new ControllerContext(Request.RequestContext, controller);
            return controller.SubmitReject(reqId);
        }


        /// <summary>
        /// โหลดหน้า Template View สำหรับแสดงผลรายการค่าใช้จ่าย ที่ส่งคำขอเงิน งปม.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetExpensesDetailView()
        {
            return View();
        }

        /// <summary>
        /// ค้นหารายการค่าใช้จ่ายต่างๆ ที่อยู่ในคำขอ งบประมาณ
        /// ซึ่งจะมีการสรุปข้อมูล จัดสรรงบประมาณ ในแต่ละรายการค่าใช้จ่ายมาด้วย เช่น ผู้จัดสรร วันที่จัดสรร จำนวนเงินที่จัดสรร ครั้งที่จัดสรร เป็นต้น
        /// *** นำไปแสดงเฉพาะที่ยอดคำขอหรือยอดจัดสรร มากกว่า 0
        /// </summary>
        /// <param name="reqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveBudgetRequestExpenses(string reqId)
        {
            if (string.IsNullOrEmpty(reqId))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_BUDGET_REQUEST_DETAIL_INFORMATIONs.Where(e => e.REQ_ID.Equals(reqId)
                        && (e.TOTAL_REQUEST_BUDGET > 0 ||
                            e.ALLOCATE_BUDGET_AMOUNT > 0 ||
                            e.ALLOCATE_OFF_BUDGET_AMOUNT > 0 ||
                            e.CASHBACK_BUDGET_AMOUNT > 0 ||
                            e.CASHBACK_OFF_BUDGET_AMOUNT > 0)
                    ).Select(e => new
                    {
                        e.REQ_ID,
                        e.ITEM_TYPE,
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
                        e.CREATED_DATETIME, // วันที่ทำคำขอ
                        e.TOTAL_REQUEST_BUDGET, // จำนวนที่ขอเงิน งปม.

                        // ข้อมูลการจัดสรร
                        e.ALLOCATE_BUDGET_AMOUNT, // เงินงบประมาณ ที่จัดสรร
                        e.ALLOCATE_OFF_BUDGET_AMOUNT, // เงินนอกงบประมาณ ที่จัดสรร
                        e.CASHBACK_BUDGET_AMOUNT, // ส่งคืนเงินงบประมาณ
                        e.CASHBACK_OFF_BUDGET_AMOUNT // ส่งคืนเงินนอกงบประมาณ
                    }).GroupBy(e => new
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
                    }).ToList();

                return Json(expr, JsonRequestBehavior.DenyGet);
            };
        }
    }
}