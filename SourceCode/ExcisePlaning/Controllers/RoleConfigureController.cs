using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class RoleConfigureController : Controller
    {
        // GET: RoleConfigure
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_ROLE_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_ROLE_CONFIGURE;
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

        [HttpGet]
        public ActionResult GetModalResource()
        {
            return View("_Role_Configure_Modal_form");
        }

        [HttpPost, Route("roleName:string, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveRole(string roleName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ROLEs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(roleName))
                    expr = expr.Where(e => e.ROLE_NAME.Contains(roleName));

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalPages / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    ROLE_ID = e.ROLE_ID,
                    ROLE_NAME = e.ROLE_NAME,
                    ROLE_CONST = e.ROLE_CONST,
                    ITEM_DESCRIPTION = e.ITEM_DESCRIPTION,
                    MENUs = (from roleAuthorize in db.T_MENU_AUTHORIZEs.Where(a => a.ROLE_ID.Equals(e.ROLE_ID))
                             join menu in db.T_MENUs
                             on roleAuthorize.MENU_ID equals menu.MENU_ID
                             select new
                             {
                                 MENU_ID = menu.MENU_ID,
                                 MENU_NAME = menu.MENU_NAME
                             }).ToList(),
                    PERSONs = (from personAuthorize in db.T_PERSONNEL_AUTHORIZEs.Where(p => p.ROLE_ID.Equals(e.ROLE_ID))
                               join person in db.T_PERSONNELs.Where(p => p.ACTIVE.Equals(1))
                               on personAuthorize.PERSON_ID equals person.PERSON_ID
                               select new
                               {
                                   PERSON_ID = person.PERSON_ID,
                                   FIRST_NAME = person.FIRST_NAME,
                                   LAST_NAME = person.LAST_NAME
                               }).ToList()
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult SubmitSave(RoleFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "errors", null } };

            // ตรวจสอบการระบุค่า จากผู้ใช้งาน ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0)
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                T_ROLE roleExpr = null;
                if (model.RoleId != null)
                    roleExpr = db.T_ROLEs.Where(e => e.ACTIVE.Equals(1) && e.ROLE_ID.Equals(model.RoleId.Value)).FirstOrDefault();

                if (null == roleExpr)
                {
                    roleExpr = new T_ROLE()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    };
                    db.T_ROLEs.InsertOnSubmit(roleExpr);
                }
                else
                {
                    roleExpr.UPDATED_DATETIME = DateTime.Now;
                    roleExpr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                roleExpr.ROLE_NAME = model.RoleName;
                roleExpr.ROLE_CONST = model.RoleConst;
                roleExpr.ITEM_DESCRIPTION = model.RoleRemark;
                roleExpr.ACTIVE = 1;

                db.SubmitChanges();


                // บันทึกข้อมูล การเข้าถึงเมนู ของกลุ่มผู้ใช้งาน
                var roleAuthorizeExpr = db.T_MENU_AUTHORIZEs.Where(e => e.ROLE_ID.Equals(roleExpr.ROLE_ID));
                db.T_MENU_AUTHORIZEs.DeleteAllOnSubmit(roleAuthorizeExpr);

                if (model.MenuIds != null)
                    model.MenuIds.ForEach(menuId =>
                    {
                        db.T_MENU_AUTHORIZEs.InsertOnSubmit(new T_MENU_AUTHORIZE
                        {
                            MENU_ID = menuId,
                            ROLE_ID = roleExpr.ROLE_ID,
                            CREATED_DATETIME = DateTime.Now,
                            USER_ID = userAuthorizeProfile.EmpId,
                            ACTIVE = 1
                        });
                    });


                // บันทึก ผู้ใช้งาน ที่อยู่ภายใต้กลุ่มนี้
                var personAuthorizeExpr = db.T_PERSONNEL_AUTHORIZEs.Where(e => e.ROLE_ID.Equals(roleExpr.ROLE_ID));
                db.T_PERSONNEL_AUTHORIZEs.DeleteAllOnSubmit(personAuthorizeExpr);

                if (model.PersonIds != null)
                    model.PersonIds.ForEach(personId =>
                    {
                        db.T_PERSONNEL_AUTHORIZEs.InsertOnSubmit(new T_PERSONNEL_AUTHORIZE
                        {
                            PERSON_ID = personId,
                            ROLE_ID = roleExpr.ROLE_ID,
                            CREATED_DATETIME = DateTime.Now,
                            USER_ID = userAuthorizeProfile.EmpId,
                            ACTIVE = 1
                        });
                    });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("roleId:int?")]
        public void SubmitDelete(int? roleId)
        {
            if (roleId == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var roleExpr = db.T_ROLEs.Where(e => e.ACTIVE.Equals(1) && e.ROLE_ID.Equals(roleId.Value)).FirstOrDefault();
                if (roleExpr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                roleExpr.ACTIVE = -1;
                roleExpr.UPDATED_DATETIME = DateTime.Now;
                roleExpr.UPDATED_ID = userAuthorizeProfile.EmpId;

                // ยกเลิกบุคลาการออกจากกลุ่มผู้ใช้งาน
                var personAuthorizeExpr = db.T_PERSONNEL_AUTHORIZEs.Where(e => e.ROLE_ID.Equals(roleExpr.ROLE_ID));
                db.T_PERSONNEL_AUTHORIZEs.DeleteAllOnSubmit(personAuthorizeExpr);

                // ยกเลิกการเข้าถึงเมนู ออกจากกลุ่มผู้ใช้งาน
                var menuAuthorizeExpr = db.T_MENU_AUTHORIZEs.Where(e => e.ROLE_ID.Equals(roleExpr.ROLE_ID));
                db.T_MENU_AUTHORIZEs.DeleteAllOnSubmit(menuAuthorizeExpr);

                db.SubmitChanges();
            };
        }


        public class RoleFormMapper
        {
            public short? RoleId { get; set; }

            /// <summary>
            /// ชือ่กลุ่มผู้ใช้งาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RoleName { get; set; }


            /// <summary>
            /// ชือ่กลุ่มผู้ใช้งาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(20, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RoleConst { get; set; }

            /// <summary>
            /// ชือ่กลุ่มผู้ใช้งาน
            /// </summary>
            [MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RoleRemark { get; set; }

            /// <summary>
            /// รายการบุคลาการที่ อยู่ในกลุ่มผู้ใช้งานนี้
            /// </summary>
            public List<int> PersonIds { get; set; }

            /// <summary>
            /// รายการเมนูที่กลุ่มผู้ใช้งานนี้สามารถเข้าถึงได้
            /// </summary>
            public List<short> MenuIds { get; set; }
        }
    }
}