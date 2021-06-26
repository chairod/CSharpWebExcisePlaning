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
    /// กำหนดอัตราค่าอบรมและสัมนา
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class TraningAndSeminorsController : Controller
    {
        // GET: TraningAndSeminors
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_TRANING_AND_SEMINORS);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_TRANING_AND_SEMINORS;
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

        /// <summary>
        /// แบบฟอร์มการบันทึกค่าฝึกอบรมและสัมนา
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalForm()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Retrieve(string traningAndSeminorsName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_TRANING_AND_SEMINORs.Where(e => e.ACTIVE.Equals(1));
                if (!string.IsNullOrEmpty(traningAndSeminorsName))
                    expr = expr.Where(e => e.ITEM_TEXT.Contains(traningAndSeminorsName));

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.SEQ_ID,
                    e.ITEM_TEXT,
                    e.COMPENSATION_PRICE,
                    e.COMPENSATION_GOVERN_PRICE
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public void SubmitDelete(short? seqId)
        {
            if (seqId == null)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_TRANING_AND_SEMINORs.Where(e => e.SEQ_ID.Equals(seqId)).FirstOrDefault();
                if (expr == null)
                    return;

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                expr.ACTIVE = -1;
                expr.UPDATED_DATETIME = DateTime.Now;
                expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                db.SubmitChanges();
            };
        }


        [HttpPost]
        public ActionResult SubmitSave(TraningAndSeminorsFormMapper model)
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
                var expr = db.T_TRANING_AND_SEMINORs.Where(e => e.ACTIVE.Equals(1) && e.SEQ_ID.Equals(model.SeqId)).FirstOrDefault();
                if (null == expr)
                {
                    expr = new T_TRANING_AND_SEMINOR()
                    {
                        ACTIVE = 1,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    };
                    db.T_TRANING_AND_SEMINORs.InsertOnSubmit(expr);
                }
                else
                {
                    expr.UPDATED_DATETIME = DateTime.Now;
                    expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                expr.ITEM_TEXT = model.TraningAndSeminorsName;
                expr.COMPENSATION_PRICE = model.CompensationPrice.Value;
                expr.COMPENSATION_GOVERN_PRICE = model.CompensationGovernPrice.Value;
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class TraningAndSeminorsFormMapper
        {
            public short? SeqId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(150, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string TraningAndSeminorsName { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0.00", "99999999.99", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public decimal? CompensationPrice { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0.00", "99999999.99", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public decimal? CompensationGovernPrice { get; set; }
        }
    }
}