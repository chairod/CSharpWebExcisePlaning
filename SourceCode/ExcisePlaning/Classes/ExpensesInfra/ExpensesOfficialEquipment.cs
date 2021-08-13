using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ครุภัณฑ์สำนักงาน"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesOfficialEquipment
    {
        public string AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetOtherFlag { get; set; }
        public string AssetOtherName { get; set; }

        /// <summary>
        /// 1 = ราคามาตรฐาน, 0 = ไม่กำหนดราคามาตรฐาน
        /// </summary>
        public string PriceType { get; set; }

        /// <summary>
        /// จำนวนที่ ความต้องการ
        /// </summary>
        public int? RequiredAmounts { get; set; }

        /// <summary>
        /// จำนวนที่มีอยู่
        /// </summary>
        public int? StockAmounts { get; set; }

        /// <summary>
        /// จำนวนที่ขอ เพิ่มเติม
        /// </summary>
        public int? RequestAmounts { get; set; }

        /// <summary>
        /// จำนวนที่ขอ ทดแทนของเดิม
        /// </summary>
        public int? ReplaceAmounts { get; set; }

        /// <summary>
        /// ราคา/หน่วย/ชุด (บาท)
        /// </summary>
        public decimal? PricePerUnit { get; set; }

        /// <summary>
        /// ราคารวม (บาท)
        /// </summary>
        public decimal? TotalPrice { get; set; }
        public string RemarkText { get; set; }
        public string AttachFilename { get; set; }
    }
}