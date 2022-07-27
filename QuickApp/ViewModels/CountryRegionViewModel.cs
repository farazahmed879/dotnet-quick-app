using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickApp.ViewModels
{


    public class CountryRegionUserGroupVM {

        public CountryRegionUserGroupVM()
        {
            ListRegionViewModel = new List<RegionViewModel>();
            ListCountryViewModel = new List<CountryViewModel>();
            ListGroupViewModel = new List<GroupViewModel>();
        }

        public List<RegionViewModel> ListRegionViewModel { get; set; }
        public List<CountryViewModel> ListCountryViewModel { get; set; }
        public List<GroupViewModel> ListGroupViewModel { get; set; }
    }

    public class RegionViewModel
    {
        public int Id { get; set; }
        public string RegionID { get; set; }
        public string RegionName { get; set; }
    }
    public class CountryViewModel
    {
        public int Id { get; set; }
        public string CountryCode { get; set; }

        public string CountryName { get; set; }
    }

    public class GroupViewModel
    {
        public string GROUP_ID { get; set; }
        public string GROUP_NAME { get; set; }
    }
}
