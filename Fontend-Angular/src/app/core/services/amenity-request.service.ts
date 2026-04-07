import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, CreateAmenityRequestDto, AmenityRequestResponseDto, PagedAmenityRequestResponseDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class AmenityRequestService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  // Admin
  create(dto: CreateAmenityRequestDto): Observable<AmenityRequestResponseDto> {
    return this.http.post<ApiResponse<AmenityRequestResponseDto>>(
      `${this.base}/admin/amenity-requests`, dto
    ).pipe(map(r => r.data!));
  }

  getMine(page = 1, pageSize = 10, search?: string): Observable<PagedAmenityRequestResponseDto> {
    return this.http.post<ApiResponse<PagedAmenityRequestResponseDto>>(
      `${this.base}/admin/amenity-requests/list`, { page, pageSize, search }
    ).pipe(map(r => r.data!));
  }

  // SuperAdmin
  getAll(status = 'All', page = 1, pageSize = 10): Observable<PagedAmenityRequestResponseDto> {
    return this.http.post<ApiResponse<PagedAmenityRequestResponseDto>>(
      `${this.base}/superadmin/amenity-requests/list`, { status, page, pageSize }
    ).pipe(map(r => r.data!));
  }

  approve(id: string): Observable<AmenityRequestResponseDto> {
    return this.http.patch<ApiResponse<AmenityRequestResponseDto>>(
      `${this.base}/superadmin/amenity-requests/${id}/approve`, {}
    ).pipe(map(r => r.data!));
  }

  reject(id: string, note: string): Observable<AmenityRequestResponseDto> {
    return this.http.patch<ApiResponse<AmenityRequestResponseDto>>(
      `${this.base}/superadmin/amenity-requests/${id}/reject`, { note }
    ).pipe(map(r => r.data!));
  }
}
