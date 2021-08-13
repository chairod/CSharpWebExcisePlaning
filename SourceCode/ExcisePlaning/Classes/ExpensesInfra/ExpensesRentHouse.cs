using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าเช่าบ้าน"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesRentHouse
    {
        public string PersonName { get; set; }
        public int? PositionId { get; set; }
        public string PositionName { get; set; }
        public int? LevelId { get; set; }
        public string LevelName { get; set; }
        public decimal? Salary { get; set; }
        public decimal? RentPriceRate { get; set; }
        public decimal? RentMonthlyRateVal { get; set; }
        public decimal? RentYearlyRateVal { get; set; }
    }
}