import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, CityDto, CreateCityDto, PagedCityResponseDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class CityService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  search(search: string): Observable<CityDto[]> {
    const params = new HttpParams().set('search', search);
    return this.http.get<ApiResponse<CityDto[]>>(
      `${this.base}/public/cities`, { params }
    ).pipe(map(r => r.data!));
  }

  getAll(): Observable<CityDto[]> {
    return this.http.get<ApiResponse<CityDto[]>>(
      `${this.base}/public/cities/all`
    ).pipe(map(r => r.data!));
  }

  // SuperAdmin
  getAllPaged(page = 1, pageSize = 10, search?: string): Observable<PagedCityResponseDto> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<PagedCityResponseDto>>(
      `${this.base}/superadmin/cities`, { params }
    ).pipe(map(r => r.data!));
  }

  add(dto: CreateCityDto): Observable<CityDto> {
    return this.http.post<ApiResponse<CityDto>>(
      `${this.base}/superadmin/cities`, dto
    ).pipe(map(r => r.data!));
  }

  update(id: string, dto: CreateCityDto): Observable<CityDto> {
    return this.http.put<ApiResponse<CityDto>>(
      `${this.base}/superadmin/cities/${id}`, dto
    ).pipe(map(r => r.data!));
  }

  toggleStatus(id: string): Observable<{ isActive: boolean }> {
    return this.http.patch<ApiResponse<{ isActive: boolean }>>(
      `${this.base}/superadmin/cities/${id}/status`, {}
    ).pipe(map(r => r.data!));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<any>(
      `${this.base}/superadmin/cities/${id}`
    ).pipe(map(() => undefined));
  }
}
