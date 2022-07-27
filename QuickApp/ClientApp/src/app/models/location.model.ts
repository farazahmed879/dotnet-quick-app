export class Location {
    // Note: Using only optional constructor properties without backing store disables typescript's type checking for the type
    constructor(id?: string, regionID?: string, regionName?: string) {

        this.id = id;
        this.regionID = regionID;
        this.regionName = regionName;
        
    }

    public id: string;
    public regionID: string;
    public regionName: string;    
}
