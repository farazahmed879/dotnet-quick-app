using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class UserLoginVM
    {
        public int SNO { get; set; }

        public string PSID { get; set; }
        public string UserName { get; set; }
        public string PASSWORD { get; set; }
        public string StatusID { get; set; }
        public bool Active { get; set; }

        public string Status { get; set; }
        public string STATUS_REQUEST { get; set; }
        public string RegionID { get; set; }  //location id from tbl_location
        public string RegionName { get; set; }  ////location from tbl_locationo i.:location name

         public int GroupID { get; set; }
        public string GroupName { get; set; }
        public bool GroupActive { get; set; }
        public string Department { get; set; }
        public int LOGON_COUNT { get; set; }
        public string LOGIN_IP { get; set; }
        public bool User_Lock { get; set; }
        public int Login_Attempts { get; set; }
        public DateTime? Unsuccessful_Date { get; set; }

        public string SessionID { get; set; }
        public string AccountType { get; set; }
        public string AccountDescription { get; set; }
    }
}
