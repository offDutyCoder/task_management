# 要求内容

## 概要

定期タスクテンプレート機能を実装する。管理者がテンプレートを設定することで、日次・週次・月次の繰り返し業務に対するタスクをHangfireバックグラウンドジョブが自動生成する。

## 背景

日常的な繰り返し業務（朝の点検・清掃など）を毎回手動登録するコストをゼロにするため。PRDの定期タスク要件として「Hangfire または Quartz.NET のバックグラウンドジョブで自動生成」が明記されており、Hangfireはすでにプロジェクトに導入済みである。

## 実装対象の機能

### 1. バックエンド: RecurringTemplate エンティティ・CRUD API（管理者のみ）
- `RecurringTemplate` エンティティの定義と EF Core マイグレーション
- テンプレートにタイトル・説明・優先度・頻度・生成時刻・担当者・ラベル・共有ユーザーを設定できる
- CRUD API: GET/POST/PUT/DELETE /api/recurring-templates（管理者のみ）

### 2. バックグラウンドジョブ: 自動生成ロジック
- Hangfire の RecurringJob で毎分実行
- `GenerationTime`（±1分）に該当するテンプレートから `TaskItem` を自動生成
- 除外日判定（土日除外フラグ）
- 当日分の重複生成防止
- 生成時に担当者・ラベル・共有ユーザーをテンプレートから継承

### 3. フロントエンド: 管理画面 UI
- `/admin/recurring-templates` ルートに定期タスク管理ページを追加（管理者のみ）
- テンプレート一覧・作成・編集・無効化の UI
- サイドバーナビゲーションへのリンク追加

## 受け入れ条件

### バックエンド API
- [ ] GET /api/recurring-templates で管理者がテンプレート一覧を取得できる
- [ ] POST /api/recurring-templates でテンプレートを作成できる
- [ ] PUT /api/recurring-templates/{id} でテンプレートを更新できる
- [ ] DELETE /api/recurring-templates/{id} でテンプレートを無効化できる
- [ ] 一般ユーザーがアクセスした場合は 403 が返る

### 自動生成ジョブ
- [ ] Hangfire が毎分 `RecurringTaskJob` を実行する
- [ ] `GenerationTime` ±1分のアクティブなテンプレートから `TaskItem` が生成される
- [ ] 当日分がすでに生成済みの場合は重複生成しない
- [ ] `ExcludeWeekends=true` かつ土日の場合はスキップする
- [ ] 生成された `TaskItem` に担当者・ラベル・共有ユーザーが正しく引き継がれる
- [ ] `IsRecurring=true`・`RecurringTemplateId` が設定される

### フロントエンド
- [ ] 管理者がテンプレート一覧を確認できる
- [ ] テンプレートの作成・編集・無効化ができる
- [ ] サイドバーに「定期タスク管理」リンクが管理者にのみ表示される

## 成功指標
- 定期タスクの自動生成がスケジュール時刻から±5分以内に実行される（PRD 非機能要件）

## スコープ外
- 祝日除外機能（`ExcludeHolidays`）: 祝日マスタテーブルが未実装のため今回は DB カラムのみ定義
- 一般ユーザーがテンプレートを参照する UI
- E2E テスト（Playwright）

## 参照ドキュメント
- `docs/product-requirements.md` - 機能要件8: 定期タスク
- `docs/functional-design.md` - RecurringTemplate エンティティ定義・API設計・バックグラウンドジョブ設計
- `docs/architecture.md` - Hangfire 利用方針
