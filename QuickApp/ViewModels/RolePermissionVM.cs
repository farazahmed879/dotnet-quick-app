using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickApp.ViewModels
{
    public class RolePermissionVM
    {
        public GroupManagementVM GroupManagementVM { get; set; }
        
        public List<CountryViewModel> countryList { get; set; }

        public List<ModuleVM> moduleList { get; set; }
        public List<TBL_PagesVM> Fill_Modules_By_ModuleID { get; set; }
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
    }

    public class ModuleVM
    {   //SP_Fill_Modules_By_ModuleID

        public int ModuleID { get; set; }
        public int? ApplicationID { get; set; }
        public string? ModuleName { get; set; }
        public string? ModuleDescription { get; set; }
        public string? ModuleIcon { get; set; }
        public int? ModuleSortOrder { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool BackColor { get; set; }// if true of per row then read out all of their values 

        public bool? chkAllSelect { get; set; }
        public bool ChkView { get; set; } 
        public bool ChkInsert { get; set; }
        public bool ChkUpdate { get; set; }
        public bool ChkAuthorize { get; set; }
        public bool ChkReject { get; set; }
        public bool ChkDelete { get; set; }
        public bool Active { get; set; }
    }

    public class TBL_PagesVM  //SP_Fill_Pages_By_ModuleID
    {
        public int PageDetailsID { get; set; }
        public int PageID { get; set; }
        public int ModuleID { get; set; }
        public int ModuleSortOrder { get; set; }
        public string ModuleIcon { get; set; }
        public string PageName { get; set; }
        public string PageDescription { get; set; }
        public string PageUrl { get; set; }
        public string PageIcon { get; set; }
        public int PageSortOrder { get; set; }
        public bool Active { get; set; } 
        public bool Crud_View { get; set; } 
        public bool Crud_Insert { get; set; } 
        public bool Crud_Update { get; set; } 
        public bool Crud_Authorize { get; set; } 
        public bool Crud_Reject { get; set; }
        public bool Crud_Delete { get; set; }

        public int? ApplicationID { get; set; }

        public bool BackColor { get; set; } = false; // if true of per row then read out all of their values 
        public bool chkAllSelect { get; set; }
    }

    public class RolePermission
    {
        public int ModulePermissionsID { get; set; }
        public int PermissionID { get; set; }
        public int PageID { get; set; }
        public int RoleID { get; set; }
        public int UserID { get; set; }
        public int ModuleID { get; set; }
        public bool Can_View { get; set; }
        public bool Can_Insert { get; set; }
        public bool Can_Update { get; set; }
        public bool Can_Authorize { get; set; }
        public bool Can_Reject { get; set; }
        public bool Can_Delete { get; set; }
        public bool Active { get; set; }

    }
}
