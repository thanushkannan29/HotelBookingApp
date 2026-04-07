import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, PagedRevenueResponseDto, RevenueSummaryDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class RevenueService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getAll(page = 1, pageSize = 20): Observable<PagedRevenueResponseDto> {
    return this.http.post<ApiResponse<PagedRevenueResponseDto>>(
      `${this.base}/superadmin/revenue/list`, { page, pageSize }
    ).pipe(map(r => r.data!));
  }

  getSummary(): Observable<RevenueSummaryDto> {
    return this.http.get<ApiResponse<RevenueSummaryDto>>(
      `${this.base}/superadmin/revenue/summary`
    ).pipe(map(r => r.data!));
  }
}
