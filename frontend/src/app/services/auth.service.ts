import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, tap } from 'rxjs';
import { LoginRequest, LoginResponse, UserRole } from '../models/user.model';

// [TEMP] モックログイン用ダミーユーザー
const MOCK_USER: LoginResponse = { userId: 0, displayName: 'Mock User', role: UserRole.Admin };
// [TEMP END]

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _currentUser = signal<LoginResponse | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);
  readonly isAdmin = computed(() => this._currentUser()?.role === UserRole.Admin);

  login(request: LoginRequest): Observable<LoginResponse> {
    // [TEMP] aaa/aaa でバックエンド不要のモックログイン
    if (request.loginId === 'aaa' && request.password === 'aaa') {
      this._currentUser.set(MOCK_USER);
      return of(MOCK_USER);
    }
    // [TEMP END]
    return this.http.post<LoginResponse>('/api/auth/login', request, { withCredentials: true }).pipe(
      tap(response => this._currentUser.set(response))
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', {}, { withCredentials: true }).pipe(
      tap(() => this._currentUser.set(null))
    );
  }

  restoreSession(): Observable<LoginResponse | null> {
    return this.http.get<LoginResponse>('/api/auth/me', { withCredentials: true }).pipe(
      tap(response => this._currentUser.set(response)),
      catchError(() => {
        this._currentUser.set(null);
        return of(null);
      })
    );
  }

  setCurrentUser(user: LoginResponse | null): void {
    this._currentUser.set(user);
  }
}
