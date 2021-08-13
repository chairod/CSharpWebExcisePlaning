using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าตอบแทนการปฏิบัติงานนอกเวลาราชการ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesOvertimeCompensation
    {
         public string PersonName{get;set;}
        public string PositionId { get;set;}
        public string PositionName { get;set;}
        public string DivisionId { get;set;}
        public string DivisionName { get;set;}

        public string PersonTypeId { get; set; }
        public string PersonTypeName { get; set; }
        public string LevelId { get; set; }
        public string LevelName { get; set; }

        public int? WorkingAmountDays { get;set;}
        public int? WorkingAmountHoursPerDay {get;set;}
        public decimal? WorkingCompensationPerHour {get;set;}
        public decimal? TotalCompensationWorkingPrice {get;set;}

        public int? HolidayAmountDays {get;set;}
        public int? HolidayAmountHoursPerDay {get;set;}
        public decimal? HolidayCompensationPerHour {get;set;}
        public decimal? TotalCompensationHolidayPrice {get;set;}

        public decimal? NetCompensationPrice {get;set;}
    }
}