using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "เงินสมทบกองทุนเงินทดแทน"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesCompensationFund
    {
        public string PersonName{get;set;}
        public string PositionId {get;set;}
        public string PositionName { get;set;}
        public decimal? Salary { get;set;}
        public decimal? MonthlyRateVal { get;set;}
        public decimal? YearlyRateVal { get;set;}
    }
}