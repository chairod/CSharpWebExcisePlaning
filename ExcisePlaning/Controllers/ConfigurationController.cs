using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// จัดการค่าคงที่ ที่ใช้งานในระบบ ได้แก่ <para/>
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class ConfigurationController : Controller
    {

        // GET: Configuration
        [HttpGet]
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_UNDERLYING_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_UNDERLYING_CONFIGURE;
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


            // ค่าคงในที่ในระบบ
            using (Entity.ExcisePlaningDbDataContext db = new Entity.ExcisePlaningDbDataContext())
            {
                var configurations = db.T_CONFIGURATIONs.Where(e => 1.Equals(e.ACTIVE)).ToList();
                ViewBag.Configurations = configurations;
            }

            return View("Configuration_Form");
        }


        [HttpPost, Route("configId:int?, useFlag:string, pageIndex:int, pageSize:int")]
        public ActionResult RetrieveConfig(int? configId, string useFlag, int pageIndex, int pageSize)
        {
            // จัดเตรียมผลลัพธ์ เพื่อตอบกลับ
            PaggingResultMapper result = new PaggingResultMapper()
            {
                rows = null,
                totalPages = 0,
                totalRecords = 0
            };

            // ค้นหาข้อมูล ตามเงื่อนไข
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var currDate = DateTime.Now.Date;

                var expr = db.V_GET_CONFIGURATIONs.Where(e => e.ACTIVE == 1); // เฉพาะรายการที่ Active
                if (configId.HasValue)
                    expr = expr.Where(e => e.CONFIG_ID.Equals(configId.Value));
                if ("Y".Equals(useFlag))
                    expr = expr.Where(e => db.V_GET_CURR_CONFIGURATION_USINGs.Where(c => c.CONFIG_DETAIL_ID.Equals(e.CONFIG_DETAIL_ID)).Any());

                int offset = pageIndex * pageSize - pageSize;
                var rows = expr.Select(e => new
                {
                    CONFIG_ID = e.CONFIG_DETAIL_ID,
                    CONFIG_DETAIL_ID = e.CONFIG_DETAIL_ID,
                    CONFIG_NAME = e.CONFIG_NAME,e.CONFIG_DESCRIPTION,
                    CAN_DELETE_FLAG = e.EFFECTIVE_DATE.CompareTo(currDate) != -1 ? "Y" : "N",
                    USING_FLAG = "Y".Equals(useFlag) ? 1 : (db.V_GET_CURR_CONFIGURATION_USINGs.Where(c => c.CONFIG_DETAIL_ID.Equals(e.CONFIG_DETAIL_ID)).Any() ? 1 : 0),
                    ITEM_DESCRIPTION = e.ITEM_DESCRIPTION,
                    EFFECTIVE_DATE = e.EFFECTIVE_DATE,
                    EXPIRE_DATE = e.EXPIRE_DATE,
                    ITEM_VALUE = e.ITEM_VALUE,
                    ITEM_VALUE2 = e.ITEM_VALUE2,
                    ITEM_VALUE3 = e.ITEM_VALUE3,
                    CREATED_DATETIME = e.CREATED_DATETIME,
                    UPDATED_DATETIME = e.UPDATED_DATETIME
                }).OrderBy(e => e.CONFIG_NAME).ThenBy(e => e.EFFECTIVE_DATE).ToList();

                result.totalRecords = rows.Count();
                result.totalPages = Convert.ToInt32(Math.Ceiling(result.totalRecords / Convert.ToDouble(pageSize)));
                result.rows = rows.Skip(offset).Take(pageSize).ToList();
            }

            return Json(result, JsonRequestBehavior.DenyGet);
        }


        /// <summary>
        /// ลบการตั้งค่าออกจากระบบ ซึ่งจะสามารถลบได้เฉพาะรายการที่ยังไม่ถึง Effective Date (EffectiveDate > Current Date) <para/>
        /// หมายเหตุ: รายการอื่นๆที่มี Effective Date มากกว่ารายการที่กำลังลบ จะถูกยกเลิกตามไปด้วย เพราะป้องกันกรณีที่ลบรายการ ครึ่งๆ กลางๆ
        /// </summary>
        /// <param name="configDetailId"></param>
        [HttpPost, Route("configDetailId:int?")]
        public void SubmitDelete(int? configDetailId)
        {
            if (!configDetailId.HasValue)
                return;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var configEntity = db.T_CONFIGURATION_DETAILs.Where(e => e.CONFIG_DETAIL_ID.Equals(configDetailId.Value) && e.ACTIVE.Equals(1)).FirstOrDefault();
                if (null != configEntity && configEntity.EFFECTIVE_DATE.CompareTo(DateTime.Now.Date) != -1 && !configEntity.ACTIVE.Equals(-1))
                {
                    UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                    configEntity.ACTIVE = -1;
                    configEntity.UPDATED_DATETIME = DateTime.Now;
                    configEntity.UPDATED_ID = userAuthorizeProfile.EmpId;

                    // ค้นหารายการที่มี Effective Date มากกว่ารายการที่กำลังยกเลิก และ จะต้อง Active = 1
                    var sideEffectEntites = db.T_CONFIGURATION_DETAILs.Where(e => e.ACTIVE.Equals(1) && e.EFFECTIVE_DATE > configEntity.EFFECTIVE_DATE).ToList();
                    foreach (var sideEffectEntity in sideEffectEntites)
                    {
                        sideEffectEntity.ACTIVE = -1;
                        sideEffectEntity.UPDATED_DATETIME = DateTime.Now;
                        sideEffectEntity.UPDATED_ID = userAuthorizeProfile.EmpId;
                    }

                    db.SubmitChanges();
                }
            }
        }


        [HttpGet]
        public ActionResult GetModalResource()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ค่าคงที่ในระบบ
                ViewBag.Configurations = db.T_CONFIGURATIONs.Where(e => e.ACTIVE.Equals(1)).ToList();
                return View("_Configuration_Modal_Form");
            }
        }

        private DateTime GetNextEffectiveDateByConfigId(int configId)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var configEntity = db.T_CONFIGURATION_DETAILs.Where(e => e.CONFIG_ID.Equals(configId) && e.ACTIVE == 1)
                        .OrderByDescending(e => e.EFFECTIVE_DATE)
                        .FirstOrDefault();
                DateTime nextEffectivedate = DateTime.Now.Date;
                if (configEntity != null)
                    nextEffectivedate = configEntity.EFFECTIVE_DATE.AddDays(1);

                return nextEffectivedate;
            }
        }

        [HttpPost, Route("configId:int?")]
        public ActionResult GetEffectiveDate(int? configId)
        {
            if (!configId.HasValue)
                return Json(new Dictionary<string, string>(1) {
                    { "EffectiveDateStr", ""} }, JsonRequestBehavior.DenyGet);

            var nextEffectiveDate = GetNextEffectiveDateByConfigId(configId.Value);
            return Json(new Dictionary<string, object>(2) {
                    { "EffectiveDateStr", nextEffectiveDate.ToString("dd/MM/yyyy") },
                    { "EffectiveDate", nextEffectiveDate }
            }, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public ActionResult SubmitSave(ConfigurationFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>() { { "errors", null } };

            // ตรวจสอบการระบุค่า จากผู้ใช้งาน ถูกต้องหรือไม่
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0)
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ตรวจสอบวันที่มีผลบังคับใช้
            DateTime userNextEffectiveDate = AppUtils.TryValidUserDateStr(model.EffectiveDate);
            if (userNextEffectiveDate.Equals(DateTime.MinValue))
            {
                modelErrors.Add("EffectiveDate", new ModelValidateErrorProperty()
                {
                    FieldName = "EffectiveDate",
                    ErrorMessages = new List<string>(1) { { "รูปแบบวันที่ไม่ถูกต้อง" } }
                });
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // วันที่มีผลบังคับใช้ ต้องไม่น้อยกว่าที่ระบบคำนวณให้
            var nextEffectiveDate = GetNextEffectiveDateByConfigId(model.ConfigId);
            if (userNextEffectiveDate.CompareTo(nextEffectiveDate) == -1)
            {
                modelErrors.Add("EffectiveDate",
                    new ModelValidateErrorProperty("EffectiveDate", new List<string>() {
                        string.Format("วันที่มีผลบังคับใช้จะต้องไม่น้อยกว่า {0}", nextEffectiveDate.ToString("dd/MM/yyyy"))
                    }));
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);

                // ค้นหารายการก่อนหน้าเพื่ออัพเดต ExpireDate ของรายการ
                var prevEntity = db.T_CONFIGURATION_DETAILs.Where(e => e.ACTIVE == 1
                        && e.CONFIG_ID.Equals(model.ConfigId)
                        ).OrderByDescending(e => e.EFFECTIVE_DATE)
                        .FirstOrDefault();
                if (null != prevEntity)
                {
                    var expireDate = userNextEffectiveDate.AddDays(-1);
                    prevEntity.EXPIRE_DATE = expireDate;
                    prevEntity.UPDATED_DATETIME = DateTime.Now;
                    prevEntity.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                // สร้างรายการค่าคงที่ใหม่
                db.T_CONFIGURATION_DETAILs.InsertOnSubmit(new T_CONFIGURATION_DETAIL()
                {
                    CONFIG_ID = model.ConfigId,
                    ITEM_VALUE = model.ItemValue1,
                    ITEM_VALUE2 = model.ItemValue2,
                    ITEM_VALUE3 = model.ItemValue3,
                    ITEM_DESCRIPTION = model.RemarkText,
                    ACTIVE = 1,
                    EFFECTIVE_DATE = userNextEffectiveDate,
                    EXPIRE_DATE = null,
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId
                });
                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }


        public class ConfigurationFormMapper
        {
            public ConfigurationFormMapper() { }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public int ConfigId { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            [MaxLength(120, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string ItemValue1 { get; set; }
            
            [MaxLength(120, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string ItemValue2 { get; set; }
            
            [MaxLength(120, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string ItemValue3 { get; set; }
            
            [MaxLength(200, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string RemarkText { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public string EffectiveDate { get; set; }
        }
    }
}