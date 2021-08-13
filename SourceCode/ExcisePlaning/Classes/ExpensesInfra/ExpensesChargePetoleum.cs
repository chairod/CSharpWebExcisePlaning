using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าเช่ารถยนต์สำหรับใช่ในการป้องกันและปราบปรามการกระทำความผิดเกี่ยวกับปิโตรเลียม"
    /// "ค่าใช้จ่ายในการป้องกันและปราบปรามการกระทำความผิดเกี่ยวกับปิโตรเลียม"
    /// "โครงการเพิ่มประสิทธิภาพปฏิบัติภารกิจการป้องกันและปราบปรามการกระทำผิดเกี่ยวกับปิโตรเลียม"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargePetoleum
    {
        public string OrgId { get; set; }
        public string OrgName { get; set; }
        public decimal? TotalPrice { get; set; }

        /// <summary>
        /// รายการค่าใช้จ่าย ที่ใช้ในแต่ละองค์กร เพื่อปิโตรเลียม
        /// </summary>
        [XmlElement(ElementName = "Expenses")]
        public List<ExpensesChargePetoleumItem> Expenses { get; set; }
    }


    public class ExpensesChargePetoleumItem
    {
        public int? BUDGET_TYPE_ID { get; set; }
        public string BUDGET_TYPE_NAME { get; set; }
        public int? EXPENSES_GROUP_ID { get; set; }
        public string EXPENSES_GROUP_NAME { get; set; }
        public string EXPENSES_MASTER_NAME { get; set; }
        public int? EXPENSES_ID { get; set; }
        public string EXPENSES_NAME { get; set; }
        public string FORM_TEMPLATE_NAME { get; set; }
        public decimal? TOTAL_REQUEST_BUDGET { get; set; }
        
        /// <summary>
        /// JSON string format
        /// </summary>
        public dynamic EXPENSES_DESCRIBEs { get; set; }
    }
}