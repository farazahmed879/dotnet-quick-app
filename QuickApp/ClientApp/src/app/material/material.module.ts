
import { NgModule } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import {MatTabsModule} from '@angular/material/tabs';
import {MatTableModule} from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';

@NgModule({
  exports: [
    MatCheckboxModule,
    MatTabsModule,
    MatTableModule,
    MatPaginatorModule
  ]
})
export class MaterialModule { }