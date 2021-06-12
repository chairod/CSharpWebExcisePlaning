using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    public class ExpensesGroupShortFieldProperty
    {
        public int EXPENSES_GROUP_ID { get; set; }

        /// <summary>
        /// ชื่อหมวดค่าใช้จ่าย 
        /// </summary>
        public string EXPENSES_GROUP_NAME { get; set; }

        /// <summary>
        /// หมวดค่าใช้จ่าย อยู่ภายใต้งบอะไร
        /// </summary>
        public string BUDGET_TYPE_NAME { get; set; }

        /// <summary>
        /// กลุ่มของ หมวดค่าใช้จ่าย
        /// </summary>
        public string EXPENSES_MASTER_NAME { get; set; }
    }
}