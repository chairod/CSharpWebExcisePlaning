using ExcisePlaning.Classes;
using ExcisePlaning.Classes.Mappers;
using ExcisePlaning.Entity;
using ExcisePlaning.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ExcisePlaning.Controllers
{
    /// <summary>
    /// กำหนดวันหยุดประจำปีในระบบ
    /// </summary>
    [CustomAuthorize(Roles = "Admin")]
    public class HolidayConfigureController : Controller
    {
        // GET: HolidayConfigure
        [HttpGet]
        public ActionResult GetForm()
        {
            UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
            UserAuthorizeMenuProperty menuItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_HOLIDAY_DATE_CONFIGURE);
            UserAuthorizeMenuProperty menuIndexItem = userAuthorizeProfile.FindUserMenu(AppConfigConst.MENU_CONST_DASHBOARD);

            // กำหนดค่า การแสดงผลเมนู
            ViewBag.MenuConst = AppConfigConst.MENU_CONST_HOLIDAY_DATE_CONFIGURE;
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

            return View("HolidayConfigure_Form");
        }

        /// <summary>
        /// สร้างปฏิทิน ประจำปีของแต่ละปี<para/>
        /// ค้นหาวันหยุดในปีนั้นๆ
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet, Route("year:int")]
        public ActionResult GetCalendar(int year)
        {
            List<string> holidayDates = GetHolidayDatesByYear(year);
            List<CalendarMonthProperty> monthsCalendar = GetYearCalendar(year, holidayDates);
            return Json(new Dictionary<string, object>()
            {
                { "HolidayDates", holidayDates },
                { "MonthsCalendar", monthsCalendar }
            }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, Route("holidayDates:List<string>, year:int")]
        public void SubmitSave(List<string> holidayDates, int year)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                // ลบการกำหนดค่าวันหยุดก่อนหน้าทิ้งไป ทุกรายการของปี
                var oldEntities = db.T_HOLIDAY_CONFIGUREs.Where(e => e.YR.Equals(year)).ToList();
                db.T_HOLIDAY_CONFIGUREs.DeleteAllOnSubmit(oldEntities);

                // บันทึกวันหยุดประจำปีที่ ผู้ใช้งาน กำหนดค่าไว้
                if (null != holidayDates)
                {
                    UserAuthorizeProperty userAuthorizeProfile = UserAuthorizeProperty.GetUserAuthorizeProfile(HttpContext.User.Identity.Name);
                    foreach (var holidayDate in holidayDates)
                    {
                        // ตรวจสอบความถูกต้องของรูปแบบวันที่ dd/MM/yyyy
                        List<string> dateParts = holidayDate.Split(new char[] { '/' }).Where(str => !Regex.IsMatch(str, "[^0-9]")).ToList();
                        if (dateParts.Count != 3)
                            continue;
                        DateTime newHolidayDate = new DateTime(Convert.ToInt32(dateParts[2]), Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[0]));

                        db.T_HOLIDAY_CONFIGUREs.InsertOnSubmit(new T_HOLIDAY_CONFIGURE()
                        {
                            YR = Convert.ToInt16(year),
                            HOLIDAY_DATE = newHolidayDate,
                            ACTIVE = 1,
                            CREATED_DATETIME = DateTime.Now,
                            USER_ID = userAuthorizeProfile.EmpId
                        });
                    }

                    userAuthorizeProfile = null;
                }

                db.SubmitChanges();
            }
        }

        private List<string> GetHolidayDatesByYear(int year)
        {
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                return db.T_HOLIDAY_CONFIGUREs
                            .Where(e => e.YR.Equals(year) && e.ACTIVE.Equals(1) && e.HOLIDAY_DATE != null)
                            .Select(e => e.HOLIDAY_DATE)
                            .OrderBy(e => e)
                            .AsEnumerable()
                            .Select(e => e.ToString("dd/MM/yyyy"))
                            .ToList();
            }
        }

        /// <summary>
        /// สร้างปฏิทินในแต่ละปี
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        private List<CalendarMonthProperty> GetYearCalendar(int year, List<string> holidayDates)
        {
            List<CalendarMonthProperty> monthsCalendar = new List<CalendarMonthProperty>();
            DateTime sourceDate = new DateTime(year, 1, 1);
            CalendarMonthProperty monthCalendar = null;
            int sourceMonth = 1,
                oldMonthNo = -1,
                weekIndex = 1;
            do
            {
                if (oldMonthNo == -1 || !sourceMonth.Equals(oldMonthNo))
                {
                    if (oldMonthNo != -1)
                        monthsCalendar.Add(monthCalendar);
                    //var thCulture = new CultureInfo("th-TH");
                    monthCalendar = new CalendarMonthProperty()
                    {
                        MonthNo = sourceMonth,
                        YearNo = sourceDate.Year,
                        Label = string.Format("{0}, {1}", sourceDate.ToString("MMM", AppUtils.ThaiCultureInfo), sourceDate.ToString("yyyy", AppUtils.ThaiCultureInfo))
                    };
                    weekIndex = 1;
                }

                string weekKey = string.Format("Week{0}", weekIndex);
                var dayProp = monthCalendar.Weeks[weekKey].Where(day => day.DayOfWeek == sourceDate.DayOfWeek).FirstOrDefault();
                dayProp.DateStr = sourceDate.ToString("dd/MM/yyyy");
                dayProp.CanSelect = (sourceDate.DayOfWeek != DayOfWeek.Sunday && sourceDate.DayOfWeek != DayOfWeek.Saturday);
                dayProp.DayOfMonth = sourceDate.Day;
                dayProp.IsHolidayDate = holidayDates.IndexOf(dayProp.DateStr) > -1;
                dayProp.IsCurrentDay = sourceDate.Equals(DateTime.Now.Date);

                oldMonthNo = sourceMonth;
                sourceDate = sourceDate.AddDays(1);
                sourceMonth = sourceDate.Month;
                if (sourceDate.DayOfWeek == DayOfWeek.Sunday)
                    weekIndex++;

                if (!sourceDate.Year.Equals(year))
                {
                    monthsCalendar.Add(monthCalendar);
                    break;
                }
            } while (true);

            return monthsCalendar;
        }

        /// <summary>
        /// ในแต่ละเดือนจะถูกสร้าง จันทร์ - อาทิตย์ ไว้ทั้งหมด 6 สัปดาห์/เดือน (Weeks)
        /// </summary>
        public class CalendarMonthProperty
        {
            public CalendarMonthProperty()
            {
                Weeks = new Dictionary<string, List<CalendarDayProperty>>();
                Weeks.Add("Week1", new List<CalendarDayProperty>() {
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
                });

                Weeks.Add("Week2", new List<CalendarDayProperty>() {
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
                });

                Weeks.Add("Week3", new List<CalendarDayProperty>() {
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
                });

                Weeks.Add("Week4", new List<CalendarDayProperty>() {
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
                });

                Weeks.Add("Week5", new List<CalendarDayProperty>() {
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
                });
                Weeks.Add("Week6", new List<CalendarDayProperty>() {
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Sunday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Monday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Tuesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Wednesday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Thursday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Friday },
                    new CalendarDayProperty(){DayOfWeek = DayOfWeek.Saturday }
                });
            }

            public int MonthNo { get; set; }
            public int YearNo { get; set; }

            /// <summary>
            /// ข้อความสำหรับแสดงบน ปฏิทินของเดือน เช่น มี.ค., 63 เป็นต้น
            /// </summary>
            public string Label { get; set; }

            /// <summary>
            /// ข้อมูลสัปดาห์ ของแต่ละเดือน <para/>
            /// ประกอบไปด้วย 5 รายการ "Week1" ... "Week5" <para/>
            /// ในแต่ละรายการ (Week1 ... Week5) จะมีทั้งหมด 7 วัน
            /// </summary>
            public Dictionary<string, List<CalendarDayProperty>> Weeks { get; set; }
        }

        public class CalendarDayProperty
        {
            public CalendarDayProperty()
            {
                DateStr = "";
                DayOfMonth = null;
                CanSelect = false;
                IsHolidayDate = false;
                IsCurrentDay = false;
            }

            /// <summary>
            /// วันที่ในรูปแบบ dd/MM/yyyy
            /// </summary>
            public string DateStr { get; set; }

            /// <summary>
            /// วันในสัปดาห์ 0..6 (Sun ... Sat)
            /// </summary>
            public DayOfWeek DayOfWeek { get; set; }

            /// <summary>
            /// วันที่ของเดือน 1..30/31
            /// </summary>
            public int? DayOfMonth { get; set; }

            /// <summary>
            /// สามารถเลือกกำหนดเป็น วันหยุด ได้หรือไม่ (กรณีตรงกับเสาร์ อาทิตย์ ไม่ให้เลือกเป็นวันหยุด)
            /// </summary>
            public bool CanSelect { get; set; }

            /// <summary>
            /// กำหนดเป็นวันหยุดแล้วใช่หรือไม่
            /// </summary>
            public bool IsHolidayDate { get; set; }

            /// <summary>
            /// เป็นวันปัจจุบันใช่หรือไม่
            /// </summary>
            public bool IsCurrentDay { get; set; }
        }
    }
}