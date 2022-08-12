import { Component, OnInit, AfterViewInit, TemplateRef, ViewChild, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { DomainVM } from 'src/app/models/domainVM.model';
import { GridUserManagementVM } from 'src/app/models/gridUserManagementVM.model';
import { Group } from 'src/app/models/group.model';
import { Permission } from 'src/app/models/permission.model';
import { Role } from 'src/app/models/role.model';
import { UserEdit } from 'src/app/models/user-edit.model';
import { User } from 'src/app/models/user.model';
import { AccountService } from 'src/app/services/account.service';
import { AlertService, DialogType, MessageSeverity } from 'src/app/services/alert.service';
import { AppTranslationService } from 'src/app/services/app-translation.service';
import { Utilities } from 'src/app/services/utilities';
import { CreateOrEditUserComponent } from '../create-or-edit-user/create-or-edit-user.component';
// import { CreateOrEditGroupComponent } from '../../create-or-edit-group/create-or-edit-group.component';
// import { GroupInfoComponent } from '../../group-info.component';
// import { UserInfoComponent } from '../create-or-edit-user/user-info.component';


@Component({
  selector: 'app-users-management',
  templateUrl: './users-management.component.html',
  styleUrls: ['./users-management.component.scss']
})
export class UsersManagementComponent implements OnInit, AfterViewInit {
  columns: any[] = [];
  rows1: User[] = [];
  rowsCache1: User[] = [];
  rows: GridUserManagementVM[] = [];
  rowsCache: GridUserManagementVM[] = [];
  editedUser: UserEdit;
  editedGroupInfo: Group;
  sourceUser: UserEdit;
  editingUserName: { name: string };
  editingGroupName: { name: string };
  loadingIndicator: boolean;

  allRoles: Role[] = [];
  GridUserManagementVM: GridUserManagementVM[] = [];

  @ViewChild('indexTemplate', { static: true })
  indexTemplate: TemplateRef<any>;

  @ViewChild('userNameTemplate', { static: true })
  userNameTemplate: TemplateRef<any>;

  @ViewChild('rolesTemplate', { static: true })
  rolesTemplate: TemplateRef<any>;

  @ViewChild('actionsTemplate', { static: true })
  actionsTemplate: TemplateRef<any>;

  @ViewChild('editorModal', { static: true })
  editorModal: ModalDirective;

  @ViewChild('groupinfoeditorModal', { static: true })
  groupinfoeditorModal: ModalDirective;

  // @ViewChild('userEditor', { static: true })
  // userEditor: UserInfoComponent;

  constructor(
    private alertService: AlertService,
    private translationService: AppTranslationService,
    private accountService: AccountService,
    private _dialog: MatDialog) {
  }


  ngOnInit() {

    const gT = (key: string) => this.translationService.getTranslation(key);

    // this.columns = [
    //   { prop: 'index', name: '#', width: 40, cellTemplate: this.indexTemplate, canAutoResize: false },
    //   { prop: 'jobTitle', name: gT('users.management.Title'), width: 50 },
    //   { prop: 'accountOwner', name: gT('users.management.AccountOwner'), width: 170 },
    //   { prop: 'department', name: gT('users.management.Department'), width: 170 },
    //   { prop: 'userName', name: gT('users.management.UserName'), width: 90, cellTemplate: this.userNameTemplate },
    //   { prop: 'fullName', name: gT('users.management.FullName'), width: 120 },
    //   { prop: 'email', name: gT('users.management.Email'), width: 120 },
    //   { prop: 'roles', name: gT('users.management.Roles'), width: 120, cellTemplate: this.rolesTemplate },
    //   { prop: 'phoneNumber', name: gT('users.management.PhoneNumber'), width: 100 },
    //   { prop: 'country', name: gT('users.management.Country'), width: 100 },
    //   { prop: 'region', name: gT('users.management.Region'), width: 100 }
    // ];

    this.columns = [
      { prop: 'index', name: '#', width: 40, cellTemplate: this.indexTemplate, canAutoResize: false },
      { prop: 'psid', name: gT('users.manageuser.PSID'), width: 100 },
      { prop: 'name', name: gT('users.manageuser.Name'), width: 170 },
      { prop: 'status', name: gT('users.manageuser.Status'), width: 170 },
      { prop: 'statusRequest', name: gT('users.manageuser.StatusRequest'), width: 170 },
      { prop: 'createdBy', name: gT('users.manageuser.CreatedBy'), width: 90, cellTemplate: this.userNameTemplate },
      { prop: 'createdDate', name: gT('users.manageuser.CreatedDate'), width: 120 }
    ];



    if (this.canManageUsers) {
      this.columns.push({ name: '', width: 160, cellTemplate: this.actionsTemplate, resizeable: false, canAutoResize: false, sortable: false, draggable: false });
    }

    this.loadData();
    this.loadGridUserManagement();
  }


  ngAfterViewInit() {

    // this.userEditor.changesSavedCallback = () => {
    //   this.addNewUserToList();
    //   this.editorModal.hide();
    // };

    // this.userEditor.changesCancelledCallback = () => {
    //   this.editedUser = null;
    //   this.sourceUser = null;
    //   this.editorModal.hide();
    // };
  }


  addNewUserToList() {
    if (this.sourceUser) {
      Object.assign(this.sourceUser, this.editedUser);

      // let sourceIndex = this.rowsCache.indexOf(this.sourceUser, 0);
      // if (sourceIndex > -1) {
      //   Utilities.moveArrayItem(this.rowsCache, sourceIndex, 0);
      // }

      // sourceIndex = this.rows.indexOf(this.sourceUser, 0);
      // if (sourceIndex > -1) {
      //   Utilities.moveArrayItem(this.rows, sourceIndex, 0);
      // }

      this.editedUser = null;
      this.sourceUser = null;
    } else {
      const user = new User();
      Object.assign(user, this.editedUser);
      this.editedUser = null;

      let maxIndex = 0;
      for (const u of this.rowsCache) {
        if ((u as any).index > maxIndex) {
          maxIndex = (u as any).index;
        }
      }

      (user as any).index = maxIndex + 1;

      // this.rowsCache.splice(0, 0, user);
      // this.rows.splice(0, 0, user);
      // this.rows = [...this.rows];
    }
  }


  loadData() {
    this.alertService.startLoadingMessage();
    this.loadingIndicator = true;
    if (this.canViewRoles) {
      this.accountService.getUsersAndRoles().subscribe(results => this.onDataLoadSuccessful(results[0], results[1]), error => this.onDataLoadFailed(error));
    } else {
      this.accountService.getUsers().subscribe(users => this.onDataLoadSuccessful(users, this.accountService.currentUser.roles.map(x => new Role(x))), error => this.onDataLoadFailed(error));
    }
  }

  loadGridUserManagement() {

    this.alertService.stopLoadingMessage();
    this.loadingIndicator = false;

    this.accountService.getUserManagementGridData(null, null)
      .subscribe(results => {
        this.GridUserManagementVM = results;
        this.rowsCache = [...this.GridUserManagementVM];
        this.rows = this.GridUserManagementVM;
        console.log("this.GridUserManagementVM" + this.GridUserManagementVM);
      });
  }

  onDataLoadSuccessful(users: User[], roles: Role[]) {
    this.alertService.stopLoadingMessage();
    this.loadingIndicator = false;

    users.forEach((user, index) => {
      (user as any).index = index + 1;
    });

    // this.rowsCache = [...users];
    // this.rows = users;

    this.allRoles = roles;
  }


  onDataLoadFailed(error: any) {
    this.alertService.stopLoadingMessage();
    this.loadingIndicator = false;

    this.alertService.showStickyMessage('Load Error', `Unable to retrieve users from the server.\r\nErrors: "${Utilities.getHttpResponseMessages(error)}"`,
      MessageSeverity.error, error);
  }


  onSearchChanged(value: string) {
    //this.rows = this.rowsCache.filter(r => Utilities.searchArray(value, false, r.userName, r.fullName, r.email, r.phoneNumber, r.jobTitle, r.roles,r.accountOwner,r.department));
  }

  // onEditorModalHidden() {
  //   this.editingUserName = null;
  //   this.userEditor.resetForm(true);
  // }


  // newUser() {
  //   this.editingUserName = null;
  //   this.sourceUser = null;
  //   this.editedUser = this.userEditor.newUser(this.allRoles);
  //   this.editorModal.show();
  // }

  newGroupInfo() {
    this.editingGroupName = null;
    //this.sourceGroupInfo = null;
    //this.editedGroupInfo = this.groupEditor.newGroup();
    console.log("this.editedGroupInfo" + this.editedGroupInfo);
    this.groupinfoeditorModal.show();
  }


  editUser(row: UserEdit) {
    // this.editingUserName = { name: row.userName };
    // this.sourceUser = row;
    // this.editedUser = this.userEditor.editUser(row, this.allRoles);
    // //this.accountService.getUserManagementGridData(row.userID,null)
    // //.subscribe(results => {    
    //   //this.GridUserManagementVM = results;
    //   //this.rowsCache = [...this.GridUserManagementVM];
    //   //this.rows = this.GridUserManagementVM;    
    //   console.log("this.GridUserManagementVM" + this.GridUserManagementVM);      
    // //});
    // this.editorModal.show();
  }


  deleteUser(row: UserEdit) {
    this.alertService.showDialog('Are you sure you want to delete \"' + row.userName + '\"?', DialogType.confirm, () => this.deleteUserHelper(row));
  }


  deleteUserHelper(row: UserEdit) {

    this.alertService.startLoadingMessage('Deleting...');
    this.loadingIndicator = true;

    this.accountService.deleteUser(row)
      .subscribe(results => {
        this.alertService.stopLoadingMessage();
        this.loadingIndicator = false;

        // this.rowsCache = this.rowsCache.filter(item => item !== row);
        // this.rows = this.rows.filter(item => item !== row);
      },
        error => {
          this.alertService.stopLoadingMessage();
          this.loadingIndicator = false;

          this.alertService.showStickyMessage('Delete Error', `An error occured whilst deleting the user.\r\nError: "${Utilities.getHttpResponseMessages(error)}"`,
            MessageSeverity.error, error);
        });
  }

  //create or edit

  showCreateOrEditDialog(row?: any): void {
    debugger
    let createOrEditSubTypeDialog;
    if (!row) {
      createOrEditSubTypeDialog = this._dialog.open(CreateOrEditUserComponent);
    } else {
      createOrEditSubTypeDialog = this._dialog.open(
        CreateOrEditUserComponent, {
        data: row
      }
      );
    }

    createOrEditSubTypeDialog.afterClosed().subscribe((result) => {
      if (result) {

      }
    });
  }



  get canAssignRoles() {
    return this.accountService.userHasPermission(Permission.assignRolesPermission);
  }

  get canViewRoles() {
    return this.accountService.userHasPermission(Permission.viewRolesPermission);
  }

  get canManageUsers() {
    return this.accountService.userHasPermission(Permission.manageUsersPermission);
  }
}
