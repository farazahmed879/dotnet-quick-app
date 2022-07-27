using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class GroupManagementViewModel
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public string MakerStatus { get; set; }
        public bool Active { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        [NotMapped]
        public string UpdatedBy { get; set; }
        [NotMapped]
        public DateTime UpdatedDate { get; set; }

        /// <summary>
        /// Group Checker
        /// </summary>
        /// 
        [NotMapped]
        public int GroupCheckerID { get; set; }
        [NotMapped]
        public string CheckerStatus { get; set; }

        [NotMapped]
        public bool CheckerActive { get; set; }
        [NotMapped]
        public string MakerID { get; set; }
        [NotMapped]
        public DateTime MakerDate { get; set; }
        [NotMapped]
        public string CheckerID { get; set; }
        public string CountryCode { get; set; }
        [NotMapped]
        public DateTime CheckerDate { get; set; }

        public string Reference { get; set; }
        public string GroupOwnerPSID { get; set; }
        public string GroupOwnerName { get; set; }
        [NotMapped]
        public string PSID { get; set; } // login user id it is
    }
}
