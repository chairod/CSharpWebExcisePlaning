using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าซ่อมแซมครุภัณฑ์"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesRepairEquipment
    {
        public string EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentOtherFlag { get; set; }
        public string EquipmentOtherName { get; set; }
        public int? StockAmounts { get; set; }
        public int? RepairAmounts { get; set; }
        public decimal? RepairPricePerItem { get; set; }
        public decimal? RepairTotalPrice { get; set; }
    }
}