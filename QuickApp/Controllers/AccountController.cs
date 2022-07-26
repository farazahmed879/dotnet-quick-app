using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using QuickApp.ViewModels;
using AutoMapper;
using DAL.Models;
using DAL.Core.Interfaces;
using QuickApp.Authorization;
using QuickApp.Helpers;
using Microsoft.AspNetCore.JsonPatch;
using DAL.Core;
using IdentityServer4.AccessTokenValidation;
using DAL;
using Microsoft.Data.SqlClient;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Options;

namespace QuickApp.Controllers
{
    [Authorize(AuthenticationSchemes = IdentityServerAuthenticationDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IAccountManager _accountManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<AccountController> _logger;
        private const string GetUserByIdActionName = "GetUserById";
        private const string GetRoleByIdActionName = "GetRoleById";
        private readonly ApplicationDbContext _context;
        private readonly DomainConfig _config;
        public string DomainName { get; set; }
        public AccountController(IMapper mapper, IAccountManager accountManager, IAuthorizationService authorizationService,
            ILogger<AccountController> logger, ApplicationDbContext context, IOptions<AppSettings> config)
        {
            _context = context;
            _mapper = mapper;
            _accountManager = accountManager;
            _authorizationService = authorizationService;
            _logger = logger;
            _config = config.Value.DomainConfig;
            //DomainName = _config.Name;
        }


        [HttpGet("users/me")]
        [ProducesResponseType(200, Type = typeof(UserViewModel))]
        public async Task<IActionResult> GetCurrentUser()
        {
            return await GetUserById(Utilities.GetUserId(this.User));
        }


        [HttpGet("users/{id}", Name = GetUserByIdActionName)]
        [ProducesResponseType(200, Type = typeof(UserViewModel))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Read)).Succeeded)
                return new ChallengeResult();


            UserViewModel userVM = await GetUserViewModelHelper(id);

            if (userVM != null)
                return Ok(userVM);
            else
                return NotFound(id);
        }


        [HttpGet("users/username/{userName}")]
        [ProducesResponseType(200, Type = typeof(UserViewModel))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserByUserName(string userName)
        {
            ApplicationUser appUser = await _accountManager.GetUserByUserNameAsync(userName);

            if (!(await _authorizationService.AuthorizeAsync(this.User, appUser?.Id ?? "", AccountManagementOperations.Read)).Succeeded)
                return new ChallengeResult();

            if (appUser == null)
                return NotFound(userName);

            return await GetUserById(appUser.Id);
        }


        [HttpGet("users")]
        [Authorize(Authorization.Policies.ViewAllUsersPolicy)]
        [ProducesResponseType(200, Type = typeof(List<UserViewModel>))]
        public async Task<IActionResult> GetUsers()
        {
            return await GetUsers(-1, -1);
        }


        [HttpGet("users/{pageNumber:int}/{pageSize:int}")]
        [Authorize(Authorization.Policies.ViewAllUsersPolicy)]
        [ProducesResponseType(200, Type = typeof(List<UserViewModel>))]
        public async Task<IActionResult> GetUsers(int pageNumber, int pageSize)
        {
            var usersAndRoles = await _accountManager.GetUsersAndRolesAsync(pageNumber, pageSize);

            List<UserViewModel> usersVM = new List<UserViewModel>();

            foreach (var item in usersAndRoles)
            {
                var userVM = _mapper.Map<UserViewModel>(item.User);
                userVM.Roles = item.Roles;

                usersVM.Add(userVM);
            }

            return Ok(usersVM);
        }


        [HttpPut("users/me")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] UserEditViewModel user)
        {
            return await UpdateUser(Utilities.GetUserId(this.User), user);
        }


        [HttpPut("users/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserEditViewModel user)
        {
            ApplicationUser appUser = await _accountManager.GetUserByIdAsync(id);
            string[] currentRoles = appUser != null ? (await _accountManager.GetUserRolesAsync(appUser)).ToArray() : null;

            var manageUsersPolicy = _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Update);
            var assignRolePolicy = _authorizationService.AuthorizeAsync(this.User, (user.Roles, currentRoles), Authorization.Policies.AssignAllowedRolesPolicy);


            if ((await Task.WhenAll(manageUsersPolicy, assignRolePolicy)).Any(r => !r.Succeeded))
                return new ChallengeResult();


            if (ModelState.IsValid)
            {
                if (user == null)
                    return BadRequest($"{nameof(user)} cannot be null");

                if (!string.IsNullOrWhiteSpace(user.Id) && id != user.Id)
                    return BadRequest("Conflicting user id in parameter and model data");

                if (appUser == null)
                    return NotFound(id);

                bool isPasswordChanged = !string.IsNullOrWhiteSpace(user.NewPassword);
                bool isUserNameChanged = !appUser.UserName.Equals(user.UserName, StringComparison.OrdinalIgnoreCase);

                if (Utilities.GetUserId(this.User) == id)
                {
                    if (string.IsNullOrWhiteSpace(user.CurrentPassword))
                    {
                        if (isPasswordChanged)
                            AddError("Current password is required when changing your own password", "Password");

                        if (isUserNameChanged)
                            AddError("Current password is required when changing your own username", "Username");
                    }
                    else if (isPasswordChanged || isUserNameChanged)
                    {
                        if (!await _accountManager.CheckPasswordAsync(appUser, user.CurrentPassword))
                            AddError("The username/password couple is invalid.");
                    }
                }

                if (ModelState.IsValid)
                {
                    _mapper.Map<UserEditViewModel, ApplicationUser>(user, appUser);

                    var result = await _accountManager.UpdateUserAsync(appUser, user.Roles);
                    if (result.Succeeded)
                    {
                        if (isPasswordChanged)
                        {
                            if (!string.IsNullOrWhiteSpace(user.CurrentPassword))
                                result = await _accountManager.UpdatePasswordAsync(appUser, user.CurrentPassword, user.NewPassword);
                            else
                                result = await _accountManager.ResetPasswordAsync(appUser, user.NewPassword);
                        }

                        if (result.Succeeded)
                            return NoContent();
                    }

                    AddError(result.Errors);
                }
            }

            return BadRequest(ModelState);
        }


        [HttpPatch("users/me")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateCurrentUser([FromBody] JsonPatchDocument<UserPatchViewModel> patch)
        {
            return await UpdateUser(Utilities.GetUserId(this.User), patch);
        }


        [HttpPatch("users/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] JsonPatchDocument<UserPatchViewModel> patch)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Update)).Succeeded)
                return new ChallengeResult();


            if (ModelState.IsValid)
            {
                if (patch == null)
                    return BadRequest($"{nameof(patch)} cannot be null");


                ApplicationUser appUser = await _accountManager.GetUserByIdAsync(id);

                if (appUser == null)
                    return NotFound(id);


                UserPatchViewModel userPVM = _mapper.Map<UserPatchViewModel>(appUser);
                patch.ApplyTo(userPVM, (e) => AddError(e.ErrorMessage));

                if (ModelState.IsValid)
                {
                    _mapper.Map<UserPatchViewModel, ApplicationUser>(userPVM, appUser);

                    var result = await _accountManager.UpdateUserAsync(appUser);
                    if (result.Succeeded)
                        return NoContent();


                    AddError(result.Errors);
                }
            }

            return BadRequest(ModelState);
        }


        [HttpPost("users")]
        [Authorize(Authorization.Policies.ManageAllUsersPolicy)]
        [ProducesResponseType(201, Type = typeof(UserViewModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Register([FromBody] UserEditViewModel user)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, (user.Roles, new string[] { }), Authorization.Policies.AssignAllowedRolesPolicy)).Succeeded)
                return new ChallengeResult();


            if (ModelState.IsValid)
            {
                if (user == null)
                    return BadRequest($"{nameof(user)} cannot be null");


                ApplicationUser appUser = _mapper.Map<ApplicationUser>(user);

                var result = await _accountManager.CreateUserAsync(appUser, user.Roles, user.NewPassword);
                if (result.Succeeded)
                {
                    UserViewModel userVM = await GetUserViewModelHelper(appUser.Id);
                    return CreatedAtAction(GetUserByIdActionName, new { id = userVM.Id }, userVM);
                }

                AddError(result.Errors);
            }

            return BadRequest(ModelState);
        }


        [HttpDelete("users/{id}")]
        [ProducesResponseType(200, Type = typeof(UserViewModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Delete)).Succeeded)
                return new ChallengeResult();


            ApplicationUser appUser = await _accountManager.GetUserByIdAsync(id);

            if (appUser == null)
                return NotFound(id);

            if (!await _accountManager.TestCanDeleteUserAsync(id))
                return BadRequest("User cannot be deleted. Delete all orders associated with this user and try again");


            UserViewModel userVM = await GetUserViewModelHelper(appUser.Id);

            var result = await _accountManager.DeleteUserAsync(appUser);
            if (!result.Succeeded)
                throw new Exception("The following errors occurred whilst deleting user: " + string.Join(", ", result.Errors));


            return Ok(userVM);
        }


        [HttpPut("users/unblock/{id}")]
        [Authorize(Authorization.Policies.ManageAllUsersPolicy)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UnblockUser(string id)
        {
            ApplicationUser appUser = await _accountManager.GetUserByIdAsync(id);

            if (appUser == null)
                return NotFound(id);

            appUser.LockoutEnd = null;
            var result = await _accountManager.UpdateUserAsync(appUser);
            if (!result.Succeeded)
                throw new Exception("The following errors occurred whilst unblocking user: " + string.Join(", ", result.Errors));


            return NoContent();
        }


        [HttpGet("users/me/preferences")]
        [ProducesResponseType(200, Type = typeof(string))]
        public async Task<IActionResult> UserPreferences()
        {
            var userId = Utilities.GetUserId(this.User);
            ApplicationUser appUser = await _accountManager.GetUserByIdAsync(userId);

            return Ok(appUser.Configuration);
        }


        [HttpPut("users/me/preferences")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> UserPreferences([FromBody] string data)
        {
            var userId = Utilities.GetUserId(this.User);
            ApplicationUser appUser = await _accountManager.GetUserByIdAsync(userId);

            appUser.Configuration = data;

            var result = await _accountManager.UpdateUserAsync(appUser);
            if (!result.Succeeded)
                throw new Exception("The following errors occurred whilst updating User Configurations: " + string.Join(", ", result.Errors));

            return NoContent();
        }





        [HttpGet("roles/{id}", Name = GetRoleByIdActionName)]
        [ProducesResponseType(200, Type = typeof(RoleViewModel))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var appRole = await _accountManager.GetRoleByIdAsync(id);

            if (!(await _authorizationService.AuthorizeAsync(this.User, appRole?.Name ?? "", Authorization.Policies.ViewRoleByRoleNamePolicy)).Succeeded)
                return new ChallengeResult();

            if (appRole == null)
                return NotFound(id);

            return await GetRoleByName(appRole.Name);
        }


        [HttpGet("roles/name/{name}")]
        [ProducesResponseType(200, Type = typeof(RoleViewModel))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRoleByName(string name)
        {
            if (!(await _authorizationService.AuthorizeAsync(this.User, name, Authorization.Policies.ViewRoleByRoleNamePolicy)).Succeeded)
                return new ChallengeResult();


            RoleViewModel roleVM = await GetRoleViewModelHelper(name);

            if (roleVM == null)
                return NotFound(name);

            return Ok(roleVM);
        }


        [AllowAnonymous]
        [HttpGet("domains")]
        [ProducesResponseType(200, Type = typeof(List<RoleViewModel>))]
        [ProducesResponseType(200, Type = typeof(List<DomainViewModel>))]
        public async Task<IActionResult> GetDomains()
        {
            string[] Domain = _config.Name.ToString().Split(',');
            if (Domain.Length > 0)
            {

                List<DomainViewModel> usersVM = new List<DomainViewModel>();
                foreach (var item in Domain)
                {
                    var DomainViewModelObj = new DomainViewModel
                    {
                        Name = item
                    };
                    usersVM.Add(DomainViewModelObj);
                }
                return Ok(usersVM);
            }
            else
            {

            }
            return null;
        }




        [HttpGet("GroupManagementGridData")]  //"GroupManagementGridData/{ModuleID:int?}/{ApplicationID:int?}"
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(List<RolePermissionVM>))]
        public async Task<IActionResult> GetGroupManagementGridData() //int? ModuleID, int? ApplicationID
        {
            try
            {

                //[AllowAnonymous]
                //[HttpGet("GetCountryRegionUserGroup")]
                //[ProducesResponseType(200, Type = typeof(List<CountryRegionUserGroupVM>))]

                //public async Task<IActionResult> GetCountryRegionUserGroup()
                //{

                ViewModels.RolePermissionVM rolePermissionVM = new ViewModels.RolePermissionVM();
                var getCountrys = _context.Set<TBL_Country>().FromSqlRaw("sp_get_Country").ToList();
                var getSP_Fill_Pages_By_ModuleID = _context.Set<DAL.Models.Module>().FromSqlInterpolated($"exec SP_Fill_Modules_By_ModuleID {null}, {null}").ToList();
                var getSP_Fill_Pages_By_ModuleID1 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {1}").ToList();
                var getSP_Fill_Pages_By_ModuleID2 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {2}").ToList();
                var getSP_Fill_Pages_By_ModuleID3 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {3}").ToList();
                var getSP_Fill_Pages_By_ModuleID4 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {4}").ToList();
                var getSP_Fill_Pages_By_ModuleID5 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {5}").ToList();
                var getSP_Fill_Pages_By_ModuleID6 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {6}").ToList();
                var getSP_Fill_Pages_By_ModuleID7 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {7}").ToList();
                var getSP_Fill_Pages_By_ModuleID8 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {8}").ToList();
                var getSP_Fill_Pages_By_ModuleID9 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {9}").ToList();
                var getSP_Fill_Pages_By_ModuleID10 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {10}").ToList();
                var getSP_Fill_Pages_By_ModuleID11 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {11}").ToList();
                var getSP_Fill_Pages_By_ModuleID12 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {12}").ToList();
                var getSP_Fill_Pages_By_ModuleID13 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {13}").ToList();
                rolePermissionVM.countryList = _mapper.Map<List<CountryViewModel>>(getCountrys);
                rolePermissionVM.moduleList = _mapper.Map<List<ModuleVM>>(getSP_Fill_Pages_By_ModuleID);
                rolePermissionVM.Fill_Modules_By_ModuleID_1= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID1);
                rolePermissionVM.Fill_Modules_By_ModuleID_2= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID2);
                rolePermissionVM.Fill_Modules_By_ModuleID_3= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID3);
                rolePermissionVM.Fill_Modules_By_ModuleID_4= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID4);
                rolePermissionVM.Fill_Modules_By_ModuleID_5= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID5);
                rolePermissionVM.Fill_Modules_By_ModuleID_6= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID6);
                rolePermissionVM.Fill_Modules_By_ModuleID_7= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID7);
                rolePermissionVM.Fill_Modules_By_ModuleID_8= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID8);
                rolePermissionVM.Fill_Modules_By_ModuleID_9= _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID9);
                rolePermissionVM.Fill_Modules_By_ModuleID_10 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID10);
                rolePermissionVM.Fill_Modules_By_ModuleID_11 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID11);
                rolePermissionVM.Fill_Modules_By_ModuleID_12 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID12);
                rolePermissionVM.Fill_Modules_By_ModuleID_13 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID13);
                return Ok(rolePermissionVM);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        [HttpGet("UserManagementGridData/{UserID:int?}/{StatusID:int?}")]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(List<GridUserManagementVM>))]
        public async Task<IActionResult> GetUserManagementGridData(int? UserID, int? StatusID)
        {
            try
            {
                var StatusId = Convert.ToString(StatusID);
                StatusId = StatusId == "" ? null : "";
                int? userID = null;
                string statusID = null;
                int? UserCheckerID = null;
                //var getCheckers = _context.Set<GridUserAuthorization>().FromSqlInterpolated($"exec SP_Fill_User_By_UserCheckerID_Checker {UserCheckerID};").ToList();
                var getMakers = _context.Set<GridUserManagementVM>().FromSqlInterpolated($"exec SP_Fill_User_By_UserID_Maker {UserID}, {StatusId}").ToList();
                return Ok(getMakers);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        [AllowAnonymous]
        [HttpGet("GetUserAuthorizationGridData")]
        [ProducesResponseType(200, Type = typeof(List<GridUserAuthorization>))]
        public async Task<IActionResult> GetUserAuthorizationGridData()
        {
            int? userID = null;
            string StatusID = null;
            int? UserCheckerID = null;
            var getCheckers = _context.Set<GridUserAuthorization>().FromSqlInterpolated($"exec SP_Fill_User_By_UserCheckerID_Checker {UserCheckerID};").ToList();
            //var getMakers = _context.Set<GridUserManagementVM>().FromSqlInterpolated($"exec SP_Fill_User_By_UserID_Maker {userID}, {StatusID}").ToList();
            return Ok(getCheckers);
        }




        [AllowAnonymous]
        [HttpGet("GetCountryRegionUserGroup")]
        [ProducesResponseType(200, Type = typeof(List<CountryRegionUserGroupVM>))]

        public async Task<IActionResult> GetCountryRegionUserGroup()
        {
            CountryRegionUserGroupVM countryRegionUserGroupVM = new CountryRegionUserGroupVM();
            var getRegions = _context.Set<TBL_LOCATION>().FromSqlRaw("SP_Get_Region_DropDown").ToList();
            var getCountrys = _context.Set<TBL_Country>().FromSqlRaw("sp_get_Country").ToList();
            var getGroups = _context.Set<TBL_GROUP>().FromSqlRaw("SP_Get_Group_DropDown").ToList();

            var result = _mapper.Map<List<RegionViewModel>>(getRegions);
            var result1 = _mapper.Map<List<CountryViewModel>>(getCountrys);
            var result2 = _mapper.Map<List<GroupViewModel>>(getGroups);

            countryRegionUserGroupVM.ListCountryViewModel = result1;
            countryRegionUserGroupVM.ListRegionViewModel = result;
            countryRegionUserGroupVM.ListGroupViewModel = result2;

            return Ok(countryRegionUserGroupVM);

        }

        //[HttpGet("users/me/preferences")]
        //[ProducesResponseType(200, Type = typeof(string))]
        //public async Task<IActionResult> UserPreferences()
        //{
        //    var userId = Utilities.GetUserId(this.User);
        //    ApplicationUser appUser = await _accountManager.GetUserByIdAsync(userId);

        //    return Ok(appUser.Configuration);
        //}

        //[HttpGet("users")]
        //[Authorize(Authorization.Policies.ViewAllUsersPolicy)]
        //[ProducesResponseType(200, Type = typeof(List<UserViewModel>))]
        //public async Task<IActionResult> GetUsers()
        //{
        //    return await GetUsers(-1, -1);
        //}


        [HttpGet("roles")]
        [Authorize(Authorization.Policies.ViewAllRolesPolicy)]
        [ProducesResponseType(200, Type = typeof(List<RoleViewModel>))]
        public async Task<IActionResult> GetRoles()
        {
            string[] Domain = _config.Name.ToString().Split(',');
            return await GetRoles(-1, -1);
        }


        [HttpGet("roles/{pageNumber:int}/{pageSize:int}")]
        [Authorize(Authorization.Policies.ViewAllRolesPolicy)]
        [ProducesResponseType(200, Type = typeof(List<RoleViewModel>))]
        public async Task<IActionResult> GetRoles(int pageNumber, int pageSize)
        {
            try
            {
                var roles = await _accountManager.GetRolesLoadRelatedAsync(pageNumber, pageSize);

                #region SPs work

                //var getRegions = _context.Set<TBL_LOCATION>().FromSqlRaw("SP_Get_Region_DropDown").ToList();
                //var getCountrys = _context.Set<TBL_Country>().FromSqlRaw("sp_get_Country").ToList();
                //var getGroups = _context.Set<TBL_GROUP>().FromSqlRaw("SP_Get_Group_DropDown").ToList();

                //int? userID = null;
                //string StatusID = null;
                //int? UserCheckerID = null;
                //var getCheckers = _context.Set<GridUserAuthorization>().FromSqlInterpolated($"exec SP_Fill_User_By_UserCheckerID_Checker {UserCheckerID};").ToList();
                //var getMakers = _context.Set<GridUserManagementVM>().FromSqlInterpolated($"exec SP_Fill_User_By_UserID_Maker {userID}, {StatusID}").ToList();


                //int month = 02;
                //int year = 2018;
                //var x = ctx.TruckRentalPb.FromSqlInterpolated($"exec SP_Fill_User_By_UserID_Maker {userID}, {StatusID};").ToList();

                //var result = _mapper.Map<List<RegionViewModel>>(getRegions);
                //var result1 = _mapper.Map<List<CountryViewModel>>(getCountrys);
                //var result2 = _mapper.Map<List<GroupViewModel>>(getGroups);

                //var result = InsertData();

                #endregion

                #region insert work
                //InsertData();
                #endregion

                #region upadte Data
                //UpdateData();
                #endregion

                return Ok(_mapper.Map<List<RoleViewModel>>(roles));
            }
            catch (Exception ex)
            {

                throw ex;
            }



            //var lst = await _context.Set Customers.AsQueryable<CountryRegionViewModel>()
            //    .FromSql($"EXEC Sp_reportsection3comp")
            //    .ToListAsync();

            //var lst = await _context.Customers.fro
            //var lst = await _context.Customers.from from .ra Query<CountryRegionViewModel>()
            //    .FromSql($"EXEC SP_Get_Region_DropDown")
            //    .ToListAsync();

            //var getCountry = _context.Quer

            //using (var conn = new SqlConnection(_connectionString.Value))
            //{
            //    var procedure = "[SP_Registration]";
            //    var values = new
            //    {
            //        FirstName = Model.FirstName,
            //        LastName = Model.LastName,
            //        Email = Model.Email,
            //        Password = Model.PasswordHash,
            //        Industry = Model.Industry,
            //        OrgName = Model.CompanyName,
            //        OrgEmpCount = Model.OrganizationEmpRange,
            //        EmpRole = Model.JobTitle,
            //        Country = Model.CountryId,
            //        ActivationCode = Model.ConcurrencyStamp,
            //        Operation = Model.Operation
            //    };
            //    var results = await conn.QueryAsync<AspNetUsers>(procedure, values, commandType: CommandType.StoredProcedure);
            //    Model = results.FirstOrDefault();
            //    return Model;
            //}


        }

        private void InsertData()
        {

            try
            {
                var userIdParam = new SqlParameter("@Id", SqlDbType.Int);
                userIdParam.Direction = ParameterDirection.Output;
                var PSID = new SqlParameter("@PSID", "1233456");
                var Name = new SqlParameter("@Name", "yawar");
                var Department = new SqlParameter("@Department", "ABCD");
                var RegionID = new SqlParameter("@RegionID", "071");
                var GroupID = new SqlParameter("@GroupID", 3);
                var Active = new SqlParameter("@Active", true);
                var Status = new SqlParameter("@Status", "P");
                var Action = new SqlParameter("@Action", "INSERT");
                var CreatedBy = new SqlParameter("@CreatedBy", "1111111");
                var CreatedDate = new SqlParameter("@CreatedDate", DateTime.Now);
                var Signatory = new SqlParameter("@Signatory", "");
                var AuthSignatory = new SqlParameter("@AuthSignatory", true);
                var Reference = new SqlParameter("@Reference", "23498");
                var CountryCode = new SqlParameter("@CountryCode", "PK");
                var AccountType = new SqlParameter("@AccountType", "User");
                var AccountDescription = new SqlParameter("@AccountDescription", "Account Description");
                var PASSWORD = new SqlParameter("@PASSWORD", "");


                var getMakers = _context.Database.ExecuteSqlRaw("exec SP_Insert_User_Maker @PSID,@Name,@Department,@RegionID,@GroupID,@Active,@Status,@Action,@CreatedBy,@CreatedDate,@Signatory,@AuthSignatory,@Reference,@CountryCode,@AccountType,@AccountDescription,@PASSWORD, @Id out", PSID, Name, Department, RegionID, GroupID, Active, Status, Action, CreatedBy, CreatedDate, Signatory, AuthSignatory, Reference, CountryCode, AccountType, AccountDescription, PASSWORD, userIdParam);
                var results = userIdParam.Value;
                //return results;
            }


            //try
            //{
            //    var PSID = new SqlParameter("@PSID", "1233456");
            //    var Name = new SqlParameter("@Name", "yawar");
            //    var Department = new SqlParameter("@Department", "ABCD");
            //    var RegionID = new SqlParameter("@RegionID", "071");
            //    var GroupID = new SqlParameter("@GroupID", 3);
            //    var Active = new SqlParameter("@Active", true);
            //    var Status = new SqlParameter("@Status", "P");
            //    var Action = new SqlParameter("@Action", "INSERT");
            //    var CreatedBy = new SqlParameter("@CreatedBy", "1111111");
            //    var CreatedDate = new SqlParameter("@CreatedDate", DateTime.Now);
            //    var Signatory = new SqlParameter("@Signatory", "");
            //    var AuthSignatory = new SqlParameter("@AuthSignatory", true);
            //    var Reference = new SqlParameter("@Reference", "23498");
            //    var CountryCode = new SqlParameter("@CountryCode", "PK");
            //    var AccountType = new SqlParameter("@AccountType", "User");
            //    var AccountDescription = new SqlParameter("@AccountDescription", "Account Description");
            //    var PASSWORD = new SqlParameter("@PASSWORD", "");


            //    var getMakers = _context.Set<GridUserManagementVM>().FromSqlRaw("exec SP_Insert_User_Maker @PSID,@Name,@Department,@RegionID,@GroupID,@Active,@Status,@Action,@CreatedBy,@CreatedDate,@Signatory,@AuthSignatory,@Reference,@CountryCode,@AccountType,@AccountDescription,@PASSWORD", PSID, Name, Department, RegionID, GroupID, Active, Status, Action, CreatedBy, CreatedDate, Signatory, AuthSignatory, Reference, CountryCode, AccountType, AccountDescription, PASSWORD).ToList();
            //    var results = getMakers.FirstOrDefault();
            //    return results;
            //}
            catch (Exception ex)
            {

                throw ex;
            }


            //return Convert.ToInt32(SqlHelper.ExecuteScalar(trans, CommandType.StoredProcedure, "[SP_Insert_User_Maker]", param));
        }

        private void UpdateData()
        {


            try
            {

                #region update sp
                int rowsAffected;
                var userIdParam = new SqlParameter("@UserIDOutput", SqlDbType.Int);
                userIdParam.Direction = ParameterDirection.Output;
                string sql = "EXEC SP_Update_User_Status_By_UserID_Maker @UserID, @Status,@StatusRequest,@Action,@Reason,@Reference,@UserIDOutput out";



                List<SqlParameter> parms = new List<SqlParameter>
                { 
                    // Create parameters    
                    new SqlParameter { ParameterName = "@UserID", Value = 129 }, // int
                    new SqlParameter { ParameterName = "@Status", Value = "A" },
                    new SqlParameter { ParameterName = "@StatusRequest", Value = "A" },
                    new SqlParameter { ParameterName = "@Action", Value = "action now" },
                    new SqlParameter { ParameterName = "@Reason", Value = "now reason" },
                    new SqlParameter { ParameterName = "@Reference", Value = "23498" },
                    //new SqlParameter { ParameterName = "@UserID",  Direction =  System.Data.ParameterDirection.Output, Size = 50}
                };
                parms.Add(userIdParam);

                rowsAffected = _context.Database.ExecuteSqlRaw(sql, parms.ToArray());
                //int result = Convert.ToInt32(parms[6].Value);


                #endregion

                var results = userIdParam.Value;
                //return results;
            }

            catch (Exception ex)
            {

                throw ex;
            }


            //return Convert.ToInt32(SqlHelper.ExecuteScalar(trans, CommandType.StoredProcedure, "[SP_Insert_User_Maker]", param));


        }

        /// <summary>
        /// Insert User Maker
        /// </summary>
        /// <param name="BLL"></param>
        /// <param name="trans"></param>
        /// <returns></returns>
        //public virtual int Insert_User_Maker(UserManagement_BLL BLL, SqlTransaction trans)
        //{
        //    try
        //    {
        //        SqlParameter[] param = {
        //                           new SqlParameter("@PSID", BLL.PSID),
        //                           new SqlParameter("@Name", BLL.UserName),
        //                           new SqlParameter("@Department", BLL.Department),
        //                           new SqlParameter("@RegionID", BLL.RegionID),
        //                           new SqlParameter("@GroupID", BLL.GroupID),
        //                           new SqlParameter("@Active", BLL.Active),
        //                           new SqlParameter("@Status", BLL.MakerStatus),
        //                           new SqlParameter("@Action", BLL.Action),
        //                           new SqlParameter("@CreatedBy", BLL.CreatedBy),
        //                           new SqlParameter("@CreatedDate", BLL.CreatedDate),
        //                           new SqlParameter("@Signatory", BLL.Signatory),
        //                           new SqlParameter("@AuthSignatory", BLL.AuthSignatory),
        //                           new SqlParameter("@Reference", BLL.Reference),
        //                           new SqlParameter("@CountryCode", BLL.CountryCode),
        //                           new SqlParameter("@AccountType", BLL.AccountType),
        //                           new SqlParameter("@AccountDescription", BLL.AccountDescription),
        //                           new SqlParameter("@PASSWORD", BLL.Password)
        //                       };
        //        return Convert.ToInt32(SqlHelper.ExecuteScalar(trans, CommandType.StoredProcedure, "[SP_Insert_User_Maker]", param));
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}



        /// <summary>
        /// Insert User Checker
        /// </summary>
        /// <param name="BLL"></param>
        /// <returns></returns>
        //public virtual int Insert_User_Checker(UserManagement_BLL BLL)
        //{
        //    try
        //    {
        //        SqlParameter[] param = {
        //                           new SqlParameter("@UserID", BLL.UserID),
        //                           new SqlParameter("@PSID", BLL.PSID),
        //                           new SqlParameter("@Name", BLL.UserName),
        //                           new SqlParameter("@Department", BLL.Department),
        //                           new SqlParameter("@RegionID", BLL.RegionID),
        //                           new SqlParameter("@GroupID", BLL.GroupID),
        //                           new SqlParameter("@MakerStatus", BLL.MakerStatus),
        //                           new SqlParameter("@Action", BLL.Action),
        //                           new SqlParameter("@MakerID", BLL.MakerID),
        //                           new SqlParameter("@MakerDate", BLL.MakerDate),
        //                           new SqlParameter("@CheckerActive", BLL.CheckerActive),
        //                           new SqlParameter("@Reference", BLL.Reference),
        //                           new SqlParameter("@CountryCode", BLL.CountryCode)

        //                       };
        //        return Convert.ToInt32(SqlHelper.ExecuteScalar(DBConnectionString.VGSS, CommandType.StoredProcedure, "[SP_Insert_User_Checker]", param));
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        [HttpPut("roles/{id}")]
        [Authorize(Authorization.Policies.ManageAllRolesPolicy)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleViewModel role)
        {
            if (ModelState.IsValid)
            {
                if (role == null)
                    return BadRequest($"{nameof(role)} cannot be null");

                if (!string.IsNullOrWhiteSpace(role.Id) && id != role.Id)
                    return BadRequest("Conflicting role id in parameter and model data");



                ApplicationRole appRole = await _accountManager.GetRoleByIdAsync(id);

                if (appRole == null)
                    return NotFound(id);


                _mapper.Map<RoleViewModel, ApplicationRole>(role, appRole);

                var result = await _accountManager.UpdateRoleAsync(appRole, role.Permissions?.Select(p => p.Value).ToArray());
                if (result.Succeeded)
                    return NoContent();

                AddError(result.Errors);

            }

            return BadRequest(ModelState);
        }


        [HttpPost("roles")]
        [Authorize(Authorization.Policies.ManageAllRolesPolicy)]
        [ProducesResponseType(201, Type = typeof(RoleViewModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateRole([FromBody] RoleViewModel role)
        {
            if (ModelState.IsValid)
            {
                if (role == null)
                    return BadRequest($"{nameof(role)} cannot be null");


                ApplicationRole appRole = _mapper.Map<ApplicationRole>(role);

                var result = await _accountManager.CreateRoleAsync(appRole, role.Permissions?.Select(p => p.Value).ToArray());
                if (result.Succeeded)
                {
                    RoleViewModel roleVM = await GetRoleViewModelHelper(appRole.Name);
                    return CreatedAtAction(GetRoleByIdActionName, new { id = roleVM.Id }, roleVM);
                }

                AddError(result.Errors);
            }

            return BadRequest(ModelState);
        }


        [HttpDelete("roles/{id}")]
        [Authorize(Authorization.Policies.ManageAllRolesPolicy)]
        [ProducesResponseType(200, Type = typeof(RoleViewModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteRole(string id)
        {
            ApplicationRole appRole = await _accountManager.GetRoleByIdAsync(id);

            if (appRole == null)
                return NotFound(id);

            if (!await _accountManager.TestCanDeleteRoleAsync(id))
                return BadRequest("Role cannot be deleted. Remove all users from this role and try again");


            RoleViewModel roleVM = await GetRoleViewModelHelper(appRole.Name);

            var result = await _accountManager.DeleteRoleAsync(appRole);
            if (!result.Succeeded)
                throw new Exception("The following errors occurred whilst deleting role: " + string.Join(", ", result.Errors));


            return Ok(roleVM);
        }


        [HttpGet("permissions")]
        [Authorize(Authorization.Policies.ViewAllRolesPolicy)]
        [ProducesResponseType(200, Type = typeof(List<PermissionViewModel>))]
        public IActionResult GetAllPermissions()
        {
            return Ok(_mapper.Map<List<PermissionViewModel>>(ApplicationPermissions.AllPermissions));
        }



        private async Task<UserViewModel> GetUserViewModelHelper(string userId)
        {
            var userAndRoles = await _accountManager.GetUserAndRolesAsync(userId);
            if (userAndRoles == null)
                return null;

            var userVM = _mapper.Map<UserViewModel>(userAndRoles.Value.User);
            userVM.Roles = userAndRoles.Value.Roles;

            return userVM;
        }


        private async Task<RoleViewModel> GetRoleViewModelHelper(string roleName)
        {
            var role = await _accountManager.GetRoleLoadRelatedAsync(roleName);
            if (role != null)
                return _mapper.Map<RoleViewModel>(role);


            return null;
        }


        private void AddError(IEnumerable<string> errors, string key = "")
        {
            foreach (var error in errors)
            {
                AddError(error, key);
            }
        }

        private void AddError(string error, string key = "")
        {
            ModelState.AddModelError(key, error);
        }

    }
}
