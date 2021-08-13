using System;
using System.Web.Mvc;
using System.IO;
using ExcisePlaning.Models;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Classes;
using System.Text;
using System.Net;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize]
    public class ResourceController : Controller
    {
        /// <summary>
        /// ดึงส่วนการแสดงผล Html (Partial)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        [HttpGet, Route("{partialName:string}")]
        public ActionResult GetPartialResource(string partialName)
        {
            if (!VerifyVulnerability.VerifyPathTraversal(partialName))
            {
                VerifyVulnerability.ThrowBadRequest(base.HttpContext);
                return null;
            }

            return View(string.Format("Partials/_Partials_{0}", partialName));
        }

        /// <summary>
        /// ลบไฟล์ที่อัพโหลด
        /// </summary>
        /// <param name="groupType"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpGet]
        public void DeleteFile(string groupType, string filename)
        {
            if (!VerifyVulnerability.VerifyPathTraversal(filename))
                return;

            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            string filePath = "";
            if ("BudgetRequest".Equals(groupType))
                filePath = appSetting.BudgetRequestAttachFilePath;

            string file = string.Format(@"{0}/{1}", filePath, filename);
            if (!System.IO.File.Exists(file))
                return;

            System.IO.File.Delete(file);
        }

        /// <summary>
        /// ดึงข้อมูลรูปภาพนำไปแสดงบนหน้าเว็บไซด์ เพื่อปกปิดพาร์ทรูปภาพ<para/>
        /// groupType: กลุ่มของรูปภาพ ได้แก่ Standalone เป็นต้น<para/>
        /// name: ชื่อไฟล์ ที่ต้องการ Stream คืนค่ากลับไปแสดงหน้าเว็บ
        /// </summary>
        /// <param name="groupType"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet, Route("groupType:string, filename:string")]
        public ActionResult GetImage(string groupType, string filename)
        {
            if (string.IsNullOrEmpty(groupType) || string.IsNullOrEmpty(filename))
                return null;

            if (!VerifyVulnerability.VerifyPathTraversal(filename))
            {
                VerifyVulnerability.ThrowBadRequest(base.HttpContext);
                return null;
            }


            string filePath = "";
            groupType = groupType.ToLower();
            if ("standalone".Equals(groupType))
                filePath = Server.MapPath("~/Contents/StandAlone/img");
            else if ("third_party".Equals(groupType))
                filePath = Server.MapPath("~/Third_Party/assets/images");

            filePath = string.Format("{0}/{1}", filePath, filename);
            if (!System.IO.File.Exists(filePath))
                return null;
            string fileExt = Regex.Replace(filename, @"^.*\.", "");

            return base.File(filePath, string.Format("image/{0}", fileExt));
        }


        [AllowAnonymous]
        [HttpGet]
        public ActionResult GetManual()
        {
            string file = string.Format("{0}/excise-manual-2021-08-13.pdf", Server.MapPath("~/Contents"));

            // กรณีไม่พบไฟล์
            if (!System.IO.File.Exists(file))
            {
                byte[] buffer = Encoding.UTF8.GetBytes("<center><h1 style=\"color:red\">FILE NOT FOUND</h1></center>");
                return base.File(buffer, "text/html");
            }

            // อ่านไฟล์ลง stream และตอบกลับ
            using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                stream.Close();

                var fileContentResult = base.File(buffer, "application/octet-stream", "excise-manual-2021-08-13.pdf");
                return fileContentResult;
            }
        }


        /// <summary>
        /// แสดงข้อมูลไฟล์ที่อัพโหลดไว้ในระบบ <para/>
        /// groupType ประกอบด้วย overtime: ไฟล์แนบคำขอ OT, Leave: ไฟล์แนบคำขอลา, Temporary: ไฟล์รายงานหรืออื่นๆ<para/>
        /// resultFilename: ไม่ต้องระบุนามสกุลไฟล์เข้ามา
        /// </summary>
        /// <param name="groupType"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpGet, Route("groupType:string, filename:string, resultFilename:string, deleteFlag: string")]
        public ActionResult GetFile(string groupType, string filename, string resultFilename, string deleteFlag)
        {
            if (!VerifyVulnerability.VerifyPathTraversal(filename))
            {
                VerifyVulnerability.ThrowBadRequest(base.HttpContext);
                return null;
            }

            string file = null;

            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            if("Temporary".Equals(groupType))
                file = string.Format("{0}/{1}", appSetting.TemporaryPath, filename); 
            else if("BudgetRequest".Equals(groupType))
                file = string.Format("{0}/{1}", appSetting.BudgetRequestAttachFilePath, filename);

            // กรณีไม่พบไฟล์
            if (!System.IO.File.Exists(file))
            {
                byte[] buffer = Encoding.UTF8.GetBytes("<center><h1 style=\"color:red\">FILE NOT FOUND</h1></center>");
                return base.File(buffer, "text/html");
            }

            // อ่านไฟล์ลง stream และตอบกลับ
            using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string fileExt = Regex.Replace(filename, @"^.+\.", "", RegexOptions.IgnoreCase);
                resultFilename = string.IsNullOrEmpty(resultFilename) ? filename : string.Format("{0}.{1}", resultFilename, fileExt);

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                stream.Close();

                var fileContentResult = base.File(buffer, "application/octet-stream", resultFilename);
                if ("Y".Equals(deleteFlag))
                    System.IO.File.Delete(file);

                return fileContentResult;
            }
        }

        /// <summary>
        /// อัพโหลดไฟล์จากเครื่องไคเอ็นท์ ขึ้นมาในระบบ<para />
        /// fileBase64Data: data:MIME Type:base64,[file data]<para/>
        /// uploadType: Overtime: คำขอทำงานล่วงเวลา (OT), Profile: รูปโปรไฟล์ของผู้ใช้งาน, Leave: คำขอลา, RequiredChangeWorkingDate: คำขอเปลี่ยนแปลงเวลาเข้าออก <para/>
        /// oldFilename: หากมีชื่อไฟล์เดิมจะเขียนทับไฟล์เดิม โดยไม่สร้างไฟล์ใหม่<para/>
        /// </summary>
        /// <param name="fileBase64Data"></param>
        /// <param name="fileMimeType">Mime Type ของไฟล์</param>
        /// <param name="filename"></param>
        /// <param name="oldFilename"></param>
        /// <param name="uploadType"></param>
        /// <returns></returns>
        [HttpPost, Route("fileBase64Data:string, fileBytes:long, fileMimeType:string, filename:string, oldFilename: string, uploadType:string")]
        public ActionResult SubmitUploadDocument(string fileBase64Data, long fileBytes, string fileMimeType, string filename, string oldFilename, string uploadType)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(3) {
                { "filename", null }, // ชื่อไฟล์หลังจาก อัพโหลดเสร็จ
                { "mimeType", fileMimeType }, // MIME-Type ของไฟล์ที่อัพโหลดขึ้นมา
                { "errorText", null } // ข้อผิดพลาดที่แจ้งกลับไปยังไคเอ็นท์
            };

            // ขนาดของไฟล์ไม่ถูกต้อง
            if (fileBytes.CompareTo(0) <= 0 || string.IsNullOrEmpty(fileBase64Data))
            {
                res["errorText"] = string.Format("ขนาดของไฟล์ไม่ถูกต้อง {0}", fileBytes);
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            // MIME Type
            if (appSetting.AcceptMimeTypes.IndexOf(fileMimeType) == -1)
            {
                res["errorText"] = string.Format("ระบบไม่รองรับรูปแบบไฟล์ที่อัพโหลด (MIME-Type: {0})", fileMimeType);
                return Json(res, JsonRequestBehavior.DenyGet);
            }

            // ขนาดของไฟล์
            if (appSetting.LimitedFileSizeBytes > -1 && appSetting.LimitedFileSizeBytes.CompareTo(fileBytes) == -1)
            {
                res["errorText"] = string.Format("ระบบรองรับขนาดของไฟล์ไม่เกิน {0} bytes", appSetting.LimitedFileSizeBytes.ToString("#,##0.00"));
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            string filePath = null;
            if ("BudgetRequest".Equals(uploadType))
                filePath = appSetting.BudgetRequestAttachFilePath;
            else if ("Temporary".Equals(uploadType))
                filePath = appSetting.TemporaryPath;
            //else if ("Profile".Equals(uploadType))
            //    filePath = appSetting.PersonProfilePath;
            //else if ("RequiredChangeWorkingDate".Equals(uploadType))
            //    filePath = appSetting.RequireChangeWorkingDateAttachFilePath;
            //else if ("UpdateWorkingTime".Equals(uploadType))
            //    filePath = appSetting.RequestUpdateWorkingDateTemporaryPath;

            // ระบบรองรับรูปแบบการอัพโหลด
            // OverTime: ไฟล์แนบในการขอทำล่วงเวลา (OT)
            if (string.IsNullOrEmpty(filePath))
            {
                res["errorText"] = string.Format("โปรดแจ้งผู้ดูแลระบบให้กำหนดพาร์ทของการอัพโหลด ให้กับประเภท {0}", uploadType);
                return Json(res, JsonRequestBehavior.DenyGet);
            }


            string newFilename = oldFilename;
            string fileExt = Regex.Replace(filename, @"^.+\.", "", RegexOptions.IgnoreCase);
            // สร้างชื่อไฟล์ที่กำลังจะอัพโหลด
            if (string.IsNullOrEmpty(newFilename))
            {
                newFilename = AppUtils.GetMD5Value(string.Format("{0}_{1}", filename, DateTime.Now.Ticks));
                newFilename = string.Format("{0}.{1}", newFilename, fileExt);
            }
            else
            {
                // ยกเลิกนามสกุลไฟล์ตัวเดิมออก และ ใช้นามสกุลไฟล์ใหม่
                newFilename = Regex.Replace(newFilename, @"\..+$", "", RegexOptions.IgnoreCase);
                newFilename = string.Format("{0}.{1}", newFilename, fileExt);

                // ลบไฟล์เดิมทิ้ง
                string oldFile = string.Format("{0}/{1}", filePath, oldFilename);
                if (System.IO.File.Exists(oldFile))
                    System.IO.File.Delete(oldFile);
            }
            filePath = string.Format("{0}/{1}", filePath, newFilename);

            // เขียนไฟล์ลง Server
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                fs.SetLength(0);
                fs.Flush();

                string fileData = Regex.Replace(fileBase64Data, @"^data(.+)base64\,", "", RegexOptions.IgnoreCase);
                fileBase64Data = null;
                byte[] buffer = Convert.FromBase64String(fileData);
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();
                fs.Close();
            }

            res["filename"] = newFilename;

            return Json(res, JsonRequestBehavior.DenyGet);
        }
    }
}