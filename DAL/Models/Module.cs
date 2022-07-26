using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Module
    {
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
}
