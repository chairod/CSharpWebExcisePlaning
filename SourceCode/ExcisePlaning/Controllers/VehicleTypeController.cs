using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;
namespace ExcisePlaning.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class VehicleTypeController : Controller
    {
        // GET: PlanConfigure
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_VEHICLE_TYPE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_VEHICLE_TYPE;
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
        public ActionResult GetModalForm()
        {
            return View();
        }


        [HttpPost, Route("VehicleName:string, pageSize:int, pageIndex:int")]
        public ActionResult RetrieveForm(string VehicleName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                var expr = db.T_VEHICLE_TYPEs.Where(e => e.ACTIVE.Equals(1));

                if (!string.IsNullOrEmpty(VehicleName))
                    expr = expr.Where(e => e.VEHICLE_TYPE_NAME.Contains(VehicleName));


                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    VEHICLE_TYPE_ID = e.VEHICLE_TYPE_ID,
                    VEHICLE_TYPE_NAME = e.VEHICLE_TYPE_NAME,
                    COMPENSATION_PRICE = e.COMPENSATION_PRICE
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public void SubmitDelete(int? vehicleTypeId)
        {
            if (vehicleTypeId == null)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_VEHICLE_TYPEs.Where(e => e.VEHICLE_TYPE_ID.Equals(vehicleTypeId.Value)).FirstOrDefault();
                if (expr == null)
                    return;
                expr.ACTIVE = -1;
                db.SubmitChanges();
            };
        }


        [HttpPost]
        public ActionResult SubmitSave(VehicleTypeFormMapper model)
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

                var expr =  db.T_VEHICLE_TYPEs.Where(e => e.ACTIVE.Equals(1) && e.VEHICLE_TYPE_ID.Equals(model.VehicleTypeId)).FirstOrDefault();
                if (null == expr)
                {
                    expr = new T_VEHICLE_TYPE()
                    {
                        ACTIVE = 1,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    };
                    db.T_VEHICLE_TYPEs.InsertOnSubmit(expr);
                }

                expr.VEHICLE_TYPE_NAME = model.VehicleName;
                expr.COMPENSATION_PRICE = model.CompensationPrice.Value;
                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class VehicleTypeFormMapper
        {
            public VehicleTypeFormMapper() { }

            public short? VehicleTypeId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string VehicleName { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0.00", "99999999.99", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public decimal? CompensationPrice { get; set; }

        }
    }
}