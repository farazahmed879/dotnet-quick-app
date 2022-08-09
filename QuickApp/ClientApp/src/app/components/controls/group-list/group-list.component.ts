import {
  AfterViewInit, Component,
  OnInit, ViewChild
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { CreateOrEditGroup } from 'src/app/models/createGroup.model';
import { GridUserManagementVM } from 'src/app/models/gridUserManagementVM.model';
import { Group } from 'src/app/models/group.model';
import { Permission } from 'src/app/models/permission.model';
import { Role } from 'src/app/models/role.model';
import { User } from 'src/app/models/user.model';
import { AccountService } from 'src/app/services/account.service';
import {
  AlertService,
  DialogType,
  MessageSeverity
} from 'src/app/services/alert.service';
import { AppTranslationService } from 'src/app/services/app-translation.service';
import { Utilities } from 'src/app/services/utilities';
import { CreateOrEditGroupComponent } from '../create-or-edit-group/create-or-edit-group.component';

@Component({
  selector: 'app-group-list',
  templateUrl: './group-list.component.html',
  styleUrls: ['./group-list.component.scss'],
})
export class GroupListComponent implements OnInit, AfterViewInit {
  @ViewChild(MatPaginator) paginator: MatPaginator;
  displayedColumns = [
    'index',
    'groupName',
    'groupDescription',
    'makerStatus',
    'createdBy',
    'createdDate',
    'reason',
    'actions',
  ];
  public dataSource: MatTableDataSource<any> = new MatTableDataSource();
  public pageSize = 10;
  public currentPage = 0;
  public totalSize = 0;
  public pageIndex: number = 0;
  public pageLoader: boolean = false;

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

  // @ViewChild('indexTemplate', { static: true })
  // indexTemplate: TemplateRef<any>;

  // @ViewChild('userNameTemplate', { static: true })
  // userNameTemplate: TemplateRef<any>;

  // @ViewChild('rolesTemplate', { static: true })
  // rolesTemplate: TemplateRef<any>;

  // @ViewChild('actionsTemplate', { static: true })
  // actionsTemplate: TemplateRef<any>;

  // @ViewChild('editorModal', { static: true })
  // editorModal: ModalDirective;

  // @ViewChild('groupinfoeditorModal', { static: true })
  // groupinfoeditorModal: ModalDirective;

  // @ViewChild('createOrEditGroupModal', { static: true })
  // createOrEditGroupModal: CreateOrEditGroupComponent;

  constructor(
    private alertService: AlertService,
    private translationService: AppTranslationService,
    private accountService: AccountService,
    private _dialog: MatDialog
  ) {}

  ngOnInit() {
    const gT = (key: string) => this.translationService.getTranslation(key);
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
    this.dataSource.paginator = this.paginator;
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
    this.pageLoader = true;
    this.alertService.startLoadingMessage();
    this.accountService.getGroups().subscribe((res: []) => {
      if (res) {
        this.dataSource = new MatTableDataSource();
        this.dataSource.data = res;
        this.alertService.stopLoadingMessage();
        this.pageLoader = false;
      }
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

    this.alertService.showStickyMessage(
      'Load Error',
      `Unable to retrieve users from the server.\r\nErrors: "${Utilities.getHttpResponseMessages(
        error
      )}"`,
      MessageSeverity.error,
      error
    );
  }

  onSearchChanged(value: string) {
    this.dataSource.filter = value.trim().toLowerCase();
  }

  showCreateOrEditDialog(row?: any): void {
    debugger
    let createOrEditSubTypeDialog;
    if (!row) {
      createOrEditSubTypeDialog = this._dialog.open(CreateOrEditGroupComponent);
    } else {
      createOrEditSubTypeDialog = this._dialog.open(
        CreateOrEditGroupComponent,{
          data: row
       }
      );
    }

    createOrEditSubTypeDialog.afterClosed().subscribe((result) => {
      if (result) {
        
      }
    });
  }

  handleDelete(item) {
    this.alertService.showDialog(
      'Are you sure you want to delete "' + item.groupName + '"?',
      DialogType.confirm,
      () => this.deleteGroupHelper(item)
    );
  }

  deleteGroupHelper(row) {
    this.alertService.startLoadingMessage('Deleting...');
    this.loadingIndicator = true;

    this.accountService.deleteGroup(row).subscribe(
      (results) => {
        this.alertService.stopLoadingMessage();
        this.loadingIndicator = false;

        // this.rowsCache = this.rowsCache.filter(item => item !== row);
        // this.rows = this.rows.filter(item => item !== row);
      },
      (error) => {
        this.alertService.stopLoadingMessage();
        this.loadingIndicator = false;

        this.alertService.showStickyMessage(
          'Delete Error',
          `An error occured whilst deleting the group.\r\nError: "${Utilities.getHttpResponseMessages(
            error
          )}"`,
          MessageSeverity.error,
          error
        );
      }
    );
  }


  showReason(item) {
    this.alertService.showDialog('Reason:  "' + item.reason + '"');
  }


  get canAssignRoles() {
    return this.accountService.userHasPermission(
      Permission.assignRolesPermission
    );
  }

  get canViewRoles() {
    return this.accountService.userHasPermission(
      Permission.viewRolesPermission
    );
  }

  get canManageUsers() {
    return this.accountService.userHasPermission(
      Permission.manageUsersPermission
    );
  }
}
