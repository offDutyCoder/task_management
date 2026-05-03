export enum UserRole {
  Admin = 0,
  Member = 1,
}

export interface User {
  id: number;
  loginId: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface LoginRequest {
  loginId: string;
  password: string;
}

export interface LoginResponse {
  userId: number;
  displayName: string;
  role: UserRole;
}

export interface CreateUserRequest {
  loginId: string;
  password: string;
  displayName: string;
  role: UserRole;
}

export interface UpdateUserRequest {
  displayName: string;
  role: UserRole;
  isActive: boolean;
}

export interface UpdateMyProfileRequest {
  displayName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
