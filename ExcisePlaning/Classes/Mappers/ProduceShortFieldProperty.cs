using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    /// <summary>
    /// ใช้สำหรับ Map ข้อมูลจากตาราง T_PRODUCE_CONFIGURE
    /// เพื่อแสดงผลเฉพาะ ID, Name
    /// </summary>
    public class ProduceShortFieldProperty
    {
        public int PRODUCE_ID { get; set; }
        public string PRODUCE_NAME { get; set; }
    }
}