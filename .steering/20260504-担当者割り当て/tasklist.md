---
feature: 担当者割り当て
date: 2026-05-04
---

# タスクリスト: 担当者割り当て

## バックエンド

- [x] `AddAssigneeRequest.cs` DTO を作成する
- [x] `ITaskRepository` に `AddAssigneeAsync` / `RemoveAssigneeAsync` / `IsAssigneeAsync` を追加する
- [x] `TaskRepository` に上記メソッドを実装する
- [x] `ITaskService` に `AddAssigneeAsync` / `RemoveAssigneeAsync` を追加する
- [x] `TaskService` に上記メソッドを実装する（バリデーション含む）
- [x] `TasksController` に `POST /api/tasks/{id}/assignees` エンドポイントを追加する
- [x] `TasksController` に `DELETE /api/tasks/{id}/assignees/{userId}` エンドポイントを追加する
- [x] `TaskServiceTests` に担当者追加・削除のユニットテストを追加する
- [x] `TasksControllerTests` に担当者エンドポイントの統合テストを追加する

## フロントエンド

- [x] `task.service.ts` に `addAssignee` / `removeAssignee` メソッドを追加する
- [x] `TaskListComponent` にユーザーロードと担当者フィルターを追加する
- [x] `task-list.component.html` のフィルターパネルに担当者ドロップダウンを追加する
- [x] `AssigneeSelectDialogComponent` を新規作成する
- [x] `TaskDetailComponent` に担当者追加・削除ボタンと処理を追加する
- [x] `task-detail.component.html` に担当者管理UIを追加する

---

## 申し送り事項

**実装完了日:** 2026-05-04

### 計画と実績の差分

- 計画通り全タスク完了。追加作業として以下の既存バグを修正した:
  - `task-list.component.html`: `.map().join()` を Angular テンプレートで直接使用していた（NG5002エラー）→ `getAssigneeNames()` ヘルパーに置き換え
  - `task-detail.component.html`: `@else if (expr; as t)` 構文はAngularで無効（`as` は主`@if`のみ有効）→ `@else { @if (expr; as t) { ... } }` に修正

### 学んだこと

- Angular 17 のビルトイン制御フロー構文 (`@if`, `@else if`) では `as` エイリアスはプライマリ `@if` ブロックのみに制限される
- Angular テンプレートにアロー関数を含む式（`.map(x => ...)` 等）は NG5002 エラーとなるため、コンポーネントクラスにメソッドを切り出す必要がある
- `TaskService` コンストラクターに依存を追加する際、既存ユニットテストが `new TaskService(mockRepo)` を直接インスタンス化していたため、モックを追加する必要があった

### 次回への改善提案

- 担当者追加時に通知を送る `NotificationService` の実装（設計書に未実装として記載済み）
- タスク一覧の担当者フィルターでログインユーザー自身を素早く選択できる「自分のタスク」ボタンの追加
- `TaskService` の依存が増えてきたため、将来的なリファクタリング候補として `ITaskAssigneeService` の分離を検討
