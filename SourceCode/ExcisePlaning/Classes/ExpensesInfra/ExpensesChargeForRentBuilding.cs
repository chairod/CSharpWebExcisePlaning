using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าเช่าทรัพย์สิน"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForRentBuilding
    {
        public string AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetOtherFlag { get; set; }
        public string AssetOtherName { get; set; }
        public decimal? RentMonthlyPrice { get; set; }
        public decimal? RentYearlyPrice { get; set; }
        public string RemarkText { get; set; }
    }
}