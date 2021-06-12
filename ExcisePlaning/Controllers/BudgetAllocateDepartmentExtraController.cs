using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// กรมสรรพสามิตจัดสรรเงินงบประมาณเพิ่มเติม/พิเศษ ที่นอกเหนือจากคำขอ ให้กับหน่วยงานภูมิภาค
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetAllocateDepartmentExtraController : Controller
    {
        // GET: BudgetAllocateDepartmentExtra
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            // ตรวจสอบการเข้าทำงานของจอ
            var fiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            var verifyBudget = BudgetUtils.VerifyBudget(fiscalYear, null);
            if (!verifyBudget.IsComplete)
                return RedirectToAction("GetPageWarning", "BudgetAllocateDepartmentGroup");

            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_ALLOCATE_DEPARTMENT_EXTRA_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_ALLOCATE_DEPARTMENT_EXTRA_MENU;
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

            ViewBag.FiscalYear = AppUtils.GetCurrYear();
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // เฉพาะหน่วยงานที่ ทำคำขอเงินงบประมาณได้
                // ในระบบจะมีทั้ง หน่วยงานที่คุมหน่วยงานภูมิภาค ซึ่งหน่วยงานเหล่านั้นไม่สามารถทำคำขอ และ ไม่ได้รับเงิบงบประมาณ
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && e.CAN_REQUEST_BUDGET).Select(e => new DepartmentShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = db.T_AREAs.Where(a => a.AREA_ID.Equals(e.AREA_ID)).Select(a => a.AREA_NAME).FirstOrDefault(),
                    DEP_CODE = e.DEP_CODE,
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME,
                    DEP_SHORT_NAME = e.DEP_SHORT_NAME,
                    DEP_SORT_INDEX = e.SORT_INDEX
                }).OrderBy(e => e.AREA_ID).ThenBy(e => e.DEP_NAME).ToList();

                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).ToList();
            }

            return View();
        }


        /// <summary>
        /// ตรวจสอบฟอร์มจัดสรรงบประมาณ และ นำเข้าข้อมูล
        /// </summary>
        /// <param name="filename">ไฟล์ Template จัดสรรงบประมาณที่อัพโหลดไว้แล้ว</param>
        /// <param name="actionType">VERIFY = อ่านข้อมูลในไฟล์เพื่อสรุปข้อมูลการจัดสรรให้หน่วยงาน, COMMIT = อ่านข้อมูลในไฟล์และนำเข้าข้อมูลการจัดสรร</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult VerifyDocument(string filename, string actionType)
        {
            var controller = DependencyResolver.Current.GetService<BudgetAllocateDepartmentGroupController>();
            controller.ControllerContext = new ControllerContext(Request.RequestContext, controller);
            return controller.VerifyDocument(filename, actionType);
        }

        [HttpGet]
        public ActionResult GetVerifyModalForm()
        {
            //var controller = DependencyResolver.Current.GetService<BudgetAllocateDepartmentGroupController>();
            //controller.ControllerContext = new ControllerContext(Request.RequestContext, controller);
            //return controller.GetVerifyModalForm();
            return RedirectToAction("GetVerifyModalForm", "BudgetAllocateDepartmentGroup");
        }

        [HttpPost]
        public void DeleteUploadFile(string filename)
        {
            var controller = DependencyResolver.Current.GetService<BudgetAllocateDepartmentGroupController>();
            controller.ControllerContext = new ControllerContext(Request.RequestContext, controller);
            controller.DeleteUploadFile(filename);
        }

        /// <summary>
        /// สร้างแบบฟอร์มในการจัดสรร และ ดาวน์โหลดไฟล์ไปยังเครื่องไคเอ็นท์
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetDownloadAllocateForm(BudgetAllocateDepartmentExtraFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(3) {
                { "errorText", null },
                { "errors", null },
                { "filename", null }
            };

            // สามารถจัดสรรงบประมาณ ได้เฉพาะปีงบประมาณ ปัจจุบัน
            model.FiscalYear = AppUtils.GetCurrYear();
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // รหัสหน่วยงานที่ไม่สามารถส่งคำของบประมาณได้
            // ให้ยกเว้น ออกไปไม่สร้างใน Template จัดสรรคำของบประมาณ
            var depIdsIgnoreToAllocate = AppUtils.GetAllDepartmentIdsCannotRequestBudget();
            model.Departments = model.Departments.Where(e => !depIdsIgnoreToAllocate.Contains(e.DEP_ID)).ToList();
            if (!model.Departments.Any())
            {
                res["errorText"] = "หน่วยงานที่เลือกไม่สามารถจัดสรรงบประมาณได้ โปรดตรวจสอบ";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบ Template ของแบบฟอร์มจัดสรรงบประมาณ
            var appSettings = AppSettingProperty.ParseXml();
            string templateFile = string.Format("{0}/Allocate_Budget_To_Department_Template.xlsx", appSettings.ReportTemplatePath);
            if (!System.IO.File.Exists(templateFile))
            {
                res["errorText"] = "ไม่พบแบบฟอร์มคำขอ โปรดแจ้งผู้ดูแลระบบให้ตรวจสอบแบบฟอร์ม BudgetAllocateTemplate.xlsx";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            var verifyBudget = BudgetUtils.VerifyBudget(model.FiscalYear, model.AllocateType);
            if (!verifyBudget.IsComplete)
            {
                res["errorText"] = verifyBudget.FormatCauseMessageToUser();
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprExpensesAllocate = new List<proc_GetDepartmentRequestBudgetForAllocateResult>();
                model.Expenses.ForEach(expensesItem =>
                {
                    model.Departments.ForEach(departmentItem =>
                    {
                        // ค้นหางบประมาณที่เคยจัดสรรให้กับหน่วยงาน
                        decimal allocateBudgetAmounts = db.fn_GetAllocateBudgetAmountsToDepartmentBy(model.FiscalYear, departmentItem.DEP_ID
                            , expensesItem.PLAN_ID, expensesItem.PRODUCE_ID
                            , expensesItem.ACTIVITY_ID, expensesItem.BUDGET_TYPE_ID
                            , expensesItem.EXPENSES_GROUP_ID, expensesItem.EXPENSES_ID
                            , expensesItem.PROJECT_ID, model.AllocateType.Value).GetValueOrDefault(decimal.Zero);
                        decimal? depAllocateBudgetAmounts = model.AllocateType.Value.Equals(1) ? allocateBudgetAmounts : decimal.Zero;
                        decimal? depAllocateOffBudgetAmounts = model.AllocateType.Value.Equals(2) ? allocateBudgetAmounts : decimal.Zero;

                        exprExpensesAllocate.Add(new proc_GetDepartmentRequestBudgetForAllocateResult()
                        {
                            YR = model.FiscalYear,
                            PLAN_ID = expensesItem.PLAN_ID,
                            PLAN_NAME = expensesItem.PLAN_NAME,
                            PLAN_ORDER_SEQ = expensesItem.PLAN_ORDER_SEQ,
                            PRODUCE_ID = expensesItem.PRODUCE_ID,
                            PRODUCE_NAME = expensesItem.PRODUCE_NAME,
                            PRODUCE_ORDER_SEQ = expensesItem.PRODUCE_ORDER_SEQ,
                            ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                            ACTIVITY_NAME = expensesItem.ACTIVITY_NAME,
                            ACTIVITY_SHORT_NAME = string.Empty,
                            ACTIVITY_ORDER_SEQ = expensesItem.ACTIVITY_ORDER_SEQ,
                            BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                            BUDGET_TYPE_NAME = expensesItem.BUDGET_TYPE_NAME,
                            BUDGET_TYPE_ORDER_SEQ = expensesItem.BUDGET_TYPE_ORDER_SEQ,
                            EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                            EXPENSES_GROUP_NAME = expensesItem.EXPENSES_GROUP_NAME,
                            EXPENSES_GROUP_ORDER_SEQ = expensesItem.EXPENSES_GROUP_ORDER_SEQ,
                            EXPENSES_GROUP_ALLOCATE_GROUP_FLAG = expensesItem.EXPENSES_GROUP_ALLOCATE_GROUP_FLAG,
                            
                            EXPENSES_ID = expensesItem.EXPENSES_ID,
                            EXPENSES_NAME = expensesItem.EXPENSES_NAME,
                            EXPENSES_ORDER_SEQ = expensesItem.EXPENSES_ORDER_SEQ,
                            GLCODEs = expensesItem.GLCODEs,
                            PROJECT_ID = expensesItem.PROJECT_ID,
                            PROJECT_NAME = expensesItem.PROJECT_NAME,
                            

                            REQUIRED_ALLOCATE_TYPE = model.AllocateType,
                            REQUIRED_REQUEST_TYPE = 0, // กำหนดเป็น 0 เนื่องจากไม่ได้เกิดจาก การของบประมาณของหน่วยงานภูมิภาค

                            EXPENSES_ACTUAL_BUDGET_AMOUNT = decimal.Zero,
                            EXPENSES_REMAIN_BUDGET_AMOUNT = decimal.Zero,
                            EXPENSES_ACTUAL_OFF_BUDGET_AMOUNT = decimal.Zero,
                            EXPENSES_REMAIN_OFF_BUDGET_AMOUNT = decimal.Zero,

                            // จำนวนเงินที่เคยจัดสรรให้กับหน่วยงาน
                            DEP_ALLOCATE_EXPENSES_BUDGET_AMOUNT = depAllocateBudgetAmounts,
                            DEP_ALLOCATE_EXPENSES_OFF_BUDGET_AMOUNT = depAllocateOffBudgetAmounts,

                            DEP_ID = departmentItem.DEP_ID,
                            DEP_NAME = departmentItem.DEP_NAME,
                            DEP_CODE = departmentItem.DEP_CODE,
                            DEP_SORT_INDEX = departmentItem.DEP_SORT_INDEX,
                            AREA_ID = departmentItem.AREA_ID,
                            AREA_NAME = departmentItem.AREA_NAME,
                            REQ_ID = string.Empty
                        });
                    });
                });


                var controller = DependencyResolver.Current.GetService<BudgetAllocateDepartmentGroupController>();
                controller.ControllerContext = new ControllerContext(Request.RequestContext, controller);
                res["filename"] = controller.GenerateAllocateTemplateFile(exprExpensesAllocate, appSettings, userAuthorizeProfile, null, Convert.ToInt32(model.AllocateType.Value));
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetAllocateDepartmentExtraFormMapper
        {
            /// <summary>
            /// ปีงบประมาณที่ต้องการจัดสรรงบประมาณ ให้กับหน่วยงานภูมิภาค
            /// </summary>
            public short FiscalYear { get; set; }

            /// <summary>
            /// ประเภทงบประมาณ 
            /// 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาร
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), Range(typeof(short), "1", "2", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public short? AllocateType { get; set; }

            /// <summary>
            /// หน่วยงานที่เลือก เพื่อจัดสรรงบประมาณให้
            /// </summary>
            [Required(ErrorMessage = "กรุณาเลือกหน่วยงานที่ต้องการจัดสรรงบประมาณ")]
            public List<DepartmentShortFieldProperty> Departments { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่ต้องการจัดสรร ให้กับหน่วยงานที่เลือก
            /// </summary>
            [Required(ErrorMessage = "กรุณาเลือกรายการค่าใช้จ่าย")]
            public List<BudgetAllocateExtraExpensesProperty> Expenses { get; set; }
        }

        /// <summary>
        /// ข้อมูลที่นำมา Mapping ที่ Class นี้
        /// จะได้มาจากการ เลือกของ Modal form ซึ่งเป็นการค้นหาข้อมูลจาก
        /// HelperController.RetrieveBudgetExpenses
        /// </summary>
        public class BudgetAllocateExtraExpensesProperty
        {
            /// <summary>
            /// ปีงบประมาณของข้อมูล
            /// </summary>
            public short YR { get; set; }

            /// <summary>
            /// รหัสแผนงาน
            /// </summary>
            public int PLAN_ID { get; set; }

            public string PLAN_NAME { get; set; }
            public short PLAN_ORDER_SEQ { get; set; }

            /// <summary>
            /// รหัส ผลผลิต
            /// </summary>
            public int PRODUCE_ID { get; set; }
            public string PRODUCE_NAME { get; set; }
            public short PRODUCE_ORDER_SEQ { get; set; }

            /// <summary>
            /// รหัส กิจกรรม
            /// </summary>
            public int ACTIVITY_ID { get; set; }
            public string ACTIVITY_NAME { get; set; }
            public short ACTIVITY_ORDER_SEQ { get; set; }

            /// <summary>
            /// รหัส งบดำเนินงาน
            /// </summary>
            public int BUDGET_TYPE_ID { get; set; }
            public string BUDGET_TYPE_NAME { get; set; }
            public int? BUDGET_TYPE_ORDER_SEQ { get; set; }

            /// <summary>
            /// รหัสหมวด คชจ.
            /// </summary>
            public int EXPENSES_GROUP_ID { get; set; }
            public string EXPENSES_GROUP_NAME { get; set; }
            public short EXPENSES_GROUP_ORDER_SEQ { get; set; }
            /// <summary>
            /// 1 = จัดสรรงบประมาณตามหมวดค่าใช้จ่าย
            /// </summary>
            public short EXPENSES_GROUP_ALLOCATE_GROUP_FLAG { get; set; }

            /// <summary>
            /// รหัสรายการ คชจ.
            /// </summary>
            public int EXPENSES_ID { get; set; }
            public string EXPENSES_NAME { get; set; }
            public short EXPENSES_ORDER_SEQ { get; set; }

            /// <summary>
            /// รหัสทางบัญชีของรายการ คชจ.
            /// </summary>
            public string GLCODEs { get; set; }

            /// <summary>
            /// รหัสโครงการ ภายใต้รายการ คชจ.
            /// </summary>
            public int? PROJECT_ID { get; set; }
            public string PROJECT_NAME { get; set; }
        }
    }
}