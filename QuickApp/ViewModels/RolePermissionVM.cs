using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickApp.ViewModels
{
    public class RolePermissionVM
    {
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

    }

    public class TBL_PagesVM  //SP_Fill_Pages_By_ModuleID
    {
        public int PageDetailsID { get; set; }
        public int PageID { get; set; }
        public int ModuleID { get; set; }
    }
}
