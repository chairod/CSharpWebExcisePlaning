using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{

    [CustomAuthorize(Roles = "Admin,Manager1,Manager2,Manager3,General")]
    public class BudgetRequestController : Controller
    {
        // GET: BuegetRequest
        /// <summary>
        /// แสดงแบบฟอร์มคำขอเงินงบประมาณ
        /// 1.) กรณีที่ อยู่ระหว่างคำขอเงิน งปม. ต้นปีอยู่ เมื่อเข้าสู่หน้าจอให้ Default ข้อมูล
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetForm(string requestId)
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_REQUEST_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_REQUEST_MENU;
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

            // การขอเงินงบประมาณจะมี ขอ งปม. ต้นปี (START), ของบเพิ่มเติม (MORE)
            ViewBag.RequestId = string.IsNullOrEmpty(requestId) ? "" : requestId;
            ViewBag.RequestType = "START"; // คำขอเงินงบประมาณต้นปี
            ViewBag.CurrYear = AppUtils.GetCurrYear(); // ปี งปม. ปัจจุบัน
            ViewBag.FiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.DepartmentId = userAuthorizeProfile.DepId;
            ViewBag.DepartmentName = userAuthorizeProfile.DepName;
            ViewBag.EmpFullName = userAuthorizeProfile.EmpFullname;

            // เป็นหน่วยงานที่ไม่สามารถสร้างคำของบประมาณใหม่ได้
            // แต่สามารถแก้ไข คำขอให้กับหน่วยงานภายใต้ตนเองได้
            ViewBag.CanCreateBudgetRequest = userAuthorizeProfile.CanCreateRequestBudget;

            // รหัสรายการ คชจ. ที่สงวนไม่ให้เลือกได้ในกรณีที่กำลังทำรายการ ค่าปิโตรเลียม
            var appSettings = AppSettingProperty.ParseXml();
            ViewBag.ExpensesIdsReserveForPetolium = appSettings.ExpensesIdsReserveForPetoluemToList();

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // หน่วยงานที่กำลังทำคำขอในปี งปม. ทำคำของบต้นปีไปหรือยัง?
                // ถ้าอยู่ระหว่างงบประมาณต้นปี ให้ Default ข้อมูลไปยังงบประมาณต้นปี ของแต่ละหน่วยงาน
                if (string.IsNullOrEmpty(requestId))
                {
                    var entity = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.ACTIVE.Equals(1) && e.DEP_ID.Equals(userAuthorizeProfile.DepId)
                           && e.YR.Equals(userAuthorizeProfile.DefaultFiscalYear)
                           && e.REQ_TYPE.Equals(1)).Select(e => new
                           {
                               e.REQ_ID,
                               e.SIGNOFF_FLAG
                           }).FirstOrDefault();
                    if (null != entity)
                    {
                        if (entity.SIGNOFF_FLAG)
                            ViewBag.RequestType = "MORE"; // คำของบประมาณเพิ่มเติม
                        else
                            ViewBag.RequestId = entity.REQ_ID; // Default คำขอต้นปี กรณียังไม่ SignOff
                    }
                }
                else
                {
                    var expr = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.ACTIVE.Equals(1) && e.REQ_ID.Equals(requestId))
                            .Select(e => new { e.REQ_TYPE }).FirstOrDefault();
                    if (null != expr)
                        ViewBag.RequestType = expr.REQ_TYPE == 1 ? "START" : "MORE";
                }


                // ข้อมูล Master ด้านล่างนี้ ดึงมาใช้สำหรับแบบฟอร์มการคีย์ข้อมูลรายละเอียดของ รายการค่าใช้จ่าย
                // ประเภทบุคลากร
                ViewBag.PersonnelTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1)).Select(e => new PersonnelTypeShortFieldProperty()
                {
                    PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                    PERSON_TYPE_NAME = e.ITEM_TEXT
                }).ToList();

                // ตำแหน่งงาน
                ViewBag.Positions = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1)).Select(e => new PositionShortFieldProperty()
                {
                    POSITION_ID = e.POSITION_ID,
                    POSITION_NAME = e.POSITION_NAME
                }).ToList();

                // ระดับของการปฏิบัติงาน
                ViewBag.PersonnelLevels = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1)).Select(e => new PersonnelLevelShortFieldProperty()
                {
                    LEVEL_ID = e.LEVEL_ID,
                    LEVEL_NAME = e.LEVEL_NAME
                }).ToList();
            }

            return View();
        }


        /// <summary>
        /// แสดงแบบฟอร์มในการระบุรายละเอียดย่อยของแต่ล่ะ รายการ คชจ.
        /// เนื่องจาก แบบฟอร์มในการระบุรายละเอียดของแต่ละรายการ คชจ. มีโครงการสร้างเก็บข้อมูลที่แตกต่างกัน
        /// ถ้ามีรายการค่าใช้จ่ายอื่นๆขึ้นมา ก็ให้สร้างเป็นฟอร์มใหม่ที่รองรับแบบฟอร์ม หรือใช้แบบฟอร์มเดิม
        /// โดยให้กำหนดชื่อฟอร์มที่ต้องการใช้ ลงไปใน Setup ระบบ
        /// </summary>
        /// <param name="expensesFormName"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetExpensesDetailForm(string expensesFormName)
        {
            return View(string.Format("~/Views/BudgetRequest/ExpensesForms/{0}.cshtml", expensesFormName));
        }
        /// <summary>
        /// โครงการการเก็บข้อมูลของรายการค่าใช้จ่าย ซึ่งจะนิยามโครงสร้างไว้ในรูปแบบ Json 
        /// </summary>
        /// <param name="expensesFormName"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetExpensesColumn(string expensesFormName)
        {
            var absolutePath = Server.MapPath("~/Views/BudgetRequest/ExpensesColumnDefined");
            string myFile = string.Format("{0}/{1}.json", absolutePath, expensesFormName.Trim());
            if (System.IO.File.Exists(myFile))
                return Json(System.IO.File.ReadAllText(myFile), JsonRequestBehavior.AllowGet);

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// แบบฟอร์มให้เลือก Master Template คำขอเงินงบประมาณ
        /// ในกรณีที่เลือก Template คำขอมากกว่า 1 รายการ จะมี กลุ่มข้อมูล (แผนงาน ผลผลิต กิจกรรม, ...) ที่ไม่ตรงกัน
        /// ทำให้ไม่สามารถกำหนดได้ว่า ต้องใช้ Template ไหนตั้งต้น
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetSelectMasterTemplateForm()
        {
            return View();
        }

        /// <summary>
        /// ค้นหาอัตราค่าเช่าบ้าน โดยใช้ ระดับของบุคลากร และ เงินเดือน เป็นเงื่อนไข
        /// </summary>
        /// <param name="levelId"></param>
        /// <param name="salary"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetRentHouseRateBy(int? levelId, decimal? salary)
        {
            if (null == levelId || null == salary)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_RENT_HOUSE_RATE_CONFIGUREs.Where(e => e.ACTIVE.Equals(1) && e.LEVEL_ID.Equals(levelId));
                expr = expr.Where(e => salary.Value >= e.FROM_SALARY && salary.Value <= e.TO_SALARY);
                var finalExpr = expr.Select(e => e.RATE_AMOUNT).FirstOrDefault();

                return Json(finalExpr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ค้นหา ร้อยละของเงินเดือน ที่หักเข้ากองทุนประกันสังคม
        /// ประกอบด้วย 2 ค่าคือ A. ร้อยล่ะของเงินเดือน, B. จำนวนเงินต้องไม่เกินเท่าไหร่
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetSalaryRateForSocialSecurity()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = AppUtils.GetUsingAppConstByKey(AppConfigConst.APP_CONST_SALARY_RATE_FOR_SOCIAL_SECURITY);
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// อัตราค่าตอบแทนการปฏิบัติงานนอกเวลาราชการ ในวันทำการปกติ
        /// เป็นค่าตอบแทนต่อชม. 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetWorkingRateCompensation()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = AppUtils.GetUsingAppConstByKey(AppConfigConst.APP_CONST_WORKING_RATE_COMPENSATION);
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// อัตราค่าตอบแทนการปฏิบัติงานนอกเวลาราชการ ในวันหยุดและวันหยุดนขัตฤกษ์
        /// เป็นค่าตอบแทนต่อชม. 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetHolidayRateCompensation()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = AppUtils.GetUsingAppConstByKey(AppConfigConst.APP_CONST_HOLIDAY_RATE_COMPENSATION);
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }

        /// <summary>
        /// ค้นหา ร้อยล่ะของเงินเดือน ที่หักเข้ากองทุนเงินทดแทน
        /// ประกอบด้วย 1 ค่าคือ ร้อยล่ะของเงินเดือนที่หักเข้ากองทุนเงินทดแทน
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GetSalaryRateForCompensationFund()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = AppUtils.GetUsingAppConstByKey(AppConfigConst.APP_CONST_SALARY_RATE_FOR_COMPENSATION_FUND);
                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        /// <summary>
        /// ค้นหารายละเอียดสินทรัพย์ ของหน่วยงาน เช่น ราคามาตรฐาน ราคาไม่กำหนดมาตรฐาน จำนวนคงคลัง เป็นต้น
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveDepartmentAssetStockInfo(int? assetId)
        {
            if (null == assetId)
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var expr = db.T_ASSET_DEPARTMENT_STOCKs.Where(e => e.ACTIVE.Equals(1) && e.ASSET_ID.Equals(assetId) && e.DEP_ID.Equals(userAuthorizeProfile.DepId)).Select(e => new
                {
                    e.ASSET_ID,
                    e.STOCK_AMOUNT,
                    e.STANDARD_PRICE,
                    e.NON_STANDARD_PRICE,
                    e.PRICE_TYPE
                }).FirstOrDefault();

                return Json(expr, JsonRequestBehavior.DenyGet);
            };

        }

        /// <summary>
        /// ค้นหาข้อมูลในใบคำขอเงิน งปม.
        /// </summary>
        /// <param name="reqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RetrieveBudgetRequest(string reqId)
        {
            if (string.IsNullOrEmpty(reqId))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_BUDGET_REQUEST_INFORMATIONs.Where(e => e.REQ_ID.Equals(reqId)).Select(e => new
                {
                    e.REQ_ID,
                    e.REFER_REQ_ID, // เลขที่หนังสืออ้างอิง กรณีคำขอเพิ่มเติม
                    e.DEP_ID,
                    e.DEP_NAME,
                    e.SUB_DEP_ID,
                    e.SUB_DEP_NAME,
                    e.REQ_TYPE,
                    e.BUDGET_TYPE,
                    e.YR,
                    e.TOTAL_REQUEST_BUDGET,
                    e.REMARK_TEXT,

                    // สถานะคำขอ 1 = จัดสรร, 0 = รอจัดสรร, -1 = ยกเลิก
                    e.PROCESS_STATUS,

                    // ข้อมูลผู้ส่งคำขอ งปม.
                    e.CREATED_DATETIME,
                    e.CREATED_NAME,

                    // ข้อมูลการ SignOff คำขอ งปม. ต้นปี
                    e.SIGNOFF_FLAG,
                    e.SIGNOFF_NAME,
                    e.SIGNOFF_DATETIME,

                    // TemplateId ที่ใช้ทำคำขอ
                    TEMPLATE_IDs = db.T_BUDGET_REQUEST_DETAILs.Where(reqDetail => reqDetail.ACTIVE.Equals(1) && reqDetail.REQ_ID.Equals(reqId))
                        .Select(reqDetail => reqDetail.TEMPLATE_ID).GroupBy(templateId => templateId).ToList()
                }).FirstOrDefault();

                return Json(expr, JsonRequestBehavior.DenyGet);
            }
        }


        [HttpPost]
        public ActionResult SubmitSave(BudgetRequestFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(3) {
                { "errors", null },
                { "errorText", null },
                { "reqId", null }
            };

            var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

            // กรณีเป็นหน่วยงานที่ไม่สามารถสร้างคำของบประมาณใหม่ได้
            // แต่ ยอมให้แก้ไขคำขอหน่วยงานภายใต้หน่วยงานที่ตนเองรับผิดชอบได้
            if (!userAuthorizeProfile.CanCreateRequestBudget)
                if (string.IsNullOrEmpty(model.RequestId)) // ไม่สามารถสร้างคำขอใหม่ได้
                {
                    res["errorText"] = "ในระบบกำหนดให้หน่วยงานที่ท่านสังกัดอยู่ ไม่สามารถสร้างคำของบประมาณใหม่ได้ โปรดตรวจสอบ";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

            // ตรวจสอบความถูกต้องของข้อมูล
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            // บังคับให้กรอกเลขที่หนังสืออ้างอิง กรณีเป็นคำขอเพิ่มเติม
            if (model.RequestType.Equals(2) && string.IsNullOrEmpty(model.RefCode))
                modelErrors.Add("RefCode", new ModelValidateErrorProperty("RefCode", new List<string>(1) { "ระบุค่านี้ก่อน" }));
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // จำนวนเงินในคำขอต้องมากกว่าศูนย์บาท
            if (model.Expenses.Sum(e => e.TOTAL_REQUEST_BUDGET == null ? decimal.Zero : e.TOTAL_REQUEST_BUDGET.Value).CompareTo(decimal.Zero) == 0)
            {
                res["errorText"] = "ยอดคำของบประมาณเป็นศูนย์บาท โปรดตรวจสอบ";
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprRequestMas = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.REQ_ID.Equals(model.RequestId)).FirstOrDefault();
                if (null == exprRequestMas)
                {
                    // กรณีเป็นการทำคำขอ งปม. ต้นปี
                    // ให้ตรวจสอบหน่วยงานที่กำลังทำคำขอต้นปี มีรายการอยู่แล้วหรือยัง
                    if (model.RequestType.Equals(1))
                        if (db.T_BUDGET_REQUEST_MASTERs.Any(e => e.ACTIVE.Equals(1) && e.YR.Equals(model.FiscalYear) && e.REQ_TYPE.Equals(1) && e.DEP_ID.Equals(userAuthorizeProfile.DepId)))
                        {
                            res["errorText"] = "คำของบประมาณต้นปี มีทำรายการไว้แล้ว";
                            return Json(res, JsonRequestBehavior.DenyGet);
                        }

                    // กรณีคำขอเพิ่มเติม 
                    // นับจำนวนครั้งที่ส่งคำขอเพิ่มเติมของหน่วยงาน
                    short? reqMoreCount = null;
                    if (model.RequestType.Equals(2))
                    {
                        reqMoreCount = Convert.ToInt16(db.T_BUDGET_REQUEST_MASTERs.Where(e => e.YR.Equals(model.FiscalYear)
                            && e.DEP_ID.Equals(userAuthorizeProfile.DepId)
                            && e.REQ_TYPE.Equals(2)).Count());
                        reqMoreCount++;
                    }

                    var fiscalYear2Digits = (model.FiscalYear + 543).ToString().Substring(2);
                    string requestId = AppUtils.GetNextKey("BUDGET_REQUEST.REQ_ID", string.Format("R{0}", fiscalYear2Digits), 8, true, false);
                    exprRequestMas = new T_BUDGET_REQUEST_MASTER()
                    {
                        REQ_ID = requestId,
                        AREA_ID = userAuthorizeProfile.AreaId,
                        DEP_ID = userAuthorizeProfile.DepId,
                        SUB_DEP_ID = userAuthorizeProfile.SubDepId,
                        YR = model.FiscalYear,
                        REQ_TYPE = model.RequestType,
                        BUDGET_TYPE = model.BudgetTypeFlag.Value,
                        REMARK_TEXT = model.RemarkText,
                        PROCESS_STATUS = 0, // 1 = ได้รับจัดสรร, 0 = รอจัดสรร, -1 = ไม่จัดสรร
                        SIGNOFF_FLAG = false, // คำขอต้นปี จะมีการ SignOff คำขอก่อนเพื่อให้ กรมสรรพสามิตรวบรวมเงินไปขอกับรัฐบาล
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        REQ_COUNT = reqMoreCount, // ครั้งที่ส่งคำขอเพิ่มเติม
                        ACTIVE = 1 // 1 = ยังใช้งานอยู่
                    };
                    db.T_BUDGET_REQUEST_MASTERs.InsertOnSubmit(exprRequestMas);

                    // ปรับปรุงครั้งที่ของบล่าสุด ของหน่วยงาน
                    var exprAllocateMas = db.T_BUDGET_ALLOCATEs.Where(e => e.YR.Equals(exprRequestMas.YR) && e.DEP_ID.Equals(exprRequestMas.DEP_ID)).FirstOrDefault();
                    if (null != exprAllocateMas)
                    {
                        exprAllocateMas.LATEST_REQUEST_DATETIME = DateTime.Now;
                        exprAllocateMas.LATEST_REQUEST_ID = userAuthorizeProfile.EmpId;
                    }
                }
                else
                {
                    if (exprRequestMas.PROCESS_STATUS.Equals(-1))
                    {
                        res["errorText"] = "คำของบประมาณนี้ยกเลิกรายการแล้ว ไม่สามารถแก้ไขข้อมูลได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    else if (exprRequestMas.PROCESS_STATUS.Equals(1))
                    {
                        res["errorText"] = "คำของบประมาณนี้จัดสรรงบประมาณแล้ว ไม่สามารถแก้ไขข้อมูลได้";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }
                    // แก้ไขคำขอได้เฉพาะหน่วยงานที่รับผิดชอบ ยกเว้นหน่วยงานกลางแก้ไขได้ทุกหน่วยงาน
                    else if (userAuthorizeProfile.DepAuthorize.Equals(2)
                        && !userAuthorizeProfile.DepId.Equals(exprRequestMas.DEP_ID)
                        && userAuthorizeProfile.AssignDepartmentIds.IndexOf(exprRequestMas.DEP_ID) == -1)
                    {
                        res["errorText"] = "ในระบบกำหนดให้หน่วยงานที่ท่านสังกัดอยู่ แก้ไขคำขอได้เฉพาะหน่วยงานที่รับผิดชอบ โปรดตรวจสอบ";
                        return Json(res, JsonRequestBehavior.DenyGet);
                    }

                    exprRequestMas.UPDATED_DATETIME = DateTime.Now;
                    exprRequestMas.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                // จำนวนเงินที่ส่งคำขอเงิน งปม. ของหน่วยงาน
                decimal netBudgetRequestAmounts = model.Expenses.Sum(e => null == e.TOTAL_REQUEST_BUDGET ? decimal.Zero : e.TOTAL_REQUEST_BUDGET.Value);
                exprRequestMas.TOTAL_REQUEST_BUDGET = netBudgetRequestAmounts;
                exprRequestMas.REMARK_TEXT = model.RemarkText;
                exprRequestMas.REFER_REQ_ID = model.RefCode;

                // สามารถแก้ไข ประเภทงบประมาณ (เงินงบประมาณ หรือ เงินนอก งปม.) ได้เฉพาะกรณีของบเพิ่มเติม
                if (model.RequestType.Equals(2))
                    exprRequestMas.BUDGET_TYPE = model.BudgetTypeFlag.Value;


                // รายการ คชจ. ที่ส่งคำขอ งปม.
                // ล้างรายการ คชจ. เดิมออกไปก่อน แล้วค่อยลงข้อมูลใหม่
                db.T_BUDGET_REQUEST_DETAILs.DeleteAllOnSubmit(db.T_BUDGET_REQUEST_DETAILs.Where(e => e.REQ_ID.Equals(exprRequestMas.REQ_ID)));
                short seqNo = 1;
                StringBuilder sb = new StringBuilder();
                foreach (var expensesItem in model.Expenses)
                {
                    sb.Clear();
                    sb.Append("{\"root\":").Append(expensesItem.EXPENSES_DESCRIBEs).Append("}");
                    var newEntity = new T_BUDGET_REQUEST_DETAIL()
                    {
                        REQ_ID = exprRequestMas.REQ_ID,
                        DEP_ID = exprRequestMas.DEP_ID,
                        SUB_DEP_ID = exprRequestMas.SUB_DEP_ID,
                        YR = exprRequestMas.YR,
                        TEMPLATE_ID = expensesItem.TEMPLATE_ID,
                        PLAN_ID = expensesItem.PLAN_ID,
                        PRODUCE_ID = expensesItem.PRODUCE_ID,
                        ACTIVITY_ID = expensesItem.ACTIVITY_ID,
                        BUDGET_TYPE_ID = expensesItem.BUDGET_TYPE_ID,
                        EXPENSES_GROUP_ID = expensesItem.EXPENSES_GROUP_ID,
                        EXPENSES_ID = expensesItem.EXPENSES_ID,
                        EXPENSES_XML_DESCRIBE = AppUtils.JsonToLinqXml(sb.ToString()),
                        TOTAL_REQUEST_BUDGET = expensesItem.TOTAL_REQUEST_BUDGET == null ? decimal.Zero : expensesItem.TOTAL_REQUEST_BUDGET.Value,
                        SEQ_NO = seqNo++,
                        ALLOCATE_FLAG = 0, // 0 = รอจัดสรร, 1 = จัดสรรแล้ว, -1 = ไม่จัดสรร
                        ITEM_TYPE = 1, // 1 = คำขอโดยหน่วยงาน (ใช้ค่านี้)
                        ACTIVE = 1, // 1 = ยังใช้งานอยู่
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId
                    };
                    db.T_BUDGET_REQUEST_DETAILs.InsertOnSubmit(newEntity);
                }

                // ส่งเลขที่คำของบ ประมาณกลับไปเพื่อให้ Reload ข้อมูล
                res["reqId"] = exprRequestMas.REQ_ID;

                // บันทึกการเปลี่ยนแปลง
                db.SubmitChanges();
            };


            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// SignOff คำของบประมาณต้นปี
        /// </summary>
        /// <param name="reqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitSignOff(string reqId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) {
                { "errorText", null }
            };
            if (string.IsNullOrEmpty(reqId))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.REQ_ID.Equals(reqId) && e.ACTIVE.Equals(1)).FirstOrDefault();
                if (null == expr)
                    return Json(null, JsonRequestBehavior.DenyGet);

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                // Signoff คำขอได้เฉพาะหน่วยงานตนเอง หรือหน่วยงานที่รับผิดชอบ ยกเว้นหน่วยงานกลาง
                if (userAuthorizeProfile.DepAuthorize.Equals(2) && !userAuthorizeProfile.DepId.Equals(expr.DEP_ID)
                    && userAuthorizeProfile.AssignDepartmentIds.IndexOf(expr.DEP_ID) == -1)
                {
                    res["errorText"] = "ท่านไม่ได้รับสิทธิ์ให้ SignOff ให้กับหน่วยงานอื่น";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบสถานะการใช้งานคำขอ
                if (expr.ACTIVE.Equals(-1))
                {
                    res["errorText"] = "คำขอเงินงบประมาณนี้ ถูกยกเลิกแล้ว";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบสถานะการจัดสรรคำขอ
                if (!expr.PROCESS_STATUS.Equals(0))
                {
                    res["errorText"] = "คำขอเงินงบประมาณนี้ จัดสรรงบประมาณแล้ว";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบสถานะการ Signoff
                if (expr.SIGNOFF_FLAG)
                {
                    res["errorText"] = string.Format("คำขอเงินงบประมาณนี้ SignOff แล้ว (เวลา: {0})", expr.SIGNOFF_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                expr.SIGNOFF_FLAG = true;
                expr.SIGNOFF_DATETIME = DateTime.Now;
                expr.SIGNOFF_ID = userAuthorizeProfile.EmpId;
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ยกเลิก คำของบประมาณต้นปี
        /// </summary>
        /// <param name="reqId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SubmitReject(string reqId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) {
                { "errorText", null }
            };
            if (string.IsNullOrEmpty(reqId))
                return Json(null, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprReqMas = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.REQ_ID.Equals(reqId) && e.ACTIVE.Equals(1)).FirstOrDefault();
                if (null == exprReqMas)
                    return Json(null, JsonRequestBehavior.DenyGet);

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                // กรณีเป็นไม่ใช่หน่วยงานกลาง ยกเลิกได้เฉพาะข้อมูลภายในหน่วยงานของตน
                if (userAuthorizeProfile.DepAuthorize.Equals(2) && !exprReqMas.DEP_ID.Equals(userAuthorizeProfile.DepId))
                {
                    res["errorText"] = "ท่านไม่ได้รับสิทธิ์ให้ยกเลิกคำขอ ของหน่วยงานอื่น";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบสถานะการใช้งานคำขอ
                if (!exprReqMas.ACTIVE.Equals(1))
                {
                    res["errorText"] = "คำขอเงินงบประมาณนี้ ถูกยกเลิกแล้ว";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบสถานะการจัดสรรคำขอ
                if (!exprReqMas.PROCESS_STATUS.Equals(0))
                {
                    res["errorText"] = "คำขอเงินงบประมาณนี้ จัดสรรงบประมาณแล้ว";
                    if (exprReqMas.PROCESS_STATUS.Equals(-1))
                        res["errorText"] = "คำขอเงินงบประมาณนี้ ถูกปฏิเสธคำของบประมาณไปแล้ว";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                // ตรวจสอบสถานะการ Signoff
                if (exprReqMas.SIGNOFF_FLAG)
                {
                    res["errorText"] = string.Format("คำขอเงินงบประมาณนี้ SignOff แล้ว (เวลา: {0})", exprReqMas.SIGNOFF_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                exprReqMas.ACTIVE = -1;
                exprReqMas.DELETED_DATETIME = DateTime.Now;
                exprReqMas.DELETED_ID = userAuthorizeProfile.EmpId;

                // ยกเลิกรายการค่าใช้จ่ายของคำขอ
                var exprReqExpenses = db.T_BUDGET_REQUEST_DETAILs.Where(e => e.ACTIVE.Equals(1) && e.REQ_ID.Equals(reqId)).ToList();
                exprReqExpenses.ForEach(entity =>
                {
                    entity.ACTIVE = -1;
                    entity.UPDATED_DATETIME = DateTime.Now;
                    entity.UPDATED_ID = userAuthorizeProfile.EmpId;
                });

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// บันทึกรายการ ครุภัณฑ์ต่างๆ ของแต่ละหน่วยงาน อาทิเช่น ครุภัณฑ์สำนักงาน ครภัณฑ์คอมพิวเตอร์ เป็นต้น โดยประกอบด้วยข้อมูล
        /// จำนวนคงคลัง ประเภทราคา ราคา 
        /// สำหรับเก็บเป็นประวัติในการดึงค่ามาใช้งานในภายหลัง
        /// </summary>
        /// <param name="model"></param>
        [HttpPost]
        public void SubmitSaveDepartmentAsset(OfficialEquipmentFormMapper model)
        {
            if (null == model)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                bool isRequiredSaveChanged = false;
                var assetItems = model.Items.Where(e => e.AssetId != 0).GroupBy(e => e.AssetId).Select(e => new
                {
                    Value = e.First()
                }).ToList();
                foreach (var modelItem in assetItems)
                {
                    var entity = db.T_ASSET_DEPARTMENT_STOCKs.Where(e => e.DEP_ID.Equals(userAuthorizeProfile.DepId) && e.ASSET_ID.Equals(modelItem.Value.AssetId)).FirstOrDefault();
                    if (null == entity)
                    {
                        entity = new T_ASSET_DEPARTMENT_STOCK()
                        {
                            ASSET_ID = modelItem.Value.AssetId,
                            DEP_ID = userAuthorizeProfile.DepId,
                            ACTIVE = 1
                        };
                        db.T_ASSET_DEPARTMENT_STOCKs.InsertOnSubmit(entity);
                    }
                    else
                    {
                        entity.UPDATED_DATETIME = DateTime.Now;
                        entity.UPDATED_ID = userAuthorizeProfile.EmpId;
                    }


                    entity.STOCK_AMOUNT = modelItem.Value.StockAmounts;
                    entity.PRICE_TYPE = modelItem.Value.PriceType;
                    if (modelItem.Value.Equals(1))
                    {
                        entity.STANDARD_PRICE = modelItem.Value.PricePerUnit;
                        entity.NON_STANDARD_PRICE = null;
                    }
                    else
                    {
                        entity.STANDARD_PRICE = null;
                        entity.NON_STANDARD_PRICE = modelItem.Value.PricePerUnit;
                    }


                    isRequiredSaveChanged = true;
                }

                // บันทึกการเปลี่ยนแปลงไปยังฐานข้อมูล
                if (isRequiredSaveChanged)
                    db.SubmitChanges();
            }
        }


        /// <summary>
        /// คลาส Map ข้อมูลฟอร์มคำขอเงินงบประมาณ
        /// </summary>
        public class BudgetRequestFormMapper
        {
            /// <summary>
            /// เลขที่คำขอเงิน งปม.
            /// </summary>
            public string RequestId { get; set; }

            /// <summary>
            /// เลขที่หนังสืออ้างอิง ใช้ในกรณีที่เป็นคำขอเพิ่มเติม (บังคับให้กรอก)
            /// </summary>
            [MaxLength(11, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RefCode { get; set; }

            /// <summary>
            /// คำขอปีงบประมาณใด (ค.ศ.)
            /// </summary>
            public short FiscalYear { get; set; }

            /// <summary>
            /// เลขอ้างอิง Template ที่ใช้ทำคำขอ งปม.
            /// </summary>
            //public int TemplateId { get; set; }

            /// <summary>
            /// ประเภทคำขอ งปม. 1 = คำขอ งปม. ต้นปี, 2 = คำขอ งปม. เพิ่มเติม
            /// </summary>
            public short RequestType { get; set; }

            /// <summary>
            /// ประเภทงบประมาณ 1 = เงิน งปม., 2 = เงินนอก งปม.
            /// </summary>
            [Required(ErrorMessage = "กรุณาระบุค่านี้ก่อน")]
            public short? BudgetTypeFlag { get; set; }


            [MaxLength(200, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }


            /// <summary>
            /// รายการค่าใช้จ่าย ที่ส่งคำขอเงิน งปม.
            /// </summary>
            [Required(ErrorMessage = "ไม่พบรายการค่าใช้จ่ายที่ต้องการส่งคำขอเงินงบประมาณ")]
            public List<BudgetRequestExpensesProperty> Expenses { get; set; }
        }

        /// <summary>
        /// คุณสมบัติของรายการค่าใช้จ่าย ที่อยู่ในคำขอ งปม.
        /// เช่น รายการค่าใช้จ่าย จำนวนเงินที่ขอ รายละเอียดของรายการ คชจ. เป็นต้น
        /// </summary>
        public class BudgetRequestExpensesProperty
        {
            /// <summary>
            /// เลขอ้างอิง Template ที่ใช้ทำคำขอ งปม.
            /// </summary>
            public int TEMPLATE_ID { get; set; }

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
            /// งบรายจ่าย อาทิเช่น งบดำเนินงาน งบลงทุน งบอุดหนุน เป็นต้น
            /// </summary>
            public int BUDGET_TYPE_ID { get; set; }


            /// <summary>
            /// หมวดค่าใช้จ่าย เช่น ค่าตอบแทน ค่าใช้สอย เป็นต้น
            /// </summary>
            public int EXPENSES_GROUP_ID { get; set; }

            /// <summary>
            /// ชื่อหมวดค่าใช้จ่าย
            /// </summary>
            public string EXPENSES_GROUP_NAME { get; set; }

            /// <summary>
            /// เลขที่กำกับรายการค่าใช้จ่าย งปม.
            /// </summary>
            public int EXPENSES_ID { get; set; }

            /// <summary>
            /// ชื่อรายการค่าใช้จ่าย
            /// </summary>
            public string EXPENSES_NAME { get; set; }

            /// <summary>
            /// จำนวนเงินที่ขอ งปม.
            /// </summary>
            public decimal? TOTAL_REQUEST_BUDGET { get; set; }

            /// <summary>
            /// รายละเอียดเพิ่มเติม ของแต่ละรายการค่าใช้จ่าย ซึ่งจะมีโครงสร้าง
            /// ข้อมูลที่แตกต่างกันไปตามแต่ละ รายการค่าใช้จ่าย ซึ่งโครงสร้างจะถูกเก็บในรูปแบบ Json string
            /// และนำไปแปลงเป็น Xml เก็บลงฐานข้อมูล
            /// พาร์ท: BudgetRequest/ExpensesColumnDefined/*.json
            /// </summary>
            public string EXPENSES_DESCRIBEs { get; set; }
        }


        /// <summary>
        /// คลาส Map ข้อมูลฟอร์มรายการครุภัณฑ์ ที่หน่วยงานบันทึก
        /// ระบบเก็บไว้เป็นประวัติคงคลัง
        /// </summary>
        public class OfficialEquipmentFormMapper
        {
            public List<OfficialEquipmentFormProperty> Items { get; set; }
        }

        public class OfficialEquipmentFormProperty
        {
            public short AssetId { get; set; }

            /// <summary>
            /// 1 = ราคามาตรฐาน, 0 = ไม่กำหนดราคามาตรฐาน
            /// </summary>
            public short PriceType { get; set; }

            /// <summary>
            /// ราคาวัสดุสำนักงาน
            /// </summary>
            public decimal PricePerUnit { get; set; }

            /// <summary>
            /// จำนวนวัสดุสำนักงาน
            /// </summary>
            public int StockAmounts { get; set; }
        }
    }
}