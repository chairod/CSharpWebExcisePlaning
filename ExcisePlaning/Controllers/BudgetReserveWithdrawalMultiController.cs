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
    /// เบิกจ่ายรายการกันเงิน ซึ่งเลขที่ขอเบิก 1 ใบสามารถเบิกจ่ายได้มากกว่า 1 ใบกัน
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReserveWithdrawalMultiController : Controller
    {
        // GET: BudgetReserveWithdrawalMulti
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            // ตรวจสอบหน่วยงานของผู้ทำรายการกันเงิน
            // มีอำนาจตามที่ระบบได้ให้สิทธิ์ไว้หรือไม่
            var appSettings = AppSettingProperty.ParseXml();
            if (appSettings.GetAreaIdsCanReserveBudgetToList().IndexOf(userAuthorizeProfile.AreaId.Value) == -1)
                return RedirectToAction("UnableToReserveBudgetForm", "BudgetReserve");


            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_MULTI_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_MULTI_MENU;
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

            ViewBag.FiscalYear = AppUtils.GetCurrYear();
            ViewBag.SubDepId = userAuthorizeProfile.SubDepId;
            ViewBag.EmpFullName = userAuthorizeProfile.EmpFullname;

            return View();
        }


        [HttpPost]
        public ActionResult Retrieve(string withdrawalCode)
        {
            if (string.IsNullOrEmpty(withdrawalCode))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_BUDGET_RESERVE_WITHDRAWAL_INFORMATIONs.Where(e => e.ACTIVE.Equals(1) && e.WITHDRAWAL_CODE.Equals(withdrawalCode))
                        .OrderBy(e => e.WITHDRAWAL_DATE)
                        .Select(e => new
                        {
                            e.RESERVE_ID,
                            e.BUDGET_TYPE,
                            e.DEP_NAME,
                            e.WITHDRAWAL_DATE,
                            e.CREATED_DATETIME,
                            WITHDRAWAL_NAME = e.CREATED_NAME,
                            e.WITHDRAWAL_AMOUNT
                        }).ToList();
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        [HttpPost]
        public ActionResult SubmitSave(BudgetReserveWithdrawalMultiFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "errors", null }, { "errorText", null } };

            var withdrawalDate = AppUtils.TryValidUserDateStr(model.WithdrawalDateStr);

            // ตรวจสอบความถูกต้องของข้อมูลจากฟอร์ม
            var modelErrors = new Dictionary<string, object>();
            modelErrors = ModelValidateErrorProperty.TryValidate(ModelState).ToDictionary(t => t.Key, t => (object)t.Value);
            if (!modelErrors.ContainsKey("WithdrawalDateStr") && withdrawalDate == DateTime.MinValue)
                modelErrors.Add("WithdrawalDateStr", new ModelValidateErrorProperty("WithdrawalDateStr", new List<string>() { "รูปแบบวันที่ไม่ถูกต้อง" }));
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            var userAuthrizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            var itemErrors = new List<Dictionary<string, ModelValidateErrorProperty>>();
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                foreach (var item in model.Items)
                {
                    if (item.WithdrawalAmount == null)
                    {
                        var itemError = new Dictionary<string, ModelValidateErrorProperty>(1) { { "WithdrawalAmount", new ModelValidateErrorProperty("WithdrawalAmount", new List<string>() { "โปรดระบุจำนวนที่ขอเบิกก่อน" }) } };
                        itemErrors.Add(itemError);
                        continue;
                    }

                    var result = BudgetUtils.DoWithdrawalReserveBudget(db, item.RESERVE_ID, model.WithdrawalCode, string.Empty, item.WithdrawalAmount.Value, withdrawalDate, 1, model.RemarkText, string.Empty, null, userAuthrizeProfile);
                    if (result.Completed)
                        itemErrors.Add(null);
                    else
                    {
                        var itemError = new Dictionary<string, ModelValidateErrorProperty>(1) { { "WithdrawalAmount", new ModelValidateErrorProperty("WithdrawalAmount", new List<string>() { result.CauseErrorMessage }) } };
                        itemErrors.Add(itemError);
                    }
                }


                // ตรวจสอบแต่ละเลขที่กันเงิน สามารถเบิกจ่ายได้สมบูรณ์ หรือไม่
                if(itemErrors.Any(x => null != x))
                {
                    modelErrors.Add("Items", itemErrors);
                    res["errors"] = modelErrors;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetReserveWithdrawalMultiFormMapper
        {
            /// <summary>
            /// เลขที่ขอเบิกจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(10, ErrorMessage = "ความยาวไม่เกิน 10 ตัวอักษร"), MinLength(10, ErrorMessage = "ความยาวอย่างน้อย 10 ตัวอักษร")]
            public string WithdrawalCode { get; set; }


            /// <summary>
            /// วันที่เบิกจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string WithdrawalDateStr { get; set; }

            /// <summary>
            /// รายละเอียดอื่นๆ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(300, ErrorMessage = "ความยาวไม่เกิน {2} ตัวอักษร")]
            public string RemarkText { get; set; }

            /// <summary>
            /// เลขที่กันเงินที่ต้องการเบิกจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุรายการกันเงินอย่างน้อย 1 รายการ")]
            public List<BudgetReserveWithdrawalItemProperty> Items { get; set; }
        }

        public class BudgetReserveWithdrawalItemProperty
        {
            /// <summary>
            /// เลขที่กันเงิน
            /// </summary>
            public string RESERVE_ID { get; set; }

            /// <summary>
            /// จำนวนเงินที่ต้องการเบิกจ่ายของแต่ละใบกัน
            /// </summary>
            public decimal? WithdrawalAmount { get; set; }
        }
    }
}