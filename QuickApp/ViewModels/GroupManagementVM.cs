using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace QuickApp.ViewModels
{

    public class GroupManagementVM
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public string MakerStatus { get; set; }
        public bool Active { get; set; }
        public string Action { get; set; }
        public string Reason { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        /// <summary>
        /// Group Checker
        /// </summary>
        /// 
        [NotMapped]
        public int GroupCheckerID { get; set; }
        public string CheckerStatus { get; set; }

        [NotMapped]
        public bool CheckerActive { get; set; }
        public string MakerID { get; set; }
        public DateTime MakerDate { get; set; }
        public string CheckerID { get; set; }
        public string CountryCode { get; set; }
        public DateTime CheckerDate { get; set; }

        public string Reference { get; set; }
        public string GroupOwnerPSID { get; set; }
        public string GroupOwnerName { get; set; }

        public string PSID { get; set; } // login user id it is

    }
}
