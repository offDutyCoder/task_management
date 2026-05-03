# 設計書

## アーキテクチャ概要

既存のレイヤードアーキテクチャに対して差分変更を加える。新しいファイルは不要で、既存ファイルへの追記・修正のみで完結する。

```
Controller（変更なし）
    ↓ TaskFilterQuery（DueBefore/DueAfter 追加）
Service（変更なし）
    ↓
Repository（GetListAsync: 日付範囲 WHERE 句追加）
    ↓
SQL Server
```

```
Angular フロントエンド
  TaskFilterQuery モデル (dueBefore/dueAfter 追加)
  TaskService.getAll() (新パラメーター追加)
  TaskListComponent (フォーム拡張・columns 追加)
    ├── filterForm.dueBefore (date input)
    ├── filterForm.dueAfter  (date input)
    ├── filterForm.isRecurring (checkbox)
    └── displayedColumns: [..., 'labels']
```

## コンポーネント設計

### 1. TaskFilterQuery.cs（バックエンド）

**責務**:
- HTTP クエリパラメーターをバインドする DTO

**実装の要点**:
- `DateTime? DueBefore` と `DateTime? DueAfter` を追加
- ASP.NET Core のモデルバインディングで `?dueBefore=2026-05-10` 形式を自動解析
- 既存の `PageSize` Clamp ロジックには触れない

### 2. TaskRepository.cs（バックエンド）

**責務**:
- EF Core で SQL Server にクエリを発行

**実装の要点**:
- `GetListAsync` に以下を追加:
  ```csharp
  if (filter.DueAfter.HasValue)
      query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= filter.DueAfter.Value);
  if (filter.DueBefore.HasValue)
      query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= filter.DueBefore.Value);
  ```
- DueDate が null のタスクはフィルター指定時に除外（HasValue チェックで実現）
- 既存フィルター（statusId, assigneeId 等）の順序・パターンに揃える

### 3. task.model.ts（フロントエンド）

**責務**:
- フロントエンドの型定義

**実装の要点**:
- `TaskFilterQuery` インターフェースに `dueBefore?: string | null` と `dueAfter?: string | null` を追加
- 日付は `yyyy-MM-dd` 文字列形式（HTML date input と互換）

### 4. task.service.ts（フロントエンド）

**責務**:
- API への HTTP 通信

**実装の要点**:
- `getAll()` メソッドに以下を追加:
  ```typescript
  if (filter.dueBefore) params = params.set('dueBefore', filter.dueBefore);
  if (filter.dueAfter) params = params.set('dueAfter', filter.dueAfter);
  ```

### 5. task-list.component.ts（フロントエンド）

**責務**:
- フィルター UI の状態管理とタスク一覧表示

**実装の要点**:
- `filterForm` に `dueBefore`、`dueAfter`（string | null）、`isRecurring`（boolean | null）を追加
- `loadTasks()` の filter 構築部分に `dueBefore`/`dueAfter`/`isRecurring` を追加
- `displayedColumns` に `'labels'` を追加
- MatCheckboxModule を imports に追加

### 6. task-list.component.html（フロントエンド）

**責務**:
- フィルターパネルとテーブルの表示

**実装の要点**:
- フィルターパネルに以下を追加:
  - `<mat-form-field>` で type="date" の入力（期限（開始）・期限（終了））
  - `<mat-checkbox>` で isRecurring トグル
- テーブルに `labels` カラム定義を追加:
  - `@for (l of task.labels; track l.id)` でラベルバッジをspan出力

## データフロー

### 期限範囲フィルター適用
```
1. ユーザーが「期限（終了）」に 2026-05-15 を入力
2. (change) イベント → onFilterChange() 呼び出し
3. filterForm.getRawValue() から dueBefore: '2026-05-15' を取得
4. TaskService.getAll({ dueBefore: '2026-05-15', ... }) → GET /api/tasks?dueBefore=2026-05-15
5. TasksController → TaskService.GetTasksAsync(filter, userId)
6. TaskRepository.GetListAsync: WHERE DueDate <= '2026-05-15' 追加
7. フィルター済みタスクリストを返す
```

## エラーハンドリング戦略

### カスタムエラークラス

追加なし。既存の `NotFoundException` / `ForbiddenException` で十分。

### エラーハンドリングパターン

- バックエンド: 無効な日付形式はASP.NET Coreモデルバインディングが 400 Bad Request を返す
- フロントエンド: HTML date input は形式を強制するため不正な文字列が送られることはない
- 既存の `snackBar.open('タスク一覧の取得に失敗しました', ...)` で API エラーをカバー

## テスト戦略

### ユニットテスト
- `TaskRepository.GetListAsync_WithDueBefore_ShouldReturnOnlyTasksWithinDate`
- `TaskRepository.GetListAsync_WithDueAfter_ShouldReturnOnlyTasksAfterDate`
- `TaskRepository.GetListAsync_WithDueDateRange_ShouldReturnTasksInRange`
- `TaskRepository.GetListAsync_WithDueBefore_WhenTaskHasNullDueDate_ShouldExclude`

### 統合テスト

追加なし。既存の `TasksControllerTests` がフィルタリングの統合テストをカバー済み。今回追加するパラメーターは Repository レベルのロジックなのでユニットテストで十分。

## 依存ライブラリ

追加なし。既存のライブラリで実装可能。

## ディレクトリ構造

```
backend/
  TaskManagement.Application/
    DTOs/Tasks/
      TaskFilterQuery.cs          ← DueBefore/DueAfter 追加
  TaskManagement.Infrastructure/
    Repositories/
      TaskRepository.cs           ← GetListAsync: 日付範囲フィルター追加
  TaskManagement.Tests/
    Unit/
      Repositories/
        TaskRepositoryFilterTests.cs  ← 新規作成（日付範囲テスト）

frontend/src/app/
  models/
    task.model.ts                 ← TaskFilterQuery に dueBefore/dueAfter 追加
  services/
    task.service.ts               ← getAll(): 新パラメーター追加
  pages/tasks/task-list/
    task-list.component.ts        ← filterForm 拡張・displayedColumns 拡張
    task-list.component.html      ← フィルターパネル拡張・labels列追加
    task-list.component.scss      ← label-badge スタイル追加
```

## 実装の順序

1. バックエンド: `TaskFilterQuery.cs` に `DueBefore`/`DueAfter` 追加
2. バックエンド: `TaskRepository.cs` の `GetListAsync` に日付範囲フィルター追加
3. バックエンド: `TaskRepositoryFilterTests.cs` に日付範囲テスト追加
4. フロントエンド: `task.model.ts` の `TaskFilterQuery` 拡張
5. フロントエンド: `task.service.ts` の `getAll()` 拡張
6. フロントエンド: `task-list.component.ts` のフォーム・columns 拡張
7. フロントエンド: `task-list.component.html` のフィルターパネル・labels列追加
8. フロントエンド: `task-list.component.scss` にラベルバッジスタイル追加

## セキュリティ考慮事項

- 日付パラメーターは ASP.NET Core モデルバインディングが型チェックするため追加バリデーション不要
- EF Core パラメーターバインディングにより SQL インジェクション対策済み

## パフォーマンス考慮事項

- `TaskItems.DueDate` カラムには既にインデックス設計（architecture.md 参照）があり、日付範囲フィルターのパフォーマンスは確保済み
- 既存のページネーション（最大100件）は変更なし

## 将来の拡張性

- `TaskFilterQuery` に今後フィルター条件を追加する際は同パターンで拡張可能
- ソート機能は `sortBy`/`sortOrder` パラメーターとして同じ DTO に追加できる設計
