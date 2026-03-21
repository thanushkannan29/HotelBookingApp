import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, HotelListItemDto, HotelDetailsDto, SearchHotelRequestDto,
  SearchHotelResponseDto, RoomTypePublicDto, RoomAvailabilityDto,
  UpdateHotelDto, SuperAdminHotelListDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class HotelService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  // ── PUBLIC ────────────────────────────────────────────────────────────────
  getTopHotels(): Observable<HotelListItemDto[]> {
    return this.http.get<ApiResponse<HotelListItemDto[]>>(`${this.base}/public/hotels/top`)
      .pipe(map(r => r.data!));
  }

  getCities(): Observable<string[]> {
    return this.http.get<ApiResponse<string[]>>(`${this.base}/public/hotels/cities`)
      .pipe(map(r => r.data!));
  }

  getHotelsByCity(city: string): Observable<HotelListItemDto[]> {
    return this.http.get<ApiResponse<HotelListItemDto[]>>(
      `${this.base}/public/hotels/by-city`, { params: { city } }
    ).pipe(map(r => r.data!));
  }

  searchHotels(req: SearchHotelRequestDto): Observable<SearchHotelResponseDto> {
    return this.http.post<ApiResponse<SearchHotelResponseDto>>(
      `${this.base}/public/hotels/search`, req
    ).pipe(map(r => r.data!));
  }

  getHotelDetails(hotelId: string): Observable<HotelDetailsDto> {
    return this.http.get<ApiResponse<HotelDetailsDto>>(
      `${this.base}/public/hotels/${hotelId}/full-details`
    ).pipe(map(r => r.data!));
  }

  getRoomTypes(hotelId: string): Observable<RoomTypePublicDto[]> {
    return this.http.get<ApiResponse<RoomTypePublicDto[]>>(
      `${this.base}/public/hotels/${hotelId}/roomtypes`
    ).pipe(map(r => r.data!));
  }

  getAvailability(hotelId: string, checkIn: string, checkOut: string): Observable<RoomAvailabilityDto[]> {
    const params = new HttpParams().set('checkIn', checkIn).set('checkOut', checkOut);
    return this.http.get<ApiResponse<RoomAvailabilityDto[]>>(
      `${this.base}/public/hotels/${hotelId}/availability`, { params }
    ).pipe(map(r => r.data!));
  }

  // ── ADMIN ─────────────────────────────────────────────────────────────────
  updateHotel(dto: UpdateHotelDto): Observable<void> {
    return this.http.put<any>(`${this.base}/admin/hotels`, dto).pipe(map(() => undefined));
  }

  toggleHotelStatus(isActive: boolean): Observable<void> {
    return this.http.patch<any>(`${this.base}/admin/hotels/status`, {}, {
      params: { isActive: isActive.toString() }
    }).pipe(map(() => undefined));
  }

  // ── SUPERADMIN ────────────────────────────────────────────────────────────
  getAllHotelsForSuperAdmin(): Observable<SuperAdminHotelListDto[]> {
    return this.http.get<ApiResponse<SuperAdminHotelListDto[]>>(`${this.base}/superadmin/hotels`)
      .pipe(map(r => r.data!));
  }

  blockHotel(id: string): Observable<void> {
    return this.http.patch<any>(`${this.base}/superadmin/hotels/${id}/block`, {})
      .pipe(map(() => undefined));
  }

  unblockHotel(id: string): Observable<void> {
    return this.http.patch<any>(`${this.base}/superadmin/hotels/${id}/unblock`, {})
      .pipe(map(() => undefined));
  }
}
