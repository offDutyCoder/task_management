import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardResponse } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getDashboard(includeRecurring = false): Observable<DashboardResponse> {
    const params = new HttpParams().set('includeRecurring', includeRecurring);
    return this.http.get<DashboardResponse>('/api/dashboard', { params, withCredentials: true });
  }
}
