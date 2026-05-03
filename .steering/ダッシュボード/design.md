# 設計書 - ダッシュボード

## アーキテクチャ概要

既存のレイヤードアーキテクチャを踏襲し、専用の `/api/dashboard` エンドポイントを追加する。
フロントエンドは Angular Material を使用して 2 セクション構成のダッシュボードを実装する。

```
Angular SPA (dashboard)
    │ HTTPS / withCredentials
    ↓
GET /api/dashboard
    └── DashboardController   [Authorize]
         ↓
    DashboardService
         ↓
    ITaskRepository.GetListAsync() × 2
    (1) assigneeId = currentUserId, isRecurring = false  → マイタスク
    (2) isRecurring = false                               → チーム共有タスク
         ↓
    SQL Server (Tasks + TaskAssignees + TaskShares)
```

## コンポーネント設計

### 1. バックエンド

#### DTOs (`TaskManagement.Application/DTOs/Dashboard/`)

**`DashboardResponse.cs`**
```csharp
public class DashboardResponse
{
    public List<TaskListItem> MyTasks { get; set; } = [];
    public List<TaskListItem> TeamTasks { get; set; } = [];
}
```

#### Interfaces (`TaskManagement.Application/Interfaces/`)

**`IDashboardService.cs`**
```csharp
public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(int currentUserId, bool includeRecurring = false);
}
```

#### Services (`TaskManagement.Application/Services/`)

**`DashboardService.cs`**
- コンストラクタ: `ITaskRepository _taskRepository`
- `GetDashboardAsync(int currentUserId, bool includeRecurring)`:
  1. `_taskRepository.GetListAsync({ AssigneeId = currentUserId, IsRecurring = includeRecurring ? null : false, PageSize = 100 }, currentUserId)` → MyTasks
  2. `_taskRepository.GetListAsync({ IsRecurring = includeRecurring ? null : false, PageSize = 100 }, currentUserId)` → TeamTasks

#### Controllers (`TaskManagement.Api/Controllers/`)

**`DashboardController.cs`**
- `[ApiController]`, `[Route("api/dashboard")]`, `[Authorize]`
- `GET /api/dashboard?includeRecurring=false` → `DashboardResponse`

#### DI 登録 (`Program.cs`)
```csharp
builder.Services.AddScoped<IDashboardService, DashboardService>();
```

### 2. フロントエンド

#### Models (`frontend/src/app/models/`)

`task.model.ts` に `DashboardResponse` インターフェースを追加:
```typescript
export interface DashboardResponse {
  myTasks: TaskListItem[];
  teamTasks: TaskListItem[];
}
```

#### Services (`frontend/src/app/services/`)

**`dashboard.service.ts`**
```typescript
@Injectable({ providedIn: 'root' })
export class DashboardService {
  getDashboard(includeRecurring = false): Observable<DashboardResponse>
}
```
- `GET /api/dashboard?includeRecurring={includeRecurring}` を呼び出す

#### Pages (`frontend/src/app/pages/dashboard/`)

**`dashboard.component.ts`**
- `signal<TaskListItem[]>` で `myTasks`, `teamTasks` を保持
- `signal<boolean>` で `isLoading`, `showRecurring` を保持
- `ngOnInit()`: `loadDashboard()` を呼び出す
- `showRecurring` トグル変更時に再読み込み
- `isOverdue(dueDate: string | null): boolean` ヘルパー

**`dashboard.component.html`**
- 2 セクション構成
  - セクション1: マイタスク (mat-card + mat-table)
  - セクション2: チーム共有タスク (mat-card + mat-table)
- 各セクションに「定期タスクを表示」トグル
- 期限超過行に `overdue` CSS クラス付与

**`dashboard.component.scss`**
- `.overdue { color: #d32f2f; }` (Material warn color)
- `.overdue td { background-color: rgba(211, 47, 47, 0.05); }`

#### Routing (`frontend/src/app/app.routes.ts`)

変更点:
1. `dashboard` ルートを追加 (canActivate: [authGuard])
2. `''` のリダイレクト先を `'login'` から `'dashboard'` に変更

#### Login Component

ログイン成功後のリダイレクト先を `'/'` から `'/dashboard'` に変更
（`'/'` → `'dashboard'` のリダイレクトで対応するため変更不要とするアプローチも可）

## データフロー

### ダッシュボード表示フロー

```
1. ユーザーがログイン → LoginComponent が '/dashboard' にリダイレクト
2. authGuard: isLoggedIn() = true → 通過
3. DashboardComponent.ngOnInit() → DashboardService.getDashboard(false) 呼び出し
4. GET /api/dashboard?includeRecurring=false
5. DashboardController → DashboardService.GetDashboardAsync(currentUserId, false)
6. TaskRepository.GetListAsync × 2
7. DashboardResponse { myTasks, teamTasks } を返却
8. Component: myTasks.set(), teamTasks.set()
9. テンプレート: @for で行をレンダリング、isOverdue() で条件スタイル適用
```

## UI レイアウト

```
┌─────────────────────────────────────────────────────┐
│  ダッシュボード                                       │
├─────────────────────────────────────────────────────┤
│  [マイタスク]                        [定期タスク: □] │
│  ┌────────┬────────┬────────┬────────────────────┐  │
│  │タイトル │ステータス│優先度 │期限                │  │
│  ├────────┼────────┼────────┼────────────────────┤  │
│  │タスクA  │進行中  │高     │2026/05/03 ⚠赤色   │  │  (overdue)
│  │タスクB  │未着手  │中     │2026/05/10          │  │
│  └────────┴────────┴────────┴────────────────────┘  │
│  [チーム共有タスク]                  [定期タスク: □] │
│  ┌────────┬────────┬────────┬──────┬────────────┐   │
│  │タイトル │ステータス│優先度 │担当者│期限        │   │
│  ├────────┼────────┼────────┼──────┼────────────┤   │
│  │タスクC  │レビュー│低     │田中  │2026/05/05  │   │
│  └────────┴────────┴────────┴──────┴────────────┘   │
└─────────────────────────────────────────────────────┘
```

## エラーハンドリング戦略

- API エラー時は MatSnackBar で「ダッシュボードの取得に失敗しました」を表示
- 既存の `ExceptionHandlingMiddleware` が 401/500 をハンドリング

## 変更履歴

### 2026-05-04
- **変更内容**: ShellComponent（サイドバーレイアウト）の設計追加、AppComponent のルート判定変更、DashboardComponent へのタスク作成ボタン追加
- **変更理由**: サイドバーナビゲーションとダッシュボードからのタスク作成対応

## テスト戦略

### バックエンド ユニットテスト
- `DashboardService.GetDashboardAsync_ShouldReturnMyTasksFilteredByAssignee`
- `DashboardService.GetDashboardAsync_WhenIncludeRecurringFalse_ShouldExcludeRecurring`

### バックエンド 統合テスト
- `GET /api/dashboard` 認証済みユーザーが 200 を受け取る
- `GET /api/dashboard` 未認証ユーザーが 401 を受け取る

## 仕様変更: サイドバーナビゲーション + タスク作成 (2026-05-04)

### シェルレイアウト設計

認証済みページ全体を包む `ShellComponent` を新設する。`AppComponent` はルート判定を行い、ログイン画面にはシェルを適用しない。

#### アーキテクチャ変更

**変更前:**
```
AppComponent → <router-outlet>
```

**変更後:**
```
AppComponent
  ├─ LoginComponent (サイドバーなし)
  └─ ShellComponent (認証済みページ)
       ├─ mat-sidenav (サイドバー)
       │   ├─ ナビゲーションリンク
       │   └─ ログアウトボタン
       └─ mat-sidenav-content
            └─ <router-outlet>
```

#### AppComponent の変更

`app.component.ts` でルートを監視し、ログインページかどうかを判定:
```typescript
readonly isLoginPage = signal(false);
// Router.events を購読して NavigationEnd 時に更新
```

`app.component.html`:
```html
@if (isLoginPage()) {
  <router-outlet />
} @else {
  <app-shell>
    <router-outlet />
  </app-shell>
}
```

#### ShellComponent (`frontend/src/app/layout/shell/`)

**`shell.component.ts`**
- `AuthService` を inject してユーザー情報・管理者フラグを取得
- `Router` を inject してログアウト後に `/login` へ遷移
- `isAdmin` signal: `AuthService.isAdmin()` の値

**`shell.component.html`**
```html
<mat-sidenav-container>
  <mat-sidenav mode="side" opened>
    <!-- ナビゲーションリンク -->
    <mat-nav-list>
      <a mat-list-item routerLink="/dashboard" routerLinkActive="active-link">
        <mat-icon>dashboard</mat-icon> ダッシュボード
      </a>
      <a mat-list-item routerLink="/tasks" routerLinkActive="active-link">
        <mat-icon>task</mat-icon> タスク一覧
      </a>
      @if (isAdmin()) {
        <!-- 管理者メニュー -->
      }
    </mat-nav-list>
    <!-- ログアウトボタン -->
  </mat-sidenav>
  <mat-sidenav-content>
    <ng-content />
  </mat-sidenav-content>
</mat-sidenav-container>
```

### ダッシュボード タスク作成ボタン

`dashboard.component.ts` に以下を追加:
- `MatDialog` を inject
- `openCreateDialog()` メソッド: `TaskFormComponent` を開き、成功時に `loadDashboard()` を呼び出す

`dashboard.component.html` のヘッダー部分に `mat-raised-button` を追加:
```html
<button mat-raised-button color="primary" (click)="openCreateDialog()">
  <mat-icon>add</mat-icon> タスクを作成
</button>
```

### 追加ファイル

```
frontend/src/app/
├── app.component.ts     [UPDATE: isLoginPage signal 追加、shell 出し分け]
├── app.component.html   [UPDATE: shell 出し分けロジック]
└── layout/shell/
    ├── shell.component.ts    [NEW]
    ├── shell.component.html  [NEW]
    └── shell.component.scss  [NEW]
```

## ディレクトリ構造（追加ファイル）

```
backend/
├── TaskManagement.Application/
│   ├── DTOs/Dashboard/DashboardResponse.cs     [NEW]
│   ├── Interfaces/IDashboardService.cs          [NEW]
│   └── Services/DashboardService.cs             [NEW]
├── TaskManagement.Api/
│   └── Controllers/DashboardController.cs       [NEW]
└── TaskManagement.Tests/
    └── Unit/Services/DashboardServiceTests.cs   [NEW]

frontend/
└── src/app/
    ├── models/task.model.ts                     [UPDATE: DashboardResponse 追加]
    ├── services/dashboard.service.ts            [NEW]
    ├── pages/dashboard/
    │   ├── dashboard.component.ts               [NEW]
    │   ├── dashboard.component.html             [NEW]
    │   └── dashboard.component.scss             [NEW]
    └── app.routes.ts                            [UPDATE: dashboard ルート追加]
```
