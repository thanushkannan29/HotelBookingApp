import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { AppConfig } from '../../app.config';
import { tap } from 'rxjs';
import { JwtHelperService } from '@auth0/angular-jwt';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private tokenKey = 'token';
  private jwtHelper = new JwtHelperService();

  user = signal<any>(null);

  constructor(private http: HttpClient) {
    this.loadUserFromToken();
  }

  // LOGIN 
  login(data: any) {
    return this.http.post(`${AppConfig.apiBaseUrl}/auth/login`, data)
      .pipe(tap((res: any) => this.handleAuth(res)));
  }

  //  REGISTER GUEST 
  registerGuest(data: any) {
    return this.http.post(`${AppConfig.apiBaseUrl}/auth/register-guest`, data)
      .pipe(tap((res: any) => this.handleAuth(res)));
  }

  // REGISTER ADMIN 
  registerAdmin(data: any) {
    return this.http.post(`${AppConfig.apiBaseUrl}/auth/register-hotel-admin`, data)
      .pipe(tap((res: any) => this.handleAuth(res)));
  }

  // HANDLE TOKEN 
  private handleAuth(res: any) {
    const token = res.data.token;

    if (!token) return;

    localStorage.setItem(this.tokenKey, token);

    const decoded: any = this.jwtHelper.decodeToken(token); 
    this.user.set(decoded);
  }

  //  GET TOKEN 
  getToken() {
    return localStorage.getItem(this.tokenKey);
  }

  // LOGOUT 
  logout() {
    localStorage.removeItem(this.tokenKey);
    this.user.set(null);
  }

  //  CHECK LOGIN
  isLoggedIn(): boolean {
    const token = this.getToken();
    return token != null && !this.jwtHelper.isTokenExpired(token);
  }

  // GET ROLE
  getRole() {
    return this.user()?.role;
  }

  // LOAD USER 
  private loadUserFromToken() {
    const token = this.getToken();

    if (token && !this.jwtHelper.isTokenExpired(token)) {
      const decoded: any = this.jwtHelper.decodeToken(token);
      this.user.set(decoded);
    }
  }
}
