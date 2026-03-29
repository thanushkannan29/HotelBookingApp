import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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

  getMyCodes(page = 1, pageSize = 10): Observable<PagedPromoCodeResponseDto> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedPromoCodeResponseDto>>(
      `${this.base}/guest/promo-codes`, { params }
    ).pipe(map(r => r.data!));
  }

  validate(dto: ValidatePromoCodeDto): Observable<PromoCodeValidationResultDto> {
    return this.http.post<ApiResponse<PromoCodeValidationResultDto>>(
      `${this.base}/guest/promo-codes/validate`, dto
    ).pipe(map(r => r.data!));
  }
}
