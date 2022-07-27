import { Country } from "./country.model";
import { tblGroup } from "./tblGroup.model";

export class UserViewModel {    
    public  listCountryViewModel : Country[];
    public  listGroupViewModel: tblGroup[];
    public  listRegionViewModel: Location[];    
}