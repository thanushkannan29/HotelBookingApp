import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, PromoCodeResponseDto, PagedPromoCodeResponseDto,
  ValidatePromoCodeDto, PromoCodeValidationResultDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class PromoCodeService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getMyCodes(page = 1, pageSize = 10, status = 'All'): Observable<PagedPromoCodeResponseDto> {
    return this.http.post<ApiResponse<PagedPromoCodeResponseDto>>(
      `${this.base}/guest/promo-codes/list`, { page, pageSize, status }
    ).pipe(map(r => r.data!));
  }

  validate(dto: ValidatePromoCodeDto): Observable<PromoCodeValidationResultDto> {
    return this.http.post<ApiResponse<PromoCodeValidationResultDto>>(
      `${this.base}/guest/promo-codes/validate`, dto
    ).pipe(map(r => r.data!));
  }
}
