using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าซ่อมแซมยานพาหนะและขนส่ง"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesRepairVehicleAndTransport
    {
        public ExpensesRepairVehicleAndTransport()
        {
            Items = new List<ExpensesRepairVehicleAndTransportItem>();
        }

        public string VehicleTypeId { get; set; }
        public string VehicleTypeName { get; set; }
        
        [XmlElement(ElementName = "Items")]
        public List<ExpensesRepairVehicleAndTransportItem> Items { get; set; }
    }

    public class ExpensesRepairVehicleAndTransportItem
    {
        /// <summary>
        /// ทะเบียนยานพาหนะราชการ
        /// </summary>
        public string LicenseNo { get; set; }

        /// <summary>
        /// วัน/เดือน/ปี จดทะเบียน (ปี พ.ศ.) 
        /// </summary>
        [JsonProperty("RegisterDate")]
        [XmlElement(ElementName = "RegisterDate")]
        public string RegisterDateText { get; set; }

        /// <summary>
        /// อายุการใช้งาน (ตย. 17 ปี 11 เดือน 5 วัน)
        /// </summary>
        [JsonProperty("VehicleAgeValue")]
        [XmlElement(ElementName = "VehicleAgeValue")]
        public string VehicleAgeValueText { get; set; }

        /// <summary>
        /// ค่าซ่อมแซม (บาท)
        /// </summary>
        public decimal? ExpensesRepairPrice { get; set; }
    }
}