using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HRWebApp
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; } // thêm dòng này
    }
}