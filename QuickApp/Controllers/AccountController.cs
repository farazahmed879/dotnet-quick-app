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
using QuickApp.Common;
using Microsoft.EntityFrameworkCore.Storage;

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
        private static SCB_Session SessionBO;
        public string DomainName { get; set; }
        public static DataTable DtPagesPermissionList = new DataTable();
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

        [AllowAnonymous]
        [HttpGet("users/me")]
        [ProducesResponseType(200, Type = typeof(UserViewModel))]
        public async Task<IActionResult> GetCurrentUser()
        {
            return await GetUserById(Utilities.GetUserId(this.User));
        }

        [AllowAnonymous]
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
        [AllowAnonymous]
        //[Authorize(Authorization.Policies.ViewAllUsersPolicy)]
        [ProducesResponseType(200, Type = typeof(List<UserViewModel>))]
        public async Task<IActionResult> GetUsers()
        {
            return await GetUsers(-1, -1);
        }

        [AllowAnonymous]
        [HttpGet("users/{pageNumber:int}/{pageSize:int}")]
        //[Authorize(Authorization.Policies.ViewAllUsersPolicy)]

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
        [AllowAnonymous]
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


        [HttpPatch("group/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGroup(string id, [FromBody] JsonPatchDocument<GroupManagementVM> patch)
        {

            //if (!(await _authorizationService.AuthorizeAsync(this.User, id, AccountManagementOperations.Update)).Succeeded)
            //    return new ChallengeResult();


            //if (ModelState.IsValid)
            //{
            //    if (patch == null)
            //        return BadRequest($"{nameof(patch)} cannot be null");


            //    ApplicationUser appUser = await _accountManager.GetUserByIdAsync(id);

            //    if (appUser == null)
            //        return NotFound(id);


            //    UserPatchViewModel userPVM = _mapper.Map<UserPatchViewModel>(appUser);
            //    patch.ApplyTo(userPVM, (e) => AddError(e.ErrorMessage));

            //    if (ModelState.IsValid)
            //    {
            //        _mapper.Map<UserPatchViewModel, ApplicationUser>(userPVM, appUser);

            //        var result = await _accountManager.UpdateUserAsync(appUser);
            //        if (result.Succeeded)
            //            return NoContent();


            //        AddError(result.Errors);
            //    }
            //}

            //return BadRequest(ModelState);

            return null;
        }


        [HttpPost("users")]
        //[Authorize(Authorization.Policies.ManageAllUsersPolicy)]
        [AllowAnonymous]
        [ProducesResponseType(201, Type = typeof(UserViewModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Register([FromBody] UserEditViewModel user)
        {
            //if (!(await _authorizationService.AuthorizeAsync(this.User, (user.Roles, new string[] { }), Authorization.Policies.AssignAllowedRolesPolicy)).Succeeded)
            //    return new ChallengeResult();


            if (ModelState.IsValid)
            {
                if (user == null)
                    return BadRequest($"{nameof(user)} cannot be null");


                ApplicationUser appUser = _mapper.Map<ApplicationUser>(user);


                #region save user according to VGSS

                if (user.AccountType == "Generic")
                {
                    //user.Password = EncryptDecrypt.Encrypt(txtPassword.Text); NEED TO DO
                }
                else
                {
                    user.Password = "";
                }

                var isSaveUser = Save(0, user); //0 for it is new user


                #endregion

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

        private bool Save(int UserID, UserEditViewModel user)
        {
            //using var transaction = _context.Database.BeginTransaction();
            using (IDbContextTransaction transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var result = PSIDExists(null, user.PSID);

                    if (result.Count > 0)
                    {
                        if (UserID == 0) //
                        {
                            DateTime Date = DateTime.Now;
                            /// User Maker
                            user.MakerStatus = "P";
                            user.Action = "INSERT";
                            user.CreatedBy = user.PSID; // login user id 
                            user.CreatedDate = Date;
                            var UserIDD = Insert_User_Maker(user);

                            //throw new Exception();

                            /// User Checker
                            user.UserID = int.Parse(UserIDD.ToString());
                            user.MakerID = user.PSID;
                            user.MakerDate = Date;
                            user.CheckerActive = false;
                            var checkerResult = Insert_User_Checker(user);
                        }
                        else
                        {
                            ///// User Maker
                            //user.UserID = UserID;
                            //user.MakerStatus = "U";
                            //user.Action = "UPDATE";
                            //user.Reason = null;
                            //UM.Update_User_Status_By_UserID_Maker(UM.UserID, null, UM.MakerStatus, UM.Action, UM.Reason, UM.Reference, trans);

                            ///// User Checker
                            //UM.MakerID = SessionBO.PSID;
                            //UM.MakerDate = DateTime.Now;
                            //UM.CheckerActive = false;
                            //UM.Insert_User_Checker(UM, trans);
                        }
                    }
                    transaction.Commit();
                    return true;


                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }


            return false;
        }

        [HttpPost("SaveGroup")]
        //[Authorize(Authorization.Policies.ManageAllUsersPolicy)]
        [ProducesResponseType(201, Type = typeof(GroupManagementVM))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RegisterGroup([FromBody] GroupManagementVM group)
        {
            //SqlTransaction trans = con.BeginTransaction();
            try
            {
                if (ModelState.IsValid)
                {
                    if (group == null)
                        return BadRequest($"{nameof(group)} cannot be null");

                    // need work here
                    if (group.GroupID == 0) // means it create on first time
                    {
                        DateTime Date = DateTime.Now;
                        group.MakerStatus = "P";
                        group.Action = "INSERT";
                        group.CreatedBy = "1111111"; //group.PSID;  //login userid need to pull from session right now able to pass 
                        group.CreatedDate = Date;
                        var GroupID = Insert_Group_Maker(group);

                        string GroupIDNEW = GroupID.ToString();
                        //hddGroupID.Value = Convert.ToString(GroupID);
                        /// Group Checker
                        //GM.GroupID = GroupID;
                        group.MakerID = "1111111";
                        group.MakerDate = Date;

                        group.CheckerActive = false;
                        var result = Insert_Group_Checker(group);

                        #region save role permission

                        List<SavePagesVM> savePagesColor = new List<SavePagesVM>();

                        foreach (var item in group.ModuleVMList)
                        {
                            if (item.BackColor)
                            {
                                var moduleId = item.ModuleID;
                                var RoleID = GroupIDNEW != "" && GroupIDNEW != null ? Convert.ToInt32(GroupIDNEW) : 0;
                                #region save pages

                                if (moduleId == 1)
                                {

                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_1)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()


                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 2)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_2)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 3)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_3)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 4)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_4)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 5)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_5)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 6)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_6)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 7)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_7)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 8)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_8)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 9)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_9)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 10)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_10)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 11)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_11)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 12)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_12)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }
                                if (moduleId == 13)
                                {
                                    savePagesColor.Clear();
                                    foreach (var itemData in group.Fill_Modules_By_ModuleID_13)
                                    {
                                        if (itemData.BackColor)
                                        {
                                            var savePagesVM = new SavePagesVM
                                            {
                                                PageID = Convert.ToString(itemData.PageID),
                                                canView = itemData.Crud_View == true ? true : false,
                                                canInsert = itemData.Crud_Insert == true ? true : false,
                                                canUpdate = itemData.Crud_Update == true ? true : false,
                                                canAuthorize = itemData.Crud_Authorize == true ? true : false,
                                                canReject = itemData.Crud_Reject == true ? true : false,
                                                canDelete = itemData.Crud_Delete == true ? true : false,
                                                RoleID = RoleID,
                                                ModuleID = itemData.ModuleID.ToString()

                                            };
                                            savePagesColor.Add(savePagesVM);
                                        }
                                    }

                                    foreach (var itemRow in savePagesColor)
                                    {
                                        Insert_Page_Permission_Color(Convert.ToInt32(itemRow.RoleID), Convert.ToInt32(itemRow.ModuleID), Convert.ToInt32(itemRow.PageID), itemRow.canView, itemRow.canInsert, itemRow.canUpdate, itemRow.canAuthorize, itemRow.canReject, itemRow.canDelete);
                                    }
                                }


                                #endregion

                            }
                        }

                        InsertUpdateModulePermission(0, group.ModuleVMList);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_1, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_2, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_3, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_4, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_5, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_6, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_7, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_8, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_9, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_10, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_11, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_12, 0);
                        InsertUpdateRolePagePermission(group.Fill_Modules_By_ModuleID_13, 0);

                        #endregion
                    }
                    else
                    {
                        /// Group Maker

                        group.MakerStatus = "U";
                        group.Action = "UPDATE";
                        group.Reason = null;
                        //GM.Update_Group_Status_By_GroupID_Maker(GM.GroupID, GM.MakerStatus, GM.Action, GM.Reason, null, trans);

                        /// Group Checker
                        group.MakerID = "1111111";//SessionBO.PSID;
                        group.MakerDate = DateTime.Now;
                        group.CheckerActive = false;
                        //GM.Insert_Group_Checker(GM, trans);

                    }

                    return BadRequest(ModelState);

                }
            }
            catch (Exception ex)
            {

                throw ex;
            }


            return null;
        }

        [HttpPost("SaveGroupName")]
        //[Authorize(Authorization.Policies.ManageAllUsersPolicy)]
        [ProducesResponseType(201, Type = typeof(string))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SaveGroupName(string groupName)
        {
            return null;
        }

        [HttpPost("SaveGrouList")]
        //[Authorize(Authorization.Policies.ManageAllUsersPolicy)]
        [ProducesResponseType(201, Type = typeof(string))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SaveGrouList(GroupList GroupList)
        {
            return null;
        }



        #region InsertUpdateModulePermission
        private void InsertUpdateModulePermission(int GroupID, List<ModuleVM> moduleVMs)
        {


            foreach (var item in moduleVMs)
            {
                if (item.ChkView || item.ChkInsert || item.ChkAuthorize || item.ChkReject || item.ChkUpdate || item.ChkDelete)
                {
                    item.Active = true;
                }
                else
                {
                    item.Active = false;
                }

                Insert_Update_Modules_Permissions_Checker(item);
            }
        }
        #endregion


        #region InsertUpdatePagePermission
        private void InsertUpdateRolePagePermission(List<TBL_PagesVM> Grid, int GroupID)
        {
            for (int i = 0; i < Grid.Count; i++)
            {
                bool chkAllSelect = Grid[i].chkAllSelect;
                bool ChkView = Grid[i].Crud_View;
                bool ChkInsert = Grid[i].Crud_Insert;
                bool ChkUpdate = Grid[i].Crud_Update;
                bool ChkAuthorize = Grid[i].Crud_Authorize;
                bool ChkReject = Grid[i].Crud_Reject;
                bool ChkDelete = Grid[i].Crud_Delete;
                RolePermission rolePermission = new RolePermission();
                if (ChkInsert || ChkAuthorize || ChkReject || ChkUpdate || ChkDelete)
                {
                    rolePermission.Active = true;
                }
                else
                {
                    rolePermission.Active = false;
                }
                //if (chkAllSelect.Checked)
                //{
                //    if (ChkView.Checked || ChkInsert.Checked || ChkAuthorize.Checked || ChkReject.Checked || ChkUpdate.Checked || ChkDelete.Checked)
                //    {
                rolePermission.RoleID = 1063; //RoleID;
                rolePermission.PageID = Grid[i].PageID;
                rolePermission.Can_View = ChkView;
                rolePermission.Can_Insert = ChkInsert;
                rolePermission.Can_Update = ChkUpdate;
                rolePermission.Can_Authorize = ChkAuthorize;
                rolePermission.Can_Reject = ChkReject;
                rolePermission.Can_Delete = ChkDelete;

                Insert_Update_Pages_Permissions_Checker(rolePermission);
                //    }
                //}
            }
        }
        #endregion


        private void Insert_Update_Pages_Permissions_Checker(RolePermission RP)
        {
            SqlParameter[] param ={new SqlParameter("@RoleId",RP.RoleID)
                                 ,new SqlParameter("@UserId",RP.UserID)
                                 ,new SqlParameter("@PageId",RP.PageID)
                                 ,new SqlParameter("@Can_View",RP.Can_View)
                                 ,new SqlParameter("@Can_Insert",RP.Can_Insert)
                                 ,new SqlParameter("@Can_Update",RP.Can_Update)
                                 ,new SqlParameter("@Can_Authorize",RP.Can_Authorize)
                                 ,new SqlParameter("@Can_Reject",RP.Can_Reject)
                                 ,new SqlParameter("@Can_Delete",RP.Can_Delete)
                                 ,new SqlParameter("@Active",RP.Active)
                                 ,new SqlParameter("@CreatedBy",1111111) //Sess.UserID
                                 ,new SqlParameter("@CreatedDate", DateTime.Now)
                                 };
            var getChecker = _context.Database.ExecuteSqlRaw("exec [SP_Insert_Update_Pages_Permissions_Checker]", param);
        }

        private void Insert_Update_Modules_Permissions_Checker(ModuleVM moduleVM)
        {
            SqlParameter[] param1 = {new SqlParameter("@RoleID",57)   // moduleVM.RoleID
                                   ,new SqlParameter("@ModuleID",moduleVM.ModuleID)
                                   ,new SqlParameter("@Can_View",moduleVM.ChkView)
                                   ,new SqlParameter("@Can_Insert",moduleVM.ChkInsert)
                                   ,new SqlParameter("@Can_Update",moduleVM.ChkUpdate)
                                   ,new SqlParameter("@Can_Authorize",moduleVM.ChkAuthorize)
                                   ,new SqlParameter("@Can_Reject",moduleVM.ChkReject)
                                   ,new SqlParameter("@Can_Delete",moduleVM.ChkDelete)
                                   ,new SqlParameter("@Active",moduleVM.Active)
                                   ,new SqlParameter("@CreatedBy",1111111)  // Sess.UserID make it dynamic jo user login he uske id he 
                                   ,new SqlParameter("@CreatedDate",DateTime.Now)
                                   };

            var getChecker = _context.Database.ExecuteSqlRaw("exec [SP_Insert_Update_Modules_Permissions_Checker]", param1);
            //return Convert.ToInt32(SqlHelper.ExecuteScalar(Tran, "[SP_Insert_Update_Modules_Permissions_Checker]", param1));
        }

        private void Insert_Page_Permission_Color(int RoleID, int ModuleID, int PageID, bool canView, bool canInsert, bool canUpdate, bool canAuthorize, bool canReject, bool canDelete)
        {
            //var userIdParam = new SqlParameter("@Id", SqlDbType.Int);
            //userIdParam.Direction = ParameterDirection.Output;

            var RoleID1 = new SqlParameter("@RoleID", RoleID);
            var ModuleID1 = new SqlParameter("@ModuleID", ModuleID);
            var PageID1 = new SqlParameter("@PageID", PageID);
            var canView1 = new SqlParameter("@canView", canView);
            var canInsert1 = new SqlParameter("@canInsert", canInsert);
            var canUpdate1 = new SqlParameter("@canUpdate", canUpdate);
            var canAuthorize1 = new SqlParameter("@canAuthorize", canAuthorize);
            var canReject1 = new SqlParameter("@canReject", canReject);
            var canDelete1 = new SqlParameter("@canDelete", canDelete);

            //SqlHelper.ExecuteNonQuery(Trans, "[SP_PagePermissionColor]", param);
            //var getChecker = _context.Database.ExecuteSqlRaw("exec [SP_PagePermissionColor]", param);
            var Insert_Page_Permission_ColorResult = _context.Database.ExecuteSqlRaw("exec SP_PagePermissionColor @RoleID,@ModuleID,@PageID,@canView,@canInsert,@canUpdate,@canAuthorize,@canReject,@canDelete"
                , RoleID1, ModuleID1, PageID1, canView1, canInsert1, canUpdate1, canAuthorize1, canReject1, canDelete1);
            //var results = userIdParam.Value;
            //var result = userIdParam.Value;
            //return result;
        }

        private object Insert_Group_Checker(GroupManagementVM group)
        {

            var userIdParam = new SqlParameter("@IdentityID", SqlDbType.Int);
            userIdParam.Direction = ParameterDirection.Output;
            var GroupID = new SqlParameter("@GroupID", group.GroupID);
            var GroupName = new SqlParameter("@GroupName", group.GroupName);
            var GroupDescription = new SqlParameter("@GroupDescription", group.GroupDescription);
            var Active = new SqlParameter("@Active", group.Active);
            var MakerStatus = new SqlParameter("@MakerStatus", group.MakerStatus);
            var Action = new SqlParameter("@Action", group.Action);
            var MakerID = new SqlParameter("@MakerID", group.MakerID);
            var MakerDate = new SqlParameter("@MakerDate", group.MakerDate);
            var CheckerActive = new SqlParameter("@CheckerActive", group.CheckerActive);
            var Reference = new SqlParameter("@Reference", group.Reference);
            var CountryCode = new SqlParameter("@CountryCode", group.CountryCode);
            var GroupOwnerPSID = new SqlParameter("@GroupOwnerPSID", group.GroupOwnerPSID);
            var GroupOwnerName = new SqlParameter("@GroupOwnerName", group.GroupOwnerName);

            var getChecker = _context.Database.ExecuteSqlRaw("exec SP_Insert_Group_Checker @GroupID,@GroupName,@GroupDescription,@Active,@MakerStatus,@Action,@MakerID,@MakerDate,@CheckerActive,@Reference,@CountryCode,@GroupOwnerPSID,@GroupOwnerName, @IdentityID out", GroupID, GroupName, GroupDescription, Active, MakerStatus, Action, MakerID, MakerDate, CheckerActive, Reference, CountryCode, GroupOwnerPSID, GroupOwnerName, userIdParam);
            var result = userIdParam.Value;
            return result;
        }

        private object Insert_Group_Maker(GroupManagementVM group)
        {
            try
            {
                var userIdParam = new SqlParameter("@IdentityID", SqlDbType.Int);
                userIdParam.Direction = ParameterDirection.Output;
                var GroupName = new SqlParameter("@GroupName", group.GroupName);
                var GroupDescription = new SqlParameter("@GroupDescription", group.GroupDescription);
                var Active = new SqlParameter("@Active", group.Active);
                var Status = new SqlParameter("@Status", group.MakerStatus);
                var Action = new SqlParameter("@Action", group.Action);
                var CreatedBy = new SqlParameter("@CreatedBy", group.CreatedBy);
                var CreatedDate = new SqlParameter("@CreatedDate", group.CreatedDate);
                var Reference = new SqlParameter("@Reference", group.Reference);
                var CountryCode = new SqlParameter("@CountryCode", group.CountryCode);
                var GroupOwnerPSID = new SqlParameter("@GroupOwnerPSID", group.GroupOwnerPSID);
                var GroupOwnerName = new SqlParameter("@GroupOwnerName", group.GroupOwnerName);

                var getMaker = _context.Database.ExecuteSqlRaw("exec SP_Insert_Group_Maker @GroupName,@GroupDescription,@Active,@Status,@Action,@CreatedBy,@CreatedDate,@Reference,@CountryCode,@GroupOwnerPSID,@GroupOwnerName, @IdentityID out", GroupName, GroupDescription, Active, Status, Action, CreatedBy, CreatedDate, Reference, CountryCode, GroupOwnerPSID, GroupOwnerName, userIdParam);
                var result = userIdParam.Value;
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }

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


        [AllowAnonymous]
        [HttpDelete("groupDelete/{id}")]
        [ProducesResponseType(200, Type = typeof(GroupManagementVM))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteGroup(string id)
        {
            return Ok(true);
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
        [AllowAnonymous]
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





        [HttpGet("GroupAuthorizationGridData/{RoleID}")]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(List<GroupManagementVM>))]
        public async Task<IActionResult> GetGroupAuthorizationGridData(int RoleID)
        {
            try
            {
                var getMakers = _context.Set<GroupManagementViewModel>().FromSqlInterpolated($"exec SP_Get_Pages_Permissions_By_RoleID {RoleID}").ToList();
                return Ok(getMakers);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        private List<PagePermissionVM> Get_Pages_Permissions(int? RoleID, int? UserID)
        {
            try
            {
                var getMakers = _context.Set<PagePermissionVM>().FromSqlInterpolated($"exec SP_Get_Pages_Permissions {RoleID},{UserID}").ToList();
                return getMakers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
                BindAllGrids(rolePermissionVM);
                var getCountrys = _context.Set<TBL_Country>().FromSqlRaw("sp_get_Country").ToList();

                rolePermissionVM.countryList = _mapper.Map<List<CountryViewModel>>(getCountrys);
                return Ok(rolePermissionVM);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private void BindAllGrids(RolePermissionVM rolePermissionVM)
        {
            var getSP_Fill_Pages_By_ModuleID = _context.Set<DAL.Models.Module>().FromSqlInterpolated($"exec SP_Fill_Modules_By_ModuleID {null}, {null}").ToList();
            var getSP_Fill_Pages_By_ModuleID1 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {1}").ToList();
            //getSP_Fill_Pages_By_ModuleID1.All(c => { c.Crud_Insert = false; c.Crud_View = false  });
            getSP_Fill_Pages_By_ModuleID1.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });

            var getSP_Fill_Pages_By_ModuleID2 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {2}").ToList();
            getSP_Fill_Pages_By_ModuleID2.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID3 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {3}").ToList();
            getSP_Fill_Pages_By_ModuleID3.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID4 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {4}").ToList();
            getSP_Fill_Pages_By_ModuleID4.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID5 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {5}").ToList();
            getSP_Fill_Pages_By_ModuleID5.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID6 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {6}").ToList();
            getSP_Fill_Pages_By_ModuleID6.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID7 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {7}").ToList();
            getSP_Fill_Pages_By_ModuleID7.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID8 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {8}").ToList();
            getSP_Fill_Pages_By_ModuleID8.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID9 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {9}").ToList();
            getSP_Fill_Pages_By_ModuleID9.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID10 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {10}").ToList();
            getSP_Fill_Pages_By_ModuleID10.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID11 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {11}").ToList();
            getSP_Fill_Pages_By_ModuleID11.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID12 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {12}").ToList();
            getSP_Fill_Pages_By_ModuleID12.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });
            var getSP_Fill_Pages_By_ModuleID13 = _context.Set<DAL.Models.TBL_Pages>().FromSqlInterpolated($"exec SP_Fill_Pages_By_ModuleID {13}").ToList();
            getSP_Fill_Pages_By_ModuleID13.ForEach(c => { c.Crud_Insert = false; c.Crud_View = false; c.Crud_Delete = false; c.Crud_Reject = false; c.Crud_Update = false; c.Crud_Authorize = false; });

            rolePermissionVM.moduleList = _mapper.Map<List<ModuleVM>>(getSP_Fill_Pages_By_ModuleID);
            rolePermissionVM.Fill_Modules_By_ModuleID_1 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID1);
            rolePermissionVM.Fill_Modules_By_ModuleID_2 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID2);
            rolePermissionVM.Fill_Modules_By_ModuleID_3 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID3);
            rolePermissionVM.Fill_Modules_By_ModuleID_4 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID4);
            rolePermissionVM.Fill_Modules_By_ModuleID_5 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID5);
            rolePermissionVM.Fill_Modules_By_ModuleID_6 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID6);
            rolePermissionVM.Fill_Modules_By_ModuleID_7 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID7);
            rolePermissionVM.Fill_Modules_By_ModuleID_8 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID8);
            rolePermissionVM.Fill_Modules_By_ModuleID_9 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID9);
            rolePermissionVM.Fill_Modules_By_ModuleID_10 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID10);
            rolePermissionVM.Fill_Modules_By_ModuleID_11 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID11);
            rolePermissionVM.Fill_Modules_By_ModuleID_12 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID12);
            rolePermissionVM.Fill_Modules_By_ModuleID_13 = _mapper.Map<List<TBL_PagesVM>>(getSP_Fill_Pages_By_ModuleID13);
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




        [HttpGet("GroupManagementGridData/{StatusID}/{GroupID:int}")]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(List<GroupManagementVM>))]
        public async Task<IActionResult> GetGroupManagementGridData(string StatusID, int? GroupID)
        {
            try
            {
                var result = Fill_Group_By_GroupID_Maker(GroupID, StatusID);
                return Ok(result);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        /// <summary>
        ///  Fill Grid Maker
        /// </summary>
        /// <param name="GroupID"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        private List<GroupManagementViewModel> Fill_Group_By_GroupID_Maker(int? GroupID, string StatusID)
        {
            try
            {
                GroupID = GroupID == 0 ? null : GroupID;
                StatusID = StatusID == "null" ? null : StatusID;
                var getMakers = _context.Set<GroupManagementViewModel>().FromSqlInterpolated($"exec SP_Fill_Group_By_GroupID_Maker {GroupID}, {StatusID}").ToList();
                return getMakers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [AllowAnonymous]
        [HttpGet("GroupManagementGridDataEdit/{GroupID}/{PageID}")]
        [ProducesResponseType(200, Type = typeof(RolePermissionVM))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGroupManagementGridDataEdit(int GroupID, int PageID)
        {
            try
            {
                GroupManagementVM obj = new GroupManagementVM();

                ViewModels.RolePermissionVM rolePermissionVM = new ViewModels.RolePermissionVM();

                var getCountrys = _context.Set<TBL_Country>().FromSqlRaw("sp_get_Country").ToList();
                rolePermissionVM.countryList = _mapper.Map<List<CountryViewModel>>(getCountrys);
                var result = Fill_Group_By_GroupID_Maker(GroupID, null);
                if (result.Count > 0)
                {
                    if (rolePermissionVM.GroupManagementVM == null)
                    {
                        rolePermissionVM.GroupManagementVM = new GroupManagementVM();
                    }
                    rolePermissionVM.GroupManagementVM.GroupName = result.FirstOrDefault()?.GroupName;
                    rolePermissionVM.GroupManagementVM.GroupDescription = result.FirstOrDefault()?.GroupDescription;
                    rolePermissionVM.GroupManagementVM.Reference = result.FirstOrDefault()?.Reference;
                    rolePermissionVM.GroupManagementVM.GroupOwnerPSID = result.FirstOrDefault()?.Reference;
                    rolePermissionVM.GroupManagementVM.GroupOwnerName = result.FirstOrDefault()?.Reference;
                }
                BindAllGrids(rolePermissionVM);
                GetModulPermission(GroupID, rolePermissionVM.moduleList);

                var getPagePermissions = Get_Pages_Permissions(GroupID, null);

                if (getPagePermissions.Count > 0)
                {
                    foreach (var item in getPagePermissions)
                    {
                        bool Continue;
                        Continue = SetRolePagePermissionOnGridVGSSEnquiry(rolePermissionVM.Fill_Modules_By_ModuleID_1, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridVGSSOrder(rolePermissionVM.Fill_Modules_By_ModuleID_2, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridVGSSCustomer(rolePermissionVM.Fill_Modules_By_ModuleID_3, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionGridVGSSSale(rolePermissionVM.Fill_Modules_By_ModuleID_4, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridVGSSSaleOperation(rolePermissionVM.Fill_Modules_By_ModuleID_5, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridVGSSEncashment(rolePermissionVM.Fill_Modules_By_ModuleID_6, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridVGSSEncashmentOperation(rolePermissionVM.Fill_Modules_By_ModuleID_7, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridGridVGSSSecurity(rolePermissionVM.Fill_Modules_By_ModuleID_8, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridGridVGSSReports(rolePermissionVM.Fill_Modules_By_ModuleID_9, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridGridViewIPS(rolePermissionVM.Fill_Modules_By_ModuleID_10, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridViewIPSReports(rolePermissionVM.Fill_Modules_By_ModuleID_11, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridViewOPSC(rolePermissionVM.Fill_Modules_By_ModuleID_12, item);
                        if (Continue)
                            continue;
                        Continue = SetRolePagePermissionOnGridViewPPB(rolePermissionVM.Fill_Modules_By_ModuleID_13, item);
                        if (Continue)
                            continue;
                    }
                }



                //DtPagesPermissionList = GetPagesPermissionList(GroupID);
                return Ok(rolePermissionVM);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        //private void GetRolePagePermission(int GroupID, List<TBL_PagesVM> tBL_PagesVMs)
        //{
        //    var result = Get_Pages_Permissions(GroupID, null);
        //    foreach (var item in result)
        //    {
        //        bool Continue;
        //        Continue = SetRolePagePermissionOnGridVGSSEnquiry(tBL_PagesVMs., dt.Rows[i]);
        //    }
        //}

        private bool SetRolePagePermissionOnGridVGSSEnquiry(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridVGSSOrder(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridVGSSCustomer(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionGridVGSSSale(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridVGSSSaleOperation(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridVGSSEncashment(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridVGSSEncashmentOperation(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridGridVGSSSecurity(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridGridVGSSReports(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridGridViewIPS(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridViewIPSReports(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridViewOPSC(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }
        private bool SetRolePagePermissionOnGridViewPPB(List<TBL_PagesVM> tBL_PagesVMs, PagePermissionVM itemDataRow)
        {
            if (tBL_PagesVMs.Count > 0)
            {
                foreach (var item in tBL_PagesVMs)
                {
                    if (item.PageID == itemDataRow.PageID)
                    {
                        item.chkAllSelect = true;
                        item.Crud_View = itemDataRow.Can_View;
                        item.Crud_Insert = itemDataRow.Can_Insert;
                        item.Crud_Update = itemDataRow.Can_Update;
                        item.Crud_Authorize = itemDataRow.Can_Authorize;
                        item.Crud_Reject = itemDataRow.Can_Reject;
                        item.Crud_Delete = itemDataRow.Can_Delete;
                        return true;
                    }
                }

            }
            return false;
        }




        private void GetModulPermission(int GroupID, List<ModuleVM> moduleList)
        {
            var result = Get_Modules_Rights(GroupID, null);

            if (result != null)
            {
                foreach (var item in result)
                {
                    foreach (var itemModule in moduleList)
                    {
                        if (itemModule.ModuleID == item.ModuleID)
                        {
                            itemModule.chkAllSelect = true;
                            itemModule.ChkView = item.Can_View;
                            itemModule.ChkInsert = item.Can_Insert;
                            itemModule.ChkUpdate = item.Can_Update;
                            itemModule.ChkAuthorize = item.Can_Authorize;
                            itemModule.ChkReject = item.Can_Reject;
                            itemModule.ChkDelete = item.Can_Delete;
                        }
                    }
                }
            }
        }

        private async Task<DataTable> GetPagesPermissionList(int groupId)
        {
            var RoleID = groupId;
            //var result = await Get_Pages_Permissions_List_By_RoleID(RoleID);
            //return result;
            return null;
        }

        [AllowAnonymous]
        [HttpGet("GetUserAuthorizationGridData/{UserCheckerID}")]
        [ProducesResponseType(200, Type = typeof(List<GridUserAuthorization>))]
        public async Task<IActionResult> GetUserAuthorizationGridData(int? UserCheckerID) //initially pass null here
        {
            UserCheckerID = UserCheckerID == 0 ? null : UserCheckerID;
            var GroupID = new SqlParameter("@UserCheckerID", (object)UserCheckerID ?? DBNull.Value);

            var getUserAuthorization = _context.Set<GridUserAuthorization>().FromSqlInterpolated($"exec [SP_Fill_User_By_UserCheckerID_Checker] {UserCheckerID};").ToList();
            return Ok(getUserAuthorization);
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

        [AllowAnonymous]
        [HttpGet("IsGroupExist/{GroupName1}/{GroupID1}")]
        [ProducesResponseType(200, Type = typeof(bool))]

        public async Task<IActionResult> GroupNameExists(string GroupName1, int? GroupID1 = null)  //int? GroupID, string GroupName
        {
            try
            {
                GroupID1 = GroupID1 == 0 ? null : GroupID1;
                var GroupID = new SqlParameter("@GroupID", (object)GroupID1 ?? DBNull.Value);
                var GroupName = new SqlParameter("@GroupName", GroupName1);
                var queryResult = _context.IntReturnValue.FromSqlRaw<IntReturn>("exec SP_GroupNameExists @GroupID,@GroupName;", GroupID, GroupName).AsEnumerable().FirstOrDefault();
                bool result = true;
                if (queryResult.GroupExist == 0)
                {
                    result = true; // not exist
                }
                else
                {
                    result = false; // exist krta he group name
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw ex;
            }
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


        /// <summary>
        /// Insert User Checker
        /// </summary>
        /// <param name="BLL"></param>
        /// <returns></returns>
        private object Insert_User_Checker(UserEditViewModel user)
        {
            try
            {
                var userIdParam = new SqlParameter("@Id", SqlDbType.Int);
                userIdParam.Direction = ParameterDirection.Output;
                var UserID = new SqlParameter("@UserID", user.UserID);
                var PSID = new SqlParameter("@PSID", user.PSID);
                var Name = new SqlParameter("@Name", user.UserName);
                var Department = new SqlParameter("@Department", user.Department);
                var RegionID = new SqlParameter("@RegionID", user.RegionID);
                var GroupID = new SqlParameter("@GroupID", user.GroupID);
                var MakerStatus = new SqlParameter("@MakerStatus", user.MakerStatus);
                var Action = new SqlParameter("@Action", user.Action);
                var MakerID = new SqlParameter("@MakerID", user.MakerID);
                var MakerDate = new SqlParameter("@MakerDate", user.MakerDate);
                var CheckerActive = new SqlParameter("@CheckerActive", user.CheckerActive);
                var Reference = new SqlParameter("@Reference", user.Reference);
                var CountryCode = new SqlParameter("@CountryCode", user.CountryCode);
                var AccountType = new SqlParameter("@AccountType", user.AccountType);
                var AccountDescription = new SqlParameter("@AccountDescription", user.AccountDescription);
                var Password = new SqlParameter("@Password", user.Password);
                var getMakers = _context.Database.ExecuteSqlRaw("exec SP_Insert_User_Checker @UserID,@PSID,@Name,@Department,@RegionID,@GroupID,@MakerStatus,@Action,@MakerID,@MakerDate,@CheckerActive,@Reference,@CountryCode,@AccountType,@AccountDescription,@PASSWORD, @Id out", UserID, PSID, Name, Department, RegionID, GroupID, MakerStatus, Action, MakerID, MakerDate, CheckerActive, Reference, CountryCode, AccountType, AccountDescription, Password, userIdParam);
                var results = userIdParam.Value;
                return results;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
        private object Insert_User_Maker(UserEditViewModel user)
        {
            try
            {
                var userIdParam = new SqlParameter("@Id", SqlDbType.Int);
                userIdParam.Direction = ParameterDirection.Output;
                var PSID = new SqlParameter("@PSID", user.PSID);
                var Name = new SqlParameter("@Name", user.UserName);
                var Department = new SqlParameter("@Department", user.Department);
                var RegionID = new SqlParameter("@RegionID", user.RegionID);
                var GroupID = new SqlParameter("@GroupID", user.GroupID);
                var Active = new SqlParameter("@Active", user.Active);
                var Status = new SqlParameter("@Status", user.MakerStatus);
                var Action = new SqlParameter("@Action", "INSERT");
                var CreatedBy = new SqlParameter("@CreatedBy", user.CreatedBy);
                var CreatedDate = new SqlParameter("@CreatedDate", DateTime.Now);
                var Signatory = new SqlParameter("@Signatory", user.Signatory);
                var AuthSignatory = new SqlParameter("@AuthSignatory", user.AuthSignatory);
                var Reference = new SqlParameter("@Reference", user.Reference);
                var CountryCode = new SqlParameter("@CountryCode", user.CountryCode);
                var AccountType = new SqlParameter("@AccountType", user.AccountType);
                var AccountDescription = new SqlParameter("@AccountDescription", user.AccountDescription);
                var PASSWORD = new SqlParameter("@PASSWORD", "Microsoft!23");
                var getMakers = _context.Database.ExecuteSqlRaw("exec SP_Insert_User_Maker @PSID,@Name,@Department,@RegionID,@GroupID,@Active,@Status,@Action,@CreatedBy,@CreatedDate,@Signatory,@AuthSignatory,@Reference,@CountryCode,@AccountType,@AccountDescription,@PASSWORD, @Id out", PSID, Name, Department, RegionID, GroupID, Active, Status, Action, CreatedBy, CreatedDate, Signatory, AuthSignatory, Reference, CountryCode, AccountType, AccountDescription, PASSWORD, userIdParam);
                var results = userIdParam.Value;
                return results;
            }
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


        #region work for edit group

        private List<ModulesPermission> Get_Modules_Rights(int? RoleID, int? UserID)
        {
            try
            {
                var getModuleRights = _context.Set<ModulesPermission>().FromSqlInterpolated($"exec SP_Get_Modules_Rights {RoleID}, {UserID}").ToList();
                return (getModuleRights);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private List<UserLoginVM> Get_Login_User_By_PSID(string PSID)
        {
            try
            {
                var getGet_Login_User_By_PSID = _context.Set<UserLoginVM>().FromSqlInterpolated($"exec SP_Get_Login_User_By_PSID {PSID}").ToList();
                return (getGet_Login_User_By_PSID);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        #endregion
        //private async Task<DataTable> Get_Pages_Permissions_List_By_RoleID(int RoleID)
        //{
        //    try
        //    {
        //        var getModuleRights = _context.Set<ModulesPermission>().FromSqlInterpolated($"exec SP_Get_PagesPermissionListByRoleID {RoleID}").ToList();
        //        DtPagesPermissionList = getModuleRights;
        //        return Ok(getModuleRights);
        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }

        //}



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

        private List<TBL_USER> PSIDExists(int? UserID, string PSID)
        {
            try
            {
                var UserID1 = new SqlParameter("@UserID", (object)UserID ?? DBNull.Value);
                var PSID1 = new SqlParameter("@PSID", PSID);
                var getGet_Login_User_By_PSID = _context.Set<TBL_USER>().FromSqlInterpolated($"exec SP_PSIDExists {UserID1},{PSID1}").ToList();
                return getGet_Login_User_By_PSID;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

    }
}
