using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าถอนคืนรายได้แผ่นดิน"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesGovernmentIncome
    {
        public string ItemText { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RemarkText { get; set; }
    }
}