using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าจ้างเหมาบริการ บำรุงรักษาระบบ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesChargeForSoftwareMA
    {
        public ExpensesChargeForSoftwareMA()
        {
            MASoftware = new ExpensesChargeForSoftwareMAItem() { Items = new List<ExpenseChargeForSoftwareMAItemProperty>() };
            MABuilding = new ExpensesChargeForSoftwareMAItem() { Items = new List<ExpenseChargeForSoftwareMAItemProperty>() };
            MAOther = new ExpensesChargeForSoftwareMAItem() { Items = new List<ExpenseChargeForSoftwareMAItemProperty>() };
        }

        [XmlElement(ElementName = "MASoftware")]
        public ExpensesChargeForSoftwareMAItem MASoftware { get; set; }

        [XmlElement(ElementName = "MABuilding")]
        public ExpensesChargeForSoftwareMAItem MABuilding { get; set; }

        [XmlElement(ElementName = "MAOther")]
        public ExpensesChargeForSoftwareMAItem MAOther { get; set; }
    }

    public class ExpensesChargeForSoftwareMAItem
    {
        [XmlElement(ElementName = "Items")]
        public List<ExpenseChargeForSoftwareMAItemProperty> Items { get; set; }
    }

    public class ExpenseChargeForSoftwareMAItemProperty
    {
        public string ServiceName{get;set;}

        /// <summary>
        /// เลขที่สัญญา
        /// </summary>
        public string ContractNumber { get;set;}

        /// <summary>
        /// วันที่ของสัญญา (วัน/เดือน/ปี พ.ศ.) ตย. 22/08/2551
        /// </summary>
        [JsonProperty("ContractDate")]
        [XmlElement("ContractDate")]
        public string ContractDateStr { get;set;}

        /// <summary>
        /// วันที่รับมอบงาน (วัน/เดือน/ปี พ.ศ.) ตย. 22/08/2551
        /// </summary>
        [JsonProperty("VaranteeExpireDate")]
        [XmlElement("VaranteeExpireDate")]
        public string VaranteeExpireDateStr { get;set;}

        /// <summary>
        /// วงเงินตามสัญญา
        /// </summary>
        public decimal? ContractPrice { get;set;}
        
        /// <summary>
        /// ปีที่บำรุงษา (พ.ศ.)
        /// </summary>
        public int? ServiceBeginYear { get;set;}

        /// <summary>
        /// จำนวนเงิน MA (บาท)	
        /// </summary>
        public decimal? ServicePrice { get;set;}
    }
}