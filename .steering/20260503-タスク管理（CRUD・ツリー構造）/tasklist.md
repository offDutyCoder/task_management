# タスクリスト - タスク管理（CRUD・ツリー構造）

## バックエンド - ドメイン層

- [x] TaskManagement.Domain/Enums/TaskPriority.cs を作成する
- [x] TaskManagement.Domain/Entities/Label.cs を作成する
- [x] TaskManagement.Domain/Entities/TaskItem.cs を作成する
- [x] TaskManagement.Domain/Entities/TaskAssignee.cs を作成する
- [x] TaskManagement.Domain/Entities/TaskLabel.cs を作成する
- [x] TaskManagement.Domain/Entities/TaskShare.cs を作成する

## バックエンド - アプリケーション層（DTOs）

- [x] TaskManagement.Application/DTOs/Tasks/AssigneeDto.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/LabelDto.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/StatusDto.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/TaskFilterQuery.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/CreateTaskRequest.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/UpdateTaskRequest.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/TaskListItem.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/TaskListResponse.cs を作成する
- [x] TaskManagement.Application/DTOs/Tasks/TaskDetailResponse.cs を作成する
- [x] TaskManagement.Application/DTOs/Labels/LabelResponse.cs を作成する
- [x] TaskManagement.Application/DTOs/Labels/CreateLabelRequest.cs を作成する
- [x] TaskManagement.Application/DTOs/Labels/UpdateLabelRequest.cs を作成する

## バックエンド - アプリケーション層（Interfaces & Services）

- [x] TaskManagement.Application/Interfaces/ILabelRepository.cs を作成する
- [x] TaskManagement.Application/Interfaces/ILabelService.cs を作成する
- [x] TaskManagement.Application/Interfaces/ITaskRepository.cs を作成する
- [x] TaskManagement.Application/Interfaces/ITaskService.cs を作成する
- [x] TaskManagement.Application/Services/LabelService.cs を作成する
- [x] TaskManagement.Application/Services/TaskService.cs を作成する

## バックエンド - インフラ層

- [x] TaskManagement.Infrastructure/Data/Configurations/LabelConfiguration.cs を作成する
- [x] TaskManagement.Infrastructure/Data/Configurations/TaskItemConfiguration.cs を作成する
- [x] TaskManagement.Infrastructure/Data/Configurations/TaskAssigneeConfiguration.cs を作成する
- [x] TaskManagement.Infrastructure/Data/Configurations/TaskLabelConfiguration.cs を作成する
- [x] TaskManagement.Infrastructure/Data/Configurations/TaskShareConfiguration.cs を作成する
- [x] AppDbContext.cs に Label, TaskItem, TaskAssignee, TaskLabel, TaskShare DbSet を追加する
- [x] TaskManagement.Infrastructure/Repositories/LabelRepository.cs を作成する
- [x] TaskManagement.Infrastructure/Repositories/TaskRepository.cs を作成する

## バックエンド - プレゼンテーション層

- [x] TaskManagement.Api/Controllers/LabelsController.cs を作成する
- [x] TaskManagement.Api/Controllers/TasksController.cs を作成する
- [x] Program.cs に LabelRepository, LabelService, TaskRepository, TaskService の DI 登録を追加する

## バックエンド - テスト

- [x] TaskManagement.Tests/Unit/Services/TaskServiceTests.cs を作成する（共有範囲・サブタスク継承・削除権限）
- [x] TaskManagement.Tests/Integration/Controllers/TasksControllerTests.cs を作成する（CRUD・認証・共有範囲）

## フロントエンド - モデル・サービス

- [x] frontend/src/app/models/label.model.ts を作成する
- [x] frontend/src/app/models/task.model.ts を作成する
- [x] frontend/src/app/services/label.service.ts を作成する
- [x] frontend/src/app/services/task.service.ts を作成する

## フロントエンド - ページコンポーネント

- [x] frontend/src/app/pages/tasks/task-form/task-form.component.ts/.html/.scss を作成する（作成・編集ダイアログ）
- [x] frontend/src/app/pages/tasks/task-list/task-list.component.ts/.html/.scss を作成する（一覧・フィルター）
- [x] frontend/src/app/pages/tasks/task-detail/task-detail.component.ts/.html/.scss を作成する（詳細・サブタスク）

## フロントエンド - ルーティング

- [x] app.routes.ts に /tasks と /tasks/:id ルートを追加する

---

## 申し送り事項

**実装完了日**: 2026-05-04

### 計画と実績の差分

- **追加実装**: `admin/labels` ルートとラベル管理コンポーネント（`LabelManagementComponent`）を、implementation-validator の指摘を受けて追加した。当初の tasklist には含まれていなかった。
- **修正対応**: `TaskRepository.GetListAsync` の WHERE 句がサブタスクを除外していたため修正。`CreateAsync` を navigation property 経由の 1 回の SaveChanges に変更してアトミック性を確保。`TaskFilterQuery.PageSize` に上限 100 を設定。サブタスク作成時は親の共有設定を継承するよう `TaskService.CreateTaskAsync` を修正。

### テスト統合の注意点

- EF Core InMemory + `HasData` 使用時は `db.Database.EnsureCreated()` 後に `db.ChangeTracker.Clear()` を必ず呼ぶこと。`EnsureCreated` が HasData エンティティを change tracker に残すため、後続の `SaveChanges` で重複キーエラーが発生する。
- テスト内で `db.SaveChanges()` を複数回呼ぶ場合（ユーザーID を取得してから関連エンティティを追加する場合など）は明示的 ID を使って中間の SaveChanges を不要にするパターンが安全。

### 学んだこと

- EF Core InMemory の `EnsureCreated()` は `HasData` によるシードデータを change tracker に置いたまま SaveChanges する。後続 SaveChanges で重複エラーになる。`ChangeTracker.Clear()` で回避できる。
- `WebApplicationFactory.WithWebHostBuilder` は全テスト間でコンストラクタラッパー不要（`StatusesControllerTests` と同じパターン）。コンストラクタラッパーを追加すると InMemory 設定が干渉して意図しない動作をすることがある。

### 次回への改善提案

- タスク更新権限の仕様を明確化してドキュメントに追記すること（現状は読み取り権限と同等だが PRD には定義なし）。
- `data-testid` 属性をタスク関連コンポーネントに付与すること（E2E テスト向け）。
- サブタスクのネスト表示や進捗集計（完了率）を将来の機能として検討。
