using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;

namespace ExcisePlaning.Controllers
{
    [AllowAnonymous]
    public class AuthorizeController : Controller
    {
        [HttpGet]
        public ActionResult LoginForm()
        {
            return View();
        }

        /// <summary>
        /// ส่ง User & Pwd เข้าไปตรวจสอบใน AD (Active Directory) และหากตรวจสอบผ่าน<para/>
        /// 1.) ใช้ user name ส่งเข้าไปผ่าน WebService เพื่อร้องขอข้อมูลจาก Staff Directory
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userPass"></param>
        /// <returns></returns>
        [HttpPost, Route("userName:string, userPass:string")]
        public ActionResult LoginForm(string userName, string userPass)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPass))
            {
                ViewBag.ErrorMessage = "โปรดระบุ ผู้ใช้งาน และ รหัสผ่าน ให้ครบถ้วนก่อน";
                return View("LoginForm");
            }
            ViewBag.UserName = userName;


            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            string cardNumber = "", firstName = "", lastName = ""
                , emailAddr = "", personCode = "";
            if ("SSO".Equals(appSetting.LoginType))
            {
                using (ServiceReference1.LDPAGAuthenAndGetUserRolePortTypeClient client = new ServiceReference1.LDPAGAuthenAndGetUserRolePortTypeClient())
                {
                    ServiceReference1.AuthenAndGetUserRoleRequest request = new ServiceReference1.AuthenAndGetUserRoleRequest();
                    request.userId = userName;
                    request.password = userPass;
                    request.applicationId = appSetting.SSOApplicationId;
                    var result = client.LDPAGAuthenAndGetUserRoleOperation(request);
                    if (!result.message.success)
                    {
                        ViewBag.ErrorMessage = string.Format("ผู้ใช้งาน และ รหัสผ่าน ไม่ถูกต้อง ({0}: {1})", result.message.code, result.message.description);
                        return View("LoginForm");
                    }

                    cardNumber = result.userThaiId; // เลขบัตรประชาชนจาก SSO เพื่อนำไป Mapping กับระบบ
                    firstName = result.userThaiName;
                    lastName = result.userThaiSurname;
                    emailAddr = result.email;
                    personCode = result.userId; // รหัสผู้ใช้งาน
                }
            }
            //using (DirectoryEntry entry = new DirectoryEntry(appSetting.ActiveDirectoryDomain, userName, userPass))
            //{
            //    DirectorySearcher searcher = new DirectorySearcher(entry);

            //    //searcher.Filter = "(objectclass=user)";
            //    searcher.Filter = string.Format("(sAMAccountName={0})", userName);
            //    searcher.PropertiesToLoad.Add("name"); // ค้นหาชื่อของพนักงานที่กำลังตรวจสอบ
            //    searcher.PropertiesToLoad.Add("employeeNumber"); // ขอข้อมูลรหัสพนักงานจากเครื่อง AD

            // ถ้า Login จาก ActiveDirectory ไม่ผ่านจะเข้า Catch Exception
            //try
            //{
            //var result = searcher.FindOne();
            //DirectoryEntry dsresult = result.GetDirectoryEntry();
            //string personFullName = "";
            //string personCode = "";
            //if (null != dsresult.Properties["name"].Value)
            //    personFullName = dsresult.Properties["name"].Value.ToString().Trim();
            //if (null != dsresult.Properties["employeeNumber"].Value)
            //    personCode = dsresult.Properties["employeeNumber"].Value.ToString().Trim();


            // เชื่อมต่อไปยัง WebService เพื่อขอข้อมูลโปรไฟล์ของผู้ใช้งาน
            //StaffDirectoryServices.User staffDirectoryUsrProfile = null;
            //using (GWServiceSoapClient ws = new GWServiceSoapClient())
            //    staffDirectoryUsrProfile = ws.GetUserByLoginAD(appSetting.StaffDirectoryPasswordService, userName);


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                //var configResult = db.proc_GetConfigureCurrActiveByConst(AppConfigConst.WITHOUT_LOGIN_TOKEN).ToList();
                //if (null == configResult || !configResult.Any())
                //    return AppConstNotDefined("โปรดติดต่อผู้ดูแลระบบให้ตั้งค่า Token สำหรับเชื่อมต่อจากภายนอก");

                // ค้นหาข้อมูลบุคลากร เพื่อนำมาปรับปรุงข้อมูลที่ได้จาก Staff Directory
                if ("SSO".Equals(appSetting.LoginType))
                {
                    var personExpr = db.T_PERSONNELs.Where(e => e.CARD_NUMBER.Equals(cardNumber)).FirstOrDefault();
                    bool isNewPerson = null == personExpr;
                    if (isNewPerson)
                    {
                        personExpr = new T_PERSONNEL()
                        {
                            DEP_ID = 1, // ค่าเริ่มต้นเป็น Fusion sole (ระบบจะบังคับให้เปลี่ยน)
                            PERSON_TYPE_ID = 1, // ค่าเริ่มต้นเป็น ข้าราชการ (ระบบจะบังคับให้เปลี่ยน)
                            POSITION_ID = 27, // ค่าเริ่มต้นเป็น เจ้าหน้าที่ธุระการ (ระบบจะบังคับให้เปลี่ยน)
                            LEVEL_ID = 1, // ค่าเริ่มต้นเป็น ปฏิบัติการ (ระบบจะบังคับให้เปลี่ยน)
                            SEX_TYPE = 'M', // ค่าเริ่มต้นเป็น เพศชาย (ระบบจะบังคับให้เปลี่ยน)
                            PREFIX_NAME = "นาย", // ค่าเริ่มต้นเป็น นาย (ระบบจะบังคับให้เปลี่ยน)
                            ACC_TYPE = 0, // 1 = ผู้ใช้งานที่สามารถดูข้อมูลได้ทุกคน, 0 = ผู้ใช้งานทั่วไป

                            CARD_NUMBER = cardNumber,
                            CREATED_DATETIME = DateTime.Now,
                            USER_ID = 0,
                            ACTIVE = 0
                        };
                        db.T_PERSONNELs.InsertOnSubmit(personExpr);
                    }

                    // แก้ไขชื่อผู้ใช้งาน ให้ตรงกับ SSO
                    personExpr.FIRST_NAME = firstName;
                    personExpr.LAST_NAME = lastName;
                    personExpr.EMAIL_ADDR = emailAddr;
                    personExpr.PERSON_CODE = personCode;
                    db.SubmitChanges();

                    //ViewBag.ErrorMessage = string.Format("card: {0}, first: {1}, last: {2}, person: {3}, new: {4}"
                    //    , cardNumber, firstName, lastName, personCode
                    //    , isNewPerson);
                    //return View("LoginForm");
                    if (isNewPerson || (null != personExpr && personExpr.ACTIVE == 0))
                    {
                        // ถ้าเป็นผู้ใช้งานที่เคยเข้าระบบเป็นครั้งแรก
                        // ให้เพิ่มสิทธิ์กลุ่มผู้ใช้งานตามค่า Default ที่กำหนดในระบบ (Config file)
                        if (isNewPerson)
                        {
                            db.T_PERSONNEL_AUTHORIZEs.InsertOnSubmit(new T_PERSONNEL_AUTHORIZE()
                            {
                                PERSON_ID = personExpr.PERSON_ID,
                                ROLE_ID = short.Parse(appSetting.DefaultRoleIdForNewAccountStr),
                                ACTIVE = 1,
                                CREATED_DATETIME = DateTime.Now,
                                USER_ID = 0
                            });
                            db.SubmitChanges();
                        }
                        return RedirectToAction("InitNewAccount", new { cardNumber = cardNumber });
                    }
                }
                else
                {
                    var personExpr = db.T_PERSONNELs.Where(e => e.EMAIL_ADDR.Equals(userName)).FirstOrDefault();
                    if (null == personExpr)
                    {
                        ViewBag.ErrorMessage = "ไม่พบ ชื่อ-นามสกุล ของท่านในระบบ";
                        return View("LoginForm");
                    }

                    string passwordHash = personExpr.PWD_HASH;
                    if (!BCrypt.Net.BCrypt.Verify(userPass, passwordHash))
                    {
                        ViewBag.ErrorMessage = "ไม่พบ ชื่อ-นามสกุล ของท่านในระบบ";
                        return View("LoginForm");
                    }

                    personCode = personExpr.PERSON_CODE;
                }



                // ปรับปรุงข้อมูลบุคลากร ให้ตรงกับระบบ Staff Directory
                // การปรับปรุงข้อมูลส่วนอื่นๆ จะใช้ LeaveAppConsole ในการปรับปรุง
                //personExpr.PERSON_CODE = staffDirectoryUsrProfile.Empcode;
                //personExpr.PREFIX_NAME = staffDirectoryUsrProfile.PrefixName;
                //personExpr.FIRST_NAME = staffDirectoryUsrProfile.FirstName;
                //personExpr.LAST_NAME = staffDirectoryUsrProfile.LastName;
                //personExpr.REGISTER_DATE = DateTime.Parse(staffDirectoryUsrProfile.EnterWorkDate);
                //personExpr.ACTIVE = Convert.ToInt16(staffDirectoryUsrProfile.IsActive.Equals("true") ? 1 : 0);
                //personExpr.SEX_TYPE = staffDirectoryUsrProfile.Sex.Equals("true") ? 'F' : 'M'; // F = หญิง, M = ชาย
                //db.SubmitChanges();

                // ส่งคำร้องไปยัง CheckPermission
                // เพื่อตรวจสอบมอบหมายสิทธิ์ต่างๆ อาทิเช่น เมนู สิทธิ์ตามตำแหน่งงาน เป็นต้น
                return RedirectToAction("CheckPermission", "Authorize", new
                {
                    empCode = personCode, //personExpr.PERSON_CODE,
                    accessToken = appSetting.AccessToken
                });

                // นำชื่อผู้ใช้งานที่ได้ แยกชื่อ นามสกุล ออกจากกัน และนำไปค้นหาในฐานข้อมูล
                //string[] nameParts = personFullName.Split(new char[] { ' ' });
                //string firstName = nameParts.Length > 0 ? nameParts[0].Trim() : "",
                //    sureName = nameParts.Length > 1 ? nameParts[1].Trim() : "";
                //var personEntities = db.T_PERSONNELs.Where(e => e.PERSON_CODE.Equals(personCode))
                //        .Select(e => new
                //        {
                //            PERSON_CODE = e.PERSON_CODE,
                //            ACTIVE = e.ACTIVE
                //        }).ToList();


                //// ตรวจสอบความถูกต้องของข้อมูลบุคลากร
                //if (personEntities.Count == 0)
                //{
                //    ViewBag.ErrorMessage = "ไม่พบ ชื่อ-นามสกุล ของท่านในระบบลา โปรดแจ้งผู้ดูแลระบบ";
                //    return View("LoginForm");
                //}
                //else
                //{
                //    // พบ ชื่อ - นามสกุล เดียวกันมากกว่า 1 คน
                //    if (personEntities.Count > 1)
                //    {
                //        ViewBag.ErrorMessage = string.Format("ชื่อ {0} {1} ในระบบมีมากกว่า 1 คน โปรดแจ้งผู้ดูแลระบบ", firstName, sureName);
                //        return View("LoginForm");
                //    }
                //    else if (!personEntities[0].ACTIVE.Equals(1))
                //    {
                //        ViewBag.ErrorMessage = "ท่านถูกยกเลิกการใช้งานจากระบบแล้ว โปรดติดต่อผู้ดูแลระบบ";
                //        return View("LoginForm");
                //    }

                //    // ปรับปรุงชื่อบุคลาการ
                //    // เนื่องจาก บางส่วนได้ข้อมูลจากเครื่องลงเวลา ชื่อจะถูกตัดไป
                //    var personEntity = db.T_PERSONNELs.Where(e => e.PERSON_CODE.Equals(personCode)).FirstOrDefault();
                //    if(null != personEntity)
                //    {
                //        personEntity.FIRST_NAME = firstName;
                //        personEntity.LAST_NAME = sureName;
                //        personEntity.UPDATED_DATETIME = DateTime.Now;
                //        personEntity.UPDATED_ID = 1; // Admin
                //        db.SubmitChanges();
                //    }

                //    return RedirectToAction("CheckPermission", "Authorize", new
                //    {
                //        empCode = personEntities[0].PERSON_CODE,
                //        accessToken = configResult.First().ITEM_VALUE
                //    });
                //}
            }
            //}
            //catch (COMException ex)
            //{
            //    // -2147016646: Server not response
            //    // -2147023570: Password Incorrrect
            //    if (ex.ErrorCode.Equals(-2147016646))
            //        ViewBag.ErrorMessage = "ไม่สามารถเชื่อมต่อไปยังระบบ AD ได้ โปรดติดต่อผู้ดูแลระบบ";
            //    else if (ex.ErrorCode.Equals(-2147023570))
            //        ViewBag.ErrorMessage = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง โปรดตรวจสอบ";
            //    else
            //        ViewBag.ErrorMessage = string.Format("ข้อผิดพลาด {0}: {1}", ex.ErrorCode, ex.Message);
            //}
            //catch (SoapException ex)
            //{
            //    ViewBag.ErrorMessage = string.Format("โปรดติดต่อผู้พัฒนาระบบ Staff Directory โดยนำข้อผิดพลาดนี้แจ้งปัญหา: {0}", ex.Message);
            //}
            //}

            //return View("LoginForm");
        }

        /// <summary>
        /// ตรวจสอบสิทธิ์การเข้าใช้งาน ระบบด้วย empCode, token <para />
        /// ระบบลูกค้า จะสร้างลิ้งการเข้ามาใช้งานระบบ จากส่วนกลางและให้คลิกลิ้งเข้ามา <para />
        /// </summary>
        /// <param name="empCode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        // GET: Authorize
        [HttpGet, Route("{empCode:string},{accessToken:string}")]
        public ActionResult CheckPermission(string empCode, string accessToken)
        {
            if (string.IsNullOrEmpty(empCode) || string.IsNullOrEmpty(accessToken))
                return Unauthorize();

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                //var result = db.proc_GetConfigureCurrActiveByConst(AppConfigConst.WITHOUT_LOGIN_TOKEN).ToList();
                //if (null == result || !result.Any())
                //    return AppConstNotDefined("โปรดติดต่อผู้ดูแลระบบให้ตั้งค่า Token สำหรับเชื่อมต่อจากภายนอก");

                // ตรวจสอบสิทธิ์การเข้าถึงระบบ
                var appSettings = AppSettingProperty.ParseXml();
                string authorizeToken = appSettings.AccessToken;
                var empEntity = db.T_PERSONNELs.Where(e => e.ACTIVE.Equals(1) && e.PERSON_CODE.Equals(empCode)).FirstOrDefault();
                if (!authorizeToken.Equals(accessToken) || empEntity == null)
                    return View("CheckPermissionError");


                // มีการกำหนด รอบ/กะงานหรือยัง
                //if (null == empEntity.PERIOD_ID)
                //{
                //    ViewBag.ErrorMessage = "โปรดแจ้ง ชื่อ-นามสกุล หรือ รหัสพนักงาน ให้กับผู้ดูแลระบบเพื่อ [รอบ/กะงาน] ให้กับท่าน";
                //    return View("CheckPermissionError");
                //}


                // ค้นหาการกำหนดสิทธิ์ ในตำแหน่งงาน
                // ตำแหน่งงานแต่ล่ะ ตำแหน่ง จะมีบทบาทหน้าที่รับผิดชอบที่แตกต่างกัน อาทิเช่น
                // อนุมัติลาได้บางประเภท ก่อนการอนุมัติจะต้องได้รับมอบหมายหน้าที่ก่อน 
                // รับผิดชอบหน่วยงานภายใต้ตำแหน่งหน้าที่มากกว่า 1 หน่วยงาน (บางตำแหน่งเพียงหน่วยงานเดียว) เป็นต้น
                var positionEntity = db.T_POSITIONs.Where(e => e.POSITION_ID.Equals(empEntity.POSITION_ID))
                                       .Select(e => new
                                       {
                                           e.POSITION_NAME,
                                           e.ACTIVE
                                       }).FirstOrDefault();
                //if (null == positionEntity || !positionEntity.ACTIVE.Equals(1))
                //{
                //    ViewBag.ErrorMessage = "โปรดแจ้ง ชื่อ-นามสกุล หรือ รหัสพนักงาน ให้กับผู้ดูแลระบบเพื่อกำหนด [ตำแหน่งงาน] ให้กับท่าน";
                //    return View("CheckPermissionError");
                //}


                // ค้นหาเมนูที่ให้สิทธิ์การเข้าถึงไว้
                var menuExpr = db.proc_GetUserMenu(empEntity.PERSON_ID).ToList();
                if (!menuExpr.Any())
                {
                    ViewBag.ErrorMessage = "โปรดแจ้งผู้ดูแลระบบ กำหนดสิทธิ์การใช้งานเมนูในระบบให้กับบัญชีของท่านก่อน";
                    return View("CheckPermissionError");
                }
                var userMenus = menuExpr.GroupBy(e => new { e.GROUP_NAME, e.GROUP_ICON }).Select(e => new UserMenuGroupProperty()
                {
                    GroupName = e.Key.GROUP_NAME,
                    GroupIcon = e.Key.GROUP_ICON,
                    UserMenus = e.Select(m => new UserAuthorizeMenuProperty()
                    {
                        MenuName = m.MENU_NAME,
                        MenuDescription = m.MENU_DESCRIPTION,
                        MenuIcon = m.MENU_ICON,
                        MenuConst = m.MENU_CONST,
                        RouteName = m.ROUTE_CONTROLLER,
                        ActionName = m.ROUTE_METHOD,
                        QueryString = m.QUERY_STRING
                    }).ToList()
                }).ToList();

                // สร้าง One-time token สำหรับให้แต่ละบัญชีผู้ใช้งาน
                string oneTimeToken = AppUtils.GetMD5Value(string.Format("{0}.{1}.{2}", empEntity.PERSON_ID, empEntity.PERSON_CODE, DateTime.Now.Ticks));

                // ก่อนเขียนไฟล์ใหม่ ให้ลบ Authorize profile เดิมทิ้งก่อน
                string oldFile = string.Format("{0}/{1}.authorize", appSettings.UserAuthorizeCachePath, empEntity.ONE_TIME_TOKEN);
                if (!string.IsNullOrEmpty(empEntity.ONE_TIME_TOKEN) && System.IO.File.Exists(oldFile))
                    System.IO.File.Delete(oldFile);


                var depExpr = db.T_DEPARTMENTs.Where(d => d.DEP_ID.Equals(empEntity.DEP_ID) && d.ACTIVE.Equals(1)).Select(d => new
                {
                    d.DEP_NAME,
                    d.AREA_ID,
                    d.DEP_AUTHORIZE,
                    d.CAN_REQUEST_BUDGET
                }).FirstOrDefault();
                if (null == depExpr)
                {
                    ViewBag.ErrorMessage = "โปรดแจ้งผู้ดูแลระบบ กำหนดหน่วยงานที่ท่านสังกัด";
                    return View("LoginForm");
                }

                // สร้าง User profile และเก็บลงไฟล์ เพื่อใช้งานในระบบ โดยไม่ต้อง Query ข้อมูลจาก DB ใหม่
                string file = string.Format("{0}/{1}.authorize", appSettings.UserAuthorizeCachePath, oneTimeToken);
                UserAuthorizeProperty userProp = new UserAuthorizeProperty()
                {
                    EmpCode = empEntity.PERSON_CODE,
                    DefaultFiscalYear = AppUtils.GetCurrYear(),
                    EmpId = empEntity.PERSON_ID,
                    EmpFullname = string.Format("{0}{1} {2}", empEntity.PREFIX_NAME, empEntity.FIRST_NAME, empEntity.LAST_NAME),
                    PositionId = empEntity.POSITION_ID,
                    PositionName = null == positionEntity ? "" : positionEntity.POSITION_NAME,
                    PersonTypeId = empEntity.PERSON_TYPE_ID,
                    MenuGroups = userMenus,
                    AreaId = depExpr.AREA_ID,
                    DepId = empEntity.DEP_ID,
                    DepName = depExpr.DEP_NAME,
                    DepAuthorize = depExpr.DEP_AUTHORIZE,
                    SubDepId = empEntity.SUB_DEP_ID,
                    // สร้างคำขอเงินงบประมาณได้หรือไม่
                    CanCreateRequestBudget = depExpr.CAN_REQUEST_BUDGET,
                    // หน่วยงานที่อยู่ภายใต้ความรับผิดชอบของ หน่วยงานนี้
                    AssignDepartmentIds = db.T_DEPARTMENT_AUTHORIZEs.Where(x => x.DEP_ID.Equals(empEntity.DEP_ID)).Select(e => e.AUTHORIZE_DEP_ID).ToList(),
                    SexType = empEntity.SEX_TYPE,
                    AccountType = empEntity.ACC_TYPE,
                    EmailAddr = empEntity.EMAIL_ADDR,
                    MobileNo = empEntity.MOBILE_NO,
                    RoleNames = db.proc_GetUserRoles(empEntity.PERSON_ID).ToList().Select(e => e.ROLE_CONST).ToList()
                };

                try
                {
                    // Exclusive write file
                    using (Stream writer = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        writer.SetLength(0);
                        writer.Flush();
                        byte[] buffer = UTF8Encoding.UTF8.GetBytes(AppUtils.ToJson(userProp));
                        writer.Write(buffer, 0, buffer.Length);
                        writer.Flush();
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }

                // ปรับปรุงเวลาเข้าระบบล่าสุด
                empEntity.LAST_LOGIN_DATETIME = DateTime.Now;
                empEntity.LAST_ACTION_DATETIME = DateTime.Now;
                empEntity.ONE_TIME_TOKEN = oneTimeToken;
                db.SubmitChanges();


                FormsAuthentication.SetAuthCookie(oneTimeToken, false);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        /// <summary>
        /// ออกจากระบบ
        /// </summary>
        /// <returns></returns>
        public ActionResult Logout()
        {
            string onetimeToken = HttpContext.User.Identity.Name;
            // ลบไฟล์ที่ Cache บัญชีผู้ใช้งาน
            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            string cacheFile = string.Format("{0}/{1}.authorize", appSetting.UserAuthorizeCachePath, onetimeToken);
            if (System.IO.File.Exists(cacheFile))
                System.IO.File.Delete(cacheFile);


            using (Entity.ExcisePlaningDbDataContext db = new Entity.ExcisePlaningDbDataContext())
            {
                var personExpr = db.T_PERSONNELs.Where(e => e.ONE_TIME_TOKEN.Equals(onetimeToken));
                if (personExpr.Any())
                {
                    var personEntity = personExpr.First();
                    personEntity.ONE_TIME_TOKEN = null;
                    db.SubmitChanges();
                }
            }

            FormsAuthentication.SignOut();
            return RedirectToAction("LoginForm", "Authorize");
            //return View("Logout");
        }


        /// <summary>
        /// กรณีเข้าสู่ระบบผ่าน SSO และเป็นการเข้าสู่ระบบในครั้งแรก
        /// บังคับให้ผู้ใช้งาน ระบุข้อมูลเบื้องต้นสำหรับใช้งานระบบ
        /// </summary>
        /// <param name="cardNumber"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult InitNewAccount(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
            {
                ViewBag.ErrorMessage = "ไม่พบรหัสบัตรประชาชน";
                return View("LoginForm");
            }
            //return RedirectToAction("LoginForm", "Authorize");

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var personExpr = db.T_PERSONNELs.Where(e => (e.ACTIVE.Equals(0) || e.ACTIVE.Equals(1)) && e.CARD_NUMBER.Equals(cardNumber)).FirstOrDefault();
                if (null == personExpr)
                {
                    ViewBag.ErrorMessage = "ระบบไม่สามารถบันทึกข้อมูลพนักงานจาก SSO ได้สำเร็จ";
                    return View("LoginForm");
                }

                ViewBag.FirstName = personExpr.FIRST_NAME;
                ViewBag.LastName = personExpr.LAST_NAME;
                ViewBag.CardNumber = cardNumber;


                ViewBag.Areas = db.T_AREAs.OrderBy(e => e.AREA_ID).Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = e.AREA_NAME
                }).ToList();
                ViewBag.Positions = db.T_POSITIONs.Where(e => e.ACTIVE.Equals(1)).Select(e => new PositionShortFieldProperty()
                {
                    POSITION_ID = e.POSITION_ID,
                    POSITION_NAME = e.POSITION_NAME
                }).ToList();
                ViewBag.PersonTypes = db.T_PERSONNEL_TYPEs.Where(e => e.ACTIVE.Equals(1)).Select(e => new PersonnelTypeShortFieldProperty()
                {
                    PERSON_TYPE_ID = e.PERSON_TYPE_ID,
                    PERSON_TYPE_NAME = e.ITEM_TEXT
                }).ToList();
                ViewBag.PersonLevels = db.T_PERSONNEL_LEVELs.Where(e => e.ACTIVE.Equals(1)).Select(e => new PersonnelLevelShortFieldProperty()
                {
                    LEVEL_ID = e.LEVEL_ID,
                    LEVEL_NAME = e.LEVEL_NAME
                }).ToList();
                ViewBag.Provinces = db.T_PROVINCEs.OrderBy(e => e.PROVINCE_NAME).ToList();
                ViewBag.PrefixNames = db.T_PERSONNEL_PREFIXes.Where(e => e.ACTIVE.Equals(1)).ToList();
            }

            return View();
        }
        [HttpPost]
        public ActionResult SubmitSave(InitPersonnelInfoFormMapper model)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(4) {
                { "errors", null },
                { "errorText", null },
                { "empCode", null },
                { "accessToken", null }
            };

            // ตรวจสอบค่าที่ส่งจากหน้าฟอร์ม
            var modelErrors = ModelValidateErrorProperty.TryValidate(ModelState);
            if (modelErrors.Any())
            {
                res["errors"] = modelErrors;
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var personExpr = db.T_PERSONNELs.Where(e => e.CARD_NUMBER.Equals(model.CardNumber)).FirstOrDefault();
                if (null == personExpr)
                {
                    res["errorText"] = string.Format("ไม่พบรหัสบัตรประชาชน {0} ในระบบ", model.CardNumber);
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                personExpr.SEX_TYPE = model.SexType[0];
                personExpr.PREFIX_NAME = model.PrefixName;
                personExpr.FIRST_NAME = model.FirstName;
                personExpr.LAST_NAME = model.LastName;
                personExpr.PERSON_TYPE_ID = model.PersonTypeId.Value;
                personExpr.POSITION_ID = model.PositionId.Value;
                personExpr.LEVEL_ID = model.LevelId;
                personExpr.AREA_ID = model.AreaId.Value;
                personExpr.DEP_ID = model.DepId.Value;
                personExpr.PROVINCE_NAME = model.ProvinceName;
                personExpr.ACTIVE = 1;
                personExpr.UPDATED_DATETIME = DateTime.Now;

                var appSetting = AppSettingProperty.ParseXml();
                res["accessToken"] = appSetting.AccessToken;
                res["empCode"] = personExpr.PERSON_CODE;
                db.SubmitChanges();
            }


            return Json(res, JsonRequestBehavior.DenyGet);
        }

        public class InitPersonnelInfoFormMapper
        {
            /// <summary>
            /// เพศ M = ชาย, F = หญิง
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public string SexType { get; set; }

            /// <summary>
            /// คำนำหน้าชื่อ
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(20, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string PrefixName { get; set; }

            /// <summary>
            /// ชื่อ
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string FirstName { get; set; }

            /// <summary>
            /// นามสกุล
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(100, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string LastName { get; set; }

            /// <summary>
            /// ประเภทพนักงาน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public short? PersonTypeId { get; set; }

            /// <summary>
            /// ตำแหน่งงาน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public short? PositionId { get; set; }

            /// <summary>
            /// ระดับพนักงาน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public short? LevelId { get; set; }

            /// <summary>
            /// เขตพื้นที่สังกัด
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? AreaId { get; set; }

            /// <summary>
            /// หน่วยงานที่สังกัด
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน")]
            public int? DepId { get; set; }

            /// <summary>
            /// รหัสบัตรประชาชน เพื่อใช้ในการเชื่อมกับระบบ SSO
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(20, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string CardNumber { get; set; }

            /// <summary>
            /// จังหวัดที่ทำงาน
            /// </summary>
            [Required(ErrorMessage = "ระบุค่านี้ก่อน"), MaxLength(50, ErrorMessage = "ความยาวไม่เกิน {1} ตัวอักษร")]
            public string ProvinceName { get; set; }
        }


        /// <summary>
        /// แสดงหน้าข้อผิดพลาด ค่าคงที่ในระบบ ยังไม่ถูกกำหนดค่า <para/>
        /// </summary>
        /// <param name="errorText"></param>
        /// <returns></returns>
        public ActionResult AppConstNotDefined(string errorMessage)
        {
            ViewBag.ErrorMessage = errorMessage;
            return View("AppConstNotDefined");
        }

        [HttpGet]
        public ActionResult Unauthorize()
        {
            //return View("Unauthorize");
            return LoginForm();
        }

        [HttpGet]
        public ActionResult PageNotFound()
        {
            ViewBag.Title = "Page Not Found";
            return View("PageNotFound");
        }

        [HttpGet]
        public ActionResult PageErrors()
        {
            ViewBag.Title = "Website Error";
            return View("PageErrors");
        }
    }
}