using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    /// <summary>
    /// ผลลัพธ์ของการ ตรวจสอบความสมบูรณ์ของงบประมาณ ในแต่ละปีงบประมาณ
    /// </summary>
    public class VerifyBudgetResult
    {
        public VerifyBudgetResult(int fiscalYear)
        {
            FiscalYear = FiscalYear;
            IsReleaseBudget = false;
            IsReleaseOffBudget = false;

            IsComplete = false;
            CauseMessage = new List<string>();
        }

        /// <summary>
        /// จัดรูปแบบสาเหตุของงบประมาณ ให้อยู่ในรูปแบบตอบกลับไปยังผู้ใช้งาน
        /// </summary>
        /// <returns></returns>
        public string FormatCauseMessageToUser()
        {
            StringBuilder sb = new StringBuilder();
            CauseMessage.ForEach(errorText => {
                sb.Append("<div>=> ").Append(errorText).Append("</div>");
            });
           return sb.ToString();
        }

        /// <summary>
        /// ปีงบประมาณ (ค.ศ.)
        /// </summary>
        public int FiscalYear { get; set; }

        /// <summary>
        /// true = งบประมาณในปีนั้น พร้อมใช้งาน, false = ยังไม่พร้อม ให้ไปดู CauseMessage
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// เปิดใช้เงินงบประมาณหรือยัง
        /// </summary>
        public bool IsReleaseBudget { get; set; }

        /// <summary>
        /// เงินงบประมาณคงเหลือสุทธิที่สามารถนำไปจัดสรร หรือ กันเงินได้
        /// </summary>
        public decimal BudgetBalance { get; set; }

        /// <summary>
        /// เงินนอกงบประมาณที่สามารถนำไปจัดสรร หรือ กันเงินได้
        /// </summary>
        public decimal OffBudgetBalance { get; set; }

        /// <summary>
        /// เปิดใช้เงินนอกงบประมาณหรือยัง
        /// </summary>
        public bool IsReleaseOffBudget { get; set; }

        /// <summary>
        /// สาเหตุที่งบประมาณในปีนั้น ยังไม่พร้อมใช้งาน
        /// </summary>
        public List<string> CauseMessage { get; set; }
    }
}