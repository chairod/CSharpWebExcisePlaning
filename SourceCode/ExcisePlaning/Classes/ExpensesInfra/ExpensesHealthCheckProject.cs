using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "โครงการตรวจสุขภาพ (ลูกจ้างชั่วคราว)"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesHealthCheckProject
    {
        public string ItemText { get; set; }
        public int? Amounts { get; set; }
        public decimal? PricePerUnit { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}