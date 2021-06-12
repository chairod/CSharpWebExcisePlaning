using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    public class UserMenuGroupProperty
    {
        public UserMenuGroupProperty()
        {
            UserMenus = new List<UserAuthorizeMenuProperty>();
        }

        public string GroupName { get; set; }
        public string GroupIcon { get; set; }

        /// <summary>
        /// ต้องการให้แสดงกระดิ่งแจ้งเตือน หรือไม่
        /// ไว้ใช้ในกรณีที่ มี Task งานการอนุมัติในกลุ่มผู้ที่มีอำนาจในการอนุมัติคำขอ
        /// </summary>
        public bool IsAlert { get; set; }

        public List<UserAuthorizeMenuProperty> UserMenus { get; set; }
    }
}