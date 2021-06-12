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
    [CustomAuthorize(Roles = "Admin")]
    public class DepartmentController : Controller
    {
        // GET: Department
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DEPARTMENT);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_DEPARTMENT;
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


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_CODE = e.AREA_CODE,
                    AREA_NAME = e.AREA_NAME
                }).ToList();
            }

            return View();
        }


        [HttpPost]
        public ActionResult Retrieve(int? areaId, string depName, string depCode, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1));
                if (null != areaId)
                    expr = expr.Where(e => e.AREA_ID.Equals(areaId));
                if (null != depCode)
                    expr = expr.Where(e => e.DEP_CODE.Contains(depCode));
                if (null != depName)
                    expr = expr.Where(e => e.DEP_NAME.Contains(depName));
                expr = expr.OrderBy(e => e.SORT_INDEX);

                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.AREA_ID,
                    AREA_NAME = db.T_AREAs.Where(a => a.AREA_ID.Equals(e.AREA_ID)).Select(a => a.AREA_NAME).FirstOrDefault(),
                    e.DEP_ID,
                    e.DEP_CODE,
                    e.DEP_NAME,
                    ORDER_SEQ = e.SORT_INDEX,
                    e.DEP_SHORT_NAME,
                    e.DEP_AUTHORIZE, // 1 = หน่วยงานกลาง (ดูได้ทุกหน่วยงาน),2 = หน่วยงานทั่วไป
                    e.CAN_REQUEST_BUDGET // true = สามารถส่งคำขอเงินงบประมาณได้
                }).ToList();
            }
            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ถ้ายกเลิกหน่วยงาน ให้ตรวจสอบในปีงบประมาณมีการจัดสรรงบประมาณ ให้กับหน่วยงานนั้นไปหรือยัง
        /// โดยให้คืนงบประมาณกลับไปด้วย หากต้องการยกเลิก
        /// </summary>
        /// <param name="depId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitReject(int? depId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>() { { "errorText", null } };
            if (depId == null)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprDep = db.T_DEPARTMENTs.Where(e => e.DEP_ID.Equals(depId)).FirstOrDefault();
                if (null == exprDep)
                {
                    res["errorText"] = "ไม่พบหน่วยงานที่ต้องการยกเลิก";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (exprDep.ACTIVE.Equals(-1))
                {
                    res["errorText"] = string.Format("หน่วยงานนี้ถูกยกเลิกไปแล้ว เมื่อ {0}", exprDep.UPDATED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ถ้าในปีงบประมาณ มีการจัดสรรงบ ให้กับหน่วยงานแล้ว
                // ให้คืนงบประมาณกลับไปให้ส่วนกลางด้วย
                var fiscalYear = AppUtils.GetCurrYear();
                if (db.T_BUDGET_ALLOCATEs.Any(e => e.YR.Equals(fiscalYear) && e.DEP_ID.Equals(exprDep.DEP_ID)))
                {
                    res["errorText"] = string.Format("ปีงบประมาณ {0} จัดสรรงบประมาณให้หน่วยงานนี้แล้ว ไม่สามารถยกเลิกได้", fiscalYear + 543);
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                //var exprDepBudget = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.YR.Equals(fiscalYear) && e.ACTIVE.Equals(1)).ToList();
                //exprDepBudget.ForEach(expensesItem =>
                //{
                //    // คืนเงินงบประมาณกลับไปให้ส่วนกลาง
                //    if (expensesItem.ALLOCATE_BUDGET_AMOUNT.CompareTo(decimal.Zero) == 1)
                //        BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, fiscalYear
                //            , expensesItem.PLAN_ID
                //            , expensesItem.PRODUCE_ID
                //            , expensesItem.ACTIVITY_ID
                //            , expensesItem.BUDGET_TYPE_ID
                //            , expensesItem.EXPENSES_GROUP_ID
                //            , expensesItem.EXPENSES_ID
                //            , expensesItem.PROJECT_ID
                //            , 1, BudgetUtils.ADJUSTMENT_CASHBACK, expensesItem.ALLOCATE_BUDGET_AMOUNT);
                //});


                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                exprDep.ACTIVE = -1;
                exprDep.UPDATED_DATETIME = DateTime.Now;
                exprDep.USER_ID = userAuthorizeProfile.EmpId;
                db.SubmitChanges();
            }
            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// แบบฟอร์มแก้ไข/เพิ่มหน่วยงานในระบบ
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // เขตพื้นที่ของหน่วยงาน
                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_CODE = e.AREA_CODE,
                    AREA_NAME = e.AREA_NAME
                }).ToList();
            }
            return View();
        }

        /// <summary>
        /// หน่วยงานที่รับผิดชอบ ของหน่วยงานนัั้นๆ
        /// </summary>
        /// <param name="currDepId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetAuthorizeDepartment(int? currDepId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "allDepartments", null }, { "authorizeDepartmentIds", null } };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // หน่วยงานภายใต้สังกัดของ หน่วยงานหลัก
                var exprDep = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1));
                if (null != currDepId)
                    exprDep = exprDep.Where(e => !e.DEP_ID.Equals(currDepId));
                exprDep = exprDep.OrderBy(e => e.SORT_INDEX).ThenBy(e => e.DEP_ID);
                res["allDepartments"] = exprDep.OrderBy(e => e.AREA_ID).ThenBy(e => e.DEP_NAME).Select(e => new DepartmentShortFieldProperty()
                {
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME,
                    DEP_SHORT_NAME = e.DEP_SHORT_NAME,
                    AREA_NAME = db.T_AREAs.Where(a => a.AREA_ID.Equals(e.AREA_ID)).Select(a => a.AREA_NAME).FirstOrDefault()
                }).ToList();

                if (null != currDepId)
                    res["authorizeDepartmentIds"] = db.T_DEPARTMENT_AUTHORIZEs.Where(e => e.DEP_ID.Equals(currDepId)).Select(e => e.AUTHORIZE_DEP_ID).ToList();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public ActionResult SubmitSave(DepartmentFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "errors", null }, { "errorText", null } };

            // ตรวจสอบความถูกต้องของค่าในฟอร์มที่ผู้ใช้กรอกข้อมูล
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprDep = db.T_DEPARTMENTs.Where(e => e.DEP_ID.Equals(model.DepId)).FirstOrDefault();
                if (null == exprDep)
                {
                    exprDep = new T_DEPARTMENT()
                    {
                        ACTIVE = 1,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        SORT_INDEX = Convert.ToInt16(db.T_DEPARTMENTs.Count() + 1) // ให้ไปอยู่ลำดับล่างสุด
                    };
                    db.T_DEPARTMENTs.InsertOnSubmit(exprDep);
                }
                else
                {
                    if (exprDep.ACTIVE.Equals(-1))
                    {
                        res["errorText"] = "หน่วยงานนี้ยกเลิกไปแล้ว ไม่สามารถแก้ไขข้อมูลได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    exprDep.UPDATED_DATETIME = DateTime.Now;
                }

                exprDep.AREA_ID = model.AreaId;
                exprDep.DEP_NAME = model.DepName;
                exprDep.DEP_SHORT_NAME = model.DepShortName;
                exprDep.DEP_CODE = model.DepCode;
                exprDep.DEP_AUTHORIZE = model.DepAuthorize;
                exprDep.CAN_REQUEST_BUDGET = model.CanRequestBudget.Equals(1);
                if (null != model.OrderSeq)
                    exprDep.SORT_INDEX = model.OrderSeq.Value;

                db.T_DEPARTMENT_AUTHORIZEs.DeleteAllOnSubmit(db.T_DEPARTMENT_AUTHORIZEs.Where(e => e.DEP_ID.Equals(model.DepId)));

                db.SubmitChanges();

                // บันทึกหน่วยงาน ที่อยู่ภายใต้หน่วยงานนี้
                if (null != model.AuthorizeDepIds)
                {
                    model.AuthorizeDepIds.ForEach(depId =>
                    {
                        // ยกเว้นรหัสหน่วยงานของตนเอง
                        if (!exprDep.DEP_ID.Equals(depId))
                            db.T_DEPARTMENT_AUTHORIZEs.InsertOnSubmit(new T_DEPARTMENT_AUTHORIZE()
                            {
                                DEP_ID = exprDep.DEP_ID,
                                AUTHORIZE_DEP_ID = depId
                            });
                    });
                    db.SubmitChanges();
                }
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class DepartmentFormMapper
        {
            public int? DepId { get; set; }

            /// <summary>
            /// หน่วยงานอยู่ภายใต้เขตพื้นที่อะไร
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? AreaId { get; set; }


            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(190, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string DepName { get; set; }

            /// <summary>
            /// ชื่อย่อของหน่วยงาน
            /// </summary>
            [MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string DepShortName { get; set; }

            /// <summary>
            /// รหัสหน่วยรับงบประมาณ หรือ เลขที่อ้างอิงรหัสหน่วยงานของ ลค.
            /// </summary>
            [MaxLength(20, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string DepCode { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียงข้อมูล
            /// </summary> 
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? OrderSeq { get; set; }

            /// <summary>
            /// 1 = เป็นหน่วยงานกลาง ดูข้อมูลได้ทุกหน่วยงาน, 2 = หน่วยงานทั่วไป
            /// </summary>
            public short DepAuthorize { get; set; }

            /// <summary>
            /// 1 =  ทำคำของบประมาณได้, 0 = ทำคำของบประมาณไม่ได้
            /// </summary>
            public int CanRequestBudget { get; set; }

            /// <summary>
            /// รหัสหน่วยงานที่ มอบหมายให้หน่วยงาน
            /// เป็นผู้ดูแล
            /// </summary>
            public List<int> AuthorizeDepIds { get; set; }
        }
    }
}