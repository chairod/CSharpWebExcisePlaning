using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "เงินอุดหนุนสำหรับค่าสินบนและเงินรางวัล สุรา ยาสูบ ไพ่ แสตมป์สรรพสามิตและ พ.ร.บ. ภาษีสรรพสามิต พ.ศ. 2527"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesSubsidy
    {
        public string ItemText { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RemarkText { get; set; }
    }
}