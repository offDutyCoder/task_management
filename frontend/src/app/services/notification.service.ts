import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationListResponse } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<NotificationListResponse> {
    return this.http.get<NotificationListResponse>('/api/notifications', { withCredentials: true });
  }

  markAsRead(id: number): Observable<void> {
    return this.http.put<void>(`/api/notifications/${id}/read`, null, { withCredentials: true });
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>('/api/notifications/read-all', null, { withCredentials: true });
  }
}
