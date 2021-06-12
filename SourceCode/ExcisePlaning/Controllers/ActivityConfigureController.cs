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
    public class ActivityConfigureController : Controller
    {
        // GET: ActivityConfigure
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_ACTIVITY_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_ACTIVITY_CONFIGURE;
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
                ViewBag.Produces = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new ProduceShortFieldProperty()
                {
                    PRODUCE_ID = e.PRODUCE_ID,
                    PRODUCE_NAME = e.PRODUCE_NAME
                }).ToList();
            }
            return View();
        }

        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Produces = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new ProduceShortFieldProperty()
                {
                    PRODUCE_ID = e.PRODUCE_ID,
                    PRODUCE_NAME = e.PRODUCE_NAME
                }).ToList();
            }

            return View();
        }

        [HttpPost, Route("ActivityCode:string,ActivityName:string, pageSize:int, pageIndex:int")]
        public ActionResult Retrieve(int? produceId, string activityName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1));
                if (null != produceId)
                    expr = expr.Where(e => e.PRODUCE_ID.Equals(produceId));
                if (!string.IsNullOrEmpty(activityName))
                    expr = expr.Where(e => e.ACTIVITY_NAME.Contains(activityName));

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.OrderBy(e => e.ORDER_SEQ).Skip(offset).Take(pageSize).Select(e => new
                {
                    e.PRODUCE_ID,
                    PRODUCE_NAME = db.T_PRODUCE_CONFIGUREs.Where(x => x.PRODUCE_ID.Equals(e.PRODUCE_ID)).Select(x => x.PRODUCE_NAME).FirstOrDefault(),
                    e.ACTIVITY_ID,
                    e.ACTIVITY_CODE,
                    e.ACTIVITY_NAME,
                    e.SHORT_NAME,
                    e.ORDER_SEQ
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpPost, Route("ActivityID:int?")]
        public void SubmitDelete(int? ActivityID)
        {
            if (ActivityID == null)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVITY_ID.Equals(ActivityID.Value)).FirstOrDefault();

                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                Expr.UPDATED_DATETIME = DateTime.Now;
                Expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                Expr.ACTIVE = -1;
                db.SubmitChanges();
            };
        }

        [HttpPost]
        public ActionResult SubmitSave(ActivityConfigureFormMapper model)
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

                T_ACTIVITY_CONFIGURE Expr = null;

                if (model.ActivityID != null)
                    Expr = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1) && e.ACTIVITY_ID.Equals(model.ActivityID)).FirstOrDefault();

                if (null == Expr)
                {
                    Expr = new T_ACTIVITY_CONFIGURE()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_ACTIVITY_CONFIGUREs.InsertOnSubmit(Expr);
                }
                else
                {
                    Expr.UPDATED_DATETIME = DateTime.Now;
                    Expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }
                Expr.PRODUCE_ID = model.ProduceId;
                Expr.ACTIVITY_CODE = model.ActivityCode;
                Expr.ACTIVITY_NAME = model.ActivityName;
                Expr.SHORT_NAME = model.ShortName;
                Expr.ORDER_SEQ = model.OrderSeq.Value;

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class ActivityConfigureFormMapper
        {
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? ProduceId { get; set; }

            public short? ActivityID { get; set; }

            public string ActivityCode { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(200, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ActivityName { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียงข้อมูล
            /// </summary> 
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? OrderSeq { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ShortName { get; set; }
        }
    }
}