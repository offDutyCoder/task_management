# 要件定義: ステータス管理

## 概要

タスクのステータス（未着手・進行中・レビュー中など）をAdmin管理者が管理できる機能を実装する。

## 機能要件

### 参照 (全ユーザー)
- ステータス一覧を取得できる（`GET /api/statuses`）
- 有効なステータスのみ返す

### CRUD（管理者のみ）
- ステータスを新規作成できる（`POST /api/statuses`）
  - 入力: 名前（必須・最大50文字）、色（HEXカラーコード・必須）、表示順（必須）
- ステータスを更新できる（`PUT /api/statuses/{id}`）
  - 名前・色・表示順・有効/無効を変更可能
- ステータスを論理削除できる（`DELETE /api/statuses/{id}`）
  - 削除するステータスを使用中のタスクが存在する場合は 409 Conflict を返す
  - 物理削除ではなく `IsActive = false` にする

### 初期データ
| 名前 | 色 | 表示順 |
|------|-----|--------|
| 未着手 | #9E9E9E | 1 |
| 進行中 | #2196F3 | 2 |
| レビュー中 | #FF9800 | 3 |
| 保留 | #F44336 | 4 |
| 完了 | #4CAF50 | 5 |

## 非機能要件

- 管理者認証が必要なエンドポイントは `[Authorize(Roles = "Admin")]` で保護
- 論理削除時の競合チェックはサービス層で実施
- エラーハンドリングはExceptionHandlingMiddlewareに委譲（NotFoundException, ValidationException使用）
- フロントエンドは既存のuser-managementと同じUIパターンに従う（MatTable + MatDialog）
