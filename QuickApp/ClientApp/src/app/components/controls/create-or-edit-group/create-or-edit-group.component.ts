import { Component, OnInit, OnDestroy, Input, TemplateRef, ViewChild } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Observable, of } from "rxjs";
import { CreateOrEditGroup } from 'src/app/models/createGroup.model';
import { Group } from 'src/app/models/group.model';
import { AccountService } from 'src/app/services/account.service';
import { AlertService, DialogType, MessageSeverity } from 'src/app/services/alert.service';
import { AppTranslationService } from 'src/app/services/app-translation.service';
import { AuthService } from 'src/app/services/auth.service';
import { LocalStoreManager } from 'src/app/services/local-store-manager.service';
import { Utilities } from 'src/app/services/utilities';
import { TodoDemoComponent } from '../todo-demo.component';



@Component({
  selector: 'app-create-or-edit-group',
  templateUrl: './create-or-edit-group.component.html',
  styleUrls: ['./create-or-edit-group.component.scss']
})
export class CreateOrEditGroupComponent implements OnInit, OnDestroy {
  public static readonly DBKeyTodoDemo = 'todo-demo.todo_list';

  rows = [];
  rowsCache = [];
  columns = [];
  editing = {};
  group: any = {
    name: "",
    description: "",
    reference: "",
    ownerPSID: "",
    ownerName: "",
    countryId: "",
    isActive: "",
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

  public changesSavedCallback: () => void;
  public changesFailedCallback: () => void;
  public changesCancelledCallback: () => void;

  public data: any = [
    {
      id: 1, name: 'A',
      view: false,
      insert: false,
      update: false,
      authorize: false,
      reject: false,
      delete: false,
    },
    {
      id: 2, name: 'B', view: false,
      insert: false,
      update: false,
      authorize: false,
      reject: false,
      delete: false,
    },
    {
      id: 3, name: 'C', view: false,
      insert: false,
      update: false,
      authorize: false,
      reject: false,
      delete: false,
    },
    {
      id: 4, name: 'D', view: false,
      insert: false,
      update: false,
      authorize: false,
      reject: false,
      delete: false,
    }];
  modalData: any;

  get currentUserId() {
    if (this.authService.currentUser) {
      this._currentUserId = this.authService.currentUser.id;
    }

    return this._currentUserId;
  }


  set hideCompletedTasks(value: boolean) {

    if (value) {
      this.rows = this.rowsCache.filter(r => !r.completed);
    } else {
      this.rows = [...this.rowsCache];
    }


    this._hideCompletedTasks = value;
  }

  get hideCompletedTasks() {
    return this._hideCompletedTasks;
  }


  @Input()
  verticalScrollbar = false;


  @ViewChild('statusHeaderTemplate', { static: true })
  statusHeaderTemplate: TemplateRef<any>;

  @ViewChild('statusTemplate', { static: true })
  statusTemplate: TemplateRef<any>;

  @ViewChild('nameTemplate', { static: true })
  nameTemplate: TemplateRef<any>;

  @ViewChild('descriptionTemplate', { static: true })
  descriptionTemplate: TemplateRef<any>;

  @ViewChild('actionsTemplate', { static: true })
  actionsTemplate: TemplateRef<any>;

  @ViewChild('editorModal', { static: true })
  editorModal: ModalDirective;


  constructor(private alertService: AlertService,
    private translationService: AppTranslationService,
    private localStorage: LocalStoreManager,
    private authService: AuthService,
    private accountService: AccountService
  ) {
  }



  ngOnInit() {

    // this.modalData = this.data;
    // this.modalData.forEach((element: any) => {
    //   element.view = false;
    //   element.insert = false;
    //   element.update = false;
    //   element.authorize = false;
    //   element.reject = false;
    //   element.delete = false;
    // });
    this.getAllGroups();
    this.loadCountryViewModel();
  }


  getAllGroups() {
    this.alertService.startLoadingMessage();
    this.accountService.getModules().subscribe((res: any) => {
      if (res) {
        this.allData = res.moduleList.map(i => {
          let children = res[`fill_Modules_By_ModuleID_${i.moduleID}`];
          return {
            ...i, children,
            crud_View: !children.some(j => j.crud_View == false),
            crud_Insert: !children.some(j => j.crud_Insert == false),
            crud_Update: !children.some(i => i.crud_Update == false),
            crud_Authorize: !children.some(i => i.crud_Authorize == false),
            crud_Reject: !children.some(i => i.crud_Reject == false),
            crud_Delete: !children.some(i => i.crud_Delete == false),
          }
        });
        console.log("all Data", this.allData);
        //this.dataMappting()
      }

    })
    this.alertService.stopLoadingMessage();
  }


  dataMappting() {
    this.selectAllParent = true;
    this.allData.forEach((item) => {
      item.crud_View = !item.children.some(i => i.crud_View == false);
      item.crud_Insert = !item.children.some(i => i.crud_Insert == false);
      item.crud_Update = !item.children.some(i => i.crud_Update == false);
      item.crud_Authorize = !item.children.some(i => i.crud_Authorize == false);
      item.crud_Reject = !item.children.some(i => i.crud_Reject == false);
      item.crud_Delete = !item.children.some(i => i.crud_Delete == false);
      if (!item.crud_View || !item.crud_Insert || !item.crud_Update || item.crud_Authorize || !item.crud_Reject || !item.crud_Delete)
        this.selectAllParent = false;
    })
  }

  ngOnDestroy() {
    this.saveToDisk();
  }

  resetForm(replace = false) {
    // this.isChangePassword = false;

    // if (!replace) {
    //   this.form.reset();
    // } else {
    //   this.formResetToggle = false;

    //   setTimeout(() => {
    //     this.formResetToggle = true;
    //   });
    // }
  }




  fetch(cb) {
    let data = this.getFromDisk();

    if (data == null) {
      setTimeout(() => {

        data = this.getFromDisk();

        if (data == null) {
          data = [
            { completed: true, important: true, name: 'Create visual studio extension', description: 'Create a visual studio VSIX extension package that will add this project as an aspnet-core project template' },
            { completed: false, important: true, name: 'Do a quick how-to writeup', description: '' },
            {
              completed: false, important: false, name: 'Create aspnet-core/Angular tutorials based on this project', description: 'Create tutorials (blog/video/youtube) on how to build applications (full stack)' +
                ' using aspnet-core/Angular. The tutorial will focus on getting productive with the technology right away rather than the details on how and why they work so audience can get onboard quickly.'
            },
          ];
        }

        cb(data);
      }, 1000);
    } else {
      cb(data);
    }
  }


  refreshDataIndexes(data) {
    let index = 0;

    for (const i of data) {
      i.$$index = index++;
    }
  }


  onSearchChanged(value: string) {
    this.rows = this.rowsCache.filter(r =>
      Utilities.searchArray(value, false, r.name, r.description) ||
      value === 'important' && r.important ||
      value === 'not important' && !r.important);
  }


  showErrorAlert(caption: string, message: string) {
    this.alertService.showMessage(caption, message, MessageSeverity.error);
  }


  addTask() {
    this.formResetToggle = false;

    setTimeout(() => {
      this.formResetToggle = true;

      this.group = {};
      this.editorModal.show();
    });
  }

  checkGroupName(name) {
    this.accountService.checkGroupName(name).subscribe((res: any) => {
      if (res) {
        return this.alertService.showMessage("Alert", "Group Name Aleady Exist", MessageSeverity.error);
      }
    })
  }

  loadCountryViewModel() {
    this.accountService.getCountryRegionUserGroup()
      .subscribe(results => {
        this.countries = results.listCountryViewModel;
        //console.log("CountryViewModel" + this.UserViewModel);      
      });
  }

  reqObjectMapper<T>(): Observable<Group> {

    let ob: Group = {
      groupID: 0,
      groupName: this.group.name,
      groupDescription: this.group.description,
      makerStatus: "string",
      isActive: this.group.isActive,
      action: "string",
      reason: "string",
      createdBy: 1,
      createdDate: new Date(),
      updatedBy: 2,
      updatedDate: new Date(),
      groupCheckerID: 0,
      checkerStatus: "string",
      checkerActive: true,
      makerID: "string",
      makerDate:  new Date(),
      checkerID: "string",
      countryCode: this.group.countryId,
      checkerDate: new Date(),
      reference: this.group.reference,
      groupOwnerPSID: this.group.ownerPSID,
      groupOwnerName: this.group.ownerName,
      psid: "",
      moduleVMList: []
    };
    let moduleObject = {
      moduleID: 0,
      applicationID: 0,
      moduleName: "",
      moduleDescription: "",
      moduleIcon: "",
      moduleSortOrder: 0,
      createdBy: 0,
      updatedBy: 0,
      createdDate: new Date(),
      updatedDate: new Date(),
      backColor: false,
      chkAllSelect: true,
      chkView: false,
      chkInsert: false,
      chkUpdate: false,
      chkAuthorize: false,
      chkReject: false,
      chkDelete: false,
      active: false
    };
    this.allData.forEach((el) => {
      moduleObject.moduleID = el.moduleID,
        moduleObject.applicationID = 0,
        moduleObject.moduleName = el.moduleName,
        moduleObject.moduleDescription = el.moduleDescription,
        moduleObject.moduleIcon = el.moduleIcon,
        moduleObject.moduleSortOrder = el.moduleSortOrder,
        moduleObject.backColor = true,
        moduleObject.chkAllSelect = true,
        moduleObject.chkView = el.crud_View,
        moduleObject.chkInsert = el.crud_Insert,
        moduleObject.chkUpdate = el.crud_Update,
        moduleObject.chkAuthorize = el.crud_Authorize,
        moduleObject.chkReject = el.crud_Reject,
        moduleObject.chkDelete = el.crud_Delete,
        moduleObject.active = true
        moduleObject.createdBy = 1
        moduleObject.updatedBy = 1
        moduleObject.createdDate = new Date(),
        moduleObject.updatedDate = new Date(),
      ob.moduleVMList.push(moduleObject);
      let moduleChildrenName = `fill_Modules_By_ModuleID_${el.moduleID}`;
      ob[moduleChildrenName] = el.children;
    });

    return of(ob);
  }

  save(form) {
    this.reqObjectMapper().subscribe(req => {
      console.log("req",req);
      this.accountService.saveGroup(req).subscribe((res: any) => {
        if (res) {
          return this.alertService.showMessage("Alert", "Group Name Aleady Exist", MessageSeverity.error);
        }
      })
    })
    //this.saveToDisk();
    this.editorModal.hide();
  }


  updateValue(event, cell, cellValue, row) {
    this.editing[row.$$index + '-' + cell] = false;
    this.rows[row.$$index][cell] = event.target.value;
    this.rows = [...this.rows];

    this.saveToDisk();
  }


  delete(row) {
    this.alertService.showDialog('Are you sure you want to delete the task?', DialogType.confirm, () => this.deleteHelper(row));
  }


  deleteHelper(row) {
    this.rowsCache = this.rowsCache.filter(item => item !== row);
    this.rows = this.rows.filter(item => item !== row);

    this.saveToDisk();
  }

  getFromDisk() {
    return this.localStorage.getDataObject(`${TodoDemoComponent.DBKeyTodoDemo}:${this.currentUserId}`);
  }

  saveToDisk() {
    if (this.isDataLoaded) {
      this.localStorage.saveSyncedSessionData(this.rowsCache, `${TodoDemoComponent.DBKeyTodoDemo}:${this.currentUserId}`);
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
    })
  }


  editGroup(group: CreateOrEditGroup) {


  }


  cancel() {

  }

  handleSingleParentChange(item, event, parent) {
    item.children.map((el) => {
      el[parent] = event.checked;
    })
  }


  handleModuleParent(item, event) {
    this.handlePageParent(item, event);
    item.children.map((el) => {
      this.handlePageParent(el, event);
    })
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
      if (!item.crud_View || !item.crud_Insert || !item.crud_Update || !item.crud_Authorize || !item.crud_Reject || !item.crud_Delete)
        parentCheck = false;
      return;
    })
    return parentCheck;
  }

  checkIfAllChildren(data, prop) {
    let parentCheck = true;
    data.children.forEach((item: any) => {
      if (!item[prop])
        parentCheck = false;
      return;
    })
    return parentCheck;
  }


}



