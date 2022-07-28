using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class TBL_Pages  //SP_Fill_Pages_By_ModuleID
    {
        public int PageDetailsID { get; set; }
        public int PageID { get; set; }
        public int ModuleID { get; set; }
        public string PageName { get; set; }
        public string PageDescription { get; set; }
        public string PageUrl { get; set; }
        public string PageIcon { get; set; }
        public int PageSortOrder { get; set; }
        public int ModuleSortOrder { get; set; }
        public string ModuleIcon { get; set; }
        public bool Active { get; set; }
        public bool Crud_View { get; set; }
        public bool Crud_Insert { get; set; }
        public bool Crud_Update { get; set; }
        public bool Crud_Authorize { get; set; }
        public bool Crud_Reject { get; set; }
        public bool Crud_Delete { get; set; }

        public int? ApplicationID { get; set; }
    }
}
