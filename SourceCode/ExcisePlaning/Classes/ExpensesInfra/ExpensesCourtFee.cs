using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าธรรมเนียมศาล"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesCourtFee
    {
        public string ItemText { get; set; }
        public decimal? FeeAmount { get; set; }
    }
}