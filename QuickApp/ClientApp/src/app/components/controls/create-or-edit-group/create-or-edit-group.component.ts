import { Component, OnInit, OnDestroy, Input, TemplateRef, ViewChild } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { CreateOrEditGroup } from 'src/app/models/createGroup.model';
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
  group: any = {};
  isDataLoaded = false;
  loadingIndicator = true;
  isSaving = false;
  formResetToggle = true;
  _currentUserId: string;
  _hideCompletedTasks = false;
  allData: any;
  modulesList: any;

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
  }


  getAllGroups() {
    this.alertService.startLoadingMessage();
    this.accountService.getModules().subscribe((res: any) => {
      if (res) {
        this.allData = res;
        this.modulesList = res.moduleList;// this.modalData = this.data;
        this.modulesList.forEach((element: any) => {
          element.view = false;
          element.insert = false;
          element.update = false;
          element.authorize = false;
          element.reject = false;
          element.delete = false;
        });
      }
    })
    this.alertService.stopLoadingMessage();
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

  }

  save(form) {
    this.accountService.checkGroupName(form.value.groupName).subscribe((res: any) => {
      if (res) {
        return this.alertService.showMessage("Alert", "Group Name Aleady Exist", MessageSeverity.error);
      }
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


  handleCheckBoxChange(event, item) {
    if (event.checked) {
      item.view = true;
      item.insert = true;
      item.update = true;
      item.authorize = true;
      item.reject = true;
      item.delete = true;
      return;
    }

    item.view = false;
    item.insert = false;
    item.update = false;
    item.authorize = false;
    item.reject = false;
    item.delete = false;

  }


  handleSelectAll(event) {
    this.modulesList.forEach((el: any) => {
      this.handleCheckBoxChange(event, el);
    })
  }


  editGroup(group: CreateOrEditGroup) {


  }


  getModulePages(id: string) {
    let name = "fill_Modules_By_ModuleID_"
    let result = name.concat(id);
    let list = this.allData[result];
    return list;
  }

  cancel(){
    
  }


}



