import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { EndpointBase } from './endpoint-base.service';
import { ConfigurationService } from './configuration.service';


@Injectable()
export class AccountEndpoint extends EndpointBase {

  get usersUrl() { return this.configurations.baseUrl + '/api/account/users'; }
  get userByUserNameUrl() { return this.configurations.baseUrl + '/api/account/users/username'; }
  get currentUserUrl() { return this.configurations.baseUrl + '/api/account/users/me'; }
  get currentUserPreferencesUrl() { return this.configurations.baseUrl + '/api/account/users/me/preferences'; }
  get domains() { return this.configurations.baseUrl + '/api/account/domains'; }
  get countries() { return this.configurations.baseUrl + '/api/account/GetCountryRegionUserGroup'; }
  get unblockUserUrl() { return this.configurations.baseUrl + '/api/account/users/unblock'; }
  get rolesUrl() { return this.configurations.baseUrl + '/api/account/roles'; }
  get roleByRoleNameUrl() { return this.configurations.baseUrl + '/api/account/roles/name'; }
  get permissionsUrl() { return this.configurations.baseUrl + '/api/account/permissions'; }
  get userManagementGrid() { return this.configurations.baseUrl + '/api/account/UserManagementGridData'; }
  get modulesUrl() { return this.configurations.baseUrl + '/api/Account/GroupManagementGridData'; }
  get getGroupUrl() { return this.configurations.baseUrl + '/api/Account/GroupManagementGridData/null/0'; }
  get saveGroupUrl() { return this.configurations.baseUrl + '/api/Account/SaveGroup'; }
  get groupByUserNameUrl() { return this.configurations.baseUrl + '/api/account/users/username'; }

  constructor(private configurations: ConfigurationService, http: HttpClient, authService: AuthService) {
    super(http, authService);
  }


  getUserEndpoint<T>(userId?: string): Observable<T> {
    const endpointUrl = userId ? `${this.usersUrl}/${userId}` : this.currentUserUrl;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUserEndpoint(userId));
      }));
  }


  getUserByUserNameEndpoint<T>(userName: string): Observable<T> {
    const endpointUrl = `${this.userByUserNameUrl}/${userName}`;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUserByUserNameEndpoint(userName));
      }));
  }

  getGroupByUserNameEndpoint<T>(userName: string): Observable<T> {
    const endpointUrl = `${this.groupByUserNameUrl}/${userName}`;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getGroupByUserNameEndpoint(userName));
      }));
  }


  getUsersEndpoint<T>(page?: number, pageSize?: number): Observable<T> {
    const endpointUrl = page && pageSize ? `${this.usersUrl}/${page}/${pageSize}` : this.usersUrl;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUsersEndpoint(page, pageSize));
      }));
  }

  getDomains<T>(): Observable<T> {
    return this.http.get<T>(this.domains, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getDomains());
      }));
  }

  getUserManagementGridData<T>(UserID?: number, StatusID?: number): Observable<T> {
    const endpointUrl = UserID && StatusID ? `${this.userManagementGrid}/${UserID}/${StatusID}` : UserID ? `${this.userManagementGrid}/${UserID}` : `${this.userManagementGrid}/${StatusID}` ? this.userManagementGrid : this.userManagementGrid;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUserManagementGridData(UserID, StatusID));
      }));
  }


  getCountryRegionUserGroup<T>(): Observable<T> {
    return this.http.get<T>(this.countries, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getCountryRegionUserGroup());
      }));
  }



  getNewUserEndpoint<T>(userObject: any): Observable<T> {

    return this.http.post<T>(this.usersUrl, JSON.stringify(userObject), this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getNewUserEndpoint(userObject));
      }));
  }

  getUpdateUserEndpoint<T>(userObject: any, userId?: string): Observable<T> {
    const endpointUrl = userId ? `${this.usersUrl}/${userId}` : this.currentUserUrl;

    return this.http.put<T>(endpointUrl, JSON.stringify(userObject), this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUpdateUserEndpoint(userObject, userId));
      }));
  }

  getPatchUpdateUserEndpoint<T>(patch: {}, userId?: string): Observable<T>;
  getPatchUpdateUserEndpoint<T>(value: any, op: string, path: string, from?: any, userId?: string): Observable<T>;
  getPatchUpdateUserEndpoint<T>(valueOrPatch: any, opOrUserId?: string, path?: string, from?: any, userId?: string): Observable<T> {
    let endpointUrl: string;
    let patchDocument: {};

    if (path) {
      endpointUrl = userId ? `${this.usersUrl}/${userId}` : this.currentUserUrl;
      patchDocument = from ?
        [{ value: valueOrPatch, path, op: opOrUserId, from }] :
        [{ value: valueOrPatch, path, op: opOrUserId }];
    } else {
      endpointUrl = opOrUserId ? `${this.usersUrl}/${opOrUserId}` : this.currentUserUrl;
      patchDocument = valueOrPatch;
    }

    return this.http.patch<T>(endpointUrl, JSON.stringify(patchDocument), this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getPatchUpdateUserEndpoint(valueOrPatch, opOrUserId, path, from, userId));
      }));
  }


  getUserPreferencesEndpoint<T>(): Observable<T> {

    return this.http.get<T>(this.currentUserPreferencesUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUserPreferencesEndpoint());
      }));
  }

  getUpdateUserPreferencesEndpoint<T>(configuration: string): Observable<T> {
    return this.http.put<T>(this.currentUserPreferencesUrl, JSON.stringify(configuration), this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUpdateUserPreferencesEndpoint(configuration));
      }));
  }

  getUnblockUserEndpoint<T>(userId: string): Observable<T> {
    const endpointUrl = `${this.unblockUserUrl}/${userId}`;

    return this.http.put<T>(endpointUrl, null, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUnblockUserEndpoint(userId));
      }));
  }

  getDeleteUserEndpoint<T>(userId: string): Observable<T> {
    const endpointUrl = `${this.usersUrl}/${userId}`;

    return this.http.delete<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getDeleteUserEndpoint(userId));
      }));
  }





  getRoleEndpoint<T>(roleId: string): Observable<T> {
    const endpointUrl = `${this.rolesUrl}/${roleId}`;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getRoleEndpoint(roleId));
      }));
  }


  getRoleByRoleNameEndpoint<T>(roleName: string): Observable<T> {
    const endpointUrl = `${this.roleByRoleNameUrl}/${roleName}`;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getRoleByRoleNameEndpoint(roleName));
      }));
  }



  getRolesEndpoint<T>(page?: number, pageSize?: number): Observable<T> {
    const endpointUrl = page && pageSize ? `${this.rolesUrl}/${page}/${pageSize}` : this.rolesUrl;

    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getRolesEndpoint(page, pageSize));
      }));
  }

  getNewRoleEndpoint<T>(roleObject: any): Observable<T> {

    return this.http.post<T>(this.rolesUrl, JSON.stringify(roleObject), this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getNewRoleEndpoint(roleObject));
      }));
  }

  getUpdateRoleEndpoint<T>(roleObject: any, roleId: string): Observable<T> {
    const endpointUrl = `${this.rolesUrl}/${roleId}`;

    return this.http.put<T>(endpointUrl, JSON.stringify(roleObject), this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getUpdateRoleEndpoint(roleObject, roleId));
      }));
  }

  getDeleteRoleEndpoint<T>(roleId: string): Observable<T> {
    const endpointUrl = `${this.rolesUrl}/${roleId}`;

    return this.http.delete<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getDeleteRoleEndpoint(roleId));
      }));
  }


  getPermissionsEndpoint<T>(): Observable<T> {

    return this.http.get<T>(this.permissionsUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getPermissionsEndpoint());
      }));
  }


  getModulesEndpoint<T>(page?: number, pageSize?: number): Observable<T> {
    const endpointUrl = page && pageSize ? `${this.modulesUrl}/${page}/${pageSize}` : this.modulesUrl;
    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, () => this.getModulesEndpoint(page, pageSize));
      }));
  }
  getGroupEndpoint<T>(page?: number, pageSize?: number): Observable<T> {
    return this.http.get<T>(this.getGroupUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, null);
      }));
  }

  isGroupExist<T>(groupName: string): Observable<T> {
    const endpointUrl = `/api/Account/IsGroupExist/${groupName}/0`;
    return this.http.get<T>(endpointUrl, this.requestHeaders).pipe<T>(
      catchError(error => {
        return this.handleError(error, null);
      }));
  }

  saveGroup<T>(data: any): Observable<T> {
    return this.http.post<T>(this.saveGroupUrl, data, this.requestHeaders2).pipe<T>(
      catchError(error => {
        return this.handleError(error, null);
      }));
  }

  // saveGroup(data: any) {
  //   return this.http.post(this.saveGroupUrl, data, this.requestHeaders)
  // }
}
