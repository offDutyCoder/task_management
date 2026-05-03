import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ChangePasswordRequest,
  CreateUserRequest,
  UpdateMyProfileRequest,
  UpdateUserRequest,
  User,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<User[]> {
    return this.http.get<User[]>('/api/users', { withCredentials: true });
  }

  getById(id: number): Observable<User> {
    return this.http.get<User>(`/api/users/${id}`, { withCredentials: true });
  }

  create(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>('/api/users', request, { withCredentials: true });
  }

  update(id: number, request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`/api/users/${id}`, request, { withCredentials: true });
  }

  updateMyProfile(request: UpdateMyProfileRequest): Observable<User> {
    return this.http.put<User>('/api/users/me/profile', request, { withCredentials: true });
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>('/api/users/me/password', request, { withCredentials: true });
  }
}
