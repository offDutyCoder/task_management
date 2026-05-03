import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { UserRole } from '../models/user.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthService,
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should be not logged in initially', () => {
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.isAdmin()).toBeFalse();
    expect(service.currentUser()).toBeNull();
  });

  it('should update currentUser after login', () => {
    const mockResponse = { userId: 1, displayName: '管理者', role: UserRole.Admin };

    service.login({ loginId: 'admin', password: 'password123' }).subscribe(response => {
      expect(response).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);

    expect(service.isLoggedIn()).toBeTrue();
    expect(service.isAdmin()).toBeTrue();
    expect(service.currentUser()?.userId).toBe(1);
  });

  it('should clear currentUser after logout', () => {
    service.setCurrentUser({ userId: 1, displayName: '管理者', role: UserRole.Admin });
    expect(service.isLoggedIn()).toBeTrue();

    service.logout().subscribe();

    const req = httpMock.expectOne('/api/auth/logout');
    req.flush(null);

    expect(service.isLoggedIn()).toBeFalse();
    expect(service.currentUser()).toBeNull();
  });
});
