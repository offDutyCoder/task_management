# 設計書 - ユーザー管理・認証

## アーキテクチャ概要

既存ドキュメントに定義されたレイヤードアーキテクチャを踏襲する。

```
Angular SPA (frontend)
    │ HTTPS / withCredentials
    ↓
IIS → ASP.NET Core Web API
    ├── AuthController        (POST /api/auth/login, POST /api/auth/logout)
    └── UsersController       (GET /api/users, POST /api/users, PUT /api/users/{id}, PUT /api/users/me/*)
         ↓ [Authorize] + [Authorize(Roles="Admin")]
    UserService               (ビジネスロジック・BCrypt ハッシュ)
         ↓
    UserRepository (EF Core)
         ↓
    SQL Server  (Users テーブル)
```

## コンポーネント設計

### 1. ドメイン層

**`TaskManagement.Domain/Entities/User.cs`**
- `Id`, `LoginId`, `DisplayName`, `PasswordHash`, `Role` (UserRole enum), `IsActive`, `CreatedAt`, `UpdatedAt`

**`TaskManagement.Domain/Enums/UserRole.cs`**
- `Admin = 0`, `Member = 1`

### 2. アプリケーション層（Interfaces）

**`IUserRepository`**
- `GetByIdAsync(int id)`
- `GetByLoginIdAsync(string loginId)`
- `GetAllAsync()`
- `CreateAsync(User user)`
- `UpdateAsync(User user)`
- `ExistsLoginIdAsync(string loginId, int? excludeUserId)`

### 3. アプリケーション層（DTOs）

**Auth DTOs**:
- `LoginRequest`: `{ LoginId, Password }`
- `LoginResponse`: `{ UserId, DisplayName, Role }`

**User DTOs**:
- `CreateUserRequest`: `{ LoginId, Password, DisplayName, Role }`
- `UpdateUserRequest`: `{ DisplayName, Role, IsActive }` (管理者用)
- `UpdateMyProfileRequest`: `{ DisplayName }` (本人用)
- `ChangePasswordRequest`: `{ CurrentPassword, NewPassword }`
- `UserResponse`: `{ Id, LoginId, DisplayName, Role, IsActive, CreatedAt }`

### 4. アプリケーション層（Services）

**`UserService`**
- `AuthenticateAsync(LoginRequest)` → `User` or null
- `CreateUserAsync(CreateUserRequest)` → `UserResponse`
- `GetAllUsersAsync()` → `IEnumerable<UserResponse>`
- `GetUserByIdAsync(int id)` → `UserResponse`
- `UpdateUserAsync(int id, UpdateUserRequest)` → `UserResponse`
- `UpdateMyProfileAsync(int currentUserId, UpdateMyProfileRequest)` → `UserResponse`
- `ChangePasswordAsync(int currentUserId, ChangePasswordRequest)`

### 5. インフラ層

**`UserRepository`**: EF Core によるCRUD実装
**`UserConfiguration`**: テーブル定義・インデックス・制約
**`AppDbContext`**: DbSet<User> を含む

### 6. プレゼンテーション層

**`AuthController`**:
- `POST /api/auth/login` → Cookie 発行
- `POST /api/auth/logout` → Cookie 削除

**`UsersController`**:
- `GET /api/users` → `[Authorize(Roles="Admin")]`
- `POST /api/users` → `[Authorize(Roles="Admin")]`
- `PUT /api/users/{id}` → `[Authorize(Roles="Admin")]`
- `PUT /api/users/me/profile` → `[Authorize]`
- `PUT /api/users/me/password` → `[Authorize]`

**`ExceptionHandlingMiddleware`**:
- `NotFoundException` → 404
- `ForbiddenException` → 403
- `ValidationException` → 400
- その他 → 500

### 7. フロントエンド

**Models**: `user.model.ts` (User, UserRole, LoginRequest, LoginResponse, CreateUserRequest, UserResponse)

**Services**:
- `AuthService`: login/logout/currentUser Signal, isAdmin computed
- `UserService`: CRUD HTTP 呼び出し

**Guards**:
- `authGuard`: 未認証 → /login リダイレクト
- `adminGuard`: 非管理者 → /dashboard リダイレクト

**Interceptors**:
- `authInterceptor`: 401 検知 → /login リダイレクト

**Pages**:
- `LoginComponent`: ログインフォーム
- `UserManagementComponent`: ユーザー一覧・追加・編集・有効/無効切替

## データフロー

### ログインフロー
```
1. Angular: POST /api/auth/login { loginId, password }
2. AuthController: UserService.AuthenticateAsync() 呼び出し
3. UserService: UserRepository.GetByLoginIdAsync() → BCrypt.Verify()
4. 成功: ASP.NET Cookie 認証チケット発行 (HTTP-only, SameSite=Strict)
5. Angular: レスポンスの LoginResponse を AuthService に保存
```

### ユーザー作成フロー
```
1. Angular (管理者): POST /api/users { loginId, password, displayName, role }
2. UsersController: [Authorize(Roles="Admin")] チェック
3. UserService: LoginId 重複確認 → BCrypt.HashPassword() → UserRepository.CreateAsync()
4. レスポンス: 201 Created { UserResponse }
```

## エラーハンドリング戦略

### カスタムエラークラス
```csharp
class NotFoundException(string resource, int id) : Exception
class ForbiddenException(string message) : Exception
class ValidationException(string message) : Exception  // LoginId 重複など
```

### エラーハンドリングパターン
- Service 層でドメイン例外をスロー
- `ExceptionHandlingMiddleware` が HTTP ステータスに変換

## テスト戦略

### ユニットテスト (xUnit + Moq)
- `UserService.AuthenticateAsync_WhenPasswordWrong_ShouldReturnNull`
- `UserService.AuthenticateAsync_WhenUserInactive_ShouldReturnNull`
- `UserService.CreateUserAsync_WhenLoginIdExists_ShouldThrowValidationException`
- `UserService.ChangePasswordAsync_WhenCurrentPasswordWrong_ShouldThrowValidationException`

### 統合テスト (ASP.NET Core TestServer)
- `POST /api/auth/login` 正常・異常系
- `POST /api/users` 管理者/一般ユーザー権限チェック
- `GET /api/users` 管理者/一般ユーザー権限チェック

## 依存ライブラリ

### バックエンド (NuGet)
- `BCrypt.Net-Next` 4.x (パスワードハッシュ)
- `Microsoft.AspNetCore.Authentication.Cookies` (.NET 8 組み込み)
- `Microsoft.EntityFrameworkCore.SqlServer` 8.x
- `Microsoft.EntityFrameworkCore.Design` 8.x (マイグレーション)
- `xUnit` 2.x
- `Moq` 4.x
- `Microsoft.AspNetCore.Mvc.Testing` 8.x (統合テスト)

### フロントエンド (npm)
- `@angular/core` 17.x (組み込み)
- `@angular/material` 17.x (UI コンポーネント)
- `@angular/forms` 17.x (リアクティブフォーム)

## ディレクトリ構造

```
backend/
├── TaskManagement.Domain/
│   ├── Entities/User.cs
│   └── Enums/UserRole.cs
├── TaskManagement.Application/
│   ├── Interfaces/IUserRepository.cs
│   ├── DTOs/Auth/LoginRequest.cs
│   ├── DTOs/Auth/LoginResponse.cs
│   ├── DTOs/Users/CreateUserRequest.cs
│   ├── DTOs/Users/UpdateUserRequest.cs
│   ├── DTOs/Users/UpdateMyProfileRequest.cs
│   ├── DTOs/Users/ChangePasswordRequest.cs
│   ├── DTOs/Users/UserResponse.cs
│   └── Services/UserService.cs
├── TaskManagement.Infrastructure/
│   ├── Data/AppDbContext.cs
│   ├── Data/Configurations/UserConfiguration.cs
│   └── Repositories/UserRepository.cs
├── TaskManagement.Api/
│   ├── Controllers/AuthController.cs
│   ├── Controllers/UsersController.cs
│   ├── Middleware/ExceptionHandlingMiddleware.cs
│   └── Program.cs
└── TaskManagement.Tests/
    ├── Unit/Services/UserServiceTests.cs
    └── Integration/Controllers/AuthControllerTests.cs

frontend/
└── src/app/
    ├── models/user.model.ts
    ├── services/auth.service.ts
    ├── services/user.service.ts
    ├── guards/auth.guard.ts
    ├── guards/admin.guard.ts
    ├── interceptors/auth.interceptor.ts
    ├── pages/login/login.component.ts (.html, .scss)
    └── pages/admin/user-management/user-management.component.ts (.html, .scss)
```

## 実装の順序

1. プロジェクト構造セットアップ（.NET ソリューション・Angular プロジェクト）
2. Domain 層（User エンティティ・UserRole 列挙型）
3. Application 層（DTOs → Interface → Service）
4. Infrastructure 層（EF Core 設定・Repository・AppDbContext）
5. API 層（Controllers・Middleware・Program.cs）
6. フロントエンド（Models → Services → Guards → Pages）
7. テスト（Unit → Integration）
8. 品質チェック

## セキュリティ考慮事項

- パスワードは BCrypt コスト係数 12 でハッシュ化（平文保存禁止）
- Cookie: `HttpOnly = true`, `SameSite = SameSiteMode.Strict`, `SecurePolicy = Always` (開発は None)
- 管理者エンドポイントに `[Authorize(Roles = "Admin")]` を必ず付与
- LoginId 重複チェックはサービス層で実施（DB ユニーク制約も設定）

## パフォーマンス考慮事項

- ユーザー数は最大 100 名程度のため、ページネーション不要
- `LoginId` カラムにユニーク制約とインデックスを設定
