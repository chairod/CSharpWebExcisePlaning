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
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3,General")]
    public class BudgetRequestTemplateController : Controller
    {
        // GET: BudgetRequestTemplate
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_REQUEST_TEMPLATE_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_REQUEST_TEMPLATE_MENU;
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

            ViewBag.DepartmentId = userAuthorizeProfile.DepId;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // หน่วยงาน
                var depExpr = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SORT_INDEX).Select(e => new DepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME
                }).ToList();
                if (!userAuthorizeProfile.AccountType.Value.Equals(1)) // ถ้าไม่ใช่ Super user ดูได้เฉพาะหน่วยงานของตน
                    depExpr = depExpr.Where(e => e.DEP_ID.Equals(userAuthorizeProfile.DepId)).ToList();
                ViewBag.Departments = depExpr;

                // ประเภทงบประมาณ (งบดำเนินงาน งบลงทุน)
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.BUDGET_TYPE_ID).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();

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

                // หมวดค่าใช้จ่าย
                ViewBag.ExpensesGroups = db.T_EXPENSES_GROUPs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.EXPENSES_GROUP_ID).Select(e => new ExpensesGroupShortFieldProperty()
                {
                    EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                    EXPENSES_GROUP_NAME = e.EXPENSES_GROUP_NAME
                }).ToList();
            }

            return View();
        }


        /// <summary>
        /// ค้นหาข้อมูล Template
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("forDepId:int?, planId:int?, produceId:int?, activityId:int?, budgetTypeId:int?, expensesGroupId:int?, templateName:string, forYear:short?, pageIndex:int, pageSize:int")]
        public ActionResult Retrieve(int? forDepId, int? planId, int? produceId, int? activityId, int? budgetTypeId, int? expensesGroupId, string templateName, short? forYear, int pageIndex, int pageSize)
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

                var expr = db.V_GET_BUDGET_TEMPLATE_INFORMATIONs.AsQueryable();

                // กรณีไม่ใช่ Super User ให้มองเห็นได้เฉพาะ หน่วยงานที่ให้สิทธิ์
                if (!userAuthorizeProfile.AccountType.Value.Equals(1))
                    forDepId = userAuthorizeProfile.DepId;

                // กรณีระบุเจาะจงหน่วยงานที่ได้รับสิทธิ์ให้เข้าถึง Template
                // และต้องรวม Shared Template ด้วย
                if (null != forDepId)
                    expr = expr.Where(e => (e.SHARED_DEP_TEMPLATE.Equals(1) || (e.SHARED_DEP_TEMPLATE.Equals(2) && db.T_BUDGET_REQUEST_TEMPLATE_DEPARTMENT_AUTHORIZEs.Any(authorize => authorize.TEMPLATE_ID.Equals(e.TEMPLATE_ID) && authorize.DEP_ID.Equals(forDepId)))));

                // กรณีระบุเจาะจงสิทธิ์การเข้าถึง Template เฉพาะปี งปม.
                if (null != forYear)
                    expr = expr.Where(e => (e.SHARED_YR_TEMPLATE.Equals(1) || (e.SHARED_YR_TEMPLATE.Equals(2) && db.T_BUDGET_REQUEST_TEMPLATE_YR_AUTHORIZEs.Any(authorize => authorize.TEMPLATE_ID.Equals(e.TEMPLATE_ID) && authorize.YR.Equals(forYear)))));

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

                var finalExpr = expr.Select(e => new
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
        /// เรียกใช้งานจาก angular.app.js "createOrUpdateBudgetRequestTemplate"
        /// แสดงแบบฟอร์ม สร้าง/แก้ไข ข้อมูล Template สำหรับส่งคำขอเงินงบประมาณ
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalForm()
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
            }
            return View();
        }


        /// <summary>
        /// บันทึก ข้อมูล Template สำหรับจัดทำคำขอเงิน งปม. (งปม. ต้นปี, คำขอ งปม. เพิ่มเติม)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitSave(BudgetRequestTemplateFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>()
            {
                { "errors", null },
                { "errorText", null }
            };


            // ตรวจสอบความถูกต้องของการระบุข้อมูลจากหน้าฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (model.SharedDepartment.Equals(2) && (null == model.Departments || !model.Departments.Any()))
                modelErrors.Add("Departments", new ModelValidateErrorProperty("Departments", new List<string>() { "ระบุหน่วยงานที่สามารถใช้ Template อย่างน้อย 1 หน่วยงาน" }));
            if (model.SharedYear.Equals(2) && (null == model.Years || !model.Years.Any()))
                modelErrors.Add("Years", new ModelValidateErrorProperty("Years", new List<string>() { "ระบุปี งปม. ที่สามารถใช้ Template อย่างน้อย 1 ปี" }));

            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var expr = db.T_BUDGET_REQUEST_TEMPLATEs.Where(e => e.TEMPLATE_ID.Equals(model.TemplateId)).FirstOrDefault();
                if (expr == null)
                {
                    expr = new T_BUDGET_REQUEST_TEMPLATE()
                    {
                        FOR_BUDGET = true,
                        FOR_OFF_BUDGET = true,
                        FOR_SOURCE_BUDGET_BEGIN = true,
                        FOR_SOURCE_BUDGET_ADJUNCT = true,
                        FOR_SOURCE_BUDGET_INBOUND = true,
                        ACTIVE = 1,
                        CREATE_TYPE = model.CreateType.Value,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    };
                    db.T_BUDGET_REQUEST_TEMPLATEs.InsertOnSubmit(expr);
                }
                else
                {
                    expr.UPDATED_DATETIME = DateTime.Now;
                    expr.UPDATED_ID = userAuthorizeProfile.EmpId;

                    // ลบรายการ คชจ. ออกไปแล้วค่อยเพิ่มใหม่
                    db.T_BUDGET_REQUEST_TEMPLATE_EXPENSEs.DeleteAllOnSubmit(db.T_BUDGET_REQUEST_TEMPLATE_EXPENSEs.Where(e => e.TEMPLATE_ID.Equals(model.TemplateId)));

                    // ลบสิทธิ์การใช้ Template หน่วยงาน ออกไปแล้วค่อยเพิ่มใหม่
                    db.T_BUDGET_REQUEST_TEMPLATE_DEPARTMENT_AUTHORIZEs.DeleteAllOnSubmit(db.T_BUDGET_REQUEST_TEMPLATE_DEPARTMENT_AUTHORIZEs.Where(e => e.TEMPLATE_ID.Equals(model.TemplateId)));

                    // ลบสิทธิ์การใช้ Template ปี งปม. ออกไปแล้วค่อยเพิ่มใหม่
                    db.T_BUDGET_REQUEST_TEMPLATE_YR_AUTHORIZEs.DeleteAllOnSubmit(db.T_BUDGET_REQUEST_TEMPLATE_YR_AUTHORIZEs.Where(e => e.TEMPLATE_ID.Equals(model.TemplateId)));
                }


                expr.TEMPLATE_NAME = model.TemplateName;
                expr.PLAN_ID = model.PlanId;
                expr.PRODUCE_ID = model.ProduceId;
                expr.ACTIVITY_ID = model.ActivityId;
                expr.BUDGET_TYPE_ID = model.BudgetTypeId.Value;
                expr.EXPENSES_GROUP_ID = model.ExpensesGroupId.Value;
                expr.SHARED_DEP_TEMPLATE = Convert.ToInt16(model.SharedDepartment);
                expr.SHARED_YR_TEMPLATE = Convert.ToInt16(model.SharedYear);

                // กรณีเป็นการสร้างรายการใหม่ ให้บันทึกส่วน Master ก่อนเพื่อให้ได้ ID 
                // และเป็นการลบข้อมูล สิทธิ์ (หน่วยงาน, ปี งปม.), รายการ คชจ. 
                if (null == model.TemplateId)
                    db.SubmitChanges();


                // บันทึกรายการ คชจ. ที่ใช้ใน Template นี้
                model.Expenses.ForEach(expensesId =>
                {
                    db.T_BUDGET_REQUEST_TEMPLATE_EXPENSEs.InsertOnSubmit(new T_BUDGET_REQUEST_TEMPLATE_EXPENSE
                    {
                        TEMPLATE_ID = expr.TEMPLATE_ID,
                        EXPENSES_ID = expensesId
                    });
                });


                // บันทึกสิทิ์การใช้ Template ส่วนหน่วยงาน
                if (null != model.Departments)
                    model.Departments.ForEach(depId =>
                    {
                        db.T_BUDGET_REQUEST_TEMPLATE_DEPARTMENT_AUTHORIZEs.InsertOnSubmit(new T_BUDGET_REQUEST_TEMPLATE_DEPARTMENT_AUTHORIZE
                        {
                            TEMPLATE_ID = expr.TEMPLATE_ID,
                            DEP_ID = depId
                        });
                    });

                // บันทึกสิทธิ์การใช้ Template ส่วนปี งปม. (ปี ค.ศ.)
                if (null != model.Years)
                    model.Years.ForEach(year =>
                    {
                        db.T_BUDGET_REQUEST_TEMPLATE_YR_AUTHORIZEs.InsertOnSubmit(new T_BUDGET_REQUEST_TEMPLATE_YR_AUTHORIZE
                        {
                            TEMPLATE_ID = expr.TEMPLATE_ID,
                            YR = year
                        });
                    });

                db.SubmitChanges();
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetRequestTemplateFormMapper
        {
            /// <summary>
            /// เลขที่รายการของ Template กรณีแก้ไขจะมีค่านี้ส่งกลับมา
            /// </summary>
            public int? TemplateId { get; set; }

            /// <summary>
            /// ชื่อเรียก Template
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(200, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string TemplateName { get; set; }

            /// <summary>
            /// ประเภทการสร้าง Template, 1 = สร้างโดย Admin, 2 = สร้างโดยหน่วยงาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? CreateType { get; set; }

            /// <summary>
            /// แผนงาน
            /// </summary>
            //[Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            [Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public int? PlanId { get; set; }

            /// <summary>
            /// ผลผลิต
            /// </summary>
            //[Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            [Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public int? ProduceId { get; set; }

            /// <summary>
            /// กิจกรรม
            /// </summary>
            //[Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            [Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public int? ActivityId { get; set; }

            /// <summary>
            /// งบรายจ่าย เช่น งบดำเนินงาน งบอุดหนุด ฯลฯ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public int? BudgetTypeId { get; set; }

            /// <summary>
            /// หมวด คชจ.
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public int? ExpensesGroupId { get; set; }

            /// <summary>
            /// Template นี้ใช้สำหรับเงิน งปม. ค่าที่รับมาจากฟอร์ม 1 = ใช้ทุกหน่วยงาน,2 = ใช้เฉพาะบางหน่วยงาน
            /// </summary>
            public int ForBudget { get; set; }

            /// <summary>
            /// Template นี้ใช้สำหรับเงินนอก งปม. ค่าที่รับมาจากฟอร์ม 1 = ใช้ทุกปี งปม.,2 = ใช้บางปี งปม.
            /// </summary>
            public int ForOffBudget { get; set; }

            /// <summary>
            /// Template นี้ใช้สำหรับคำขอต้นปี ค่าที่รับมาจากฟอร์ม 1,0
            /// </summary>
            public int ForSourceBudgetBegin { get; set; }

            /// <summary>
            /// Template นี้ใช้สำหรับคำของบเพิ่มเติม ค่าที่รับมาจากฟอร์ม 1,0
            /// </summary>
            public int ForSourceBudgetAdjunct { get; set; }


            /// <summary>
            /// Template ใช้กับทุกหน่วยงานใช่หรือไม่ ค่าที่รับมาจากฟอร์ม 1,0
            /// </summary>
            public int SharedDepartment { get; set; }

            /// <summary>
            /// Template ใช้ได้ทุกปี งปม. หรือไม่ ค่าที่รับมาจากฟอร์ม 1,0
            /// </summary>
            public int SharedYear { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่อยู่ภายใน Template นี้ (IDs)
            /// </summary>
            [Required(ErrorMessage = "ระบุรายการค่าใช้จ่ายอย่างน้อย 1 รายการ")]
            public List<int> Expenses { get; set; }

            /// <summary>
            /// Template นี้ใช้ได้กับหน่วยงานใดบ้าง (IDs)
            /// </summary>
            public List<int> Departments { get; set; }

            /// <summary>
            /// Template นี้ใช้ได้กับปี งปม. ใดบ้าง (ปี พ.ศ./ค.ศ. ขึ้นอยู่กับ Locale ที่ตั้งไว้)
            /// </summary>
            public List<short> Years { get; set; }
        }

    }
}