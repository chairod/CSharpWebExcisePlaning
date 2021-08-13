using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าวัสดุเชื้อเพลิงและหล่อลื่น"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForOilAndLubricate
    {
        public string VehicleTypeId { get; set; }
        public string VehicleTypeName { get; set; }
        public int? VehicleAmounts { get; set; }
        public int? AmountDays { get; set; }
        public int? LiterAmountsPerMonth { get; set; }
        public decimal? PricePerLiter { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? TotalYearlyPrice { get; set; }
    }
}