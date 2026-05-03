---
feature: アプリ内通知
date: 2026-05-04
---

# 設計書: アプリ内通知

## アーキテクチャ概要

既存のレイヤードアーキテクチャ（Controller → Service → Repository）に準拠。
通知生成は `TaskService` から `INotificationService` を呼び出す形で統合。
期限超過通知は Hangfire バックグラウンドジョブで実装。

```
[TaskService]
    ↓ NotifyAssignedAsync()
[NotificationService]
    ↓
[NotificationRepository]
    ↓
[SQL Server: Notifications テーブル]

[OverdueNotificationJob (Hangfire 00:05)]
    ↓ GenerateOverdueNotificationsAsync()
[NotificationService]
    ↓
[SQL Server: Notifications テーブル]

[Angular: Shell (ポーリング30秒)]
    ↓ GET /api/notifications
[NotificationsController]
    ↓
[NotificationService]
```

## コンポーネント設計

### 1. Notification エンティティ（Domain）

```csharp
// TaskManagement.Domain/Entities/Notification.cs
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public NotificationType Type { get; set; }
    public int? TaskItemId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public TaskItem? TaskItem { get; set; }
}
```

```csharp
// TaskManagement.Domain/Enums/NotificationType.cs
public enum NotificationType { Assigned, Overdue }
```

### 2. NotificationConfiguration（Infrastructure）

```csharp
// TaskManagement.Infrastructure/Data/Configurations/NotificationConfiguration.cs
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(500);
        builder.Property(n => n.IsRead).IsRequired().HasDefaultValue(false);
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(n => n.TaskItem).WithMany().HasForeignKey(n => n.TaskItemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(n => new { n.UserId, n.IsRead });  // 通知一覧取得の最適化
    }
}
```

### 3. INotificationRepository / NotificationRepository（Infrastructure）

```csharp
// TaskManagement.Application/Interfaces/INotificationRepository.cs
public interface INotificationRepository
{
    Task<(IEnumerable<Notification> Items, int UnreadCount)> GetByUserIdAsync(int userId);
    Task CreateAsync(Notification notification);
    Task CreateRangeAsync(IEnumerable<Notification> notifications);
    Task<Notification?> GetByIdAsync(int id);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task<bool> OverdueNotificationExistsAsync(int taskId, int userId, DateTime date);
}
```

### 4. INotificationService / NotificationService（Application）

```csharp
// TaskManagement.Application/Interfaces/INotificationService.cs
public interface INotificationService
{
    Task<NotificationListResponse> GetNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int currentUserId);
    Task MarkAllAsReadAsync(int userId);
    Task NotifyAssignedAsync(int taskId, string taskTitle, IEnumerable<int> assigneeIds);
    Task GenerateOverdueNotificationsAsync();
}
```

**NotificationService の責務**:
- 通知一覧取得（ユーザーごと）
- 個別既読・全件既読
- 担当割り当て通知生成（メッセージ: 「「{title}」の担当者に追加されました」）
- 期限超過通知生成（メッセージ: 「「{title}」の期限が超過しています」）
- 期限超過通知の重複チェック

### 5. TaskService との統合

```csharp
// CreateTaskAsync と AddAssigneeAsync で呼び出す
await _notificationService.NotifyAssignedAsync(created.Id, created.Title, request.AssigneeIds);
```

- TaskService のコンストラクターに `INotificationService` を追加
- 既存テストへの影響: `TaskServiceTests` のモックに `Mock<INotificationService>` を追加

### 6. NotificationsController（Api）

```
GET  /api/notifications          → 200 { items: [], unreadCount: N }
PUT  /api/notifications/{id}/read → 200
PUT  /api/notifications/read-all  → 200
```

### 7. OverdueNotificationJob（Infrastructure/Jobs）

```csharp
// TaskManagement.Infrastructure/Jobs/OverdueNotificationJob.cs
public class OverdueNotificationJob
{
    private readonly INotificationService _notificationService;

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        await _notificationService.GenerateOverdueNotificationsAsync();
    }
}
```

- Hangfire の RecurringJob として Program.cs で登録: `"0 5 0 * * *"` (00:05)

### 8. フロントエンド: NotificationService

```typescript
// frontend/src/app/services/notification.service.ts
@Injectable({ providedIn: 'root' })
export class NotificationService {
  getAll(): Observable<NotificationListResponse>
  markAsRead(id: number): Observable<void>
  markAllAsRead(): Observable<void>
}
```

### 9. フロントエンド: Shell 通知ベル

- `ShellComponent` に `NotificationService` を inject
- `unreadCount` signal を定義し、30秒ごとにポーリングして更新
- `mat-icon-button` + `notification` アイコン + `MatBadge` でバッジ表示
- クリックで `/notifications` ルートへナビゲート

### 10. フロントエンド: 通知一覧ページ

- `/notifications` ルートに新しいページコンポーネントを追加
- `mat-list` で通知一覧表示
- 未読通知は背景色（薄い青など）で強調
- 「すべて既読にする」 `mat-button` を提供
- 通知クリック（または関連タスクリンク）で `/tasks/{taskId}` へナビゲート

## データフロー

### 担当割り当て通知
```
1. POST /api/tasks/{id}/assignees
2. TaskService.AddAssigneeAsync → _taskRepository.AddAssigneeAsync
3. TaskService → _notificationService.NotifyAssignedAsync(taskId, title, [userId])
4. NotificationService → NotificationRepository.CreateAsync(Notification { Type=Assigned })
5. GET /api/notifications でフロントエンドに反映（ポーリング）
```

### 期限超過通知
```
1. Hangfire 00:05 → OverdueNotificationJob.ExecuteAsync()
2. NotificationService.GenerateOverdueNotificationsAsync()
   - TaskRepository から期限超過・非完了タスクを取得
   - 各担当者の当日通知済みチェック
   - Notification レコードを一括 INSERT
```

## エラーハンドリング戦略

### API エラー
| 条件 | HTTPステータス |
|------|---------------|
| 通知が存在しない | 404 Not Found |
| 他ユーザーの通知を操作 | 403 Forbidden |

### バックグラウンドジョブ
- Hangfire の AutomaticRetry（最大3回）で対応
- 失敗時はHangfireダッシュボードで確認可能

## テスト戦略

### ユニットテスト
- `NotificationServiceTests`:
  - `GetNotificationsAsync`: ユーザーの通知のみ返ること
  - `MarkAsReadAsync`: 他ユーザーの通知は 403 ForbiddenException
  - `NotifyAssignedAsync`: 各 assigneeId に対して通知が生成されること
  - `GenerateOverdueNotificationsAsync`: 重複チェック・完了タスク除外を検証
- 既存 `TaskServiceTests`: `Mock<INotificationService>` を追加してコンパイルエラーを解消

### 統合テスト
- `NotificationsControllerTests`:
  - `GET /api/notifications` 未認証 → 401
  - `GET /api/notifications` 認証済み → 200 + 自分の通知のみ
  - `PUT /api/notifications/{id}/read` 他ユーザーの通知 → 403
  - `PUT /api/notifications/read-all` → 200 + 全件既読

## 依存ライブラリ

```xml
<!-- TaskManagement.Infrastructure.csproj に追加 -->
<PackageReference Include="Hangfire.Core" Version="1.8.14" />
<PackageReference Include="Hangfire.SqlServer" Version="1.8.14" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.14" />
<!-- テスト用 InMemory ストレージ -->
<!-- <PackageReference Include="Hangfire.InMemory" Version="0.4.2" /> -->
```

## ディレクトリ構造

```
backend/
  TaskManagement.Domain/
    Entities/
      Notification.cs         ← NEW
    Enums/
      NotificationType.cs     ← NEW
  TaskManagement.Application/
    DTOs/
      Notifications/
        NotificationResponse.cs      ← NEW
        NotificationListResponse.cs  ← NEW
    Interfaces/
      INotificationRepository.cs  ← NEW
      INotificationService.cs     ← NEW
    Services/
      NotificationService.cs      ← NEW
  TaskManagement.Infrastructure/
    Data/
      AppDbContext.cs              ← MODIFY (DbSet<Notification> 追加)
      Configurations/
        NotificationConfiguration.cs  ← NEW
    Repositories/
      NotificationRepository.cs   ← NEW
    Jobs/
      OverdueNotificationJob.cs   ← NEW
  TaskManagement.Api/
    Controllers/
      NotificationsController.cs  ← NEW
    Program.cs                    ← MODIFY (DI + Hangfire 登録)
  TaskManagement.Tests/
    Unit/Services/
      NotificationServiceTests.cs  ← NEW
    Integration/Controllers/
      NotificationsControllerTests.cs  ← NEW

frontend/src/app/
  models/
    notification.model.ts          ← NEW
  services/
    notification.service.ts        ← NEW
  pages/
    notifications/
      notifications.component.ts   ← NEW
      notifications.component.html ← NEW
      notifications.component.scss ← NEW
  layout/shell/
    shell.component.ts             ← MODIFY (通知ベル追加)
    shell.component.html           ← MODIFY (通知ベルUI追加)
  app.routes.ts                    ← MODIFY (/notifications ルート追加)
```

## 実装の順序

1. Domain 層（エンティティ・列挙型）
2. Infrastructure 層（Configuration・Repository・AppDbContext・Hangfire）
3. Application 層（DTO・Interface・Service）
4. TaskService との統合
5. API 層（Controller・DI 登録）
6. フロントエンド（Model → Service → Shell → NotificationPage → Route）
7. ユニットテスト・統合テスト

## セキュリティ考慮事項

- 通知の取得・既読更新は「自分の通知のみ」に限定（Service 層でチェック）
- 他ユーザーの通知への操作は 403 Forbidden

## パフォーマンス考慮事項

- `Notifications(UserId, IsRead)` 複合インデックスで通知一覧取得を最適化（architecture.md 仕様通り）
- ポーリング間隔は 30秒（仕様通り）
- 一覧は最新50件に絞る（初期実装）

## 将来の拡張性

- `NotificationService` を抽象化しておくことで、将来的なメール・Slack 通知をプラグイン的に追加可能（architecture.md 記載通り）
