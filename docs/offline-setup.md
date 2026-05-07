# オフライン環境セットアップ手順

このドキュメントは、インターネットに接続できない社内 PC でこのプロジェクトを動かすための手順書です。

---

## プロジェクト内のファイル・ドキュメント一覧

| パス | 内容 |
|---|---|
| `create_database.sql` | DBとテーブルを一括作成するSQLスクリプト（初期データ含む） |
| `docs/offline-setup.md` | **このファイル** — オフライン環境セットアップ手順 |
| `docs/product-requirements.md` | プロダクト要件定義書 |
| `docs/functional-design.md` | 機能設計書 |
| `docs/architecture.md` | アーキテクチャ設計書 |
| `docs/development-guidelines.md` | 開発ガイドライン・コーディング規約 |
| `docs/repository-structure.md` | リポジトリ構成の説明 |
| `docs/glossary.md` | 用語集 |
| `backend/TaskManagement.Api/appsettings.json` | バックエンド設定（DB接続文字列はここを変更） |
| `frontend/proxy.conf.json` | フロントエンドのAPIプロキシ設定 |

---

## 前提条件（オフライン PC に確認済みのもの）

| ツール | 備考 |
|---|---|
| Visual Studio（.NET 8 SDK 含む） | インストール済み |
| Node.js | インストール済み |
| Angular Material 関連パッケージ | node_modules テンプレートとして保持 |
| SQL Server | インストール済み（インスタンス名は後述の方法で確認） |

---

## STEP 1: USB でファイルを転送する

オンライン PC でリポジトリを ZIP にまとめ、USB で移動します。

### 1-1. オンライン PC での操作

```powershell
# リポジトリのルートで実行
cd C:\Users\chomu\Desktop\todo\task_management

# git のファイル一式を ZIP に圧縮（node_modules は含まれない）
git archive --format=zip --output=C:\temp\task_management.zip HEAD
```

> **補足**: `git archive` はソースコードのみをパッケージします。`node_modules/`、`bin/`、`obj/` などのビルド成果物は自動的に除外されます。

### 1-2. USB に入れるファイル

| ファイル | 説明 |
|---|---|
| `task_management.zip` | ソースコード一式 |
| `node_modules/`（テンプレート） | 既存の Angular 用 node_modules をそのまま持参 |

---

## STEP 2: オフライン PC に展開する

```
C:\projects\task_management\    ← ZIP を展開する場所（任意）
```

展開後のフォルダ構成：

```
task_management/
├── backend/
├── frontend/
│   ├── src/
│   ├── package.json
│   └── node_modules/   ← ここに持参した node_modules を配置
├── create_database.sql
└── docs/
```

### node_modules の配置

持参した node_modules テンプレートを `frontend/` 直下にコピーします。

```powershell
# 例: USB が D: ドライブの場合
Copy-Item -Recurse "D:\node_modules_template\node_modules" "C:\projects\task_management\frontend\node_modules"
```

---

## STEP 3: SQL Server のインスタンス名を調べる

接続文字列の設定に必要です。以下のどれかで確認できます。

### 方法A: SQL Server Management Studio (SSMS) を起動する

SSMS を開くと「サーバーへの接続」ダイアログが表示されます。  
**「サーバー名」欄に表示されている値がインスタンス名**です。

例: `DESKTOP-ABC1234\SQLEXPRESS`

### 方法B: PowerShell で確認する

```powershell
# インストール済みの SQL Server インスタンスを一覧表示
Get-Service | Where-Object {$_.Name -like "MSSQL*"} | Select-Object Name, Status
```

出力例:
```
MSSQLSERVER          → インスタンス名は (local) または . または コンピュータ名
MSSQL$SQLEXPRESS     → インスタンス名は コンピュータ名\SQLEXPRESS
MSSQL$SQL2019        → インスタンス名は コンピュータ名\SQL2019
```

### 方法C: コンピュータ名を調べる

```powershell
$env:COMPUTERNAME
# 例: OFFICE-PC01
# → SQL Server のインスタンス名は OFFICE-PC01\SQLEXPRESS
```

---

## STEP 4: 接続文字列を変更する

[backend/TaskManagement.Api/appsettings.json](../backend/TaskManagement.Api/appsettings.json) を編集します。

### 変更前（開発元の PC 名が入っている）

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=LAPTOP-UIGGUU9G\\SQLEXPRESS;Database=TaskManagement;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 変更後（オフライン PC のインスタンス名に書き換える）

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=【インスタンス名】;Database=TaskManagement;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### インスタンス名の例

| SQL Server の種類 | Server= の値 |
|---|---|
| SQL Server Express（標準） | `コンピュータ名\\SQLEXPRESS` |
| SQL Server（デフォルトインスタンス） | `コンピュータ名` または `.` |
| SQL Server（名前付きインスタンス） | `コンピュータ名\\インスタンス名` |

> **注意**: JSON 内でバックスラッシュは `\\` と2つ書く必要があります。  
> 例: `OFFICE-PC01\SQLEXPRESS` → JSON では `OFFICE-PC01\\SQLEXPRESS`

---

## STEP 5: データベースを作成する

SQL Server Management Studio または `sqlcmd` でスクリプトを実行します。

### SSMS を使う場合

1. SSMS を開き、SQL Server に接続する
2. メニュー「ファイル」→「開く」→「ファイル」で `create_database.sql` を選択
3. 「実行」ボタン（F5）を押す

### sqlcmd を使う場合

```powershell
# プロジェクトルートで実行
sqlcmd -S "【インスタンス名】" -E -i create_database.sql
```

実行後、`TaskManagement` データベースと以下のテーブルが作成されます：

- `Users`（初期ユーザー: admin / admin123、member / member123）
- `TaskStatuses`（未着手、進行中、レビュー中、保留、完了）
- `TaskItems`、`Labels`、`Teams` など

---

## STEP 6: バックエンドを起動する

### Visual Studio を使う場合

1. `backend\TaskManagement.sln` を Visual Studio で開く
2. スタートアッププロジェクトを `TaskManagement.Api` に設定
3. **F5** または「デバッグなしで開始（Ctrl+F5）」を実行

起動後、API は `http://localhost:5000` で待ち受けます。

### dotnet CLI を使う場合

```powershell
cd C:\projects\task_management\backend\TaskManagement.Api
dotnet run
```

---

## STEP 7: フロントエンドを起動する

```powershell
cd C:\projects\task_management\frontend
npm start
```

ブラウザで `http://localhost:4200` を開きます。

> `npm start` は内部で `ng serve` を実行し、API への通信は `proxy.conf.json` の設定により自動的に `http://localhost:5000` に転送されます。

---

## 初期ログイン情報

| ロール | ログインID | パスワード |
|---|---|---|
| 管理者 | `admin` | `admin123` |
| メンバー | `member` | `member123` |

---

## トラブルシューティング

### データベース接続エラーが出る場合

- STEP 3 で調べたインスタンス名が `appsettings.json` と一致しているか確認
- SQL Server のサービスが起動しているか確認

```powershell
Get-Service | Where-Object {$_.Name -like "MSSQL*"}
# Status が "Running" になっていること
```

### `npm start` でエラーが出る場合

node_modules のバージョンが合っていない可能性があります。

```powershell
cd C:\projects\task_management\frontend
# node_modules を一旦削除して再配置
Remove-Item -Recurse -Force node_modules
# テンプレートの node_modules を再コピーしてから npm install を試みる
```

### ポートが使用中のエラーが出る場合

- バックエンド（5000番）: `appsettings.json` の `"urls"` を変更、または既存プロセスを終了
- フロントエンド（4200番）: `npm start -- --port 4300` で別ポートを使用し、`proxy.conf.json` の `target` も合わせて変更
