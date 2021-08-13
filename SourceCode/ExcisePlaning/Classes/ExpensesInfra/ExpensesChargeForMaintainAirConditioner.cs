using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าใช้จ่ายในการบำรุงรักษาเครื่องปรับอากาศ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForMaintainAirConditioner
    {
        public string ServiceName { get; set; }
        public int? Amounts { get; set; }
        public decimal? PricePerUnit { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RemarkText { get; set; }
    }
}