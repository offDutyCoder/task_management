# 要求内容

## 概要

タスク一覧画面に期限範囲フィルター（dueBefore/dueAfter）と定期タスクフラグフィルター（isRecurring）を追加し、機能設計書に定義された全フィルター条件をバックエンド・フロントエンド双方で完全に実装する。また、テーブルにラベル列を追加して視認性を向上させる。

## 背景

`GET /api/tasks` の仕様（機能設計書）では `dueBefore`/`dueAfter`/`isRecurring` をクエリパラメーターとしてサポートするが、現状の実装では以下が未実装:
- バックエンド: `TaskFilterQuery` に `DueBefore`/`DueAfter` が存在しない
- バックエンド: `TaskRepository.GetListAsync` が日付範囲フィルターを適用していない
- フロントエンド: `TaskFilterQuery` モデルに `dueBefore`/`dueAfter` が存在しない
- フロントエンド: フィルターパネルに期限範囲入力・定期タスクトグルが存在しない
- フロントエンド: タスクテーブルにラベル列が表示されていない

## 実装対象の機能

### 1. バックエンド: 期限範囲フィルター追加
- `TaskFilterQuery` に `DueBefore`（DateTime?）と `DueAfter`（DateTime?）プロパティを追加
- `TaskRepository.GetListAsync` で `DueBefore`/`DueAfter` に基づく WHERE 句を追加
- `DueBefore` は `DueDate <= value`、`DueAfter` は `DueDate >= value` で絞り込み

### 2. フロントエンド: フィルターパネル拡張
- `TaskFilterQuery` モデルに `dueBefore`/`dueAfter` フィールドを追加
- `TaskService.getAll()` で `dueBefore`/`dueAfter` を HTTP パラメーターに追加
- フィルターフォームに期限範囲（開始日・終了日）の date 入力フィールドを追加
- フィルターフォームに「定期タスクのみ表示」チェックボックス（isRecurring）を追加
- フィルター変更時に `onFilterChange()` が呼ばれる既存の仕組みに接続

### 3. フロントエンド: タスクテーブルへのラベル列追加
- `displayedColumns` に `labels` を追加
- テーブルにラベルバッジ（mat-chip 相当のspan）を表示するカラムを追加

## 受け入れ条件

### 期限範囲フィルター
- [ ] GET /api/tasks?dueBefore=2026-05-10 でDueDate <= 2026-05-10 のタスクのみ返る
- [ ] GET /api/tasks?dueAfter=2026-05-01 でDueDate >= 2026-05-01 のタスクのみ返る
- [ ] dueBefore と dueAfter を組み合わせて期限範囲で絞り込みできる
- [ ] DueDate が null のタスクは日付範囲フィルター指定時に除外される

### フロントエンド UI
- [ ] フィルターパネルに「期限（開始）」と「期限（終了）」のdate入力が表示される
- [ ] フィルターパネルに「定期タスクのみ」チェックボックスが表示される
- [ ] 期限範囲やisRecurringを変更するとリスト再取得が実行される
- [ ] タスクテーブルにラベル列（色付きバッジ）が表示される

### テスト
- [ ] TaskRepository の日付範囲フィルタリングのユニットテストが通る

## 成功指標

- 機能設計書記載の全フィルターパラメーターがバックエンドで動作すること
- フロントエンドのフィルターパネルが機能設計書 UI 設計図に一致すること

## スコープ外

以下はこのフェーズでは実装しません:
- ソート機能（列ヘッダークリックによる並び替え）
- 全文検索インデックス（SQL Server CONTAINS 関数への切り替え）
- フィルター条件の URL パラメーター保存
- 期限超過ハイライト（別途機能として設計必要）

## 参照ドキュメント

- `docs/functional-design.md` - API設計・UI設計図（フィルターパネル仕様）
- `docs/architecture.md` - レイヤードアーキテクチャ
- `docs/development-guidelines.md` - コーディング規約
