# タスクリスト

## 🚨 タスク完全完了の原則

**このファイルの全タスクが完了するまで作業を継続すること**

### 必須ルール
- **全てのタスクを`[x]`にすること**
- 「時間の都合により別タスクとして実施予定」は禁止
- 「実装が複雑すぎるため後回し」は禁止
- 未完了タスク（`[ ]`）を残したまま作業を終了しない

---

## フェーズ1: バックエンド基盤変更

### 1-1. TaskRepository.GetByIdAsync を更新
- [x] `GetByIdAsync` の Shares include に `.ThenInclude(s => s.User)` を追加
  - ファイル: `backend/TaskManagement.Infrastructure/Repositories/TaskRepository.cs`
  - 変更箇所: `.Include(t => t.Shares)` → `.Include(t => t.Shares).ThenInclude(s => s.User)`

### 1-2. ITaskRepository にメソッドを追加
- [x] `IsShareUserAsync(int taskId, int userId): Task<bool>` を追加
- [x] `AddShareUserAsync(int taskId, int userId): Task` を追加
- [x] `RemoveShareUserAsync(int taskId, int userId): Task` を追加
  - ファイル: `backend/TaskManagement.Application/Interfaces/ITaskRepository.cs`

### 1-3. TaskRepository にメソッドを実装
- [x] `IsShareUserAsync` を実装（TaskShares で TaskItemId + UserId の AnyAsync）
- [x] `AddShareUserAsync` を実装（TaskShare を追加して SaveChangesAsync）
- [x] `RemoveShareUserAsync` を実装（TaskShare を検索して削除して SaveChangesAsync）
  - ファイル: `backend/TaskManagement.Infrastructure/Repositories/TaskRepository.cs`
  - パターン: 既存の `IsAssigneeAsync` / `AddAssigneeAsync` / `RemoveAssigneeAsync` と対称に実装

### 1-4. TaskDetailResponse.cs を更新
- [x] `List<int> ShareUserIds` → `List<AssigneeDto> ShareUsers` に変更
  - ファイル: `backend/TaskManagement.Application/DTOs/Tasks/TaskDetailResponse.cs`

### 1-5. ITaskService にメソッドを追加
- [x] `AddShareUserAsync(int taskId, int userId, int currentUserId): Task<AssigneeDto>` を追加
- [x] `RemoveShareUserAsync(int taskId, int userId, int currentUserId): Task` を追加
  - ファイル: `backend/TaskManagement.Application/Interfaces/ITaskService.cs`

### 1-6. TaskService を更新
- [x] `MapToDetailResponse` の `ShareUserIds = task.Shares.Select(s => s.UserId).ToList()` を
  `ShareUsers = task.Shares.Select(s => new AssigneeDto { Id = s.UserId, DisplayName = s.User?.DisplayName ?? string.Empty }).ToList()` に変更
- [x] `AddShareUserAsync` を実装
  - EnsureReadAccessAsync でアクセスチェック
  - UserRepository でユーザー存在・有効性チェック
  - IsShareUserAsync で重複チェック → ConflictException
  - AddShareUserAsync 呼び出し
  - AssigneeDto を返す
- [x] `RemoveShareUserAsync` を実装
  - EnsureReadAccessAsync でアクセスチェック
  - `task.CreatedByUserId == userId` なら ForbiddenException（作成者は削除不可）
  - IsShareUserAsync で存在確認 → NotFoundException
  - RemoveShareUserAsync 呼び出し
  - ファイル: `backend/TaskManagement.Application/Services/TaskService.cs`

### 1-7. TasksController にエンドポイントを追加
- [x] `POST /api/tasks/{id}/shares` エンドポイントを追加 → `AddShareUserAsync` 呼び出し → `201 Created { AssigneeDto }`
- [x] `DELETE /api/tasks/{id}/shares/{userId}` エンドポイントを追加 → `RemoveShareUserAsync` 呼び出し → `204 NoContent`
  - ファイル: `backend/TaskManagement.Api/Controllers/TasksController.cs`
  - パターン: 既存の `AddAssignee` / `RemoveAssignee` と対称に実装

---

## フェーズ2: バックエンドユニットテスト

### 2-1. TaskServiceTests に共有ユーザーテストを追加
- [x] `AddShareUserAsync_WhenUserHasAccess_ShouldAddShareAndReturnDto` テスト追加
- [x] `AddShareUserAsync_WhenUserAlreadyShared_ShouldThrowConflictException` テスト追加
- [x] `AddShareUserAsync_WhenTargetUserIsInactive_ShouldThrowNotFoundException` テスト追加
- [x] `RemoveShareUserAsync_WhenRemovingCreator_ShouldThrowForbiddenException` テスト追加
- [x] `RemoveShareUserAsync_WhenUserNotInShareList_ShouldThrowNotFoundException` テスト追加
- [x] `RemoveShareUserAsync_WhenValidUser_ShouldRemoveShare` テスト追加
  - ファイル: `backend/TaskManagement.Tests/Unit/Services/TaskServiceTests.cs`
  - パターン: 既存の AddAssignee/RemoveAssignee テストと対称に実装

---

## フェーズ3: フロントエンド更新

### 3-1. task.model.ts を更新
- [x] `TaskDetailResponse.shareUserIds: number[]` → `shareUsers: AssigneeDto[]` に変更
  - ファイル: `frontend/src/app/models/task.model.ts`
  - 注意: `CreateTaskRequest.shareUserIds` と `UpdateTaskRequest.shareUserIds` は変更しない

### 3-2. task.service.ts を更新
- [x] `addShare(taskId: number, userId: number): Observable<AssigneeDto>` を追加
- [x] `removeShare(taskId: number, userId: number): Observable<void>` を追加
  - ファイル: `frontend/src/app/services/task.service.ts`
  - パターン: 既存の `addAssignee` / `removeAssignee` と対称に実装

### 3-3. task-form.component.ts を更新
- [x] `shareUserIds: [this.data.task?.shareUserIds ?? []]` を
  `shareUserIds: [this.data.task?.shareUsers?.map(u => u.id) ?? []]` に変更
  - ファイル: `frontend/src/app/pages/tasks/task-form/task-form.component.ts`

### 3-4. task-detail.component.ts を更新
- [x] `openAddShareDialog()` メソッドを追加（AssigneeSelectDialogComponent を流用）
- [x] `removeShare(userId: number)` メソッドを追加（taskService.removeShare 呼び出し）
  - ファイル: `frontend/src/app/pages/tasks/task-detail/task-detail.component.ts`

### 3-5. task-detail.component.html に共有ユーザーセクションを追加
- [x] ラベルセクションの後に「共有ユーザー」セクションを追加
  - サブタスクの場合（`t.parentTask != null`）は「親タスクの設定を継承」と表示
  - 通常タスクの場合は共有ユーザー一覧 + 削除ボタン + 追加ボタンを表示
  - 0人の場合は「未設定」と表示
  - ファイル: `frontend/src/app/pages/tasks/task-detail/task-detail.component.html`
  - UIパターン: 担当者セクション（meta-row, assignee-list等）と同様

---

## フェーズ4: 品質チェックと修正

- [x] バックエンドのビルドが成功することを確認
  - [x] `cd backend && dotnet build`
- [x] バックエンドのテストがパスすることを確認
  - [x] `cd backend && dotnet test`（58テスト全パス）
- [x] フロントエンドのビルドが成功することを確認
  - [x] `cd frontend && npm run build`
- [x] フロントエンドのlintエラーがないことを確認
  - [x] `cd frontend && npm run lint`
- [x] フロントエンドの型エラーがないことを確認
  - [x] `cd frontend && npx tsc --noEmit`（exit code: 0）

---

## 実装後の振り返り

### 実装完了日
2026-05-04

### 計画と実績の差分

**計画と異なった点**:
- バリデーターの指摘により、テストケースを当初計画の6つから9つに増加した（TaskNotFound、UserNotFound系を補完）
- フロントエンドのエラーハンドリングで 403/409 を区別するために `HttpErrorResponse` のインポートを追加した（計画時に未記載だったが必要と判断）

**新たに必要になったタ��ク**:
- `AddShareUserAsync_WhenTaskNotFound_ShouldThrowNotFoundException` テスト追加（バリデーター指摘）
- `AddShareUserAsync_WhenUserNotFound_ShouldThrowNotFoundException` テスト追加（バリデーター指摘）
- `RemoveShareUserAsync_WhenTaskNotFound_ShouldThrowNotFoundException` テスト追加（バリデーター指摘）
- フロントエンドの 403/409 区別エラーメッセージ実装（バリデーター推奨）

**技術的理由でスキップしたタスク**:
- なし（全タスク完了）

### 学んだこと

**技術的な学び**:
- `ThenInclude(s => s.User)` の追加は一行の変更だが、N+1 問題の防止に重要。既存の AssigneeDto パターンを流用する際は、必ずナビゲーションプロパティのIncludeが揃っているかを確認する
- `EnsureReadAccessAsync` は「読み取り権限」チェックを担うため、書き込み操作（Add/Remove）でも利用すると権限設計が曖昧になりうる。将来的には `EnsureWriteAccessAsync` 等の役割分担を検討する価値がある

**プロセス上の改善点**:
- tasklist.md をフェーズ別に管理したことで、実装の進捗とドキュメントの一致が保ちやすかった
- implementation-validator による第三者レビューで、テスト網羅性の漏れ（TaskNotFound/UserNotFound系）を事前に発見できた

### 次回への改善提案
- 新機能を既存の Assignee パターンに追加する場合、インターフェース・サービス・リポジトリ・コントローラー・テスト・フロントエンドの7箇所を対称的に追加するチェックリストとしてまとめると効率的
- フロントエンドのエ���ーハンドリング共通化（status → message マッピング）は task-detail 全体で共通ユーティリティとして切り出す価値がある
