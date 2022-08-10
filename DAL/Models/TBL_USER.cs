using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class TBL_USER
    {
        public int SNO { get; set; }
        public string LOGIN_ID { get; set; }
        public string NAME { get; set; }
        public string PASSWORD { get; set; }
        public string STATUS { get; set; }
        public int GROUP_ID { get; set; }
        public string LOCATION_ID { get; set; }
        public string DEPARTMENT { get; set; }
        public string MAKER_ID { get; set; }
        public string CHECKER_ID { get; set; }
        public DateTime? MAKER_DATE { get; set; }
        public DateTime? CHECKER_DATE { get; set; }
        public DateTime? LAST_UPDATED { get; set; }
        public string LAST_UPDATED_BY { get; set; }
        public int LOGON_COUNT { get; set; }
        public string LOGIN_IP { get; set; }
        public string STATUS_REQUEST { get; set; }
        public bool Active { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        public bool User_Lock { get; set; }
        public int Login_Attempts { get; set; }
        public int Login_History_ID { get; set; }

        public DateTime? Unsuccessful_Date { get; set; }

        public string Signatory { get; set; }
        
        public bool AuthSignatory { get; set; }
        public bool SignatoryAvailable { get; set; }
        public string Reference { get; set; }
        public string PASSWORD2 { get; set; }
        public string SessionID { get; set; }
        public string CountryCode { get; set; }
        public string AccountType { get; set; }
        public string PriviligedID { get; set; }
        public string AccountDescription { get; set; }




    }
}
