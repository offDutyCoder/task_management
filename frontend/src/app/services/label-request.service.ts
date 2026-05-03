import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateLabelRequestDto,
  LabelRequest,
  LabelRequestStatus,
  UpdateLabelRequestStatusDto,
} from '../models/label-request.model';

@Injectable({ providedIn: 'root' })
export class LabelRequestService {
  private readonly http = inject(HttpClient);

  getAll(status?: LabelRequestStatus): Observable<LabelRequest[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<LabelRequest[]>('/api/labels/requests', { withCredentials: true, params });
  }

  create(dto: CreateLabelRequestDto): Observable<LabelRequest> {
    return this.http.post<LabelRequest>('/api/labels/requests', dto, { withCredentials: true });
  }

  updateStatus(id: number, dto: UpdateLabelRequestStatusDto): Observable<LabelRequest> {
    return this.http.put<LabelRequest>(`/api/labels/requests/${id}`, dto, { withCredentials: true });
  }
}
