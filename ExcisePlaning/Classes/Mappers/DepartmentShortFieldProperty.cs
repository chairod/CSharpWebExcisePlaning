using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    public class DepartmentShortFieldProperty
    {
        public int DEP_ID { get; set; }
        public string DEP_NAME { get; set; }

        /// <summary>
        /// รหัสหน่วยรับงบประมาณ ของหน่วยงาน
        /// </summary>
        public string DEP_CODE { get; set; }
        public string DEP_SHORT_NAME { get; set; }
        public short? DEP_SORT_INDEX { get; set; }

        public int? AREA_ID { get; set; }
        public string AREA_NAME { get; set; }
    }
}