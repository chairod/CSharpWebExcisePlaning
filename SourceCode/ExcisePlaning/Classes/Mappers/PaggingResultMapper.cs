using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExcisePlaning.Classes.Mappers
{
    public class PaggingResultMapper
    {
        public int totalRecords { get; set; }
        public double totalPages { get; set; }

        public dynamic rows { get; set; }

        public dynamic responseOpts { get; set; }
    }
}