import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AssigneeDto,
  CreateTaskRequest,
  TaskDetailResponse,
  TaskFilterQuery,
  TaskListResponse,
  UpdateTaskRequest,
} from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  getAll(filter?: TaskFilterQuery): Observable<TaskListResponse> {
    let params = new HttpParams();
    if (filter) {
      if (filter.statusId != null) params = params.set('statusId', filter.statusId);
      if (filter.assigneeId != null) params = params.set('assigneeId', filter.assigneeId);
      if (filter.labelId != null) params = params.set('labelId', filter.labelId);
      if (filter.priority != null) params = params.set('priority', filter.priority);
      if (filter.search) params = params.set('search', filter.search);
      if (filter.isRecurring != null) params = params.set('isRecurring', filter.isRecurring);
      if (filter.dueBefore) params = params.set('dueBefore', filter.dueBefore);
      if (filter.dueAfter) params = params.set('dueAfter', filter.dueAfter);
      if (filter.page != null) params = params.set('page', filter.page);
      if (filter.pageSize != null) params = params.set('pageSize', filter.pageSize);
    }
    return this.http.get<TaskListResponse>('/api/tasks', { params, withCredentials: true });
  }

  getById(id: number): Observable<TaskDetailResponse> {
    return this.http.get<TaskDetailResponse>(`/api/tasks/${id}`, { withCredentials: true });
  }

  create(request: CreateTaskRequest): Observable<TaskDetailResponse> {
    return this.http.post<TaskDetailResponse>('/api/tasks', request, { withCredentials: true });
  }

  update(id: number, request: UpdateTaskRequest): Observable<TaskDetailResponse> {
    return this.http.put<TaskDetailResponse>(`/api/tasks/${id}`, request, { withCredentials: true });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/tasks/${id}`, { withCredentials: true });
  }

  addAssignee(taskId: number, userId: number): Observable<AssigneeDto> {
    return this.http.post<AssigneeDto>(`/api/tasks/${taskId}/assignees`, { userId }, { withCredentials: true });
  }

  removeAssignee(taskId: number, userId: number): Observable<void> {
    return this.http.delete<void>(`/api/tasks/${taskId}/assignees/${userId}`, { withCredentials: true });
  }

  addShare(taskId: number, userId: number): Observable<AssigneeDto> {
    return this.http.post<AssigneeDto>(`/api/tasks/${taskId}/shares`, { userId }, { withCredentials: true });
  }

  removeShare(taskId: number, userId: number): Observable<void> {
    return this.http.delete<void>(`/api/tasks/${taskId}/shares/${userId}`, { withCredentials: true });
  }
}
