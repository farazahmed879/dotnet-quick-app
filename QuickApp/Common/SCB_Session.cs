using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QuickApp.Common
{
    public class SCB_Session
    {
        public SCB_Session()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        public DataTable PermissionTable = new DataTable();
        public DataTable DashBoardPermission = new DataTable();
        public DataTable DashBoardMenuPermission = new DataTable();
        public DataTable AuthorizedSignature = new DataTable();
        public int ApplicationID { get; set; }
        public int UserID { get; set; }
        public string SessionID { get; set; }
        public string Sno { get; set; }
        public string RegionID { get; set; }
        public int GroupID { get; set; }
        public string PSID { get; set; }
        public string UserName { get; set; }
        public string UserStatus { get; set; }
        public string RegionName { get; set; }
        public string GroupName { get; set; }
        public bool GroupActive { get; set; }
        public string Department { get; set; }
        public bool Can_Insert { get; set; }
        public bool Can_Update { get; set; }
        public bool Can_Delete { get; set; }
        public bool Can_View { get; set; }
        public bool Can_Authorize { get; set; }
        public bool Can_Reject { get; set; }
        public string Permission { get; set; }
        public string PageRefrence { get; set; }
        public int? Login_Counter { get; set; }
        public bool? User_Lock { get; set; }
        public bool? Active { get; set; }
        public int? Login_Attempts { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime? Last_Unsuccessful_Date { get; set; }
        public int LoginHistoryID { get; set; }
        public int ApplicationAccess { get; set; }
        public string ApplicationName { get; set; }
        public string isLogin { get; set; }
    }
}
