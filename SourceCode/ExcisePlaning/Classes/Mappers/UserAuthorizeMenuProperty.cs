using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    public class UserAuthorizeMenuProperty
    {
        public string MenuName { get; set; }
        public string MenuDescription { get; set; }
        public string MenuIcon { get; set; }
        public string MenuConst { get; set; }
        public string RouteName { get; set; }
        public string ActionName { get; set; }
        public string QueryString { get; set; }
    }
}