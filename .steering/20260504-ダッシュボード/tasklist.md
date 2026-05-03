# タスクリスト - ダッシュボード

## 🚨 タスク完全完了の原則

**このファイルの全タスクが完了するまで作業を継続すること**

---

## フェーズ1: バックエンド実装

- [x] `DashboardResponse.cs` DTO を作成する
  - [x] `backend/TaskManagement.Application/DTOs/Dashboard/DashboardResponse.cs` を新規作成
  - [x] `MyTasks: List<TaskListItem>` と `TeamTasks: List<TaskListItem>` を含む

- [x] `IDashboardService.cs` インターフェースを作成する
  - [x] `backend/TaskManagement.Application/Interfaces/IDashboardService.cs` を新規作成
  - [x] `GetDashboardAsync(int currentUserId, bool includeRecurring)` を定義

- [x] `DashboardService.cs` サービスを実装する
  - [x] `backend/TaskManagement.Application/Services/DashboardService.cs` を新規作成
  - [x] `ITaskRepository` を DI で注入
  - [x] マイタスク取得: `GetListAsync({ AssigneeId = currentUserId, IsRecurring = false })` を呼び出す
  - [x] チーム共有タスク取得: `GetListAsync({ IsRecurring = false })` を呼び出す
  - [x] `includeRecurring = true` の場合は `IsRecurring = null` にする

- [x] `DashboardController.cs` コントローラーを作成する
  - [x] `backend/TaskManagement.Api/Controllers/DashboardController.cs` を新規作成
  - [x] `[Route("api/dashboard")]`, `[Authorize]` を付与
  - [x] `GET /api/dashboard?includeRecurring=false` エンドポイントを実装

- [x] `Program.cs` に DI 登録を追加する
  - [x] `builder.Services.AddScoped<IDashboardService, DashboardService>()` を追加

## フェーズ2: バックエンド テスト

- [x] `DashboardServiceTests.cs` ユニットテストを作成する
  - [x] `backend/TaskManagement.Tests/Unit/Services/DashboardServiceTests.cs` を新規作成
  - [x] `GetDashboardAsync_ShouldReturnMyTasksFilteredByAssignee` テストを実装
  - [x] `GetDashboardAsync_WhenIncludeRecurringFalse_ShouldExcludeRecurring` テストを実装

## フェーズ3: フロントエンド実装

- [x] `task.model.ts` に `DashboardResponse` インターフェースを追加する

- [x] `dashboard.service.ts` を作成する
  - [x] `frontend/src/app/services/dashboard.service.ts` を新規作成
  - [x] `getDashboard(includeRecurring: boolean): Observable<DashboardResponse>` を実装
  - [x] `GET /api/dashboard?includeRecurring={value}` を呼び出す

- [x] `dashboard.component.ts` を作成する
  - [x] `frontend/src/app/pages/dashboard/dashboard.component.ts` を新規作成
  - [x] `myTasks`, `teamTasks` を `signal<TaskListItem[]>` で保持
  - [x] `isLoading`, `showRecurring` を signal で保持
  - [x] `ngOnInit()` で `loadDashboard()` を呼び出す
  - [x] `isOverdue(dueDate: string | null): boolean` ヘルパーを実装
  - [x] トグル変更時に再読み込みするメソッドを実装

- [x] `dashboard.component.html` を作成する
  - [x] マイタスクセクション (mat-card + mat-table)
  - [x] チーム共有タスクセクション (mat-card + mat-table)
  - [x] 期限超過行に `overdue-row` CSS クラスを適用
  - [x] 定期タスク表示トグル (mat-slide-toggle) を各セクションに配置

- [x] `dashboard.component.scss` を作成する
  - [x] `.overdue` スタイル（赤色テキスト）を定義
  - [x] セクション間のマージンを定義

- [x] `app.routes.ts` にダッシュボードルートを追加する
  - [x] `dashboard` ルートを追加 (canActivate: [authGuard])
  - [x] デフォルトルート `''` のリダイレクト先を `'dashboard'` に変更

## フェーズ4: 品質チェックと修正

- [x] すべてのテストが通ることを確認する
  - [x] バックエンド: `dotnet test` — 64 passed, 0 failed
- [x] リントエラーがないことを確認する
  - [x] `npm run lint` — All files pass linting
- [x] 型エラーがないことを確認する
  - [x] `npm run typecheck` — No errors
- [x] ビルドが成功することを確認する
  - [x] `npm run build` — Application bundle generation complete

---

## 実装後の振り返り

### 実装完了日
2026-05-04

### 計画と実績の差分

**計画と異なった点**:
- `showRecurring` は両セクション共有の単一シグナルとして実装し、トグルはページヘッダーに1つだけ配置（当初設計ではカードヘッダー内に2つ置いていたが、implementation-validator の指摘を受けて修正）
- `isOverdue()` の日付比較ロジックをタイムゾーン安全な ISO 文字列比較に変更（`new Date().toISOString().slice(0, 10)`）
- 日付表示フォーマットを `MM/dd` から `yy/MM/dd` に変更（年跨ぎ対応）
- `DashboardController.GetCurrentUserId()` を `int.TryParse` を使用した安全な実装に変更
- `MapToListItem` を `DashboardService` 内に private static として複製（共有ヘルパー抽出は過剰な抽象化のため見送り）

**新たに必要になったタスク**:
- implementation-validator の指摘対応（UX/安全性の修正 4 件）

**技術的理由でスキップしたタスク**: なし

### 学んだこと

**技術的な学び**:
- `ITaskRepository.GetListAsync` の `AssigneeId` フィルターで「マイタスク」を自然に取得できた（新規リポジトリメソッド不要）
- バックエンド `bool? IsRecurring` フィルターで `null` = 全件、`false` = 非定期のみを切り替えられる
- Angular の `mat-slide-toggle` は `[checked]` バインディングで signal 値と連携できる
- `new Date().toISOString().slice(0, 10)` による ISO 日付文字列比較がタイムゾーンに依存しない安全なアプローチ

**プロセス上の改善点**:
- 既存の `GetListAsync` がフィルター拡張済みだったため、新たなリポジトリメソッドの追加なしに実装できた

### 次回への改善提案
- `DashboardService.MapToListItem` と `TaskService.MapToListItem` の重複を共有マッパークラスに抽出する（リファクタリングフェーズ）
- 統合テスト `DashboardControllerTests.cs` を追加する（認証済み/未認証の 200/401 検証）
- `TeamTasks` の `PageSize = 100` 上限と「もっと見る」リンクの設計を検討する
