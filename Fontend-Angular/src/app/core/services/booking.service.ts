import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, CreateReservationDto, ReservationResponseDto,
  ReservationDetailsDto, PagedReservationResponseDto,
  CancelReservationDto, AvailableRoomDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  // ── GUEST ─────────────────────────────────────────────────────────────────
  createReservation(dto: CreateReservationDto): Observable<ReservationResponseDto> {
    return this.http.post<ApiResponse<ReservationResponseDto>>(
      `${this.base}/guest/reservations`, dto
    ).pipe(map(r => r.data!));
  }

  getMyReservations(): Observable<ReservationDetailsDto[]> {
    return this.http.get<ApiResponse<ReservationDetailsDto[]>>(`${this.base}/guest/reservations`)
      .pipe(map(r => r.data!));
  }

  getMyReservationsHistory(page: number, pageSize: number): Observable<PagedReservationResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedReservationResponseDto>>(
      `${this.base}/guest/reservations/history`, { params }
    ).pipe(map(r => r.data!));
  }

  getReservationByCode(code: string): Observable<ReservationDetailsDto> {
    return this.http.get<ApiResponse<ReservationDetailsDto>>(
      `${this.base}/guest/reservations/${code}`
    ).pipe(map(r => r.data!));
  }

  cancelReservation(code: string, dto: CancelReservationDto): Observable<void> {
    return this.http.patch<any>(
      `${this.base}/guest/reservations/${code}/cancel`, dto
    ).pipe(map(() => undefined));
  }

  getAvailableRooms(
    hotelId: string, roomTypeId: string, checkIn: string, checkOut: string
  ): Observable<AvailableRoomDto[]> {
    const params = new HttpParams()
      .set('hotelId', hotelId).set('roomTypeId', roomTypeId)
      .set('checkIn', checkIn).set('checkOut', checkOut);
    return this.http.get<ApiResponse<AvailableRoomDto[]>>(
      `${this.base}/guest/reservations/available-rooms`, { params }
    ).pipe(map(r => r.data!));
  }

  // ── ADMIN ─────────────────────────────────────────────────────────────────
  getHotelReservations(page: number, pageSize: number): Observable<PagedReservationResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedReservationResponseDto>>(
      `${this.base}/admin/reservations`, { params }
    ).pipe(map(r => r.data!));
  }

  completeReservation(code: string): Observable<void> {
    return this.http.patch<any>(
      `${this.base}/admin/reservations/${code}/complete`, {}
    ).pipe(map(() => undefined));
  }
}
