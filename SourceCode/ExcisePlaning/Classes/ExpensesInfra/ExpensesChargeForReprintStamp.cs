using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าจ้างพิมพ์แก้ไขแสตมป์ (ค่าจ้างเหมา)"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForReprintStamp
    {
        public string ServiceName { get; set; }
        public decimal? ServicePrice { get; set; }
    }
}