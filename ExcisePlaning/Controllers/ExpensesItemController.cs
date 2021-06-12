using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class ExpensesItemController : Controller
    {
        public ActionResult ExpensesItemForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_EXPENSES_ITEM);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_EXPENSES_ITEM;
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
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new BudgetTypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();
            }

            return View();
        }

        [HttpGet]
        public ActionResult GetModalResource()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.ExpensesGroup = db.V_GET_EXPENSES_GROUP_INFORMATIONs.Where(e => e.ACTIVE.Equals(1))
                    .OrderBy(e => e.EXPENSES_GROUP_NAME)
                    .ThenBy(e => e.BUDGET_TYPE_NAME)
                    .Select(e => new ExpensesGroupShortFieldProperty()
                    {
                        EXPENSES_GROUP_ID = e.EXPENSES_GROUP_ID,
                        EXPENSES_GROUP_NAME = e.EXPENSES_GROUP_NAME,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME,
                        EXPENSES_MASTER_NAME = e.EXPENSES_MASTER_NAME
                    }).ToList();
            }
            return View();
            //return View("_ExpensesItemModal");
        }

        [HttpPost]
        public ActionResult RetrieveForm(int? budgetTypeId, int? expensesGroupId, string expensesName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_EXPENSES_ITEMs.Where(e => e.ACTIVE > 0);
                if (budgetTypeId != null)
                    expr = expr.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId));
                if (expensesGroupId != null)
                    expr = expr.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId.Value));
                if (!string.IsNullOrEmpty(expensesName))
                    expr = expr.Where(e => e.EXPENSES_NAME.Contains(expensesName));

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.OrderBy(e => e.EXPENSES_GROUP_ORDER_SEQ).ThenBy(e => e.EXPENSES_ORDER_SEQ)
                    .Skip(offset).Take(pageSize).Select(e => new
                    {
                        e.BUDGET_TYPE_NAME,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_GROUP_NAME,
                        e.EXPENSES_MASTER_NAME,
                        e.EXPENSES_ID,
                        e.EXPENSES_NAME,
                        ORDER_SEQ = e.EXPENSES_ORDER_SEQ,
                        e.FORM_TEMPLATE_NAME,
                        e.CAN_ADD_PROJECT
                    }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ค้นหารายการทางบัญชีของ รายการค่าใช้จ่าย
        /// </summary>
        /// <param name="expensesId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetExpensesGL(int? expensesId)
        {
            if (null == expensesId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.T_EXPENSES_GLCODEs.Where(e => e.EXPENSES_ID.Equals(expensesId))
                        .Select(e => new
                        {
                            GLCode = e.GLCODE,
                            GLText = e.GL_TEXT
                        }).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        [HttpPost]
        public ActionResult SubmitSave(ExpensesFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "errors", null } };

            // ตรวจสอบการระบุค่า จากผู้ใช้งาน ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0 && modelErrors.Keys.Any(keyName => !keyName.Contains("ExpensesGLs[")))
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบความถูกต้องของรายการทางบัญชี
            List<Dictionary<string, ModelValidateErrorProperty>> expensesGLErrors = new List<Dictionary<string, ModelValidateErrorProperty>>();
            model.ExpensesGLs.ForEach(expensesGLItem =>
            {
                expensesGLErrors.Add(ModelValidateErrorProperty.TryOneValidate(expensesGLItem));
            });
            if (expensesGLErrors.Any(e => null != e))
            {
                res["errors"] = new Dictionary<string, object>(1) { { "ExpensesGLs", expensesGLErrors } };
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var expr = db.T_EXPENSES_ITEMs.Where(e => e.EXPENSES_ID.Equals(model.ExpensesId)).FirstOrDefault();
                if (expr == null)
                {
                    expr = new T_EXPENSES_ITEM()
                    {
                        ACTIVE = 1,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    };
                    db.T_EXPENSES_ITEMs.InsertOnSubmit(expr);
                }
                else
                {
                    expr.UPDATED_DATETIME = DateTime.Now;
                    expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                expr.EXPENSES_GROUP_ID = model.ExpensesGroupId.Value;
                expr.EXPENSES_NAME = model.ExpensesName;
                expr.FORM_TEMPLATE_NAME = model.FormTemplateName;
                expr.CAN_ADD_PROJECT = model.CanAddProject.Equals(1);
                expr.ORDER_SEQ = model.OrderSeq.Value;
                db.SubmitChanges();


                // รายการทางบัญชีของ รายการค่าใช้จ่าย
                db.T_EXPENSES_GLCODEs.DeleteAllOnSubmit(db.T_EXPENSES_GLCODEs.Where(e => e.EXPENSES_ID.Equals(expr.EXPENSES_ID)));
                model.ExpensesGLs.ForEach(expensesGLItem =>
                {
                    db.T_EXPENSES_GLCODEs.InsertOnSubmit(new T_EXPENSES_GLCODE()
                    {
                        EXPENSES_ID = expr.EXPENSES_ID,
                        GLCODE = expensesGLItem.GLCode,
                        GL_TEXT = expensesGLItem.GLText
                    });
                });
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        //[HttpPost, Route("ExpensesID:int?")]
        //public void SubmitDelete(int? ExpensesID)
        //{
        //    if (ExpensesID == null)
        //        return;
        //    using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
        //    {
        //        var Expr = db.T_EXPENSES_ITEMs.Where(e => e.EXPENSES_ID.Equals(ExpensesID.Value)).FirstOrDefault();

        //        if (Expr == null)
        //            return;

        //        UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
        //        Expr.ACTIVE = -1;
        //        Expr.UPDATED_DATETIME = DateTime.Now;
        //        Expr.UPDATED_ID = userAuthorizeProfile.EmpId;
        //        //db.T_EXPENSES_ITEMs.DeleteOnSubmit(Expr);
        //        db.SubmitChanges();


        //    };
        //}


        public class ExpensesFormMapper
        {
            public short? ExpensesId { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่าย อยู่ในหมวดค่าใช้จ่ายใด
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? ExpensesGroupId { get; set; }


            /// <summary>
            /// ชื่อรายการค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ExpensesName { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียงข้อมูล
            /// </summary> 
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? OrderSeq { get; set; }

            /// <summary>
            /// ชื่อแบบฟอร์ม ใช้สำหรับระบุรายละเอียดในการทำคำของบประมาณ
            /// 
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(200, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string FormTemplateName { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายนี้ สามารถเพิ่มโครงการ ในตอนที่รับเงินที่ได้รับจัดสรรจากรัฐบาลมาได้หรือไม่
            /// </summary>
            public short CanAddProject { get; set; }

            /// <summary>
            /// รายการทางบัญชีของรายการค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "กรุณาเพิ่มรายการทางบัญชีก่อน")]
            public List<ExpensesGLProperty> ExpensesGLs { get; set; }
        }

        public class ExpensesGLProperty
        {
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MinLength(10, ErrorMessage = "ความยาวอย่างน้อย {1} ตัวอักษร"), MaxLength(50, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string GLCode { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string GLText { get; set; }
        }
    }
}