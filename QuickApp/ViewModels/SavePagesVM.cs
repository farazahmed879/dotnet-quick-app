using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickApp.ViewModels
{
    public class SavePagesVM
    {
        public string RoleID { get; set; }
        public string ModuleID { get; set; }
        public string PageID { get; set; }
        public bool canView { get; set; }
        public bool canInsert { get; set; }
        public bool canUpdate { get; set; }
        public bool canAuthorize { get; set; }
        public bool canReject { get; set; }
        public bool canDelete { get; set; }
    }
}
