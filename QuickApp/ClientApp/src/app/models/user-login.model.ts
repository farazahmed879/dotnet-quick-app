export class UserLogin {
    constructor(userName?: string, password?: string, rememberMe?: boolean,domain?: string) {
        this.userName = userName;
        this.password = password;
        this.rememberMe = rememberMe;
        this.domain = domain;
    }

    userName: string;
    password: string;
    rememberMe: boolean;
    domain: string
}
