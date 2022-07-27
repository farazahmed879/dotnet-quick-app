export class DomainVM {
    // Note: Using only optional constructor properties without backing store disables typescript's type checking for the type
    constructor(name?: string,domain?: string) {

        this.name = name;
        this.domain = domain;
    }    
    public name: string;
    public domain: string;    
}
