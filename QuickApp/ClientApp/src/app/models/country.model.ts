export class Country {
    // Note: Using only optional constructor properties without backing store disables typescript's type checking for the type
    constructor(id?: string, countryCode?: string, countryName?: string) {

        this.id = id;
        this.countryCode = countryCode;
        this.countryName = countryName;
        
    }

    public id: string;
    public countryCode: string;
    public countryName: string;    
}
