using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    /// <summary>
    /// ใช้สำหรับ Map ข้อมูลจากตาราง T_ACTIVITY_CONFIGURE
    /// เพื่อแสดงผลเฉพาะ ID, Name
    /// </summary>
    public class ActivityShortFieldProperty
    {
        public int ACTIVITY_ID { get; set; }
        public string ACTIVITY_NAME { get; set; }
    }
}