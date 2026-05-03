import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateStatusRequest, TaskStatus, UpdateStatusRequest } from '../models/task-status.model';

@Injectable({ providedIn: 'root' })
export class StatusService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<TaskStatus[]> {
    return this.http.get<TaskStatus[]>('/api/statuses', { withCredentials: true });
  }

  getById(id: number): Observable<TaskStatus> {
    return this.http.get<TaskStatus>(`/api/statuses/${id}`, { withCredentials: true });
  }

  create(request: CreateStatusRequest): Observable<TaskStatus> {
    return this.http.post<TaskStatus>('/api/statuses', request, { withCredentials: true });
  }

  update(id: number, request: UpdateStatusRequest): Observable<TaskStatus> {
    return this.http.put<TaskStatus>(`/api/statuses/${id}`, request, { withCredentials: true });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/statuses/${id}`, { withCredentials: true });
  }
}
