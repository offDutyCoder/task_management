# タスクリスト: ステータス管理

## バックエンド実装

- [x] B-01: Domain/Entities/TaskStatus.cs エンティティ作成
- [x] B-02: Application/DTOs/Statuses/ DTOs作成 (StatusResponse, CreateStatusRequest, UpdateStatusRequest)
- [x] B-03: Application/Exceptions/ConflictException.cs 作成
- [x] B-04: Application/Interfaces/IStatusRepository.cs インターフェース作成
- [x] B-05: Application/Interfaces/IStatusService.cs インターフェース作成
- [x] B-06: Application/Services/StatusService.cs 実装
- [x] B-07: Infrastructure/Data/AppDbContext.cs に TaskStatuses DbSet 追加
- [x] B-08: Infrastructure/Data/Configurations/TaskStatusConfiguration.cs 作成（シードデータ含む）
- [x] B-09: Infrastructure/Repositories/StatusRepository.cs 実装
- [x] B-10: Api/Controllers/StatusesController.cs 実装
- [x] B-11: Api/Program.cs に DI 登録追加
- [x] B-12: Api/Middleware/ExceptionHandlingMiddleware.cs に ConflictException ハンドリング追加

## フロントエンド実装

- [x] F-01: frontend/src/app/models/task-status.model.ts 作成
- [x] F-02: frontend/src/app/services/status.service.ts 作成
- [x] F-03: frontend/src/app/pages/admin/status-management/ ディレクトリ作成
- [x] F-04: status-management.component.ts / .html / .scss 作成
- [x] F-05: status-dialog.component.ts 作成（inline template）
- [x] F-06: frontend/src/app/app.routes.ts にルート追加

## テスト実装

- [x] T-01: backend/TaskManagement.Tests/Unit/Services/StatusServiceTests.cs 作成
- [x] T-02: backend/TaskManagement.Tests/Integration/Controllers/StatusesControllerTests.cs 作成

## 申し送り事項

### 実装完了日
2026-05-03

### 計画と実績の差分
- 計画通り全タスクを完了 (B-01〜B-12, F-01〜F-06, T-01〜T-02)
- バリデーター検証の結果、以下を計画外で追加実装:
  - DTOへのDataAnnotationsバリデーション追加
  - `IStatusRepository.ExistsNameAsync` 追加（重複名チェック）
  - `TaskStatusConfiguration` に Name の一意インデックス追加
  - `StatusService.CreateAsync/UpdateAsync` に重複名チェック追加
  - `openEditDialog` のエラーハンドリング細分化
  - 統合テストのシードデータ重複バグ修正

### 学んだこと
- **C# 名前衝突**: `System.Threading.Tasks.TaskStatus` (enum) との衝突。Global usings が有効な.NET 6+ プロジェクトでは `TaskStatus` という名前のドメインエンティティが必ず衝突する。対策: `using TaskStatus = TaskManagement.Domain.Entities.TaskStatus;` という using alias を各ファイルに設定。次回 TaskItem など System名前空間と被る名前のエンティティを定義する際は最初から using alias を使う。
- **InMemory HasData**: EF Core の InMemory プロバイダーで `EnsureCreated()` を呼ぶと `HasData()` のシードデータが自動投入される。統合テストで同じ ID のエンティティを手動で追加するとDuplicate key エラーになる。

### 未完了・申し送り事項
- `StatusRepository.ExistsUsedByTaskAsync` が常に `false` を返すスタブ。`TaskItem` エンティティ実装時に実際のFK参照チェックを追加すること
- 管理画面での無効ステータスの再有効化：現在 `GetAllAsync` は `IsActive = true` のみ返すため、無効化されたステータスが一覧から消える。タスク管理機能実装時に管理者向け全件取得エンドポイントの追加を検討すること
- `GetById` を管理者限定にしているが、将来タスク作成画面でステータスID検索が必要になった場合は `[Authorize]` に緩和すること

### 次回への改善提案
- ドメインエンティティ名は `System.Threading.Tasks.*` / `System.Collections.*` などとの衝突を事前確認する
- 統合テストのシードデータ投入は `HasData` と重複しないよう、シードデータを使う場合はIDを既存シードID以外（例: 100+）から始める
