using ExcisePlaning.Classes.Mappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace ExcisePlaning.Models
{
    public class CustomExceptionHandle : ActionFilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            // เขียนข้อผิดพลาดที่เกิดขึ้นลง Log file
            AppSettingProperty appSetting = AppSettingProperty.ParseXml();
            string logFile = string.Format("{0}/App-Error.log", appSetting.ErrorLogPath);
            int fileSizeMB = 0;
            using (FileStream fs = new FileStream(logFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                // ย้าย Cursor ไปบรรทัดสุดท้ายเพื่อเตรียมเขียน Error
                fs.Position = fs.Length;
                byte[] buffer = null;

                // ขึ้นบรรทัดใหม่ กรณีที่มีการเขียน Log ไว้แล้ว
                if(fs.Length > 0)
                {
                    buffer = UTF8Encoding.UTF8.GetBytes("\n");
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Flush();
                }

                // เขียน Error ลง Log file
                var exception = filterContext.Exception;
                string errorMsg = string.Format("{0} - {1} \n{2}", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), exception.Message, exception.StackTrace);
                buffer = UTF8Encoding.UTF8.GetBytes(errorMsg);
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();

                // คำนวนขนาดของไฟล์ เพื่อใช้เป็นเงื่อนไขในการย้าย ไฟล์
                // แปลงจาก Byte ให้เป็น Megabyte
                fileSizeMB = Convert.ToInt32(Math.Ceiling(fs.Length / Convert.ToDouble("1048576")));
            }

            // ย้ายไฟล์ไปเก็บที่พาร์ท Archives หากขนาดไฟล์เกิน 10M
            if (fileSizeMB.CompareTo(10) != -1)
                File.Move(logFile, 
                    string.Format("{0}/Archives/App-Error_{1}.log", appSetting.ErrorLogPath, DateTime.Now.ToString("dd-MM-yyyy")));


            // กรณีเรียกเข้ามาด้วย Ajax: headers: { 'X-Requested-With': 'XMLHttpRequest' }
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                try
                {
                    filterContext.HttpContext.Response.StatusCode = 500;
                    filterContext.HttpContext.Response.End();
                }
                catch(HttpException ex)
                {
                    Console.WriteLine("errorCode: {0}, errorMessage: {1}", ex.ErrorCode, ex.Message);
                }
            }

            // กรณีเรียกด้วย Http Get
            // ให้ตรวจสอบ Context Path (Alias Name) ก่อน เพื่อระบุ Route ในการ Redirect ได้ถูกต้อง
            //string redirectTo = "/";
            //filterContext.HttpContext.Response.Redirect()
        }
    }
}