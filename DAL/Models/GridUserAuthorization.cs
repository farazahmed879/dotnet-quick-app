using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class GridUserAuthorization
    {
        [NotMapped]
        public int UserCheckerID { get; set; }
        public int UserID { get; set; }
        public string PSID { get; set; }
        public string Name { get; set; }
        public string RegionID { get; set; }
        public string RegionName { get; set; }
        public int GroupID { get; set; }
        public string GroupName { get; set; } //  remove space between group name
        public string Department { get; set; }
        public bool Active { get; set; }
        public string Action { get; set; }
        [NotMapped]
        public string ReasonID { get; set; }

        [NotMapped] 
        public string Reason { get; set; }
        public string StatusID { get; set; }
        [NotMapped]
        public string MakerStatus { get; set; }  // not work so take bool that
        
        [NotMapped]
        public string MakerBy { get; set; }
        [NotMapped]
        public DateTime? MakerDate { get; set; }

        [NotMapped]
        public string MakerID { get; set; }
        public string Signatory { get; set; }
        public bool AuthSignatory { get; set; }
        public string Reference { get; set; }
        public string CountryCode { get; set; }
        public string AccountType { get; set; }
        public string AccountDescription { get; set; }
        public string Password { get; set; }        

        [NotMapped]
        public string CreatedBy { get; set; }
        [NotMapped]
        public string CreatedDate { get; set; } // datetime if need

    }
}
