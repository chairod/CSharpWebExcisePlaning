using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าตอบแทนข้าราชการเกษียณ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesRetiredGovernmentCompensation
    {
        public string PersonName { get; set; }
        public string PositionNameBeforeRetired { get; set; }
        public decimal? CompensationMonthlyRatePrice { get; set; }
        public decimal? CompensationYearlyPrice { get; set; }
    }
}