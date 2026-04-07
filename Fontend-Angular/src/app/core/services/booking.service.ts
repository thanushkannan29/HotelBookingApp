import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, CreateReservationDto, ReservationResponseDto,
  ReservationDetailsDto, PagedReservationResponseDto,
  CancelReservationDto, AvailableRoomDto, QrPaymentResponseDto,
  ValidatePromoCodeDto, PromoCodeValidationResultDto
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

  getMyReservationsHistory(page: number, pageSize: number, status?: string, search?: string): Observable<PagedReservationResponseDto> {
    return this.http.post<ApiResponse<PagedReservationResponseDto>>(
      `${this.base}/guest/reservations/history`, { page, pageSize, status, search }
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

  getPaymentQr(reservationId: string): Observable<QrPaymentResponseDto> {
    return this.http.get<ApiResponse<QrPaymentResponseDto>>(
      `${this.base}/guest/payment/qr/${reservationId}`
    ).pipe(map(r => r.data!));
  }

  validatePromoCode(dto: ValidatePromoCodeDto): Observable<PromoCodeValidationResultDto> {
    return this.http.post<ApiResponse<PromoCodeValidationResultDto>>(
      `${this.base}/guest/promo-codes/validate`, dto
    ).pipe(map(r => r.data!));
  }

  // ── ADMIN ─────────────────────────────────────────────────────────────────
  getHotelReservations(
    page: number, pageSize: number,
    status?: string, search?: string,
    sortField?: string, sortDir?: string
  ): Observable<PagedReservationResponseDto> {
    return this.http.post<ApiResponse<PagedReservationResponseDto>>(
      `${this.base}/admin/reservations/list`, { page, pageSize, status, search, sortField, sortDir }
    ).pipe(map(r => r.data!));
  }

  completeReservation(code: string): Observable<void> {
    return this.http.patch<any>(
      `${this.base}/admin/reservations/${code}/complete`, {}
    ).pipe(map(() => undefined));
  }

  confirmReservation(code: string): Observable<void> {
    return this.http.patch<any>(
      `${this.base}/admin/reservations/${code}/confirm`, {}
    ).pipe(map(() => undefined));
  }

  recordFailedPayment(reservationId: string): Observable<void> {
    return this.http.post<any>(
      `${this.base}/transactions/${reservationId}/record-failed`, {}
    ).pipe(map(() => undefined));
  }
}
