export class tblGroup {
    // Note: Using only optional constructor properties without backing store disables typescript's type checking for the type
    constructor(grouP_ID?: number, grouP_NAME?: string) {
        
        this.grouP_ID = grouP_ID;
        this.grouP_NAME = grouP_NAME;
        
    }

    
    public grouP_ID: number;
    public grouP_NAME: string;    
}
