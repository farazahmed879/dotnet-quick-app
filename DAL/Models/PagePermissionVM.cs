using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class PagePermissionVM
    {
        public int ModuleID { get; set; }
        public int RoleID { get; set; }
        public int UserID { get; set; }
        public int PageID { get; set; }

        public bool Can_View { get; set; }
        public bool Can_Insert { get; set; }
        public bool Can_Update { get; set; }
        public bool Can_Authorize { get; set; }
        public bool Can_Reject { get; set; }
        public bool Can_Delete { get; set; }
        public bool Active { get; set; }
        public string PageUrl { get; set; }
        public string ModuleName { get; set; }
    }
}
