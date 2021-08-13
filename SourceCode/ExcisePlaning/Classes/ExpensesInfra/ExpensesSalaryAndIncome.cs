using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าจ้างลูกจ้างชั่วคราว"
    /// "เงิน พ.ป.ผ."
    /// "เงิน พ.ป.พ."
    /// "เงิน พ.ส.ร."
    /// "เงิน พ.ส.ร. (ค่าจ้างประจำ)"
    /// "เงิน ส.ป.พ."
    /// "เงิน ส.ป.พ."
    /// "เงินค่าตอบแทนรายเดือนสำหรับข้าราชการ"
    /// "เงินช่วยเหลือค่าครองชีพ ข้าราชการระดับต้น"
    /// "เงินประจำตำแหน่ง"
    /// "เงินเพิ่มการครองชีพลูกจ้างชั่วคราว"
    /// "เงินเพิ่มพิเศษสำหรับตำแหน่งที่มีเหตุนิติกร"
    /// "อัตราเดิม (ข้าราชการ)"
    /// "อัตราเดิม (ค่าจ้างประจำ)"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesSalaryAndIncome
    {
        public string ItemText { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}