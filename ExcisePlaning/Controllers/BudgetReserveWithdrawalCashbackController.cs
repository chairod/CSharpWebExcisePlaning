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
    // <summary>
    /// เบิกเกินส่งคืน นำเงินส่วนเกินที่เบิกจ่ายไป เพื่อใช้ในกิจกรรมต่างๆ คืนกลับไปยังส่วนกลาง
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReserveWithdrawalCashbackController : Controller
    {
        // GET: BudgetReserveWithdrawalAdjust
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_CASHBACK_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_CASHBACK_MENU;
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
        /// ค้นหารายการเบิกจ่าย จากเลขที่เบิกจ่าย
        /// สามารถมีมากกว่า 1 รายการได้ เพราะ 1 เลขที่เบิกจ่ายสามารถเบิกได้มากกว่า 1 ใบกัน
        /// </summary>
        /// <param name="withdrawalCode"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(string withdrawalCode)
        {
            if (string.IsNullOrEmpty(withdrawalCode))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = (from withdrawal in db.V_GET_BUDGET_RESERVE_WITHDRAWAL_INFORMATIONs.Where(e => e.ACTIVE.Equals(1) && e.WITHDRAWAL_CODE.Equals(withdrawalCode))
                            join reserve in db.V_GET_BUDGET_RESERVE_INFORMATIONs
                            on withdrawal.RESERVE_ID equals reserve.RESERVE_ID
                            select new
                            {
                                reserve.YR,
                                reserve.RESERVE_ID,
                                reserve.PLAN_ID,
                                reserve.PLAN_NAME,
                                reserve.PRODUCE_ID,
                                reserve.PRODUCE_NAME,
                                reserve.ACTIVITY_ID,
                                reserve.ACTIVITY_NAME,
                                reserve.BUDGET_TYPE_ID,
                                reserve.BUDGET_TYPE_NAME,
                                reserve.EXPENSES_GROUP_ID,
                                reserve.EXPENSES_GROUP_NAME,
                                reserve.EXPENSES_ID,
                                reserve.EXPENSES_NAME,
                                reserve.PROJECT_ID,
                                reserve.PROJECT_NAME,

                                withdrawal.DEP_NAME,
                                withdrawal.WITHDRAWAL_CODE,
                                withdrawal.WITHDRAWAL_DATE,
                                withdrawal.WITHDRAWAL_AMOUNT,
                                withdrawal.CREATED_DATETIME,
                                withdrawal.CREATED_NAME,
                                withdrawal.REMARK_TEXT
                            }).ToList();
                return Json(new Dictionary<string, object>(1) { { "withdrawals", expr } }, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// กรณีค้นหา เพื่อจะเบิกเกินส่งคืน แต่เป็นรายการขอเบิกมากกว่า 1 ใบกันจะต้องให้ผู้ใช้งานเลือก ว่าจะเบิกเกินส่งคืนจากรายการใด
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetSelectItemModal()
        {
            return View();
        }


        [HttpPost]
        public ActionResult SubmitSave(WithdrawalReserveBudgetCashbackFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errors", null },
                { "errorText", null }
            };

            // ตรวจสอบความถูกต้องของ ค่าที่ผ่านจากหน้าฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            // ทำรายการเบิกเกินส่งคืน
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var result = BudgetUtils.DoCashbackReserveBudgetWithdrawal(db, model.ReserveId, model.WithdrawalCode, null, model.AdjustmentCode, model.AdjustmentAmount.Value, 2, model.RemarkText, userAuthorizeProfile);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                db.SubmitChanges();
            }


            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class WithdrawalReserveBudgetCashbackFormMapper
        {
            /// <summary>
            /// เลขที่กันเงิน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ReserveId { get; set; }


            /// <summary>
            /// เลขที่เบิกจ่าย ที่ต้องการทำการรายเบิกเกินส่งคืน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string WithdrawalCode { get; set; }

            /// <summary>
            /// เลขที่เบิกเกินส่งคืน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MinLength(10, ErrorMessage = "เลขที่เบิกเกินส่งคืนจะต้องมี 10 หลัก"), MaxLength(10, ErrorMessage = "เลขที่เบิกเกินส่งคืนจะต้องมี 10 หลัก")]
            public string AdjustmentCode { get; set; }

            /// <summary>
            /// จำนวนเงินที่ต้องการเบิกเกินส่งคืน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public decimal? AdjustmentAmount { get; set; }

            /// <summary>
            /// เลขที่กันเงิน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(150, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}