import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, CreatePaymentDto, RefundRequestDto, TransactionResponseDto,
  PagedTransactionResponseDto, CreateReviewDto, UpdateReviewDto,
  ReviewResponseDto, MyReviewsResponseDto, PagedReviewResponseDto,
  GetHotelReviewsRequestDto, RefundRequestResponseDto, ProcessRefundDto,
  UserProfileResponseDto, UpdateUserProfileDto, PagedBookingHistoryDto,
  PaginationDto, AdminDashboardDto, GuestDashboardDto, SuperAdminDashboardDto,
  PagedAuditLogResponseDto, PagedLogResponseDto,
  CreateRoomTypeDto, UpdateRoomTypeDto, RoomTypeListDto, CreateRoomTypeRateDto,
  UpdateRoomTypeRateDto, GetRateByDateRequestDto, CreateRoomDto, UpdateRoomDto,
  RoomListResponseDto, CreateInventoryDto, UpdateInventoryDto, InventoryResponseDto
} from '../models/models';

// ─── TRANSACTION SERVICE ──────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class TransactionService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  createPayment(dto: CreatePaymentDto): Observable<TransactionResponseDto> {
    return this.http.post<ApiResponse<TransactionResponseDto>>(
      `${this.base}/transactions`, dto
    ).pipe(map(r => r.data!));
  }

  directRefund(id: string, dto: RefundRequestDto): Observable<TransactionResponseDto> {
    return this.http.post<ApiResponse<TransactionResponseDto>>(
      `${this.base}/transactions/${id}/refund`, dto
    ).pipe(map(r => r.data!));
  }

  getTransactions(page: number, pageSize: number): Observable<PagedTransactionResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedTransactionResponseDto>>(
      `${this.base}/transactions`, { params }
    ).pipe(map(r => r.data!));
  }
}

// ─── REVIEW SERVICE ───────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class ReviewService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  addReview(dto: CreateReviewDto): Observable<ReviewResponseDto> {
    return this.http.post<ApiResponse<ReviewResponseDto>>(
      `${this.base}/reviews`, dto
    ).pipe(map(r => r.data!));
  }

  updateReview(id: string, dto: UpdateReviewDto): Observable<ReviewResponseDto> {
    return this.http.put<ApiResponse<ReviewResponseDto>>(
      `${this.base}/reviews/${id}`, dto
    ).pipe(map(r => r.data!));
  }

  deleteReview(id: string): Observable<void> {
    return this.http.delete<any>(`${this.base}/reviews/${id}`).pipe(map(() => undefined));
  }

  getHotelReviews(dto: GetHotelReviewsRequestDto): Observable<PagedReviewResponseDto> {
    return this.http.post<ApiResponse<PagedReviewResponseDto>>(
      `${this.base}/reviews/hotel`, dto
    ).pipe(map(r => r.data!));
  }

  getMyReviews(): Observable<MyReviewsResponseDto[]> {
    return this.http.get<ApiResponse<MyReviewsResponseDto[]>>(`${this.base}/reviews/my-reviews`)
      .pipe(map(r => r.data!));
  }
}

// ─── REFUND SERVICE ───────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class RefundService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getGuestRefundRequests(): Observable<RefundRequestResponseDto[]> {
    return this.http.get<ApiResponse<RefundRequestResponseDto[]>>(
      `${this.base}/guest/refund-requests`
    ).pipe(map(r => r.data!));
  }

  getHotelRefundRequests(): Observable<RefundRequestResponseDto[]> {
    return this.http.get<ApiResponse<RefundRequestResponseDto[]>>(
      `${this.base}/admin/refund-requests`
    ).pipe(map(r => r.data!));
  }

  approveRefund(id: string, dto: ProcessRefundDto): Observable<RefundRequestResponseDto> {
    return this.http.post<ApiResponse<RefundRequestResponseDto>>(
      `${this.base}/admin/refund-requests/${id}/approve`, dto
    ).pipe(map(r => r.data!));
  }

  rejectRefund(id: string, dto: ProcessRefundDto): Observable<RefundRequestResponseDto> {
    return this.http.post<ApiResponse<RefundRequestResponseDto>>(
      `${this.base}/admin/refund-requests/${id}/reject`, dto
    ).pipe(map(r => r.data!));
  }
}

// ─── USER SERVICE ─────────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getProfile(): Observable<UserProfileResponseDto> {
    return this.http.get<ApiResponse<UserProfileResponseDto>>(`${this.base}/user-profile`)
      .pipe(map(r => r.data!));
  }

  updateProfile(dto: UpdateUserProfileDto): Observable<UserProfileResponseDto> {
    return this.http.put<ApiResponse<UserProfileResponseDto>>(
      `${this.base}/user-profile`, dto
    ).pipe(map(r => r.data!));
  }

  getBookingHistory(dto: PaginationDto): Observable<PagedBookingHistoryDto> {
    return this.http.post<ApiResponse<PagedBookingHistoryDto>>(
      `${this.base}/user-profile/booking-history`, dto
    ).pipe(map(r => r.data!));
  }
}

// ─── DASHBOARD SERVICE ────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getAdminDashboard(): Observable<AdminDashboardDto> {
    return this.http.get<ApiResponse<AdminDashboardDto>>(`${this.base}/dashboard/admin`)
      .pipe(map(r => r.data!));
  }

  getGuestDashboard(): Observable<GuestDashboardDto> {
    return this.http.get<ApiResponse<GuestDashboardDto>>(`${this.base}/dashboard/guest`)
      .pipe(map(r => r.data!));
  }

  getSuperAdminDashboard(): Observable<SuperAdminDashboardDto> {
    return this.http.get<ApiResponse<SuperAdminDashboardDto>>(`${this.base}/dashboard/superadmin`)
      .pipe(map(r => r.data!));
  }
}

// ─── AUDIT LOG SERVICE ────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getAdminAuditLogs(page: number, pageSize: number): Observable<PagedAuditLogResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedAuditLogResponseDto>>(
      `${this.base}/admin/audit-logs`, { params }
    ).pipe(map(r => r.data!));
  }

  getAllAuditLogs(page: number, pageSize: number): Observable<PagedAuditLogResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedAuditLogResponseDto>>(
      `${this.base}/superadmin/audit-logs`, { params }
    ).pipe(map(r => r.data!));
  }
}

// ─── LOG SERVICE ──────────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class LogService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getMyLogs(page: number, pageSize: number): Observable<PagedLogResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedLogResponseDto>>(
      `${this.base}/logs/my-logs`, { params }
    ).pipe(map(r => r.data!));
  }

  getAllLogs(page: number, pageSize: number): Observable<PagedLogResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedLogResponseDto>>(
      `${this.base}/logs`, { params }
    ).pipe(map(r => r.data!));
  }
}

// ─── ROOM TYPE SERVICE ────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class RoomTypeService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getRoomTypes(): Observable<RoomTypeListDto[]> {
    return this.http.get<ApiResponse<RoomTypeListDto[]>>(`${this.base}/admin/roomtypes`)
      .pipe(map(r => r.data!));
  }

  addRoomType(dto: CreateRoomTypeDto): Observable<void> {
    return this.http.post<any>(`${this.base}/admin/roomtypes`, dto).pipe(map(() => undefined));
  }

  updateRoomType(dto: UpdateRoomTypeDto): Observable<void> {
    return this.http.put<any>(`${this.base}/admin/roomtypes`, dto).pipe(map(() => undefined));
  }

  toggleRoomTypeStatus(id: string, isActive: boolean): Observable<void> {
    return this.http.patch<any>(
      `${this.base}/admin/roomtypes/${id}/status`, {}, { params: { isActive: isActive.toString() } }
    ).pipe(map(() => undefined));
  }

  addRate(dto: CreateRoomTypeRateDto): Observable<void> {
    return this.http.post<any>(`${this.base}/admin/roomtypes/rate`, dto).pipe(map(() => undefined));
  }

  updateRate(dto: UpdateRoomTypeRateDto): Observable<void> {
    return this.http.put<any>(`${this.base}/admin/roomtypes/rate`, dto).pipe(map(() => undefined));
  }

  getRateByDate(dto: GetRateByDateRequestDto): Observable<number> {
    return this.http.post<ApiResponse<number>>(`${this.base}/admin/roomtypes/rate-by-date`, dto)
      .pipe(map(r => r.data!));
  }
}

// ─── ROOM SERVICE ─────────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class RoomService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getRooms(pageNumber: number, pageSize: number): Observable<RoomListResponseDto[]> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<ApiResponse<RoomListResponseDto[]>>(
      `${this.base}/admin/rooms`, { params }
    ).pipe(map(r => r.data!));
  }

  addRoom(dto: CreateRoomDto): Observable<void> {
    return this.http.post<any>(`${this.base}/admin/rooms`, dto).pipe(map(() => undefined));
  }

  updateRoom(dto: UpdateRoomDto): Observable<void> {
    return this.http.put<any>(`${this.base}/admin/rooms`, dto).pipe(map(() => undefined));
  }

  toggleRoomStatus(id: string, isActive: boolean): Observable<void> {
    return this.http.patch<any>(
      `${this.base}/admin/rooms/${id}/status`, {}, { params: { isActive: isActive.toString() } }
    ).pipe(map(() => undefined));
  }
}

// ─── INVENTORY SERVICE ────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class InventoryService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getInventory(roomTypeId: string, start: string, end: string): Observable<InventoryResponseDto[]> {
    const params = new HttpParams().set('roomTypeId', roomTypeId).set('start', start).set('end', end);
    return this.http.get<ApiResponse<InventoryResponseDto[]>>(
      `${this.base}/admin/inventory`, { params }
    ).pipe(map(r => r.data!));
  }

  addInventory(dto: CreateInventoryDto): Observable<void> {
    return this.http.post<any>(`${this.base}/admin/inventory`, dto).pipe(map(() => undefined));
  }

  updateInventory(dto: UpdateInventoryDto): Observable<void> {
    return this.http.put<any>(`${this.base}/admin/inventory`, dto).pipe(map(() => undefined));
  }
}
