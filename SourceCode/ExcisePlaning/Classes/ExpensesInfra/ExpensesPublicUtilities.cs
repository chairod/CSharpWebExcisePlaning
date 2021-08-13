using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าโทรศัพท์"
    /// "ค่าน้ำประปา"
    /// "ค่าบริการสื่อสารและโทรคมนาคม"
    /// "ค่าไปรษณีย์"
    /// "ค่าไฟฟ้า"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesPublicUtilities
    {
         public string ItemText {get;set;}
        
        /// <summary>
        /// จ่ายจริงปีก่อนหน้า (บาท)
        /// </summary>
        public decimal? ActualPayPreviousYearAmounts { get;set;}

        /// <summary>
        /// คำของบประมาณ (บาท)
        /// </summary>
        public decimal? RequestBudgetAmounts { get;set;}
    }
}