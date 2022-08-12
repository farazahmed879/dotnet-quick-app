import {
  Component, Inject,
  Injector, OnDestroy, OnInit, Optional
} from '@angular/core';
import {
  MatDialogRef,
  MAT_DIALOG_DATA
} from '@angular/material/dialog';
import { Observable, of } from 'rxjs';
import { Group } from 'src/app/models/group.model';
import { AccountService } from 'src/app/services/account.service';
import {
  AlertService,
  DialogType,
  MessageSeverity
} from 'src/app/services/alert.service';
import { AppTranslationService } from 'src/app/services/app-translation.service';
import { AuthService } from 'src/app/services/auth.service';
import { LocalStoreManager } from 'src/app/services/local-store-manager.service';
import { TodoDemoComponent } from '../todo-demo.component';

@Component({
  selector: 'app-create-or-edit-group',
  templateUrl: './create-or-edit-group.component.html',
  styleUrls: ['./create-or-edit-group.component.scss'],
})
export class CreateOrEditGroupComponent implements OnInit, OnDestroy {
  constructor(
    private alertService: AlertService,
    private translationService: AppTranslationService,
    private localStorage: LocalStoreManager,
    private authService: AuthService,
    private accountService: AccountService,
    injector: Injector,
    private _dialogRef: MatDialogRef<CreateOrEditGroupComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) private data: any
  ) { }
  editableMode = false;
  rowsCache = [];
  group: any = {
    groupID: 0,
    groupName: '',
    groupDescription: '',
    reference: '',
    groupOwnerPSID: '',
    groupOwnerName: '',
    countryCode: '',
    active: '',
  };
  isDataLoaded = false;
  loadingIndicator = true;
  isSaving = false;
  formResetToggle = true;
  _currentUserId: string;
  _hideCompletedTasks = false;
  allData: any;
  modulesList: any;
  selectAllParent = false;
  countries = [];
  saving: false;

  public changesSavedCallback: () => void;
  public changesFailedCallback: () => void;
  public changesCancelledCallback: () => void;

  modalData: any;

  currentUserId() {
    let currentUser: any = sessionStorage.getItem("current_user");
    let data = JSON.parse(currentUser);
    if (data)
      return data['id'];
  }


  ngOnInit() {
    if (this.data) {
      console.log('this.data', this.data);
      this.group = this.data;
      this.editableMode = true;
    }
    this.getAllGroups();
    this.loadCountryViewModel();
  }

  getAllGroups() {
    this.alertService.startLoadingMessage('', 'Retrieving Data');
    this.accountService.getModules().subscribe((res: any) => {
      if (res) {
        this.allData = res.moduleList.map((i) => {
          let children = res[`fill_Modules_By_ModuleID_${i.moduleID}`];
          return {
            ...i,
            children,
            crud_View: !children.some((j) => j.crud_View == false),
            crud_Insert: !children.some((j) => j.crud_Insert == false),
            crud_Update: !children.some((i) => i.crud_Update == false),
            crud_Authorize: !children.some((i) => i.crud_Authorize == false),
            crud_Reject: !children.some((i) => i.crud_Reject == false),
            crud_Delete: !children.some((i) => i.crud_Delete == false),
          };
        });
        console.log('all Data', this.allData);
        this.alertService.stopLoadingMessage();
      }
    });

  }

  dataMappting() {
    this.selectAllParent = true;
    this.allData.forEach((item) => {
      item.crud_View = !item.children.some((i) => i.crud_View == false);
      item.crud_Insert = !item.children.some((i) => i.crud_Insert == false);
      item.crud_Update = !item.children.some((i) => i.crud_Update == false);
      item.crud_Authorize = !item.children.some(
        (i) => i.crud_Authorize == false
      );
      item.crud_Reject = !item.children.some((i) => i.crud_Reject == false);
      item.crud_Delete = !item.children.some((i) => i.crud_Delete == false);
      if (
        !item.crud_View ||
        !item.crud_Insert ||
        !item.crud_Update ||
        item.crud_Authorize ||
        !item.crud_Reject ||
        !item.crud_Delete
      )
        this.selectAllParent = false;
    });
  }

  ngOnDestroy() {
    this.saveToDisk();
  }

  resetForm(replace = false) {

  }

  showErrorAlert(caption: string, message: string) {
    this.alertService.showMessage(caption, message, MessageSeverity.error);
  }

  checkGroupName(name) {
    this.accountService.checkGroupName(name).subscribe((res: any) => {
      if (res) {
        return this.alertService.showMessage(
          'Alert',
          'Group Name Aleady Exist',
          MessageSeverity.error
        );
      }
    });
  }

  loadCountryViewModel() {
    this.accountService.getCountryRegionUserGroup().subscribe((results) => {
      this.countries = results.listCountryViewModel;
      //console.log("CountryViewModel" + this.UserViewModel);
    });
  }

  reqObjectMapper(): Observable<Group> {
    let ob: Group = {
      groupID: 0,
      groupName: this.group.groupName,
      groupDescription: this.group.groupDescription,
      makerStatus: '',
      isActive: this.group.active,
      action: '',
      reason: '',
      createdBy: this.currentUserId(),
      createdDate: new Date(),
      updatedBy: this.currentUserId(),
      updatedDate: new Date(),
      groupCheckerID: 0,
      checkerStatus: '',
      checkerActive: true,
      makerID: '',
      makerDate: new Date(),
      checkerID: '',
      countryCode: this.group.countryCode,
      checkerDate: new Date(),
      reference: this.group.reference,
      groupOwnerPSID: this.group.groupOwnerPSID,
      groupOwnerName: this.group.groupOwnerName,
      psid: '',
      moduleVMList: [],
    };
    this.allData.forEach((el) => {
      let moduleObject: any = {};
      moduleObject.moduleID = el.moduleID;
      moduleObject.applicationID = 0;
      moduleObject.moduleName = el.moduleName;
      moduleObject.moduleDescription = el.moduleDescription;
      moduleObject.moduleIcon = el.moduleIcon;
      moduleObject.moduleSortOrder = el.moduleSortOrder;
      moduleObject.backColor = el.children.some(
        (i) =>
          i.can_View ||
          i.crud_Insert ||
          i.crud_Update ||
          i.crud_Authorize ||
          i.crud_Reject ||
          i.crud_Delete
      );
      moduleObject.chkAllSelect = true;
      moduleObject.chkView = el.crud_View;
      moduleObject.chkInsert = el.crud_Insert;
      moduleObject.chkUpdate = el.crud_Update;
      moduleObject.chkAuthorize = el.crud_Authorize;
      moduleObject.chkReject = el.crud_Reject;
      moduleObject.chkDelete = el.crud_Delete;
      moduleObject.active = true;
      moduleObject.createdBy = this.currentUserId();
      moduleObject.updatedBy = this.currentUserId();
      moduleObject.createdDate = new Date();
      moduleObject.updatedDate = new Date();
      ob.moduleVMList.push(moduleObject);
      el.children.forEach((ch) => {
        ch.backColor =
          ch.crud_View ||
          ch.crud_Insert ||
          ch.crud_Update ||
          ch.crud_Authorize ||
          ch.crud_Reject ||
          ch.crud_Delete;
      });
      let moduleChildrenName = `fill_Modules_By_ModuleID_${el.moduleID}`;
      ob[moduleChildrenName] = el.children;
    });

    return of(ob);
  }

  save(form) {
    this.isSaving = true;
    this.reqObjectMapper().subscribe((req) => {
      console.log('req', req);
      this.accountService.saveGroup(req).subscribe((res: any) => {
        if (res) {
          debugger;
          this.isSaving = false;
          return this.alertService.showMessage(
            'Alert',
            'Group Name Aleady Exist',
            MessageSeverity.error
          );
        }
      });
    });
    //this.saveToDisk();
  }


  delete(row) {
    this.alertService.showDialog(
      'Are you sure you want to delete the task?',
      DialogType.confirm,
      () => this.deleteHelper(row)
    );
  }

  deleteHelper(row) {

  }

  getFromDisk() {
    return this.localStorage.getDataObject(
      `${TodoDemoComponent.DBKeyTodoDemo}:${this.currentUserId}`
    );
  }

  saveToDisk() {
    if (this.isDataLoaded) {
      this.localStorage.saveSyncedSessionData(
        this.rowsCache,
        `${TodoDemoComponent.DBKeyTodoDemo}:${this.currentUserId}`
      );
    }
  }

  // newGroup() {
  //   this.isGeneralEditor = true;
  //   this.isNewGroup = true;
  //   this.edit();

  //   return this.group;
  // }

  handleChangeSelectAll(event) {
    this.allData.forEach((el: any) => {
      this.handleModuleParent(el, event);
    });
  }

  editGroup(group: any) {
    debugger;
  }

  handleSingleParentChange(item, event, parent) {
    item.children.map((el) => {
      el[parent] = event.checked;
    });
  }

  handleModuleParent(item, event) {
    this.handlePageParent(item, event);
    item.children.map((el) => {
      this.handlePageParent(el, event);
    });
  }

  handlePageParent(item, event) {
    item.crud_View = event.checked;
    item.crud_Insert = event.checked;
    item.crud_Update = event.checked;
    item.crud_Authorize = event.checked;
    item.crud_Reject = event.checked;
    item.crud_Delete = event.checked;
  }

  handleSelectAll() {
    let parentCheck = true;
    this.allData.forEach((item: any) => {
      if (
        !item.crud_View ||
        !item.crud_Insert ||
        !item.crud_Update ||
        !item.crud_Authorize ||
        !item.crud_Reject ||
        !item.crud_Delete
      )
        parentCheck = false;
      return;
    });
    return parentCheck;
  }

  checkIfAllChildren(data, prop) {
    let parentCheck = true;
    data.children.forEach((item: any) => {
      if (!item[prop]) parentCheck = false;
      return;
    });
    return parentCheck;
  }

  close(result: boolean): void {
    this._dialogRef.close(result);
  }
}
