import { HttpClient } from "@angular/common/http";

import { Injectable } from "@angular/core";
import { LoginModel } from "../authencation/Models/LoginModel";
import { RegisterGuestModel } from "../authencation/Models/Register-Guest-Model";
import { RegisterAdminModel } from "../authencation/Models/Register-Admin-Model";

@Injectable({
    providedIn: 'root'
})
export class APIAuthenactionService {
    constructor(private http: HttpClient) {
    }
    apiLogin(loginModel:LoginModel){
        return this.http.post('https://localhost:7208/api/Authentication/login', loginModel);
    }

    apiRegisterGuest(GuestRegisterModel:RegisterGuestModel){
        return this.http.post('https://localhost:7208/api/Authentication/register-guest', GuestRegisterModel);
    }

    apiRegisterAdminHotel(AdminHotelRegisterModel:RegisterAdminModel){
        return this.http.post('https://localhost:7208/api/Authentication/register-guest', AdminHotelRegisterModel);
    }
}