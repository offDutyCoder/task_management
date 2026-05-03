import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  RecurringTemplate,
  CreateRecurringTemplateRequest,
  UpdateRecurringTemplateRequest,
} from '../models/recurring-template.model';

@Injectable({ providedIn: 'root' })
export class RecurringTemplateService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<RecurringTemplate[]> {
    return this.http.get<RecurringTemplate[]>('/api/recurring-templates', { withCredentials: true });
  }

  getById(id: number): Observable<RecurringTemplate> {
    return this.http.get<RecurringTemplate>(`/api/recurring-templates/${id}`, { withCredentials: true });
  }

  create(request: CreateRecurringTemplateRequest): Observable<RecurringTemplate> {
    return this.http.post<RecurringTemplate>('/api/recurring-templates', request, { withCredentials: true });
  }

  update(id: number, request: UpdateRecurringTemplateRequest): Observable<RecurringTemplate> {
    return this.http.put<RecurringTemplate>(`/api/recurring-templates/${id}`, request, { withCredentials: true });
  }

  deactivate(id: number): Observable<void> {
    return this.http.delete<void>(`/api/recurring-templates/${id}`, { withCredentials: true });
  }
}
