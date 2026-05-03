# 設計書

## アーキテクチャ概要

既存のレイヤードアーキテクチャ（Controller → Service → Repository）に準拠して実装する。担当者（Assignee）の個別追加・削除パターンを共有ユーザーにも適用する。

```
[フロントエンド]
  task-detail.component (共有ユーザー表示・追加・削除)
      ↓ addShare(taskId, userId) / removeShare(taskId, userId)
  task.service.ts
      ↓ POST/DELETE /api/tasks/{id}/shares

[バックエンド]
  TasksController
      ↓ AddShareUserAsync / RemoveShareUserAsync
  TaskService
      ↓ IsShareUserAsync / AddShareUserAsync / RemoveShareUserAsync
  TaskRepository → SQL Server (TaskShares テーブル)
```

## コンポーネント設計

### 1. TaskDetailResponse (DTO変更)

**責務**:
- タスク詳細のレスポンスに共有ユーザーの表示名を含める

**実装の要点**:
- `List<int> ShareUserIds` → `List<AssigneeDto> ShareUsers` に変更
- `AssigneeDto` を再利用（`{ Id, DisplayName }` の構造が同一）
- フロントエンドの `TaskDetailResponse.shareUserIds` も対応して変更

### 2. ITaskRepository / TaskRepository

**責務**:
- 共有ユーザーの個別追加・削除・存在確認

**実装の要点**:
- `GetByIdAsync` の Shares include に `.ThenInclude(s => s.User)` を追加（表示名取得のため）
- `IsShareUserAsync(int taskId, int userId): Task<bool>` を追加
- `AddShareUserAsync(int taskId, int userId): Task` を追加
- `RemoveShareUserAsync(int taskId, int userId): Task` を追加
- 命名は `AddAssigneeAsync` / `RemoveAssigneeAsync` と対称に

### 3. ITaskService / TaskService

**責務**:
- 共有ユーザー追加・削除のビジネスルール適用

**実装の要点**:
- `AddShareUserAsync(int taskId, int userId, int currentUserId)` を追加
  - アクセスチェック（EnsureReadAccessAsync）
  - 対象ユーザーの存在・有効性チェック
  - 重複チェック（IsShareUserAsync）→ ConflictException
  - AddShareUserAsync 呼び出し
  - AssigneeDto を返す（表示名のため）
- `RemoveShareUserAsync(int taskId, int userId, int currentUserId)` を追加
  - アクセスチェック
  - **作成者の削除を禁止**: `task.CreatedByUserId == userId` なら ForbiddenException
  - IsShareUserAsync で存在確認 → NotFoundException
  - RemoveShareUserAsync 呼び出し
- サブタスクへの共有操作を禁止: `task.ParentTaskId.HasValue` なら BadRequestException（またはエラーレスポンス）

### 4. TasksController

**責務**:
- 共有ユーザー追加・削除のHTTPエンドポイント

**実装の要点**:
- `POST /api/tasks/{id}/shares` → `AddShareUserAsync` を呼び出し `201 Created` で `AssigneeDto` を返す
- `DELETE /api/tasks/{id}/shares/{userId}` → `RemoveShareUserAsync` を呼び出し `204 NoContent` を返す
- 既存の `AddAssignee` / `RemoveAssignee` のパターンと対称に実装

### 5. task-detail.component (フロントエンド)

**責務**:
- 共有ユーザーの表示・追加・削除UI

**実装の要点**:
- `task-detail.component.html` に「共有ユーザー」セクションを追加（担当者セクションと同様の構造）
- サブタスクの場合は「親タスクの設定を継承」と表示する（`t.parentTask != null` で判定）
- `openAddShareDialog()` メソッド: 既存の `AssigneeSelectDialogComponent` を流用
  - 既に共有済みのユーザーを除外したリストを渡す
- `removeShare(userId: number)` メソッド: `taskService.removeShare()` を呼び出し

### 6. task.service.ts (フロントエンド)

**責務**:
- 共有ユーザーAPIへのHTTP通信

**実装の要点**:
- `addShare(taskId: number, userId: number): Observable<AssigneeDto>` を追加
- `removeShare(taskId: number, userId: number): Observable<void>` を追加
- `addAssignee` と対称の実装

### 7. task.model.ts (フロントエンド)

**責務**:
- APIレスポンスの型定義を更新

**実装の要点**:
- `TaskDetailResponse.shareUserIds: number[]` → `shareUsers: AssigneeDto[]`
- `CreateTaskRequest.shareUserIds: number[]` は変更なし（送信はIDのみ）
- `UpdateTaskRequest.shareUserIds: number[]` は変更なし（送信はIDのみ）

### 8. task-form.component.ts (フロントエンド)

**責務**:
- フォームの初期値取得を更新

**実装の要点**:
- `shareUserIds: [this.data.task?.shareUserIds ?? []]` → `shareUserIds: [this.data.task?.shareUsers?.map(u => u.id) ?? []]`

## データフロー

### 共有ユーザー追加
```
1. ユーザーが「共有ユーザー追加」ボタンをクリック
2. AssigneeSelectDialogComponent を開く（既存共有ユーザーを除外したリストを渡す）
3. ユーザーを選択してダイアログを閉じる
4. TaskService.addShare(taskId, userId) を呼び出す
5. POST /api/tasks/{id}/shares { userId }
6. TasksController → TaskService.AddShareUserAsync → TaskRepository.AddShareUserAsync
7. 成功後 loadTask() を呼び出してUIを再描画
```

### 共有ユーザー削除
```
1. ユーザーが削除ボタンをクリック
2. TaskService.removeShare(taskId, userId) を呼び出す
3. DELETE /api/tasks/{id}/shares/{userId}
4. TasksController → TaskService.RemoveShareUserAsync → TaskRepository.RemoveShareUserAsync
5. 成功後 loadTask() を呼び出してUIを再描画
```

### タスク詳細取得（共有ユーザー表示名の取得）
```
1. GET /api/tasks/{id}
2. TaskRepository.GetByIdAsync (Shares.ThenInclude(s => s.User) を含む)
3. TaskService.MapToDetailResponse: task.Shares → List<AssigneeDto>
4. フロントエンド: t.shareUsers を表示名とともにレンダリング
```

## エラーハンドリング戦略

### バックエンドのエラーケース

| ケース | 例外 | HTTPステータス |
|--------|------|----------------|
| タスクが存在しない | NotFoundException | 404 |
| アクセス権なし（閲覧不可） | ForbiddenException | 403 |
| 対象ユーザーが存在しない/無効 | NotFoundException | 404 |
| 既に共有済み | ConflictException | 409 |
| 作成者を共有から削除しようとした | ForbiddenException | 403 |
| サブタスクに共有設定しようとした | ValidationException | 400 |

### フロントエンドのエラーハンドリング
- 各操作（追加・削除）は try/catch で囲み、失敗時に MatSnackBar でエラー表示
- 既存の担当者操作と同様のパターン

## テスト戦略

### ユニットテスト (TaskServiceTests.cs に追加)

- `AddShareUserAsync_WhenUserIsNotCreatorAndHasAccess_ShouldAddShare`
- `AddShareUserAsync_WhenUserAlreadyShared_ShouldThrowConflictException`
- `AddShareUserAsync_WhenTargetUserIsInactive_ShouldThrowNotFoundException`
- `RemoveShareUserAsync_WhenRemovingCreator_ShouldThrowForbiddenException`
- `RemoveShareUserAsync_WhenUserNotInShareList_ShouldThrowNotFoundException`
- `RemoveShareUserAsync_WhenValidUser_ShouldRemoveShare`

### 統合テスト (AssigneesControllerTests.csを参考に)
- 既存の統合テストは変更なし（TasksControllerTests はそのまま）

## 依存ライブラリ

新規追加ライブラリなし。既存の Angular Material, ASP.NET Core を使用。

## ディレクトリ構造

```
backend/
├── TaskManagement.Application/
│   ├── DTOs/Tasks/
│   │   └── TaskDetailResponse.cs        ← ShareUserIds → ShareUsers 変更
│   ├── Interfaces/
│   │   ├── ITaskService.cs              ← AddShareUserAsync, RemoveShareUserAsync 追加
│   │   └── ITaskRepository.cs           ← IsShareUserAsync, AddShareUserAsync, RemoveShareUserAsync 追加
│   └── Services/
│       └── TaskService.cs               ← 2メソッド追加 + MapToDetailResponse 更新
├── TaskManagement.Infrastructure/
│   ├── Data/Configurations/
│   │   └── (変更なし)
│   └── Repositories/
│       └── TaskRepository.cs            ← GetByIdAsync 更新 + 3メソッド追加
├── TaskManagement.Api/
│   └── Controllers/
│       └── TasksController.cs           ← 2エンドポイント追加
└── TaskManagement.Tests/
    └── Unit/Services/
        └── TaskServiceTests.cs          ← 6テストケース追加

frontend/src/app/
├── models/
│   └── task.model.ts                    ← shareUserIds → shareUsers 変更
├── services/
│   └── task.service.ts                  ← addShare, removeShare 追加
└── pages/tasks/task-detail/
    ├── task-detail.component.ts          ← openAddShareDialog, removeShare 追加
    └── task-detail.component.html        ← 共有ユーザーセクション追加
```

## 実装の順序

1. バックエンド基盤変更（Repository → Service → Controller）
2. バックエンドDTO更新（TaskDetailResponse）
3. バックエンドユニットテスト追加
4. フロントエンドモデル更新（task.model.ts）
5. フロントエンドサービス更新（task.service.ts）
6. フロントエンドUIコンポーネント更新（task-detail, task-form）

## セキュリティ考慮事項

- 作成者を共有ユーザーから削除できない（作成者は常にアクセス可能を保証）
- サブタスクへの直接共有設定は禁止（親タスク継承のルールを保護）
- 共有操作はアクセス権チェック後に実行（EnsureReadAccessAsync）
- APIレベルで公開範囲外のタスクへのアクセスを拒否（既存ロジック維持）

## パフォーマンス考慮事項

- `GetByIdAsync` で Shares に `.ThenInclude(s => s.User)` を追加（追加JOINが1回増えるが許容範囲）
- `TaskShares(UserId)` インデックスは既に設計書に記載済み（パフォーマンス問題なし）

## 将来の拡張性

- 部署・グループ単位の共有設定（Post-MVP）に備え、TaskShare を User 単位で持つ設計は維持
- 共有ユーザー変更通知は NotificationService に追加可能
