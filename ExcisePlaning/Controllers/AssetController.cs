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
    public class AssetController : Controller
    {
        // GET: AssetForm
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_ASSET_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_ASSET_MENU;
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
                ViewBag.AssetTypes = db.T_ASSET_TYPEs.OrderBy(e => e.ORDER_SEQ).ToList();
            }
            return View();
        }

        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.AssetTypes = db.T_ASSET_TYPEs.ToList();
            }
            return View();
        }

        [HttpPost]
        public ActionResult Retrieve(string assetName, int? assetTypeId, int pageSize, int pageIndex)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                totalPages = 0,
                totalRecords = 0,
                rows = null
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = from exprAsset in db.T_ASSETs.Where(e => e.ACTIVE.Equals(1))
                           join exprAssetType in db.T_ASSET_TYPEs
                           on exprAsset.ASSET_TYPE equals exprAssetType.ASSET_TYPE_ID
                           select new
                           {
                               exprAsset.ASSET_ID,
                               exprAsset.ASSET_NAME,
                               exprAsset.ASSET_TYPE,
                               exprAsset.ASSET_OTHER_FLAG,
                               exprAsset.ORDER_SEQ,
                               exprAssetType.ASSET_TYPE_NAME,
                               ASSET_TYPE_ORDER_SEQ = exprAssetType.ORDER_SEQ
                           };

                if (!string.IsNullOrEmpty(assetName))
                    expr = expr.Where(e => e.ASSET_NAME.Contains(assetName));
                if (assetTypeId != null)
                    expr = expr.Where(e => e.ASSET_TYPE.Equals(assetTypeId));

                expr = expr.OrderBy(e => e.ASSET_TYPE_ORDER_SEQ).ThenBy(e => e.ORDER_SEQ);

                pagging.totalRecords = expr.Count();
                int offset = pageIndex * pageSize - pageSize;
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                pagging.rows = expr.Skip(offset).Take(pageSize).Select(e => new
                {
                    e.ASSET_ID,
                    e.ASSET_NAME,
                    e.ASSET_TYPE,
                    e.ASSET_OTHER_FLAG,
                    e.ASSET_TYPE_NAME
                }).ToList();
            };

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public void SubmitDelete(int? assetId)
        {
            if (assetId == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_ASSETs.Where(e => e.ASSET_ID.Equals(assetId)).FirstOrDefault();
                if (expr == null)
                    return;

                expr.ACTIVE = -1;
                db.SubmitChanges();
            };
        }

        [HttpPost]
        public ActionResult SubmitSave(AssetFormMapper model)
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

                var exprAsset = db.T_ASSETs.Where(e => e.ACTIVE.Equals(1) && e.ASSET_ID.Equals(model.AssetId)).FirstOrDefault();
                if (null == exprAsset)
                {
                    exprAsset = new T_ASSET()
                    {
                        ACTIVE = 1
                    };
                    db.T_ASSETs.InsertOnSubmit(exprAsset);
                }

                exprAsset.ASSET_TYPE = model.AssetTypeId.Value;
                exprAsset.ASSET_NAME = model.AssetName;
                exprAsset.ASSET_OTHER_FLAG = model.AssetOtherFlag == null || model.AssetOtherFlag.Value.Equals(0) ? false : true;
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class AssetFormMapper
        {
            public AssetFormMapper() { }

            public short? AssetId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(150, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string AssetName { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? AssetTypeId { get; set; }

            /// <summary>
            /// สินทรัพย์นี้เป็นชื่อ อื่นๆใช่หรือไม่ 1 = ใช่
            /// ใช้เป็นเงื่อนไขแสดง inputbox ให้ชื่อกรอกรายละเอียดเข้าไปเพิ่มเติม
            /// </summary>
            public short? AssetOtherFlag { get; set; }

        }
    }
}