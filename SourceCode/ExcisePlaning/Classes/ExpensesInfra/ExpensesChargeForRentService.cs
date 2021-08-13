using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าจ้างเหมาบริการอื่นๆ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForRentService
    {
        public string ServiceName { get; set; }
        public decimal? ServicePrice { get; set; }
    }
}