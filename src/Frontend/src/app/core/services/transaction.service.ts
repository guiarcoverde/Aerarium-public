import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateTransactionRequest,
  PagedResult,
  TransactionCategory,
  TransactionDto,
  TransactionType,
} from '../../models/transaction';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/transactions`;

  create(request: CreateTransactionRequest): Observable<TransactionDto> {
    return this.http.post<TransactionDto>(this.baseUrl, request);
  }

  list(
    page: number,
    pageSize: number,
    month?: number | null,
    year?: number | null,
    type?: TransactionType | null,
    category?: TransactionCategory | null,
  ): Observable<PagedResult<TransactionDto>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (month != null) params = params.set('month', month);
    if (year != null) params = params.set('year', year);
    if (type != null) params = params.set('type', type);
    if (category != null) params = params.set('category', category);

    return this.http.get<PagedResult<TransactionDto>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<TransactionDto> {
    return this.http.get<TransactionDto>(`${this.baseUrl}/${id}`);
  }

  update(id: string, request: CreateTransactionRequest): Observable<TransactionDto> {
    return this.http.put<TransactionDto>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  deleteSeries(recurrenceGroupId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/series/${recurrenceGroupId}`,
    );
  }
}
