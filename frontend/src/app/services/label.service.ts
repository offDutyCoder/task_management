import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateLabelRequest, Label, UpdateLabelRequest } from '../models/label.model';

@Injectable({ providedIn: 'root' })
export class LabelService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Label[]> {
    return this.http.get<Label[]>('/api/labels', { withCredentials: true });
  }

  create(request: CreateLabelRequest): Observable<Label> {
    return this.http.post<Label>('/api/labels', request, { withCredentials: true });
  }

  update(id: number, request: UpdateLabelRequest): Observable<Label> {
    return this.http.put<Label>(`/api/labels/${id}`, request, { withCredentials: true });
  }
}
