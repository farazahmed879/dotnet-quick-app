import { Component, OnInit, AfterViewInit, TemplateRef, ViewChild, Input } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { CreateOrEditGroup } from 'src/app/models/createGroup.model';
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
import { CreateOrEditGroupComponent } from '../create-or-edit-group/create-or-edit-group.component';
import { GroupInfoComponent } from '../group-info.component';
import { UserInfoComponent } from '../user-info.component';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-group-list',
  templateUrl: './group-list.component.html',
  styleUrls: ['./group-list.component.scss']
})
export class GroupListComponent implements OnInit, AfterViewInit {

  displayedColumns = ['index', 'groupName', 'groupDescription', 'makerStatus', 'reason', 'createdBy', 'createdDate'];
  public dataSource: MatTableDataSource<any> = new MatTableDataSource();
  public pageSize = 10;
  public currentPage = 0;
  public totalSize = 0;
  public pageIndex: number = 0;


  columns: any[] = [];
  rows1: User[] = [];
  rowsCache1: User[] = [];
  rows: GridUserManagementVM[] = [];
  rowsCache: GridUserManagementVM[] = [];
  editedGroup: any;
  editedGroupInfo: CreateOrEditGroup;
  sourceUser: Group;
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

  @ViewChild('createOrEditGroupModal', { static: true })
  createOrEditGroupModal: CreateOrEditGroupComponent;

  @ViewChild('asd', { static: true })
  asd: UserInfoComponent;

  constructor(private alertService: AlertService, private translationService: AppTranslationService, private accountService: AccountService) {
  }


  ngOnInit() {
    const gT = (key: string) => this.translationService.getTranslation(key);


    this.columns = [
      { prop: 'index', name: '#', width: 40, cellTemplate: this.indexTemplate, canAutoResize: false },
      { prop: 'groupName', name: 'Group Name', width: 100 },
      { prop: 'groupDescription', name: 'Description', width: 170 },
      { prop: 'makerStatus', name: 'Status', width: 170 },
      { prop: 'reason', name: 'Reason', width: 170 },
      { prop: 'createdBy', name: 'Created By', width: 90, cellTemplate: this.userNameTemplate },
      { prop: 'createdDate', name: 'Craeted Date', width: 120 }
    ];



    // if (this.canManageUsers) {
    //   this.columns.push({ name: '', width: 160, cellTemplate: this.actionsTemplate, resizeable: false, canAutoResize: false, sortable: false, draggable: false });
    // }

    this.getAllGroups();
  }


  pageChange(e: any) {
    e.pageIndex = e.pageIndex + 1;
    this.currentPage = e.pageIndex;
    this.pageSize = e.pageSize;
    this.getAllGroups();
  }


  ngAfterViewInit() {

    // this.createOrEditGroupModal.changesSavedCallback = () => {
    //   this.addNewUserToList();
    //   this.editorModal.hide();
    // };

    // this.createOrEditGroupModal.changesCancelledCallback = () => {
    //   this.editedGroup = null;
    //   this.sourceUser = null;
    //   this.editorModal.hide();
    // };
  }


  addNewUserToList() {
    if (this.sourceUser) {
      Object.assign(this.sourceUser, this.editedGroup);

      this.editedGroup = null;
      this.sourceUser = null;
    } else {
      const user = new User();
      Object.assign(user, this.editedGroup);
      this.editedGroup = null;

      let maxIndex = 0;
      for (const u of this.rowsCache) {
        if ((u as any).index > maxIndex) {
          maxIndex = (u as any).index;
        }
      }

      (user as any).index = maxIndex + 1;
    }
  }


  getAllGroups() {
    this.alertService.startLoadingMessage();
    this.accountService.getGroups().subscribe((res: []) => {
      if (res) {
        this.dataSource = new MatTableDataSource();
        this.dataSource.data = res;
      }
    })
    this.alertService.stopLoadingMessage();
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

  onEditorModalHidden() {
    this.editingUserName = null;
    this.createOrEditGroupModal.resetForm(true);
  }


  newGroupInfo() {
    this.editingGroupName = null;
    //this.sourceGroupInfo = null;
    //this.editedGroupInfo = this.groupEditor.newGroup();
    // console.log("this.editedGroupInfo" + this.editedGroupInfo);
    this.groupinfoeditorModal.show();
  }


  editGroup(row: any) {
    this.editingUserName = { name: row.userName };
    this.sourceUser = row;
    this.editedGroup = this.createOrEditGroupModal.editGroup(row);
    console.log("this.GridUserManagementVM" + this.GridUserManagementVM);
    this.editorModal.show();
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
