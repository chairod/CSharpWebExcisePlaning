using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    public class UserAuthorizeProperty
    {
        /// <summary>
        /// ค้นหาโปรไฟล์ของผู้ใช้งานที่ออนไลน์ อยู่ในระบบ ด้วย Onetime token<para />
        /// หลังจากเข้าสู่ระบบสำเร็จ จะทำการ Cache ข้อมูลโปรไฟล์ของผู้ใช้งานลง cache path เพื่อลดการอ่านข้อมูลจากฐานข้อมูล<para/>
        /// 1. ระบบจะอัพเดตเวลาล่าสุด ที่ผู้ใช้งานปฏิสัมพันธ์กับระบบ (เพื่อใช้ในข้อ 2)<para/>
        /// 2. ระบบจะตรวจสอบระยะเวลา การ Inactive (หยุดปฏิสัมพันธ์) กับเว็บไซด์ถ้านานกว่า 30 นาทีจะถูกตัดการเชื่อมต่อ
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static UserAuthorizeProperty GetUserAuthorizeProfile(string userOnetimetoken)
        {
            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            string cacheFile = string.Format("{0}/{1}.authorize", appSetting.UserAuthorizeCachePath, userOnetimetoken);
            if (File.Exists(cacheFile))
            {
                FileInfo finfo = new FileInfo(cacheFile);

                // ตรวจสอบเวลาการปฏิสัมพันธ์ กับ ระบบ
                double maximumIgnoreInactiveMinutes = 30;
                var interval = (DateTime.Now - finfo.LastWriteTime);
                if (interval.TotalMinutes.CompareTo(maximumIgnoreInactiveMinutes) == 1)
                {
                    finfo.Delete();
                    HttpContext.Current.Response.Redirect("/"); // ไปยังหน้า Login
                    HttpContext.Current.Response.End();
                }
                else
                {
                    try
                    {
                        // บางครั้งอาจจะมีการแก้ไข เวลาในช่วงเวลาเดียวกัน กับ Thread อื่นๆ
                        // เพราะมีการ Request เข้ามาแบบถี่ๆ ให้เข้า catch exception ไป
                        finfo.LastWriteTime = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    return AppUtils.ParseJson<UserAuthorizeProperty>(File.ReadAllText(cacheFile));
                }

                //bool isAllow = true;
                //using (Entity.LeaveDbDataContext db = new Entity.LeaveDbDataContext())
                //{
                //    var empExpr = db.T_PERSONNELs.Where(e => e.ONE_TIME_TOKEN.Equals(userOnetimetoken));
                //    if (empExpr.Any())
                //    {
                //        var empEntity = empExpr.First();
                //        // ตรวจสอบเวลาการปฏิสัมพันธ์ กับ ระบบ
                //        double maximumIgnoreInactiveMinutes = 30.0;
                //        double diffMinutes = 0.0;
                //        if (null != empEntity.LAST_ACTION_DATETIME)
                //            diffMinutes = (DateTime.Now - empEntity.LAST_ACTION_DATETIME).Value.TotalMinutes;
                //        if (diffMinutes.CompareTo(maximumIgnoreInactiveMinutes) >= 0)
                //        {
                //            empEntity.ONE_TIME_TOKEN = null;
                //            File.Delete(cacheFile);
                //            isAllow = false;
                //        }
                //        else
                //            empEntity.LAST_ACTION_DATETIME = DateTime.Now;

                //        // ปรับปรุงการปฏิสัมพันธ์กับระบบทุกๆ 1.5 นาที
                //        //if (diffMinutes.CompareTo(15) == 1)
                //        //    db.SubmitChanges();
                //    }
                //}


                //if (isAllow)
                //    return AppUtils.ParseJson<UserAuthorizeProperty>(File.ReadAllText(cacheFile));
            }
            else
            {
                HttpContext.Current.Response.Redirect("/"); // ไปยังหน้า Login
                HttpContext.Current.Response.End();
            }

            // การ Return new UserAuthorizeProperty() กลับไป
            // จะทำให้ Models.CustomRoleProvider ตรวจสอบหา User Role ไม่เจอ
            // ดังนั้น จะเป็นผลทำให้ บัญชีผู้ใช้งาน ณ ปัจจุบันถูกเด้งออกจากระบบไป
            return new UserAuthorizeProperty();
        }

        /// <summary>
        /// ค้นหาเมนูตาม ค่าคงที่ของเมนู ซึ่งเป็นรายการเมนูที่ถูกกำหนดสิทธิ์ไว้แล้วในแต่ละบัญชีผู้ใช้งาน
        /// </summary>
        /// <param name="menuConst"></param>
        /// <returns></returns>
        public UserAuthorizeMenuProperty FindUserMenu(string menuConst)
        {
            UserAuthorizeMenuProperty menuItem = null;
            foreach (var menuGroupItem in MenuGroups)
            {
                var menuExpr = menuGroupItem.UserMenus.Where(m => m.MenuConst.Equals(menuConst));
                if (menuExpr.Any())
                {
                    menuItem = menuExpr.First();
                    menuExpr = null;
                    break;
                }
            }

            if (null == menuItem)
                menuItem = new UserAuthorizeMenuProperty();
            return menuItem;
        }


        /// <summary>
        /// ตรวจสอบรหัสหน่วยงานเป็นหน่วยงานตามสิทธิ์ที่มอบหมาย หรือ เป็นหน่วยงานที่ตนเองสังกัดอยู่หรือไม่
        /// ยกเว้น หน่วยงานกลาง จะมองเห็นข้อมูลของทุกหน่วยงาน
        /// สำหรับตรวจสอบการเข้าถึงข้อมูลของหน่วยงานนั้นๆ
        /// </summary>
        /// <param name="depId"></param>
        /// <returns></returns>
        public bool VerifyAuthorizeDepartmentById(int depId)
        {
            // เป็นหน่วยงานกลาง
            if (DepAuthorize.Equals(1))
                return true;

            // รหัสหน่วยงาน ตรงกับรหัสหน่วยงานที่ตนสังกัด
            if (DepId.Equals(depId))
                return true;

            // รหัสหน่วยงาน มีในรายการหน่วยงานที่รับผิดชอบ
            if (AssignDepartmentIds.IndexOf(depId) > -1)
                return true;

            return false;
        }

        public UserAuthorizeProperty()
        {
            MenuGroups = new List<UserMenuGroupProperty>();
            RoleNames = new List<string>();
            AssignDepartmentIds = new List<int>();
        }

        /// <summary>
        /// รหัสพนักงาน ที่อ้างอิง กับระบบลูกค้า
        /// </summary>
        public string EmpCode { get; set; }

        /// <summary>
        /// รหัสพนักงาน ที่ใช้ในระบบ
        /// </summary>
        public int EmpId { get; set; }

        /// <summary>
        /// ปีงบประมาณ ที่เลือกมาจากตอน เข้าสู่ระบบ
        /// ซึ่งในแต่ละจอ จะต้อง Default ค่าปีงบประมาณนี้ให้ (ค.ศ.)
        /// </summary>
        public int DefaultFiscalYear { get; set; }

        /// <summary>
        /// เลขที่ตำแหน่งงานในระบบ
        /// </summary>
        public short PositionId { get; set; }

        /// <summary>
        /// ชื่อตำแหน่งงาน
        /// </summary>
        public string PositionName { get; set; }

        /// <summary>
        /// ประเภทบุคลากร เช่น ข้าราชการ พนักงานราชการ ลูกค้าชั่วคราว เป็นต้น
        /// </summary>
        public short PersonTypeId { get; set; }

        /// <summary>
        /// เขตพื้นที่ของหน่วยงาน
        /// </summary>
        public int? AreaId { get; set; }

        /// <summary>
        /// รหัสหน่วยงาน ที่บุคลากรสังกัดอยู่
        /// </summary>
        public int DepId { get; set; }

        /// <summary>
        /// ชื่อหน่วยงานที่บุคลากรสังกัดอยู่
        /// </summary>
        public string DepName { get; set; }

        /// <summary>
        /// 1=หน่วยงานกลาง, 2=หน่วยงานทั่วไป
        /// </summary>
        public short DepAuthorize { get; set; }

        /// <summary>
        /// สามารถเลือกหน่วยงานใน Dropdown ได้หรือไม่
        /// 1. หน่วยงานกลาง ดูได้ทุกหน่วยงาน
        /// 2. หน่วยงานหลักของแต่ละภูมิภาค ดูได้เฉพาะหน่วยงานที่ตนรับผิดชอบ
        /// 3. หน่วยงานทั่วไป หรือ หน่วยงานภูมิภาค ดูได้เฉพาะหน่วยงานตนเองสังกัดอยู่
        /// </summary>
        public bool CanSelectDepartment
        {
            get
            {
                return DepAuthorize.Equals(1) || (DepAuthorize.Equals(2) && AssignDepartmentIds.Count > 0);
            }
        }

        /// <summary>
        /// หน่วยงานนี้สามารถสร้างคำของบประมาณใหม่ได้หรือไม่ (true = สามารถสร้างคำของบประมาณใหม่ได้)
        /// บางหน่วยงานจะเป็นหน่วยงานที่คุมหน่วยงานภูมิภาคอื่นๆ อีกที
        /// มีสิทธิ์เป็นไปได้ว่า หน่วยงานที่คุมหน่วยงานภูมิภาค จะไม่สามารถสร้างคำของบประมาณใหม่ได้
        /// </summary>
        public bool CanCreateRequestBudget { get; set; }

        /// <summary>
        /// รหัสหน่วยงานที่อยู่ภายใต้การดูแลของหน่วยงานนี้
        /// </summary>
        public List<int> AssignDepartmentIds { get; set; }

        /// <summary>
        /// รหัสฝ่าย/ส่วน ของบุคลากร (บางคนไม่มีสังกัด ส่วน/ฝ่าย)
        /// </summary>
        public int? SubDepId { get; set; }

        /// <summary>
        /// ชื่อพนักงาน ที่ใช้แสดงผลในหน้าเว็บไซด์
        /// </summary>
        public string EmpFullname { get; set; }

        /// <summary>
        /// เมนูของผู้ใช้งานที่ ให้สิทธิ์ไว้
        /// </summary>
        public List<UserMenuGroupProperty> MenuGroups { get; set; }

        /// <summary>
        /// เพศของผู้ใช้งาน M = ผู้ชาย, F = ผู้หญิง
        /// </summary>
        public char SexType { get; set; }

        /// <summary>
        /// 1 = Super User ดูข้อมูลได้ทุกหน่วยงาน ทุกคน เช่น Admin เป็นต้น, 0 = ผู้ใช้งานทั่วไป
        /// </summary>
        public short? AccountType { get; set; }

        /// <summary>
        /// อีเมล์ของผู้ใช้งาน
        /// </summary>
        public string EmailAddr { get; set; }

        /// <summary>
        /// เบอร์มือถือของผู้ใช้งาน
        /// </summary>
        public string MobileNo { get; set; }

        /// <summary>
        /// รายชื่อ Role ที่ผู้ใช้งานถูกกำหนดสิทธิ์ไว้
        /// </summary>
        public List<string> RoleNames { get; set; }
    }
}