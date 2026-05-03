# リポジトリ構造定義書 (Repository Structure Document)

## プロジェクト構造（全体）

```
task_management/                        # リポジトリルート
├── backend/                            # ASP.NET Core ソリューション
│   ├── TaskManagement.sln
│   ├── TaskManagement.Api/             # Web API プロジェクト（エントリポイント）
│   ├── TaskManagement.Application/     # アプリケーション層（ビジネスロジック）
│   ├── TaskManagement.Domain/          # ドメイン層（エンティティ・列挙型）
│   ├── TaskManagement.Infrastructure/  # インフラ層（DB・ジョブ・外部依存）
│   └── TaskManagement.Tests/           # テストプロジェクト
├── frontend/                           # Angular プロジェクト
│   ├── src/
│   └── ...
├── docs/                               # プロジェクトドキュメント
├── .claude/                            # Claude Code 設定
├── .steering/                          # 作業ステアリングファイル
└── .gitignore
```

---

## バックエンド構造

### TaskManagement.Api/（プレゼンテーション層）

**役割**: HTTP リクエストの受付・レスポンス、認証・認可の適用、DTO 変換

```
TaskManagement.Api/
├── Controllers/
│   ├── AuthController.cs           # ログイン・ログアウト・パスワード変更
│   ├── TasksController.cs          # タスク CRUD・フィルター・検索
│   ├── StatusesController.cs       # ステータス管理（管理者のみ）
│   ├── LabelsController.cs         # ラベル管理・リクエスト
│   ├── RecurringTemplatesController.cs  # 定期タスクテンプレート管理
│   ├── UsersController.cs          # ユーザー管理
│   └── NotificationsController.cs  # 通知一覧・既読処理
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  # グローバル例外ハンドリング
├── appsettings.json                # 環境設定（接続文字列等）
├── appsettings.Development.json
└── Program.cs                      # アプリケーション起動・DI 設定
```

**命名規則**:
- コントローラー: `{リソース名}Controller.cs`（PascalCase）
- ミドルウェア: `{機能名}Middleware.cs`

**依存関係**:
- 依存可能: `TaskManagement.Application`
- 依存禁止: `TaskManagement.Infrastructure`（直接参照禁止・DI 経由のみ）

---

### TaskManagement.Application/（アプリケーション層）

**役割**: ビジネスロジックの実装、トランザクション管理、ドメインルールの適用

```
TaskManagement.Application/
├── Services/
│   ├── TaskService.cs              # タスク CRUD・共有範囲チェック・サブタスク継承
│   ├── StatusService.cs            # ステータス管理・論理削除チェック
│   ├── LabelService.cs             # ラベル管理・リクエスト承認フロー
│   ├── RecurringTaskService.cs     # テンプレートからのタスク自動生成ロジック
│   ├── NotificationService.cs      # 通知生成（担当割当・期限超過）
│   └── UserService.cs              # アカウント管理・パスワードハッシュ
├── Interfaces/
│   ├── ITaskRepository.cs          # リポジトリ抽象化（テストのモック用）
│   ├── IUserRepository.cs
│   ├── INotificationRepository.cs
│   └── ...（各リポジトリのインターフェース）
└── DTOs/
    ├── Tasks/
    │   ├── CreateTaskRequest.cs
    │   ├── UpdateTaskRequest.cs
    │   └── TaskResponse.cs
    ├── Auth/
    │   ├── LoginRequest.cs
    │   └── LoginResponse.cs
    └── ...（各機能のリクエスト・レスポンス DTO）
```

**命名規則**:
- サービス: `{機能名}Service.cs`
- インターフェース: `I{クラス名}.cs`
- リクエスト DTO: `{動詞}{リソース名}Request.cs`（例: `CreateTaskRequest`）
- レスポンス DTO: `{リソース名}Response.cs`（例: `TaskResponse`）

**依存関係**:
- 依存可能: `TaskManagement.Domain`、`TaskManagement.Application/Interfaces`
- 依存禁止: `TaskManagement.Api`、`TaskManagement.Infrastructure`（インターフェース経由のみ）

---

### TaskManagement.Domain/（ドメイン層）

**役割**: エンティティ定義、列挙型、ドメイン固有の値オブジェクト

```
TaskManagement.Domain/
├── Entities/
│   ├── TaskItem.cs
│   ├── TaskStatus.cs
│   ├── TaskAssignee.cs
│   ├── TaskLabel.cs
│   ├── TaskShare.cs
│   ├── Label.cs
│   ├── LabelRequest.cs
│   ├── RecurringTemplate.cs
│   ├── Notification.cs
│   └── User.cs
└── Enums/
    ├── TaskPriority.cs             # High / Medium / Low
    ├── UserRole.cs                 # Admin / Member
    ├── RecurringFrequency.cs       # Daily / Weekly / Monthly
    ├── LabelRequestStatus.cs       # Pending / Approved / Rejected
    └── NotificationType.cs         # Assigned / Overdue
```

**命名規則**:
- エンティティ: `{エンティティ名}.cs`（PascalCase）
- 列挙型: `{列挙名}.cs`（PascalCase）

**依存関係**:
- 依存可能: なし（他プロジェクトへの依存禁止）
- 依存禁止: 全プロジェクト（最下層のため）

---

### TaskManagement.Infrastructure/（インフラ層）

**役割**: EF Core によるデータアクセス、Hangfire バックグラウンドジョブ、外部依存の実装

```
TaskManagement.Infrastructure/
├── Data/
│   ├── AppDbContext.cs             # EF Core DbContext
│   ├── Configurations/             # Fluent API によるテーブル設定
│   │   ├── TaskItemConfiguration.cs
│   │   ├── TaskStatusConfiguration.cs
│   │   ├── UserConfiguration.cs
│   │   └── ...
│   └── Migrations/                 # EF Core マイグレーションファイル
│       ├── 20260101_InitialCreate.cs
│       └── ...
├── Repositories/
│   ├── TaskRepository.cs           # ITaskRepository の実装
│   ├── UserRepository.cs
│   ├── NotificationRepository.cs
│   └── ...（各リポジトリの実装）
└── Jobs/
    ├── RecurringTaskGenerationJob.cs  # 定期タスク自動生成ジョブ
    └── OverdueNotificationJob.cs      # 期限超過通知ジョブ
```

**命名規則**:
- リポジトリ実装: `{エンティティ名}Repository.cs`
- EF Core 設定: `{エンティティ名}Configuration.cs`
- ジョブ: `{機能名}Job.cs`
- マイグレーション: `{YYYYMMDD}_{説明}.cs`

**依存関係**:
- 依存可能: `TaskManagement.Domain`、`TaskManagement.Application/Interfaces`
- 依存禁止: `TaskManagement.Api`

---

### TaskManagement.Tests/（テストプロジェクト）

**役割**: ユニットテスト・統合テストの配置

```
TaskManagement.Tests/
├── Unit/
│   ├── Services/
│   │   ├── TaskServiceTests.cs         # TaskService のユニットテスト
│   │   ├── RecurringTaskServiceTests.cs
│   │   └── NotificationServiceTests.cs
│   └── Jobs/
│       └── RecurringTaskGenerationJobTests.cs
└── Integration/
    ├── Controllers/
    │   ├── TasksControllerTests.cs     # API エンドポイントの統合テスト
    │   └── AuthControllerTests.cs
    └── TestHelpers/
        ├── TestDbContextFactory.cs     # テスト用 DB 設定
        └── AuthTestHelper.cs           # 認証ヘッダー生成ヘルパー
```

**命名規則**:
- テストクラス: `{テスト対象クラス名}Tests.cs`
- テストメソッド: `{メソッド名}_When{条件}_Should{期待結果}()` 形式
  - 例: `GetTask_WhenUserNotInShareList_ShouldThrowForbiddenException`

---

## フロントエンド構造

### frontend/src/app/（Angular アプリケーション）

```
frontend/src/
├── app/
│   ├── pages/                      # ルーティング単位のページコンポーネント
│   │   ├── dashboard/
│   │   │   ├── dashboard.component.ts
│   │   │   ├── dashboard.component.html
│   │   │   └── dashboard.component.scss
│   │   ├── task-list/
│   │   │   └── ...
│   │   ├── task-detail/
│   │   │   └── ...
│   │   ├── task-form/
│   │   │   └── ...
│   │   ├── login/
│   │   │   └── ...
│   │   └── admin/
│   │       ├── status-management/
│   │       ├── label-management/
│   │       ├── user-management/
│   │       └── recurring-template-management/
│   ├── components/                 # 再利用可能な UI コンポーネント
│   │   ├── task-card/
│   │   │   └── ...
│   │   ├── filter-panel/
│   │   │   └── ...
│   │   ├── notification-bell/
│   │   │   └── ...
│   │   └── status-badge/
│   │       └── ...
│   ├── services/                   # API 通信・状態管理
│   │   ├── task.service.ts
│   │   ├── auth.service.ts
│   │   ├── status.service.ts
│   │   ├── label.service.ts
│   │   ├── notification.service.ts
│   │   ├── user.service.ts
│   │   └── recurring-template.service.ts
│   ├── models/                     # TypeScript 型定義（API レスポンスの型）
│   │   ├── task.model.ts
│   │   ├── user.model.ts
│   │   ├── status.model.ts
│   │   ├── label.model.ts
│   │   └── notification.model.ts
│   ├── guards/                     # ルートガード
│   │   ├── auth.guard.ts           # 未認証ユーザーをログイン画面へ
│   │   └── admin.guard.ts          # 一般ユーザーの管理画面アクセスを防止
│   ├── interceptors/
│   │   └── auth.interceptor.ts     # 401 検知・自動リダイレクト
│   └── app.routes.ts               # ルーティング定義
├── assets/                         # 静的ファイル（画像・フォント等）
└── environments/
    ├── environment.ts              # 開発環境設定
    └── environment.prod.ts         # 本番環境設定
```

**命名規則**:
- ページコンポーネント: `{機能名}.component.ts`（kebab-case ディレクトリ）
- 再利用コンポーネント: `{コンポーネント名}.component.ts`
- サービス: `{リソース名}.service.ts`
- モデル: `{リソース名}.model.ts`
- ガード: `{ガード名}.guard.ts`
- インターセプター: `{機能名}.interceptor.ts`

**依存関係**:
- `pages/` → `services/`（OK）
- `pages/` → `components/`（OK）
- `components/` → `services/`（OK）
- `services/` → `models/`（OK）
- `services/` → `pages/`（NG・禁止）

---

### E2Eテスト（Playwright）

```
frontend/
└── e2e/
    ├── task-workflow.spec.ts       # タスク作成〜完了の基本フロー
    ├── filter.spec.ts              # フィルター・検索機能
    ├── recurring-task.spec.ts      # 定期タスクフィルター切り替え
    ├── admin.spec.ts               # 管理画面の操作
    └── helpers/
        ├── login.helper.ts         # ログイン操作ヘルパー
        └── task.helper.ts          # タスク操作ヘルパー
```

**命名規則**:
- テストファイル: `{機能名}.spec.ts`（kebab-case）

---

## ドキュメント構造

```
docs/
├── ideas/
│   └── initial-requirements.md    # 壁打ち・ブレスト用の初期要件メモ
├── product-requirements.md        # プロダクト要求定義書（PRD）
├── functional-design.md           # 機能設計書
├── architecture.md                # アーキテクチャ設計書
├── repository-structure.md        # リポジトリ構造定義書（本ドキュメント）
├── development-guidelines.md      # 開発ガイドライン
└── glossary.md                    # 用語集
```

---

## ファイル配置規則

### バックエンド

| ファイル種別 | 配置先 | 命名規則 | 例 |
|------------|--------|---------|-----|
| コントローラー | `TaskManagement.Api/Controllers/` | `{リソース}Controller.cs` | `TasksController.cs` |
| サービス | `TaskManagement.Application/Services/` | `{機能}Service.cs` | `TaskService.cs` |
| インターフェース | `TaskManagement.Application/Interfaces/` | `I{クラス名}.cs` | `ITaskRepository.cs` |
| DTO | `TaskManagement.Application/DTOs/{機能}/` | `{動詞}{リソース}Request/Response.cs` | `CreateTaskRequest.cs` |
| エンティティ | `TaskManagement.Domain/Entities/` | `{エンティティ名}.cs` | `TaskItem.cs` |
| 列挙型 | `TaskManagement.Domain/Enums/` | `{列挙名}.cs` | `TaskPriority.cs` |
| リポジトリ実装 | `TaskManagement.Infrastructure/Repositories/` | `{エンティティ}Repository.cs` | `TaskRepository.cs` |
| EF 設定 | `TaskManagement.Infrastructure/Data/Configurations/` | `{エンティティ}Configuration.cs` | `TaskItemConfiguration.cs` |
| ジョブ | `TaskManagement.Infrastructure/Jobs/` | `{機能}Job.cs` | `RecurringTaskGenerationJob.cs` |
| ユニットテスト | `TaskManagement.Tests/Unit/` | `{対象クラス}Tests.cs` | `TaskServiceTests.cs` |
| 統合テスト | `TaskManagement.Tests/Integration/` | `{対象コントローラー}Tests.cs` | `TasksControllerTests.cs` |

### フロントエンド

| ファイル種別 | 配置先 | 命名規則 | 例 |
|------------|--------|---------|-----|
| ページコンポーネント | `pages/{機能名}/` | `{機能名}.component.ts` | `dashboard.component.ts` |
| 再利用コンポーネント | `components/{コンポーネント名}/` | `{コンポーネント名}.component.ts` | `task-card.component.ts` |
| サービス | `services/` | `{リソース名}.service.ts` | `task.service.ts` |
| モデル | `models/` | `{リソース名}.model.ts` | `task.model.ts` |
| ガード | `guards/` | `{機能名}.guard.ts` | `auth.guard.ts` |
| E2Eテスト | `e2e/` | `{機能名}.spec.ts` | `task-workflow.spec.ts` |

---

## 命名規則まとめ

### バックエンド（C#）

| 種別 | 規則 | 例 |
|------|------|-----|
| クラス | PascalCase | `TaskService` |
| インターフェース | `I` プレフィックス + PascalCase | `ITaskRepository` |
| メソッド | PascalCase | `GetTaskById` |
| プライベートフィールド | `_` プレフィックス + camelCase | `_taskRepository` |
| 定数 | PascalCase（C# 標準） | `DefaultPageSize` |
| 名前空間 | `TaskManagement.{レイヤー名}` | `TaskManagement.Application.Services` |

### フロントエンド（TypeScript / Angular）

| 種別 | 規則 | 例 |
|------|------|-----|
| クラス | PascalCase | `TaskService` |
| インターフェース（モデル） | PascalCase | `Task`, `TaskResponse` |
| ファイル名 | kebab-case | `task.service.ts` |
| ディレクトリ名 | kebab-case | `task-list/` |
| 変数・メソッド | camelCase | `createTask()` |
| 定数 | `UPPER_SNAKE_CASE` | `API_BASE_URL` |

---

## 依存関係のルール

### バックエンドレイヤー間

```
TaskManagement.Api
    ↓ 参照OK
TaskManagement.Application
    ↓ 参照OK
TaskManagement.Domain
    ↑ 参照禁止（Domainは最下層）

TaskManagement.Infrastructure
    ↓ 参照OK（Interfaces 経由）
TaskManagement.Application/Interfaces
```

- `Api` → `Infrastructure` への直接参照禁止（DI 経由のみ）
- `Infrastructure` → `Api` への参照禁止
- `Domain` → 他プロジェクトへの参照禁止

---

## 除外設定（.gitignore）

```gitignore
# ビルド成果物
bin/
obj/
dist/
.angular/

# 依存関係
node_modules/

# IDE
.vs/
*.user
.idea/

# 環境設定（機密情報）
appsettings.Production.json

# ログ
*.log
logs/

# テンポラリ
.steering/
```

---

## スケーリング戦略

### 機能の追加

| 規模 | 方針 |
|------|------|
| 小規模（単一エンドポイント追加） | 既存コントローラー・サービスに追加 |
| 中規模（新機能モジュール） | 既存レイヤーに新ファイルを追加（例: `WikiService.cs`） |
| 大規模（全社展開・マイクロサービス化） | プロジェクトを分割してソリューションに追加 |

### ファイルサイズの管理

- 1ファイル 300行以下を推奨
- 300〜500行: リファクタリング検討
- 500行以上: 責務単位で分割（例: `TaskService` → `TaskService` + `TaskShareService`）
