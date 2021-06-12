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
    /// จัดเก็บรายได้เงินนอกงบประมาณ ในภาพรวม
    /// โดยไม่แยกลงแต่ละรายการค่าใช้จ่าย
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class OffBudgetIncomeController : Controller
    {
        // GET: OffBudgetIncome
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_OFF_BUDGET_INCOME);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_OFF_BUDGET_INCOME;
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
            return View();
        }

        [HttpPost]
        public ActionResult Retrieve(int fiscalYear)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return Json(db.T_OFF_BUDGET_MASTER_INCOME_HISTORies.Where(e => e.YR.Equals(fiscalYear))
                       .Select(e => new
                       {
                           e.YR, // PK
                           e.SEQ_NO, // PK
                           e.PERIOD_MN,
                           e.PERIOD_YR,
                           e.BUDGET_AMOUNT,
                           e.REMARK_TEXT,
                           e.CREATED_DATETIME,
                           PERSON = db.T_PERSONNELs.Where(person => person.PERSON_ID.Equals(e.USER_ID))
                            .Select(person => new
                            {
                                person.PREFIX_NAME,
                                person.FIRST_NAME,
                                person.LAST_NAME
                            }).FirstOrDefault(),
                           e.DELETED_DATETIME,
                           PERSON_DELETE = db.T_PERSONNELs.Where(person => person.PERSON_ID.Equals(e.DELETED_ID))
                            .Select(person => new
                            {
                                person.PREFIX_NAME,
                                person.FIRST_NAME,
                                person.LAST_NAME
                            }).FirstOrDefault(),
                           e.ACTIVE
                       }).OrderBy(e => e.SEQ_NO).ToList(), JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ยกเลิกรายการ รับเงินประจำงวด
        /// 1. ยอดคงเหลือ ต้องไม่น้อยกว่า ยอดใช้จ่าย
        /// </summary>
        /// <param name="seqNo"></param>
        /// <param name="fiscalYear"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitReject(int seqNo, int fiscalYear)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) { { "errorText", null } };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprBudgetOffIncome = db.T_OFF_BUDGET_MASTER_INCOME_HISTORies.Where(e => e.YR.Equals(fiscalYear) && e.SEQ_NO.Equals(seqNo)).FirstOrDefault();
                if (null == exprBudgetOffIncome)
                {
                    res["errorText"] = "ไม่พบประวัติการรับเงินประจำงวด ที่ต้องการยกเลิก";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (exprBudgetOffIncome.ACTIVE.Equals(-1))
                {
                    res["errorText"] = string.Format("รายการรับเงินประจำงวด ยกเลิกไปแล้ว เมื่อ {0}", exprBudgetOffIncome.DELETED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear)).FirstOrDefault();
                if (null == exprBudgetMas)
                {
                    res["errorText"] = "งบประมาณประจำปียังไม่มีการจัดสรร";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // เงินนอกงบประมาณคงเหลือสุทธิ ต้องไม่น้องกว่ายอดใช้จ่าย
                exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT -= exprBudgetOffIncome.BUDGET_AMOUNT;
                exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                if (exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                {
                    res["errorText"] = "หลังจากยกเลิกรายการรับเงินประจำงวด เงินนอกงบประมาณคงเหลือสุทธิ น้อยกว่ายอดใช้จ่าย";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ยกเลิกรายการรับเงินนอก
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                exprBudgetOffIncome.ACTIVE= -1;
                exprBudgetOffIncome.DELETED_DATETIME = DateTime.Now;
                exprBudgetOffIncome.DELETED_ID = userAuthorizeProfile.EmpId;


                // บันทึกการเปลี่ยนแปลง
                db.SubmitChanges();
            }
            return Json(res, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public ActionResult SubmitSave(OffBudgetIncomeFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errors", null },
                { "errorText", null }
            };

            // ตรวจสอบค่าจากหน้าฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // จัดเก็บรายได้ปีงบประมาณก่อนหน้าไม่ได้
            if (model.FiscalYear.CompareTo(AppUtils.GetCurrYear()) == -1)
            {
                res["errorText"] = "ไม่สามารถบันทึกจัดเก็บรายได้ของปีงบประมาณย้อนหลังได้";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ระบุค่างวดที่จัดเก็บรายได้ถูกต้องหรือไม่
            string[] incomePeriodParts = model.ReceivePeriod.Split(new char[] { '/' });
            if (incomePeriodParts.Length != 2)
            {
                res["errorText"] = "รูปแบบงวดที่จัดเก็บรายได้ ไม่ถูกต้อง";
                return Json(res, JsonRequestBehavior.DenyGet);
            }
            short incomeYear = Convert.ToInt16(incomePeriodParts[1])
                , incomeMonth = Convert.ToInt16(incomePeriodParts[0]);
            if (incomeMonth > 12)
            {
                res["errorText"] = "รูปแบบงวดที่จัดเก็บรายได้ เดือนไม่ถูกต้อง";
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(model.FiscalYear)).FirstOrDefault();
                if (null == exprBudgetMas)
                {
                    res["errorText"] = "โปรดจัดการเงินงบประมาณก่อน (จัดการเงินงบประมาณ -> เงินงบประมาณ, เงินนอกงบประมาณ)";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // เพิ่มรายได้ที่จัดเก็บมา ลงเงินนอกงบประมาณ
                exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT += model.ReceiveAmounts.Value;
                exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                exprBudgetMas.LATEST_INCOME_OFF_BUDGET = DateTime.Now;

                // งบประมาณ (รับจากรัฐบาล) ต้องไม่น้อยกว่า เงินประจำงวดสะสม
                if (exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT.CompareTo(exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT) == -1)
                {
                    res["errorText"] = "เงินงบประมาณ น้อยกว่า เงินประจำนวนสะสม โปรดตรวจสอบ";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }


                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                // เก็บประวัติ
                int seqNo = db.T_OFF_BUDGET_MASTER_INCOME_HISTORies.Where(e => e.YR.Equals(model.FiscalYear)).Count() + 1;
                db.T_OFF_BUDGET_MASTER_INCOME_HISTORies.InsertOnSubmit(new T_OFF_BUDGET_MASTER_INCOME_HISTORY()
                {
                    YR = Convert.ToInt16(model.FiscalYear),
                    SEQ_NO = Convert.ToInt16(seqNo),
                    PERIOD_MN = incomeMonth,
                    PERIOD_YR = incomeYear,
                    BUDGET_AMOUNT = model.ReceiveAmounts.Value,
                    REMARK_TEXT = model.RemarkText,
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId,
                    ACTIVE = 1
                });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class OffBudgetIncomeFormMapper
        {
            /// <summary>
            /// จัดเก็บรายได้ของปีงบประมาณใด
            /// </summary>
            public int FiscalYear { get; set; }

            /// <summary>
            /// งวดที่จัดเก็บรายได้
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ReceivePeriod { get; set; }

            /// <summary>
            /// จำนวนเงิน ที่จัดเก็บรายได้ (บาท)
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public decimal? ReceiveAmounts { get; set; }

            /// <summary>
            /// รายละเอียดเพิ่มเติม
            /// </summary>
            [MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}