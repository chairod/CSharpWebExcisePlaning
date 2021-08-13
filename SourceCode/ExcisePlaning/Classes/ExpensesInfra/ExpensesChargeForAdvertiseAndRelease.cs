using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าโฆษณาและเผยแพร่"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForAdvertiseAndRelease
    {
        public string AssetId { get; set; }
        public string AssetName { get; set; }
        public string UnitName { get; set; }

        public int? Amounts { get; set; }
        public decimal? PricePerUnit { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RemarkText { get; set; }
    }
}