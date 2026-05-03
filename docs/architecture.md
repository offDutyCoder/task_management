# 技術仕様書 (Architecture Design Document)

## テクノロジースタック

### バックエンド

| 技術 | バージョン | 用途 | 選定理由 |
|------|-----------|------|----------|
| .NET (ASP.NET Core) | 8.x LTS | Web API・認証・ビジネスロジック | 社内主軸技術・長期サポート・IIS との親和性 |
| C# | 12 | 実装言語 | 静的型付けによる品質保証・チームスキル保有 |
| Entity Framework Core | 8.x | ORM・マイグレーション | Code First マイグレーション・LINQ クエリ・SQLインジェクション防止 |
| Hangfire | 1.8.x | バックグラウンドジョブ | 管理UI標準装備・再試行・スケジュール管理・SQL Server 永続化対応 |
| xUnit | 2.x | ユニット・統合テスト | .NET 標準的テストフレームワーク |
| Moq | 4.x | モック | xUnit との組み合わせで標準的なモック手法 |

### フロントエンド

| 技術 | バージョン | 用途 | 選定理由 |
|------|-----------|------|----------|
| Angular | 17.x | SPAフレームワーク | 型安全・コンポーネント設計・社内既存スキル |
| Angular Material | 17.x | UIコンポーネント | 統一されたデザインシステム・アクセシビリティ対応 |
| TypeScript | 5.x | 実装言語 | 型安全・IDE補完・バグ早期検出 |
| RxJS | 7.x | 非同期処理 | Angular 標準・Observableによる状態管理 |
| Playwright | 1.x | E2Eテスト | クロスブラウザ対応・CI連携 |

### インフラ・データベース

| 技術 | バージョン | 用途 | 選定理由 |
|------|-----------|------|----------|
| SQL Server | 2019以上 | メインデータベース | 社内標準DB・ACID準拠・全文検索 |
| Windows Server | 2019以上 | 実行環境 | 社内既存インフラを流用 |
| IIS | 10.x | Webサーバー・リバースプロキシ | 社内標準・Windows との統合・SSL終端 |

---

## アーキテクチャパターン

### 全体構成（SPA + REST API）

```
┌──────────────────────────────────────────────────────────────┐
│                    イントラネット                              │
│                                                              │
│  ┌───────────────┐    HTTPS     ┌───────────────────────┐   │
│  │  ブラウザ       │ ──────────▶  │  IIS                  │   │
│  │  (Chrome/Edge) │             │  ├─ Angular SPA (静的)  │   │
│  └───────────────┘             │  └─ /api/* → Kestrel    │   │
│                                └──────────┬────────────────┘   │
│                                           │                   │
│                              ┌────────────▼────────────────┐   │
│                              │  ASP.NET Core Web API        │   │
│                              │  ├─ Controllers              │   │
│                              │  ├─ Services                 │   │
│                              │  ├─ Repositories (EF Core)   │   │
│                              │  └─ Hangfire (バックグラウンド) │   │
│                              └────────────┬────────────────┘   │
│                                           │                   │
│                              ┌────────────▼────────────────┐   │
│                              │  SQL Server                  │   │
│                              └──────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

### バックエンド レイヤードアーキテクチャ

```
┌────────────────────────────────────────┐
│  Presentation Layer（Controllers）      │ ← HTTP受付・レスポンス・認可チェック
├────────────────────────────────────────┤
│  Application Layer（Services）          │ ← ビジネスロジック・トランザクション管理
├────────────────────────────────────────┤
│  Infrastructure Layer（Repositories）   │ ← EF Core・SQL Serverアクセス
└────────────────────────────────────────┘
```

#### Presentation Layer（Controllers）
- **責務**: HTTPリクエストの受付、入力バリデーション、レスポンスの返却、認可チェック
- **許可される操作**: Service の呼び出し、DTO への変換
- **禁止される操作**: Repository への直接アクセス、ビジネスロジックの実装

#### Application Layer（Services）
- **責務**: ビジネスロジックの実装、トランザクション管理、ドメインルールの適用
- **許可される操作**: Repository の呼び出し、他サービスの呼び出し
- **禁止される操作**: HTTP 依存コードの実装、Controller への依存

#### Infrastructure Layer（Repositories）
- **責務**: SQL Server へのデータアクセス、クエリの実装
- **許可される操作**: EF Core の DbContext 操作
- **禁止される操作**: ビジネスロジックの実装

### フロントエンド コンポーネント設計

```
┌──────────────────────────────────────────────┐
│  Pages（ルーティング単位）                      │
│  ├─ DashboardPage                            │
│  ├─ TaskListPage                             │
│  ├─ TaskDetailPage                           │
│  └─ AdminPage                               │
├──────────────────────────────────────────────┤
│  Components（再利用可能なUI部品）               │
│  ├─ TaskCardComponent                        │
│  ├─ TaskFormComponent                        │
│  ├─ FilterPanelComponent                    │
│  └─ NotificationBellComponent               │
├──────────────────────────────────────────────┤
│  Services（API通信・状態管理）                  │
│  ├─ TaskService（API呼び出し）                │
│  ├─ AuthService（ログイン状態管理）             │
│  └─ NotificationService（通知ポーリング）      │
└──────────────────────────────────────────────┘
```

---

## プロジェクト構造

```
task_management/
├── backend/
│   ├── TaskManagement.Api/          # ASP.NET Core Web API プロジェクト
│   │   ├── Controllers/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── TaskManagement.Application/  # ビジネスロジック層
│   │   ├── Services/
│   │   └── DTOs/
│   ├── TaskManagement.Domain/       # エンティティ・ドメインモデル
│   │   ├── Entities/
│   │   └── Enums/
│   ├── TaskManagement.Infrastructure/ # EF Core・Hangfire
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   └── Jobs/
│   └── TaskManagement.Tests/        # xUnit テストプロジェクト
│       ├── Unit/
│       └── Integration/
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── pages/
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   ├── models/
│   │   │   └── guards/
│   │   ├── assets/
│   │   └── environments/
│   ├── angular.json
│   └── package.json
└── docs/
```

---

## データ永続化戦略

### ストレージ方式

| データ種別 | ストレージ | 方式 | 理由 |
|-----------|----------|------|------|
| タスク・ユーザー等すべての業務データ | SQL Server | リレーショナルDB | ACID・結合クエリ・トランザクション |
| Hangfireジョブ情報 | SQL Server（専用スキーマ） | Hangfire 標準 | 設定の簡略化・再起動後も永続化 |
| セッション | HTTP-only Cookie | ASP.NET Core Cookie認証 | XSS対策・ステートレスAPI |

### バックアップ戦略

- **頻度**: SQL Server の定期バックアップ（日次フルバックアップ + 差分バックアップ）
- **保存先**: 社内ファイルサーバー（Windows Backup エージェント）
- **世代管理**: 日次バックアップを7世代保持
- **復元方法**: SQL Server Management Studio から BAK ファイルを使用してリストア

---

## 認証・認可アーキテクチャ

### Cookie 認証フロー

```
1. POST /api/auth/login
   → パスワードを BCrypt で検証
   → 成功時: HTTP-only Cookie（認証チケット）を発行
   → Angular は Cookie を自動送信（withCredentials: true）

1-b. GET /api/auth/me（セッション復元）
   → ページリロード時に APP_INITIALIZER から呼び出し
   → Cookie が有効であれば LoginResponse を返す
   → AuthService._currentUser Signal に保存してルートガードが機能する

2. 認証済みリクエスト
   → ASP.NET Core ミドルウェアが Cookie を検証
   → [Authorize] 属性のエンドポイントのみ処理続行

3. 認可チェック（2段階）
   → [Authorize(Roles = "Admin")] でロールチェック（コントローラー）
   → サービス層で公開範囲チェック（タスクごとのアクセス権）
```

### Angular ルートガード

```typescript
// AuthGuard: 未認証ユーザーをログイン画面へ
// AdminGuard: 一般ユーザーが管理画面へアクセスすることを防止
```

---

## パフォーマンス要件

### レスポンスタイム目標

| 操作 | 目標時間 | 測定環境 |
|------|---------|---------|
| ダッシュボード初期表示 | 2秒以内 | 社内LAN・SQL Server同一セグメント |
| タスク登録・更新 API | 500ms以内 | 同上 |
| タスク一覧（1,000件） | 3秒以内 | 同上 |
| ログイン | 1秒以内 | 同上 |

### リソース使用量目標

| リソース | 目標 | 理由 |
|---------|------|------|
| APIサーバー メモリ | 512MB以内（通常時） | IIS アプリケーションプール設定 |
| SQL Server CPU | ピーク50%以下 | 他システムとサーバー共有の可能性 |
| ディスク（ログ） | 1GB/月以内 | ログローテーションで管理 |

### インデックス設計

| テーブル | インデックスカラム | 用途 |
|---------|----------------|------|
| TaskItems | StatusId | ステータスフィルター |
| TaskItems | DueDate | 期限フィルター・超過通知 |
| TaskItems | ParentTaskId | サブタスク取得 |
| TaskItems | CreatedByUserId | 作成者フィルター |
| TaskAssignees | UserId | マイタスク取得 |
| TaskShares | UserId | 閲覧権限チェック |
| Notifications | UserId, IsRead | 通知一覧取得 |

---

## セキュリティアーキテクチャ

### データ保護

- **パスワード**: BCrypt（コスト係数12）でハッシュ化・平文保存禁止
- **通信**: IIS で TLS 1.2 以上を強制、HTTP → HTTPS リダイレクト
- **セッション**: HTTP-only + SameSite=Strict Cookie
- **機密情報**: 接続文字列は `appsettings.json` の暗号化または環境変数で管理（コードへのハードコード禁止）

### 入力検証・出力エスケープ

- **バリデーション**: ASP.NET Core の DataAnnotations + FluentValidation でAPIレベル検証
- **XSS対策**: Angular テンプレートバインディングによる自動エスケープ
- **SQLインジェクション**: EF Core のパラメータバインディングで防止
- **CSRF対策**: AntiForgery トークン（Cookie + ヘッダーの二重送信方式）

### アクセス制御

```
公開API（認証不要）:
  POST /api/auth/login

認証済みユーザーのみ（セッション確認）:
  GET /api/auth/me

認証済みユーザーのみ:
  GET/POST/PUT/DELETE /api/tasks
  GET /api/statuses
  GET /api/labels
  GET/POST /api/labels/requests
  GET/PUT /api/notifications
  PUT /api/users/me/*

管理者のみ:
  POST/PUT/DELETE /api/statuses
  POST/PUT/DELETE /api/labels
  PUT /api/labels/requests/{id}
  GET/POST/PUT /api/users（自分以外）
  GET/POST/PUT/DELETE /api/recurring-templates
```

---

## スケーラビリティ設計

### 初期〜将来のスケールパス

| フェーズ | ユーザー数 | 構成 |
|---------|----------|------|
| 初期 | 〜5人 | APIサーバー・DBサーバー各1台（または同一サーバー） |
| 中期 | 〜50人 | APIサーバーとDBサーバーを分離 |
| 将来 | 〜100人以上 | APIサーバーを複数台にスケールアウト（セッションは Cookie 認証のためステートレス） |

### データ増加への対応

- **タスクアーカイブ**: 完了日から1年以上経過したタスクを別テーブルへ移動（将来対応）
- **ページネーション**: タスク一覧は常にページネーション（最大100件/ページ）
- **インデックス最適化**: データ量増加に応じてクエリ実行計画を見直す

### 機能拡張性

- **ステータス・ラベルの柔軟性**: マスタデータとして管理者が設定可能（コード変更不要）
- **通知の拡張**: `NotificationService` を抽象化しておき、将来のメール・Slack連携をプラグイン的に追加可能
- **バックグラウンドジョブの追加**: Hangfire ダッシュボードからジョブの監視・再実行が可能

---

## テスト戦略

### ユニットテスト（xUnit + Moq）

- **対象**: Service クラスのビジネスロジック
- **カバレッジ目標**: 主要サービス（TaskService・RecurringTaskService・NotificationService）80%以上
- **方針**: Repository をモック化し、ビジネスロジックのみを検証

```csharp
// 例: 共有範囲チェックのテスト
[Fact]
public async Task GetTask_WhenUserNotInShareList_ShouldThrowForbiddenException()
{
    // Arrange
    var mockRepo = new Mock<ITaskRepository>();
    // ...
}
```

### 統合テスト（ASP.NET Core TestServer）

- **対象**: Controller → Service → Repository の結合
- **DB**: SQL Server LocalDB または SQLite InMemory（EF Core）
- **検証内容**: 認証・認可チェック、API レスポンスの形式、トランザクション動作

### E2Eテスト（Playwright）

- **対象ブラウザ**: Chrome・Edge
- **主要シナリオ**:
  1. ログイン → タスク作成 → 担当者割り当て → ステータス更新 → 完了
  2. 定期タスクフィルターのON/OFF切り替え
  3. 管理者によるステータス追加→タスクへの適用

---

## 技術的制約

### 環境要件

- **OS**: Windows Server 2019 以上（IIS ホスティング）
- **ランタイム**: .NET 8 Runtime（サーバーにインストール必要）
- **ブラウザ**: Chrome 最新版・Edge 最新版（IE・Safari は非サポート）
- **DB**: SQL Server 2019 以上（LocalDB は開発環境のみ）

### パフォーマンス制約

- タスク一覧は最大100件/ページ（ページネーション必須）
- 定期タスク生成ジョブは1分ごとの実行（生成件数が多い場合はバッチ化を検討）

### セキュリティ制約

- 外部ネットワークへの通信禁止（イントラネット完結）
- パスワードのメール送信禁止（管理者が直接伝達）
- ユーザーの自己登録禁止（管理者による発行のみ）

---

## 依存関係管理

### バックエンド（NuGet）

| ライブラリ | 用途 | バージョン管理方針 |
|-----------|------|-------------------|
| Microsoft.AspNetCore | Web API フレームワーク | .NET 8 LTS に追従（固定） |
| Microsoft.EntityFrameworkCore.SqlServer | ORM | .NET 8 LTS に追従（固定） |
| Hangfire.SqlServer | バックグラウンドジョブ | マイナーバージョンアップ可（^ 相当） |
| BCrypt.Net-Next | パスワードハッシュ | パッチバージョンのみ自動 |
| xUnit | テストフレームワーク | メジャー固定 |
| Moq | モックライブラリ | メジャー固定 |

### フロントエンド（npm）

| ライブラリ | 用途 | バージョン管理方針 |
|-----------|------|-------------------|
| @angular/core | SPAフレームワーク | メジャーバージョン固定（年次アップデート） |
| @angular/material | UIコンポーネント | Angular と同期 |
| rxjs | 非同期処理 | Angular に追従 |
| @playwright/test | E2Eテスト | マイナーバージョンアップ可 |

**方針**:
- セキュリティパッチは即時適用
- メジャーバージョンアップは事前に互換性確認の上、スプリント計画に組み込む
- `package-lock.json` / `packages.lock.json` で依存関係を固定し、CI環境での再現性を確保
