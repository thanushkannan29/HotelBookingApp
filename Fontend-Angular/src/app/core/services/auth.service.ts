import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, map } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../environments/environment';
import {
  LoginDto, RegisterUserDto, RegisterHotelAdminDto,
  AuthResponseDto, JwtPayload, CurrentUser, ApiResponse
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'hotel_token';
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private _currentUser = signal<CurrentUser | null>(null);
  private _token = signal<string | null>(null);
  private _profileImageUrl = signal<string | null>(null);
  private _hotelImageUrl = signal<string | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly token = this._token.asReadonly();
  readonly profileImageUrl = this._profileImageUrl.asReadonly();
  readonly hotelImageUrl = this._hotelImageUrl.asReadonly();
  readonly isAuthenticated = computed(() => !!this._currentUser());
  readonly isGuest = computed(() => this._currentUser()?.role === 'Guest');
  readonly isAdmin = computed(() => this._currentUser()?.role === 'Admin');
  readonly isSuperAdmin = computed(() => this._currentUser()?.role === 'SuperAdmin');

  constructor() {
    this.loadFromStorage();
  }

  private loadFromStorage(): void {
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (token) {
      try {
        const payload = jwtDecode<JwtPayload>(token);
        if (payload.exp * 1000 > Date.now()) {
          this._token.set(token);
          this._currentUser.set(this.payloadToUser(payload));
        } else {
          this.clearStorage();
        }
      } catch {
        this.clearStorage();
      }
    }
  }

  private payloadToUser(payload: JwtPayload): CurrentUser {
    return {
      userId: payload.nameid,
      userName: payload.unique_name,
      role: payload.role as CurrentUser['role'],
      hotelId: payload.HotelId,
    };
  }

  login(dto: LoginDto): Observable<AuthResponseDto> {
    return this.http.post<ApiResponse<AuthResponseDto>>(
      `${environment.apiUrl}/auth/login`, dto
    ).pipe(map(r => r.data!), tap(res => this.setToken(res.token)));
  }

  registerGuest(dto: RegisterUserDto): Observable<AuthResponseDto> {
    return this.http.post<ApiResponse<AuthResponseDto>>(
      `${environment.apiUrl}/auth/register-guest`, dto
    ).pipe(map(r => r.data!), tap(res => this.setToken(res.token)));
  }

  registerHotelAdmin(dto: RegisterHotelAdminDto): Observable<AuthResponseDto> {
    return this.http.post<ApiResponse<AuthResponseDto>>(
      `${environment.apiUrl}/auth/register-hotel-admin`, dto
    ).pipe(map(r => r.data!), tap(res => this.setToken(res.token)));
  }

  private setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this._token.set(token);
    const payload = jwtDecode<JwtPayload>(token);
    this._currentUser.set(this.payloadToUser(payload));
  }

  logout(): void {
    this.clearStorage();
    this.router.navigate(['/auth/login']);
  }

  /** Call after a profile update so the navbar/dashboard reflect the new name immediately */
  updateUserName(newName: string): void {
    const user = this._currentUser();
    if (user) this._currentUser.set({ ...user, userName: newName });
  }

  /** Call after profile load/update to sync profile image in navbar */
  updateProfileImage(url: string | null): void {
    this._profileImageUrl.set(url);
  }

  /** Call after admin dashboard load to sync hotel image in navbar */
  updateHotelImage(url: string | null): void {
    this._hotelImageUrl.set(url);
  }

  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this._token.set(null);
    this._currentUser.set(null);
  }

  getRedirectUrl(): string {
    const role = this._currentUser()?.role;
    if (role === 'Admin') return '/admin/dashboard';
    if (role === 'SuperAdmin') return '/superadmin/dashboard';
    return '/guest/dashboard';
  }
}
