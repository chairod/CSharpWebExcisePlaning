using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าจ้างเหมาบริการ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForHireService
    {
        public string ServiceName { get; set; }
        public int? PersonAmounts { get; set; }
        public decimal? ServicePrice { get; set; }
        public decimal? ServiceMonthlyPrice { get; set; }
        public decimal? ServiceYearlyPrice { get; set; }
        public string RemarkText { get; set; }
    }
}