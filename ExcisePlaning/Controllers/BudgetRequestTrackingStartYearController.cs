using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using OfficeOpenXml.FormulaParsing.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    [CustomAuthorize]
    public class BudgetRequestTrackingStartYearController : Controller
    {
        // GET: BudgetRequestTrackingStartYear
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_BUDGET_REQUEST_TRAKCING_START_YEAR_MENU);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_BUDGET_REQUEST_TRAKCING_START_YEAR_MENU;
            ViewBag.Title = menuItem.MenuName;
            ViewBag.MenuGroups = userAuthorizeProfile.MenuGroups;
            ViewBag.PageName = menuItem.MenuName;
            ViewBag.PageDescription = menuItem.MenuDescription;
            ViewBag.LoginName = userAuthorizeProfile.EmpFullname;

            // กำหนด Breadcrump
            List<Breadcrump> breadcrumps = new List<Breadcrump>(2);
            breadcrumps.Add(new Breadcrump()
            {
                Text = menuIndexItem.MenuName,
                CssIcon = menuIndexItem.MenuIcon,
                ControllerName = menuIndexItem.RouteName,
                ActionName = menuIndexItem.ActionName
            });
            breadcrumps.Add(new Breadcrump()
            {
                Text = menuItem.MenuName,
                CssIcon = menuItem.MenuIcon,
                ControllerName = menuItem.RouteName,
                ActionName = menuItem.ActionName
            });
            ViewBag.Breadcrumps = breadcrumps;

            ViewBag.FiscalYear = userAuthorizeProfile.DefaultFiscalYear;
            ViewBag.AreaId = userAuthorizeProfile.AreaId;
            ViewBag.DepartmentId = userAuthorizeProfile.DepId;
            ViewBag.DepAuthorize = userAuthorizeProfile.DepAuthorize; // 1 = หน่วยงานกลาง, 2 = หน่วยงานทั่วไป
            ViewBag.CanSelectDepartment = userAuthorizeProfile.CanSelectDepartment;
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ข้อมูลเขตพื้นที่
                // ไม่ใช่หน่วยงานกลาง เห็นได้เฉพาะเขตพื้นที่ตนเอง
                var areaExpr = db.T_AREAs.Select(e => new AreaShortFieldProperty()
                {
                    AREA_ID = e.AREA_ID,
                    AREA_NAME = e.AREA_NAME,
                    AREA_CODE = e.AREA_CODE
                });
                if (userAuthorizeProfile.DepAuthorize.Equals(2))
                    areaExpr = areaExpr.Where(e => e.AREA_ID.Equals(userAuthorizeProfile.AreaId));
                ViewBag.Areas = areaExpr.ToList();
            }

            return View();
        }


        /// <summary>
        /// ยกเลิกการ SignOff คำของบประมาณต้นปี
        /// </summary>
        /// <param name="reqId"></param>
        [HttpPost]
        public ActionResult SubmitRejectSignOff(string reqId)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(1) {
                { "errorText", null }
            };
            if (string.IsNullOrEmpty(reqId))
                return Json(res, JsonRequestBehavior.DenyGet);

            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var exprReqMas = db.T_BUDGET_REQUEST_MASTERs.Where(e => e.REQ_ID.Equals(reqId)).FirstOrDefault();
                if (null == exprReqMas)
                    return Json(res, JsonRequestBehavior.DenyGet);
                if (exprReqMas.ACTIVE.Equals(-1))
                {
                    res["errorText"] = string.Format("คำขอถูกยกเลิกไปแล้ว เมื่อ {0}", exprReqMas.DELETED_DATETIME.Value.ToString("dd/MM/yyyy HH:mm:ss", AppUtils.ThaiCultureInfo));
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (!exprReqMas.REQ_TYPE.Equals(1))
                {
                    res["errorText"] = "ไม่ใช้คำขอต้นปี ไม่สามารถยกเลิก SignOff ได้";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }
                else if (!exprReqMas.SIGNOFF_FLAG)
                {
                    res["errorText"] = "คำขอนี้ยังไม่ผ่านการ SignOff";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                // กรณีถ้าไม่ใช่หน่วยงานกลาง ยกเลิกได้เฉพาะคำขอในหน่วยงานตนเอง
                if (!userAuthorizeProfile.VerifyAuthorizeDepartmentById(exprReqMas.DEP_ID))
                {
                    res["errorText"] = "ท่านไม่ได้รับสิทธิ์ให้ยกเลิกการ SignOff ให้กับหน่วยงานอื่น";
                    return Json(res, JsonRequestBehavior.DenyGet);
                }

                exprReqMas.SIGNOFF_ID = null;
                exprReqMas.SIGNOFF_DATETIME = null;
                exprReqMas.SIGNOFF_FLAG = false;
                exprReqMas.UPDATED_DATETIME = DateTime.Now;
                exprReqMas.UPDATED_ID = userAuthorizeProfile.EmpId;

                db.SubmitChanges();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// ค้นหาคำขอ งปม. ต้นปี เพื่อติดตามข้อมูลของแต่ละหน่วยงาน
        /// ดังนี้  หน่วยงานที่ยังไม่ทำคำขอต้นปี  หน่วยงานที่ทำคำขอแล้ว (ยืนยันแล้ว, ยังไม่ยืนยัน)
        /// </summary>
        /// <param name="fiscalYear"></param>
        /// <param name="areaId"></param>
        /// <param name="depId"></param>
        /// <param name="filterType">-1=รอทำคำขอ, 1 = SignOff, 0 = รอ SignOff</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Retrieve(short fiscalYear, int? areaId, int? depId, short? filterType)
        {
            Dictionary<string, object> res = new Dictionary<string, object>(2) {
                { "summaryInfo", null },
                { "rows", null }
            };


            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                var expr = db.proc_GetTrackingBudgetRequestStartYear(fiscalYear).AsQueryable();

                // ตรวจสอบการเข้าถึงข้อมูลของหน่วยงาน
                // 1. กรณีไม่เลือกหน่วยงาน ให้ใช้ข้อมูล Profile กรองข้อมูลตามสิทธิ์
                // 2. กรณีเลือกหน่วยงาน ให้ดูสิทธิ์การเข้าถึงข้อมูลของหน่วยงาน ที่เลือก
                var depFilterAuthorize = DepartmentAuthorizeFilterProperty.Verfity(userAuthorizeProfile, depId);
                if (depFilterAuthorize.Authorize.Equals(2))
                    expr = expr.Where(e => depFilterAuthorize.AssignDepartmentIds.Contains(e.DEP_ID));
                if (null != areaId && null == depId)
                    expr = expr.Where(e => e.AREA_ID.Equals(areaId));

                //// หน่วยงานกลาง เข้าถึงข้อมูลของทุกหน่วยงาน
                //// หน่วยงานทั่วไป หรือ หน่วยงานหลักของภูมิภาค เข้าถึงข้อมูลได้เฉพาะหน่วยงานตนเองหรือที่ได้รับมอบหมาย
                //if (userAuthorizeProfile.DepAuthorize.Equals(2))
                //{
                //    var authorizeDepIds = userAuthorizeProfile.AssignDepartmentIds;
                //    authorizeDepIds.Add(userAuthorizeProfile.DepId);
                //    if (null != depId && authorizeDepIds.IndexOf(depId.Value) > -1)
                //    {
                //        authorizeDepIds.Clear();
                //        authorizeDepIds.Add(depId.Value);
                //    }
                //    expr = expr.Where(e => authorizeDepIds.Contains(e.DEP_ID));
                //}
                //else
                //{
                //    if (null != areaId)
                //        expr = expr.Where(e => e.AREA_ID.Equals(areaId.Value));
                //    if (null != depId)
                //        expr = expr.Where(e => e.DEP_ID.Equals(depId.Value));
                //}

                var finalExpr = expr.ToList();

                // สรุปข้อมูลภาพรวม
                Dictionary<string, object> summaryInfo = new Dictionary<string, object>();
                summaryInfo.Add("TotalBudgetRequestAmounts", finalExpr.Sum(e => e.TOTAL_REQUEST_BUDGET));
                summaryInfo.Add("CountDepartmentHasTransactionAmounts", finalExpr.Where(e => e.REQ_ID != null).Count());

                // จำนวนที่ยืนยันยอดคำของบประมาณ (ยอดงบประมาณ, หน่วยงาน)
                var exprSignOff = finalExpr.Where(e => e.REQ_ID != null && e.SIGNOFF_FLAG != null && e.SIGNOFF_FLAG.Value == true);
                summaryInfo.Add("TotalBudgetRequestSignOffAmounts", exprSignOff.Sum(e => e.TOTAL_REQUEST_BUDGET));
                summaryInfo.Add("CountDepartmentSignOffAmounts", exprSignOff.Count());

                // จำนวนที่รอยืนยันคำของบประมาณ (ยอดงบประมาณ, หน่วยงาน)
                var exprUnSignOff = finalExpr.Where(e => e.REQ_ID != null && (e.SIGNOFF_FLAG == null || e.SIGNOFF_FLAG.Value == false));
                summaryInfo.Add("TotalBudgetRequestUnSignOffAmounts", exprUnSignOff.Sum(e => e.TOTAL_REQUEST_BUDGET));
                summaryInfo.Add("CountDepartmentUnSignOffAmounts", exprUnSignOff.Count());

                // จำนวนที่ยังไม่ทำคำขอ (หน่วยงาน)
                summaryInfo.Add("CountDepartmentNoTransactionAmounts", finalExpr.Where(e => e.REQ_ID == null).Count());

                res["summaryInfo"] = summaryInfo;

                if (null != filterType)
                {
                    if (filterType.Value.Equals(-1))
                        finalExpr = finalExpr.Where(e => e.REQ_ID == null).ToList();
                    else if (filterType.Value.Equals(1))
                        finalExpr = finalExpr.Where(e => e.REQ_ID != null && e.SIGNOFF_FLAG != null && e.SIGNOFF_FLAG == true).ToList();
                    else if (filterType.Value.Equals(0))
                        finalExpr = finalExpr.Where(e => e.REQ_ID != null && e.SIGNOFF_FLAG != null && e.SIGNOFF_FLAG == false).ToList();
                }
                res["rows"] = finalExpr.OrderBy(e => e.AREA_ID).ThenBy(e => e.DEP_SORT_INDEX).ToList();
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }
    }
}