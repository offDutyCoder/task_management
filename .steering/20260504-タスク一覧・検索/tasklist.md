# タスクリスト - タスク一覧・検索

## 🚨 タスク完全完了の原則

**このファイルの全タスクが完了するまで作業を継続すること**

### 必須ルール
- **全てのタスクを`[x]`にすること**
- 「時間の都合により別タスクとして実施予定」は禁止
- 「実装が複雑すぎるため後回し」は禁止
- 未完了タスク（`[ ]`）を残したまま作業を終了しない

---

## フェーズ1: バックエンド - 期限範囲フィルター実装

- [x] `TaskManagement.Application/DTOs/Tasks/TaskFilterQuery.cs` に `DueBefore`（DateTime?）と `DueAfter`（DateTime?）プロパティを追加する
- [x] `TaskManagement.Infrastructure/Repositories/TaskRepository.cs` の `GetListAsync` に `DueBefore`/`DueAfter` の WHERE 句を追加する
  - [x] `DueAfter` フィルター: `t.DueDate.HasValue && t.DueDate.Value >= filter.DueAfter.Value`
  - [x] `DueBefore` フィルター: `t.DueDate.HasValue && t.DueDate.Value <= filter.DueBefore.Value`

## フェーズ2: バックエンド - テスト

- [x] `TaskManagement.Tests/Unit/Repositories/TaskRepositoryFilterTests.cs` を新規作成し、日付範囲フィルターのテストを追加する
  - [x] `GetListAsync_WithDueBefore_ShouldReturnOnlyTasksOnOrBeforeDate` テストを実装する
  - [x] `GetListAsync_WithDueAfter_ShouldReturnOnlyTasksOnOrAfterDate` テストを実装する
  - [x] `GetListAsync_WithDueDateRange_ShouldReturnTasksInRange` テストを実装する
  - [x] `GetListAsync_WithDueBefore_WhenTaskHasNullDueDate_ShouldExcludeTask` テストを実装する

## フェーズ3: フロントエンド - モデル・サービス更新

- [x] `frontend/src/app/models/task.model.ts` の `TaskFilterQuery` インターフェースに `dueBefore?: string | null` と `dueAfter?: string | null` を追加する
- [x] `frontend/src/app/services/task.service.ts` の `getAll()` メソッドに `dueBefore`/`dueAfter` のパラメーター送信を追加する

## フェーズ4: フロントエンド - UI 拡張

- [x] `frontend/src/app/pages/tasks/task-list/task-list.component.ts` を更新する
  - [x] `filterForm` に `dueBefore`（string | null）、`dueAfter`（string | null）、`isRecurring`（boolean | null）フィールドを追加する
  - [x] `loadTasks()` 内の `filter` 構築に `dueBefore`/`dueAfter`/`isRecurring` を追加する
  - [x] `displayedColumns` に `'labels'` を追加する
  - [x] `imports` に `MatCheckboxModule` を追加する
- [x] `frontend/src/app/pages/tasks/task-list/task-list.component.html` を更新する
  - [x] フィルターパネルに「期限（開始）」date 入力フィールドを追加する（`formControlName="dueAfter"`）
  - [x] フィルターパネルに「期限（終了）」date 入力フィールドを追加する（`formControlName="dueBefore"`）
  - [x] フィルターパネルに「定期タスクのみ」チェックボックスを追加する（`formControlName="isRecurring"`）
  - [x] テーブルに `labels` カラム定義を追加する（ラベルバッジを span で表示）
- [x] `frontend/src/app/pages/tasks/task-list/task-list.component.scss` に `.label-badge` スタイルを追加する

## フェーズ5: 品質チェックと修正

- [x] バックエンドビルドが通ることを確認する
  - [x] `dotnet build` (backend ディレクトリで実行)
- [x] バックエンドテストが通ることを確認する
  - [x] `dotnet test` (backend ディレクトリで実行) - 68テスト全てパス
- [x] フロントエンドの型チェックが通ることを確認する
  - [x] `npm run typecheck` (frontend ディレクトリで実行)
- [x] フロントエンドのリントが通ることを確認する
  - [x] `npm run lint` (frontend ディレクトリで実行)

## フェーズ6: ドキュメント更新

- [ ] 実装後の振り返りをこのファイルの下部に記録する

---

## 実装後の振り返り

### 実装完了日
{YYYY-MM-DD}

### 計画と実績の差分

**計画と異なった点**:
- {計画時には想定していなかった技術的な変更点}

**新たに必要になったタスク**:
- {実装中に追加したタスク}

**技術的理由でスキップしたタスク**（該当する場合のみ）:
- なし

### 学んだこと

**技術的な学び**:
- {実装を通じて学んだ技術的な知見}

**プロセス上の改善点**:
- {タスク管理で良かった点}

### 次回への改善提案
- {次回の機能追加で気をつけること}
