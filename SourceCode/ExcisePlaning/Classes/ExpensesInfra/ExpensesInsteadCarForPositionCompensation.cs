using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าตอบแทนแหมาจ่ายแทนการจัดหารถประจำตำแหน่ง"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesInsteadCarForPositionCompensation
    {
        public string PersonName { get; set; }
        public string PositionId { get; set; }
        public string PositionName { get; set; }
        public decimal? CompensationMonthlyRatePrice { get; set; }
        public decimal? CompensationYearlyPrice { get; set; }
    }
}