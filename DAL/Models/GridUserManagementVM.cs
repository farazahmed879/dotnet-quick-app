using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class GridUserManagementVM
    {
        public int? UserID { get; set; }
        public int? GroupID { get; set; }
        public string GroupName { get; set; }
        public string PSID { get; set; }
        public string NAME { get; set; }

        public string Status { get; set; }

        public string StatusID { get; set; }

        public string StatusRequest { get; set; }

        public string DEPARTMENT { get; set; }
        public bool Active { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        //public DateTime? MAKER_DATE { get; set; }
        public string Signatory { get; set; }
        public bool? AuthSignatory { get; set; }
        public string Reference { get; set; }
        public string CountryCode { get; set; }
        public string AccountType { get; set; }

        [NotMapped]
        public string AccountDescription { get; set; }
        public string Password { get; set; }
        
        public string RegionID { get; set; }
        public string RegionName { get; set; }
        
        //[NotMapped]
        public string CreatedBy { get; set; }
        //[NotMapped]
        public string CreatedDate { get; set; }
    }
}
