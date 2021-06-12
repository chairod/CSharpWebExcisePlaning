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
    /// กำหนดสูตรที่ใช้ในการคำนวณมูลค่า ของค่าใช้จ่าย
    /// การประมาณการรายได้ภาษี ได้แก่ ภาษีเพื่อมหาดไทย เงินค่าใช้จ่ายท้องถิ่นในประเทศ เงินค่าใช้จ่ายท้องถิ่นนำเข้า, ... เป็นต้น
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class TaxForcastExpensesTypeFormulaController : Controller
    {
        // GET: TaxForcastExpensesTypeFormula
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_TAX_FORCAST_EXPENSES_TYPE_FORMULA);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_TAX_FORCAST_EXPENSES_TYPE_FORMULA;
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
                ViewBag.ForcastExpensesTypes = db.T_TAX_FORCAST_EXPENSES_TYPEs.ToList();
            }

            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="effectiveDateStr">วันที่มีผลบังคับใช้สูตร</param>
        /// <param name="taxExpensesTypeId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(string effectiveDateStr, short? taxExpensesTypeId, int pageIndex, int pageSize)
        {
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalPages = 0,
                totalRecords = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_TAX_FORCAST_EXPENSES_TYPE_FORMULA_INFORMATIONs.AsQueryable();
                DateTime effectiveDate = AppUtils.TryValidUserDateStr(effectiveDateStr);
                if (effectiveDate != DateTime.MinValue)
                    expr = expr.Where(e => effectiveDate >= e.START_DATE && effectiveDate <= e.EXPIRE_DATE);
                if (null != taxExpensesTypeId)
                    expr = expr.Where(e => e.FORCAST_EXPENSES_TYPE_ID.Equals(taxExpensesTypeId.Value));


                var currDate = DateTime.Now.Date;
                var finalExpr = expr.AsEnumerable()
                        .GroupBy(e => new { e.FORMULA_ID, e.START_DATE, e.EXPIRE_DATE, e.FORCAST_EXPENSES_TYPE_ID, e.FORCAST_EXPENSES_TYPE_NAME })
                        .Select(e => new
                        {
                            e.Key.FORMULA_ID,
                            e.Key.START_DATE,
                            e.Key.EXPIRE_DATE,
                            e.Key.FORCAST_EXPENSES_TYPE_ID,
                            e.Key.FORCAST_EXPENSES_TYPE_NAME,
                            CAL_PERCENT_FORMULA = string.Join(",", e.OrderBy(x => x.CAL_PRIORITY).Select(x => x.PERCENT_VAL).ToList()),
                            // รายการใดหมดอายุแล้ว ไม่ให้แก้ไขรายการ
                            // เพราะจะส่งผลกับ Transaction
                            EDIT_ABLE = currDate <= e.Key.EXPIRE_DATE
                        }).OrderBy(e => e.FORCAST_EXPENSES_TYPE_ID)
                        .ThenByDescending(e => e.EXPIRE_DATE);

                pagging.totalRecords = finalExpr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalPages / Convert.ToDouble(pageSize));
                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = finalExpr.Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                ViewBag.ForcastExpensesTypes = db.T_TAX_FORCAST_EXPENSES_TYPEs.ToList();

            return View();
        }


        /// <summary>
        /// วันที่มีผลบังคับใช้วันถัดไป ของประเภทค่าใช้จ่ายการประมาณการรายได้ภาษี
        /// </summary>
        /// <param name="forcastExpensesTypeId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveNextStartDateBy(int forcastExpensesTypeId)
        {
            return Json(new Dictionary<string, DateTime?>() {
                { "NextStartDate", GetNextStartDate(forcastExpensesTypeId) }
            }, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// วันที่มีผลบังคับใช้วันถัดไป ของประเภทค่าใช้จ่ายการประมาณการรายได้ภาษี
        /// </summary>
        /// <param name="forcastExpensesTypeId"></param>
        /// <returns></returns>
        private DateTime? GetNextStartDate(int forcastExpensesTypeId)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULAs.Where(e => e.ACTIVE.Equals(1) && e.FORCAST_EXPENSES_TYPE_ID.Equals(forcastExpensesTypeId))
                        .OrderByDescending(e => e.EXPIRE_DATE).FirstOrDefault();
                if (null == expr)
                    return null; ;
                return expr.EXPIRE_DATE.AddDays(1);
            }
        }


        /// <summary>
        /// แสดงรายละเอียดของสูตรที่ระบุในหน้าฟอร์ม
        /// </summary>
        /// <param name="formulaId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveFormulaDetail(int? formulaId)
        {
            if (null == formulaId)
                return Json(null, JsonRequestBehavior.DenyGet);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                return Json(db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULA_DETAILs.Where(e => e.FORMULA_ID.Equals(formulaId)).ToList(), JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult SubmitDelete(int? formulaId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) { { "errorText", null } };
            if (null == formulaId)
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULAs.Where(e => e.FORMULA_ID.Equals(formulaId)).FirstOrDefault();
                if (null == expr)
                {
                    res["errorText"] = "ไม่พบสูตรการคำนวณที่ต้องการยกเลิก";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (expr.ACTIVE.Equals(-1))
                {
                    res["errorText"] = string.Format("สูตรการคำนวณนี้ถูกยกเลิกไปแล้วเมื่อ {0}", expr.DELETED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (DateTime.Now.Date.CompareTo(expr.EXPIRE_DATE) == 1)
                {
                    res["errorText"] = "สูตรการคำนวณนี้เลยกำหนดวันหมดอายุแล้ว ไม่สามารถยกเลิกได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var usrAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                expr.ACTIVE = -1;
                expr.DELETED_DATETIME = DateTime.Now;
                expr.DELETED_ID = usrAuthorizeProfile.EmpId;


                // ยกเลิกรายการที่วันที่เริ่มใช้มากกว่า รายการที่กำลังขอยกเลิกด้วย
                // เพราะจะเป็นผลทำให้วันที่แหว่งไป
                var exprMore = db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULAs.Where(e => e.START_DATE > expr.EXPIRE_DATE).ToList();
                exprMore.ForEach(e =>
                {
                    e.ACTIVE = -1;
                    e.DELETED_DATETIME = DateTime.Now;
                    e.DELETED_ID = usrAuthorizeProfile.EmpId;
                });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        [HttpPost]
        public ActionResult SubmitSave(TaxExpensesTypePercentFormulaFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(1) { { "errors", null }, { "errorText", null } };

            // ตรวจสอบความถูกต้องของค่าที่ส่งจากฟอร์ม
            var startDate = AppUtils.TryValidUserDateStr(model.StartDateStr);
            var expireDate = AppUtils.TryValidUserDateStr(model.ExpireDateStr);
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (!modelErrors.ContainsKey("StartDateStr") && startDate == DateTime.MinValue)
                modelErrors.Add("StartDateStr", new ModelValidateErrorProperty("StartDateStr", new List<string>(1) { { "รูปแบบวันที่ไม่ถูกต้อง" } }));
            if (!modelErrors.ContainsKey("ExpireDateStr") && expireDate == DateTime.MinValue)
                modelErrors.Add("ExpireDateStr", new ModelValidateErrorProperty("ExpireDateStr", new List<string>(1) { { "รูปแบบวันที่ไม่ถูกต้อง" } }));
            if (startDate != DateTime.MinValue && expireDate != DateTime.MinValue && expireDate.CompareTo(startDate) == -1)
                modelErrors.Add("ExpireDateStr", new ModelValidateErrorProperty("ExpireDateStr", new List<string>(1) { { "วันที่หมดอายุต้องมากกว่าวันที่เริ่มใช้" } }));

            if (modelErrors.Any())
            {
                if (modelErrors.Keys.Where(keyName => keyName.StartsWith("Formulas[")).Any())
                {
                    modelErrors = modelErrors.Where(errorItem => !errorItem.Key.StartsWith("Formulas[")).ToDictionary(t => t.Key, t => t.Value);
                    modelErrors.Add("Formulas", new ModelValidateErrorProperty("Formulas", new List<string>(1) { "โปรดตรวจสอบค่าสูตรในคอลัมล์ ค่าร้อยละ หรือ ลำดับการคำนวณ ระบุค่าให้ครบถ้วน" }));
                }

                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบการระบุ ลำดับการคำนวณซ้ำ (เพราะนำไปเป็น PK ร่วมกับ FormulaId)
            var priorityDupVals = model.Formulas.GroupBy(e => e.CAL_PRIORITY).Select(e => new { e.Key, COUNTs = e.Count() })
                    .Where(e => e.COUNTs > 1).Select(e => e.Key).ToList();
            if (priorityDupVals.Any())
            {
                res["errorText"] = string.Format("ระบุลำดับการคำนวณซ้ำกัน (ลำดับที่ซ้ำ: {0})", string.Join(",", priorityDupVals));
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var exprFormula = db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULAs.Where(e => e.FORMULA_ID.Equals(model.FormulaId)).FirstOrDefault();
                if (null == exprFormula)
                {
                    // วันที่เริ่มใช้ จะต้องต่อเนื่องจากรายการล่าสุด (ดูที่ระบบสร้างให้)
                    var nextStartDate = GetNextStartDate(model.TaxExpensesTypeId.Value);
                    if (null != nextStartDate && nextStartDate.Value.CompareTo(startDate) != 0)
                    {
                        res["errorText"] = string.Format("วันที่เริ่มใช้จะต้องต่อเนื่องจากการกำหนดสูตรล่าสุด (วันที่: {0})", nextStartDate.Value.ToString("dd/MM/yyyy", AppUtils.ThaiCultureInfo));
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    // วันที่เริ่มใช้งาน คาบเกี่ยว กับรายการอื่นหรือไม่ (เฉพาะที่ Active = 1)
                    var exprInvalidDate = db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULAs.Where(e => e.FORCAST_EXPENSES_TYPE_ID.Equals(model.TaxExpensesTypeId) 
                        && e.ACTIVE.Equals(1) 
                        && (
                            ((startDate >= e.START_DATE && expireDate >= e.START_DATE) && (startDate <= e.EXPIRE_DATE && expireDate <= e.EXPIRE_DATE))
                            || (startDate >= e.START_DATE && startDate <= e.EXPIRE_DATE)
                            || (expireDate >= e.START_DATE && expireDate <= e.EXPIRE_DATE)));
                    if (exprInvalidDate.Any())
                    {
                        res["errorText"] = "วันที่เริ่มใช้งาน หรือ วันที่หมดอายุ อยู่ในช่วงที่เคยกำหนดการใช้งานสูตร รายการอื่น";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    exprFormula = new T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULA()
                    {
                        START_DATE = startDate,
                        EXPIRE_DATE = expireDate,
                        FORCAST_EXPENSES_TYPE_ID = model.TaxExpensesTypeId.Value,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        ACTIVE = 1
                    };
                    db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULAs.InsertOnSubmit(exprFormula);
                    db.SubmitChanges();
                }
                else
                {
                    exprFormula.UPDATED_DATETIME = DateTime.Now;
                    exprFormula.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                // ลบสูตรเดิมออกไป
                var exprFormulaDetails = db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULA_DETAILs.Where(e => e.FORMULA_ID.Equals(exprFormula.FORMULA_ID));
                db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULA_DETAILs.DeleteAllOnSubmit(exprFormulaDetails);

                // บันทึกสูตรใหม่เข้าไป
                model.Formulas.ForEach(formulaProp =>
                {
                    db.T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULA_DETAILs.InsertOnSubmit(new T_TAX_FORCAST_EXPENSES_TYPE_PERCENT_FORMULA_DETAIL()
                    {
                        FORMULA_ID = exprFormula.FORMULA_ID,
                        PERCENT_VAL = formulaProp.PERCENT_VAL,
                        CAL_PRIORITY = formulaProp.CAL_PRIORITY,
                        REMARK_TEXT = formulaProp.REMARK_TEXT
                    });
                });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class TaxExpensesTypePercentFormulaFormMapper
        {
            /// <summary>
            /// เลขที่รายการที่ระบบสร้างขึ้นของสูตร
            /// </summary>
            public int? FormulaId { get; set; }

            /// <summary>
            /// วันที่เริ่มมีผลบังคับใช้สูตรนี้
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string StartDateStr { get; set; }


            /// <summary>
            /// วันหมดอายุของสูตรนี้
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string ExpireDateStr { get; set; }


            /// <summary>
            /// ประเภทรายจ่าย ของการประมาณการรายได้ภาษี
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? TaxExpensesTypeId { get; set; }

            /// <summary>
            /// สูตรที่ใช้ในการคำนวณหามูลค่าของค่าใช้จ่าย ประกอบไปด้วยร้อยละ
            /// อะไรบ้าง พร้อมลำดับในการคำนวณ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุสูตรในการคำนวณอย่างน้อย 1 รายการ")]
            public List<TaxExpensesTypeFormulaProperty> Formulas { get; set; }
        }

        public class TaxExpensesTypeFormulaProperty
        {
            /// <summary>
            /// ลำดับที่ใช้สูตรในการคำนวณ 1..100
            /// </summary>
            public short CAL_PRIORITY { get; set; }

            /// <summary>
            /// ร้อยละที่ใช้ในการนวณ 0..100
            /// </summary>
            public double PERCENT_VAL { get; set; }

            /// <summary>
            /// รายละเอียด อื่นๆเพิ่มเติม
            /// </summary>
            [MaxLength(100, ErrorMessage = "ความยาวไม่เกิน 100 ตัวอักษร")]
            public string REMARK_TEXT { get; set; }
        }
    }
}