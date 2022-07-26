export class GridUserAuthorizationVM {
    // Note: Using only optional constructor properties without backing store disables typescript's type checking for the type
    constructor(userID?: number, groupID?: string, groupName?: string, PSID?: string, Name?: string, status?: string,
        statusID?: string, statusRequest?: string, department?: string, active?: string, action?: string,
        reason?: string, signatory?: string, authSignatory?: string, reference?: string, countryCode?: string,
        accountType?: string, accountDescription?: string, password?: string, regionID?: string, regionName?: string, createdBy? : string,
        createdDate? : string) {

        this.userID = userID;
        this.groupID = groupID;
        this.groupName = groupName;
        this.PSID = PSID;
        this.Name = Name;
        this.status = status;
        this.StatusID = statusID;
        this.statusRequest = statusRequest;
        this.groupName = groupName;
        this.groupID = groupID;
        this.groupName = groupName;
        this.groupID = groupID;
        this.groupName = groupName;
        this.groupID = groupID;
        this.groupName = groupName;
        this.createdBy = createdBy;
        this.createdDate = createdDate;
    }
    public userID: number;
    public groupID: string;
    public groupName: string;
    public PSID : string;
    public Name : string;
    public status : string;
    public  StatusID : string;
    public  statusRequest : string;
    public  department : string;
    public  active : boolean;
    public  action : string;
    public  reason : string;     
    public  signatory : string;
    public  authSignatory : boolean;
    public  reference : string;
    public  countryCode : string;
    public  accountType : string;
    public accountDescription : string;
    public password : string;    
    public regionID : string;
    public  regionName : string;
    public createdBy : string;
    public  createdDate : string;
}
