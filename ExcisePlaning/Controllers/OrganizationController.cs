using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class OrganizationController : Controller
    {
        // GET: Organization
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_ORGANIZATION);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_ORGANIZATION;
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

            return View();
        }


        [HttpPost]
        public ActionResult Retrieve(string orgName, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ORGANIZATIONs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(orgName))
                    expr = expr.Where(e => e.ORG_NAME.Contains(orgName));
                expr = expr.OrderBy(e => e.SORT_INDEX).ThenBy(e => e.ORG_ID);

                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.ORG_ID,
                    e.ORG_CODE,
                    e.ORG_NAME,
                    e.CREATED_DATETIME,
                    EXPENSES = (from orgExpenses in db.T_ORGANIZATION_RELATION_EXPENSEs.Where(s => s.ORG_ID.Equals(e.ORG_ID))
                                join expenses in db.T_EXPENSES_ITEMs.Where(x => x.ACTIVE.Equals(1))
                                on orgExpenses.EXPENSES_ID equals expenses.EXPENSES_ID
                                select new
                                {
                                    orgExpenses.EXPENSES_ID,
                                    expenses.EXPENSES_NAME
                                }).ToList()
                }).ToList();
            }
            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpGet]
        public ActionResult GetModalForm()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SubmitDelete(int? orgId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) { { "errorText", null } };
            if (null == orgId)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ORGANIZATIONs.Where(e => e.ORG_ID.Equals(orgId)).FirstOrDefault();
                if (null == expr)
                {
                    res["errorText"] = "ไม่พบองค์กรที่ต้องการยกเลิก";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (expr.ACTIVE.Equals(-1))
                {
                    res["errorText"] = "องค์กรนี้ถูกยกเลิกไปแล้ว";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                expr.ACTIVE = -1;
                expr.DELETED_DATETIME = DateTime.Now;
                expr.DELETED_ID = userAuthorizeProfile.EmpId;
                db.SubmitChanges();
            }
            return Json(res, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult SubmitSave(OrganizationFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) { { "errorText", null }, { "errors", null } };

            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var expr = db.T_ORGANIZATIONs.Where(e => e.ORG_ID.Equals(model.OrgId)).FirstOrDefault();
                if (null == expr)
                {
                    expr = new T_ORGANIZATION()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_ORGANIZATIONs.InsertOnSubmit(expr);
                }

                expr.ORG_NAME = model.OrgName;
                expr.ORG_CODE = model.OrgCode;

                // ยกเลิกองค์กรที่เกี่ยวข้องกับรายการ คชจ. เดิมทิ้งไปก่อน
                db.T_ORGANIZATION_RELATION_EXPENSEs.DeleteAllOnSubmit(db.T_ORGANIZATION_RELATION_EXPENSEs.Where(e => e.ORG_ID.Equals(model.OrgId)));

                db.SubmitChanges();


                // บันทึกรายการค่าใช้จ่าย ที่นำองค์กรไปใช้ในการบันทึกคำขอ
                if (null != model.ExpensesIds)
                {
                    model.ExpensesIds.ForEach(expensesId =>
                    {
                        db.T_ORGANIZATION_RELATION_EXPENSEs.InsertOnSubmit(new T_ORGANIZATION_RELATION_EXPENSE()
                        {
                            ORG_ID = expr.ORG_ID,
                            EXPENSES_ID = expensesId
                        });
                    });

                    db.SubmitChanges();
                }
            }
            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class OrganizationFormMapper
        {
            /// <summary>
            /// เลขที่กำลังขององกรค์
            /// </summary>
            public int? OrgId { get; set; }

            /// <summary>
            /// เลขที่อ้างอิงของ ลค.
            /// </summary>
            [MaxLength(30, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string OrgCode { get; set; }

            /// <summary>
            /// ชื่อองกรค์
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(200, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string OrgName { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่ายที่เกี่ยวข้องกับองกรค์
            /// ไว้ใช้สำหรับการคีย์คำขอเงินงบประมาณ ที่เป็นค่าใช้จ่ายปิโตรเลียม
            /// </summary>
            public List<int> ExpensesIds { get; set; }
        }
    }
}