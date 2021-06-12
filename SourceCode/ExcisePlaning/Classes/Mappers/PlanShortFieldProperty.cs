using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    /// <summary>
    /// ใช้สำหรับ Map ข้อมูลจากตาราง T_PLAN_CONFIGURE
    /// เพื่อแสดงผลเฉพาะ ID, Name
    /// </summary>
    public class PlanShortFieldProperty
    {
        public int PLAN_ID { get; set; }
        public string PLAN_NAME { get; set; }
    }
}