using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าตอบแทนพิเศษรายเดือนสำหรับผู้ปฏิบัติงานในเขตพื้นที่พิเศษ (ส.ป.พ.)"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesMonthlyCompensationExtra
    {
        public string PersonName { get; set; }
        public string PersonTypeId { get; set; }
        public string PersonTypeName { get; set; }
        public string PositionId { get; set; }
        public string PositionName { get; set; }
        public string LevelId { get; set; }
        public string LevelName { get; set; }
        public decimal? CompensationMonthlyVal { get; set; }
        public decimal? CompensationYearlyVal { get; set; }
    }
}