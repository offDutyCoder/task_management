---
feature: アプリ内通知
date: 2026-05-04
---

# タスクリスト: アプリ内通知

## 🚨 タスク完全完了の原則

**このファイルの全タスクが完了するまで作業を継続すること**

### 必須ルール
- **全てのタスクを`[x]`にすること**
- 「時間の都合により別タスクとして実施予定」は禁止
- 未完了タスク（`[ ]`）を残したまま作業を終了しない

---

## フェーズ1: Domain 層

- [x] `NotificationType` 列挙型を作成する
  - ファイル: `backend/TaskManagement.Domain/Enums/NotificationType.cs`
  - 値: `Assigned`, `Overdue`
- [x] `Notification` エンティティを作成する
  - ファイル: `backend/TaskManagement.Domain/Entities/Notification.cs`
  - フィールド: Id, UserId, Type, TaskItemId?, Message, IsRead, CreatedAt
  - ナビゲーションプロパティ: User, TaskItem

## フェーズ2: Infrastructure 層

- [x] `NotificationConfiguration` を作成する
  - ファイル: `backend/TaskManagement.Infrastructure/Data/Configurations/NotificationConfiguration.cs`
  - テーブル名: `Notifications`
  - インデックス: `(UserId, IsRead)`
  - FK: UserId → Users (Cascade), TaskItemId → TaskItems (SetNull)
- [x] `AppDbContext` に `Notifications` DbSet を追加する
  - ファイル: `backend/TaskManagement.Infrastructure/Data/AppDbContext.cs`
- [x] Hangfire NuGet パッケージを Infrastructure プロジェクトに追加する
  - `Hangfire.Core` 1.8.14, `Hangfire.SqlServer` 1.8.14, `Hangfire.AspNetCore` 1.8.14
- [x] `INotificationRepository` インターフェースを作成する
  - ファイル: `backend/TaskManagement.Application/Interfaces/INotificationRepository.cs`
  - メソッド: GetByUserIdAsync, CreateAsync, CreateRangeAsync, GetByIdAsync, MarkAsReadAsync, MarkAllAsReadAsync, OverdueNotificationExistsAsync
- [x] `NotificationRepository` を実装する
  - ファイル: `backend/TaskManagement.Infrastructure/Repositories/NotificationRepository.cs`
  - `GetByUserIdAsync`: UserId でフィルター、CreatedAt 降順、最新50件、UnreadCount 含む
  - `OverdueNotificationExistsAsync`: 指定日の Overdue 通知重複チェック
- [x] `OverdueNotificationJob` を作成する
  - ファイル: `backend/TaskManagement.Infrastructure/Jobs/OverdueNotificationJob.cs`
  - `ExecuteAsync()` → `_notificationService.GenerateOverdueNotificationsAsync()`

## フェーズ3: Application 層

- [x] `NotificationResponse` DTO を作成する
  - ファイル: `backend/TaskManagement.Application/DTOs/Notifications/NotificationResponse.cs`
  - フィールド: Id, Type, TaskItemId?, Message, IsRead, CreatedAt
- [x] `NotificationListResponse` DTO を作成する
  - ファイル: `backend/TaskManagement.Application/DTOs/Notifications/NotificationListResponse.cs`
  - フィールド: Items (List<NotificationResponse>), UnreadCount
- [x] `INotificationService` インターフェースを作成する
  - ファイル: `backend/TaskManagement.Application/Interfaces/INotificationService.cs`
  - メソッド: GetNotificationsAsync, MarkAsReadAsync, MarkAllAsReadAsync, NotifyAssignedAsync, GenerateOverdueNotificationsAsync
- [x] `NotificationService` を実装する
  - ファイル: `backend/TaskManagement.Application/Services/NotificationService.cs`
  - `NotifyAssignedAsync`: 各 assigneeId に Assigned 通知を生成（メッセージ: 「「{title}」の担当者に追加されました」）
  - `GenerateOverdueNotificationsAsync`: 期限超過・非完了タスクを TaskRepository から取得し、当日未通知の担当者に Overdue 通知を一括生成（メッセージ: 「「{title}」の期限が超過しています」）
  - `MarkAsReadAsync`: 他ユーザーの通知操作は ForbiddenException
- [x] `TaskService` に `INotificationService` を統合する
  - `TaskService` コンストラクターに `INotificationService` を追加
  - `CreateTaskAsync` の完了後に `NotifyAssignedAsync` を呼び出す
  - `AddAssigneeAsync` の完了後に `NotifyAssignedAsync` を呼び出す
- [x] `ITaskRepository` に期限超過タスク取得メソッドを追加する
  - `GetOverdueTasksWithAssigneesAsync()`: 期限日 < 今日・非完了・担当者あり のタスクを返す
- [x] `TaskRepository` に上記メソッドを実装する

## フェーズ4: API 層

- [x] `NotificationsController` を作成する
  - ファイル: `backend/TaskManagement.Api/Controllers/NotificationsController.cs`
  - `GET /api/notifications` → 200 NotificationListResponse
  - `PUT /api/notifications/{id}/read` → 200
  - `PUT /api/notifications/read-all` → 200
- [x] `Program.cs` にサービスの DI 登録と Hangfire 設定を追加する
  - `INotificationRepository` → `NotificationRepository`
  - `INotificationService` → `NotificationService`
  - Hangfire サービスの追加（SQL Server ストレージ）
  - `OverdueNotificationJob` の RecurringJob 登録（毎日 00:05）

## フェーズ5: フロントエンド

- [x] `notification.model.ts` を作成する
  - ファイル: `frontend/src/app/models/notification.model.ts`
  - 型: `NotificationType` enum, `NotificationItem`, `NotificationListResponse`
- [x] `notification.service.ts` を作成する
  - ファイル: `frontend/src/app/services/notification.service.ts`
  - メソッド: `getAll()`, `markAsRead(id)`, `markAllAsRead()`
- [x] `ShellComponent` に通知ベルを追加する
  - `shell.component.ts`: `NotificationService` inject、`unreadCount` signal、30秒ポーリング、`ngOnDestroy` でタイマーをクリア
  - `shell.component.html`: `mat-icon-button` + `notifications` アイコン + `MatBadge`、クリックで `/notifications` へ遷移
- [x] 通知一覧ページを作成する
  - `frontend/src/app/pages/notifications/notifications.component.ts`
  - `frontend/src/app/pages/notifications/notifications.component.html`
  - `frontend/src/app/pages/notifications/notifications.component.scss`
  - 通知一覧を `mat-list` で表示、未読を強調、「すべて既読」ボタン、タスクリンク
- [x] `app.routes.ts` に `/notifications` ルートを追加する
  - `canActivate: [authGuard]`

## フェーズ6: テスト

- [x] 既存の `TaskServiceTests` を `INotificationService` モック追加に対応させる
  - `Mock<INotificationService>` を追加し `TaskService` インスタンス生成を修正
- [x] `NotificationServiceTests` ユニットテストを作成する
  - ファイル: `backend/TaskManagement.Tests/Unit/Services/NotificationServiceTests.cs`
  - `GetNotificationsAsync_ReturnsOnlyUserNotifications`
  - `MarkAsReadAsync_OtherUsersNotification_ThrowsForbiddenException`
  - `NotifyAssignedAsync_CreatesNotificationForEachAssignee`
- [x] `NotificationsControllerTests` 統合テストを作成する
  - ファイル: `backend/TaskManagement.Tests/Integration/Controllers/NotificationsControllerTests.cs`
  - 未認証 → 401
  - 認証済み → 自分の通知のみ返る
  - 他ユーザーの通知既読 → 403
  - read-all → 全件既読

## フェーズ7: 品質チェック

- [x] バックエンドビルドが通ることを確認する
  - `dotnet build backend/`
- [x] バックエンドテストが全て通ることを確認する
  - `dotnet test backend/`
- [x] フロントエンド lint が通ることを確認する
  - `cd frontend && npm run lint`
- [x] フロントエンド型チェックが通ることを確認する
  - `cd frontend && npm run build`

---

## 実装後の振り返り

### 実装完了日
2026-05-05

### 計画と実績の差分

**計画と異なった点**:
- `RecurringJob.AddOrUpdate` をスタートアップで直接呼び出すと、テスト用の WebApplicationFactory 起動時に SQL Server 接続を試みてクラッシュすることが判明。`HangfireJobRegistrationService`（`IHostedService`）に移動し try-catch で囲む対応を追加した。
- `NotificationRepository.MarkAllAsReadAsync` で使用した `ExecuteUpdateAsync`（EF Core バルク更新）が InMemory プロバイダー非対応であることが判明。ロード＆個別更新方式に変更した。
- `TaskStatus` が `HasData` でシード済みのため、統合テストのシード関数で重複追加するとキー衝突が発生。シード関数から `TaskStatus` 追加を除外した。
- `Program.cs` 上 `connectionString` を `AddDbContext` より後で宣言していたため、変数参照順を修正した。

**新たに必要になったタスク（validator 指摘後に追加）**:
- `UpdateTaskAsync` に新規担当者への `NotifyAssignedAsync` 呼び出しを追加（PRD「担当者が変更されたとき通知が届く」要件の漏れ）
- `TaskServiceTests` に `UpdateTaskAsync_WhenNewAssigneeAdded` / `WhenNoNewAssignees` の 2 ユニットテストを追加
- `HangfireAuthorizationFilter` を作成し Hangfire ダッシュボードに管理者専用認可を適用
- `INotificationRepository.CreateAsync` は呼び出し箇所がなかったため削除（インターフェース・実装とも）

### 学んだこと

**技術的な学び**:
- **EF Core InMemory と `ExecuteUpdateAsync`**: SQL Server では問題なく動作するが InMemory プロバイダーはバルク操作（`ExecuteUpdateAsync` / `ExecuteDeleteAsync`）を未サポート。テスト用に互換性のある実装を選ぶ必要がある。
- **Hangfire と WebApplicationFactory**: `RecurringJob.AddOrUpdate` は静的呼び出しで Hangfire ストレージに即座にアクセスするため、接続文字列ガードだけでは不十分。`IHostedService` へ移動して例外を内部処理することでテスト環境でも安全に起動できる。
- **EF Core HasData と統合テスト**: `EnsureCreated()` はマイグレーションをスキップして `HasData` のシードデータも投入する。テスト側でシードに含まれるエンティティを重複追加すると `ArgumentException: An item with the same key has already been added` が発生する。
- **PRD のユースケース網羅**: 担当者追加には「タスク作成時（AssigneeIds 一括）」「個別 POST 追加」「タスク編集（UpdateTaskAsync）」の 3 経路があるが、implementation-validator が `UpdateTaskAsync` の漏れを検出した。テストに「編集フォームからの担当者追加」シナリオも含めるべきだった。

**プロセス上の改善点**:
- Hangfire のような外部ストレージ依存コンポーネントは、計画段階でテスト環境での動作を設計ドキュメントに明記しておくとスムーズだった。

### 次回への改善提案
- バックグラウンドジョブを追加する際は、テスト環境での起動戦略（try-catch + IHostedService / フラグでの無効化）を設計段階で決定しておく。
- 担当者追加パスが複数ある機能は、全パスの通知送信を要件・設計書に一覧として明示し、実装・レビュー時のチェックリストとして活用する。
- 統合テストのシード関数では `HasData` 対象テーブル（`TaskStatuses` 等）を追加しないことを開発ガイドラインに追記する。
- `GetOverdueTasksWithAssigneesAsync` のステータス判定に文字列「完了」をハードコードしている。`TaskStatus` に `IsCompleted` フラグまたはシステムロールの概念を追加すれば壊れにくくなる（次のリファクタリング候補）。
