using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    // <summary>
    /// การปรับปรุงบัญชี
    /// ปรับปรุงข้อมูลการเบิกจ่าย เช่น ปรับเปลี่ยนประเภทงบการเบิกจ่าย (เงินงบ เงินนอกงบ) ปรับเปลี่ยนกลุ่มค่าใช้จ่าย (แผนงาน ผลผลิต กิจกรรม ... โครงการ) เป็นต้น
    /// </summary>
    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3")]
    public class BudgetReserveWithdrawalAdjustmentController : Controller
    {
        // GET: BudgetReserveWithdrawalAdjustment
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_ADJUSTMENT_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_ADJUSTMENT_MENU;
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
                var appSettings = AppSettingProperty.ParseXml();
                var areaIdsCanReserveBudget = appSettings.GetAreaIdsCanReserveBudgetToList();

                // หน่วยงานภายในกรมสรรพสามิต ที่จะกันเงิน
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && areaIdsCanReserveBudget.Contains(e.AREA_ID.Value))
                    .OrderBy(e => e.SORT_INDEX)
                    .Select(e => new DepartmentShortFieldProperty()
                    {
                        DEP_ID = e.DEP_ID,
                        DEP_NAME = e.DEP_NAME
                    }).ToList();
                // แผนงาน
                ViewBag.Plans = db.T_PLAN_CONFIGUREs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new PlanShortFieldProperty()
                {
                    PLAN_ID = e.PLAN_ID,
                    PLAN_NAME = e.PLAN_NAME
                }).ToList();
                // งบรายจ่าย
                ViewBag.BudgetTypes = db.T_BUDGET_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.ORDER_SEQ).Select(e => new BudgetTypeShortFieldProperty()
                {
                    BUDGET_TYPE_ID = e.BUDGET_TYPE_ID,
                    BUDGET_TYPE_NAME = e.BUDGET_TYPE_NAME
                }).ToList();
            }

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
                                reserve.BUDGET_TYPE,
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

                                withdrawal.DEP_ID,
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


        [HttpPost]
        public ActionResult SubmitSave(AdjustmentBudgetReserveWithdrawalFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errorText", null },
                { "errors", null }
            };

            // ตรวจสอบค่าที่รับจากฟอร์ม ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (model.ProjectIdRequired && null == model.PROJECT_ID)
                modelErrors.Add("PROJECT_ID", new ModelValidateErrorProperty("PROJECT_ID", new List<string>() { "โปรดระบุค่านี้ก่อน" }));
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ตรวจสอบการใช้เลขที่เอกสารอ้างอิง รายการปรับปรุงบัญชี ซ้ำ
                if (db.T_BUDGET_RESERVE_WITHDRAWALs.Any(e => e.ACTIVE.Equals(1) && e.REFER_DOC_CODE.Equals(model.ReferDocNo)))
                {
                    res["errorText"] = string.Format("เลขที่เอกสารการปรับปรุงบัญชี {0} ถูกนำไปใช้อ้างอิงในรายการปรับปรุงอื่นแล้ว", model.ReferDocNo);
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var exprWithdrawal = (from withdrawal in db.V_GET_BUDGET_RESERVE_WITHDRAWAL_INFORMATIONs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(model.RESERVE_ID) && e.WITHDRAWAL_CODE.Equals(model.WITHDRAWAL_CODE))
                                      join reserve in db.V_GET_BUDGET_RESERVE_INFORMATIONs
                                      on withdrawal.RESERVE_ID equals reserve.RESERVE_ID
                                      select new
                                      {
                                          reserve.YR,
                                          reserve.RESERVE_ID,
                                          reserve.RESERVE_TYPE,
                                          reserve.RESERVE_DATE,
                                          reserve.BUDGET_TYPE,
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

                                          withdrawal.DEP_ID,
                                          withdrawal.DEP_NAME,
                                          withdrawal.SEQ_NO,
                                          withdrawal.WITHDRAWAL_CODE,
                                          withdrawal.WITHDRAWAL_DATE,
                                          withdrawal.WITHDRAWAL_AMOUNT,
                                          withdrawal.CREATED_DATETIME,
                                          withdrawal.CREATED_NAME,
                                          withdrawal.REMARK_TEXT
                                      }).FirstOrDefault();
                if (null == exprWithdrawal)
                {
                    res["errorText"] = string.Format("ไม่พบเลขที่เบิกจ่าย {0} หรือถูกยกเลิกไปแล้ว", model.WITHDRAWAL_CODE); ;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบการเปลี่ยนแปลงข้อมูล
                StringBuilder sbFormData = new StringBuilder();
                sbFormData.Append(model.DEP_ID).Append("_")
                    .Append(model.BUDGET_TYPE).Append("_")
                    .Append(model.PLAN_ID).Append("_")
                    .Append(model.PRODUCE_ID).Append("_")
                    .Append(model.ACTIVITY_ID).Append("_")
                    .Append(model.BUDGET_TYPE_ID).Append("_")
                    .Append(model.EXPENSES_GROUP_ID).Append("_")
                    .Append(model.EXPENSES_ID).Append("_")
                    .Append(model.PROJECT_ID);
                StringBuilder sbOrinData = new StringBuilder();
                sbOrinData.Append(exprWithdrawal.DEP_ID).Append("_")
                    .Append(exprWithdrawal.BUDGET_TYPE).Append("_")
                    .Append(exprWithdrawal.PLAN_ID).Append("_")
                    .Append(exprWithdrawal.PRODUCE_ID).Append("_")
                    .Append(exprWithdrawal.ACTIVITY_ID).Append("_")
                    .Append(exprWithdrawal.BUDGET_TYPE_ID).Append("_")
                    .Append(exprWithdrawal.EXPENSES_GROUP_ID).Append("_")
                    .Append(exprWithdrawal.EXPENSES_ID).Append("_")
                    .Append(exprWithdrawal.PROJECT_ID);
                if (sbFormData.Equals(sbOrinData))
                {
                    res["errorText"] = "โปรดแก้ไขข้อมูลในการเบิกจ่าย อย่างน้อย 1 รายการก่อนปรับปรุง";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // การปรับปรุงบัญชี 
                // เป็นการเปลี่ยนแปลงประเภทงบประมาณ (เงินงบ หรือ เงินนอกงบ)
                // หรือ เปลี่ยนแปลงกลุ่มค่าใช้จ่าย (แผนงาน ผลผลิต กิจกรรม ... โครงการ)
                // ทำให้ส่งผลกระทบต่อเงินของส่วนกลาง จึงมีขั้นตอนการปรับปรุงบัญชีดังนี้
                // 1.) ทำรายการ เบิกเกินส่งคืน (เก็บประวัติไว้เป็น ปรับปรุงบัญชี) และ ยกเลิกเลขเบิกจ่ายนั้นออกจากใบกัน 
                // 2.) กันเงินงบประมาณใหม่
                // 3.) เบิกจ่ายเงินงบประมาณเต็มยอด และได้เลขขอเบิกจ่ายเลขเดิม


                var adjustmentAmounts = exprWithdrawal.WITHDRAWAL_AMOUNT;
                var withdrawalCode = model.WITHDRAWAL_CODE;
                var currDatetime = DateTime.Now;

                // ขั้นตอนที่ 1 ทำเรื่องเบิกเกินส่งคืน (แต่เก็บประวัติไว้เป็น ปรับปรุงบัญชี) และยกเบิกรายการเบิกจ่าย จากใบกัน และคืนเงินกลับไปส่วนกลาง
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var result = BudgetUtils.DoCashbackReserveBudgetWithdrawal(db, model.RESERVE_ID, withdrawalCode, model.ReferDocNo, withdrawalCode, adjustmentAmounts, 3, model.RemarkText, userAuthorizeProfile);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ขั้นตอนที่ 2 กันเงินงบประมาณใหม่
                result = BudgetUtils.DoReserveBudget(db, string.Empty, exprWithdrawal.YR
                        , model.DEP_ID.Value
                        , model.PLAN_ID, model.PRODUCE_ID
                        , model.ACTIVITY_ID, model.BUDGET_TYPE_ID.Value
                        , model.EXPENSES_GROUP_ID.Value, model.EXPENSES_ID.Value
                        , model.PROJECT_ID, Convert.ToInt16(model.BUDGET_TYPE.Value)

                        , exprWithdrawal.RESERVE_TYPE, adjustmentAmounts
                        , currDatetime, model.RemarkText, userAuthorizeProfile);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ขั้นตอนที่ 3 เบิกจ่าย เต็มจำนวนที่กันเงิน
                result = BudgetUtils.DoWithdrawalReserveBudget(db, result.RunningCode, withdrawalCode, model.ReferDocNo, adjustmentAmounts
                        , currDatetime, 2, model.RemarkText
                        // อ้างอิงรายการกลับไปยัง รายการเบิกจ่ายที่ขอปรับปรุงบัญชี
                        // เพื่อให้สามารถอ้างอิงกลับไปยังใบกันใบเดิม ที่ขอปรับปรุงบัญชีได้
                        , exprWithdrawal.RESERVE_ID // เลขที่กันเงินใบเดิม
                        , exprWithdrawal.SEQ_NO // เลขที่ลำดับการเบิกจ่าย
                        , userAuthorizeProfile);
                if (!result.Completed)
                {
                    res["errorText"] = result.CauseErrorMessage;
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class AdjustmentBudgetReserveWithdrawalFormMapper
        {
            /// <summary>
            /// เลขที่กันเงิน
            /// </summary>
            public string RESERVE_ID { get; set; }

            /// <summary>
            /// เลขที่เบิกจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string WITHDRAWAL_CODE { get; set; }

            /// <summary>
            /// เลขที่เอกสารอ้างอิงรายการปรับปรุงบัญชี GMIF
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MinLength(10, ErrorMessage = "ความยาวอย่างน้อย {1} หลัก"), MaxLength(20, ErrorMessage = "ความยาวไม่เกิน {1} หลัก")]
            public string ReferDocNo { get; set; }

            /// <summary>
            /// หน่วยงานที่เบิกจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? DEP_ID { get; set; }

            /// <summary>
            /// แผนงาน
            /// </summary>
            public int? PLAN_ID { get; set; }

            /// <summary>
            /// ผลผลิต
            /// </summary>
            public int? PRODUCE_ID { get; set; }

            /// <summary>
            /// กิจกรรม
            /// </summary>
            public int? ACTIVITY_ID { get; set; }

            /// <summary>
            /// งบรายจ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? BUDGET_TYPE_ID { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? EXPENSES_GROUP_ID { get; set; }

            /// <summary>
            /// รายการค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int? EXPENSES_ID { get; set; }

            /// <summary>
            /// โครงการ
            /// </summary>
            public int? PROJECT_ID { get; set; }

            /// <summary>
            /// ประเภทงบ 1 = งบประมาณ, 2 = เงินนอกงบประมาณ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(int), "1", "2", ErrorMessage = "ค่าจะต้องอยู่ระหว่าง {1} - {2} เท่านั้น")]
            public int? BUDGET_TYPE { get; set; }


            /// <summary>
            /// จำเป็นต้องระบุโครงการหรือไม่
            /// </summary>
            public bool ProjectIdRequired { get; set; }

            /// <summary>
            /// หมวดค่าใช้จ่าย
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }
        }
    }
}