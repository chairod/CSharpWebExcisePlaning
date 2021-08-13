using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ExcisePlaning.Classes.ExpensesInfra
{
    /// <summary>
    /// โครงสร้าง Class สำหรับ Match XML ของรายการค่าใช้จ่าย
    /// "ค่าใช้จ่ายในการสัมมนาและฝึกอบรม"
    /// </summary>
    [XmlRoot(ElementName = "root")]
    public class ExpensesTraningAndSeminors
    {
        public ExpensesTraningAndSeminors()
        {
            Activities = new List<ExpensesTraningAndSeminorsActivity>();
        }


        public string ProjectName { get; set; }

        /// <summary>
        /// 1 = สถานที่ดำเนินการ ราชการ	, 2 = สถานที่ดำเนินการ เอกชน
        /// </summary>
        public string PlaceTypeFlag { get; set; }
        public string RemarkText { get; set; }
        public List<ExpensesTraningAndSeminorsActivity> Activities { get; set; }
    }

    public class ExpensesTraningAndSeminorsActivity
    {
        public ExpensesTraningAndSeminorsActivity()
        {
            Expenses = new List<ExpensesTraningAndSeminorsActivityItem>();
        }

        public string ActivityName { get; set; }
        public List<ExpensesTraningAndSeminorsActivityItem> Expenses { get; set; }
    }

    public class ExpensesTraningAndSeminorsActivityItem
    {
        public string TraningAndSeminorsId { get; set; }
        public string TraningAndSeminorsName { get; set; }
        public int? TimeAmounts { get; set; }

        /// <summary>
        /// จำนวนคน ประเภท ก	
        /// </summary>
        public int? TypeAPersonAmounts { get; set; }

        /// <summary>
        /// จำนวนคน ประเภท ข	
        /// </summary>
        public int? TypeBPersonAmounts { get; set; }

        /// <summary>
        /// จำนวนคน บุคคลภายนอก
        /// </summary>
        public int? GuestAmounts { get; set; }

        /// <summary>
        /// จำนวนคน คณะผู้จัด
        /// </summary>
        public int? StaffAmounts { get; set; }

        /// <summary>
        /// จำนวน/รุ่น
        /// </summary>
        public int? LecturerAmounts { get; set; }
        
        public int? TotalPersonAmounts { get; set; }

        /// <summary>
        /// ระบุจำนวน/หน่วย
        /// </summary>
        public int? UnitAmounts { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }

        /// <summary>
        /// อัตราค่าใช้จ่ายที่ตั้ง
        /// </summary>
        public decimal? CompensationPrice { get; set; }
        public string CompensationUnitId { get; set; }
        public string CompensationUnitName { get; set; }
        
        public decimal? TotalPrice { get; set; }
    }
}