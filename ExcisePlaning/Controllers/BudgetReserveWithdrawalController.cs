using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// เบิกจ่าย เงินที่กันไว้
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReserveWithdrawalController : Controller
    {
        // GET: BudgetReserveWithdrawal
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            // ตรวจสอบหน่วยงานของผู้ทำรายการกันเงิน
            // มีอำนาจตามที่ระบบได้ให้สิทธิ์ไว้หรือไม่
            var appSettings = AppSettingProperty.ParseXml();
            if (appSettings.GetAreaIdsCanReserveBudgetToList().IndexOf(userAuthorizeProfile.AreaId.Value) == -1)
                return RedirectToAction("UnableToReserveBudgetForm", "BudgetReserve");


            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_MENU;
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


        /// <summary>
        /// แบบฟอร์มการปรับปรุงรายการเบิกจ่าย
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalAdjustmentWithdrawalForm()
        {
            return View();
        }

        /// <summary>
        /// ปรับปรุงยอดเงินในรายการเบิกจ่าย
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //[HttpPost]
        //public ActionResult SubmitAdjustmentWithdrawal(WithdrawalAdjustmentFormMapper model)
        //{
        //    Dictionary<string, object> res = new Dictionary<string, object>(2) { { "errors", null }, { "errorText", null } };

        //    // ตรวจสอบความถูกต้องของข้อมูลที่ส่งจากฟอร์ม
        //    var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
        //    if (modelErrors.Any())
        //    {
        //        res["errors"] = modelErrors;
        //        return Json(res, JsonRequestBehavior.DenyGet);
        //    }

        //    using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
        //    {
        //        var exprReserveWithdrawal = db.T_BUDGET_RESERVE_WITHDRAWALs.Where(e => e.RESERVE_ID.Equals(model.ReserveId) && e.SEQ_NO.Equals(model.SeqNo)).FirstOrDefault();
        //        if (exprReserveWithdrawal == null)
        //        {
        //            res["errorText"] = "ไม่พบรายการเบิกจ่ายที่ต้องการปรับปรุงยอด";
        //            return Json(res, JsonRequestBehavior.DenyGet);
        //        }
        //        else if (exprReserveWithdrawal.ACTIVE.Equals(-1))
        //        {
        //            res["errorText"] = "รายการเบิกจ่ายที่ต้องการปรับปรุงยอด ถูกยกเลิกแล้ว";
        //            return Json(res, JsonRequestBehavior.DenyGet);
        //        }

        //        // ปรับปรุงยอดเบิกจ่าย
        //        var oldWithdrawalAmounts = exprReserveWithdrawal.WITHDRAWAL_AMOUNT;
        //        exprReserveWithdrawal.WITHDRAWAL_AMOUNT -= model.AdjustmentAmounts.Value;
        //        if (exprReserveWithdrawal.WITHDRAWAL_AMOUNT.CompareTo(decimal.Zero) == -1)
        //        {
        //            res["errorText"] = "จำนวนเงินเบิกจ่ายหลังจากปรับปรุงยอด มีค่าน้อยกว่าศูนย์ (0) โปรดตรวจสอบ";
        //            return Json(res, JsonRequestBehavior.DenyGet);
        //        }


        //        // คืนจำนวนเงินกลับไปให้ใบกัน
        //        var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.RESERVE_ID.Equals(model.ReserveId)).FirstOrDefault();
        //        if (null == exprReserve)
        //        {
        //            res["errorText"] = "เลขที่ใบกันเงินที่อ้างอิงกับรายการเบิกจ่าย ไม่พบในระบบ";
        //            return Json(res, JsonRequestBehavior.DenyGet);
        //        }
        //        else if (exprReserve.ACTIVE.Equals(-1))
        //        {
        //            res["errorText"] = "เลขที่ใบกันเงินที่อ้างอิงกับรายการเบิกจ่าย ถูกยกเลิกแล้ว";
        //            return Json(res, JsonRequestBehavior.DenyGet);
        //        }
        //        exprReserve.USE_AMOUNT -= model.AdjustmentAmounts.Value;
        //        exprReserve.REMAIN_AMOUNT = exprReserve.RESERVE_BUDGET_AMOUNT - exprReserve.USE_AMOUNT;


        //        // บันทึกประวัติการปรับปรุงรายการเบิกจ่าย
        //        var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
        //        db.T_BUDGET_RESERVE_WITHDRAWAL_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_WITHDRAWAL_HISTORY()
        //        {
        //            RESERVE_ID = exprReserveWithdrawal.RESERVE_ID,
        //            WITHDRAWAL_SEQ_NO = exprReserveWithdrawal.SEQ_NO, // อ้างอิงลำดับในใบเบิกจ่าย
        //            // ลำดับที่ของ ประวัติการปรับปรุงรายการเบิกจ่าย
        //            SEQ_NO = db.T_BUDGET_RESERVE_WITHDRAWAL_HISTORies.Where(e => e.RESERVE_ID.Equals(model.ReserveId)
        //                && e.WITHDRAWAL_SEQ_NO.Equals(model.SeqNo)).Count() + 1,

        //            DEP_ID = exprReserveWithdrawal.DEP_ID,
        //            SUB_DEP_ID = exprReserveWithdrawal.SUB_DEP_ID,

        //            TRAN_TYPE = 2, // ลดยอดจากใบเบิกจ่าย
        //            WITHDRAWAL_CODE = model.ReferDocNo, // เลขที่อ้างอิงการเบิกเกินส่งคืน

        //            CURR_WITHDRAWAL_AMOUNT = oldWithdrawalAmounts,
        //            ADJUSTMENT_AMOUNT = model.AdjustmentAmounts.Value,
        //            CASHBACK_AMOUNT = model.AdjustmentAmounts.Value,
        //            BALANCE_AMOUNT = exprReserveWithdrawal.WITHDRAWAL_AMOUNT,

        //            CREATED_DATETIME = DateTime.Now,
        //            USER_ID = userAuthorizeProfile.EmpId,
        //            REMARK_TEXT = model.RemarkText
        //        });

        //        db.SubmitChanges();
        //    }

        //    return Json(res, JsonRequestBehavior.DenyGet);
        //}

        /// <summary>
        /// ค้นหาข้อมูลในใบกันเงิน เพื่อนำไปทำรายการเบิกจ่าย
        /// </summary>
        /// <param name="reserveId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(string reserveId)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "row", null },
                { "histories", null }
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // รายการกันเงิน
                res["row"] = db.V_GET_BUDGET_RESERVE_INFORMATIONs.Where(e => e.RESERVE_ID.Equals(reserveId) && e.ACTIVE.Equals(1) && e.REMAIN_AMOUNT > 0)
                        .Select(e => new
                        {
                            e.RESERVE_ID,
                            e.DEP_ID,
                            e.DEP_NAME,
                            e.YR,
                            e.RESERVE_BUDGET_AMOUNT,
                            e.USE_AMOUNT,
                            e.REMAIN_AMOUNT,
                            e.CREATED_DATETIME,
                            e.RESERVE_NAME,
                            e.REMARK_TEXT,
                            e.RESERVE_TYPE,
                            e.BUDGET_TYPE,

                            // เบิกจ่ายล่าสุด
                            e.LATEST_WITHDRAWAL_DATETIME,
                            e.LATEST_WITHDRAWAL_NAME,

                            // กลุ่ม คชจ.
                            e.PLAN_NAME,
                            e.PRODUCE_NAME,
                            e.ACTIVITY_NAME,
                            e.BUDGET_TYPE_NAME,
                            e.EXPENSES_GROUP_NAME,
                            e.EXPENSES_NAME,
                            e.PROJECT_NAME
                        }).FirstOrDefault();

                // ประวัติการเบิกจ่าย
                res["histories"] = db.V_GET_BUDGET_RESERVE_WITHDRAWAL_INFORMATIONs.Where(e => e.RESERVE_ID.Equals(reserveId))
                        .OrderBy(e => e.SEQ_NO)
                        .Select(e => new
                        {
                            e.SEQ_NO,
                            e.RESERVE_ID,
                            e.WITHDRAWAL_CODE,
                            e.WITHDRAWAL_AMOUNT,
                            e.WITHDRAWAL_DATE, // วันที่กันเงิน
                            WITHDRAWAL_DATETIME = e.CREATED_DATETIME,
                            WITHDRAWAL_NAME = e.CREATED_NAME,
                            e.REMARK_TEXT,
                            e.ACTIVE
                        }).OrderByDescending(e => e.SEQ_NO).ToList();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public ActionResult SubmitSave(BudgetReserveWithdrawalFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>()
            {
                { "errorText", null },
                { "errors", null }
            };

            // ตรวจสอบความถูกต้องของค่าที่ส่งจากฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            var withdrawalDate = AppUtils.TryValidUserDateStr(model.WithdrawalDateStr);
            if (!modelErrors.ContainsKey("WithdrawalDateStr") && withdrawalDate == DateTime.MinValue)
                modelErrors.Add("WithdrawalDateStr", new ModelValidateErrorProperty("WithdrawalDateStr", new List<string>(1) { "รูปแบบวันที่ไม่ถูกต้อง" }));
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var result = BudgetUtils.DoWithdrawalReserveBudget(db, model.ReserveId, model.WithdrawalCode, null, model.WithdrawalAmounts.Value
                        , withdrawalDate, 1, model.RemarkText, string.Empty, null, userAuthorizeProfile);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                db.SubmitChanges();

                // ย้ายโค้ดไป BudgetUtils.DoWithdrawalReserveBudget
                // เนื่องจากมีการเรียกใช้ตอน ปรับปรุงบัญชี
                //var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(model.ReserveId)).FirstOrDefault();
                //if (null == exprReserve)
                //{
                //    res["errorText"] = "ไม่พบเลขที่ใบกันเงิน ที่ระบุ";
                //    return Json(res, JsonRequestBehavior.DenyGet);
                //}

                //var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                //var appSettings = AppSettingProperty.ParseXml();
                //if (appSettings.GetDepartmentIdsCanReserveBudgetToList().IndexOf(userAuthorizeProfile.DepId) == -1)
                //{
                //    res["errorText"] = "หน่วยงานที่ท่านสังกัดไม่สามารถ เบิกจ่ายเงินงบประมาณได้";
                //    return Json(res, JsonRequestBehavior.DenyGet);
                //}

                //if (!AppUtils.CanChangeDataByIntervalYear(exprReserve.YR, AppUtils.GetCurrYear()))
                //{
                //    res["errorText"] = "เลขที่กันเงินเป็นของปีงบประมาณก่อนหน้า ไม่สามารถทำรายการเบิกจ่ายได้ โปรดตรวจสอบ";
                //    return Json(res, JsonRequestBehavior.DenyGet);
                //}

                //// ไม่สามารถใช้เลขที่เบิกจ่าย ซ้ำกับรายการอื่นๆ
                //if(db.T_BUDGET_RESERVE_WITHDRAWALs.Any(e => e.ACTIVE.Equals(1) && e.WITHDRAWAL_CODE.Equals(model.WithdrawalCode)))
                //{
                //    res["errorText"] = string.Format("เลขที่เบิกจ่าย {0} ถูกนำไปเบิกจ่ายแล้ว ไม่สามารถนำมาใช้ซ้ำได้", model.WithdrawalCode);
                //    return Json(res, JsonRequestBehavior.DenyGet);
                //}

                //// ปรับปรุงยอดคงเหลือ
                //exprReserve.USE_AMOUNT += model.WithdrawalAmounts.Value;
                //exprReserve.REMAIN_AMOUNT = exprReserve.RESERVE_BUDGET_AMOUNT - exprReserve.USE_AMOUNT;
                //exprReserve.LATEST_WITHDRAWAL_DATETIME = DateTime.Now;
                //exprReserve.LATEST_WITHDRAWAL_ID = userAuthorizeProfile.EmpId;
                //if (exprReserve.REMAIN_AMOUNT.CompareTo(decimal.Zero) == -1)
                //{
                //    res["errorText"] = "ยอดเงินไม่เพียงพอให้เบิกจ่าย";
                //    return Json(res, JsonRequestBehavior.DenyGet);
                //}

                //// เก็บประวัติการเบิกจ่าย ของใบกันเงิน
                //short seqNo = Convert.ToInt16(db.T_BUDGET_RESERVE_WITHDRAWALs.Where(e => e.RESERVE_ID.Equals(model.ReserveId)).Count());
                //seqNo++;
                //db.T_BUDGET_RESERVE_WITHDRAWALs.InsertOnSubmit(new T_BUDGET_RESERVE_WITHDRAWAL()
                //{
                //    SEQ_NO = seqNo,
                //    RESERVE_ID = model.ReserveId,
                //    WITHDRAWAL_CODE = model.WithdrawalCode,
                //    WITHDRAWAL_AMOUNT = model.WithdrawalAmounts.Value,
                //    DEP_ID = exprReserve.DEP_ID,
                //    SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                //    MN = Convert.ToInt16(DateTime.Now.Month),
                //    YR = exprReserve.YR,
                //    REMARK_TEXT = model.RemarkText,
                //    CREATED_DATETIME = DateTime.Now,
                //    USER_ID = userAuthorizeProfile.EmpId,
                //    ACTIVE = 1
                //});

                //// เก็บประวัติของรายการเบิกจ่าย
                //db.T_BUDGET_RESERVE_WITHDRAWAL_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_WITHDRAWAL_HISTORY()
                //{
                //    RESERVE_ID = model.ReserveId,
                //    DEP_ID = exprReserve.DEP_ID,
                //    SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                //    WITHDRAWAL_SEQ_NO = seqNo,
                //    SEQ_NO = 1,
                //    TRAN_TYPE = 1, // เบิกจ่าย
                //    WITHDRAWAL_CODE = model.WithdrawalCode,
                //    CURR_WITHDRAWAL_AMOUNT = model.WithdrawalAmounts.Value,
                //    ADJUSTMENT_AMOUNT = decimal.Zero,
                //    CASHBACK_AMOUNT = decimal.Zero,
                //    BALANCE_AMOUNT = model.WithdrawalAmounts.Value,
                //    CREATED_DATETIME = DateTime.Now,
                //    REMARK_TEXT = model.RemarkText,
                //    USER_ID = userAuthorizeProfile.EmpId
                //});

                //db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class BudgetReserveWithdrawalFormMapper
        {
            /// <summary>
            /// เลขที่ใบกันเงิน
            /// </summary>
            public string ReserveId { get; set; }

            /// <summary>
            /// เลขที่ขอเบิก
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MinLength(10, ErrorMessage = "เลขที่ขอเบิกจะต้องมี {1} หลัก"), MaxLength(10, ErrorMessage = "เลขที่ขอเบิกจะต้องมี {1} หลัก")]
            public string WithdrawalCode { get; set; }

            /// <summary>
            /// วันที่ขอเบิกจ่าย dd/MM/yyyy (ปี พ.ศ.)
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string WithdrawalDateStr { get; set; }

            /// <summary>
            /// จำนวนเงินที่ต้องขอเบิกจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(0.01, 99999999999, ErrorMessage = "ค่าอยู่ระหว่าง {1} - {2}")]
            public decimal? WithdrawalAmounts { get; set; }


            [MaxLength(300, ErrorMessage = "ข้อความไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }


        public class WithdrawalAdjustmentFormMapper
        {
            /// <summary>
            /// เลขที่ใบกันเงิน
            /// </summary>
            public string ReserveId { get; set; }

            /// <summary>
            /// ลำดับรายการเบิกจ่าย
            /// </summary>
            public int SeqNo { get; set; }

            /// <summary>
            /// เลขที่อ้างอิงการเบิกเกินส่งคืน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(50, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ReferDocNo { get; set; }

            /// <summary>
            /// จำนวนเงินที่ต้องการปรับปรุง
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(decimal), "0", "999999999999999999.99", ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public decimal? AdjustmentAmounts { get; set; }

            /// <summary>
            /// หมายเหตุอื่นๆ
            /// </summary>
            [MaxLength(120, ErrorMessage = "ข้อความไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}