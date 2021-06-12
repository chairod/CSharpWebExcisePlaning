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
    public class GraphAnnualBudgetResultController : Controller
    {
        // GET: GraphAnnualBudget
        public ActionResult GraphAnnualBudgetResultForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_GRAPH_ANNUAL_BUDGET_RESULT);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_GRAPH_ANNUAL_BUDGET_RESULT;
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
                ViewBag.PlanConfig = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.PLAN_NAME)
                    .Select(e => new PlanShortFieldProperty()
                    {
                        PLAN_ID = e.PLAN_ID,
                        PLAN_NAME = e.PLAN_NAME
                    }).ToList();
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.product = db.T_PRODUCE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.PRODUCE_NAME)
                    .Select(e => new ProduceShortFieldProperty()
                    {
                        PRODUCE_ID = e.PRODUCE_ID,
                        PRODUCE_NAME = e.PRODUCE_NAME
                    }).ToList();
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Activity = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ACTIVITY_NAME)
                    .Select(e => new ActivityShortFieldProperty()
                    {
                        ACTIVITY_ID = e.ACTIVITY_ID,
                        ACTIVITY_NAME = e.ACTIVITY_NAME
                    }).ToList();
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Budget = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.BUDGET_TYPE_NAME)
                    .Select(e => new BudgetTypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();
            }

            ViewBag.FiscalYear = userAuthorizeProfile.DefaultFiscalYear;

            return View();
        }

        [HttpPost, Route("Yr:int?,PlanID:int?,ProductID:int?,ActivityID:int?,BudgetID :int?,Strategic :int?")]
        public ActionResult ShowData(int? Yr, int? PlanID, int? ProductID, int? ActivityID, int? BudgetID, int? Strategic)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) {
                { "chartData1", null },
                { "chartData2",null},
                { "category",null}
            };


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {

                if (Yr == null)
                    Yr = 0;
                else
                    Yr = Yr - 543;
                if (PlanID == null)
                    PlanID = 0;
                if (ProductID == null)
                    ProductID = 0;
                if (ActivityID == null)
                    ActivityID = 0;
                if (BudgetID == null)
                    BudgetID = 0;
                if (Strategic == null)
                    Strategic = 0;
               
                var exprAllocate = db.pro_GRAPH_ANNUAL_BUDGET(Yr.Value,PlanID.Value,ProductID.Value, ActivityID.Value, BudgetID.Value, Strategic.Value).AsQueryable();              

                var exprUsed = db.pro_GRAPH_ANNUAL_BUDGET_RESULT(Yr.Value, PlanID.Value, ProductID.Value, ActivityID.Value, BudgetID.Value, Strategic.Value).AsQueryable();
                var exprcategory = db.pro_GRAPH_ANNUAL_BUDGET(Yr.Value, PlanID.Value, ProductID.Value, ActivityID.Value, BudgetID.Value, Strategic.Value).AsQueryable();

                var finalExprAllocate = exprAllocate.Select(e => new
                {
                    value = e.value
                }).ToList();

                var finalExprUsed = exprUsed.Select(e => new
                {
                    value = e.value
                }).ToList();

                var categoryExpr = exprcategory.Select(e => new
                {
                    label = e.label
                }).ToList();



                res["chartDataAllocate"] = finalExprAllocate.ToList();
                res["chartDataUsed"] = finalExprUsed.ToList();
                res["category"] = categoryExpr.ToList();




            };


            return Json(res, JsonRequestBehavior.AllowGet);

           // return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class GrapgFormMapper
        {
            public GrapgFormMapper() { }

            public short? Yr { get; set; }
            public short? PlanID { get; set; }
            public short? ProductID { get; set; }
            public short? ActivityID { get; set; }
            public short? BudgetID { get; set; }

            public short? Strategic { get; set; }
        }
    }
}