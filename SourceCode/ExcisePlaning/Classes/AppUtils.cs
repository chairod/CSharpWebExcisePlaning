using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes
{
    public class AppUtils
    {
        public static CultureInfo ThaiCultureInfo = new CultureInfo("th-TH");

        /// <summary>
        /// เข้ารหัสข้อความด้วย MD5
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string GetMD5Value(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] buffer = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(text));
                StringBuilder sb = new StringBuilder();
                foreach (byte buff in buffer)
                    sb.Append(buff.ToString("x2"));
                return sb.ToString();
            }
        }


        /// <summary>
        /// เข้ารหัสรหัสข้อความด้วย Bcrypt
        /// </summary>
        /// <param name="inputPassword"></param>
        /// <returns></returns>
        public static string GetPasswordHash(string str)
        {
            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            return BCrypt.Net.BCrypt.HashPassword(str, salt);
        }


        public static T ParseJson<T>(string jsonVal)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonVal);// new JavaScriptSerializer().Deserialize<T>(jsonVal);
            }
            catch
            {
                return default(T);
            }
        }

        public static string ToJson(object value)
        {
            try
            {
                return JsonConvert.SerializeObject(value);// (new JavaScriptSerializer().Serialize(value));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// แปลง Xml ของคลาส Linq.XElement ให้เป็น json string
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="isRemoveRoot">ต้องการให้ลบ {"root": xxxx } ทิ้ง</param>
        /// <returns></returns>
        public static string LinqXmlToJson(System.Xml.Linq.XElement xml, bool isRemoveRoot = true)
        {
            if (null == xml)
                return "";
            using (XmlReader reader = xml.CreateReader())
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(reader);

                string jsonStr = JsonConvert.SerializeXmlNode(xmlDocument.DocumentElement);
                if (isRemoveRoot)
                {
                    jsonStr = jsonStr.Replace("{\"root\":{\"root\":", "");
                    jsonStr = Regex.Replace(jsonStr, "\\}{2}$", "");
                    jsonStr = jsonStr.Replace("{\"root\":null}", "");
                }

                return jsonStr;
            }
        }

        /// <summary>
        /// แปลง Json string ให้อยู่ในรูปแบบ Linq.XElement
        /// เพื่อนำไปเก็บในฐานข้อมูล
        /// รูปแบบ json จะต้องอยู่ในรูปแบบ {"root": ข้อมูล json ต้นทาง }
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public static System.Xml.Linq.XElement JsonToLinqXml(string jsonStr)
        {
            if (string.IsNullOrEmpty(jsonStr))
                return null;

            var xnode = JsonConvert.DeserializeXNode(jsonStr, "root", true);
            return xnode.Root;
        }


        /// <summary>
        /// แปลง XElement ให้เป็น object class เพื่อให้ง่ายต่อการเข้าถึงข้อมูลใน XML
        /// ** จะต้องสร้าง Class ขึ้นมา Match XML ตามโครงสร้างของ XML **
        /// เช่น จะถูกเรียกเมื่อต้องการอ่านข้อมูล T_BUDGET_REQUEST_DETAIL.EXPENSES_XML_DECRIBE
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xelement"></param>
        /// <returns>null หรือ Object T</returns>
        public static T ConvertXElementTo<T>(System.Xml.Linq.XElement xelement)
        {
            if (null == xelement || null == xelement.Element("root"))
                return default(T);

            //XmlSerializer xserialize = new XmlSerializer(typeof(T));
            //return (T)xserialize.Deserialize(xelement.Element("root").CreateReader());

            string jsonStr = LinqXmlToJson(xelement);
            return JsonConvert.DeserializeObject<T>(jsonStr);
        }


        /// <summary>
        /// ตรวจสอบรูปแบบวันที่ ที่ส่งจากหน้าฟอร์มของผู้ใช้งาน ระบุถูกต้องหรือไม่ <para/>
        /// โดยระบบจะรองรับเฉพาะรูปแบบ dd/MM/yyyy (Default คือ ปี พ.ศ. จะถูกแปลงเป็น ค.ศ. ให้, isBuddhist = false คือปีที่ผ่านเข้ามาเป็น ค.ศ.)
        /// </summary>
        /// <param name="dateStr"></param>
        /// <returns></returns>
        public static DateTime TryValidUserDateStr(string dateStr, bool isBuddhist = true)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            // ตรวจสอบรูปแบบวันที่ถูกต้องตามรูปแบบ dd/MM/yyyy หรือไม่
            List<string> dateParts = dateStr.Split(new char[] { '/' }).Where(str => !Regex.IsMatch(str, "[^0-9]")).ToList();
            if (dateParts.Count != 3)
                return DateTime.MinValue;

            // ตรวจสอบการระบุวันที่ เช่นใส่ 00/00/0000 เข้ามาต้องไม่ผ่าน
            if (dateParts.Select(str => Regex.Replace(str, "0{1,}", "")).Where(str => !string.IsNullOrEmpty(str)).Count() != 3)
                return DateTime.MinValue;

            int userDay = Convert.ToInt32(dateParts[0]);
            int userMonth = Convert.ToInt32(dateParts[1]);
            int userYear = Convert.ToInt32(dateParts[2]);

            // ระบุเดือนเกิน 12 เดือน
            if (userMonth > 12 || userYear.CompareTo(1900) == -1)
                return DateTime.MinValue;

            // ระบุจำนวนวันของเดือน เกินจำนวนวันที่มีอยู่จริงในเดือนนั้น
            int maximumDayInMonth = DateTime.DaysInMonth(userYear, userMonth);
            if (userDay > maximumDayInMonth)
                return DateTime.MinValue;

            // กรณีเป็นปี พ.ศ. ให้แปลงเป็น ค.ศ.
            if (isBuddhist)
                userYear -= 543;

            return new DateTime(userYear, userMonth, userDay);
        }


        /// <summary>
        /// สร้าง Primary key ให้กับตารางต่างๆในระบบ ในรูปแบบ prefix + running (leading zero) <para/>
        /// keyConst ค่าคงที่สำหรับแต่ล่ะ Key, prefix ค่าที่ต้องการใส่นำหน้า digits เช่น 62 จะได้ 6200001 เป็นต้น, digits จำนวนหลักของตัวเลข เช่น 5 หลัก เลขรันนิ่งปัจจุบันเป็น 1 ผลลัพธ์จะได้ 00001
        /// resetEveryYear ให้ Reset ค่ากลับไปเป็น 1 เหมือนเดิม เมื่อเป็นปีอื่นๆหรือไม่
        /// </summary>
        /// <param name="keyConst"></param>
        /// <param name="resetEveryYear"></param>
        /// <returns></returns>
        private static object lockGetNextKey = new object();
        public static string GetNextKey(string keyConst, string prefix, int digits, bool resetEveryMonth = false, bool resetEveryYear = false)
        {
            lock (lockGetNextKey)
                using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                {
                    var entity = db.T_GENKEYs.Where(e => e.KEY_CONST.Equals(keyConst)).FirstOrDefault();
                    if (null == entity)
                    {
                        entity = new T_GENKEY()
                        {
                            KEY_CONST = keyConst,
                            CURR_VAL = 0,
                            MN = Convert.ToInt16(DateTime.Now.Month),
                            YR = Convert.ToInt16(DateTime.Now.Year),
                            PREFIX_VAL = prefix
                        };
                        db.T_GENKEYs.InsertOnSubmit(entity);
                    }

                    // ต้องการเริ่มค่าเป็น 1 ใหม่เมื่อเป็นปีอื่นๆ
                    if (resetEveryYear && !DateTime.Now.Year.Equals(entity.YR))
                        entity.CURR_VAL = 0;
                    // ต้องการเริ่มค่าเป็น 1 ใหม่เมื่อเป็นเดือนอื่นๆ
                    if (resetEveryMonth && !DateTime.Now.Month.Equals(entity.MN))
                        entity.CURR_VAL = 0;

                    entity.CURR_VAL += 1;
                    entity.MN = Convert.ToInt16(DateTime.Now.Month);
                    entity.YR = Convert.ToInt16(DateTime.Now.Year);
                    db.SubmitChanges();

                    return string.Format("{0}{1}", prefix, entity.CURR_VAL.ToString("D" + digits));
                }
        }


        /// <summary>
        /// จัดรูปแบบการแสดงผล เวลาการมาทำงาน เช่น 7.21 => 7 ชั่วโมง 21 นาที เป็นต้น
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static string FormatDisplayWorkingHours(decimal? hours)
        {
            if (hours == null)
                return "";

            string[] hourStrParts = hours.Value.ToString().Split(new char[] { '.' });
            int hourVal = int.Parse(hourStrParts[0]);
            int minuteVal = int.Parse(hourStrParts[1]);

            StringBuilder sb = new StringBuilder();
            if (hourVal.CompareTo(0) == 1)
                sb.Append(hourVal.ToString("#,##0 ชั่วโมง "));

            if (hourStrParts[1].StartsWith("0"))
                sb.Append("0 นาที");
            else
                sb.Append(minuteVal.ToString("#,##0 นาที"));

            return sb.ToString();
        }

        /// <summary>
        /// รูปแบบการแสดงผล จำนวนวันลา การขอลาจะมีทั้งเป็นเต็มวัน (1) หรือ ครึ่งวัน (0.5) ทำให้บางคำขอลา จะมีทั้งตัวเลขที่เป็น
        /// จำนวนเต็ม หรือ ทศนิยม
        /// 1. จำนวนวันลาไม่มีเศษทศนิยม ให้แสดงเป็น จำนวนเต็ม
        /// 2. จำนวนวันลามีเศษทศนิยม ให้แสดงเศษส่วน 1 หลัก 
        /// </summary>
        /// <param name="amountDays"></param>
        /// <returns></returns>
        public static string FormatDisplayDecimal(decimal? amountDays)
        {
            if (null == amountDays)
                return "";
            string amountDaysStr = amountDays.Value.ToString("#,##0.0");
            string[] amountDaysStrParts = amountDaysStr.Split(new char[] { '.' });
            int decimalVal = int.Parse(amountDaysStrParts[1]);

            return decimalVal > 0 ? amountDaysStr : amountDays.Value.ToString("#,##0");
        }


        /// <summary>
        /// ดึงค่าปีปัจจุบันมาใช้งาน เนื่องจากจำเป็นต้องสร้างเป็นฟังชันก์ 
        /// 1. หน่วยงานราชการจะใช้ ปีงบประมาณ (01/10/2019 - 30/03/2020)
        /// 2. ทั่วไปจะใช้ปีปฏิทิน
        /// </summary>
        /// <returns></returns>
        public static short GetCurrYear()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return db.fn_GetCurrentYear(null).Value;
            }
        }

        /// <summary>
        /// ค้นหาค่าคงที่ ที่ตั้งค่าไว้ในระบบมาใช้งาน ซึ่งเป็นค่าคงที่ในปัจจุบันที่กำลังใช้งานอยู่
        /// </summary>
        /// <param name="configConst">ดูค่าคงที่ ที่ใช้งานได้จาก Classes.AppConfigConst</param>
        /// <returns></returns>
        public static proc_GetUsingAppConstByKeyResult GetUsingAppConstByKey(string configConst)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return db.proc_GetUsingAppConstByKey(configConst).FirstOrDefault();
            }
        }


        /// <summary>
        /// ค้นหารหัสหน่วยงาน ที่ไม่สามารถส่งคำของบประมาณได้
        /// 1. ใช้เป็นเงื่อนไข กรองหน่วยงานที่กำลังจะจัดสรรงบประมาณออกไป
        /// หน่วยงานที่ทำคำขอไม่ได้ ระบบมองว่าจะต้องไม่ได้รับงบประมาณ
        /// </summary>
        /// <returns></returns>
        public static List<int> GetAllDepartmentIdsCannotRequestBudget()
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                return db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && !e.CAN_REQUEST_BUDGET).Select(e => e.DEP_ID).ToList();
        }

        /// <summary>
        /// ตรวจสอบหน่วยงาน เป็นหน่วยงานกลาง หรือ หน่วยงานทั่วไป
        /// โดยใช้ รหัสหน่วยงาน ที่ระบบสร้างขึ้นเป็นคีย์ ในการค้นหา
        /// </summary>
        /// <param name="depId"></param>
        /// <returns></returns>
        public static short? GetDepartmentAuthorizeById(int depId)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                return db.T_DEPARTMENTs.Where(e => e.ACTIVE.Equals(1) && e.DEP_ID.Equals(depId)).Select(e => e.DEP_AUTHORIZE).FirstOrDefault();
        }

        /// <summary>
        /// รายการรหัสหน่วยงาน ที่ระบบมอบหมายให้รับผิดชอบ หรือ คุมหน่วยงานอื่นๆ 
        /// โดยใช้รหัสหน่วยงาน เป็นคีย์ ในการค้นหา
        /// </summary>
        /// <param name="depId"></param>
        /// <returns></returns>
        public static List<int> GetAssignDepartmentById(int depId)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
                return db.T_DEPARTMENT_AUTHORIZEs.Where(e => e.DEP_ID.Equals(depId)).GroupBy(e => e.AUTHORIZE_DEP_ID).Select(e => e.Key).ToList();
        }

        /// <summary>
        /// แปลงวันในสัปดาห์ ที่เป็นตัวเลข (0-6, Sun .. Sat) ให้อยู่ในรูปแบบ DayOfWeek Enum
        /// </summary>
        /// <param name="daysWeekIndex"></param>
        /// <returns></returns>
        public static List<DayOfWeek> ConvertDayWeekIndexToEnum(List<short> daysWeekIndex)
        {
            List<DayOfWeek> ret = new List<DayOfWeek>();
            daysWeekIndex.ForEach(index =>
            {
                if (index == 0)
                    ret.Add(DayOfWeek.Sunday);
                else if (index == 1)
                    ret.Add(DayOfWeek.Monday);
                else if (index == 2)
                    ret.Add(DayOfWeek.Tuesday);
                else if (index == 3)
                    ret.Add(DayOfWeek.Wednesday);
                else if (index == 4)
                    ret.Add(DayOfWeek.Thursday);
                else if (index == 5)
                    ret.Add(DayOfWeek.Friday);
                else if (index == 6)
                    ret.Add(DayOfWeek.Saturday);
            });
            return ret;
        }

        /// <summary>
        /// ตรวจเช็คช่วงอายุของข้อมูล โดยใช้ปีงบประมาณ เป็นเงื่อนไขในการตรวจสอบ
        /// หาก ปีงบประมาณที่ต้องการยกเลิกข้อมูล ห่างจาก ปีงบประมาณปัจจุบันตั้งแต่ 2 ปีขึ้นไป ไม่สามารถยกเลิกได้
        /// </summary>
        /// <param name="fromYear"></param>
        /// <param name="toYear"></param>
        /// <returns></returns>
        public static bool CanChangeDataByIntervalYear(int fromYear, int toYear)
        {
            //int diff = Math.Abs(toYear - fromYear);
            int diff = toYear - fromYear;
            return diff <= 0;
        }


        /// <summary>
        /// ค้นหาตารางที่รอการปรับปรุงข้อมูลจาก DbChangeSet.Updates
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindObjFromDbChangeSetUpdate<T>(ExcisePlaningDbDataContext db)
        {
            Type objType = typeof(T);
            return db.GetChangeSet().Updates.Where(x => x.GetType().Equals(objType)).Select(x => (T)x).AsEnumerable();
        }


        /// <summary>
        /// ค้นหาตารางที่รอการเพิ่มข้อมูลจาก DbChangeSet.Inserts
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindObjFromDbChangeSetInsert<T>(ExcisePlaningDbDataContext db)
        {
            Type objType = typeof(T);
            return db.GetChangeSet().Inserts.Where(x => x.GetType().Equals(objType)).Select(x => (T)x).AsEnumerable();
        }

        /// <summary>
        /// ค้นหาตารางที่รอการ เพิ่ม หรือ ปรับปรุงข้อมูลจาก DbChangeSet.Updates/Inserts
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindObjFromDbChangeSet<T>(ExcisePlaningDbDataContext db)
        {
            Type objType = typeof(T);
            var fromUpdates = db.GetChangeSet().Updates.Where(x => x.GetType().Equals(objType)).Select(x => (T)x).AsEnumerable();
            var fromInserts = db.GetChangeSet().Inserts.Where(x => x.GetType().Equals(objType)).Select(x => (T)x).AsEnumerable();
            return fromUpdates.Concat(fromInserts);
        }
    }
}