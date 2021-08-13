using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes
{
    public class AppConfigConst
    {

        /// <summary>
        /// ร้อยละของเงินเดือน ที่หักเข้ากองทุนประกันสังคม แต่ไม่เกิน 750 บาท/เดือน
        /// </summary>
        public static string APP_CONST_SALARY_RATE_FOR_SOCIAL_SECURITY = "SALARY_RATE_FOR_SOCIAL_SECURITY";

        /// <summary>
        /// ร้อยละของอัตราเงินเดือนที่ใช้สำหรับเงินสมทบกองทุนเงินทดแทน
        /// </summary>
        public static string APP_CONST_SALARY_RATE_FOR_COMPENSATION_FUND = "SALARY_RATE_FOR_COMPENSATION_FUND";

        /// <summary>
        /// ค่าตอบแทนการปฏิบัติงานนอกเวลาราชการ วันทำการ
        /// </summary>
        public static string APP_CONST_WORKING_RATE_COMPENSATION = "WORKING_RATE_COMPENSATION";

        /// <summary>
        /// ค่าตอบแทนการปฏิบัติงานนอกเวลาราชการ วันหยุด
        /// </summary>
        public static string APP_CONST_HOLIDAY_RATE_COMPENSATION = "HOLIDAY_RATE_COMPENSATION";

        /// <summary>
        /// หน่วยงานภูมิภาคสามารถบันทึกผลการใช้จ่ายงบประมาณล่าช้าได้กี่วัน หากเกินระบบจะไม่ยอมให้บันทึกผลการใช้จ่ายในเดือนก่อนหน้าได้
        /// </summary>
        public static string APP_CONST_REPORT_USED_BUDGET_WAIVE_DAYs = "REPORT_USED_BUDGET_WAIVE_DAYs";




        //2 ค่าตอบแทน ใช้สอยและวัสดุ
        public static int Compensation_Master_ID = 2;
        public static int Compensation_EXPENSES_GROUP_ID = 6;

        //3 ค่าครุภัณฑ์
        public static int EquipmentOffice_Master_ID = 3;
        public static int EquipmentOffice_EXPENSES_GROUP_ID = 22;

        //4 ค่าที่ดินและสิ่งก่อสร้าง
        public static int LandAndBuilding_Master_ID = 3;
        public static int LandAndBuilding_EXPENSES_GROUP_ID = 23;
        //5 หมวดรายจ่ายอื่น
        public static int OtherExpense_Master_ID = 2;
        public static int OtherExpense_EXPENSES_GROUP_ID = 5;

        //6 โอนให้กระทรวงการคลังและหน่วยงานในสังกัดกระทรวงการคลัง
        public static int PermanentSecretary_Master_ID = 0;
        public static int PermanentSecretary_EXPENSES_GROUP_ID = 0;




        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// หน้าแรก
        /// </summary>
        public static string MENU_CONST_DASHBOARD = "DASHBOARD_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดค่าพื้นฐาน
        /// </summary>
        public static string MENU_CONST_UNDERLYING_CONFIGURE = "CONFIGURE_UNDERLYING_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดวันหยุดประจำปี
        /// </summary>
        public static string MENU_CONST_HOLIDAY_DATE_CONFIGURE = "CONFIGURE_HOLIDAY_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดตำแหน่งงานในระบบ
        /// </summary>
        public static string MENU_CONST_POSITION_CONFIGURE = "CONFIGURE_POSITION_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ข้อมูลพนักงาน
        /// </summary>
        public static string MENU_CONST_PERSONNEL_INFORMATION = "UNDERLYING_PERSONNEL_INFORMATION_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดสูตรที่ใช้ในการคำนวณมูลค่า ของค่าใช้จ่าย
        /// การประมาณการรายได้ภาษี ได้แก่ ภาษีเพื่อมหาดไทย เงินค่าใช้จ่ายท้องถิ่นในประเทศ เงินค่าใช้จ่ายท้องถิ่นนำเข้า, ... เป็นต้น
        /// </summary>
        public static string MENU_CONST_TAX_FORCAST_EXPENSES_TYPE_FORMULA = "TAX_FORCAST_EXPENSES_TYPE_FORMULA_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// เวลาเข้าออกงานของพนักงาน
        /// </summary>
        public static string MENU_CONST_PERSONNEL_TYPE = "UNDERLYING_PERSONNEL_TYPE_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ระดับ C ของบุคลากร
        /// </summary>
        public static string MENU_CONST_PERSONNEL_LEVEL = "UNDERLYING_PERSONNEL_LEVEL_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ข้อมูลหน่วยงาน ได้แก่ กำหนดสิทธิ์หน่วยงานที่รับผิดชอบ ชื่อหน่วยงาน เป็นต้น
        /// </summary>
        public static string MENU_CONST_DEPARTMENT = "UNDERLYING_DEPARTMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายชื่อองค์กรอื่นๆ ที่เกี่ยวข้องกับกรมสรรพสามิต
        /// </summary>
        public static string MENU_CONST_ORGANIZATION = "UNDERLYING_ORGANIZATION_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดอัตรา ค่าอบรมและสัมนา
        /// </summary>
        public static string MENU_CONST_TRANING_AND_SEMINORS = "UNDERLYING_TRANING_AND_SEMINORS_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// หน่วยนับ
        /// </summary>
        public static string MENU_CONST_UNIT = "UNDERLYING_UNIT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดสิทธิ์การใช้งานระบบ เช่น กำหนดเมนู กำหนดกลุ่มผู้ใช้งาน และ กำหนดผู้ใช้งานไว้ในกลุ่มนั้นๆ เป็นต้น
        /// </summary>
        public static string MENU_CONST_ROLE_CONFIGURE = "MENU_CONST_ROLE_CONFIGURE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// สร้างคำขอเงินงบประมาณ
        /// </summary>
        public static string MENU_CONST_BUDGET_REQUEST_MENU = "BUDGET_REQUEST_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ติดตามคำขอต้นปี
        /// </summary>
        public static string MENU_CONST_BUDGET_REQUEST_TRAKCING_START_YEAR_MENU = "BUDGET_REQUEST_TRAKCING_START_YEAR_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการเงินงบประมาณ และเงินนอกงบประมาณ เมนูรับเงินงบประมาณที่รัฐบาลจัดสรรมาให้กับกรมสรรพสามิตในแต่ละปี
        /// </summary>
        public static string MENU_CONST_ALL_BUDGET_RECEIVE_GORNVERMENT_MENU = "ALL_BUDGET_RECEIVE_GORNVERMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการเงินงบประมาณ เมนูรับเงินงบประมาณที่รัฐบาลจัดสรรมาให้กับกรมสรรพสามิตในแต่ละปี
        /// </summary>
        public static string MENU_CONST_BUDGET_RECEIVE_GORNVERMENT_MENU = "BUDGET_RECEIVE_GORNVERMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการเงินนอกงบประมาณ เมนูรับเงินงบประมาณที่รัฐบาลจัดสรรมาให้กับกรมสรรพสามิตในแต่ละปี
        /// </summary>
        public static string MENU_CONST_OFF_BUDGET_RECEIVE_GORNVERMENT_MENU = "OFF_BUDGET_RECEIVE_GORNVERMENT_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รับเงินงบประมาณ ที่รับจัดสรรมาเป็นก้อนๆ ในแต่ละปีงบประมาณ
        /// ซึ่งรัฐบาลจะแบ่งจ่ายเงินงบประมาณ มาให้กับกรมสรรพสามิตเป็น งวดๆ จนครบจำนวน
        /// </summary>
        public static string MENU_CONST_ALL_BUDGET_INCOME_GORNVERMENT_MENU = "ALL_BUDGET_INCOME_GORNVERMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รับเงินงบประมาณ ที่รับจัดสรรมาเป็นก้อนๆ ในแต่ละปีงบประมาณ
        /// ซึ่งรัฐบาลจะแบ่งจ่ายเงินงบประมาณ มาให้กับกรมสรรพสามิตเป็น งวดๆ จนครบจำนวน
        /// </summary>
        public static string MENU_CONST_BUDGET_INCOME_GORNVERMENT_MENU = "BUDGET_INCOME_GORNVERMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// สรุปภาพรวม กิจกรรมที่เกิดขึ้นกับเงินงบประมาณของกรมสรรพสามิตในแต่ละปีงบประมาณ (เงินงบประมาณ และ เงินนอกงบประมาณ)
        /// เช่น ได้รับจัดสรรจากรัฐบาล เงินประจำงวด จัดสรร กันเงิน คงเหลือ และ สามารถค้นหาแยกย่อยลงรายละเอียดส่วน แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย ค่าใช้จ่าย  เป็นต้น
        /// </summary>
        public static string MENU_CONST_SUMMARY_OVERALL_BOTH_BUDGET_MENU = "BUDGET_SUMMARY_OVERALL_BOTH_BUDGET_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// สรุปภาพรวม กิจกรรมที่เกิดขึ้นกับเงินงบประมาณของกรมสรรพสามิตในแต่ละปีงบประมาณ เฉพาะเงินงบประมาณ
        /// เช่น ได้รับจัดสรรจากรัฐบาล เงินประจำงวด จัดสรร กันเงิน คงเหลือ และ สามารถค้นหาแยกย่อยลงรายละเอียดส่วน แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย ค่าใช้จ่าย  เป็นต้น
        /// </summary>
        public static string MENU_CONST_SUMMARY_OVERALL_BUDGET_MENU = "BUDGET_SUMMARY_OVERALL_BUDGET_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// สรุปภาพรวม กิจกรรมที่เกิดขึ้นกับเงินงบประมาณของกรมสรรพสามิตในแต่ละปีงบประมาณ เฉพาะเงินนอกงบประมาณ
        /// เช่น ได้รับจัดสรรจากรัฐบาล เงินประจำงวด จัดสรร กันเงิน คงเหลือ และ สามารถค้นหาแยกย่อยลงรายละเอียดส่วน แผนงาน ผลผลิต กิจกรรม งบรายจ่าย หมวดค่าใช้จ่าย ค่าใช้จ่าย  เป็นต้น
        /// </summary>
        public static string MENU_CONST_SUMMARY_OVERALL_OFF_BUDGET_MENU = "BUDGET_SUMMARY_OVERALL_OFF_BUDGET_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// สรุปภาพรวมเงินงบประมาณที่หน่วยงานได้รับจัดสรรจากส่วนกลาง ในแต่ละปีงบประมาณ
        /// </summary>
        public static string MENU_CONST_DEPARTMENT_BUDGET_OVERVIEW_MENU = "DEPARTMENT_BUDGET_OVERVIEW_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// สรุปภาพรวมการกันเงิน และ การเบิกจ่าย เงินงบประมาณของหน่วยงานภายในกรมสรรพสามิต
        /// </summary>
        public static string MENU_CONST_DEPARTMENT_BUDGET_RESERVE_OVERVIEW_MENU = "DEPARTMENT_BUDGET_RESERVE_OVERVIEW_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รับเงินงบประมาณ ที่รับจัดสรรมาเป็นก้อนๆ ในแต่ละปีงบประมาณ
        /// ซึ่งรัฐบาลจะแบ่งจ่ายเงินงบประมาณ มาให้กับกรมสรรพสามิตเป็น งวดๆ จนครบจำนวน
        /// </summary>
        public static string MENU_CONST_OFF_BUDGET_INCOME_GORNVERMENT_MENU = "OFF_BUDGET_INCOME_GORNVERMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดเก็บรายได้ในแต่ละงวดของ เงินนอกงบ
        /// </summary>
        public static string MENU_CONST_OFF_BUDGET_INCOME = "OFF_BUDGET_INCOME_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดสรรเงินงบประมาณ ลงไปให้กับหน่วยงาน
        /// </summary>
        public static string MENU_CONST_BUDGET_ALLOCATE_MENU = "BUDGET_ALLOCATE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดสรรเงินงบประมาณ ลงไปให้กับหน่วยงาน โดยจัดกลุ่มตามหน่วยงาน
        /// </summary>
        public static string MENU_CONST_BUDGET_ALLOCATE_GROUP_BY_DEAPRTMENT_MENU = "BUDGET_ALLOCATE_GROUP_BY_DEPARTMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กรมสรรพสามิตจัดสรรเงินงบประมาณเพิ่มเติม/พิเศษ ที่นอกเหนือจากคำขอ ให้กับหน่วยงานภูมิภาค
        /// </summary>
        public static string MENU_CONST_BUDGET_ALLOCATE_DEPARTMENT_EXTRA_MENU = "BUDGET_ALLOCATE_DEPARTMENT_EXTRA_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// หน่วยงานภูมิภาคบันทึกผลการใช้จ่ายเงินงบประมาณ
        /// ที่ได้รับจัดสรรจากส่วนกลาง (กรมสรรพสามิต)
        /// ซึ่งเป็นการนำออกข้อมูลจากระบบ แล้วคีย์ลงไฟล์
        /// และนำมา Import ให้ระบบอ่านข้อมูลลงไปยัง Db
        /// </summary>
        public static string MENU_CONST_BUDGET_REPORT_IMPORT_MENU = "BUDGET_REPORT_IMPORT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานผลการใช้จ่าย
        /// </summary>
        public static string MENU_CONST_BUDGET_REPORT_MENU = "BUDGET_ALLOWCATE_REPORT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// สร้างคำขอเงินนอกงบประมาณ
        /// </summary>
        public static string MENU_CONST_OFF_BUDGET_REQUEST_MENU = "OFF_BUDGET_REQUEST_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ประวัติการส่งคำขอเงินงบประมาณ
        /// </summary>
        public static string MENU_CONST_BUDGET_REQUEST_HISTORY_MENU = "BUDGET_REQUEST_HISTORY_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ Template คำของบประมาณ
        /// </summary>
        public static string MENU_CONST_BUDGET_REQUEST_TEMPLATE_MENU = "MENU_CONST_BUDGET_REQUEST_TEMPLATE_CONFIGURE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ แหล่งที่มาของเงินนอก งบประมาณ
        /// </summary>
        /// 
        public static string MENU_CONST_OFF_BUDGET_SOURCE_MENU = "OFF_BUDGET_SOURCE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ ประเภทงบประมาณ
        /// </summary>
        public static string MENU_CONST_BUDGET_TYPE_MENU = "BUDGET_TYPE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ รหัสอ้างอิงแหล่งที่มาเงินงบประมาณ
        /// </summary>
        public static string MENU_CONST_BUDGET_TYPE_GOVERNMENT_REFER_CODE_MENU = "BUDGET_TYPE_GOVERNMENT_REFER_CODE_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ หมวดค่าตอบแทน ใช้สอยและวัสดุ
        /// </summary>
        public static string MENU_CONST_EXPENSES_MASTER = "EXPENSES_MASTER_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ หมวดรายการค่าใช้จ่าย
        /// </summary>
        public static string MENU_CONST_EXPENSES_GROUP = "EXPENSES_GROUP_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ หมวดรายการค่าใช้จ่าย
        /// </summary>
        /// 
        public static string MENU_CONST_EXPENSES_GROUP_GOVERNMENT_REFER_CODE = "EXPENSES_GROUP_GOVERNMENT_REFER_CODE";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ รายการค่าใช้จ่ายต่างๆที่อยู่ภายใต้หมวดค่าใช้จ่าย
        /// </summary>
        public static string MENU_CONST_EXPENSES_ITEM = "EXPENSES_ITEM";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ แต่ล่ะรายการค่าใช้จ่ายประกอบไปด้วยเลขที่ทางบัญชีอะไรบ้าง (GL CODE)    
        /// </summary>
        public static string MENU_CONST_EXPENSES_GLCODE = "EXPENSES_GLCODE";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ รายชื่อแผนงาน ที่ใช้ประกอบในการ จัดทำคำขอ   
        /// </summary>
        public static string MENU_CONST_PLAN_CONFIGURE = "PLAN_CONFIGURE";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ ผลผลิต ของระบบ   
        /// </summary>
        public static string MENU_CONST_PRODUCE_CONFIGURE = "PRODUCE_CONFIGURE";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ ข้อมูลกิจกรรมต่างๆ ที่จะนำไปใช้ในการดำเนินงาน ตามที่ได้รับจัดสรรงบประมาณ  
        /// </summary>
        public static string MENU_CONST_ACTIVITY_CONFIGURE = "ACTIVITY_CONFIGURE";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// จัดการ เงินงบประมาณในแต่ละปี งปม. ที่ได้รับจัดสรรจากรัฐบาลในภาพรวมของกรมสรรพสามิต
        /// </summary>
        public static string MENU_CONST_BUDGET_MASTER = "BUDGET_MASTER";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดอัตราค่าเช่าประเภทต่างๆ ที่อ้างอิงช่วงของเงินเดือนเป็นหลัก
        /// </summary>
        public static string MENU_CONST_RENT_HOUSE_RATE_CONFIGURE = "RENT_HOUSE_RATE_CONFIGURE";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดอัตราค่าตอบแทน ของแต่ละระดับของบุคลากร
        /// </summary>
        public static string MENU_CONST_VEHICLE_TYPE = "VEHICLE_TYPE";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กำหนดอัตราค่าตอบแทน ของแต่ละระดับของบุคลากร
        /// </summary>
        public static string MENU_CONST_PERSONNEL_LEVEL_COMPENSATION_RATE = "PERSONNEL_LEVEL_COMPENSATION_RATE";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ข้อมูลสินทรัพย์
        /// </summary>
        public static string MENU_CONST_ASSET_MENU = "ASSET";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ประเภทรายได้ภาษี
        /// </summary>
        public static string MENU_CONST_TAX_FORCAST_INCOME_MONTHLY = "TAX_FORCAST_INCOME_MONTHLY_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// แผนยุทธศาสตร์กรมสรรพสามิต
        /// </summary>
        public static string MENU_CONST_STRATEGIC_PLAN = "STRATEGIC_PLAN";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กันเงินงบประมาณ
        /// </summary>
        public static string MENU_CONST_BUDGET_RESERVE_MENU = "BUDGET_RESERVE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ปรับปรุงข้อมูลใบกัน ที่ยังไม่มีการเบิกจ่าย 
        /// </summary>
        public static string MENU_CONST_BUDGET_RESERVE_ADJUSTMENT_MENU = "BUDGET_RESERVE_ADJUSTMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// เบิกเกินส่งคืน
        /// </summary>
        public static string MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_CASHBACK_MENU = "BUDGET_RESERVE_WITHDRAWAL_CASHBACK_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// ปรับปรุงข้อมูลการเบิกจ่าย เช่น ปรับเปลี่ยนประเภทงบการเบิกจ่าย ปรับเปลี่ยนกลุ่มค่าใช้จ่าย (แผนงาน ผลผลิต กิจกรรม ... โครงการ) เป็นต้น
        /// </summary>
        public static string MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_ADJUSTMENT_MENU = "BUDGET_RESERVE_WITHDRAWAL_ADJUSTMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// เบิกจ่ายเงินกันเงิน
        /// </summary>
        public static string MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_MENU = "BUDGET_RESERVE_WITHDRAWAL_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// เบิกจ่ายรายการกันเงิน ซึ่งเลขที่ขอเบิก 1 ใบสามารถเบิกจ่ายได้มากกว่า 1 ใบกัน
        /// </summary>
        public static string MENU_CONST_BUDGET_RESERVE_WITHDRAWAL_MULTI_MENU = "BUDGET_RESERVE_WITHDRAWAL_MULTI_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// โอนเปลี่ยนแปลงงบประมาณของรายการค่าใช้จ่าย หรือ โครงการ ไปยัง ค่าใช้จ่ายหรือโครงการอื่นๆ
        /// จะย้าย เงินประจำงวด และ งบประมาณที่รัฐจัดสรร ไปพร้อมกัน
        /// *** เงินงบประมาณ ***
        /// </summary>
        public static string MENU_CONST_BUDGET_EXPENSES_ADJUSTMENT_MENU = "BUDGET_EXPENSES_ADJUSTMENT_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// โอนเปลี่ยนแปลงงบประมาณของรายการค่าใช้จ่าย หรือ โครงการ ไปยัง ค่าใช้จ่ายหรือโครงการอื่นๆ
        /// จะย้าย เงินประจำงวด และ งบประมาณที่รัฐจัดสรร ไปพร้อมกัน
        /// *** เงินนอกงบประมาณ ***
        /// </summary>
        public static string MENU_CONST_OFF_BUDGET_EXPENSES_ADJUSTMENT_MENU = "OFF_BUDGET_EXPENSES_ADJUSTMENT_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// การฟบริหารจัดการงบประมาณ
        /// </summary>
        public static string MENU_CONST_GRAPH_ANNUAL_BUDGET = "GRAPH_ANNUAL_BUDGET";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// กราฟแสดงรายงานผลการใช้จ่าย
        /// </summary>
        public static string MENU_CONST_GRAPH_ANNUAL_BUDGET_RESULT = "GRAPH_ANNUAL_BUDGET_RESULT";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// แผนการรับ - จ่ายเงินฝากค่าใช้จ่ายเก็บภาษีท้องถิ่น ประจำปีงบประมาณ
        /// </summary>
        public static string MENU_CONST_PlansForReceivingAndPlayingExpenses = "PlansForReceivingAndPlayingExpenses_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานผลการใช้จ่ายงบประมาณภาพรวมกรมสรรพสามิต
        /// </summary>
        public static string MENU_RptSummaryBudgetUsed = "RptSummaryBudgetUsed_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานผลการใช้จ่ายงบประมาณภาพรวมกรมสรรพสามิต
        /// </summary>
        public static string MENU_RptRequestBudgetOfYear = "RptRequestBudgetOfYear_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// R003-รายงานงบประมาณรายจ่ายประจำปีงบประมาณ
        /// </summary>
        public static string MENU_CONST_REPORT_SUMMARY_ALLOCATE_BUDGET_MENU = "REPORT_SUMMARY_ALLOCATE_BUDGET_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// R004-รายงานแผนรายรับรายจ่ายเงินนอกงบประมาณประจำปี
        /// </summary>
        public static string MENU_CONST_REPORT_SUMMARY_ALLOCATE_OFF_BUDGET_MENU = "REPORT_SUMMARY_ALLOCATE_OFF_BUDGET_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// R012-รายงานผลการใช้จ่ายเงินงบประมาณประจำปี
        /// </summary>
        public static string MENU_CONST_REPORT_SUMMARY_REPORT_BUDGET_MENU = "REPORT_SUMMARY_REPORT_BUDGET_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// R011-รายงานผลการใช้จ่ายเงินนอกงบประมาณประจำปี
        /// </summary>
        public static string MENU_CONST_REPORT_SUMMARY_REPORT_OFF_BUDGET_MENU = "REPORT_SUMMARY_REPORT_OFF_BUDGET_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานสรุปเงินงบประมาณตามประเภทรายจ่ายของหน่วยงาน เช่น งบประมาณที่ได้รับจัดสรร (เงินงบ เงินนอก) ผลการใช้จ่าย คงเหลือ เป็นต้น
        /// </summary>
        public static string MENU_CONST_REPORT_DEPARTMENT_BUDGET_GROUP_BY_EXPENSES = "REPORT_DEPARTMENT_BUDGET_GROUP_BY_EXPENSES_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานทะเบียนคุมเงินงบประมาณของหน่วยงานภูมิภาค
        /// </summary>
        public static string MENU_CONST_REPORT_DEPARTMENT_BUDGET_CASH_FLOW = "REPORT_DEPARTMENT_BUDGET_CASH_FLOW_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานสรุปเงินงบประมาณตามงบรายจ่ายของหน่วยงาน เช่น งบประมาณที่ได้รับจัดสรร (เงินงบ เงินนอก) ผลการใช้จ่าย คงเหลือ เป็นต้น
        /// </summary>
        public static string MENU_CONST_REPORT_DEPARTMENT_BUDGET_GROUP_BY_BUDGET_TYPE = "REPORT_DEPARTMENT_BUDGET_GROUP_BY_BUDGET_TYPE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานสรุปการรับเงินประจำงวด แยกตามงบรายจ่าย (เงินงบประมาณ)
        /// </summary>
        public static string MENU_CONST_REPORT_BUDGET_INCOME_GROUP_BY_BUDGET_TYPE = "REPORT_BUDGET_INCOME_GROUPBY_BUDGET_TYPE_MENU";

        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานสรุปกระแสเงินงบประมาณของกรมสรรพสามิต ได้แก่ รับเงินประจำงวด จัดสรรงบประมาณให้หน่วยงานภูมิภาค กันเงินงบประมาณและเบิกจ่าย
        /// </summary>
        public static string MENU_CONST_REPORT_BUDGET_CASH_FLOW = "REPORT_BUDGET_CASH_FLOW_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานการกันเงินงบประมาณ
        /// </summary>
        public static string MENU_CONST_REPORT_BUDGET_RESERVE = "REPORT_BUDGET_RESERVE_MENU";


        /// <summary>
        /// ค่าคงที่นี้มาจากตาราง T_MENU.MENU_CONST จะต้องกำหนดให้ตรงกัน<para/>
        /// เพื่อใช้ในการ Mapping หาชื่อเมนู<para/>
        /// รายงานรายละเอียดของรายการค่าใช้จ่าย ที่หน่วยงานระบุ เพื่อส่งคำขอ งบประมาณ
        /// </summary>
        public static string MENU_CONST_REPORT_BUDGET_REQUEST_EXPENSES_DETAIL = "REPORT_BUDGET_REQUEST_EXPENSES_DETAIL_MENU";
    }


}