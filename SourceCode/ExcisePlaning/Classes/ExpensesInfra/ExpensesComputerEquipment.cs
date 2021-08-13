using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ครุภัณฑ์คอมพิวเตอร์"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesComputerEquipment
    {
        public string AssetId{get;set;}
        public string AssetName { get;set;}
        public string AssetOtherFlag { get;set;}
        public string AssetOtherName { get;set;}

        /// <summary>
        /// 1 = ราคามาตรฐาน, 2 = ไม่กำหนดราคามาตรฐาน
        /// </summary>
        public string PriceType { get;set;}

        /// <summary>
        /// ความต้องการ
        /// </summary>
        public int? RequiredAmounts { get;set;}

        /// <summary>
        /// จำนวนที่มีอยู่
        /// </summary>
        public int? StockAmounts { get;set;}

        /// <summary>
        /// จำนวนที่ขอเพิ่มเติม
        /// </summary>
        public int? RequestAmounts { get;set;}

        /// <summary>
        /// จำนวนที่ ทดแทนของเดิม
        /// </summary>
        public int? ReplaceAmounts { get;set;}

        /// <summary>
        /// ราคา/หน่วย/ชุด (บาท)
        /// </summary>
        public decimal? PricePerUnit { get;set;}

        /// <summary>
        /// ราคารวม (บาท)
        /// </summary>
        public decimal? TotalPrice { get;set;}

        /// <summary>
        /// ปัญหา อุปสรรค เหตุผลจำเป็นที่ต้องจัดหาครุภัณฑ์นี้(ต้องระบุให้ชัดเจนเพื่อใช้ประกอบการชี้แจงสำนักงบประมาณ)
        /// </summary>
        public string RemarkText { get;set;}
        
        /// <summary>
        /// ไฟล์แนบ
        /// </summary>
        public string AttachFilename { get;set;}
    }
}