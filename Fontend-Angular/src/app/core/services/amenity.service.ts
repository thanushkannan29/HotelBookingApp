import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, AmenityResponseDto, CreateAmenityDto, UpdateAmenityDto, PagedAmenityResponseDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class AmenityService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/superadmin/amenities`;

  getAllPaged(page: number, pageSize: number, search?: string, category?: string): Observable<PagedAmenityResponseDto> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (category && category !== 'All') params = params.set('category', category);
    return this.http.get<ApiResponse<PagedAmenityResponseDto>>(this.base, { params })
      .pipe(map(r => r.data!));
  }

  create(dto: CreateAmenityDto): Observable<AmenityResponseDto> {
    return this.http.post<ApiResponse<AmenityResponseDto>>(this.base, dto)
      .pipe(map(r => r.data!));
  }

  update(dto: UpdateAmenityDto): Observable<AmenityResponseDto> {
    return this.http.put<ApiResponse<AmenityResponseDto>>(this.base, dto)
      .pipe(map(r => r.data!));
  }

  toggleStatus(id: string): Observable<{ isActive: boolean }> {
    return this.http.patch<ApiResponse<{ isActive: boolean }>>(`${this.base}/${id}/toggle-status`, {})
      .pipe(map(r => r.data!));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<any>(`${this.base}/${id}`).pipe(map(() => undefined));
  }
}
