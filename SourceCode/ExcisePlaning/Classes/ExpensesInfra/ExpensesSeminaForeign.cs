using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าใช้จ่ายในการอบรมต่างประเทศ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesSeminaForeign
    {
        public string ItemText { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}