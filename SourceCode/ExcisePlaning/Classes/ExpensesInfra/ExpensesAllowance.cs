using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าเบี้ยเลี้ย ค่าเช่าที่พัก และค่ายานพาหนะ ค่าน้ำมัน"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesAllowance
    {
        [XmlElement(ElementName = "ItemText")]
        public string ItemText { get; set; }
        [XmlElement(ElementName = "Items")]
        public List<ExpensesAllowanceItem> Items { get; set; }
    }


    public class ExpensesAllowanceItem
    {
        public string LevelId { get; set; }
        public string LevelName { get; set; }
        public int TimeAmounts { get; set; }

        /// <summary>
        /// 1 = มากกว่า 12 ชม., 0 = น้อยกว่า 12 ชม.
        /// </summary>
        public int MoreThanHalfDay { get; set; }

        public int? AllowancePersonAmounts { get; set; }
        public int? AllowanceAmountDays { get; set; }

        /// <summary>
        /// อัตราค่าเบี้ยเลี้ยง มากกว่า 12 ชม.
        /// </summary>
        public decimal? AllowanceCompensationRatePrice { get; set; }
        /// <summary>
        /// อัตราค่าเบี้ยเลี้ยงน้อยกว่า 12 ชม.
        /// </summary>
        public decimal? AllowanceCompensationRateHalfPrice { get; set; }
        public decimal? AllowanceTotalCompensationPrice { get; set; }


        public int? RentRoomPersonAmounts { get; set; }
        public int? RentRoomAmountDays { get; set; }
        /// <summary>
        /// อัตราค่าเช่าที่พักมากกว่า 12 ชม.
        /// </summary>
        public decimal? RentRoomCompensationRatePrice { get; set; }
        /// <summary>
        /// อัตราค่าเช่าที่พักน้อยกว่า 12 ชม.
        /// </summary>
        public decimal? RentRoomCompensationRateHalfPrice { get; set; }
        public decimal? RentRoomTotalCompensationPrice { get; set; }


        /// <summary>
        /// ค่ายานพาหนะและค่าน้ำมัน
        /// </summary>
        public decimal? VehicleAndOilPrice { get; set; }

        /// <summary>
        /// รวม ค่าใช้จ่ายทั้งสิ้น
        /// </summary>
        public decimal? NetExpensesPrice { get; set; }
    }
}