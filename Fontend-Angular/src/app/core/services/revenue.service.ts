import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedRevenueResponseDto>>(
      `${this.base}/superadmin/revenue`, { params }
    ).pipe(map(r => r.data!));
  }

  getSummary(): Observable<RevenueSummaryDto> {
    return this.http.get<ApiResponse<RevenueSummaryDto>>(
      `${this.base}/superadmin/revenue/summary`
    ).pipe(map(r => r.data!));
  }
}
