import { Component, inject } from '@angular/core';
import { LoginModel } from './Models/LoginModel';
import { RegisterAdminModel } from './Models/Register-Admin-Model';
import { RegisterGuestModel } from './Models/Register-Guest-Model';
import { FormsModule } from '@angular/forms';
import { APIAuthenactionService } from '../Services/api.Authencation.Service';

@Component({
  selector: 'app-authencation',
  imports: [FormsModule],
  templateUrl: './authencation.html',
  styleUrl: './authencation.css',
})
export class Authencation {

  showGuest:boolean=false;
  showAdmin:boolean=false;
  loginModel:LoginModel;
  registerGuestModel:RegisterGuestModel;
  registerAdminHotelModel:RegisterAdminModel;


  private apiauthservice:APIAuthenactionService=inject(APIAuthenactionService);
  constructor(){
    this.loginModel=new LoginModel();
    this.registerGuestModel=new RegisterGuestModel();
    this.registerAdminHotelModel=new RegisterAdminModel();
  }
//max milling angular
  //login Fuction Below
  login(){
    console.log(this.loginModel);
    this.apiauthservice.apiLogin(this.loginModel).subscribe({
      next:(response:any)=>{
        if(response){
         // localStorage.setItem('token', response?.token);//local Storage

          sessionStorage.setItem('token',response?.token)//session storage

          alert('Login successful!');
          
        }
      },
      error:(error)=>{
        alert('Login failed: ' + error.message);
      },
      complete:()=>{
        console.log('Login request completed');
      }
    });
  }
  
  
  //Register Guest User Service Fuction Below

    RegisterGuest(){
    console.log(this.registerGuestModel);
    this.apiauthservice.apiRegisterGuest(this.registerGuestModel).subscribe({
      next:(response:any)=>{
        if(response){
         // localStorage.setItem('token', response?.token);//local Storage

          sessionStorage.setItem('token',response?.token)//session storage

          alert('Register Guest successful!');
          
        }
      },
      error:(error)=>{
        alert('Register failed: ' + error.message);
      },
      complete:()=>{
        console.log('Register request completed');
      }
    });
  }

      RegisterAdminHotel(){
    console.log(this.registerAdminHotelModel);
    this.apiauthservice.apiRegisterAdminHotel(this.registerAdminHotelModel).subscribe({
      next:(response:any)=>{
        if(response){
         // localStorage.setItem('token', response?.token);//local Storage

          sessionStorage.setItem('token',response?.token)//session storage

          alert('Register Admin Hotel successful!');
          
        }
      },
      error:(error)=>{
        alert('Register Admin Hotel failed: ' + error.message);
      },
      complete:()=>{
        console.log('Register Admin Hotel request completed');
      }
    });
  }

  reset(){
    this.loginModel = new LoginModel();
    this.registerGuestModel=new RegisterGuestModel();
    this.registerAdminHotelModel=new RegisterAdminModel();
  }


}
