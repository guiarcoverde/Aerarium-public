import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardSummaryDto } from '../../models/transaction';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getSummary(month: number, year: number): Observable<DashboardSummaryDto> {
    const params = new HttpParams()
      .set('month', month)
      .set('year', year);

    return this.http.get<DashboardSummaryDto>(
      `${environment.apiUrl}/dashboard/summary`,
      { params },
    );
  }
}
