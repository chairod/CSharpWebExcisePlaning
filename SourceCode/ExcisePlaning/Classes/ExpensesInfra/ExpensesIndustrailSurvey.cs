using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าตรวจปฏิบัติการโรงงานอุตสาหกรรรม"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesIndustrailSurvey
    {
        public string PersonName{get;set;}
        public string PositionId { get;set;}
        public string PositionName { get;set;}
        public string PersonTypeId { get;set;}
        public string PersonTypeName { get;set;}
        public int? AmountDays { get;set;}
        public string VehicleTypeId { get;set;}
        public string VehicleTypeName { get;set;}
        public decimal? CompensationRatePrice { get;set;}
        public decimal? TotalPrice { get;set;}
    }
}