using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class ModulesPermission
    {
        public int ModulePermissionsID { get; set; }
        public int RoleID { get; set; }
        public int UserID { get; set; }
        public int ModuleID { get; set; }
        public bool Can_View { get; set; }
        public bool Can_Insert { get; set; }
        public bool Can_Update { get; set; }
        public bool Can_Delete { get; set; }
        public bool Can_Authorize { get; set; }
        public bool Can_Reject { get; set; }
        public bool Active { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? CheckerID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? CheckerDate { get; set; }
    }
}
