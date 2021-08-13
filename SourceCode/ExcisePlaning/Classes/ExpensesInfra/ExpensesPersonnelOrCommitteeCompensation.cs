using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าตอบแทนบุคคลหรือคณะกรรมการ"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesPersonnelOrCommitteeCompensation
    {
        public List<PersonnelOrCommitteeCompensationItem> CommitteeGroups { get; set; }
        public List<PersonnelOrCommitteeCompensationItem> QualityAssuranceGroups { get; set; }
    }

    public class PersonnelOrCommitteeCompensationItem
    {
        public string CommitteeGroupName { get; set; }
        public List<PersonnelOrCommitteeCompensationItemProperty> Items { get; set; }
    }

    public class PersonnelOrCommitteeCompensationItemProperty
    {
        public string CommitteeTypeName { get; set; }
        public int? CommitteeAmounts { get; set; }
        public int? AmountDays { get; set; }
        public decimal? CompensationPriceRate { get; set; }
        public decimal? TotalCompensationPrice { get; set; }
    }
}