# 開発ガイドライン (Development Guidelines)

## コーディング規約

### バックエンド（C#）

#### 命名規則

```csharp
// ✅ クラス・メソッド: PascalCase
public class TaskService { }
public class RecurringTaskGenerationJob { }
public async Task<TaskResponse> GetTaskByIdAsync(int id) { }

// ✅ プライベートフィールド: _ プレフィックス + camelCase
private readonly ITaskRepository _taskRepository;
private readonly INotificationService _notificationService;

// ✅ ローカル変数・パラメーター: camelCase
var assigneeIds = new List<int>();
async Task<TaskItem> createTask(CreateTaskRequest request) { }

// ✅ 定数: PascalCase（C# 標準）
public const int DefaultPageSize = 20;
public const int MaxPageSize = 100;

// ✅ Boolean: Is / Has / Can / Should で始める
public bool IsActive { get; set; }
public bool HasSubTasks => SubTasks.Any();

// ❌ 悪い例
private ITaskRepository repo;      // アンダースコアなし・略語
public async Task<object> Get()    // object 型・意味不明な名前
```

#### インターフェース

```csharp
// ✅ I プレフィックス
public interface ITaskRepository { }
public interface INotificationService { }

// ❌ 悪い例
public interface TaskRepository { }  // I なし
```

#### コードフォーマット

- **インデント**: 4スペース（Visual Studio デフォルト）
- **行の長さ**: 最大120文字
- **`using` ディレクティブ**: ファイル先頭にまとめる（`global using` の活用を検討）

```csharp
// ✅ 良い例: 明示的な型指定（複雑な型のみ var）
var task = await _taskRepository.GetByIdAsync(id);  // 型が明確な場合は var OK
TaskItem? existingTask = await _taskRepository.GetByIdAsync(id);  // null許容を明示

// ❌ 悪い例: 意味不明な var 乱用
var x = GetSomeData();
```

#### 非同期処理

```csharp
// ✅ 非同期メソッドには Async サフィックス
public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request)
{
    var task = new TaskItem { Title = request.Title };
    await _taskRepository.SaveAsync(task);
    return MapToResponse(task);
}

// ✅ ConfigureAwait(false) はライブラリプロジェクトで使用
// ✅ Task.WhenAll で並列処理
var (assignees, labels) = await (
    _userRepository.GetByIdsAsync(request.AssigneeIds),
    _labelRepository.GetByIdsAsync(request.LabelIds)
).WhenAll();

// ❌ 悪い例: async void（コントローラー以外）
public async void ProcessTask() { }  // 例外を補足できない
```

#### エラーハンドリング

```csharp
// ✅ ドメイン例外クラスを定義
public class NotFoundException : Exception
{
    public NotFoundException(string resource, int id)
        : base($"{resource} が見つかりません (ID: {id})") { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "このリソースへのアクセス権がありません")
        : base(message) { }
}

// ✅ サービス層では業務エラーをスロー、技術エラーはログ後に再スロー
public async Task<TaskItem> GetTaskAsync(int taskId, int currentUserId)
{
    var task = await _taskRepository.GetByIdAsync(taskId)
        ?? throw new NotFoundException("タスク", taskId);

    if (!await _taskService.CanAccessAsync(task, currentUserId))
        throw new ForbiddenException();

    return task;
}

// ✅ グローバル例外ハンドラー（ExceptionHandlingMiddleware）で HTTP ステータスコードに変換
// NotFoundException → 404、ForbiddenException → 403
```

---

### フロントエンド（TypeScript / Angular）

#### 命名規則

```typescript
// ✅ クラス: PascalCase
class TaskService { }
class TaskCardComponent { }

// ✅ インターフェース（モデル）: PascalCase（I プレフィックスなし）
interface Task { }
interface TaskResponse { }

// ✅ ファイル名: kebab-case
// task.service.ts / task-card.component.ts

// ✅ 変数・関数: camelCase
const currentUserId = 1;
function formatDueDate(date: string): string { }

// ✅ 定数: UPPER_SNAKE_CASE
const API_BASE_URL = '/api';
const DEFAULT_PAGE_SIZE = 20;

// ✅ Boolean: is / has / should で始める
const isLoading = signal(false);
const hasOverdueTasks = computed(() => tasks().some(t => t.isOverdue));
```

#### Angular コンポーネント

```typescript
// ✅ Signal-based state management（Angular 17+）
@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [MatTableModule, MatButtonModule, AsyncPipe],
  templateUrl: './task-list.component.html',
})
export class TaskListComponent {
  private taskService = inject(TaskService);

  tasks = signal<Task[]>([]);
  isLoading = signal(false);

  async loadTasks(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(this.taskService.getTasks());
      this.tasks.set(result.items);
    } finally {
      this.isLoading.set(false);
    }
  }
}

// ✅ HTTP サービス: Observable を返す
@Injectable({ providedIn: 'root' })
export class TaskService {
  private http = inject(HttpClient);

  getTasks(params?: TaskFilterParams): Observable<PagedResult<Task>> {
    return this.http.get<PagedResult<Task>>('/api/tasks', { params });
  }

  createTask(request: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>('/api/tasks', request);
  }
}
```

#### エラーハンドリング（フロントエンド）

```typescript
// ✅ HTTP インターセプターで 401 を一元処理
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private router = inject(Router);

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.router.navigate(['/login']);
        }
        return throwError(() => error);
      })
    );
  }
}

// ✅ コンポーネントでエラーメッセージを表示
async saveTask(): Promise<void> {
  try {
    await firstValueFrom(this.taskService.createTask(this.form.value));
    this.snackBar.open('タスクを作成しました', '閉じる', { duration: 3000 });
  } catch (error: unknown) {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      this.errorMessage.set('入力内容を確認してください');
    } else {
      this.errorMessage.set('エラーが発生しました。管理者にご連絡ください');
    }
  }
}
```

#### コードフォーマット

- **インデント**: 2スペース
- **行の長さ**: 最大120文字
- **セミコロン**: あり
- **クォート**: シングルクォート

---

## コメント規約

### バックエンド（C#）

```csharp
// ✅ 公開 API には XML ドキュメントコメント
/// <summary>
/// 指定されたユーザーが閲覧可能なタスクを取得する。
/// サブタスクの場合は親タスクの共有設定を参照する。
/// </summary>
/// <param name="taskId">タスク ID</param>
/// <param name="currentUserId">現在ログイン中のユーザー ID</param>
/// <returns>タスク情報</returns>
/// <exception cref="NotFoundException">タスクが存在しない場合</exception>
/// <exception cref="ForbiddenException">閲覧権限がない場合</exception>
public async Task<TaskItem> GetTaskAsync(int taskId, int currentUserId) { }

// ✅ インラインコメント: WHY（なぜ）を説明する
// サブタスクは親タスクの公開範囲を継承するため、自身の TaskShares ではなく親のものを参照する
var shareList = task.ParentTaskId.HasValue
    ? await _taskShareRepository.GetByTaskIdAsync(task.ParentTaskId.Value)
    : await _taskShareRepository.GetByTaskIdAsync(task.Id);

// ❌ 悪い例: コードを繰り返すだけ
// task が null の場合に例外をスロー
var task = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Task", id);
```

### フロントエンド（TypeScript）

```typescript
// ✅ 複雑なロジックに WHY コメント
// 定期タスクはデフォルトでフィルターから除外するため、URLパラメーターに明示的に false をセットする
params.set('isRecurring', 'false');

// ❌ 悪い例: WHAT を繰り返す
// isLoading を true にセット
this.isLoading.set(true);
```

---

## Git 運用ルール

### ブランチ戦略（Git Flow）

```
main（本番環境）
└── develop（開発・統合環境）
    ├── feature/{機能名}  // 新機能
    ├── fix/{修正内容}    // バグ修正
    └── refactor/{対象}   // リファクタリング
```

**ルール**:
- `main` / `develop` への直接コミット禁止（PR 必須）
- `feature` / `fix` ブランチは `develop` から切り、完了後に PR で `develop` へマージ
- `develop` → `main` は squash merge で履歴をクリーンに保つ
- マージ後はフィーチャーブランチを削除

---

### コミットメッセージ規約（Conventional Commits）

```
<type>(<scope>): <日本語の件名>

<本文（任意）>

<フッター（任意）>
```

**Type 一覧**:

| Type | 用途 |
|------|------|
| `feat` | 新機能 |
| `fix` | バグ修正 |
| `docs` | ドキュメントのみ変更 |
| `style` | フォーマット（動作に影響なし） |
| `refactor` | リファクタリング |
| `perf` | パフォーマンス改善 |
| `test` | テスト追加・修正 |
| `chore` | ビルド設定・依存関係更新 |

**Scope（主なもの）**:
`task` / `status` / `label` / `user` / `auth` / `notification` / `recurring` / `admin`

**例**:

```
feat(task): タスクのツリー構造（サブタスク）を追加

- TaskItem に ParentTaskId を追加
- サブタスク作成 API を実装
- サブタスクは親タスクの公開範囲を自動継承する仕様を実装

Closes #42
```

```
fix(notification): 期限超過通知ジョブが重複して実行される問題を修正

同日に複数回ジョブが起動した場合に通知が重複する不具合を修正。
送信前に当日分の通知レコードの存在確認を追加。

Fixes #87
```

---

### プルリクエストのプロセス

**PR 作成前チェックリスト**:
- [ ] ビルドが通る（バックエンド: `dotnet build`、フロントエンド: `ng build`）
- [ ] 全テストがパスする（`dotnet test` / `ng test`）
- [ ] EF Core マイグレーションが必要な場合は追加済み
- [ ] セルフレビューを実施済み

**PR テンプレート**:
```markdown
## 変更の種類
- [ ] 新機能 (feat)
- [ ] バグ修正 (fix)
- [ ] リファクタリング (refactor)
- [ ] ドキュメント (docs)

## 変更内容
### 何を変更したか
[簡潔な説明]

### なぜ変更したか
[背景・理由]

### どのように変更したか
- [変更点1]
- [変更点2]

## テスト
- [ ] ユニットテスト追加
- [ ] 統合テスト追加
- [ ] 手動テスト実施

## 関連 Issue
Closes #[番号]

## レビューポイント
[レビュアーに特に見てほしい点]
```

---

## テスト戦略

### テストピラミッド

```
       /\
      /E2E\       少（Playwright）
     /------\
    / 統合   \     中（ASP.NET Core TestServer）
   /----------\
  / ユニット   \   多（xUnit + Moq）
 /--------------\
```

**目標比率**: ユニットテスト 70% / 統合テスト 20% / E2Eテスト 10%

---

### ユニットテスト（xUnit + Moq）

**カバレッジ目標**: 主要サービスクラス 80% 以上

**テスト構造（Given-When-Then）**:

```csharp
public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepo;
    private readonly Mock<ITaskShareRepository> _mockShareRepo;
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _mockTaskRepo = new Mock<ITaskRepository>();
        _mockShareRepo = new Mock<ITaskShareRepository>();
        _sut = new TaskService(_mockTaskRepo.Object, _mockShareRepo.Object);
    }

    [Fact]
    public async Task GetTaskAsync_WhenUserNotInShareList_ShouldThrowForbiddenException()
    {
        // Given: 現在ユーザーが共有リストに含まれていないタスク
        var taskId = 1;
        var currentUserId = 99;
        _mockTaskRepo.Setup(r => r.GetByIdAsync(taskId))
            .ReturnsAsync(new TaskItem { Id = taskId, ParentTaskId = null });
        _mockShareRepo.Setup(r => r.GetByTaskIdAsync(taskId))
            .ReturnsAsync(new List<TaskShare> { new() { UserId = 1 } });

        // When / Then
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.GetTaskAsync(taskId, currentUserId));
    }

    [Fact]
    public async Task GetTaskAsync_WhenUserInShareList_ShouldReturnTask()
    {
        // Given: 現在ユーザーが共有リストに含まれているタスク
        var taskId = 1;
        var currentUserId = 1;
        var expected = new TaskItem { Id = taskId };
        _mockTaskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(expected);
        _mockShareRepo.Setup(r => r.GetByTaskIdAsync(taskId))
            .ReturnsAsync(new List<TaskShare> { new() { UserId = currentUserId } });

        // When
        var result = await _sut.GetTaskAsync(taskId, currentUserId);

        // Then
        Assert.Equal(expected, result);
    }
}
```

**テストメソッド命名**: `{メソッド名}_{条件}_{期待結果}` 形式
- 例: `CreateTaskAsync_WhenTitleIsEmpty_ShouldThrowValidationException`

---

### 統合テスト（ASP.NET Core TestServer）

```csharp
public class TasksControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TasksControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // テスト用 DB に差し替え（LocalDB または SQLite InMemory）
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(opt =>
                        opt.UseSqlite("DataSource=:memory:"));
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task GET_Tasks_WithoutAuth_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

---

### E2Eテスト（Playwright）

```typescript
// e2e/task-workflow.spec.ts
import { test, expect } from '@playwright/test';
import { loginAsUser } from './helpers/login.helper';

test('タスク作成〜ステータス更新〜完了の基本フロー', async ({ page }) => {
  // Given: ログイン済み
  await loginAsUser(page, 'tanaka', 'password123');

  // When: タスクを作成
  await page.click('[data-testid="new-task-button"]');
  await page.fill('[data-testid="task-title-input"]', 'E2Eテストタスク');
  await page.click('[data-testid="save-task-button"]');

  // Then: ダッシュボードにタスクが表示される
  await expect(page.locator('text=E2Eテストタスク')).toBeVisible();

  // When: ステータスを進行中に変更
  await page.click('text=E2Eテストタスク');
  await page.selectOption('[data-testid="status-select"]', { label: '進行中' });

  // Then: ステータスが更新される
  await expect(page.locator('[data-testid="status-badge"]')).toHaveText('進行中');
});
```

---

## コードレビュー基準

### レビューポイント

**機能性**:
- [ ] PRD・機能設計書の要件を満たしているか
- [ ] エッジケース（null・空配列・権限チェック）が考慮されているか
- [ ] 公開範囲チェック（`TaskShare`）が API レベルで確実に行われているか

**可読性**:
- [ ] 命名が明確か（略語・単文字変数を避けているか）
- [ ] WHY コメントが必要な箇所にあるか

**保守性**:
- [ ] 重複コードがないか（共通処理をサービスに切り出しているか）
- [ ] レイヤー間の依存ルールを守っているか（Controller → Service → Repository）
- [ ] EF Core の `Include` が必要なものだけに限定されているか（N+1 問題）

**セキュリティ**:
- [ ] 入力バリデーションが API レベルで実施されているか
- [ ] 管理者専用エンドポイントに `[Authorize(Roles = "Admin")]` があるか
- [ ] 機密情報（パスワード・接続文字列）がコードにハードコードされていないか

**パフォーマンス**:
- [ ] ページネーションが実装されているか（リスト取得）
- [ ] 不要な SQL クエリが発行されていないか（EF Core のクエリログ確認）

### レビューコメントの書き方

```markdown
// ✅ 良い例: 建設的・具体的
[推奨] この LINQ クエリは N+1 問題が発生します。
Include を使って関連データを一括取得してください:

```csharp
var tasks = await _context.TaskItems
    .Include(t => t.Assignees)
    .Include(t => t.Labels)
    .ToListAsync();
```

// ❌ 悪い例: 曖昧
このコードは良くないです。
```

**優先度の明示**:
- `[必須]`: マージ前に修正が必要
- `[推奨]`: 修正を推奨（軽微な場合はマージ可）
- `[提案]`: 検討してほしい代替案
- `[質問]`: 意図の確認

---

## 開発環境セットアップ

### 必要なツール

| ツール | バージョン | インストール方法 |
|--------|-----------|-----------------|
| .NET SDK | 8.x | [dotnet.microsoft.com](https://dotnet.microsoft.com) |
| Node.js | 20.x LTS | [nodejs.org](https://nodejs.org) |
| Angular CLI | 17.x | `npm install -g @angular/cli` |
| SQL Server | 2019+ | SQL Server Express でも可 |
| Visual Studio または VS Code | 最新 | 任意 |

### セットアップ手順

```powershell
# 1. リポジトリのクローン
git clone <URL>
cd task_management

# 2. バックエンドのセットアップ
cd backend
dotnet restore

# 3. DB マイグレーションの適用
cd TaskManagement.Api
dotnet ef database update

# 4. バックエンドの起動
dotnet run

# 5. フロントエンドのセットアップ（別ターミナル）
cd ../../frontend
npm install
ng serve
```

### 環境設定

```json
// backend/TaskManagement.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagement;Trusted_Connection=True;"
  },
  "Hangfire": {
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=TaskManagement_Hangfire;Trusted_Connection=True;"
  }
}
```

---

## 実装完了チェックリスト

### バックエンド

- [ ] 命名規則（PascalCase・`_` プレフィックス）を守っているか
- [ ] 非同期メソッドに `Async` サフィックスがあるか
- [ ] 適切な例外クラスを使用しているか（`NotFoundException`・`ForbiddenException`）
- [ ] DTO を使用しており、エンティティを直接返していないか
- [ ] 認可チェック（`[Authorize]`・サービス層の権限チェック）があるか
- [ ] EF Core の N+1 問題を回避しているか（必要な `Include` のみ）
- [ ] ユニットテストを追加したか

### フロントエンド

- [ ] 命名規則（camelCase / PascalCase / kebab-case）を守っているか
- [ ] `HttpErrorResponse` を適切にハンドリングしているか
- [ ] ローディング状態・エラー状態を UI に反映しているか
- [ ] Angular Material コンポーネントを使用し、独自スタイルを最小限にしているか
- [ ] `data-testid` 属性を E2E テスト対象の要素に付与しているか

### 共通

- [ ] コミットメッセージが Conventional Commits に従っているか
- [ ] セキュリティ上の問題（ハードコードされた機密情報など）がないか
- [ ] EF Core マイグレーションファイルが必要な場合に追加されているか
