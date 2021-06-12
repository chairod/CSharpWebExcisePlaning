using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes
{
    public class BudgetUtils
    {
        /// <summary>
        /// นำเงินไปลดยอดงบประมาณ เนื่องจากนำไปใช้จ่ายจัดสรร หรือ กันเงิน
        /// </summary>
        public static string ADJUSTMENT_PAY = "PAY";
        /// <summary>
        /// คืนเงินกลับไปยังส่วนกลาง
        /// </summary>
        public static string ADJUSTMENT_CASHBACK = "CASHBACK";

        /// <summary>
        /// หน่วยงานกลาง จัดสรรงบประมาณ ลงมาให้กับหน่วยงานภูมิภาค
        /// </summary>
        public static string ADJUSTMENT_ALLOCATE = "ALLOCATE";


        /// <summary>
        /// จัดรูปแบบครั้งที่จัดสรรเงินงบประมาณ
        /// เงินงบ => ง241/ + เลขรันอีก 3 หลัก, เงินนอก = ง + เลขรันอีก 3 หลัก
        /// </summary>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="allocateNumber"></param>
        /// <returns></returns>
        public static string FormatBudgetPeriod(int budgetType, int allocateNumber)
        {
            return string.Format("{0}{1}", budgetType.Equals(1) ? "ง241/" : "งน", allocateNumber.ToString("000"));
        }


        /// <summary>
        /// ปรับปรุงงบประมาณของกรมสรรพสามิต ได้แก่
        /// 1.) T_BUDGET_MASTER งบประมาณภาพรวมของกรม
        /// 2.) T_BUDGET_EXPENSES, T_BUDGET_EXPENSES_PROJECT ภาพรวมงบประมาณ รายการค่าใช้จ่าย หรือ โครงการ
        /// </summary>
        /// <param name="exprBudgetType">งบรายจ่าย ตรวจสอบการใช้งบประมาณ ใช้เป็นกลุ่ม หรือลงเป็นรายการค่าใช้จ่าย CAN_PAY_OVER_BUDGET_EXPENSES (1 = ใช้งบประมาณในภาพรวมกลุ่ม คือ แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)</param>
        /// <param name="exprBudgetMas"></param>
        /// <param name="exprExpenses"></param>
        /// <param name="exprExpensesProject"></param>
        /// <param name="totalActualAmountsByGroup">จำนวนเงิน ประจำงวด ในภาพรวมของกลุ่ม ซึ่งประกอบด้วย แผนงาน ผลผลิต กิจกรรม งบรายจ่าย และ หมวดค่าใช้จ่าย จะใช้เป็นเงื่อนไขในการตรวจสอบงบคงเหลือเพียงพอต่อการ จัดสรรหรือกันเงิน หรือไม่ เรียกอีกอย่างว่า การจัดสรรเป็นกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)</param>
        /// <param name="budgetType">ประเภทของเงินงบประมาณที่ต้องการปรับปรุง 1 = เงินงบประมาณ, 2 = เงินนอก</param>
        /// <param name="adjustmentType">PAY = ใช้จ่าย, CASHBACK = คืนเงิน</param>
        /// <param name="adjustmentAmounts">จำนวนเงินที่ต้องการนำไปปรับปรุงยอด</param>
        /// <returns></returns>
        public static AdjustmentBudgetResult DoAdjustmentOverallBudgetBalance(T_BUDGET_TYPE exprBudgetType, T_BUDGET_MASTER exprBudgetMas, T_BUDGET_EXPENSE exprExpenses, T_BUDGET_EXPENSES_PROJECT exprExpensesProject, ref decimal totalActualAmountsByGroup, int budgetType, string adjustmentType, decimal adjustmentAmounts)
        {
            var ret = new AdjustmentBudgetResult()
            {
                Completed = false,
                CauseErrorMessage = ""
            };

            if (null == exprBudgetMas)
            {
                ret.CauseErrorMessage = "ปีงบประมาณนี้ยังไม่มีการจัดสรรรายการค่าใช้จ่ายใดๆ";
                return ret;
            }

            if (null == exprExpenses)
            {
                ret.CauseErrorMessage = "รายการค่าใช้จายนี้ ยังไม่มีการจัดสรรในปีงบประมาณ";
                return ret;
            }

            decimal adjustmentBudgetAmounts = decimal.Zero;
            decimal adjustmentOffBudgetAmounts = decimal.Zero;
            if (budgetType.Equals(1))
            {
                adjustmentBudgetAmounts = adjustmentAmounts;
                if (!exprBudgetMas.RELEASE_BUDGET)
                {
                    ret.CauseErrorMessage = "กรุณาเปิดใช้งบประมาณประจำปีก่อน ถึงจะสามารถใช้งบประมาณได้";
                    return ret;
                }
            }
            else if (budgetType.Equals(2))
            {
                adjustmentOffBudgetAmounts = adjustmentAmounts;
                if (!exprBudgetMas.RELEASE_OFF_BUDGET)
                {
                    ret.CauseErrorMessage = "กรุณาเปิดใช้เงินนอกงบประมาณประจำปีก่อน ถึงจะสามารถใช้งบประมาณได้";
                    return ret;
                }
            }

            // ปรับปรุงยอดในส่วนของโครงการ
            adjustmentType = adjustmentType.ToUpper();
            if (exprExpensesProject != null)
            {
                if ("PAY".Equals(adjustmentType))
                {
                    // ตรวจสอบประเภทของโครงการ กับ ประเภทงบประมาณที่จัดสรร
                    // โครงการจะมีการแยก โครงการสำหรับเงินงบประมาณ และ โครงการสำหรับเงินนอกงบประมาณ
                    if (budgetType.Equals(1) && exprExpensesProject.PROJECT_FOR_TYPE.Equals(2))
                    {
                        ret.CauseErrorMessage = string.Format("{0} เป็นโครงการของเงินงบประมาณ ไม่สามารถใช้เงินนอกงบประมาณจัดสรรได้", exprExpensesProject.PROJECT_NAME);
                        return ret;
                    }
                    else if (budgetType.Equals(2) && exprExpensesProject.PROJECT_FOR_TYPE.Equals(1))
                    {
                        ret.CauseErrorMessage = string.Format("{0} เป็นโครงการของเงินนอกงบประมาณ ไม่สามารถใช้เงินงบประมาณจัดสรรได้", exprExpensesProject.PROJECT_NAME);
                        return ret;
                    }

                    exprExpensesProject.USE_BUDGET_AMOUNT += adjustmentBudgetAmounts;
                    exprExpensesProject.USE_OFF_BUDGET_AMOUNT += adjustmentOffBudgetAmounts;

                    totalActualAmountsByGroup -= adjustmentBudgetAmounts;
                    totalActualAmountsByGroup -= adjustmentOffBudgetAmounts;
                }
                else if ("CASHBACK".Equals(adjustmentType))
                {
                    exprExpensesProject.USE_BUDGET_AMOUNT -= adjustmentBudgetAmounts;
                    exprExpensesProject.USE_OFF_BUDGET_AMOUNT -= adjustmentOffBudgetAmounts;

                    totalActualAmountsByGroup += adjustmentBudgetAmounts;
                    totalActualAmountsByGroup += adjustmentOffBudgetAmounts;
                }

                // เงินงบประมาณ
                // งบรายจ่าย มีการใช้เงินในภาพรวม หรือ เป็นรายการ คชจ. 
                exprExpensesProject.REMAIN_BUDGET_AMOUNT = exprExpensesProject.ACTUAL_BUDGET_AMOUNT - exprExpensesProject.USE_BUDGET_AMOUNT;
                if (budgetType.Equals(1) &&
                    ((exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(0) && exprExpensesProject.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1) ||
                    (exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(1) && totalActualAmountsByGroup.CompareTo(decimal.Zero) == -1)))
                {
                    ret.CauseErrorMessage = string.Format("เงินงบประมาณของโครงการ {0} ไม่เพียงพอ", exprExpensesProject.PROJECT_NAME);
                    return ret;
                }

                // 1. เงินนอกงบประมาณ มีปีงบประมาณนั้น มีการแตก เงินประจำงวด ลงสู่รายการค่าใช้จ่าย
                // 2. งบรายจ่าย มีการใช้เงินในภาพรวม หรือ เป็นรายการ คชจ.
                exprExpensesProject.REMAIN_OFF_BUDGET_AMOUNT = exprExpensesProject.ACTUAL_OFF_BUDGET_AMOUNT - exprExpensesProject.USE_OFF_BUDGET_AMOUNT;
                if (budgetType.Equals(2))
                {
                    // ถ้ามีการรับเงินประจำงวดให้ตรวจสอบจาก เงินประจำงวด
                    if (exprBudgetMas.OFF_BUDGET_SPREAD_TO_EXPENSES == true)
                    {
                        if ((exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(0) && exprExpensesProject.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1) ||
                            (exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(1) && totalActualAmountsByGroup.CompareTo(decimal.Zero) == -1))
                        {
                            ret.CauseErrorMessage = string.Format("เงินประจำงวด - เงินนอกงบประมาณของโครงการ {0} ไม่เพียงพอ", exprExpensesProject.PROJECT_NAME);
                            return ret;
                        }
                    }
                    else
                    {
                        // ใช้ยอดตาม แผนรายรับ/รายจ่าย (เงินที่รัฐบาลจะจัดสรรมาให้)
                        var estimateRemainOffBudgetAmount = exprExpensesProject.OFF_BUDGET_AMOUNT - exprExpensesProject.USE_OFF_BUDGET_AMOUNT;
                        if ((exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(0) && estimateRemainOffBudgetAmount.CompareTo(decimal.Zero) == -1) ||
                            (exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(1) && totalActualAmountsByGroup.CompareTo(decimal.Zero) == -1))
                        {
                            ret.CauseErrorMessage = string.Format("แผนรายรับ/รายจ่าย - เงินนอกงบประมาณ ของโครงการ {0} ไม่เพียงพอ", exprExpensesProject.PROJECT_NAME);
                            return ret;
                        }
                    }
                }
            }


            // ปรับปรุงยอดส่วนของ คชจ.
            if ("PAY".Equals(adjustmentType))
            {
                exprExpenses.USE_BUDGET_AMOUNT += adjustmentBudgetAmounts;
                exprExpenses.USE_OFF_BUDGET_AMOUNT += adjustmentOffBudgetAmounts;
                exprExpenses.LATEST_USE_BUDGET = DateTime.Now;

                if (exprExpensesProject == null)
                {
                    totalActualAmountsByGroup -= adjustmentBudgetAmounts;
                    totalActualAmountsByGroup -= adjustmentOffBudgetAmounts;
                }
            }
            else if ("CASHBACK".Equals(adjustmentType))
            {
                exprExpenses.USE_BUDGET_AMOUNT -= adjustmentBudgetAmounts;
                exprExpenses.USE_OFF_BUDGET_AMOUNT -= adjustmentOffBudgetAmounts;

                if (exprExpensesProject == null)
                {
                    totalActualAmountsByGroup += adjustmentBudgetAmounts;
                    totalActualAmountsByGroup += adjustmentOffBudgetAmounts;
                }
            }

            // เงินงบประมาณ
            // งบรายจ่าย มีการใช้เงินในภาพรวม หรือ เป็นรายการ คชจ. 
            exprExpenses.REMAIN_BUDGET_AMOUNT = exprExpenses.ACTUAL_BUDGET_AMOUNT - exprExpenses.USE_BUDGET_AMOUNT;
            if (budgetType.Equals(1) &&
                ((exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(0) && exprExpenses.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1) ||
                (exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(1) && totalActualAmountsByGroup.CompareTo(decimal.Zero) == -1)))
            {
                ret.CauseErrorMessage = "เงินงบประมาณของค่าใช้จ่ายไม่เพียงพอ";
                return ret;
            }

            // 1. เงินนอกงบประมาณ มีปีงบประมาณนั้น มีการแตก เงินประจำงวด ลงสู่รายการค่าใช้จ่าย
            // 2. งบรายจ่าย มีการใช้เงินในภาพรวม หรือ เป็นรายการ คชจ.
            exprExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprExpenses.ACTUAL_OFF_BUDGET_AMOUNT - exprExpenses.USE_OFF_BUDGET_AMOUNT;
            if (budgetType.Equals(2))
            {
                // ถ้ามีการรับเงินประจำงวดให้ตรวจสอบจาก เงินประจำงวด
                if (exprBudgetMas.OFF_BUDGET_SPREAD_TO_EXPENSES == true)
                {
                    if ((exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(0) && exprExpenses.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1) ||
                        (exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(1) && totalActualAmountsByGroup.CompareTo(decimal.Zero) == -1))
                    {
                        ret.CauseErrorMessage = "เงินประจำงวด - เงินนอกงบประมาณของค่าใช้จ่ายไม่เพียงพอ";
                        return ret;
                    }
                }
                else
                {
                    // ใช้ยอดตาม แผนรายรับ/รายจ่าย (เงินที่รัฐบาลจะจัดสรรมาให้)
                    var estimateRemainOffBudgetAmount = exprExpenses.OFF_BUDGET_AMOUNT - exprExpenses.USE_OFF_BUDGET_AMOUNT;
                    if ((exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(0) && estimateRemainOffBudgetAmount.CompareTo(decimal.Zero) == -1) ||
                        (exprBudgetType.CAN_PAY_OVER_BUDGET_EXPENSES.Equals(1) && totalActualAmountsByGroup.CompareTo(decimal.Zero) == -1))
                    {
                        ret.CauseErrorMessage = "แผนรายรับ/รายจ่าย - เงินนอกงบประมาณ ของค่าใช้จ่ายไม่เพียงพอ";
                        return ret;
                    }
                }
            }


            // ปรับปรุงยอดในส่วนของงบภาพรวมทั้งกรมสรรพสามิต
            if ("PAY".Equals(adjustmentType))
            {
                exprBudgetMas.USE_BUDGET_AMOUNT += adjustmentBudgetAmounts;
                exprBudgetMas.USE_OFF_BUDGET_AMOUNT += adjustmentOffBudgetAmounts;
            }
            else if ("CASHBACK".Equals(adjustmentType))
            {
                exprBudgetMas.USE_BUDGET_AMOUNT -= adjustmentBudgetAmounts;
                exprBudgetMas.USE_OFF_BUDGET_AMOUNT -= adjustmentOffBudgetAmounts;
            }

            // เงินงบประมาณ
            exprBudgetMas.REMAIN_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_BUDGET_AMOUNT - exprBudgetMas.USE_BUDGET_AMOUNT;
            if (exprBudgetMas.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.CauseErrorMessage = "เงินงบประมาณของกรมสรรพสามิต ไม่เพียงพอ";
                return ret;
            }

            // เงินนอกงบประมาณ
            exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT = exprBudgetMas.ACTUAL_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
            if (exprBudgetMas.OFF_BUDGET_SPREAD_TO_EXPENSES)
            {
                if (exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                {
                    ret.CauseErrorMessage = "เงินประจำงวด - เงินนอกงบประมาณของกรมสรรพสามิต ไม่เพียงพอ";
                    return ret;
                }
            }
            else
            {
                var estimateRemainAmount = exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT - exprBudgetMas.USE_OFF_BUDGET_AMOUNT;
                if (estimateRemainAmount.CompareTo(decimal.Zero) == -1)
                {
                    ret.CauseErrorMessage = "แผนรายรับ/รายจ่าย - เงินนอกงบประมาณของกรมสรรพสามิต ไม่เพียงพอ";
                    return ret;
                }
            }

            ret.Completed = true;
            return ret;
        }


        /// <summary>
        /// ปรับปรุงเงินงบประมาณ หรือ เงินนอกงบประมาณ คงเหลือ ในส่วนของ 
        /// 1. ภาพรวมทั้งกรมสรรพสามิต
        /// 2. รายการค่าใช้จ่าย
        /// 3. โครงการ
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="projectId"></param>
        /// <param name="budgetType">ประเภทของเงินงบประมาณที่ต้องการปรับปรุง 1 = เงินงบประมาณ, 2 = เงินนอก</param>
        /// <param name="adjustmentType">PAY = ใช้จ่าย, CASHBACK = คืนเงิน</param>
        /// <param name="adjustmentAmounts">จำนวนเงินที่ต้องการนำไปปรับปรุงยอด</param>
        /// <returns></returns>
        public static AdjustmentBudgetResult AdjustmentOverallBudgetBalanceBy(ExcisePlaningDbDataContext db, int fiscalYear, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, int? projectId, int budgetType, string adjustmentType, decimal adjustmentAmounts)
        {
            // งบประมาณ หรือ เงินนอกงบประมาณ ในภาพรวมของกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)
            var totalActualBudgetOrOffBudgetByGroup = GetTotalActualBudgetOrOffBudgetBalanceByGroup(fiscalYear, planId, produceId, budgetTypeId, budgetType);

            // งบรายจ่าย สำหรับตรวจสอบเงื่อนไข งบรายจ่ายบางประเภท ใช้การถั่วเฉลี่ยงบประมาณในกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)
            var exprBudgetType = db.T_BUDGET_TYPEs.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId)).FirstOrDefault();

            // งบประมาณภาพรวมทั้งกรมสรรพสามิต
            var exprBudgetMas = AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_MASTER>(db).Where(e => e.YR == fiscalYear).FirstOrDefault();
            if (null == exprBudgetMas)
                exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear)).FirstOrDefault();

            // งบประมาณรายการค่าใช้จ่าย
            var exprBudgetExpenses = AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_EXPENSE>(db).Where(e => e.YR == fiscalYear
               && e.ACTIVE == 1
               && e.PLAN_ID == planId
               && e.PRODUCE_ID == produceId
               && e.ACTIVITY_ID == activityId
               && e.BUDGET_TYPE_ID == budgetTypeId
               && e.EXPENSES_GROUP_ID == expensesGroupId
               && e.EXPENSES_ID == expensesId).FirstOrDefault();
            if (null == exprBudgetExpenses)
                exprBudgetExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.YR.Equals(fiscalYear)
                    && e.ACTIVE.Equals(1)
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId).FirstOrDefault();
            // งบประมาณโครงการ
            T_BUDGET_EXPENSES_PROJECT exprBudgetProject = null;
            if (null != projectId)
            {
                exprBudgetProject = AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_EXPENSES_PROJECT>(db).Where(e => e.YR == fiscalYear
                   && e.ACTIVE == 1
                   && e.PLAN_ID == planId
                   && e.PRODUCE_ID == produceId
                   && e.ACTIVITY_ID == activityId
                   && e.BUDGET_TYPE_ID == budgetTypeId
                   && e.EXPENSES_GROUP_ID == expensesGroupId
                   && e.EXPENSES_ID == expensesId
                   && e.PROJECT_ID == projectId).FirstOrDefault();
                if (null == exprBudgetProject)
                    exprBudgetProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.YR.Equals(fiscalYear)
                        && e.ACTIVE.Equals(1)
                        && e.PLAN_ID == planId
                        && e.PRODUCE_ID == produceId
                        && e.ACTIVITY_ID == activityId
                        && e.BUDGET_TYPE_ID == budgetTypeId
                        && e.EXPENSES_GROUP_ID == expensesGroupId
                        && e.EXPENSES_ID == expensesId
                        && e.PROJECT_ID == projectId).FirstOrDefault();
            }

            return DoAdjustmentOverallBudgetBalance(exprBudgetType, exprBudgetMas
                    , exprBudgetExpenses, exprBudgetProject
                    , ref totalActualBudgetOrOffBudgetByGroup
                    , budgetType
                    , adjustmentType, adjustmentAmounts);
        }

        public static AdjustmentBudgetResult AdjustmentOverallBudgetBalanceBy(ExcisePlaningDbDataContext db, int fiscalYear, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, int? projectId, int budgetType, string adjustmentType, decimal adjustmentAmounts, ref decimal totalActualBudgetOrOffBudgetByGroup)
        {
            // งบประมาณ หรือ เงินนอกงบประมาณ ในภาพรวมของกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)
            //var totalActualBudgetOrOffBudgetByGroup = GetTotalActualBudgetOrOffBudgetAmountByGroup(fiscalYear, planId, produceId, activityId, budgetTypeId, expensesGroupId, budgetType);

            // งบรายจ่าย สำหรับตรวจสอบเงื่อนไข งบรายจ่ายบางประเภท ใช้การถั่วเฉลี่ยงบประมาณในกลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย)
            var exprBudgetType = db.T_BUDGET_TYPEs.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId)).FirstOrDefault();


            // งบประมาณภาพรวมทั้งกรมสรรพสามิต
            var exprBudgetMas = AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_MASTER>(db).Where(e => e.YR == fiscalYear).FirstOrDefault();
            if (null == exprBudgetMas)
                exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear)).FirstOrDefault();

            // งบประมาณรายการค่าใช้จ่าย
            var exprBudgetExpenses = AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_EXPENSE>(db).Where(e => e.YR == fiscalYear
                    && e.ACTIVE == 1
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId).FirstOrDefault();
            if (null == exprBudgetExpenses)
                exprBudgetExpenses = db.T_BUDGET_EXPENSEs.Where(e => e.YR.Equals(fiscalYear)
                    && e.ACTIVE.Equals(1)
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId).FirstOrDefault();
            // งบประมาณโครงการ
            T_BUDGET_EXPENSES_PROJECT exprBudgetProject = null;
            if (null != projectId)
            {
                exprBudgetProject = AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_EXPENSES_PROJECT>(db).Where(e => e.YR == fiscalYear
                   && e.ACTIVE == 1
                   && e.PLAN_ID == planId
                   && e.PRODUCE_ID == produceId
                   && e.ACTIVITY_ID == activityId
                   && e.BUDGET_TYPE_ID == budgetTypeId
                   && e.EXPENSES_GROUP_ID == expensesGroupId
                   && e.EXPENSES_ID == expensesId
                   && e.PROJECT_ID == projectId).FirstOrDefault();
                if (null == exprBudgetProject)
                    exprBudgetProject = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.YR.Equals(fiscalYear)
                        && e.ACTIVE.Equals(1)
                        && e.PLAN_ID == planId
                        && e.PRODUCE_ID == produceId
                        && e.ACTIVITY_ID == activityId
                        && e.BUDGET_TYPE_ID == budgetTypeId
                        && e.EXPENSES_GROUP_ID == expensesGroupId
                        && e.EXPENSES_ID == expensesId
                        && e.PROJECT_ID == projectId).FirstOrDefault();
            }

            return DoAdjustmentOverallBudgetBalance(exprBudgetType, exprBudgetMas
                    , exprBudgetExpenses, exprBudgetProject
                    , ref totalActualBudgetOrOffBudgetByGroup
                    , budgetType
                    , adjustmentType, adjustmentAmounts);
        }

        /// <summary>
        /// เงินงบประมาณ หรือ เงินนอกงบประมาณ ในภาพรวมของกลุ่ม (แผนงาน ผลผลิต งบรายจ่าย) ที่สามารถใช้จัดสรร หรือ กันเงิน ได้
        /// </summary>
        /// <param name="fiscalYear">ปี ค.ศ. ที่ต้องการดูงบภาพรวม</param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <returns></returns>
        public static decimal GetTotalActualBudgetOrOffBudgetBalanceByGroup(int fiscalYear, int? planId, int? produceId, int budgetTypeId, int budgetType)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var expr = db.T_BUDGET_EXPENSEs.Where(e => e.ACTIVE.Equals(1)
                    && e.YR.Equals(fiscalYear)
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.BUDGET_TYPE_ID == budgetTypeId)
                   .GroupBy(e => new { e.PLAN_ID, e.PRODUCE_ID, e.BUDGET_TYPE_ID })
                   .Select(e => new
                   {
                       // งบประมาณ
                       ACTUAL_BUDGET_AMOUNTs = e.Sum(s => s.ACTUAL_BUDGET_AMOUNT),
                       USE_BUDGET_AMOUNTs = e.Sum(s => s.USE_BUDGET_AMOUNT),
                       // นอกงบประมาณ
                       OFF_BUDGET_AMOUNTs = e.Sum(s => s.OFF_BUDGET_AMOUNT),
                       ACTUAL_OFF_BUDGET_AMOUNTs = e.Sum(s => s.ACTUAL_OFF_BUDGET_AMOUNT),
                       USE_OFF_BUDGET_AMOUNTs = e.Sum(s => s.USE_OFF_BUDGET_AMOUNT)
                   }).FirstOrDefault();

                if (null == expr)
                    return 0;

                decimal budgetBalance = expr.ACTUAL_BUDGET_AMOUNTs - expr.USE_BUDGET_AMOUNTs;
                //decimal offBudgetBalance = expr.ACTUAL_OFF_BUDGET_AMOUNTs - expr.USE_OFF_BUDGET_AMOUNTs;
                decimal offBudgetBalance = expr.OFF_BUDGET_AMOUNTs - expr.USE_OFF_BUDGET_AMOUNTs; // เงินนอกงบประมาณ ให้คำนวณจาก แผนรายรับ/รายจ่าย หรือ เงินที่รัฐบาลจะจัดสรรมาให้ 
                return budgetType.Equals(1) ? budgetBalance : offBudgetBalance;
            }
        }


        /// <summary>
        /// ตรวจสอบความพร้อมของงบประมาณ พร้อมสำหรับการนำไปจัดสรร หรือ กันเงินหรือไม่
        /// </summary>
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="budgetType">ประเภทงบประมาณ 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <returns></returns>
        public static VerifyBudgetResult VerifyBudget(int fiscalYear, int? budgetType)
        {
            var ret = new VerifyBudgetResult(fiscalYear);
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprBudgetMas = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear)).FirstOrDefault();
                if (null == exprBudgetMas)
                    ret.CauseMessage.Add("ไม่มีงบประมาณในปี งบประมาณนี้");
                else
                {
                    ret.BudgetBalance = exprBudgetMas.REMAIN_BUDGET_AMOUNT;
                    ret.IsReleaseBudget = exprBudgetMas.RELEASE_BUDGET;

                    // เงินนอกงบประมาณ จะจัดเก็บรายได้เป็นก้อน และ ไม่กระจายลงรายการค่าใช้จ่าย
                    // เงินที่นำไปจัดสรร หรือ กันเงิน ให้ดูจากยอด ที่รัฐบาลจัดสรรมาให้เป็นหลัก
                    // ผู้ใช้งาน จะเป็นคนควบคุมยอดเอง ดังนั้น มีโอกาสที่จะจัดสรรเกินเงินประจำงวด ได้
                    ret.OffBudgetBalance = exprBudgetMas.OFF_BUDGET_SPREAD_TO_EXPENSES ? exprBudgetMas.REMAIN_OFF_BUDGET_AMOUNT : exprBudgetMas.ALLOCATE_OFF_BUDGET_AMOUNT;
                    ret.IsReleaseOffBudget = exprBudgetMas.RELEASE_OFF_BUDGET;

                    if (null == budgetType)
                    {
                        // จัดเก็บรายได้ เงินงบประมาณ หรือ เงินนอกงบประมาณ หรือยัง
                        if (ret.BudgetBalance.CompareTo(decimal.Zero) == 0 && ret.OffBudgetBalance.CompareTo(decimal.Zero) == 0)
                            ret.CauseMessage.Add("งบประมาณไม่เพียงพอสำหรับจัดสรร หรือ กันเงินกรุณาบันทึกเงินประจำงวด/จัดเก็บรายได้ก่อน");

                        // เปิดใช้ เงินงบประมาณ หรือ เงินนอกงบประมาณ
                        if (!ret.IsReleaseBudget && !ret.IsReleaseOffBudget)
                            ret.CauseMessage.Add("ยังไม่สามารถนำเงินงบประมาณไปจัดสรร หรือ กันเงินได้ กรุณาเปิดใช้งบประมาณ/เงินนอกงบประมาณก่อน");
                    }
                    else
                    {
                        // จัดเก็บรายได้ เงินงบประมาณ หรือ เงินนอกงบประมาณ หรือยัง
                        if (budgetType.Value.Equals(1) && ret.BudgetBalance.CompareTo(decimal.Zero) == 0)
                            ret.CauseMessage.Add("เงินงบประมาณไม่เพียงพอสำหรับจัดสรร หรือ กันเงิน กรุณาบันทึกเงินประจำงวดของปีงบประมาณก่อน");
                        if (budgetType.Value.Equals(2) && ret.OffBudgetBalance.CompareTo(decimal.Zero) == 0)
                            ret.CauseMessage.Add("เงินนอกงบประมาณไม่เพียงพอสำหรับจัดสรร หรือ กันเงิน กรุณาจัดเก็บรายได้ก่อน");

                        // เปิดใช้ เงินงบประมาณ หรือ เงินนอกงบประมาณ
                        if (budgetType.Value.Equals(1) && !ret.IsReleaseBudget)
                            ret.CauseMessage.Add("เงินงบประมาณยังไม่สามารถนำไปจัดสรรหรือกันเงินได้ กรุณาเปิดใช้งบประมาณก่อน");
                        if (budgetType.Value.Equals(2) && !ret.IsReleaseOffBudget)
                            ret.CauseMessage.Add("เงินนอกงบประมาณยังไม่สามารถนำไปจัดสรรหรือกันเงินได้ กรุณาเปิดใช้เงินนอกงบประมาณก่อน");
                    }
                }
            }

            ret.IsComplete = ret.CauseMessage.Count == 0;
            return ret;
        }


        /// <summary>
        /// ปรับปรุงเงินงบประมาณของหน่วยงาน
        /// เช่น ได้รับจัดสรรจากส่วนกลาง หรือ ส่วนกลางขอดึงเงินคืน
        /// </summary>
        /// <param name="db"></param>
        /// <param name="depId"></param>
        /// <param name="fiscalYear">ปีงบประมาณ ค.ศ.</param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesGroupAllocateGroupFlag">1 = จัดสรรงบประมาณตามหมวดค่าใช้จ่าย</param>
        /// <param name="expensesId"></param>
        /// <param name="projectId"></param>
        /// <param name="reqId">รายการค่าใช้จ่ายที่จัดสรร เกิดจากคำขอเลขที่ใด (บางรายการอาจจะไม่ต้องมีคำขอ)</param>
        /// <param name="reqType">1 = คำขอต้นปี, 2 = คำขอเพิ่มเติม, 0 = จัดสรรโดยไม่มีคำขอ</param>
        /// <param name="allocateBudgetType">จัดสรรจาก 1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="adjustmentType">ให้เรียกใช้จาก BudgetUtils.ADJUSTMENT_ALLOCATE, BudgetUtils.ADJUSTMENT_CASHBACK</param>
        /// <param name="adjustmentAmounts"></param>
        /// <param name="periodText">งวดที่จัดสรร (รับค่าจากผู้ใช้งาน ซึ่งระบุได้เฉพาะตัวเลข ระบบจะกำหนดรูปแบบให้เช่น ง001, งน001 เป็นต้น)</param>
        /// <param name="referDocNo">เลขที่อ้างอิงของเอกสาร ในการจัดสรรงบประมาณในแต่ล่ะครั้ง</param>
        /// <returns></returns>
        public static AdjustmentBudgetResult AdjustmentDepartmentBudgetBalance(ExcisePlaningDbDataContext db, int areaId, int depId, short fiscalYear, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, short expensesGroupAllocateGroupFlag, int expensesId, int? projectId, string reqId, int reqType, int allocateBudgetType, string adjustmentType, decimal adjustmentAmounts, string periodText, string referDocNo, UserAuthorizeProperty userAuthorizeProfile)
        {
            AdjustmentBudgetResult ret = new AdjustmentBudgetResult()
            {
                Completed = true,
                CauseErrorMessage = ""
            };

            // รายการ Entity ทั้งหมดที่รอการ Insert record ใหม่
            // สำหรับตรวจหารายการที่รอ Insert เพื่อปรับปรุงข้อมูล
            // เพราะการใช้คำสั่ง Linq .Where จะไม่เห็นรายการ
            List<object> allEntityWaitInserts = db.GetChangeSet().Inserts.ToList();

            // adjustmentAmounts = Math.Abs(adjustmentAmounts);
            decimal adjustmentBudgetAmounts = allocateBudgetType.Equals(1) ? Math.Abs(adjustmentAmounts) : decimal.Zero;
            decimal adjustmentOffBudgetAmout = allocateBudgetType.Equals(2) ? Math.Abs(adjustmentAmounts) : decimal.Zero;

            var fiscalYear2Digits = (fiscalYear + 543).ToString().Substring(2);
            string periodCodeVal = FormatBudgetPeriod(allocateBudgetType, Convert.ToInt32(periodText));// string.Format("{0}{1}", allocateBudgetType.Equals(1) ? "ง241/" : "งน", Convert.ToInt32(periodText).ToString("000"));

            // งบประมาณของหน่วยงานภายนอก
            var exprDepBudget = db.T_BUDGET_ALLOCATEs.Where(e => e.DEP_ID.Equals(depId) && e.YR.Equals(fiscalYear)).FirstOrDefault();
            // ค้นหาจาก ChangeSet ของ db
            if (null == exprDepBudget)
            {
                var exprSeek = allEntityWaitInserts.Where(e => e.GetType() == typeof(T_BUDGET_ALLOCATE) && ((T_BUDGET_ALLOCATE)e).DEP_ID.Equals(depId)).Select(e => (T_BUDGET_ALLOCATE)e).FirstOrDefault();
                if (null != exprSeek && exprSeek.YR.Equals(fiscalYear))
                    exprDepBudget = exprSeek;
            }
            if (null == exprDepBudget)
            {
                string allocateId = AppUtils.GetNextKey("BUDGET_ALLOCATE.ALLOCATE_ID", string.Format("A{0}", fiscalYear2Digits), 8, false, true);
                exprDepBudget = new T_BUDGET_ALLOCATE()
                {
                    ALLOCATE_ID = allocateId,
                    AREA_ID = areaId,
                    DEP_ID = depId,
                    SUB_DEP_ID = null,
                    YR = fiscalYear,

                    ALLOCATE_BUDGET_AMOUNT = decimal.Zero, // ยอดสะสมที่จัดสรรจาก เงินงบประมาณ
                    USE_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินงบประมาณ สะสม
                    REMAIN_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินงบประมาณ

                    ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดสะสมที่จัดสรรจาก เงินนอกงบประมาณ
                    USE_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินนอกงบประมาณ สะสม
                    REMAIN_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินนอกงบประมาณ

                    NET_BUDGET_AMOUNT = decimal.Zero, // เงินงบประมาณของหน่วยงานสุทธิ
                    NET_USE_BUDGET_AMOUNT = decimal.Zero, // ใช้ไป
                    NET_REMAIN_BUDGET_AMOUNT = decimal.Zero, // คงเหลือ

                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId,
                    LATEST_REQUEST_DATETIME = DateTime.Now,
                    LATEST_REQUEST_ID = userAuthorizeProfile.EmpId
                };
                db.T_BUDGET_ALLOCATEs.InsertOnSubmit(exprDepBudget);
            }

            if (adjustmentType.Equals(ADJUSTMENT_ALLOCATE)) // จัดสรรงบให้หน่วยงาน
            {
                exprDepBudget.ALLOCATE_BUDGET_AMOUNT += adjustmentBudgetAmounts;
                exprDepBudget.ALLOCATE_OFF_BUDGET_AMOUNT += adjustmentOffBudgetAmout;
                exprDepBudget.LATEST_ALLOCATE_DATETIME = DateTime.Now;
                exprDepBudget.LATEST_ALLOCATE_ID = userAuthorizeProfile.EmpId;
            }
            else // ขอยอดจัดสรรงบประมาณคืน
            {
                exprDepBudget.ALLOCATE_BUDGET_AMOUNT -= adjustmentBudgetAmounts;
                exprDepBudget.ALLOCATE_OFF_BUDGET_AMOUNT -= adjustmentOffBudgetAmout;
            }
            exprDepBudget.NET_BUDGET_AMOUNT = exprDepBudget.ALLOCATE_BUDGET_AMOUNT + exprDepBudget.ALLOCATE_OFF_BUDGET_AMOUNT;
            exprDepBudget.NET_REMAIN_BUDGET_AMOUNT = exprDepBudget.NET_BUDGET_AMOUNT - exprDepBudget.NET_USE_BUDGET_AMOUNT;
            if (exprDepBudget.NET_REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "งบประมาณคงเหลือสุทธิของหน่วยงานน้อยกว่าศูนย์ โปรดตรวจสอบ";
                return ret;
            }
            exprDepBudget.REMAIN_BUDGET_AMOUNT = exprDepBudget.ALLOCATE_BUDGET_AMOUNT - exprDepBudget.USE_BUDGET_AMOUNT;
            if (allocateBudgetType.Equals(1) && exprDepBudget.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "เงินงบประมาณ คงเหลือสุทธิของหน่วยงานน้อยกว่าศูนย์ โปรดตรวจสอบ";
                return ret;
            }
            exprDepBudget.REMAIN_OFF_BUDGET_AMOUNT = exprDepBudget.ALLOCATE_OFF_BUDGET_AMOUNT - exprDepBudget.USE_OFF_BUDGET_AMOUNT;
            if (allocateBudgetType.Equals(2) && exprDepBudget.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "เงินนอกงบประมาณ คงเหลือสุทธิของหน่วยงานน้อยกว่าศูนย์ โปรดตรวจสอบ";
                return ret;
            }






            // ถ้าเป็นการจัดสรรงบประมาณ ตามหมวดค่าใช้จ่าย
            // ทุกรายการค่าใช้จ่ายภายใต้กลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย) 
            // จะต้องถูกจัดสรรลงไปให้หน่วยงานภูมิภาคทั้งหมด แต่จัดสรรด้วยยอด 0 บาท
            // และอ้างอิงรายการไปยัง หมวดค่าใช้จ่าย ซึ่งจะเก็บจำนวนเงินสะสมที่จัดสรรให้ไว้
            if (expensesGroupAllocateGroupFlag.Equals(1))
            {
                // บันทึกงบประมาณลงใน หมวดค่าใช้จ่าย
                var exprDepExpensesGroup = db.T_BUDGET_ALLOCATE_EXPENSES_GROUPs.Where(e => e.YR.Equals(fiscalYear)
                    && e.DEP_ID.Equals(depId)
                    && e.ACTIVE.Equals(1)
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId).FirstOrDefault();
                var isAllowToRunNextAllocateNo = true;
                // ค้นหาจาก ChangeSet ของ db
                if (null == exprDepExpensesGroup)
                {
                    var exprSeeks = allEntityWaitInserts.Where(e => e.GetType() == typeof(T_BUDGET_ALLOCATE_EXPENSES_GROUP)).Select(e => (T_BUDGET_ALLOCATE_EXPENSES_GROUP)e).ToList();
                    exprDepExpensesGroup = exprSeeks.Where(e => e.YR.Equals(fiscalYear)
                        && e.DEP_ID.Equals(depId)
                        && e.PLAN_ID == planId
                        && e.PRODUCE_ID == produceId
                        && e.ACTIVITY_ID == activityId
                        && e.BUDGET_TYPE_ID == budgetTypeId
                        && e.EXPENSES_GROUP_ID == expensesGroupId).FirstOrDefault();
                    isAllowToRunNextAllocateNo = false;
                }
                if (null == exprDepExpensesGroup)
                {
                    string allocateExpensesGroupId = AppUtils.GetNextKey("BUDGET_ALLOCATE_EXPENSES_GROUP.ID", string.Format("AG{0}", fiscalYear2Digits), 7, false, true);
                    exprDepExpensesGroup = new T_BUDGET_ALLOCATE_EXPENSES_GROUP()
                    {
                        ALLOCATE_EXPENSES_GROUP_ID = allocateExpensesGroupId,
                        ALLOCATE_ID = exprDepBudget.ALLOCATE_ID,
                        AREA_ID = areaId,
                        DEP_ID = exprDepBudget.DEP_ID,
                        SUB_DEP_ID = exprDepBudget.SUB_DEP_ID,
                        YR = exprDepBudget.YR,
                        PLAN_ID = planId,
                        PRODUCE_ID = produceId,
                        ACTIVITY_ID = activityId,
                        BUDGET_TYPE_ID = budgetTypeId,
                        EXPENSES_GROUP_ID = expensesGroupId,
                        ALLOCATE_COUNT = 1,

                        ALLOCATE_BUDGET_AMOUNT = decimal.Zero, // ยอดสะสมที่จัดสรรจาก เงินงบประมาณ
                        USE_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินงบประมาณ สะสม
                        REMAIN_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินงบประมาณ

                        ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero,// ยอดสะสมที่จัดสรรจาก เงินนอกงบประมาณ
                        USE_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินนอกงบประมาณ สะสม
                        REMAIN_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินนอกงบประมาณ

                        NET_BUDGET_AMOUNT = decimal.Zero, // งบประมาณสุทธิของรายการ คชจ.
                        NET_USE_BUDGET_AMOUNT = decimal.Zero, // ใช้ไป
                        NET_REMAIN_BUDGET_AMOUNT = decimal.Zero, // คงเหลือสุทธิ

                        ACTIVE = 1,
                        CREATED_DATETIME = DateTime.Now,
                        USER_ID = userAuthorizeProfile.EmpId,
                        UPDATED_DATETIME = null,
                        UPDATED_ID = null
                    };
                    db.T_BUDGET_ALLOCATE_EXPENSES_GROUPs.InsertOnSubmit(exprDepExpensesGroup);
                }
                else
                {
                    // นับครั้งที่จัดสรร เฉพาะการจัดสรรเพิ่ม และ ดึงรายการจากฐานข้อมูลมาปรับปรุงยอด
                    // ถ้าดึงจาก changeSet ของ dbcontext ไม่ต้องนับเพราะถือว่าเป็นการจัดสรรในครั้งเดียวกัน
                    if (ADJUSTMENT_ALLOCATE.Equals(adjustmentType) && isAllowToRunNextAllocateNo)
                        exprDepExpensesGroup.ALLOCATE_COUNT += 1;
                    exprDepExpensesGroup.UPDATED_DATETIME = DateTime.Now;
                    exprDepExpensesGroup.UPDATED_ID = userAuthorizeProfile.EmpId;
                }

                // ปรับปรุงเงินงบประมาณของ หมวดค่าใช้จ่าย
                if (adjustmentType.Equals(ADJUSTMENT_ALLOCATE)) // จัดสรรเพิ่ม
                {
                    exprDepExpensesGroup.ALLOCATE_BUDGET_AMOUNT += adjustmentBudgetAmounts;
                    exprDepExpensesGroup.ALLOCATE_OFF_BUDGET_AMOUNT += adjustmentOffBudgetAmout;
                    exprDepExpensesGroup.LAST_ALLOCATE_DATETIME = DateTime.Now;
                    exprDepExpensesGroup.LAST_ALLOCATE_ID = userAuthorizeProfile.EmpId;
                }
                else // ตัดงบประมาณคืน
                {
                    exprDepExpensesGroup.ALLOCATE_BUDGET_AMOUNT -= adjustmentBudgetAmounts;
                    exprDepExpensesGroup.ALLOCATE_OFF_BUDGET_AMOUNT -= adjustmentOffBudgetAmout;
                }
                exprDepExpensesGroup.NET_BUDGET_AMOUNT = exprDepExpensesGroup.ALLOCATE_BUDGET_AMOUNT + exprDepExpensesGroup.ALLOCATE_OFF_BUDGET_AMOUNT;
                exprDepExpensesGroup.NET_REMAIN_BUDGET_AMOUNT = exprDepExpensesGroup.NET_BUDGET_AMOUNT - exprDepExpensesGroup.NET_USE_BUDGET_AMOUNT;
                if (exprDepExpensesGroup.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = "งบประมาณคงเหลือสุทธิของรายการค่าใช้จ่ายน้อยกว่าศูนย์ โปรดตรวจสอบ";
                    return ret;
                }
                exprDepExpensesGroup.REMAIN_BUDGET_AMOUNT = exprDepExpensesGroup.ALLOCATE_BUDGET_AMOUNT - exprDepExpensesGroup.USE_BUDGET_AMOUNT;
                if (allocateBudgetType.Equals(1) && exprDepExpensesGroup.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = "เงินงบประมาณ คงเหลือสุทธิของรายการค่าใช้จ่ายน้อยกว่าศูนย์ โปรดตรวจสอบ";
                    return ret;
                }
                exprDepExpensesGroup.REMAIN_OFF_BUDGET_AMOUNT = exprDepExpensesGroup.ALLOCATE_OFF_BUDGET_AMOUNT - exprDepExpensesGroup.USE_OFF_BUDGET_AMOUNT;
                if (allocateBudgetType.Equals(2) && exprDepExpensesGroup.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = "เงินนอกงบประมาณ คงเหลือสุทธิของรายการค่าใช้จ่ายน้อยกว่าศูนย์ โปรดตรวจสอบ";
                    return ret;
                }

                // เก็บประวัติการจัดสรรงบประมาณ ของหมวดค่าใช้จ่าย
                var allocateGroupHisId = AppUtils.GetNextKey("BUDGET_ALLOCATE_EXPENSE_GRP_HIS.ID", string.Format("G{0}", fiscalYear2Digits), 8, false, true);
                db.T_BUDGET_ALLOCATE_EXPENSES_GROUP_HISTORies.InsertOnSubmit(new T_BUDGET_ALLOCATE_EXPENSES_GROUP_HISTORY()
                {
                    ALLOCATE_EXPENSES_GROUP_HIS_ID = allocateGroupHisId,
                    AREA_ID = exprDepBudget.AREA_ID,
                    DEP_ID = exprDepBudget.DEP_ID,
                    SUB_DEP_ID = exprDepBudget.SUB_DEP_ID,
                    YR = exprDepBudget.YR,
                    MN = Convert.ToInt16(DateTime.Now.Month),
                    REQ_ID = reqId, // การจัดสรรแต่ละครั้ง อาจจะไม่ต้องมีคำขอเข้ามา
                    PLAN_ID = planId,
                    PRODUCE_ID = produceId,
                    ACTIVITY_ID = activityId,
                    BUDGET_TYPE_ID = budgetTypeId,
                    EXPENSES_GROUP_ID = expensesGroupId,
                    REFER_DOC_NO = referDocNo,
                    PERIOD_CODE = periodCodeVal, //string.Format("{0}{1}", allocateBudgetType.Equals(1) ? "ง241/" : "งน", Convert.ToInt32(periodText).ToString("000")),
                    ALLOCATE_TYPE = Convert.ToInt16(reqType), // งบต้นปี หรือ งบเพิ่มเติม หรือ จัดสรรโดยไม่มีคำขอ
                    BUDGET_TYPE = Convert.ToInt16(allocateBudgetType), // จัดสรรจาก เงินงบประมาณ หรือ เงินนอกงบประมาณ
                    ADJUSTMENT_TYPE = Convert.ToInt16(adjustmentType.Equals(ADJUSTMENT_CASHBACK) ? 2 : 1), // 1 = จัดสรรเพิ่ม, 2 = ตัดเงินงบ
                    REMARK_TEXT = "รายการจัดสรรจากแบบฟอร์มจัดสรร",
                    ALLOCATE_BUDGET_AMOUNT = Math.Abs(adjustmentAmounts),
                    ALLOCATE_DATETIME = DateTime.Now,
                    ALLOCATE_ID = userAuthorizeProfile.EmpId,
                    ACTIVE = 1,
                    SEQ_NO = exprDepExpensesGroup.ALLOCATE_COUNT.Value // จัดสรรเพิ่มเติมครั้งที่
                });


                // ค้นหารายการค่าใช้จ่ายและรายละเอียดของค่าใช้จ่าย
                // ที่อยู่ภายใต้กลุ่ม (แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย) 
                // เพื่อนำมาลงในตาราง จัดสรรรายการค่าใช้จ่าย ของหน่วยงาน
                // ซึ่งจะจัดสรรเป็น 0 บาท แต่อ้างอิงรายการไปยัง หมวดค่าใช้จ่าย
                var exprAllExpensesInGroup = db.V_GET_BUDGET_EXPENSES_INFORMATIONs.Where(e => e.YR.Equals(fiscalYear)
                        && e.ACTIVE.Equals(1)
                        && e.PLAN_ID == planId
                        && e.PRODUCE_ID == produceId
                        && e.ACTIVITY_ID == activityId
                        && e.BUDGET_TYPE_ID == budgetTypeId
                        && e.EXPENSES_GROUP_ID == expensesGroupId
                        && (e.PROJECT_ID == null || (e.PROJECT_ID != null && e.PROJECT_FOR_TYPE.Equals(allocateBudgetType))))
                    .Select(e => new
                    {
                        e.PLAN_ID,
                        e.PRODUCE_ID,
                        e.ACTIVITY_ID,
                        e.BUDGET_TYPE_ID,
                        e.EXPENSES_GROUP_ID,
                        e.EXPENSES_ID,
                        e.PROJECT_ID
                    }).ToList();
                exprAllExpensesInGroup.ForEach(expr =>
                {
                    var exprNewDepExpenses = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.YR.Equals(fiscalYear)
                        && e.DEP_ID.Equals(depId)
                        && e.ACTIVE.Equals(1)
                        && e.PLAN_ID == expr.PLAN_ID
                        && e.PRODUCE_ID == expr.PRODUCE_ID
                        && e.ACTIVITY_ID == expr.ACTIVITY_ID
                        && e.BUDGET_TYPE_ID == expr.BUDGET_TYPE_ID
                        && e.EXPENSES_GROUP_ID == expr.EXPENSES_GROUP_ID
                        && e.EXPENSES_ID == expr.EXPENSES_ID
                        && (e.PROJECT_ID == null || e.PROJECT_ID.Equals(expr.PROJECT_ID))).FirstOrDefault();
                    isAllowToRunNextAllocateNo = true;
                    if (null == exprNewDepExpenses)
                    {
                        var exprSeeks = db.GetChangeSet().Inserts.Where(e => e.GetType() == typeof(T_BUDGET_ALLOCATE_EXPENSE)).Select(e => (T_BUDGET_ALLOCATE_EXPENSE)e).ToList();
                        exprNewDepExpenses = exprSeeks.Where(e => e.YR.Equals(fiscalYear)
                            && e.DEP_ID.Equals(depId)
                            && e.PLAN_ID == expr.PLAN_ID
                            && e.PRODUCE_ID == expr.PRODUCE_ID
                            && e.ACTIVITY_ID == expr.ACTIVITY_ID
                            && e.BUDGET_TYPE_ID == expr.BUDGET_TYPE_ID
                            && e.EXPENSES_GROUP_ID == expr.EXPENSES_GROUP_ID
                            && e.EXPENSES_ID == expr.EXPENSES_ID
                            && e.PROJECT_ID == expr.PROJECT_ID).FirstOrDefault();
                        isAllowToRunNextAllocateNo = false;
                    }
                    if (null == exprNewDepExpenses)
                        db.T_BUDGET_ALLOCATE_EXPENSEs.InsertOnSubmit(new T_BUDGET_ALLOCATE_EXPENSE()
                        {
                            ALLOCATE_ID = exprDepBudget.ALLOCATE_ID,
                            ALLOCATE_EXPENSES_GROUP_ID = exprDepExpensesGroup.ALLOCATE_EXPENSES_GROUP_ID,
                            AREA_ID = areaId,
                            DEP_ID = exprDepBudget.DEP_ID,
                            SUB_DEP_ID = exprDepBudget.SUB_DEP_ID,
                            YR = exprDepBudget.YR,

                            PLAN_ID = expr.PLAN_ID,
                            PRODUCE_ID = expr.PRODUCE_ID,
                            ACTIVITY_ID = expr.ACTIVITY_ID,
                            BUDGET_TYPE_ID = expr.BUDGET_TYPE_ID,
                            EXPENSES_GROUP_ID = expr.EXPENSES_GROUP_ID,
                            EXPENSES_ID = expr.EXPENSES_ID,
                            PROJECT_ID = expr.PROJECT_ID,
                            ALLOCATE_COUNT = 1,

                            ALLOCATE_BUDGET_AMOUNT = decimal.Zero, // ยอดสะสมที่จัดสรรจาก เงินงบประมาณ
                            USE_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินงบประมาณ สะสม
                            REMAIN_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินงบประมาณ

                            ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero,// ยอดสะสมที่จัดสรรจาก เงินนอกงบประมาณ
                            USE_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินนอกงบประมาณ สะสม
                            REMAIN_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินนอกงบประมาณ

                            NET_BUDGET_AMOUNT = decimal.Zero, // งบประมาณสุทธิของรายการ คชจ.
                            NET_USE_BUDGET_AMOUNT = decimal.Zero, // ใช้ไป
                            NET_REMAIN_BUDGET_AMOUNT = decimal.Zero, // คงเหลือสุทธิ

                            ACTIVE = 1,
                            CREATED_DATETIME = DateTime.Now,
                            USER_ID = userAuthorizeProfile.EmpId,
                            UPDATED_DATETIME = null,
                            UPDATED_ID = null
                        });
                    else
                    {
                        // นับเฉพาะ ครั้งที่เป็นการจัดสรร และ ค้นหาเจอจาก DB
                        if (ADJUSTMENT_ALLOCATE.Equals(adjustmentType) && isAllowToRunNextAllocateNo)
                        {
                            exprNewDepExpenses.LAST_ALLOCATE_DATETIME = DateTime.Now;
                            exprNewDepExpenses.LAST_ALLOCATE_ID = userAuthorizeProfile.EmpId;
                            exprNewDepExpenses.ALLOCATE_COUNT += 1;
                        }

                        // มีรายการอยู่แล้ว ให้เอารหัสการจัดสรรตามหมวดค่าใช้จ่ายไปอัพเดต รายการค่าใช้จ่ายด้วย
                        // เผื่อกรณีที่ จัดสรรไปแล้ว และ มาเลือกจัดสรรเป็นก้อน ในหมวดค่าใช้จ่ายนั้น
                        exprNewDepExpenses.ALLOCATE_EXPENSES_GROUP_ID = exprDepExpensesGroup.ALLOCATE_EXPENSES_GROUP_ID;
                        exprNewDepExpenses.UPDATED_DATETIME = DateTime.Now;
                        exprNewDepExpenses.UPDATED_ID = userAuthorizeProfile.EmpId;
                    }

                    // เก็บประวัติการจัดสรรงบประมาณ, ยังไม่ต้องลงประวัติรายการค่าใช้จ่าย กรณีจัดสรรเป็นหมวดค่าใช้จ่าย
                    //db.T_BUDGET_ALLOCATE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_ALLOCATE_EXPENSES_HISTORY()
                    //{
                    //    ALLOCATE_EXPENSES_GROUP_HIS_ID = allocateGroupHisId,
                    //    AREA_ID = exprDepBudget.AREA_ID,
                    //    DEP_ID = exprDepBudget.DEP_ID,
                    //    SUB_DEP_ID = exprDepBudget.SUB_DEP_ID,
                    //    YR = exprDepBudget.YR,
                    //    MN = Convert.ToInt16(DateTime.Now.Month),
                    //    REQ_ID = reqId, // การจัดสรรแต่ละครั้ง อาจจะไม่ต้องมีคำขอเข้ามา

                    //    PLAN_ID = expr.PLAN_ID,
                    //    PRODUCE_ID = expr.PRODUCE_ID,
                    //    ACTIVITY_ID = expr.ACTIVITY_ID,
                    //    BUDGET_TYPE_ID = expr.BUDGET_TYPE_ID,
                    //    EXPENSES_GROUP_ID = expr.EXPENSES_GROUP_ID,
                    //    EXPENSES_ID = expr.EXPENSES_ID,
                    //    PROJECT_ID = expr.PROJECT_ID,

                    //    REFER_DOC_NO = referDocNo,
                    //    PERIOD_CODE = periodCodeVal,
                    //    ALLOCATE_TYPE = Convert.ToInt16(reqType), // งบต้นปี หรือ งบเพิ่มเติม หรือ จัดสรรโดยไม่มีคำขอ
                    //    BUDGET_TYPE = Convert.ToInt16(allocateBudgetType), // จัดสรรจาก เงินงบประมาณ หรือ เงินนอกงบประมาณ
                    //    ADJUSTMENT_TYPE = Convert.ToInt16(adjustmentType.Equals(ADJUSTMENT_CASHBACK) ? 2 : 1), // 1 = จัดสรรเพิ่ม, 2 = ตัดเงินงบ
                    //    REMARK_TEXT = "รายการจัดสรรจากแบบฟอร์มจัดสรร",
                    //    ALLOCATE_BUDGET_AMOUNT = decimal.Zero, // จัดสรรเป็น 0 บาทเสมอ เพราะจัดสรรเป็นหมวดค่าใช้จ่าย
                    //    ALLOCATE_DATETIME = DateTime.Now,
                    //    ALLOCATE_ID = userAuthorizeProfile.EmpId,
                    //    ACTIVE = 1,
                    //    SEQ_NO = exprDepExpenses.ALLOCATE_COUNT.Value // จัดสรรเพิ่มเติมครั้งที่
                    //});
                });


                return ret;
            }







            // บันทึกงบประมาณ ของแต่ละรายการค่าใช้จ่ายให้กับหน่วยงาน (จัดสรรตามรายการค่าใช้จ่าย)
            var exprDepExpenses = db.T_BUDGET_ALLOCATE_EXPENSEs.Where(e => e.YR.Equals(fiscalYear)
                && e.DEP_ID.Equals(depId)
                && e.ACTIVE.Equals(1)
                && e.PLAN_ID == planId
                && e.PRODUCE_ID == produceId
                && e.ACTIVITY_ID == activityId
                && e.BUDGET_TYPE_ID == budgetTypeId
                && e.EXPENSES_GROUP_ID == expensesGroupId
                && e.EXPENSES_ID == expensesId
                && (e.PROJECT_ID == null || e.PROJECT_ID.Equals(projectId))).FirstOrDefault();
            var isCanCountExpensesAllocate = exprDepExpenses != null;
            // ค้นหาจาก ChangeSet ของ db
            if (null == exprDepExpenses)
            {
                var exprSeeks = allEntityWaitInserts.Where(e => e.GetType() == typeof(T_BUDGET_ALLOCATE_EXPENSE) && ((T_BUDGET_ALLOCATE_EXPENSE)e).DEP_ID.Equals(depId)).Select(e => (T_BUDGET_ALLOCATE_EXPENSE)e).ToList();
                exprDepExpenses = exprSeeks.Where(e => e.YR.Equals(fiscalYear)
                    && e.PLAN_ID == planId
                    && e.PRODUCE_ID == produceId
                    && e.ACTIVITY_ID == activityId
                    && e.BUDGET_TYPE_ID == budgetTypeId
                    && e.EXPENSES_GROUP_ID == expensesGroupId
                    && e.EXPENSES_ID == expensesId
                    && e.PROJECT_ID == projectId).FirstOrDefault();
            }
            if (null == exprDepExpenses)
            {
                exprDepExpenses = new T_BUDGET_ALLOCATE_EXPENSE()
                {
                    ALLOCATE_ID = exprDepBudget.ALLOCATE_ID,
                    AREA_ID = areaId,
                    DEP_ID = exprDepBudget.DEP_ID,
                    SUB_DEP_ID = exprDepBudget.SUB_DEP_ID,
                    YR = exprDepBudget.YR,
                    PLAN_ID = planId,
                    PRODUCE_ID = produceId,
                    ACTIVITY_ID = activityId,
                    BUDGET_TYPE_ID = budgetTypeId,
                    EXPENSES_GROUP_ID = expensesGroupId,
                    EXPENSES_ID = expensesId,
                    PROJECT_ID = projectId,
                    ALLOCATE_COUNT = 1,

                    ALLOCATE_BUDGET_AMOUNT = decimal.Zero, // ยอดสะสมที่จัดสรรจาก เงินงบประมาณ
                    USE_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินงบประมาณ สะสม
                    REMAIN_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินงบประมาณ

                    ALLOCATE_OFF_BUDGET_AMOUNT = decimal.Zero,// ยอดสะสมที่จัดสรรจาก เงินนอกงบประมาณ
                    USE_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดใช้จ่ายเงินนอกงบประมาณ สะสม
                    REMAIN_OFF_BUDGET_AMOUNT = decimal.Zero, // ยอดคงเหลือสุทธิ เงินนอกงบประมาณ

                    NET_BUDGET_AMOUNT = decimal.Zero, // งบประมาณสุทธิของรายการ คชจ.
                    NET_USE_BUDGET_AMOUNT = decimal.Zero, // ใช้ไป
                    NET_REMAIN_BUDGET_AMOUNT = decimal.Zero, // คงเหลือสุทธิ

                    ACTIVE = 1,
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId,
                    UPDATED_DATETIME = null,
                    UPDATED_ID = null
                };
                db.T_BUDGET_ALLOCATE_EXPENSEs.InsertOnSubmit(exprDepExpenses);
            }
            else
            {
                // นับครั้งที่จัดสรร เฉพาะการจัดสรรเพิ่ม และ ดึงรายการจากฐานข้อมูลมาปรับปรุงยอด
                // ถ้าดึงจาก changeSet ของ dbcontext ไม่ต้องนับเพราะถือว่าเป็นการจัดสรรในครั้งเดียวกัน
                if (ADJUSTMENT_ALLOCATE.Equals(adjustmentType) && isCanCountExpensesAllocate)
                    exprDepExpenses.ALLOCATE_COUNT += 1;
                exprDepExpenses.UPDATED_DATETIME = DateTime.Now;
                exprDepExpenses.UPDATED_ID = userAuthorizeProfile.EmpId;
            }


            if (adjustmentType.Equals(ADJUSTMENT_ALLOCATE)) // จัดสรรเพิ่ม
            {
                exprDepExpenses.ALLOCATE_BUDGET_AMOUNT += adjustmentBudgetAmounts;
                exprDepExpenses.ALLOCATE_OFF_BUDGET_AMOUNT += adjustmentOffBudgetAmout;
                exprDepExpenses.LAST_ALLOCATE_DATETIME = DateTime.Now;
                exprDepExpenses.LAST_ALLOCATE_ID = userAuthorizeProfile.EmpId;
            }
            else // ตัดงบประมาณคืน
            {
                exprDepExpenses.ALLOCATE_BUDGET_AMOUNT -= adjustmentBudgetAmounts;
                exprDepExpenses.ALLOCATE_OFF_BUDGET_AMOUNT -= adjustmentOffBudgetAmout;
            }
            exprDepExpenses.NET_BUDGET_AMOUNT = exprDepExpenses.ALLOCATE_BUDGET_AMOUNT + exprDepExpenses.ALLOCATE_OFF_BUDGET_AMOUNT;
            exprDepExpenses.NET_REMAIN_BUDGET_AMOUNT = exprDepExpenses.NET_BUDGET_AMOUNT - exprDepExpenses.NET_USE_BUDGET_AMOUNT;
            if (exprDepExpenses.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "งบประมาณคงเหลือสุทธิของรายการค่าใช้จ่ายน้อยกว่าศูนย์ โปรดตรวจสอบ";
                return ret;
            }
            exprDepExpenses.REMAIN_BUDGET_AMOUNT = exprDepExpenses.ALLOCATE_BUDGET_AMOUNT - exprDepExpenses.USE_BUDGET_AMOUNT;
            if (allocateBudgetType.Equals(1) && exprDepExpenses.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "เงินงบประมาณ คงเหลือสุทธิของรายการค่าใช้จ่ายน้อยกว่าศูนย์ โปรดตรวจสอบ";
                return ret;
            }
            exprDepExpenses.REMAIN_OFF_BUDGET_AMOUNT = exprDepExpenses.ALLOCATE_OFF_BUDGET_AMOUNT - exprDepExpenses.USE_OFF_BUDGET_AMOUNT;
            if (allocateBudgetType.Equals(2) && exprDepExpenses.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "เงินนอกงบประมาณ คงเหลือสุทธิของรายการค่าใช้จ่ายน้อยกว่าศูนย์ โปรดตรวจสอบ";
                return ret;
            }

            // เก็บประวัติการจัดสรรงบประมาณ
            db.T_BUDGET_ALLOCATE_EXPENSES_HISTORies.InsertOnSubmit(new T_BUDGET_ALLOCATE_EXPENSES_HISTORY()
            {
                AREA_ID = exprDepBudget.AREA_ID,
                DEP_ID = exprDepBudget.DEP_ID,
                SUB_DEP_ID = exprDepBudget.SUB_DEP_ID,
                YR = exprDepBudget.YR,
                MN = Convert.ToInt16(DateTime.Now.Month),
                REQ_ID = reqId, // การจัดสรรแต่ละครั้ง อาจจะไม่ต้องมีคำขอเข้ามา
                PLAN_ID = planId,
                PRODUCE_ID = produceId,
                ACTIVITY_ID = activityId,
                BUDGET_TYPE_ID = budgetTypeId,
                EXPENSES_GROUP_ID = expensesGroupId,
                EXPENSES_ID = expensesId,
                PROJECT_ID = projectId,
                REFER_DOC_NO = referDocNo,
                PERIOD_CODE = periodCodeVal, //string.Format("{0}{1}", allocateBudgetType.Equals(1) ? "ง241/" : "งน", Convert.ToInt32(periodText).ToString("000")),
                ALLOCATE_TYPE = Convert.ToInt16(reqType), // งบต้นปี หรือ งบเพิ่มเติม หรือ จัดสรรโดยไม่มีคำขอ
                BUDGET_TYPE = Convert.ToInt16(allocateBudgetType), // จัดสรรจาก เงินงบประมาณ หรือ เงินนอกงบประมาณ
                ADJUSTMENT_TYPE = Convert.ToInt16(adjustmentType.Equals(ADJUSTMENT_CASHBACK) ? 2 : 1), // 1 = จัดสรรเพิ่ม, 2 = ตัดเงินงบ
                REMARK_TEXT = "รายการจัดสรรจากแบบฟอร์มจัดสรร",
                ALLOCATE_BUDGET_AMOUNT = Math.Abs(adjustmentAmounts),
                ALLOCATE_DATETIME = DateTime.Now,
                ALLOCATE_ID = userAuthorizeProfile.EmpId,
                ACTIVE = 1,
                SEQ_NO = exprDepExpenses.ALLOCATE_COUNT.Value // จัดสรรเพิ่มเติมครั้งที่
            });

            return ret;
        }

        /// <summary>
        /// ปรับปรุงรายการเบิกจ่าย เช่น เบิกเกินส่งคืน หรือ ปรับปรุงบัญชี เป็นต้น
        /// 1. เบิกเกินส่งคืน เกิดจาก การขอเบิกเงินไปใช้จ่ายในกิจกรรมต่างๆ เกินยอดที่ใช้จ่ายจริง และต้องการนำเงินคืนกลับไปให้ส่วนกลาง
        /// 2. ปรับปรุงบัญชี เกิดจาก ขอเบิกจ่ายผิดประเภทงบ (เงินงบประมาณ หรือ เงินนอกงบประมาณ) หรือ ผิดหมวด (แผนงาน ผลผลิต กิจกรรม ... โครงการ)
        /// 
        /// </summary>
        /// <param name="reserveId">เลขที่กันเงิน เพื่อรองรับ เบิกจ่ายเลขเดียวมากกว่า 1 ใบกัน</param>
        /// <param name="withdrawalCode">เลขที่เบิกจ่าย</param>
        /// <param name="referDocNo">เลขที่อ้างอิงเอกสาร กรณี tranType = 3 ปรับปรุงบัญชี</param>
        /// <param name="cashbackReferCode">เลขที่เบิกเกินส่งคืน จำนวน 10 หลัก</param>
        /// <param name="adjustmentAmount">ยอดเงินที่ขอปรับปรุง ถ้าปรับปรุงเท่ากับยอดเบิกจ่าย จะถูกยกเลิกรายการออก</param>
        /// <param name="tranType">2 = เบิกเกินส่งคืน, 3 = ปรับปรุงบัญชี</param>
        /// <param name="remarkText">หมายเหตุอื่นๆ ที่ต้องการระบุ ไม่เกิน 120 ตัวอักษร</param>
        /// <returns></returns>
        public static AdjustmentBudgetResult DoCashbackReserveBudgetWithdrawal(ExcisePlaningDbDataContext db, string reserveId, string withdrawalCode, string referDocNo, string cashbackReferCode, decimal adjustmentAmount, short tranType, string remarkText, UserAuthorizeProperty userAuthorizeProfile)
        {
            var ret = new AdjustmentBudgetResult();
            ret.Completed = true;
            ret.CauseErrorMessage = "";

            // หากส่งค่ามาติดลบ ให้แปลง เป็นจำนวนเต็ม
            adjustmentAmount = Math.Abs(adjustmentAmount);
            //using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            //{
            var exprReserveWithdrawal = db.T_BUDGET_RESERVE_WITHDRAWALs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(reserveId) && e.WITHDRAWAL_CODE.Equals(withdrawalCode)).FirstOrDefault();
            if (null == exprReserveWithdrawal)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = string.Format("ไม่พบเลขที่เบิกจ่าย {0} หรือถูกยกเลิกไปแล้ว", withdrawalCode);
                return ret;
            }

            int compareVal = exprReserveWithdrawal.WITHDRAWAL_AMOUNT.CompareTo(adjustmentAmount);
            if (compareVal == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = string.Format("ยอดปรับปรุง ({0}) มากกว่า ยอดเบิกจ่าย ({1})", adjustmentAmount.ToString("#,##.00"), exprReserveWithdrawal.WITHDRAWAL_AMOUNT.ToString("#,##0.00"));
                return ret;
            }
            else if (compareVal == 0)
            {
                // ยอดเงินที่ขอปรับปรุง = ยอดเบิกจ่าย ให้ยกเลิกใบนี้ออก
                exprReserveWithdrawal.ACTIVE = -1;
                exprReserveWithdrawal.DELETED_DATETIME = DateTime.Now;
                exprReserveWithdrawal.DELETED_ID = userAuthorizeProfile.EmpId;
                exprReserveWithdrawal.REJECT_REMARK_TEXT = "ดำเนินการโดยระบบ - ขอปรับปรุงยอดเท่ากับเบิกจ่าย";
            }

            //var reserveId = exprReserveWithdrawal.RESERVE_ID;
            var withdrawalSeqNo = exprReserveWithdrawal.SEQ_NO;

            // เก็บประวัติการเปลี่ยนแปลงรายการเบิกจ่าย
            db.T_BUDGET_RESERVE_WITHDRAWAL_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_WITHDRAWAL_HISTORY()
            {
                RESERVE_ID = reserveId,
                WITHDRAWAL_SEQ_NO = withdrawalSeqNo,
                // ลำดับที่ของ ประวัติการปรับปรุงรายการเบิกจ่าย
                SEQ_NO = db.T_BUDGET_RESERVE_WITHDRAWAL_HISTORies.Where(e => e.RESERVE_ID.Equals(reserveId)
                    && e.WITHDRAWAL_SEQ_NO.Equals(withdrawalSeqNo)).Count() + 1,

                DEP_ID = exprReserveWithdrawal.DEP_ID,
                SUB_DEP_ID = exprReserveWithdrawal.SUB_DEP_ID,

                TRAN_TYPE = tranType, // 2 = เบิกเกินส่งคืน, 3 = ปรับปรุงบัญชี
                WITHDRAWAL_CODE = cashbackReferCode, //withdrawalCode,
                REFER_DOC_CODE = referDocNo, // เลขที่เอกสารการปรับปรุงบัญชี
                CURR_WITHDRAWAL_AMOUNT = exprReserveWithdrawal.WITHDRAWAL_AMOUNT,
                ADJUSTMENT_AMOUNT = adjustmentAmount,
                CASHBACK_AMOUNT = adjustmentAmount,
                BALANCE_AMOUNT = exprReserveWithdrawal.WITHDRAWAL_AMOUNT - adjustmentAmount,
                CREATED_DATETIME = DateTime.Now,
                REMARK_TEXT = remarkText,
                USER_ID = userAuthorizeProfile.EmpId
            });

            // ปรับปรุงยอดเบิกจ่าย
            exprReserveWithdrawal.WITHDRAWAL_AMOUNT = exprReserveWithdrawal.WITHDRAWAL_AMOUNT - adjustmentAmount;



            // ปรับปรุงยอดในใบกันเงิน
            var exprBudgetReserve = db.T_BUDGET_RESERVEs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(reserveId)).FirstOrDefault();
            if (null == exprBudgetReserve)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = string.Format("ไม่พบข้อมูลเลขที่กันเงิน โปรดตรวจสอบ (เลขที่เบิกจ่าย: {0}, เลขที่กันเงิน: {1})", withdrawalCode, reserveId);
                return ret;
            }

            var oldReserveAmounts = exprBudgetReserve.RESERVE_BUDGET_AMOUNT;
            //var oldUseAmount = exprBudgetReserve.USE_AMOUNT;
            exprBudgetReserve.USE_AMOUNT -= adjustmentAmount;
            exprBudgetReserve.RESERVE_BUDGET_AMOUNT -= adjustmentAmount;
            exprBudgetReserve.REMAIN_AMOUNT = exprBudgetReserve.RESERVE_BUDGET_AMOUNT - exprBudgetReserve.USE_AMOUNT;
            exprBudgetReserve.CASHBACK_AMOUNT += adjustmentAmount;
            if (exprBudgetReserve.REMAIN_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "หลังจากปรับปรุงยอดกันเงิน จำนวนเงินน้อยกว่าศูนย์ โปรดตรวจสอบ";
                return ret;
            }
            // เก็บประวัติการปรับปรุงยอดกันเงิน
            short reserveHisTranType = 5; // ปรับปรุงยอด จากการเบิกเกินส่งคืน
            if (tranType.Equals(3))
                reserveHisTranType = 6; // ปรับปรุงยอด จากการปรับปรุงบัญชี
            db.T_BUDGET_RESERVE_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_HISTORY()
            {
                RESERVE_ID = reserveId,
                SEQ_NO = db.T_BUDGET_RESERVE_HISTORies.Where(e => e.RESERVE_ID.Equals(reserveId)).Count() + 1,
                DEP_ID = exprBudgetReserve.DEP_ID,
                SUB_DEP_ID = exprBudgetReserve.SUB_DEP_ID,
                TRAN_TYPE = reserveHisTranType,
                ADJUSTMENT_REFER_CODE = cashbackReferCode,
                BUDGET_TYPE = exprBudgetReserve.BUDGET_TYPE,
                RESERVE_TYPE = exprBudgetReserve.RESERVE_TYPE,

                CURR_RESERVE_AMOUNT = oldReserveAmounts,
                CURR_WITHDRAWAL_AMOUNT = exprBudgetReserve.USE_AMOUNT,
                ADJUSTMENT_AMOUNT = adjustmentAmount,
                CASHBACK_AMOUNT = adjustmentAmount,
                BALANCE_AMOUNT = exprBudgetReserve.REMAIN_AMOUNT,

                REMARK_TEXT = remarkText,
                CREATED_DATETIME = DateTime.Now,
                USER_ID = userAuthorizeProfile.EmpId,
                LATEST_WITHDRAWAL_DATETIME = exprBudgetReserve.LATEST_WITHDRAWAL_DATETIME,
                LATEST_WITHDRAWAL_ID = exprBudgetReserve.LATEST_WITHDRAWAL_ID
            });


            // ส่งเงินคืนให้ส่วนกลาง
            var result = AdjustmentOverallBudgetBalanceBy(db, exprBudgetReserve.YR, exprBudgetReserve.PLAN_ID, exprBudgetReserve.PRODUCE_ID
                    , exprBudgetReserve.ACTIVITY_ID, exprBudgetReserve.BUDGET_TYPE_ID, exprBudgetReserve.EXPENSES_GROUP_ID
                    , exprBudgetReserve.EXPENSES_ID, exprBudgetReserve.PROJECT_ID, exprBudgetReserve.BUDGET_TYPE
                    , ADJUSTMENT_CASHBACK, adjustmentAmount);
            if (!result.Completed)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = result.CauseErrorMessage;
                return ret;
            }


            //    db.SubmitChanges();
            //}

            return ret;
        }


        /// <summary>
        /// กันเงินงบประมาณ
        /// </summary>
        /// <param name="db"></param>
        /// <param name="reserveId">เลขที่กันเงิน ถ้าผ่านค่าเข้ามาจะเป็นการปรับปรุง ค่าดังนี้ หน่วยงานที่กันเงิน หมายเหตุ และ วันที่กันเงิน</param>
        /// <param name="fiscalYear">ปี ค.ศ.</param>
        /// <param name="depId"></param>
        /// <param name="planId"></param>
        /// <param name="produceId"></param>
        /// <param name="activityId"></param>
        /// <param name="budgetTypeId"></param>
        /// <param name="expensesGroupId"></param>
        /// <param name="expensesId"></param>
        /// <param name="budgetType">1 = เงินงบประมาณ, 2 = เงินนอกงบประมาณ</param>
        /// <param name="reserveType">1 = ผูกพัน, 2 = กันไว้เบิก</param>
        /// <param name="reserveAmount">ยอดเงินที่ต้องการกัน</param>
        /// <param name="reserveDate">วันที่กันเงิน (รับค่าจากผู้ใช้งาน)</param>
        /// <param name="remarkText"></param>
        /// <returns></returns>
        public static AdjustmentBudgetResult DoReserveBudget(ExcisePlaningDbDataContext db, string reserveId, int fiscalYear, int depId, int? planId, int? produceId, int? activityId, int budgetTypeId, int expensesGroupId, int expensesId, int? projectId, short budgetType, short reserveType, decimal reserveAmounts, DateTime reserveDate, string remarkText, UserAuthorizeProperty userAuthorizeProfile)
        {
            var ret = new AdjustmentBudgetResult();
            ret.Completed = true;
            ret.CauseErrorMessage = "";

            //var depId = db.T_SUB_DEPARTMENTs.Where(e => e.SUB_DEP_ID.Equals(subDepId)).Select(e => e.DEP_ID).FirstOrDefault();
            var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.RESERVE_ID.Equals(reserveId)).FirstOrDefault();
            if (null != exprReserve)
            {
                if (exprReserve.ACTIVE.Equals(-1))
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = string.Format("ไม่สามารถกันเงินใบกันที่ยกเลิกไปแล้ว วันที่ยกเลิก {0}", exprReserve.DELETED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return ret;
                }

                ret.RunningCode = reserveId;
                exprReserve.DEP_ID = depId;
                exprReserve.SUB_DEP_ID = null;
                exprReserve.RESERVE_DATE = reserveDate;
                exprReserve.REMARK_TEXT = remarkText;
            }
            else
            {
                // ขอกันเงินจากส่วนกลาง
                ret = BudgetUtils.AdjustmentOverallBudgetBalanceBy(db, fiscalYear
                        , planId, produceId, activityId, budgetTypeId
                        , expensesGroupId, expensesId
                        , projectId, budgetType
                        , BudgetUtils.ADJUSTMENT_PAY, reserveAmounts);
                if (!ret.Completed)
                    return ret;


                // กันเงินงบประมาณให้กับหน่วยงาน
                var activityOrderSeq = db.T_ACTIVITY_CONFIGUREs.Where(e => e.ACTIVITY_ID.Equals(activityId)).Select(e => e.ORDER_SEQ).FirstOrDefault();
                var budgetTypeOrderSeq = db.T_BUDGET_TYPEs.Where(e => e.BUDGET_TYPE_ID.Equals(budgetTypeId)).Select(e => e.ORDER_SEQ).FirstOrDefault();
                var expensesGroupOrderSeq = db.T_EXPENSES_GROUPs.Where(e => e.EXPENSES_GROUP_ID.Equals(expensesGroupId)).Select(e => e.ORDER_SEQ).FirstOrDefault();

                var fiscalYear2Digits = (fiscalYear + 543).ToString().Substring(2);
                reserveId = AppUtils.GetNextKey(string.Format("BUDGET_RESERVE.RESERVE_ID_{0}_{1}_{2}_{3}", fiscalYear2Digits, budgetType, activityId, budgetTypeId)
                    , string.Format("{0}{1}{2}{3}{4}"
                        , fiscalYear2Digits
                        , budgetType
                        , activityOrderSeq
                        , budgetTypeOrderSeq
                        , expensesGroupOrderSeq), 4, false, true);
                ret.RunningCode = reserveId;
                exprReserve = new T_BUDGET_RESERVE()
                {
                    RESERVE_ID = reserveId,
                    DEP_ID = depId,
                    SUB_DEP_ID = null,
                    YR = Convert.ToInt16(fiscalYear),
                    RESERVE_DATE = reserveDate,

                    PLAN_ID = planId,
                    PRODUCE_ID = produceId,
                    ACTIVITY_ID = activityId,
                    BUDGET_TYPE_ID = budgetTypeId,
                    EXPENSES_GROUP_ID = expensesGroupId,
                    EXPENSES_ID = expensesId,
                    PROJECT_ID = projectId,

                    RESERVE_TYPE = reserveType,
                    BUDGET_TYPE = budgetType,

                    RESERVE_BUDGET_AMOUNT = reserveAmounts,
                    USE_AMOUNT = decimal.Zero,
                    REMAIN_AMOUNT = reserveAmounts,
                    CASHBACK_AMOUNT = decimal.Zero,

                    REMARK_TEXT = remarkText,
                    ACTIVE = 1,
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId
                };
                db.T_BUDGET_RESERVEs.InsertOnSubmit(exprReserve);

                // บันทึกประวัติการกันเงินงบประมาณ
                db.T_BUDGET_RESERVE_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_HISTORY()
                {
                    RESERVE_ID = exprReserve.RESERVE_ID,
                    SEQ_NO = 1,
                    DEP_ID = exprReserve.DEP_ID,
                    SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                    TRAN_TYPE = 1, // กันเงินงบประมาณ
                    BUDGET_TYPE = exprReserve.BUDGET_TYPE,
                    RESERVE_TYPE = exprReserve.RESERVE_TYPE,
                    CURR_RESERVE_AMOUNT = exprReserve.RESERVE_BUDGET_AMOUNT,
                    ADJUSTMENT_AMOUNT = decimal.Zero,
                    CASHBACK_AMOUNT = decimal.Zero,
                    BALANCE_AMOUNT = exprReserve.RESERVE_BUDGET_AMOUNT,
                    CURR_WITHDRAWAL_AMOUNT = decimal.Zero,
                    REMARK_TEXT = exprReserve.REMARK_TEXT,
                    CREATED_DATETIME = DateTime.Now,
                    USER_ID = userAuthorizeProfile.EmpId,
                    LATEST_WITHDRAWAL_DATETIME = null,
                    LATEST_WITHDRAWAL_ID = null
                });
            }

            return ret;
        }


        /// <summary>
        /// เบิกจ่ายงบประมาณที่กันไว้ ไปใช้จ่าย
        /// </summary>
        /// <param name="db"></param>
        /// <param name="reserveId"></param>
        /// <param name="withdrawalCode">เลขที่เอกสารการเบิกจ่าย</param>
        /// <param name="referDocNo">เลขที่เอกสาร อ้างอิงสำหรับรายการ ปรับปรุงบัญชี</param>
        /// <param name="withdrawalAmounts">จำนวนที่ขอเบิกจ่าย (บาท)</param>
        /// <param name="withdrawalDate">วันที่ขอเบิกจ่าย (รับค่าจากผู้ใช้งาน)</param>
        /// <param name="withdrawalType">1 = เบิกจ่ายปกติ, 2 = เบิกจ่ายจากการปรับปรุงบัญชี</param>
        /// <param name="remarkText"></param>
        /// <param name="referReserveId">กรณี withdrawalType = 2 จะอ้างอิงไปยัง ข้อมูลการเบิกจ่ายตัวที่ขอ ปรับปรุง</param>
        /// <param name="referWithdrawalSeqNo">กรณี withdrawalType = 2 จะอ้างอิงไปยัง ข้อมูลการเบิกจ่ายตัวที่ขอ ปรับปรุง</param>
        /// <param name="userAuthorizeProfile"></param>
        /// <returns></returns>
        public static AdjustmentBudgetResult DoWithdrawalReserveBudget(ExcisePlaningDbDataContext db, string reserveId, string withdrawalCode, string referDocNo, decimal withdrawalAmounts, DateTime? withdrawalDate, short withdrawalType, string remarkText, string referReserveId, short? referWithdrawalSeqNo, UserAuthorizeProperty userAuthorizeProfile)
        {
            var ret = new AdjustmentBudgetResult();
            ret.Completed = true;
            ret.CauseErrorMessage = "";

            var exprReserve = db.T_BUDGET_RESERVEs.Where(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(reserveId)).FirstOrDefault();
            if (null == exprReserve)// ค้นหาจากฐานข้อมูลไม่พบ ให้หาจากรายการอ Insert ของ Entity
                exprReserve = db.GetChangeSet().Inserts.Where(e => e.GetType() == typeof(T_BUDGET_RESERVE) && ((T_BUDGET_RESERVE)e).RESERVE_ID.Equals(reserveId)).Select(e => (T_BUDGET_RESERVE)e).FirstOrDefault();

            if (null == exprReserve)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "ไม่พบเลขที่ใบกันเงิน ที่ระบุ";
                return ret;
            }
            else if (withdrawalDate.Value.CompareTo(exprReserve.RESERVE_DATE.Value) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = string.Format("วันที่ขอเบิก จะต้อง มากกว่าหรือเท่ากับ วันที่กันเงิน (วันที่กันเงิน: {0})", exprReserve.RESERVE_DATE.Value.ToString("dd/MM/yyyy", AppUtils.ThaiCultureInfo)); ;
                return ret;
            }

            var appSettings = AppSettingProperty.ParseXml();
            if (appSettings.GetAreaIdsCanReserveBudgetToList().IndexOf(userAuthorizeProfile.AreaId.Value) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "หน่วยงานที่ท่านสังกัดไม่สามารถ เบิกจ่ายเงินงบประมาณได้";
                return ret;
            }

            // สามารถเบิกจ่าย รายการกันเงินข้ามปีงบประมาณได้ แต่ต้องไม่เกิน 1 ปี
            var currYear = AppUtils.GetCurrYear();
            if ((currYear - exprReserve.YR) > 1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = string.Format("ทำรายการเบิกจ่ายเงิน จากรายการกันเงินเกิน 1 ปีไม่ได้ โปรดตรวจสอบ (ปีที่กันเงิน: {0}, ปีที่เบิกจ่าย: {1})", exprReserve.YR + 543, currYear + 543);
                return ret;
            }

            //ไม่สามารถใช้เลขที่เบิกจ่าย ซ้ำในใบกันเงินเดียวกันได้
            if (db.T_BUDGET_RESERVE_WITHDRAWALs.Any(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(reserveId) && e.WITHDRAWAL_CODE.Equals(withdrawalCode))
               || AppUtils.FindObjFromDbChangeSet<T_BUDGET_RESERVE_WITHDRAWAL>(db).Any(e => e.ACTIVE.Equals(1) && e.RESERVE_ID.Equals(reserveId) && e.WITHDRAWAL_CODE.Equals(withdrawalCode)))
            {
                // ตรวจสอบรายการเบิกจ่าย ที่รออัพเดตไปยังฐานข้อมูล
                // มีเลขที่เบิกจ่ายที่กำหนดตรวจสอบการใช้เลข เบิกจ่ายซ้ำหรือไม่
                if (!AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_RESERVE_WITHDRAWAL>(db).Any(e => e.RESERVE_ID.Equals(reserveId) && e.WITHDRAWAL_CODE.Equals(withdrawalCode) && e.ACTIVE.Equals(-1)))
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = string.Format("เลขที่เบิกจ่าย {0} ถูกเบิกจ่ายที่รายการกันเงินเลขที่ {1} นี้แล้ว ไม่สามารถนำมาใช้ซ้ำได้", withdrawalCode, reserveId);
                    return ret;
                }
            }


            // ตรวจสอบเลขที่เบิกจ่าย ที่กำลังขอเบิก
            // หากมีการใช้ไปแล้ว ประเภทงบประมาณ จะต้องเป็นประเภทเดียวกันกับประเภทงบ (เงินงบ เงินนอกงบ) ที่เบิกจ่ายก่อนหน้า ถึงจะให้เบิกจ่ายได้
            var exprOldWithdrawal = AppUtils.FindObjFromDbChangeSet<T_BUDGET_RESERVE_WITHDRAWAL>(db).Where(e => e.ACTIVE == 1 && e.WITHDRAWAL_CODE.Equals(withdrawalCode)).FirstOrDefault();
            if (null == exprOldWithdrawal)
                exprOldWithdrawal = db.T_BUDGET_RESERVE_WITHDRAWALs.Where(e => e.ACTIVE == 1 && e.WITHDRAWAL_CODE.Equals(withdrawalCode)).FirstOrDefault();
            if (null != exprOldWithdrawal
                // และรายการที่ขอเบิกไปแล้ว ยังไม่อยู่ระหว่างการยกเลิก (ChangeSet ของ db)
                && !AppUtils.FindObjFromDbChangeSetUpdate<T_BUDGET_RESERVE_WITHDRAWAL>(db).Any(e => e.ACTIVE == -1 && e.RESERVE_ID.Equals(exprOldWithdrawal.RESERVE_ID) && e.WITHDRAWAL_CODE.Equals(exprOldWithdrawal.WITHDRAWAL_CODE)))
            {
                var expr = db.T_BUDGET_RESERVEs.Where(e => e.RESERVE_ID.Equals(exprOldWithdrawal.RESERVE_ID)).Select(e => new { e.DEP_ID, e.BUDGET_TYPE }).FirstOrDefault();
                var oldWithdrawalBudgetType = expr.BUDGET_TYPE;
                var oldWithdrawalDepId = expr.DEP_ID;
                if (!oldWithdrawalBudgetType.Equals(exprReserve.BUDGET_TYPE))
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = string.Format("เลขที่ขอเบิก {0} ไม่สามารถเบิกจ่ายข้ามงบประมาณได้", withdrawalCode);
                    return ret;
                }
                //else if (null != oldWithdrawalDepId && !oldWithdrawalDepId.Value.Equals(exprReserve.DEP_ID))
                //{
                //    ret.Completed = false;
                //    ret.CauseErrorMessage = string.Format("เลขที่ขอเบิก {0} ไม่สามารถเบิกจ่ายข้ามหน่วยงานภายในได้", withdrawalCode);
                //    return ret;
                //}
            }


            // ปรับปรุงข้อมูลในใบกัน
            exprReserve.USE_AMOUNT += withdrawalAmounts;
            exprReserve.REMAIN_AMOUNT = exprReserve.RESERVE_BUDGET_AMOUNT - exprReserve.USE_AMOUNT;
            exprReserve.LATEST_WITHDRAWAL_DATETIME = DateTime.Now;
            exprReserve.LATEST_WITHDRAWAL_ID = userAuthorizeProfile.EmpId;
            if (exprReserve.REMAIN_AMOUNT.CompareTo(decimal.Zero) == -1)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "ยอดเงินไม่เพียงพอให้เบิกจ่าย";
                return ret;
            }

            // บันทึกข้อมูลการเบิกจ่าย
            short seqNo = Convert.ToInt16(db.T_BUDGET_RESERVE_WITHDRAWALs.Where(e => e.RESERVE_ID.Equals(reserveId)).Count());
            seqNo++;
            db.T_BUDGET_RESERVE_WITHDRAWALs.InsertOnSubmit(new T_BUDGET_RESERVE_WITHDRAWAL()
            {
                SEQ_NO = seqNo,
                RESERVE_ID = reserveId,
                WITHDRAWAL_CODE = withdrawalCode,
                REFER_DOC_CODE = referDocNo, // เลขที่เอกสารอ้างอิงปรับปรุงบัญชี
                WITHDRAWAL_DATE = withdrawalDate,

                WITHDRAWAL_TYPE = withdrawalType,
                WITHDRAWAL_REFER_RESERVE_ID = referReserveId,
                WITHDRAWAL_REFER_SEQ_NO = referWithdrawalSeqNo,

                WITHDRAWAL_AMOUNT = withdrawalAmounts,
                DEP_ID = exprReserve.DEP_ID,
                SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                MN = Convert.ToInt16(DateTime.Now.Month),
                YR = exprReserve.YR,
                REMARK_TEXT = remarkText,
                CREATED_DATETIME = DateTime.Now,
                USER_ID = userAuthorizeProfile.EmpId,
                ACTIVE = 1
            });

            // บันทึกประวัติการเบิกจ่าย
            db.T_BUDGET_RESERVE_WITHDRAWAL_HISTORies.InsertOnSubmit(new T_BUDGET_RESERVE_WITHDRAWAL_HISTORY()
            {
                RESERVE_ID = reserveId,
                DEP_ID = exprReserve.DEP_ID,
                SUB_DEP_ID = exprReserve.SUB_DEP_ID,
                WITHDRAWAL_SEQ_NO = seqNo,
                SEQ_NO = 1,
                TRAN_TYPE = 1, // เบิกจ่าย
                WITHDRAWAL_CODE = withdrawalCode,
                CURR_WITHDRAWAL_AMOUNT = withdrawalAmounts,
                ADJUSTMENT_AMOUNT = decimal.Zero,
                CASHBACK_AMOUNT = decimal.Zero,
                BALANCE_AMOUNT = withdrawalAmounts,
                CREATED_DATETIME = DateTime.Now,
                REMARK_TEXT = remarkText,
                USER_ID = userAuthorizeProfile.EmpId
            });

            return ret;
        }

        /// <summary>
        /// เดือนที่สามารถรายงานผลการใช้จ่ายงบประมาณได้
        /// 1. เดือนปัจจุบัน
        /// 2. เดือนก่อนหน้า แต่ต้องไม่เกินวันที่ 3 ของเดือนถัดไป (จำนวนวันจะตรวจสอบจาก T_CONFIGURATION_DETAIL ของระบบ)
        /// </summary>
        /// <returns></returns>
        public static List<int> GetCanReportBudgetMonthsNo()
        {
            List<int> canReportBudgetMonths = new List<int>() { DateTime.Now.Month };

            // เดือนตุลาคม ต้องไม่ยอมให้รายงานผลในเดือนก่อนหน้า เพราะถือว่าเริ่มปีงบประมาณใหม่แล้ว
            if (DateTime.Now.Month.Equals(10))
                return canReportBudgetMonths;

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprConst = db.proc_GetUsingAppConstByKey(AppConfigConst.APP_CONST_REPORT_USED_BUDGET_WAIVE_DAYs).FirstOrDefault();
                if (null != exprConst && !string.IsNullOrEmpty(exprConst.ITEM_VALUE) && DateTime.Now.Day.CompareTo(Convert.ToInt32(exprConst.ITEM_VALUE)) != 1)
                    canReportBudgetMonths.Add(DateTime.Now.Month - 1);
            }

            return canReportBudgetMonths;
        }


        /// <summary>
        /// โอนเปลี่ยนแปลงงบประมาณของรายการค่าใช้จ่าย หรือ โครงการ ไปยัง ค่าใช้จ่ายหรือโครงการอื่นๆ
        /// จะย้าย เงินประจำงวด และ งบประมาณที่รัฐจัดสรร ไปพร้อมกัน
        /// <para>กรณีโอนเปลี่ยนแปลง ตามโครงการ ระบบจะปรับปรุงยอดเงินส่วนของรายการค่าใช้จ่ายด้วย</para>
        /// <para>[เคส 1] โอนจาก คชจ. ไปยัง คชจ., [เคส 2] โอนจาก คชจ. ไปยัง โครงการ, [เคส 3] โอนจาก โครงการ ไปยัง โครงการ, [เคส 4] โอนจาก โครงการ ไปยัง คชจ. เป็นต้น</para>
        /// </summary>
        /// <param name="db"></param>
        /// <param name="fiscalYear">ปีงบประมาณ ค.ศ.</param>
        /// <param name="budgetType">1 = เงินงบ, 2 = เงินนอกงบ</param>
        /// <param name="fromPlanId"></param>
        /// <param name="fromProduceId"></param>
        /// <param name="fromActivityId"></param>
        /// <param name="fromBudgetTypeId"></param>
        /// <param name="fromExpensesGroupId"></param>
        /// <param name="fromExpensesId"></param>
        /// <param name="fromProjectId"></param>
        /// <param name="toPlanId"></param>
        /// <param name="toProduceId"></param>
        /// <param name="toActivityId"></param>
        /// <param name="toBudgetTypeId"></param>
        /// <param name="toExpensesGroupId"></param>
        /// <param name="toExpensesId"></param>
        /// <param name="toProjectId"></param>
        /// <param name="tranferAmount">จำนวนที่ขอโอนเปลี่ยนแปลง</param>
        /// <param name="tranferDate">วันที่ขอโอนเปลี่ยนแปลง</param>
        /// <param name="referDocNo">เลขที่อ้างอิงการโอนเปลี่ยนแปลง</param>
        /// <param name="remarkText">รายละเอียดอื่นๆเพิ่มเติม</param>
        /// <param name="userAuthorizeProfile"></param>
        /// <returns></returns>
        public static AdjustmentBudgetResult DoTranferBudgetExpensesToOther(ExcisePlaningDbDataContext db, short fiscalYear, short budgetType, int? fromPlanId, int? fromProduceId, int? fromActivityId, int fromBudgetTypeId, int fromExpensesGroupId, int fromExpensesId, int? fromProjectId
            , int? toPlanId, int? toProduceId, int? toActivityId, int toBudgetTypeId, int toExpensesGroupId, int toExpensesId, int? toProjectId
            , decimal tranferAmount, DateTime? tranferDate, string referDocNo, string remarkText, UserAuthorizeProperty userAuthorizeProfile)
        {
            var ret = new AdjustmentBudgetResult()
            {
                CauseErrorMessage = string.Empty,
                Completed = true,
                RunningCode = string.Empty
            };


            // ตรวจสอบความพร้อมของงบประมาณ ก่อนการโอนเปลี่ยนแปลง
            var verifyResult = VerifyBudget(fiscalYear, budgetType);
            if (!verifyResult.IsComplete)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = verifyResult.FormatCauseMessageToUser();
                return ret;
            }

            // ประวัติการโอนเปลี่ยนแปลง
            // สำหรับนับ จำนวนครั้งการเปลี่ยนแปลง
            var exprAdjust = db.T_BUDGET_EXPENSES_ADJUSTMENT_HISTORies.Where(e => e.ACTIVE.Equals(1)
                && e.YR.Equals(fiscalYear)
                && e.FROM_PLAN_ID.Equals(fromPlanId)
                && e.FROM_PRODUCE_ID.Equals(fromProduceId)
                && e.FROM_ACTIVITY_ID.Equals(fromActivityId)
                && e.FROM_BUDGET_TYPE_ID.Equals(fromBudgetTypeId)
                && e.FROM_EXPENSES_GROUP_ID.Equals(fromExpensesGroupId)
                && e.FROM_EXPENSES_ID.Equals(fromExpensesId));


            // โอนรายการเดียวกันไม่ได้
            string fromStr = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}"
                , fromPlanId, fromProduceId, fromActivityId, fromBudgetTypeId
                , fromExpensesGroupId, fromExpensesId, fromProjectId);
            string toStr = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}"
                , toPlanId, toProduceId, toActivityId, toBudgetTypeId
                , toExpensesGroupId, toExpensesId, toProjectId);
            if (fromStr.Equals(toStr))
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "ไม่สามารถโอนไปยังรายการเดียวกันได้";
                return ret;
            }

            // เงินนอกงบประมาณ เงินประจำงวด จะไม่มีการกระจาย
            // เงินลงไปยังค่าใช้จ่าย ซึ่งมี Flag On/Off
            bool spreadBudgetToExpenses = true;
            if (budgetType.Equals(2))
                spreadBudgetToExpenses = db.T_BUDGET_MASTERs.Where(e => e.YR.Equals(fiscalYear)).FirstOrDefault().OFF_BUDGET_SPREAD_TO_EXPENSES;
            decimal tranferBudgetAmount = budgetType == 1 ? tranferAmount : decimal.Zero;
            decimal tranferOffBudgetAmount = budgetType == 2 ? tranferAmount : decimal.Zero;


            // โอนเปลี่ยนแปลงตามรายการค่าใช้จ่าย

            // ค่าใช้จ่ายต้นทาง
            var exprExpenseSource = db.T_BUDGET_EXPENSEs.Where(e => e.YR.Equals(fiscalYear)
                && e.ACTIVE.Equals(1)
                && e.PLAN_ID.Equals(fromPlanId)
                && e.PRODUCE_ID.Equals(fromProduceId)
                && e.ACTIVITY_ID.Equals(fromActivityId)
                && e.BUDGET_TYPE_ID.Equals(fromBudgetTypeId)
                && e.EXPENSES_GROUP_ID.Equals(fromExpensesGroupId)
                && e.EXPENSES_ID.Equals(fromExpensesId)).FirstOrDefault();
            if (null == exprExpenseSource)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "ไม่พบรายการค่าใช้จ่ายต้นทางที่ต้องการโอนเปลี่ยนแปลง";
                return ret;
            }
            exprExpenseSource.BUDGET_AMOUNT -= tranferBudgetAmount;
            exprExpenseSource.OFF_BUDGET_AMOUNT -= tranferOffBudgetAmount;
            if (spreadBudgetToExpenses)
            {
                exprExpenseSource.ACTUAL_BUDGET_AMOUNT -= tranferBudgetAmount;
                exprExpenseSource.REMAIN_BUDGET_AMOUNT = exprExpenseSource.ACTUAL_BUDGET_AMOUNT - exprExpenseSource.USE_BUDGET_AMOUNT;

                exprExpenseSource.ACTUAL_OFF_BUDGET_AMOUNT -= tranferOffBudgetAmount;
                exprExpenseSource.REMAIN_OFF_BUDGET_AMOUNT = exprExpenseSource.ACTUAL_OFF_BUDGET_AMOUNT - exprExpenseSource.USE_OFF_BUDGET_AMOUNT;
            }
            if (budgetType == 1 && (exprExpenseSource.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1 || exprExpenseSource.BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1))
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "เงินงบประมาณไม่เพียงพอสำหรับการโอนเปลี่ยนแปลง";
                return ret;
            }
            else if (budgetType == 2 && ((spreadBudgetToExpenses && exprExpenseSource.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1) || exprExpenseSource.OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1))
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "เงินนอกงบประมาณไม่เพียงพอสำหรับปรับปรุงแผนรายรับ/จ่าย";
                return ret;
            }


            // ค่าใช้จ่ายปลายทาง
            var exprExpenseTarget = db.T_BUDGET_EXPENSEs.Where(e => e.YR.Equals(fiscalYear)
                && e.ACTIVE.Equals(1)
                && e.PLAN_ID.Equals(toPlanId)
                && e.PRODUCE_ID.Equals(toProduceId)
                && e.ACTIVITY_ID.Equals(toActivityId)
                && e.BUDGET_TYPE_ID.Equals(toBudgetTypeId)
                && e.EXPENSES_GROUP_ID.Equals(toExpensesGroupId)
                && e.EXPENSES_ID.Equals(toExpensesId)).FirstOrDefault();
            if (null == exprExpenseTarget)
            {
                ret.Completed = false;
                ret.CauseErrorMessage = "ไม่พบรายการค่าใช้จ่ายปลายทางที่ต้องการโอนเปลี่ยนแปลง";
                return ret;
            }
            exprExpenseTarget.BUDGET_AMOUNT += tranferBudgetAmount;
            exprExpenseTarget.OFF_BUDGET_AMOUNT += tranferOffBudgetAmount;
            if (spreadBudgetToExpenses)
            {
                exprExpenseTarget.ACTUAL_BUDGET_AMOUNT += tranferBudgetAmount;
                exprExpenseTarget.REMAIN_BUDGET_AMOUNT = exprExpenseTarget.ACTUAL_BUDGET_AMOUNT - exprExpenseTarget.USE_BUDGET_AMOUNT;

                exprExpenseTarget.ACTUAL_OFF_BUDGET_AMOUNT += tranferOffBudgetAmount;
                exprExpenseTarget.REMAIN_OFF_BUDGET_AMOUNT = exprExpenseTarget.ACTUAL_OFF_BUDGET_AMOUNT - exprExpenseTarget.USE_OFF_BUDGET_AMOUNT;
            }



            // โอนเปลี่ยนแปลงตามโครงการ
            if (fromProjectId != null)
            {
                // ค่าใช้จ่ายต้นทาง
                var exprProjectSource = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.YR.Equals(fiscalYear)
                    && e.ACTIVE.Equals(1)
                    && e.PLAN_ID.Equals(fromPlanId)
                    && e.PRODUCE_ID.Equals(fromProduceId)
                    && e.ACTIVITY_ID.Equals(fromActivityId)
                    && e.BUDGET_TYPE_ID.Equals(fromBudgetTypeId)
                    && e.EXPENSES_GROUP_ID.Equals(fromExpensesGroupId)
                    && e.EXPENSES_ID.Equals(fromExpensesId)
                    && e.PRODUCE_ID.Equals(fromProjectId)).FirstOrDefault();
                if (null == exprProjectSource)
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = "ไม่พบโครงการต้นทางที่ต้องการโอนเปลี่ยนแปลง";
                    return ret;
                }
                exprProjectSource.BUDGET_AMOUNT -= tranferBudgetAmount;
                exprProjectSource.OFF_BUDGET_AMOUNT -= tranferOffBudgetAmount;
                if (spreadBudgetToExpenses)
                {
                    exprProjectSource.ACTUAL_BUDGET_AMOUNT -= tranferBudgetAmount;
                    exprProjectSource.REMAIN_BUDGET_AMOUNT = exprProjectSource.ACTUAL_BUDGET_AMOUNT - exprProjectSource.USE_BUDGET_AMOUNT;

                    exprProjectSource.ACTUAL_OFF_BUDGET_AMOUNT -= tranferOffBudgetAmount;
                    exprProjectSource.REMAIN_OFF_BUDGET_AMOUNT = exprProjectSource.ACTUAL_OFF_BUDGET_AMOUNT - exprProjectSource.USE_OFF_BUDGET_AMOUNT;
                }
                if (budgetType == 1 && (exprProjectSource.REMAIN_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1 || exprProjectSource.BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1))
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = "เงินงบประมาณไม่เพียงพอสำหรับการโอนเปลี่ยนแปลง";
                    return ret;
                }
                else if (budgetType == 2 && ((spreadBudgetToExpenses && exprProjectSource.REMAIN_OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1) || exprProjectSource.OFF_BUDGET_AMOUNT.CompareTo(decimal.Zero) == -1))
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = "เงินนอกงบประมาณไม่เพียงพอสำหรับปรับปรุงแผนรายรับ/จ่าย";
                    return ret;
                }
            }


            if(null != toProjectId)
            {
                exprAdjust = exprAdjust.Where(e => e.FROM_PROJECT_ID.Equals(toProjectId));

                // ค่าใช้จ่ายปลายทาง
                var exprProjectTarget = db.T_BUDGET_EXPENSES_PROJECTs.Where(e => e.YR.Equals(fiscalYear)
                    && e.ACTIVE.Equals(1)
                    && e.PLAN_ID.Equals(toPlanId)
                    && e.PRODUCE_ID.Equals(toProduceId)
                    && e.ACTIVITY_ID.Equals(toActivityId)
                    && e.BUDGET_TYPE_ID.Equals(toBudgetTypeId)
                    && e.EXPENSES_GROUP_ID.Equals(toExpensesGroupId)
                    && e.EXPENSES_ID.Equals(toExpensesId)
                    && e.PROJECT_ID.Equals(toProjectId)).FirstOrDefault();
                if (null == exprProjectTarget)
                {
                    ret.Completed = false;
                    ret.CauseErrorMessage = "ไม่พบโครงการปลายทางที่ต้องการโอนเปลี่ยนแปลง";
                    return ret;
                }
                exprProjectTarget.BUDGET_AMOUNT += tranferBudgetAmount;
                exprProjectTarget.OFF_BUDGET_AMOUNT += tranferOffBudgetAmount;
                if (spreadBudgetToExpenses)
                {
                    exprProjectTarget.ACTUAL_BUDGET_AMOUNT += tranferBudgetAmount;
                    exprProjectTarget.REMAIN_BUDGET_AMOUNT = exprProjectTarget.ACTUAL_BUDGET_AMOUNT - exprProjectTarget.USE_BUDGET_AMOUNT;

                    exprProjectTarget.ACTUAL_OFF_BUDGET_AMOUNT += tranferOffBudgetAmount;
                    exprProjectTarget.REMAIN_OFF_BUDGET_AMOUNT = exprProjectTarget.ACTUAL_OFF_BUDGET_AMOUNT - exprProjectTarget.USE_OFF_BUDGET_AMOUNT;
                }
            }



            // เก็บประวัติการโอนเปลี่ยนแปลง เงินงบประมาณ และ เงินประจำงวด ไปยังรายการอื่นๆ
            db.T_BUDGET_EXPENSES_ADJUSTMENT_HISTORies.InsertOnSubmit(new T_BUDGET_EXPENSES_ADJUSTMENT_HISTORY()
            {
                YR = fiscalYear,
                SEQ_NO = Convert.ToInt16(exprAdjust.Count() + 1), // นับจำนวนครั้งที่มีการโอนเปลี่ยนแปลง ไปยังรายการปลายทาง
                BUDGET_TYPE = budgetType,

                FROM_PLAN_ID = fromPlanId,
                FROM_PRODUCE_ID = fromProduceId,
                FROM_ACTIVITY_ID = fromActivityId,
                FROM_BUDGET_TYPE_ID = fromBudgetTypeId,
                FROM_EXPENSES_GROUP_ID = fromExpensesGroupId,
                FROM_EXPENSES_ID = fromExpensesId,
                FROM_PROJECT_ID = fromProjectId,

                TO_PLAN_ID = toPlanId,
                TO_PRODUCE_ID = toProduceId,
                TO_ACTIVITY_ID = toActivityId,
                TO_BUDGET_TYPE_ID = toBudgetTypeId,
                TO_EXPENSES_GROUP_ID = toExpensesGroupId,
                TO_EXPENSES_ID = toExpensesId,
                TO_PROJECT_ID = toProjectId,

                TRANFER_AMOUNT = tranferAmount,
                TRANFER_DATE = tranferDate,
                REFER_CODE = referDocNo,

                ACTIVE = 1,
                CREATED_DATETIME = DateTime.Now,
                REMARK_TEXT = remarkText,
                USER_ID = userAuthorizeProfile.EmpId
            });

            return ret;
        }
    }
}