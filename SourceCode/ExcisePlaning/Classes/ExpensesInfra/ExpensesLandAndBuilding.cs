using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ที่ดินและสิ่งก่อสร้าง"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesLandAndBuilding
    {
        public string AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetOtherFlag { get; set; }
        public string AssetOtherName { get; set; }
        public string ProjectName { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RemarkText { get; set; }
        public string AttachFilename { get; set; }
    }
}