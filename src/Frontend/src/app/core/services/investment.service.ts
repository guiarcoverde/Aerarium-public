import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateInvestmentRequest,
  InvestmentDto,
} from '../../models/investment';
import { PagedResult } from '../../models/transaction';

@Injectable({ providedIn: 'root' })
export class InvestmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/investments`;

  list(page: number, pageSize: number): Observable<PagedResult<InvestmentDto>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<InvestmentDto>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<InvestmentDto> {
    return this.http.get<InvestmentDto>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateInvestmentRequest): Observable<InvestmentDto> {
    return this.http.post<InvestmentDto>(this.baseUrl, request);
  }

  update(id: string, request: CreateInvestmentRequest): Observable<InvestmentDto> {
    return this.http.put<InvestmentDto>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
