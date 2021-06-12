using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    public class AdjustmentBudgetResult
    {
        /// <summary>
        /// สถานะการปรับปรุง เงินงบประมาณ true = สำเร็จ
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// สาเหตุที่ไม่สามารถปรับปรุงเงินงบประมาณได้
        /// </summary>
        public string CauseErrorMessage { get; set; }

        /// <summary>
        /// เลขที่รายการจากการประมวลผล หรือ บันทึกข้อมูล
        /// </summary>
        public string RunningCode { get; set; }
    }
}