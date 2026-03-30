import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
    return this.http.post<ApiResponse<PagedSupportRequestResponseDto>>(
      `${this.base}/guest/support/list`, { page, pageSize }
    ).pipe(map(r => r.data!));
  }

  // ── Admin ─────────────────────────────────────────────────────────────────
  submitAdmin(dto: AdminSupportRequestDto): Observable<SupportRequestResponseDto> {
    return this.http.post<ApiResponse<SupportRequestResponseDto>>(
      `${this.base}/admin/support`, dto
    ).pipe(map(r => r.data!));
  }

  getAdminRequests(page = 1, pageSize = 10): Observable<PagedSupportRequestResponseDto> {
    return this.http.post<ApiResponse<PagedSupportRequestResponseDto>>(
      `${this.base}/admin/support/list`, { page, pageSize }
    ).pipe(map(r => r.data!));
  }

  // ── SuperAdmin ────────────────────────────────────────────────────────────
  getAll(status = 'All', role = 'All', search = '', page = 1, pageSize = 10): Observable<PagedSupportRequestResponseDto> {
    return this.http.post<ApiResponse<PagedSupportRequestResponseDto>>(
      `${this.base}/superadmin/support/list`, { status, role, search, page, pageSize }
    ).pipe(map(r => r.data!));
  }

  respond(id: string, dto: RespondSupportRequestDto): Observable<SupportRequestResponseDto> {
    return this.http.patch<ApiResponse<SupportRequestResponseDto>>(
      `${this.base}/superadmin/support/${id}/respond`, dto
    ).pipe(map(r => r.data!));
  }
}
