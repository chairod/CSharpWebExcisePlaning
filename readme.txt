ระบบจัดการเงินงบประมาณ (กรมสรรพสามิต)
แบ่งออกเป็น เงินงบประมาณ และ เงินนอกงบประมาณ
Modules:
    1.) วางแผนเงินงบประมาณที่คาดว่าจะได้รับในแต่ละปี งปม.
        -> งบประมาณปกติ
        -> งบประมาณพลางก่อน
    2.) จัดการ เงินประจำงวด และ จัดเก็บภาษี
    3.) ประมาณการรายได้ภาษีในแต่ล่ะปีงบประมาณ แยกเป็นเดือน
    4.) หน่วยงานภายนอก
        -> สร้างคำขอใช้เงินงบประมาณ จากส่วนกลาง
        -> รับจัดสรรงบประมาณ
        -> รายงานผลการใช้จ่าย งบประมาณ ในแต่ละเดือน
    5.) หน่วยงานภายใน
        -> กันเงินงบประมาณ
        -> เบิกจ่าย จากการกันเงิน
        -> เบิกเกินส่งคืน
        -> ปรับปรุงบัญชี ปรับปรุงรายการเบิกจ่ายที่เบิกไปแล้ว แต่เบิกผิดประเภท
    6.) รายงาน

เทคโนโลยีที่นำมาใช้
 - C# ASP.NET MVC 5
 - Razor
 - angularjs 1.6
 - Bootstrap 4
 - รองรับ 100% Responsive
 - SQL Server 2019
 - .NET Framework 4.6
=================================================

โปรเจ็คต้นแบบ ที่พร้อมนำไปพัฒนาต่อยอด เป็นโครงการอื่นๆได้ทันที ซึ่งประกอบด้วย ส่วน Module การทำงานพื้นฐาน ได้แก่

1.) การทำ Authentication & Authorization
    - Login ตรวจสอบตัวตน การใช้ระบบ
    - ตรวจสอบสิทธิ์การเข้าถึง Controller ต่างๆ ด้วย Role (T_ROLE.ROLE_CONST) 
    - ตรวจจับการปฏิสัมพันธ์ ของผู้ใช้งาน หากเกิน 30 นาที จะให้เข้าสู่ระบบใหม่ (กำหนดค่าผ่าน Config file)
2.) หน้าจอจัดการ กลุ่มผู้ใช้งาน (T_ROLE, T_PERSONNEL_AUTHORIZE)
    - กำหนดชื่อกลุ่ม และ สิทธิ์การเข้าถึง Controller (หน้าจอ) ในระบบ
    - กำหนด รายชื่อเมนู ให้กับกลุ่มผู้ใช้งาน
    - กำหนด ผู้ใช้งานที่อยู่ในกลุ่มผู้ใช้งาน
3.) หน้าจอจัดการ วันหยุดประจำปี (T_HOLIDAY_CONFIGURE)
4.) หน้าจอจัดการ พนักงาน พร้อมกับสร้างผู้ใช้งานในระบบ (T_PERSONNEL)
    - สร้างและแก้ไข ผู้ใช้งานในระบบ
    - รหัสผ่าน Default ไว้ที่ 1234 (ในกรณีสร้างผู้ใช้งานใหม่)
    - ใช้ Email ในการเข้าสู่ระบบ
5.) หน้าจอจัดการ หน่วยงาน (T_DEPARTMENT)
    - ผูกข้อมูลไว้กับ เขตพื้นที่ (T_AREA)
ุ6.) หน้าจอจัดการประเภทบุคลากร (T_PERSONNEL_TYPE) เช่น พนักงานราชการ ลูกค้าชั่วคราว เป็นต้น
7.) หน้าจอจัดการ ตำแหน่งงาน
    - ผูกข้อมูลเข้ากับ ประเภทบุคลากร  ซึ่งการระบุตำแหน่งงานนั้น จะขึ้นอยู่กับบุคลากรแต่ละประเภท
8.) Logout
9.) ResourceController บริการจัดการ Static Content ในระบบโดยไม่ชี้พาร์ทที่เก็บไว้ของไฟล์ต่างๆ เพื่อป้องการการเข้าถึงโดยตรงจาก Client
    - DeleteFile    ลบไฟล์ โดยอ้างอิงจากพาร์ทที่ต้องการลบจาก groupType & filename
    - GetImage      อ่านไฟล์รูปภาพ และตอบกลับเป็น Stream โดยอ้างอิงพาร์ทรูปภาพจาก groupType & filename
    - GetFile       อ่านไฟล์ และตอบกลับเป็น Stream โดยอ้างอิงพาร์ทไฟล์จาก groupType & filename, และเมื่อตอบกลับต้องการให้ลบไฟล์หรือไม่ (deleteFlag = Y)
    - SubmitUploadFile Upload file ขึ้นมาเก็บในระบบ ใช้งานร่วมกับ angularjs fwFileUpload directive
        <fw-file-upload upload-url="@Url.Action("SubmitUploadFile", "Resource", new { uploadType = "Temporary" })"
                                accept-filetypes="xls,xlsx"
                                ng-disabled="$settings.isLoading"
                                callback="uploadDone($res)"></fw-file-upload>
        function uploadDone(res){
            console.log(res.data.filename); // ชื่อไฟล์ที่ระบบ Upload สำเร็จ
            console.log(res.data.errorText); // ข้อความข้อผิดพลาด กรณีระบบ Upload ไม่สำเร็จ
            console.log(res.data.mimeType); // mime type ของไฟล์ที่ Upload ไปยังระบบ
        };

=================================================
การ Initial ระบบเพื่อนำไปใช้งาน
1. AppSettings.xml
    => UserAuthorizeCachePath
        พาร์ทสำหรับเขียน Cache profile หลังจากเข้าสู่ระบบ
    => ErrorLogPath
        พาร์ทสำหรับเขียนข้อผิดพลาดลงไป
    => TemporaryPath
        พาร์ทสำหรับเขียน Temporary File ของระบบ
    => ReportTemplatePath
        พาร์ทสำหรับเก็บ Template ของรายงาน
	=> AcceptMimeTypeValues
		MIME Type ที่ระบบรองรับการอัพโหลดไฟล์จากเครื่องไคเอ็นท์
	=> LimitedFileSizeBytesValue
		ขนาดไฟล์สูงสุดที่ระบบรองรับในการอัพโหลดไฟล์จากเครื่องไคเอ็นท์
    => MaximumWaitUserInteractiveMinutesStr
        ระยะเวลาสูงสุดที่ผู้ใช้งาน สามารถ หยุดปฏิสัมพันธ์กับระบบ (นาที)
    หากเกินค่านี้ ระบบจะให้ Login ใหม่
2. Web.Config
    => customErrors <system.web>
        กรณีนำเป็นใช้งานโดยกำหนด AliasName ให้ระบุ AliasName นำหน้าด้วย 
        เช่น DefualtRedirect="/Authorize/PageErrors" ถ้ามี AliasName = "LeaveSystem" ให้กำหนดค่าให้เป็น
        DefaultRedirect="/LeaveSystem/Authorize/PageErrors"
    => globalization <system.web>
        ในระบบจะใช้ Culture inf เป็น "en-US" กำหนดไว้ในกรณีไม่ต้องการให้อ่าน Culture infomation จากเครื่อง Server ที่ตั้งไว้

[third_party/assets/js/angular.app.js]
    // Base Url หลักของระบบ
    $rootScope.baseUrl = 'http://localhost:2194/';
[Contents/Standalone/vegas.js]
    หาก Deploy Website ไปยัง Production หรือ Server มี AliasName ให้ระบุ AliasName นำหน้าด้วย 
    เช่น AliasName = "LeaveSystem" ค่าที่กำหนดจะเป็น /LeaveSystem/Resource/GetImage...