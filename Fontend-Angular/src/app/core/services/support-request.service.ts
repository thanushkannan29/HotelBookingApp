import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  PublicSupportRequestDto,
  GuestSupportRequestDto,
  AdminSupportRequestDto,
  SupportRequestResponseDto,
  PagedSupportRequestResponseDto,
  RespondSupportRequestDto,
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class SupportRequestService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  // ── Public ────────────────────────────────────────────────────────────────
  submitPublic(dto: PublicSupportRequestDto): Observable<SupportRequestResponseDto> {
    return this.http.post<ApiResponse<SupportRequestResponseDto>>(
      `${this.base}/support`, dto
    ).pipe(map(r => r.data!));
  }

  // ── Guest ─────────────────────────────────────────────────────────────────
  submitGuest(dto: GuestSupportRequestDto): Observable<SupportRequestResponseDto> {
    return this.http.post<ApiResponse<SupportRequestResponseDto>>(
      `${this.base}/guest/support`, dto
    ).pipe(map(r => r.data!));
  }

  getGuestRequests(page = 1, pageSize = 10): Observable<PagedSupportRequestResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedSupportRequestResponseDto>>(
      `${this.base}/guest/support`, { params }
    ).pipe(map(r => r.data!));
  }

  // ── Admin ─────────────────────────────────────────────────────────────────
  submitAdmin(dto: AdminSupportRequestDto): Observable<SupportRequestResponseDto> {
    return this.http.post<ApiResponse<SupportRequestResponseDto>>(
      `${this.base}/admin/support`, dto
    ).pipe(map(r => r.data!));
  }

  getAdminRequests(page = 1, pageSize = 10): Observable<PagedSupportRequestResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedSupportRequestResponseDto>>(
      `${this.base}/admin/support`, { params }
    ).pipe(map(r => r.data!));
  }

  // ── SuperAdmin ────────────────────────────────────────────────────────────
  getAll(status = 'All', role = 'All', search = '', page = 1, pageSize = 10): Observable<PagedSupportRequestResponseDto> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (status && status !== 'All') params = params.set('status', status);
    if (role && role !== 'All') params = params.set('role', role);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<PagedSupportRequestResponseDto>>(
      `${this.base}/superadmin/support`, { params }
    ).pipe(map(r => r.data!));
  }

  respond(id: string, dto: RespondSupportRequestDto): Observable<SupportRequestResponseDto> {
    return this.http.patch<ApiResponse<SupportRequestResponseDto>>(
      `${this.base}/superadmin/support/${id}/respond`, dto
    ).pipe(map(r => r.data!));
  }
}
