import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, PagedWalletTransactionDto, WalletResponseDto, TopUpWalletDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getWallet(page = 1, pageSize = 10): Observable<PagedWalletTransactionDto> {
    return this.http.post<ApiResponse<PagedWalletTransactionDto>>(
      `${this.base}/guest/wallet/list`, { page, pageSize }
    ).pipe(map(r => r.data!));
  }

  topUp(dto: TopUpWalletDto): Observable<WalletResponseDto> {
    return this.http.post<ApiResponse<WalletResponseDto>>(
      `${this.base}/guest/wallet/topup`, dto
    ).pipe(map(r => r.data!));
  }
}
