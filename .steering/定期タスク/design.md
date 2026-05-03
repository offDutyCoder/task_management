# 設計書

## アーキテクチャ概要

既存のレイヤードアーキテクチャ（Controller → Service → Repository → EF Core → SQL Server）に従う。バックグラウンドジョブは Hangfire の RecurringJob として Infrastructure 層の Jobs/ に配置する。

```
[管理者ブラウザ] → HTTPS/REST
  └→ [RecurringTemplatesController] (Presentation)
       └→ [RecurringTaskService - CRUD部] (Application)
            └→ [RecurringTemplateRepository] (Infrastructure)
                 └→ [SQL Server: RecurringTemplates / RecurringTemplateAssignees / ...]

[Hangfire スケジューラー] → 毎分
  └→ [RecurringTaskJob] (Infrastructure/Jobs)
       └→ [RecurringTaskService - GenerateTasksAsync] (Application)
            └→ [RecurringTemplateRepository + AppDbContext]
                 └→ [TaskItems / TaskAssignees / TaskLabels / TaskShares を INSERT]
```

## コンポーネント設計

### 1. Domain: エンティティ・Enum

**RecurringTemplate**（新規）:
- `Id`, `Title`(nvarchar200), `Description`(nvarchar4000?)
- `Priority`(TaskPriority enum)
- `Frequency`(RecurringFrequency enum: Daily/Weekly/Monthly)
- `WeekDays`(nvarchar(50)?: "1,2,3,4,5" 形式のCSV文字列 - 週次用, 0=日〜6=土)
- `DayOfMonth`(int?: 1〜31 - 月次用)
- `ExcludeWeekends`(bool)
- `ExcludeHolidays`(bool, 祝日マスタ未実装のため今回は参照しない)
- `GenerationTime`(TimeSpan: 生成時刻)
- `DefaultStatusId`(int FK→TaskStatus: 生成するタスクの初期ステータス)
- `IsActive`(bool)
- `CreatedByUserId`(int FK→User)
- `CreatedAt`, `UpdatedAt`
- ナビゲーション: `Assignees`(RecurringTemplateAssignee[]), `Labels`(RecurringTemplateLabel[]), `Shares`(RecurringTemplateShare[])

**RecurringTemplateAssignee**（新規、中間テーブル）:
- `RecurringTemplateId`(int FK), `UserId`(int FK) - 複合PK

**RecurringTemplateLabel**（新規、中間テーブル）:
- `RecurringTemplateId`(int FK), `LabelId`(int FK) - 複合PK

**RecurringTemplateShare**（新規、中間テーブル）:
- `RecurringTemplateId`(int FK), `UserId`(int FK) - 複合PK

**RecurringFrequency**（新規 Enum）:
```csharp
public enum RecurringFrequency { Daily = 0, Weekly = 1, Monthly = 2 }
```

### 2. Application: DTOs

**CreateRecurringTemplateRequest**:
- `Title`(string, Required, MaxLength200)
- `Description`(string?)
- `Priority`(TaskPriority)
- `Frequency`(RecurringFrequency)
- `WeekDays`(int[]? - 0〜6)
- `DayOfMonth`(int? - 1〜31)
- `ExcludeWeekends`(bool)
- `GenerationTime`(string: "HH:mm" 形式)
- `DefaultStatusId`(int)
- `AssigneeIds`(int[])
- `LabelIds`(int[])
- `ShareUserIds`(int[])

**UpdateRecurringTemplateRequest**: CreateRecurringTemplateRequest と同じ + `IsActive`(bool)

**RecurringTemplateResponse**:
- `Id`, `Title`, `Description`, `Priority`(string), `Frequency`(string)
- `WeekDays`(int[]?), `DayOfMonth`(int?), `ExcludeWeekends`(bool), `GenerationTime`(string)
- `DefaultStatusId`(int), `IsActive`(bool)
- `Assignees`(AssigneeDto[]), `Labels`(LabelDto[]), `ShareUsers`(AssigneeDto[])
- `CreatedAt`(DateTime)

### 3. Application: IRecurringTaskService インターフェース

```csharp
Task<IEnumerable<RecurringTemplateResponse>> GetAllAsync();
Task<RecurringTemplateResponse> GetByIdAsync(int id);
Task<RecurringTemplateResponse> CreateAsync(CreateRecurringTemplateRequest request, int createdByUserId);
Task<RecurringTemplateResponse> UpdateAsync(int id, UpdateRecurringTemplateRequest request);
Task DeactivateAsync(int id);
Task GenerateTasksAsync(DateOnly today, TimeSpan currentTime);
```

### 4. Application: RecurringTaskService

**GenerateTasksAsync の処理フロー**:
1. `GetActiveForGenerationAsync(currentTime)` → `GenerationTime` が `currentTime ±1分` 以内のテンプレートを取得
2. 各テンプレートに対して:
   a. 除外日チェック: `ExcludeWeekends && (today.DayOfWeek == Saturday || Sunday)` → スキップ
   b. 重複チェック: `TaskItems.Any(t => t.RecurringTemplateId == id && t.CreatedAt.Date == today.ToDateTime())` → スキップ
   c. `TaskItem` を生成（`IsRecurring=true`, `RecurringTemplateId=template.Id`, `StatusId=template.DefaultStatusId`）
   d. `TaskAssignee` をテンプレートの担当者からコピー
   e. `TaskLabel` をテンプレートのラベルからコピー
   f. `TaskShare` をテンプレートの共有ユーザーからコピー
   g. `SaveChangesAsync()`

### 5. Infrastructure: RecurringTemplateRepository

**主要メソッド**:
- `GetAllAsync()`: Include(Assignees, Labels, Shares)
- `GetByIdAsync(int id)`: FindAsync
- `GetActiveForGenerationAsync(TimeSpan currentTime)`: `IsActive=true` かつ `|GenerationTime - currentTime| <= 1分`
- `CreateAsync()`, `UpdateAsync()`
- `ExistsTitleAsync(string title, int? excludeId)`

### 6. Infrastructure/Jobs: RecurringTaskJob

既存の `OverdueNotificationJob` パターンに倣う:
```csharp
[AutomaticRetry(Attempts = 3)]
public async Task ExecuteAsync()
{
    var now = DateTime.Now;
    await _recurringTaskService.GenerateTasksAsync(DateOnly.FromDateTime(now), now.TimeOfDay);
}
```

`HangfireJobRegistrationService` に毎分実行のジョブ登録を追加:
```csharp
RecurringJob.AddOrUpdate<RecurringTaskJob>("recurring-task-generation", job => job.ExecuteAsync(), Cron.Minutely);
```

### 7. Presentation: RecurringTemplatesController

```
[ApiController]
[Route("api/recurring-templates")]
[Authorize(Roles = "Admin")]  ← 全エンドポイントを管理者のみに制限
```

| メソッド | パス | 操作 |
|---|---|---|
| GET | /api/recurring-templates | 一覧取得 |
| GET | /api/recurring-templates/{id} | 単件取得 |
| POST | /api/recurring-templates | 作成 |
| PUT | /api/recurring-templates/{id} | 更新 |
| DELETE | /api/recurring-templates/{id} | 無効化 |

### 8. Frontend: RecurringTemplateManagement

パターンは既存の `status-management` に倣う（MatTable + MatDialog）。

**ファイル構成**:
- `models/recurring-template.model.ts` - モデル定義
- `services/recurring-template.service.ts` - API 通信
- `pages/admin/recurring-template-management/recurring-template-management.component.ts/.html/.scss`
- `pages/admin/recurring-template-management/recurring-template-dialog.component.ts`

**一覧テーブルの列**: タイトル / 頻度 / 生成時刻 / 土日除外 / 状態 / 操作

**ダイアログフォームのフィールド**:
- タイトル（必須）
- 説明（任意）
- 優先度（High/Medium/Low セレクト）
- 頻度（Daily/Weekly/Monthly セレクト）
- WeekDays（週次時のみ表示: 月〜日チェックボックス）
- DayOfMonth（月次時のみ表示: 1〜31 数値入力）
- 生成時刻（HH:mm 入力）
- 初期ステータス（ステータスセレクト）
- 土日除外（トグル）
- 担当者（ユーザー複数選択）
- ラベル（ラベル複数選択）
- 共有ユーザー（ユーザー複数選択）
- 有効/無効（編集時のみ）

## データフロー

### テンプレート作成
```
1. POST /api/recurring-templates
2. RecurringTemplatesController → RecurringTaskService.CreateAsync()
3. RecurringTemplate + RecurringTemplateAssignee[] + RecurringTemplateLabel[] + RecurringTemplateShare[] を INSERT
4. 201 Created
```

### タスク自動生成
```
1. Hangfire が RecurringTaskJob.ExecuteAsync() を毎分実行
2. GenerateTasksAsync(today, currentTime)
3. GenerationTime ±1分 のアクティブテンプレートを取得
4. 除外日チェック（土日フラグ）
5. 重複チェック（同日生成済みか）
6. TaskItem INSERT（IsRecurring=true）
7. TaskAssignee / TaskLabel / TaskShare INSERT
```

## エラーハンドリング戦略

既存の ExceptionHandlingMiddleware を利用:
- `NotFoundException`: テンプレートが見つからない場合 → 404
- `ValidationException`: タイトル重複など → 400

## テスト戦略

### ユニットテスト
- `RecurringTaskService.GenerateTasksAsync`: 除外日判定・重複防止・生成ロジック

### 統合テスト
- `RecurringTemplatesController` の CRUD
- 一般ユーザーによるアクセス拒否（403）

## 依存ライブラリ

追加なし（Hangfire・EF Core はすでに導入済み）

## ディレクトリ構造

```
backend/
  TaskManagement.Domain/
    Entities/
      RecurringTemplate.cs              (NEW)
      RecurringTemplateAssignee.cs      (NEW)
      RecurringTemplateLabel.cs         (NEW)
      RecurringTemplateShare.cs         (NEW)
    Enums/
      RecurringFrequency.cs             (NEW)
  TaskManagement.Application/
    DTOs/RecurringTemplates/
      CreateRecurringTemplateRequest.cs (NEW)
      UpdateRecurringTemplateRequest.cs (NEW)
      RecurringTemplateResponse.cs      (NEW)
    Interfaces/
      IRecurringTemplateRepository.cs   (NEW)
      IRecurringTaskService.cs          (NEW)
    Services/
      RecurringTaskService.cs           (NEW)
  TaskManagement.Infrastructure/
    Data/
      AppDbContext.cs                   (MODIFY: DbSet追加)
      Configurations/
        RecurringTemplateConfiguration.cs          (NEW)
        RecurringTemplateAssigneeConfiguration.cs  (NEW)
        RecurringTemplateLabelConfiguration.cs     (NEW)
        RecurringTemplateShareConfiguration.cs     (NEW)
      Migrations/
        {timestamp}_AddRecurringTemplates.cs       (NEW: 手動作成)
        {timestamp}_AddRecurringTemplates.Designer.cs (NEW)
        AppDbContextModelSnapshot.cs               (MODIFY)
    Repositories/
      RecurringTemplateRepository.cs    (NEW)
    Jobs/
      RecurringTaskJob.cs               (NEW)
      HangfireJobRegistrationService.cs (MODIFY: ジョブ登録追加)
  TaskManagement.Api/
    Controllers/
      RecurringTemplatesController.cs   (NEW)
    Program.cs                          (MODIFY: DI登録)
  TaskManagement.Tests/
    Unit/Services/
      RecurringTaskServiceTests.cs      (NEW)
    Integration/Controllers/
      RecurringTemplatesControllerTests.cs (NEW)

frontend/src/app/
  models/
    recurring-template.model.ts         (NEW)
  services/
    recurring-template.service.ts       (NEW)
  pages/admin/
    recurring-template-management/
      recurring-template-management.component.ts   (NEW)
      recurring-template-management.component.html (NEW)
      recurring-template-management.component.scss (NEW)
      recurring-template-dialog.component.ts       (NEW)
  app.routes.ts                         (MODIFY)
  layout/shell/shell.component.html     (MODIFY)
```

## 実装の順序

1. Domain: RecurringFrequency enum・RecurringTemplate エンティティ・関連エンティティ
2. Application: DTOs・インターフェース
3. Infrastructure: EF Core 設定（Configurations）
4. Infrastructure: AppDbContext 更新・マイグレーション手動作成・ModelSnapshot 更新
5. Infrastructure: RecurringTemplateRepository
6. Application: RecurringTaskService（CRUD + GenerateTasksAsync）
7. Infrastructure/Jobs: RecurringTaskJob・HangfireJobRegistrationService 更新
8. Presentation: RecurringTemplatesController・Program.cs DI 登録
9. Frontend: モデル・サービス
10. Frontend: 管理 UI コンポーネント・ルート・ナビゲーション
11. テスト: ユニットテスト・統合テスト
12. ビルド確認

## セキュリティ考慮事項
- 全 API エンドポイントをコントローラーレベルで `[Authorize(Roles = "Admin")]` 保護
- フロントエンドルートに `adminGuard` を適用

## パフォーマンス考慮事項
- `RecurringTemplateId` + 生成日時のインデックスにより重複チェッククエリを高速化
- Hangfire ジョブは毎分実行だが対象テンプレート数は少ない想定のため問題なし

## 将来の拡張性
- `ExcludeHolidays` カラムはDB定義済み、祝日マスタは別タスクで実装予定
