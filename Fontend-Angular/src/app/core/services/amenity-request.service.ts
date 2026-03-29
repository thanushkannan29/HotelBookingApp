import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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

  getMine(page = 1, pageSize = 10): Observable<PagedAmenityRequestResponseDto> {
    const params = new HttpParams()
      .set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedAmenityRequestResponseDto>>(
      `${this.base}/admin/amenity-requests`, { params }
    ).pipe(map(r => r.data!));
  }

  // SuperAdmin
  getAll(status = 'All', page = 1, pageSize = 10): Observable<PagedAmenityRequestResponseDto> {
    const params = new HttpParams()
      .set('status', status).set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedAmenityRequestResponseDto>>(
      `${this.base}/superadmin/amenity-requests`, { params }
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
