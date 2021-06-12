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
    public class TaxForcastIncomeMonthlyController : Controller
    {
        // GET: TaxTypeIncomeForm
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_TAX_FORCAST_INCOME_MONTHLY);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_TAX_FORCAST_INCOME_MONTHLY;
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
            return View();
        }

        [HttpGet]
        public ActionResult GetModalAdjustmentForm()
        {
            return View();
        }

        /// <summary>
        /// ค้นหารายการประมาณการรายได้ภาษีในแต่ละปีงบประมาณ และ เดือน
        /// </summary>
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="monthNo">เดือน 1...12</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(short fiscalYear, short monthNo)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(3) { 
                { "rows", null }, 
                { "mode", "new" },
                { "warningMessage", null } // แสดงข้อความให้กำหนดสูตรการคำนวณมูลค่าค่าใช้จ่าย ของการประมาณการรายได้ภาษี
            };
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ลองค้นหาการกำหนดสูตรการคำนวณมูลค่า ค่าใช้จ่าย
                //if (!db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULAs.Any(x => x.YR.Equals(fiscalYear) && x.ACTIVE.Equals(1)))
                //    res["warningMessage"] = "ปีงบประมาณที่เลือก ยังไม่กำหนดสูตรสำหรับการคำนวณ ไปที่เมนู (ตั้งค่าระบบ => สูตรการคำนวณประเภทรายจ่าย ประมาณการภาษี)";


                // ค้นหาจากรายการประมาณการที่เคยบันทึกไว้
                var rows = db.V_GET_TAX_FORCAST_INCOME_MONTHLY_INFORMATIONs
                        .Where(e => e.YR.Equals(fiscalYear) && e.MN.Equals(monthNo))
                        .Select(e => new
                        {
                            e.FORCAST_INCOME_ID,
                            e.YR,
                            e.MN,
                            e.TAX_SOURCE_ID,
                            e.TAX_SOURCE_NAME,
                            e.TAX_SOURCE_ORDER_SEQ,

                            e.DOMESTIC_INCOME_AMOUNT,
                            e.IMPORT_INCOME_AMOUNT,
                            e.TOTAL_INCOME_AMOUNT,

                            e.DOMESTIC_EXPENSES_AMOUNT,
                            e.DOMESTIC_EXPENSES_LOCAL_AMOUNT,
                            e.IMPORT_EXPENSES_LOCAL_AMOUNT,
                            e.REMARK_TEXT,
                            e.CREATED_DATETIME,
                            e.CREATED_NAME
                        }).OrderBy(e => e.TAX_SOURCE_ORDER_SEQ).ToList();
                if (rows.Any())
                {
                    res["rows"] = rows;
                    res["mode"] = "edit";
                }
                else
                {
                    // กรณีไม่พบข้อมูลให้ Default จากรายการแหล่งเงินได้ภาษี

                    short? defaultShort = null;
                    decimal? defaultDecimal = decimal.Zero;
                    DateTime? defaultDatetime = null;

                    res["rows"] = db.T_TAX_INCOME_SOURCEs.Select(e => new
                    {
                        FORCAST_INCOME_ID = defaultShort,
                        YR = defaultShort,
                        MN = defaultShort,
                        e.TAX_SOURCE_ID,
                        e.TAX_SOURCE_NAME,
                        TAX_SOURCE_ORDER_SEQ = e.ORDER_SEQ,

                        DOMESTIC_INCOME_AMOUNT = defaultDecimal,
                        IMPORT_INCOME_AMOUNT = defaultDecimal,
                        TOTAL_INCOME_AMOUNT = defaultDecimal,

                        DOMESTIC_EXPENSES_AMOUNT = defaultDecimal,
                        DOMESTIC_EXPENSES_LOCAL_AMOUNT = defaultDecimal,
                        IMPORT_EXPENSES_LOCAL_AMOUNT = defaultDecimal,
                        REMARK_TEXT = "",
                        CREATED_DATETIME = defaultDatetime,
                        CREATED_NAME = ""
                    }).OrderBy(e => e.TAX_SOURCE_ORDER_SEQ).ToList();

                }
            };

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// แสดงแบบฟอร์มประวัติการปรับปรุง ประมาณการราย
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetModalHistoryForm()
        {
            return View();
        }


        /// <summary>
        /// แสดงประวัติการปรับปรุงการประมาณการรายได้ภาษี ของแต่ละประเภทรายได้ภาษี
        /// </summary>
        /// <param name="forcastIncomeId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveHistory(int forcastIncomeId, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_TAX_FORCAST_INCOME_HISTORY_INFORMATIONs
                    .Where(e => e.FORCAST_INCOME_ID.Equals(forcastIncomeId))
                    .Select(e => new
                    {
                        e.FORCAST_INCOME_ID,
                        e.ADJUSTMENT_TYPE,
                        e.SEQ_NO,
                        e.DOMESTIC_INCOME_AMOUNT,
                        e.IMPORT_INCOME_AMOUNT,
                        e.REMARK_TEXT,
                        e.CREATED_DATETIME,
                        e.CREATED_NAME
                    }).OrderByDescending(e => e.SEQ_NO);

                pagging.totalRecords = expr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = expr.Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// คำนวณมูลค่า ภาษีเพื่อมหาดไทย, ค่าใช้จ่ายท้องถิ่นภายในประเทศ, ค่าใช้จ่ายท้องถิ่นนำเข้า
        /// </summary>
        /// <param name="taxDomesticIncomeAmount"></param>
        /// <param name="taxImportIncomeAmount"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CalculateExpenses(decimal taxDomesticIncomeAmount, decimal taxImportIncomeAmount)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(3) {
                { "DomesticExpensesAmount", null },
                { "DomesticLocalAmount", null },
                { "ImportLocalAmount", null }
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var currDate = DateTime.Now.Date;
                // ภาษีเพื่อมหาดไทย
                res["DomesticExpensesAmount"] = db.fn_CalForcastExpensesTypeValue(taxDomesticIncomeAmount + taxImportIncomeAmount, currDate, 1);
                // ภาษีค่าใช้จ่ายท้องถิ่น ภายในประเทศ
                res["DomesticLocalAmount"] = db.fn_CalForcastExpensesTypeValue(taxDomesticIncomeAmount, currDate, 2);
                // ภาษีค่าใช้จ่ายท้องถิ่น นำเข้า
                res["ImportLocalAmount"] = db.fn_CalForcastExpensesTypeValue(taxImportIncomeAmount, currDate, 3);
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ปรับปรุงรายการประมาณการรายรับเงินภาษี
        /// </summary>
        /// <param name="forcastIncomeId"></param>
        /// <param name="adjustmentType">1 = เพิ่มจำนวนเงิน, 2 = ลดจำนวนเงิน</param>
        /// <param name="domesticForcastAmount">จำนวนที่ขอปรับปรุง ประมาณการรายรับ ภาษีภายในประเทศ (บาท)</param>
        /// <param name="importForcastAmount">จำนวนที่ขอปรับปรุง ประมาณการรายรับ ภาษีนำเข้า (บาท)</param>
        /// <param name="remarkText"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitAdjustment(TaxForcastIncomeSourceProperty model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) { { "errors", null }, { "errorText", null } };
            // ตรวจสอบความถูกต้องของหน้าฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            var totalForcastAmounts = model.DOMESTIC_INCOME_AMOUNT.Value + model.IMPORT_INCOME_AMOUNT.Value;
            if(totalForcastAmounts.CompareTo(decimal.Zero) <= 0)
            {
                res["errorText"] = "ยอดปรับปรุงการประมาณการรายได้ภาษีต้องมากกว่าศูนย์";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprForcast = db.T_TAX_FORCAST_INCOMEs.Where(e => e.FORCAST_INCOME_ID.Equals(model.FORCAST_INCOME_ID)).FirstOrDefault();
                if (null == exprForcast)
                {
                    res["errorText"] = "ไม่พบรายการประมาณการรายได้ภาษีที่ต้องการปรับปรุง";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ปรับปรุงยอดประมาณการรายได้ภาษี
                if (model.ADJUSTMENT_TYPE.Value.Equals(1))
                {
                    exprForcast.DOMESTIC_INCOME_AMOUNT += model.DOMESTIC_INCOME_AMOUNT.Value;
                    exprForcast.IMPORT_INCOME_AMOUNT += model.IMPORT_INCOME_AMOUNT.Value;
                }
                else
                {
                    exprForcast.DOMESTIC_INCOME_AMOUNT -= model.DOMESTIC_INCOME_AMOUNT.Value;
                    exprForcast.IMPORT_INCOME_AMOUNT -= model.IMPORT_INCOME_AMOUNT.Value;
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                exprForcast.UPDATED_DATETIME = DateTime.Now;
                exprForcast.UPDATED_ID = userAuthorizeProfile.EmpId;


                // เก็บเป็นประวัติการปรับปรุง
                var nextSeqNo = db.T_TAX_FORCAST_INCOME_HISTORies.Count(e => e.FORCAST_INCOME_ID.Equals(exprForcast.FORCAST_INCOME_ID));
                nextSeqNo++;
                db.T_TAX_FORCAST_INCOME_HISTORies.InsertOnSubmit(new T_TAX_FORCAST_INCOME_HISTORY()
                {
                    FORCAST_INCOME_ID = exprForcast.FORCAST_INCOME_ID,
                    SEQ_NO = Convert.ToInt16(nextSeqNo),
                    TAX_SOURCE_ID = exprForcast.TAX_SOURCE_ID,
                    YR = exprForcast.YR,
                    MN = exprForcast.MN,
                    ADJUSTMENT_TYPE = model.ADJUSTMENT_TYPE.Value, // 1 = เพิ่ม, 2 = ลด
                    DOMESTIC_INCOME_AMOUNT = model.DOMESTIC_INCOME_AMOUNT,
                    IMPORT_INCOME_AMOUNT = model.IMPORT_INCOME_AMOUNT,
                    REMARK_TEXT = model.REMARK_TEXT,
                    ACTIVE = 1,
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId
                });

                db.SubmitChanges();
            }
            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// จะเป็นการบันทึกในครั้งแรก ที่ยังไม่เคยประมาณการรายรับภาษีใน ปี/เดือนนั้น
        /// กรณีปรับปรุงจะต้อง ปรับปรุงผ่าน SubmitAdjustment
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitSave(TaxForcastIncomeFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "errors", null }, { "errorText", null } };

            // ตรวจสอบการระบุค่า จากผู้ใช้งาน ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            modelErrors = modelErrors.Where(e => !e.Key.StartsWith("TaxForcastIncomes[")).ToDictionary(t => t.Key, t => t.Value);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ยอดประมาณการรายรับภาษีต้องมากกว่าศูนย์
            if (model.TaxForcastIncomes.Sum(e => (e.DOMESTIC_INCOME_AMOUNT == null ? decimal.Zero : e.DOMESTIC_INCOME_AMOUNT.Value) + (e.IMPORT_INCOME_AMOUNT == null ? decimal.Zero : e.IMPORT_INCOME_AMOUNT.Value)) == 0)
            {
                res["errorText"] = "ผลรวมประมาณการรายรับภาษีต้องมากกว่า 0 บาท";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                model.TaxForcastIncomes.ForEach(forcastIncomeItem =>
                {
                    var exprForcast = new T_TAX_FORCAST_INCOME()
                    {
                        YR = model.FiscalYear.Value,
                        MN = model.MonthNo.Value,
                        TAX_SOURCE_ID = forcastIncomeItem.TAX_SOURCE_ID,
                        DOMESTIC_INCOME_AMOUNT = forcastIncomeItem.DOMESTIC_INCOME_AMOUNT == null ? decimal.Zero : forcastIncomeItem.DOMESTIC_INCOME_AMOUNT.Value,
                        IMPORT_INCOME_AMOUNT = forcastIncomeItem.IMPORT_INCOME_AMOUNT == null ? decimal.Zero : forcastIncomeItem.IMPORT_INCOME_AMOUNT.Value,
                        REMARK_TEXT = forcastIncomeItem.REMARK_TEXT,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    exprForcast.TOTAL_INCOME_AMOUNT = exprForcast.DOMESTIC_INCOME_AMOUNT + exprForcast.IMPORT_INCOME_AMOUNT;
                    db.T_TAX_FORCAST_INCOMEs.InsertOnSubmit(exprForcast);
                });

                var exprForcastIncomes = db.GetChangeSet().Inserts.Where(e => e.GetType() == typeof(T_TAX_FORCAST_INCOME))
                        .Select(e => (T_TAX_FORCAST_INCOME)e).ToList();

                // บันทึกการเปลี่ยนแปลงเพื่อให้ได้ ID ก่อน 
                // เพื่อนำไปบันทึกประวัติ
                db.SubmitChanges();

                // บันทึกประวัติการประมาณการรายรับภาษี
                exprForcastIncomes.ForEach(exprForcastIncome =>
                {
                    db.T_TAX_FORCAST_INCOME_HISTORies.InsertOnSubmit(new T_TAX_FORCAST_INCOME_HISTORY()
                    {
                        FORCAST_INCOME_ID = exprForcastIncome.FORCAST_INCOME_ID,
                        SEQ_NO = 1,
                        TAX_SOURCE_ID = exprForcastIncome.TAX_SOURCE_ID,
                        YR = exprForcastIncome.YR,
                        MN = exprForcastIncome.MN,
                        ADJUSTMENT_TYPE = 1, // 1 = เพิ่ม, 2 = ลด
                        DOMESTIC_INCOME_AMOUNT = exprForcastIncome.DOMESTIC_INCOME_AMOUNT,
                        IMPORT_INCOME_AMOUNT = exprForcastIncome.IMPORT_INCOME_AMOUNT,
                        REMARK_TEXT = exprForcastIncome.REMARK_TEXT,
                        ACTIVE = 1,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    });
                });
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class TaxForcastIncomeFormMapper
        {
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? FiscalYear { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(typeof(short), "1", "12", ErrorMessage = "เดือนที่ระบุอยู่ระหว่าง 1 - 12")]
            public short? MonthNo { get; set; }

            [Required(ErrorMessage = "แหล่งที่มาเงินได้ภาษีจะต้องมีอย่างน้อย 1 รายการ")]
            public List<TaxForcastIncomeSourceProperty> TaxForcastIncomes { get; set; }
        }

        public class TaxForcastIncomeSourceProperty
        {
            /// <summary>
            /// เลขที่รายการของแหล่งภาษีที่ประมาณการรายได้ ในปี/เดือน นั้น
            /// </summary>
            public int FORCAST_INCOME_ID { get; set; }

            /// <summary>
            /// แหล่งเงินได้ที่จะเก็บภาษี
            /// </summary>
            public short TAX_SOURCE_ID { get; set; }

            /// <summary>
            /// ประมาณการรายรับภาษีภายในประเทศ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public decimal? DOMESTIC_INCOME_AMOUNT { get; set; }

            /// <summary>
            /// ประมาณการรายรับภาษีนำเข้า
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public decimal? IMPORT_INCOME_AMOUNT { get; set; }

            /// <summary>
            /// รายละเอียดอื่นๆ
            /// </summary>
            [MaxLength(120, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string REMARK_TEXT { get; set; }


            /// <summary>
            /// ใช้สำหรับกรณีปรับปรุง รายรายประมาณการรายได้ภาษี
            /// 1 = เพิ่มยอดประมาณการ, 2 = ลดยอดประมาณการ
            /// </summary>
            [Range(typeof(short), "1", "2", ErrorMessage = "ค่าที่ระบุได้อยู่ระหว่าง {1} - {2}")]
            public short? ADJUSTMENT_TYPE { get; set; }
        }

    }
}