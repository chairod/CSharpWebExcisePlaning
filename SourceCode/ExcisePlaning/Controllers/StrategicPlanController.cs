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
    public class StrategicPlanController : Controller
    {
        // GET: StrategicPlan
        public ActionResult StrategicPlanForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_STRATEGIC_PLAN);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_STRATEGIC_PLAN;
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
        public ActionResult RetrieveForm()
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) {
                { "Visionrows", null },
                { "Missionrows", null },
                { "Popularityrows", null },
                { "Policyrows", null }
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                var expr = db.T_VISIONs.AsQueryable();
                var finalExpr = expr.ToList();
                res["Visionrows"] = finalExpr.OrderBy(e => e.VISION_ID).ToList();

                var Missionexpr = db.T_MISSIONs.AsQueryable();
                var finalMission = Missionexpr.ToList();
                res["Missionrows"] = finalMission.OrderBy(e => e.MISSION_ID).ToList();

                var Popularityexpr = db.T_POPULARITies.AsQueryable();
                var finalPopularity = Popularityexpr.ToList();
                res["Popularityrows"] = finalPopularity.OrderBy(e => e.POPULARITY_ID).ToList();

                var Policyexpr = db.T_POLICies.AsQueryable();
                var finalPolicyexpr = Policyexpr.ToList();
                res["Policyrows"] = finalPolicyexpr.OrderBy(e => e.POLICY_ID).ToList();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        //Vision
        [HttpGet]
        public ActionResult GetVisionModalResource()
        {
            return View("_VisionModel");
        }
        public class VisionFormMapper
        {
            public VisionFormMapper() { }

            public short? VisionID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string IndicatorsName { get; set; }
            public decimal? TargetValue { get; set; }

        }
        

        [HttpPost]
        public ActionResult SubmitSaveVision(VisionFormMapper model)
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

                T_VISION Expr = null;

                Expr = new T_VISION()
                {
                    INDICATORS_NAME = model.IndicatorsName,
                    TARGET_VALUE = model.TargetValue.Value
                };

                db.T_VISIONs.InsertOnSubmit(Expr);

                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("VisionID:int?")]
        public void SubmitDeleteVision(int? VisionID)
        {
            if (VisionID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_VISIONs.Where(e => e.VISION_ID.Equals(VisionID.Value)).FirstOrDefault();
                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_VISIONs.DeleteOnSubmit(Expr);
                db.SubmitChanges();

            };
        }



        //------------------------------ Mission---------------------------
        [HttpGet]
        public ActionResult GetMissionModalResource()
        {
            return View("_MissionModel");
        }
        public class MissionFormMapper
        {
            public MissionFormMapper() { }

            public short? MissionID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string MissionName { get; set; }
           

        }

        public ActionResult SubmitSaveMission(MissionFormMapper model)
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

                T_MISSION Expr = null;

                Expr = new T_MISSION()
                {
                    MISSION_NAME = model.MissionName
                };

                db.T_MISSIONs.InsertOnSubmit(Expr);

                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("MissionID:int?")]
        public void SubmitDeleteMission(int? MissionID)
        {
            if (MissionID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_MISSIONs.Where(e => e.MISSION_ID.Equals(MissionID.Value)).FirstOrDefault();
                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_MISSIONs.DeleteOnSubmit(Expr);
                db.SubmitChanges();

            };
        }

        //------------------------------ Popularity---------------------------
        [HttpGet]
        public ActionResult GetPopularityModalResource()
        {
            return View("_PopularityModel");
        }
        public class PopularityFormMapper
        {
            public PopularityFormMapper() { }

            public short? PopularityID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string PopularityName { get; set; }


        }

        public ActionResult SubmitSavePopularity(PopularityFormMapper model)
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

                T_POPULARITY Expr = null;

                Expr = new T_POPULARITY()
                {
                    POPULARITY_NAME = model.PopularityName
                };

                db.T_POPULARITies.InsertOnSubmit(Expr);

                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("PopularityID:int?")]
        public void SubmitDeletePopularity(int? PopularityID)
        {
            if (PopularityID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_POPULARITies.Where(e => e.POPULARITY_ID.Equals(PopularityID.Value)).FirstOrDefault();
                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_POPULARITies.DeleteOnSubmit(Expr);
                db.SubmitChanges();

            };
        }

        //------------------------------ Policy---------------------------
        [HttpGet]
        public ActionResult GetPolicyModalResource()
        {
            return View("_PolicyModel");
        }
        public class PolicyFormMapper
        {
            public PolicyFormMapper() { }

            public short? PolicyID { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string PolicyName { get; set; }


        }

        public ActionResult SubmitSavePolicy(PolicyFormMapper model)
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

                T_POLICY Expr = null;

                Expr = new T_POLICY()
                {
                    POLICY_NAME = model.PolicyName
                };

                db.T_POLICies.InsertOnSubmit(Expr);

                db.SubmitChanges();


            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost, Route("PolicyID:int?")]
        public void SubmitDeletePolicy(int? PolicyID)
        {
            if (PolicyID == null)
                return;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var Expr = db.T_POLICies.Where(e => e.POLICY_ID.Equals(PolicyID.Value)).FirstOrDefault();
                if (Expr == null)
                    return;

                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                db.T_POLICies.DeleteOnSubmit(Expr);
                db.SubmitChanges();

            };
        }
    }
}