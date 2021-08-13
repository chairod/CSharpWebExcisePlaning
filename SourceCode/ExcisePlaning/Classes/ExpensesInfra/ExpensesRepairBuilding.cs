using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าซ่อมแซมสิ่งก่อสร้าง"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesRepairBuilding
    {
        public string BuildingId { get; set; }
        public string BuildingName { get; set; }
        public string BuildingOtherFlag { get; set; }
        public string BuildingOtherName { get; set; }
        public decimal? RepairPrice { get; set; }
    }
}