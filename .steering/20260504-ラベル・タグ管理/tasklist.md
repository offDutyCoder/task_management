# タスクリスト

## 🚨 タスク完全完了の原則

**このファイルの全タスクが完了するまで作業を継続すること**

### 必須ルール
- **全てのタスクを`[x]`にすること**
- 「時間の都合により別タスクとして実施予定」は禁止
- 「実装が複雑すぎるため後回し」は禁止
- 未完了タスク（`[ ]`）を残したまま作業を終了しない

---

## フェーズ1: バックエンド – LabelRequest 基盤

- [x] `LabelRequest.cs` エンティティ作成 (`backend/TaskManagement.Domain/Entities/`)
  - [x] Id, RequestedByUserId, RequestedName, Reason, Status, CreatedAt, UpdatedAt, ナビゲーション
  - [x] `LabelRequestStatus` enum (Pending/Approved/Rejected) を同ファイルに定義
- [x] DTO 作成 (`backend/TaskManagement.Application/DTOs/Labels/`)
  - [x] `CreateLabelRequestDto.cs` (RequestedName Required/Max50, Reason Max200)
  - [x] `UpdateLabelRequestStatusDto.cs` (Status, Color)
  - [x] `LabelRequestResponse.cs` (Id, RequestedName, Reason, Status, RequestedByDisplayName, CreatedAt)
- [x] `ILabelRequestRepository.cs` インターフェース作成 (`backend/TaskManagement.Application/Interfaces/`)
- [x] `ILabelRequestService.cs` インターフェース作成 (`backend/TaskManagement.Application/Interfaces/`)
- [x] `LabelRequestRepository.cs` 実装 (`backend/TaskManagement.Infrastructure/Repositories/`)
  - [x] GetAllAsync (status フィルター付き)
  - [x] GetByIdAsync
  - [x] CreateAsync
  - [x] UpdateAsync
- [x] `LabelRequestService.cs` 実装 (`backend/TaskManagement.Application/Services/`)
  - [x] GetAllAsync (全件 or ステータスフィルター)
  - [x] CreateAsync (ユーザーIDと DTO を受け取り保存)
  - [x] ApproveAsync (ラベル自動作成 + ステータス更新、既処理チェック)
  - [x] RejectAsync (ステータス更新、既処理チェック)
- [x] `AppDbContext.cs` に `DbSet<LabelRequest>` 追加 + EF Fluent API 設定
- [x] `Program.cs` に DI 登録 (ILabelRequestRepository, ILabelRequestService)

## フェーズ2: バックエンド – LabelsController 拡張

- [x] `LabelsController.cs` にリクエスト関連エンドポイントを追加
  - [x] `POST /api/labels/requests` (認証済みユーザー – リクエスト送信)
  - [x] `GET /api/labels/requests` (管理者のみ – 一覧取得、statusクエリパラメーター)
  - [x] `PUT /api/labels/requests/{id}` (管理者のみ – 承認/却下)
- [x] `ClaimTypes.NameIdentifier` からユーザーIDを取得するヘルパー実装（既存パターンに従う）

## フェーズ3: バックエンド – EFマイグレーション

- [x] EF Core マイグレーション追加 (`dotnet ef migrations add AddLabelRequests`)
- [x] マイグレーション内容確認（LabelRequests テーブル作成、FK設定、インデックス）

## フェーズ4: バックエンド – ユニットテスト

- [x] `LabelRequestServiceTests.cs` 作成
  - [x] `ApproveAsync_WhenPending_CreatesLabelAndUpdatesStatus` テスト
  - [x] `ApproveAsync_WhenAlreadyApproved_ThrowsValidationException` テスト
  - [x] `CreateAsync_WhenDuplicateLabelName_ThrowsValidationException` テスト（既存ラベル名チェック）

## フェーズ5: フロントエンド – モデル・サービス

- [x] `label-request.model.ts` 作成 (`frontend/src/app/models/`)
  - [x] LabelRequest, CreateLabelRequestDto, UpdateLabelRequestStatusDto インターフェース
  - [x] LabelRequestStatus 型 ('Pending' | 'Approved' | 'Rejected')
- [x] `label-request.service.ts` 作成 (`frontend/src/app/services/`)
  - [x] getAll(status?: string): Observable<LabelRequest[]>
  - [x] create(request: CreateLabelRequestDto): Observable<LabelRequest>
  - [x] updateStatus(id: number, dto: UpdateLabelRequestStatusDto): Observable<LabelRequest>

## フェーズ6: フロントエンド – LabelManagementComponent 修正

- [x] `label-management.component.ts` の `LabelDialogComponent` を修正
  - [x] `MAT_DIALOG_DATA` を使ってモード・既存ラベルを注入（StatusDialogComponent パターンに統一）
  - [x] 編集時に既存値（name, color, isActive）を初期値としてフォームに設定
  - [x] HEX カラーコードのバリデーション (`/^#[0-9A-Fa-f]{6}$/`) と エラーメッセージ追加
  - [x] 編集モードに `mat-slide-toggle` (isActive) を追加
  - [x] カラープレビュー（入力フィールド横に円形ドット）をテンプレートに追加
- [x] `openEditDialog` でダイアログに既存ラベルのデータを渡すように修正
- [x] `openCreateDialog` でモード 'create' を渡すように修正

## フェーズ7: フロントエンド – LabelRequestManagementComponent 新規作成

- [x] `label-request-management.component.ts` 作成 (`frontend/src/app/pages/admin/label-request-management/`)
  - [x] リクエスト一覧テーブル (要求者名・ラベル名・理由・ステータス・日時・操作)
  - [x] ステータスフィルター (全件 / Pending / Approved / Rejected) の MatSelectまたはボタングループ
  - [x] 承認ボタン → カラー入力ダイアログ → ApproveAsync 呼び出し
  - [x] 却下ボタン → 確認SnackBar → RejectAsync 呼び出し
- [x] ステータスバッジ色分け (Pending=orange / Approved=green / Rejected=red)

## フェーズ8: フロントエンド – LabelRequestDialogComponent 新規作成

- [x] `label-request-dialog.component.ts` 作成 (`frontend/src/app/pages/tasks/label-request-dialog/`)
  - [x] ラベル名（必須・最大50文字）と理由（任意・最大200文字）の入力フォーム
  - [x] 送信時に LabelRequestService.create() を呼び出す
  - [x] 既存ラベル一覧を取得して重複名をバリデーション（クライアント側 warning）
- [x] タスク一覧ページのフィルターパネル付近に「ラベルをリクエスト」ボタンを追加

## フェーズ9: フロントエンド – ルーティング追加

- [x] `app.routes.ts` に `/admin/label-requests` ルートを追加（LabelRequestManagementComponent、AdminGuard + AuthGuard）

## フェーズ10: 品質チェックと修正

- [x] フロントエンドビルドが成功することを確認 (`npm run build`)
- [x] リントエラーがないことを確認 (`npm run lint`)
- [x] 型エラーがないことを確認 (`npm run typecheck`)
- [x] バックエンドビルドが成功することを確認 (`dotnet build`)
- [x] バックエンドテストが通ることを確認 (`dotnet test`) - 71件 Passed

---

## 実装後の振り返り

### 実装完了日
2026-05-04

### 計画と実績の差分

**計画と異なった点**:
- `ApproveAsync` のトランザクション設計: 設計書では「両エンティティをトランザクション内で保存」と明記していたが、最初の実装では Repository を2回個別に呼び出す形になっていた。`ILabelRequestRepository` に `ApproveWithLabelAsync` メソッドを追加し、Repository 層でトランザクションを管理する形で修正
- `LabelRequestDialogComponent` の状態管理: 最初は関数参照の書き換えで実装したが、既存パターンとの整合性のため Angular `signal()` に統一
- 却下ボタンの確認ステップ: 最初の実装では省略していたが、tasklist.md の仕様に従って Snackbar アクションによる確認を追加

**新たに必要になったタスク**:
- バックエンドの EF マイグレーション実行に必要な `AppDbContextFactory` の作成（デザインタイムファクトリーが存在しなかった）
- `LabelRequestServiceTests` の追加テスト: `RejectAsync` の正常系・処理済み・NotFound の各ケース、`ApproveAsync` の NotFound ケース（合計+4テスト、71→75件）

### 学んだこと

**技術的な学び**:
- EF Core のデザインタイムマイグレーションには `IDesignTimeDbContextFactory` が必要。スタートアッププロジェクトが `Microsoft.EntityFrameworkCore.Design` を参照していない場合は Infrastructure プロジェクトに配置する
- Repository 層でのトランザクション管理: 複数エンティティをアトミックに保存する場合は、専用メソッド（`ApproveWithLabelAsync`）を Repository に追加し `await using var transaction` で包む方法が既存パターンを壊さずに対応できる

**プロセス上の改善点**:
- 設計書にトランザクション要件が明記されていたにもかかわらず実装時に見落とした。設計書の「データフロー」セクションを実装前に再確認する習慣が重要

### 次回への改善提案
- 設計書でトランザクションが必要なオペレーションに `[TRANSACTION]` のようなタグを付けておくと実装時の見落としを防げる
- `GetCurrentUserId()` がコントローラー間で重複しているため、次回コントローラーを追加する際は共通の基底クラスまたは拡張メソッドへの移行を検討する
