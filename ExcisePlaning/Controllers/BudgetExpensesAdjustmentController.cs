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
    /// โอนเปลี่ยนแปลงงบประมาณของรายการค่าใช้จ่าย หรือ โครงการ ไปยัง ค่าใช้จ่ายหรือโครงการอื่นๆ
    /// จะย้าย เงินประจำงวด และ งบประมาณที่รัฐจัดสรร ไปพร้อมกัน
    /// [เคส 1] โอนจาก คชจ. ไปยัง คชจ.
    /// [เคส 2] โอนจาก คชจ. ไปยัง โครงการ
    /// [เคส 3] โอนจาก โครงการ ไปยัง โครงการ
    /// [เคส 4] โอนจาก โครงการ ไปยัง คชจ.</para>
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetExpensesAdjustmentController : Controller
    {
        // GET: BudgetExpensesAdjustment
        public ActionResult GetForm(string pageType)
        {
            string currentMenuConst = AppConfigConst.MENU_CONST_BUDGET_EXPENSES_ADJUSTMENT_MENU;
            if ("off_budget".Equals(pageType))
                currentMenuConst = AppConfigConst.MENU_CONST_OFF_BUDGET_EXPENSES_ADJUSTMENT_MENU;

            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(currentMenuConst);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กรณีไม่ผ่านค่า type เข้าไปให้เด้งกลับไปหน้า Dashboard/หน้าแรก
            List<string> acceptPageTypes = new List<string>() { "budget", "off_budget" };
            if (string.IsNullOrEmpty(pageType) || acceptPageTypes.IndexOf(pageType) == -1)
                return RedirectToAction(menuIndexItem.ActionName, menuIndexItem.RouteName);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = currentMenuConst;
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
                ActionName = menuItem.ActionName,
                QueryString = menuItem.QueryString
            });
            ViewBag.Breadcrumps = breadcrumps;

            ViewBag.FiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.PageType = pageType;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new PlanShortFieldProperty()
                    {
                        PLAN_ID = e.PLAN_ID,
                        PLAN_NAME = e.PLAN_NAME
                    }).ToList();
                // งบรายจ่าย
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ)
                    .Select(e => new BudgetTypeShortFieldProperty()
                    {
                        BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                        BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                    }).ToList();
            }

            return View();
        }


        [HttpPost]
        public ActionResult SubmitSave(BudgetExpensesAdjustmentFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "errors", null }, { "errorText", null } };

            // ตรวจสอบความถูกต้องของข้อมูลที่ส่งจากหน้าฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (model.RequiredFromProjectId == 1 && null == model.FromProjectId)
                modelErrors.Add("FromProjectId", new ModelValidateErrorProperty("FromProjectId", new List<string>() { "ระบุค่านี้ก่อน" }));
            if (model.RequiredToProjectId == 1 && null == model.ToProjectId)
                modelErrors.Add("ToProjectId", new ModelValidateErrorProperty("ToProjectId", new List<string>() { "ระบุค่านี้ก่อน" }));
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                DateTime tranferDate = AppUtils.TryValidUserDateStr(model.TranferDateStr);

                var result = BudgetUtils.DoTranferBudgetExpensesToOther(db, model.FiscalYear, model.BudgetType
                    // รายการต้นทางที่ต้องการโอน
                    , model.FromPlanId, model.FromProduceId, model.FromActivityId
                    , model.FromBudgetTypeId.Value, model.FromExpensesGroupId.Value
                    , model.FromExpensesId.Value, model.FromProjectId

                    // รายการปลายทาง
                    , model.ToPlanId, model.ToProduceId, model.ToActivityId
                    , model.ToBudgetTypeId.Value, model.ToExpensesGroupId.Value
                    , model.ToExpensesId.Value, model.ToProjectId

                    // รายละเอียดการโอน
                    , model.TranferAmount.Value, tranferDate
                    , model.ReferCode, model.RemarkText, userAuthorizeProfile);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetExpensesAdjustmentFormMapper
        {
            /// <summary>
            /// ปีงบประมาณที่โอนเปลี่ยนแปลง
            /// </summary>
            public short FiscalYear { get; set; }

            /// <summary>
            /// ประเภทงบ 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ
            /// </summary>
            public short BudgetType { get; set; }

            /// <summary>
            /// โอนจาก แผนงาน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? FromPlanId { get; set; }

            /// <summary>
            /// โอนจาก ผลผลิต
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? FromProduceId { get; set; }


            /// <summary>
            /// โอนจาก กิจกรรม
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? FromActivityId { get; set; }

            /// <summary>
            /// โอนจาก งบรายจ่าย
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? FromBudgetTypeId { get; set; }

            /// <summary>
            /// โอนจาก หมวดค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? FromExpensesGroupId { get; set; }


            /// <summary>
            /// โอนจาก ค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? FromExpensesId { get; set; }

            /// <summary>
            /// โอนจาก โครงการ
            /// </summary>
            public int? FromProjectId { get; set; }

            /// <summary>
            /// ข้อมูลการโอนเปลี่ยนแปลงจำเป็นต้องระบุ โครงการหรือไม่
            /// 1 = จำเป็น
            /// </summary>
            public short RequiredFromProjectId { get; set; }


            /// <summary>
            /// แผนงาน ที่รับโอน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? ToPlanId { get; set; }

            /// <summary>
            /// ผลผลิต ที่รับโอน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? ToProduceId { get; set; }


            /// <summary>
            /// กิจกรรม ที่รับโอน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? ToActivityId { get; set; }

            /// <summary>
            /// งบรายจ่าย ที่รับโอน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? ToBudgetTypeId { get; set; }


            /// <summary>
            /// หมวดค่าใช้จ่าย ที่รับโอน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? ToExpensesGroupId { get; set; }

            /// <summary>
            /// ค่าใช้จ่าย ที่รับโอน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? ToExpensesId { get; set; }

            /// <summary>
            /// โครงการ ที่รับโอน
            /// </summary>
            public int? ToProjectId { get; set; }

            /// <summary>
            /// บังคับให้ระบุ โครงการปลายทางหรือไม่
            /// 1 = บังคับ
            /// </summary>
            public short RequiredToProjectId { get; set; }


            /// <summary>
            /// จำนวนเงินที่ขอโอน เปลี่ยนแปลง
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "ค่าอยู่ระหว่าง {1} - {2}")]
            public decimal? TranferAmount { get; set; }

            /// <summary>
            /// วันที่ขอโอนเปลี่ยนแปลง
            /// </summary>
            public string TranferDateStr { get; set; }

            /// <summary>
            /// เลขที่อ้างอิง การโอนเปลี่ยนแปลง
            /// </summary>
            [MaxLength(20, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ReferCode { get; set; }

            /// <summary>
            /// หมายเหตุ
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(150, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}