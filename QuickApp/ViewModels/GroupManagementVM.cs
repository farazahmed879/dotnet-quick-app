using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace QuickApp.ViewModels
{

    public class GroupManagementVM
    {
        public List<ModuleVM> ModuleVMList { get; set; }

        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_1 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_2 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_3 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_4 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_5 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_6 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_7 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_8 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_9 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_10 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_11 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_12 { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID_13 { get; set; }
        

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

    public class GroupList {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
    }
}
