using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.Mappers
{
    /// <summary>
    /// ใช้สำหรับ Map ข้อมูลใน XML file "AppSettings.xml"
    /// </summary>
    public class AppSettingProperty
    {
        public static AppSettingProperty ParseXml()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettingProperty));
            string xmlFile = HttpContext.Current.Server.MapPath("~/AppSettings.xml");
            using (Stream stream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (AppSettingProperty)serializer.Deserialize(stream);
            }
        }
        /// <summary>
        /// Domain Name/IP Address สำหรับเชื่อมต่อกับ AD (Active Directory)
        /// </summary>
        public string ActiveDirectoryDomain { get; set; }

        /// <summary>
        /// Application ID ที่ใช้สำหรับเชื่อมต่อกับ SSO ของกรมสรรพสามิต
        /// </summary>
        public string SSOApplicationId { get; set; }

        /// <summary>
        /// รูปแบบการ Authen ของระบบ 
        /// SSO = ผ่าน Single Signon ของกรมสรรพสามิต, 
        /// LOCAL = เข้าสู่ระบบด้วยตัวระบบเอง
        /// </summary>
        public string LoginType { get; set; }

        // <summary>
        /// Role ที่ใช้สำหรับบุคลากรใหม่ ที่เคยเข้าสู่ระบบเป็นครั้งแรก จะให้ Default ไปที่ใด
        /// </summary>
        public string DefaultRoleIdForNewAccountStr { get; set; }

        /// <summary>
        /// รหัสหน่วยงานที่สามารถกันเงินงบประมาณได้
        /// </summary>
        public string AreaIdsCanReserveBudgetStr { get; set; }
        /// <summary>
        /// รหัสเขตพื้นที่ ที่สามารถทำรายการกันเงินงบประมาณได้
        /// </summary>
        /// <returns></returns>
        public List<int> GetAreaIdsCanReserveBudgetToList()
        {
            if (string.IsNullOrEmpty(AreaIdsCanReserveBudgetStr))
                return new List<int>();
            return AreaIdsCanReserveBudgetStr.Split(new char[] { ',' }).Select(id => Convert.ToInt32(id.Trim())).ToList();
        }

        /// <summary>
        /// รหัสรายการค่าใช้จ่าย ที่สงวนไว้ ไม่สามารถเลือกได้
        /// ในการบันทึกรายการค่าใช้จ่ายปิโตรเลียม
        /// เนื่องจาก: ค่าปิโตรเลียมจะเป็นการรวม แต่ละรายการค่าใช้จ่ายมาบันทึกข้อมูล
        /// </summary>
        public string ExpensesIdsReserveForPetoluem { get; set; }
        /// <summary>
        /// รหัสรายการค่าใช้จ่าย ที่สงวนไว้ ไม่สามารถเลือกได้
        /// ในการบันทึกรายการค่าใช้จ่ายปิโตรเลียม
        /// เนื่องจาก: ค่าปิโตรเลียมจะเป็นการรวม แต่ละรายการค่าใช้จ่ายมาบันทึกข้อมูล
        /// </summary>
        public List<int> ExpensesIdsReserveForPetoluemToList()
        {
            if (string.IsNullOrEmpty(ExpensesIdsReserveForPetoluem))
                return new List<int>();
            return ExpensesIdsReserveForPetoluem.Split(new char[] { ',' }).Select(id => Convert.ToInt32(id.Trim())).ToList();
        }

        /// <summary>
        /// พาร์ทสำหรับเขียน Output file ของระบบ อาทิเช่น รายงาน เป็นต้น
        /// </summary>
        public string TemporaryPath { get; set; }

        /// <summary>
        /// พาร์ทสำหรับเขียนไฟล์ที่อัพโหลดจากหน้า คำขอเงินงบประมาณ
        /// </summary>
        public string BudgetRequestAttachFilePath { get; set; }

        /// <summary>
        /// พาร์ทที่เก็บไฟล์ Template ของรายงานแต่ละตัวไว้
        /// </summary>
        public string ReportTemplatePath { get; set; }

        /// <summary>
        /// ชื่อฟอร์นของรายงาน
        /// </summary>
        public string ReportDefaultFontName { get; set; }

        /// <summary>
        /// ขนาดของฟอร์นรายงาน
        /// </summary>
        public float ReportDefaultFontSize { get; set; }

        /// <summary>
        /// พาร์ทสำหรับเก็บข้อมูล สิทธิ์ต่างๆของผู้ใช้งานในระบบหลังจากที่ยืนยันตัวตนผ่านแล้ว
        /// </summary>
        public string UserAuthorizeCachePath { get; set; }

        /// <summary>
        /// พาร์ทสำหรับเขียนข้อผิดพลาดของระบบไว้ <para />
        /// จะเขียนไฟล์ชื่อ App-Error.log หากขนาดไฟล์มากกว่า 10M จะย้ายไปไว้ใน Archives
        /// </summary>
        public string ErrorLogPath { get; set; }

        /// <summary>
        /// Token ที่ใช้สำหรับในกรณีที่ต้องการเข้าสู่ระบบด้วย รหัสพนักงาน
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// MIME Type ที่ระบบยอมให้อัพโหลดขึ้นมา ประกอบด้วย gif, pdf, jpeg, jpg, png, docx, xlsx, xls
        /// </summary>
        [XmlIgnore]
        public List<string> AcceptMimeTypes { get; set; }
        public string AcceptMimeTypeValues
        {
            get
            {
                return AcceptMimeTypeValues;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    AcceptMimeTypes = new List<string>();
                else
                    AcceptMimeTypes = value.Split(new char[] { ',' }).ToList();
            }
        }

        /// <summary>
        /// ขนาดของไฟล์ที่ระบบยอมให้อัพโหลด 2M (2097152 bytes)<para/>
        /// กรณีไม่กำหนดค่าจะ Default: -1 ไม่จำกัดขนาดของไฟล์
        /// </summary>
        [XmlIgnore]
        public long LimitedFileSizeBytes { get; set; }
        public string LimitedFileSizeBytesValue
        {
            get
            {
                return LimitedFileSizeBytesValue;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    LimitedFileSizeBytes = -1;
                else
                    LimitedFileSizeBytes = long.Parse(Regex.Replace(value, @"[^\d]+", ""));
            }
        }

    }
}