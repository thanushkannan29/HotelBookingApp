import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, PromoCodeResponseDto, ValidatePromoCodeDto, PromoCodeValidationResultDto
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class PromoCodeService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getMyCodes(): Observable<PromoCodeResponseDto[]> {
    return this.http.get<ApiResponse<PromoCodeResponseDto[]>>(
      `${this.base}/guest/promo-codes`
    ).pipe(map(r => r.data!));
  }

  validate(dto: ValidatePromoCodeDto): Observable<PromoCodeValidationResultDto> {
    return this.http.post<ApiResponse<PromoCodeValidationResultDto>>(
      `${this.base}/guest/promo-codes/validate`, dto
    ).pipe(map(r => r.data!));
  }
}
