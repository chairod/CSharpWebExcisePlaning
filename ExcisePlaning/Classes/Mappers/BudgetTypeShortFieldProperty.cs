using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    /// <summary>
    /// ใช้สำหรับ Map ข้อมูลจากตาราง T_BUDGET_TYPE
    /// เพื่อแสดงผลเฉพาะ ID, Name
    /// </summary>
    public class BudgetTypeShortFieldProperty
    {
        public int BUDGET_TYPE_ID { get; set; }
        public string BUDGET_TYPE_NAME { get; set; }
    }
}