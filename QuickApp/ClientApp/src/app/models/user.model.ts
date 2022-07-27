import { GridUserManagementVM } from "./gridUserManagementVM.model";

export class User {
    // Note: Using only optional constructor properties without backing store disables typescript's type checking for the type
    constructor(id?: string, userName?: string, fullName?: string, email?: string, jobTitle?: string, phoneNumber?: string, roles?: string[],
         accountOwner?: string, department?: string,country?: string, region?: string,userID?: number,groupID?: number,groupName?: string,countryCode?: string,
         regionID?: string,regionName?: string) {

        this.id = id;
        this.userName = userName;
        this.fullName = fullName;
        this.email = email;
        this.jobTitle = jobTitle;
        this.phoneNumber = phoneNumber;
        this.roles = roles;
        this.department = department;
        this.accountOwner = accountOwner;
        this.country = country;
        this.region = region;
        this.userID = userID;
        this.groupID= groupID;
        this.groupName= groupName;
        this.countryCode= countryCode;
        this.regionID= regionID;
        this.regionName= regionName;
    }


    get friendlyName(): string {
        let name = this.fullName || this.userName;

        if (this.jobTitle) {
            name = this.jobTitle + ' ' + name;
        }

        return name;
    }
    public id: string;
    public userName: string;
    public fullName: string;
    public email: string;
    public jobTitle: string;
    public phoneNumber: string;
    public accountOwner: string;
    public department: string;
    public country: string;
    public region: string;    
    public isEnabled: boolean;
    public isAD: boolean;
    public isLockedOut: boolean;
    public roles: string[];
    public userID: number;
    public groupID: number;
    public groupName: string;
    public countryCode: string;
    public regionID: string;
    public regionName: string;
    
    

    

    //public  GridUserManagementVM: GridUserManagementVM[];  
}
