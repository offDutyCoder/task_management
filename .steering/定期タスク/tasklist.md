# タスクリスト

## 🚨 タスク完全完了の原則

**このファイルの全タスクが完了するまで作業を継続すること**

### 必須ルール
- **全てのタスクを`[x]`にすること**
- 「時間の都合により別タスクとして実施予定」は禁止
- 未完了タスク（`[ ]`）を残したまま作業を終了しない

### タスクスキップが許可される唯一のケース
技術的理由（実装方針変更・アーキテクチャ変更・依存関係変更）に該当する場合のみ:
```markdown
- [x] ~~タスク名~~（実装方針変更により不要: 具体的な技術的理由）
```

---

## フェーズ1: Domain・Application 層

- [x] `RecurringFrequency` enum を作成
  - [x] `backend/TaskManagement.Domain/Enums/RecurringFrequency.cs` を新規作成（Daily/Weekly/Monthly）

- [x] `RecurringTemplate` エンティティを作成
  - [x] `backend/TaskManagement.Domain/Entities/RecurringTemplate.cs` を新規作成

- [x] 関連エンティティを作成
  - [x] `RecurringTemplateAssignee.cs` を新規作成
  - [x] `RecurringTemplateLabel.cs` を新規作成
  - [x] `RecurringTemplateShare.cs` を新規作成

- [x] Application DTOs を作成
  - [x] `backend/TaskManagement.Application/DTOs/RecurringTemplates/CreateRecurringTemplateRequest.cs` を新規作成
  - [x] `backend/TaskManagement.Application/DTOs/RecurringTemplates/UpdateRecurringTemplateRequest.cs` を新規作成
  - [x] `backend/TaskManagement.Application/DTOs/RecurringTemplates/RecurringTemplateResponse.cs` を新規作成

- [x] インターフェースを作成
  - [x] `IRecurringTemplateRepository.cs` を新規作成
  - [x] `IRecurringTaskService.cs` を新規作成

## フェーズ2: Infrastructure 層（EF Core + Repository）

- [x] EF Core 設定ファイルを作成
  - [x] `RecurringTemplateConfiguration.cs` を新規作成
  - [x] `RecurringTemplateAssigneeConfiguration.cs` を新規作成
  - [x] `RecurringTemplateLabelConfiguration.cs` を新規作成
  - [x] `RecurringTemplateShareConfiguration.cs` を新規作成

- [x] `AppDbContext` を更新
  - [x] `RecurringTemplates`, `RecurringTemplateAssignees`, `RecurringTemplateLabels`, `RecurringTemplateShares` の DbSet を追加

- [x] EF Core マイグレーションを手動作成
  - [x] `20260505000001_AddNotificationsAndRecurringTemplates.cs` を新規作成（Notifications も同時追加）
  - [x] `20260505000001_AddNotificationsAndRecurringTemplates.Designer.cs` を新規作成
  - [x] `AppDbContextModelSnapshot.cs` を更新

- [x] `RecurringTemplateRepository` を実装
  - [x] `GetAllAsync()` - Include: Assignees, Labels, Shares
  - [x] `GetByIdAsync(int id)`
  - [x] `GetActiveForGenerationAsync(TimeSpan currentTime)` - GenerationTime ±1分
  - [x] `CreateAsync()` / `UpdateAsync()`
  - [x] `ExistsTitleAsync(string title, int? excludeId)`

## フェーズ3: Application Service + Infrastructure Jobs

- [x] `RecurringTaskService` を実装
  - [x] `GetAllAsync()` / `GetByIdAsync(int id)`
  - [x] `CreateAsync()` - 担当者・ラベル・共有ユーザーの関連も保存
  - [x] `UpdateAsync()` - 関連データの差分更新
  - [x] `DeactivateAsync(int id)`
  - [x] `GenerateTasksAsync(DateOnly today, TimeSpan currentTime)` - 除外日判定・重複防止・TaskItem生成

- [x] `RecurringTaskJob` を作成
  - [x] `backend/TaskManagement.Infrastructure/Jobs/RecurringTaskJob.cs` を新規作成（AutomaticRetry）

- [x] `HangfireJobRegistrationService` を更新
  - [x] `RecurringTaskJob` を毎分実行（`Cron.Minutely`）で登録追加

## フェーズ4: Presentation 層 + DI 登録

- [x] `RecurringTemplatesController` を作成
  - [x] `backend/TaskManagement.Api/Controllers/RecurringTemplatesController.cs` を新規作成
  - [x] GET / POST / PUT / DELETE エンドポイントを実装（全て `[Authorize(Roles = "Admin")]`）

- [x] `Program.cs` を更新
  - [x] `IRecurringTemplateRepository` → `RecurringTemplateRepository` を DI 登録
  - [x] `IRecurringTaskService` → `RecurringTaskService` を DI 登録
  - [x] `RecurringTaskJob` を DI 登録

## フェーズ5: フロントエンド

- [x] モデルを作成
  - [x] `frontend/src/app/models/recurring-template.model.ts` を新規作成

- [x] サービスを作成
  - [x] `frontend/src/app/services/recurring-template.service.ts` を新規作成

- [x] 管理 UI コンポーネントを作成
  - [x] `recurring-template-management.component.ts` を新規作成
  - [x] `recurring-template-management.component.html` を新規作成
  - [x] `recurring-template-management.component.scss` を新規作成
  - [x] `recurring-template-dialog.component.ts` を新規作成

- [x] ルートを追加
  - [x] `app.routes.ts` に `/admin/recurring-templates` を追加

- [x] ナビゲーションを追加
  - [x] `shell.component.html` に「定期タスク管理」リンクを追加（管理者のみ）

## フェーズ6: テスト

- [x] ユニットテストを作成
  - [x] `RecurringTaskServiceTests.cs` を新規作成
    - [x] `GenerateTasksAsync`: 除外日判定のテスト（土日スキップ）
    - [x] `GenerateTasksAsync`: 重複防止のテスト
    - [x] `GenerateTasksAsync`: 正常生成のテスト

- [x] 統合テストを作成
  - [x] `RecurringTemplatesControllerTests.cs` を新規作成
    - [x] 管理者認証なしで 401
    - [x] 一般ユーザーで 403
    - [x] 管理者でテンプレート作成・取得・無効化

## フェーズ7: 品質チェック

- [x] バックエンドビルドが成功することを確認
  - [x] `dotnet build` を実行 → 0 Error(s)
- [x] バックエンドテストが通ることを確認
  - [x] `dotnet test` を実行 → Passed: 104, Failed: 0
- [x] フロントエンドビルドが成功することを確認
  - [x] `npm run build` を実行（frontend/ ディレクトリ）→ 成功（バジェット警告のみ・エラーなし）
- [x] フロントエンド型チェックが通ることを確認
  - [x] `npm run typecheck` を実行 → エラーなし

---

## 実装後の振り返り

### 実装完了日
2026-05-05

### 計画と実績の差分

**計画と異なった点**:
- `Notification` エンティティがすでに AppDbContext に追加されていたが Migration が存在しなかったため、`AddRecurringTemplates` マイグレーションに Notifications テーブルの作成も含めた（`AddNotificationsAndRecurringTemplates` に変更）
- `UpdateRecurringTemplateRequest` を当初独立したクラスとして作成したが、検証後 `CreateRecurringTemplateRequest` の継承に変更

**新たに必要になったタスク**:
- `IRecurringTemplateRepository.HasGeneratedTodayAsync` の追加（Application 層から DbContext を直接使用しないため）
- 検証指摘による `UpdateAsync` 後の `GetByIdAsync` 再取得修正
- `ParseGenerationTime` の ValidationException ガード追加

**技術的理由でスキップしたタスク**（該当する場合のみ）:
- `ExcludeHolidays` のフロントエンド UI: 祝日マスターテーブルが未実装のためバックエンドも常時 false でハードコード（DB カラムは定義済み）

### 学んだこと

**技術的な学び**:
- EF Core の Update 後にナビゲーションプロパティが遅延ロードされないため、MapToResponse 前に再取得が必要
- Angular テンプレートではアロー関数（`.map()` 等）を直接バインドできないため、コンポーネントにヘルパーメソッドを追加する必要がある
- Hangfire ジョブの DateTime.Now（ローカル時刻）と DateTime.UtcNow の混在はタイムゾーン依存のバグになるため、コメントで前提を明記する重要性

**プロセス上の改善点**:
- 実装検証サブエージェントが必須修正点（UpdateAsync 後の再取得漏れ）を正確に特定できた
- tasklist.md のフェーズ区切りがあることで実装の進捗把握が容易だった

### 次回への改善提案
- 週次テンプレートで WeekDays が null の場合の CreateAsync/UpdateAsync バリデーション追加（現在は無音でタスク非生成）
- PUT 統合テストケースの追加（更新後レスポンスのデータ整合性検証）
- ExcludeHolidays の有効化（祝日マスターテーブル実装後）
- `DEFAULT_STATUS_ID` として設定なしタスクの考慮
