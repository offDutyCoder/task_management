# 設計書

## アーキテクチャ概要

既存のレイヤードアーキテクチャ（Controller → Service → Repository → SQL Server）とAngular SPAパターンに完全準拠。

```
Angular SPA
  LabelManagementComponent (修正)    ← admin/labels
  LabelRequestManagementComponent    ← admin/label-requests (新規)
  LabelRequestDialogComponent        ← 一般ユーザー用送信ダイアログ (新規)
      ↓ HTTP /api/labels, /api/labels/requests
ASP.NET Core Web API
  LabelsController (修正: リクエストエンドポイント追加)
      ↓
  LabelService (既存)
  LabelRequestService (新規)
      ↓
  LabelRepository (既存)
  LabelRequestRepository (新規)
      ↓
SQL Server
  Labels (既存)
  LabelRequests (新規テーブル)
```

## コンポーネント設計

### バックエンド

#### LabelRequest エンティティ (Domain)

```csharp
public class LabelRequest
{
    public int Id { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedName { get; set; }
    public string? Reason { get; set; }
    public LabelRequestStatus Status { get; set; }  // Pending / Approved / Rejected
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User RequestedBy { get; set; }
}

public enum LabelRequestStatus { Pending, Approved, Rejected }
```

#### DTOs

- `CreateLabelRequestDto`: RequestedName (Required, Max50), Reason (optional, Max200)
- `UpdateLabelRequestStatusDto`: Status (Approved/Rejected のみ受付), Color (Approved時は必須)
- `LabelRequestResponse`: Id, RequestedName, Reason, Status, RequestedByDisplayName, CreatedAt

#### ILabelRequestRepository

```csharp
Task<IEnumerable<LabelRequest>> GetAllAsync(LabelRequestStatus? status = null);
Task<LabelRequest?> GetByIdAsync(int id);
Task<LabelRequest> CreateAsync(LabelRequest request);
Task<LabelRequest> UpdateAsync(LabelRequest request);
```

#### ILabelRequestService

```csharp
Task<IEnumerable<LabelRequestResponse>> GetAllAsync(LabelRequestStatus? status);
Task<LabelRequestResponse> CreateAsync(int userId, CreateLabelRequestDto request);
Task<LabelRequestResponse> ApproveAsync(int id, string color);
Task<LabelRequestResponse> RejectAsync(int id);
```

#### LabelsController 追加エンドポイント

```
POST /api/labels/requests        → 一般ユーザーがリクエスト送信 [Authorize]
GET  /api/labels/requests        → 管理者がリクエスト一覧取得 [Authorize(Roles="Admin")]
PUT  /api/labels/requests/{id}   → 管理者が承認/却下 [Authorize(Roles="Admin")]
```

### フロントエンド

#### LabelManagementComponent 修正点

- `LabelDialogComponent` を `status-dialog.component.ts` と同パターンに修正
  - `MAT_DIALOG_DATA` を使ってモード・既存ラベルを注入
  - 編集時に既存値を初期値としてフォームに設定
  - isActive のスライドトグルを編集モードに追加
  - HEX カラーコードのバリデーション (`/^#[0-9A-Fa-f]{6}$/`)
  - カラープレビュー（input右横に円形ドット表示）

#### LabelRequestManagementComponent (新規)

- ルート: `/admin/label-requests`
- AdminGuard + AuthGuard 適用
- テーブル: 要求者名・ラベル名・理由・ステータス・日時・操作
- 承認ダイアログ: カラーコードを入力してから承認
- ステータスフィルター（全件 / Pending / Approved / Rejected）

#### LabelRequestDialogComponent (新規・一般ユーザー向け)

- 場所: `/pages/tasks/label-request-dialog/`
- ラベル名（必須・最大50文字）、理由（任意・最大200文字）の入力フォーム
- LabelService.getAll() で既存ラベル一覧を取得し、重複名はwarning表示

## データフロー

### ラベルリクエスト承認フロー

```
1. 管理者: PUT /api/labels/requests/{id} { status: "Approved", color: "#4CAF50" }
2. LabelRequestService.ApproveAsync(id, color)
3. LabelRequest.Status = Approved, UpdatedAt = now
4. LabelService.CreateAsync({ name: request.RequestedName, color })
5. 両エンティティをトランザクション内で保存
6. LabelRequestResponse を返す
```

## エラーハンドリング戦略

| エラー | HTTP | 処理 |
|--------|------|------|
| ラベル名重複 | 400 | ValidationException |
| リクエスト未存在 | 404 | NotFoundException |
| 既に処理済みリクエストを再処理 | 400 | ValidationException |
| 一般ユーザーが管理者エンドポイントにアクセス | 403 | ASP.NET Core [Authorize(Roles)] |

フロントエンド: `HttpInterceptor` で 401 → ログイン画面リダイレクト（既存）

## テスト戦略

### ユニットテスト (xUnit + Moq)

- `LabelRequestService.ApproveAsync`: LabelRepository.CreateAsync が呼ばれることを検証
- `LabelRequestService.ApproveAsync` (既に承認済みのリクエスト): ValidationException が throw されることを検証
- `LabelRequestService.CreateAsync` (重複名): ValidationException が throw されることを検証

## ディレクトリ構造

```
backend/
  TaskManagement.Domain/Entities/
    LabelRequest.cs                  ← 新規
  TaskManagement.Application/
    DTOs/Labels/
      CreateLabelRequestDto.cs       ← 新規
      UpdateLabelRequestStatusDto.cs ← 新規
      LabelRequestResponse.cs        ← 新規
    Interfaces/
      ILabelRequestRepository.cs     ← 新規
      ILabelRequestService.cs        ← 新規
    Services/
      LabelRequestService.cs         ← 新規
  TaskManagement.Infrastructure/
    Data/
      AppDbContext.cs                ← 修正 (DbSet<LabelRequest> 追加)
    Repositories/
      LabelRequestRepository.cs      ← 新規
  TaskManagement.Api/
    Controllers/
      LabelsController.cs            ← 修正 (リクエストエンドポイント追加)
  TaskManagement.Tests/
    Unit/
      LabelRequestServiceTests.cs    ← 新規

frontend/src/app/
  models/
    label-request.model.ts           ← 新規
  services/
    label-request.service.ts         ← 新規
  pages/
    admin/
      label-management/
        label-management.component.ts ← 修正
      label-request-management/
        label-request-management.component.ts ← 新規
    tasks/
      label-request-dialog/
        label-request-dialog.component.ts ← 新規
  app.routes.ts                       ← 修正 (admin/label-requests ルート追加)
```

## セキュリティ考慮事項

- `GET/POST /api/labels/requests`: 認証済みユーザーのみ（一般ユーザーは自分のリクエスト送信のみ）
- `GET /api/labels/requests` (全件): 管理者のみ
- `PUT /api/labels/requests/{id}`: 管理者のみ

## 実装の順序

1. バックエンド: Domain エンティティ → DTO → Interface → Service → Repository → Controller → DI登録
2. フロントエンド: モデル → サービス → コンポーネント修正・新規作成 → ルーティング
3. テスト・品質チェック
