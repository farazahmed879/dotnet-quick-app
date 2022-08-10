




using FluentValidation;
using QuickApp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace QuickApp.ViewModels
{
    public class UserViewModel : UserBaseViewModel
    {
        public bool IsLockedOut { get; set; }

        [MinimumCount(1, ErrorMessage = "Roles cannot be empty")]
        public string[] Roles { get; set; }
    }



    public class UserEditViewModel : UserBaseViewModel
    {
        public string CurrentPassword { get; set; }

        [MinLength(6, ErrorMessage = "New Password must be at least 6 characters")]
        public string NewPassword { get; set; }

        [MinimumCount(1, ErrorMessage = "Roles cannot be empty")]
        public string[] Roles { get; set; }

        
    }



    public class UserPatchViewModel
    {
        public string FullName { get; set; }

        public string JobTitle { get; set; }

        public string PhoneNumber { get; set; }

        public string Configuration { get; set; }
    }



    public abstract class UserBaseViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Username is required"), StringLength(200, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 200 characters")]
        public string UserName { get; set; }

        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required"), StringLength(200, ErrorMessage = "Email must be at most 200 characters"), EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        public string JobTitle { get; set; }

        public string PhoneNumber { get; set; }

        public string Configuration { get; set; }

        public bool IsEnabled { get; set; }

        public string Department { get; set; }
        public string AccountOwner { get; set; }

        public string Country { get; set; }

        public string Region { get; set; }
        public string Group { get; set; }

        public string PSID { get; set; }
        public string RegionID { get; set; }       
        public int GroupID { get; set; }
        public bool Active { get; set; }

        public string Signatory { get; set; }        
        public bool AuthSignatory { get; set; }

        public string Reference { get; set; }
        public string CountryCode { get; set; }
        public string AccountType { get; set; }
        public string AccountDescription { get; set; }
        public string Password { get; set; }

        public string MakerStatus { get; set; }
        public string Action { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        

        public int UserID { get; set; }
        public string MakerID { get; set; }

        public DateTime? MakerDate { get; set; }

        public bool CheckerActive { get; set; }

        public string Reason { get; set; }
    }




    //public class UserViewModelValidator : AbstractValidator<UserViewModel>
    //{
    //    public UserViewModelValidator()
    //    {
    //        //Validation logic here
    //        RuleFor(user => user.UserName).NotEmpty().WithMessage("Username cannot be empty");
    //        RuleFor(user => user.Email).EmailAddress().NotEmpty();
    //        RuleFor(user => user.Password).NotEmpty().WithMessage("Password cannot be empty").Length(4, 20);
    //    }
    //}
}
