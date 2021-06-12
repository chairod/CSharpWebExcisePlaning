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
    public class ProductConfigureController : Controller
    {
        // GET: ProductConfigure
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_PRODUCE_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_PRODUCE_CONFIGURE;
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
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();
            }

            return View();
        }

        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();
            }
            return View();
        }

        [HttpPost, Route("ProductCode:string,ProductName:string, pageSize:int, pageIndex:int")]
        public ActionResult Retrieve(int? planId, string produceName, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                var expr = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1));

                if (null != planId)
                    expr = expr.Where(e => e.PLAN_ID.Equals(planId));
                if (!string.IsNullOrEmpty(produceName))
                    expr = expr.Where(e => e.PRODUCE_NAME.Contains(produceName));

                int offset = pageIndex * pageSize - pageSize;
                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.OrderBy(e => e.ORDER_SEQ).Skip(offset).Take(pageSize).Select(e => new
                {
                    e.PLAN_ID,
                    PLAN_NAME = db.T_PLAN_CONFIGUREs.Where(x => x.PLAN_ID.Equals(e.PLAN_ID)).Select(x => x.PLAN_NAME).FirstOrDefault(),
                    e.PRODUCE_ID,
                    e.PRODUCE_CODE,
                    e.PRODUCE_NAME,
                    e.SHORT_NAME,
                    e.ORDER_SEQ
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("ProductID:int?")]
        public void SubmitDelete(int? ProductID)
        {
            if (ProductID == null)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_PRODUCE_CONFIGUREs.Where(e => e.PRODUCE_ID.Equals(ProductID.Value)).FirstOrDefault();
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
        public ActionResult SubmitSave(PlanConfugureFormMapper model)
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

                T_PRODUCE_CONFIGURE Expr = null;
                if (model.ProductID != null)
                    Expr = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1) && e.PRODUCE_ID.Equals(model.ProductID)).FirstOrDefault();

                if (null == Expr)
                {
                    Expr = new T_PRODUCE_CONFIGURE()
                    {
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_PRODUCE_CONFIGUREs.InsertOnSubmit(Expr);
                }
                else
                {
                    Expr.UPDATED_DATETIME = DateTime.Now;
                    Expr.UPDATED_ID = userAuthorizeProfile.EmpId;
                }
                Expr.PLAN_ID = model.PlanId;
                Expr.PRODUCE_CODE = model.ProductCode;
                Expr.PRODUCE_NAME = model.ProductName;
                Expr.ORDER_SEQ = model.OrderSeq.Value;
                Expr.SHORT_NAME = model.ShortName;

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class PlanConfugureFormMapper
        {
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? PlanId { get; set; }

            public short? ProductID { get; set; }

            public string ProductCode { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(200, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ProductName { get; set; }

            /// <summary>
            /// ลำดับการจัดเรียงข้อมูล
            /// </summary> 
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? OrderSeq { get; set; }

            [MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ShortName { get; set; }

        }
    }
}