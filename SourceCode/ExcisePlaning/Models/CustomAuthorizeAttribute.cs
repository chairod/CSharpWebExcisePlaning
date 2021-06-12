using ExcisePlaning.Classes.Mappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Models
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // กรณีมีการส่งคำร้องเข้ามายังระบบ ให้ตรวจสอบก่อนว่าไฟล์ Authorize ยังมีอยู่หรือไม่
            AppSettingProperty appSettings = AppSettingProperty.ParseXml();
            string authorizeFile = string.Format("{0}/{1}.authorize", appSettings.UserAuthorizeCachePath, httpContext.User.Identity.Name);
            return File.Exists(authorizeFile);

            //return base.AuthorizeCore(httpContext);
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (filterContext.Result == null)
                return;

            // กรณีไม่ต้องการ ใช้ Code ในการ Redirect ให้ตั้งค่าที่ web.config
            // <system.web>
            //      <authentication mode="Forms">
            //          <forms defaultUrl="Authorize/Unauthorize" />
            //      </authentication>
            // </system.web>
            if (filterContext.Result.GetType().Equals(typeof(HttpUnauthorizedResult)))
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.HttpContext.Response.StatusCode = 401;
                    filterContext.HttpContext.Response.End();
                }
                else
                {
                    // ให้หา Context Path (AliasName) ของเว็บไซด์ก่อน เพื่อให้สามารถกำหนด Redirect ได้ถูกต้อง
                    // เช่น /LeaveSystem, หรือ /
                    string contextPath = filterContext.HttpContext.Request.ApplicationPath;
                    string redirectUnauthorizeRoute = "/Authorize/Unauthorize";
                    if (!"/".Equals(contextPath))
                        redirectUnauthorizeRoute = string.Format("{0}/Authorize/Unauthorize", contextPath);
                    filterContext.HttpContext.Response.Redirect(redirectUnauthorizeRoute);
                }
        }
    }
}