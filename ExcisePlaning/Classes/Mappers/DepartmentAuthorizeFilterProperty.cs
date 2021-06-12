using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    /// <summary>
    /// ตรวจสอบสิทธิ์การเข้าถึงข้อมูล ของแต่ล่ะหน่วยงาน
    /// 
    /// </summary>
    public class DepartmentAuthorizeFilterProperty
    {
        public DepartmentAuthorizeFilterProperty()
        {
            Authorize = 2;
            AssignDepartmentIds = new List<int>();
        }


        /// <summary>
        /// 1 = ดูข้อมูลได้ทุกหน่วยงาน (หน่วยงานกลาง) AssignDepartmentIds จะมีค่า = 0 รายการเพราะถือว่าดูได้ทุกหน่วยงานอยู่แล้ว
        /// 2 = ดูได้เฉพาะหน่วยงานที่สังกัด หรือ เฉพาะที่ได้รับสิทธิ์ โดยให้ใช้คุณสมบัติ AssignDepartmentIds ประกอบการเข้าถึงข้อมูล
        /// </summary>
        public int Authorize { get; set; }

        /// <summary>
        /// รหัสหน่วยงาน (ที่ระบบสร้างขึ้น) ที่กำหนดสิทธิ์ให้เข้าถึงข้อมูลได้
        /// </summary>
        public List<int> AssignDepartmentIds { get; set; }


        /// <summary>
        /// ตรวจสอบสิทธิ์การเข้าถึงข้อมูล โดยใช้ Profile ของผู้ใช้งานที่เข้าสู่ระบบมา และ หน่วยงานที่เลือก
        /// </summary>
        /// <param name="userAuthorizeProfile"></param>
        /// <param name="selectedDepId">หน่วยงานที่เลือกจากหน้าเว็บไซด์ เพื่อประกอบเงื่อนไขการตรวจสอบสิทธิ์</param>
        /// <returns></returns>
        public static DepartmentAuthorizeFilterProperty Verfity(UserAuthorizeProperty userAuthorizeProfile, int? selectedDepId)
        {
            DepartmentAuthorizeFilterProperty ret = new DepartmentAuthorizeFilterProperty();
            ret.Authorize = 2;

            // สิทธิ์ที่เข้าระบบมาเป็น หน่วยงานกลาง
            if (userAuthorizeProfile.DepAuthorize.Equals(1))
            {
                if (null == selectedDepId)
                    ret.Authorize = 1;
                else
                {
                    short? selectedDepAuthorize = AppUtils.GetDepartmentAuthorizeById(selectedDepId.Value);
                    if (null != selectedDepAuthorize)
                    {
                        if (selectedDepAuthorize.Value.Equals(1))
                            ret.Authorize = 1;
                        else
                        {
                            // รายการรหัสหน่วยงาน ที่สามารถเข้าถึงข้อมูลได้
                            ret.AssignDepartmentIds.Add(selectedDepId.Value);
                            ret.AssignDepartmentIds.AddRange(AppUtils.GetAssignDepartmentById(selectedDepId.Value));
                        }
                    }
                    else
                        ret.Authorize = 1;
                }
            }
            else
            {
                // หน่วยงานภูมิภาค แบ่งออกเป็น 2 ประเภท
                // 1.) หน่วยงานภูมิภาค ที่คุมหน่วยงานภูมิภาค
                // 2.) หน่วยงานภูมิภาค


                // ไม่เลือกหน่วยงาน หรือ ถ้าเลือกหน่วยงาน หน่วยงานที่เลือกจะต้องอยู่ภายใต้สิทธิ์ที่ระบบกำหนดหนดให้
                if (null == selectedDepId || (null != selectedDepId && userAuthorizeProfile.AssignDepartmentIds.IndexOf(selectedDepId.Value) == -1))
                {
                    // เข้าถึงข้อมูลได้เฉพาะหน่วยงาน ที่ระบบกำหนดสิทธิ์ให้
                    ret.AssignDepartmentIds.Add(userAuthorizeProfile.DepId);
                    ret.AssignDepartmentIds.AddRange(userAuthorizeProfile.AssignDepartmentIds);
                }
                else
                {
                    short? selectedDepAuthorize = AppUtils.GetDepartmentAuthorizeById(selectedDepId.Value);
                    if (null != selectedDepAuthorize && selectedDepAuthorize.Value.Equals(1))
                        ret.Authorize = 1;
                    else
                    {
                        // รายการรหัสหน่วยงาน ที่สามารถเข้าถึงข้อมูลได้
                        ret.AssignDepartmentIds.Add(selectedDepId.Value);
                        ret.AssignDepartmentIds.AddRange(AppUtils.GetAssignDepartmentById(selectedDepId.Value));
                    }
                }
            }

            return ret;
        }
    }
}