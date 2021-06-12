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
    [CustomAuthorize(Roles = "Admin")]
    public class PersonnelInformationController : Controller
    {
        // GET: PersonnelInformation
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_PERSONNEL_INFORMATION);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_PERSONNEL_INFORMATION;
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
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SORT_INDEX)
                    .Select(e => new DepartmentShortFieldProperty()
                    {
                        DEP_ID = e.DEP_ID,
                        DEP_NAME = e.DEP_NAME,
                        DEP_SHORT_NAME = e.DEP_SHORT_NAME
                    }).ToList();

                ViewBag.Positions = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PositionShortFieldProperty()
                    {
                        POSITION_ID = e.POSITION_ID,
                        POSITION_NAME = e.POSITION_NAME
                    }).ToList();
                ViewBag.PersonnelTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PersonnelTypeShortFieldProperty()
                    {
                        PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                        PERSON_TYPE_NAME = e.ITEM_TEXT
                    }).ToList();

                ViewBag.AreaName = db.T_AREAs.AsQueryable()
                    .Select(e => new AreaShortFieldProperty()
                    {
                        AREA_ID = e.AREA_ID,
                        AREA_NAME = e.AREA_NAME
                    }).ToList();
            }

            return View();
        }

        [HttpGet]
        public ActionResult GetModalForm()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                ViewBag.Departments = db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SORT_INDEX)
                    .Select(e => new DepartmentShortFieldProperty()
                    {
                        DEP_ID = e.DEP_ID,
                        DEP_NAME = e.DEP_NAME,
                        DEP_SHORT_NAME = e.DEP_SHORT_NAME
                    }).ToList();

                ViewBag.Positions = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PositionShortFieldProperty()
                    {
                        POSITION_ID = e.POSITION_ID,
                        POSITION_NAME = e.POSITION_NAME
                    }).ToList();
                ViewBag.PersonnelTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1)).OrderBy(e => e.SORT_INDEX)
                    .Select(e => new PersonnelTypeShortFieldProperty()
                    {
                        PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                        PERSON_TYPE_NAME = e.ITEM_TEXT
                    }).ToList();

                ViewBag.AreaName = db.T_AREAs.AsQueryable()
                    .Select(e => new AreaShortFieldProperty()
                    {
                        AREA_ID = e.AREA_ID,
                        AREA_NAME = e.AREA_NAME
                    }).ToList();

                ViewBag.Province = db.T_PROVINCEs.AsQueryable()
                    .Select(e => new ProvinceShortFieldProperty()
                    {
                        PROVINCE_NAME = e.PROVINCE_NAME
                    }).ToList();

                ViewBag.PersonnelPrefixs = db.T_PERSONNEL_PREFIXes.Where(e => e.ACTIVE.Equals(1)).ToList();
            }

            return View();
        }


        [HttpPost]
        public ActionResult Retrieve(int? depId, short? positionId, short? personTypeId, string personCode, string personName, short? areaId, int pageIndex, int pageSize)
        {
            // จัดเตรียมข้อมูลเพื่อตอบกลับ
            PaggingResultMapper pagging = new PaggingResultMapper()
            {
                rows = null,
                totalRecords = 0,
                totalPages = 0
            };

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.V_GET_PERSONNEL_INFORMATIONs.Where(e => e.ACTIVE.Equals(1));
                // ค้นหาตามหน่วยงาน
                if (depId != null)
                    expr = expr.Where(e => e.DEP_ID.Equals(depId));
                // ค้นหาตามตำแหน่งงาน
                if (positionId != null)
                    expr = expr.Where(e => e.POSITION_ID.Equals(positionId));
                // ค้นหาตามประเภทบุคลากร
                if (personTypeId != null)
                    expr = expr.Where(e => e.PERSON_TYPE_ID.Equals(personTypeId));
                if (areaId != null)
                    expr = expr.Where(e => e.AREA_ID.Equals(areaId));
                // ค้นหาตามรหัสพนักงาน
                if (!string.IsNullOrEmpty(personCode))
                    expr = expr.Where(e => e.PERSON_CODE.Contains(personCode));
                // ค้นหาตามชื่อ - นามสกุล
                if (!string.IsNullOrEmpty(personName))
                    expr = expr.Where(e => e.FIRST_NAME.Contains(personName) || e.LAST_NAME.Contains(personName));


                var finalExpr = expr.Select(e => new
                {
                    PERSON_ID = e.PERSON_ID,
                    PERSON_CODE = e.PERSON_CODE,
                    e.CARD_NUMBER,
                    e.PREFIX_NAME,
                    FIRST_NAME = e.FIRST_NAME,
                    LAST_NAME = e.LAST_NAME,
                    POSITION_ID = e.POSITION_ID,
                    POSITION_NAME = e.POSITION_NAME,
                    PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                    PERSON_TYPE_NAME = e.PERSON_TYPE_NAME,
                    DEP_ID = e.DEP_ID,
                    DEP_NAME = e.DEP_NAME,
                    e.SUB_DEP_ID,
                    e.SUB_DEP_NAME,
                    LAST_LOGIN_DATETIME = e.LAST_LOGIN_DATETIME,
                    EMAIL_ADDR = e.EMAIL_ADDR,
                    REGISTER_DATE = e.REGISTER_DATE,
                    ACC_TYPE = e.ACC_TYPE,
                    SEX_TYPE = e.SEX_TYPE,
                    ADDRESS = e.ADDRESS,
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = e.AREA_NAME,
                    STREET = e.STREET,
                    VILLAGE = e.VILLAGE,
                    DISTRICT = e.DISTRICT,
                    PREFEXTURE = e.PREFEXTURE,
                    PROVINCE_NAME = e.PROVINCE_NAME,
                    POST_CODE = e.POST_CODE,
                }).OrderBy(e => e.PERSON_ID);

                pagging.totalRecords = finalExpr.Count();
                pagging.totalPages = Math.Ceiling(pagging.totalRecords / Convert.ToDouble(pageSize));

                int offset = pageIndex * pageSize - pageSize;
                pagging.rows = finalExpr.Skip(offset).Take(pageSize).ToList();
            }

            return Json(pagging, JsonRequestBehavior.DenyGet);
        }


        private static object SubimtSaveLock = new object();
        [HttpPost]
        public ActionResult SubmitSave(PersonnelEditFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "errors", null },
                { "errorText", null },
            };
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Count > 0)
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            lock (SubimtSaveLock)
                using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                {
                    var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                    var personEntity = db.T_PERSONNELs.Where(e => e.ACTIVE.Equals(1) && e.PERSON_ID.Equals(model.PersonId)).FirstOrDefault();
                    if (null == personEntity)
                    {
                        personEntity = new T_PERSONNEL()
                        {
                            CREATED_DATETIME = DateTime.Now,
                            USER_ID = userAuthorizeProfile.EmpId,
                            PWD_HASH = BCrypt.Net.BCrypt.HashPassword("1234"),
                            PREFIX_NAME = "นาย",
                            ACTIVE = 1
                        };
                        db.T_PERSONNELs.InsertOnSubmit(personEntity);

                        if (!string.IsNullOrEmpty(model.EmailAddr) && db.T_PERSONNELs.Where(e => e.ACTIVE.Equals(1) && e.EMAIL_ADDR.Equals(model.EmailAddr)).Count() > 0)
                        {
                            res["errorText"] = "อีเมล์ที่ระบุซ้ำกับที่มีอยู่แล้ว";
                            return Json(res, JsonRequestBehavior.DenyGet);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(model.EmailAddr) && !model.EmailAddr.Equals(model.OldEmailAddr) && db.T_PERSONNELs.Where(e => e.ACTIVE.Equals(1) && e.EMAIL_ADDR.Equals(model.EmailAddr)).Count() > 0)
                        {
                            res["errorText"] = "อีเมล์ที่ระบุซ้ำกับที่มีอยู่แล้ว";
                            return Json(res, JsonRequestBehavior.DenyGet);
                        }
                        personEntity.UPDATED_DATETIME = DateTime.Now;
                        personEntity.UPDATED_ID = userAuthorizeProfile.EmpId;
                    }



                    // บันทึกการเปลี่ยนแปลงข้อมูลพนักงาน
                    personEntity.DEP_ID = model.DepId;
                    personEntity.SUB_DEP_ID = model.SubDepId;
                    personEntity.POSITION_ID = model.PositionId;
                    personEntity.PERSON_TYPE_ID = model.PersonTypeId;
                    personEntity.CARD_NUMBER = model.CardNumber;
                    personEntity.PREFIX_NAME = model.PrefixName;
                    personEntity.FIRST_NAME = model.FirstName;
                    personEntity.LAST_NAME = model.LastName;
                    personEntity.PERSON_CODE = model.PersonCode;
                    personEntity.SEX_TYPE = model.SexType[0];
                    personEntity.ACC_TYPE = model.AccountType;
                    if (model.CancelFlag.Equals(1))
                        personEntity.ACTIVE = -1;

                    personEntity.EMAIL_ADDR = model.EmailAddr;

                    personEntity.ADDRESS = model.Address;
                    personEntity.AREA_ID = model.AreaId.Value;
                    personEntity.STREET = model.Street;
                    personEntity.VILLAGE = model.Village;
                    personEntity.DISTRICT = model.Distinct;
                    personEntity.PREFEXTURE = model.Prefexture;
                    personEntity.PROVINCE_NAME = model.ProvinceName;
                    personEntity.POST_CODE = model.PostCode;

                    db.SubmitChanges();
                }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class PersonnelEditFormMapper
        {
            public int? PersonId { get; set; }

            /// <summary>
            /// หน่วยงานที่ผู้ใช้งานสังกัด
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public int DepId { get; set; }

            /// <summary>
            /// หน่วยงานภายในที่ผู้ใช้งานสังกัด
            /// </summary>
            [Range(1, int.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public int? SubDepId { get; set; }

            /// <summary>
            /// ตำแหน่งงาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, short.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public short PositionId { get; set; }

            /// <summary>
            /// ประเภทพนักงาน อาทิเช่น พนักงานราชการ ข้าราชการ เป็นต้น
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), Range(1, short.MaxValue, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public short PersonTypeId { get; set; }

            /// <summary>
            /// รหัสบัตรประชาชน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(14, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string CardNumber { get; set; }

            /// <summary>
            /// รหัสพนักงาน อ้างอิงจากระบบ AD, สำหรับใช้ดึงข้อมูลเวลาเข้าออกจากเครื่องลงเวลา
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(5, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string PersonCode { get; set; }

            /// <summary>
            /// คำนำหน้าชื่อ
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(50, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string PrefixName { get; set; }

            /// <summary>
            /// ชื่อพนักงาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string FirstName { get; set; }

            /// <summary>
            /// นามสกุลพนักงาน
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string LastName { get; set; }

            /// <summary>
            /// เพศของบุคลากร
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(1, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string SexType { get; set; }

            /// <summary>
            /// 1 = ต้องการยกเลิกพนักงานนี้ออกจากระบบ, 0 = ไม่ต้องทำอะไร
            /// </summary>
            [Range(0, 1, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public short CancelFlag { get; set; }

            /// <summary>
            /// 1 = Super User, 0 = ผู้ใช้งานทั่วไป
            /// </summary>
            [Range(0, 1, ErrorMessage = "ค่าต้องอยู่ระหว่าง {1} - {2}")]
            public short AccountType { get; set; }

            /// <summary>
            /// อีเมล์สำหรับใช้ในการเข้าสู่ระบบในกรณีที่ SSO ใช้งานไม่ได้
            /// </summary>
            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), EmailAddress(ErrorMessage = "ระบุรูปแบบอีเมล์ไม่ถูกต้อง"), MaxLength(150, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string EmailAddr { get; set; }

            /// <summary>
            /// ใช้สำหรับตรวจสอบค่าตอนที่แก้ไข
            /// เพื่อดูว่ามีอีเมล์ซ้ำกันคนอื่น หรือไม่
            /// </summary>
            public string OldEmailAddr { get; set; }

            public string Address { get; set; }

            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน")]
            public short? AreaId { get; set; }

            public string Street { get; set; }
            public string Village { get; set; }
            public string Distinct { get; set; }
            public string Prefexture { get; set; }


            [Required(ErrorMessage = "โปรดระบุค่านี้ก่อน"), MaxLength(50, ErrorMessage = "ความยาวต้องไม่เกิน {1} ตัวอักษร")]
            public string ProvinceName { get; set; }

            public string PostCode { get; set; }
        }
    }
}