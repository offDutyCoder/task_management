# 設計書 - タスク管理（CRUD・ツリー構造）

## アーキテクチャ概要

既存のレイヤードアーキテクチャを踏襲する。

```
Angular SPA (frontend)
    │ HTTPS / withCredentials
    ↓
ASP.NET Core Web API
    ├── TasksController     (GET/POST/PUT/DELETE /api/tasks, /api/tasks/{id})
    └── LabelsController    (GET/POST/PUT /api/labels)
         ↓ [Authorize]
    TaskService / LabelService   (ビジネスロジック・共有範囲チェック)
         ↓
    TaskRepository / LabelRepository (EF Core)
         ↓
    SQL Server  (TaskItems, TaskAssignees, TaskLabels, TaskShares, Labels テーブル)
```

## コンポーネント設計

### 1. ドメイン層

**`TaskManagement.Domain/Entities/TaskItem.cs`**
```csharp
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? ParentTaskId { get; set; }
    public bool IsRecurring { get; set; }
    public int? RecurringTemplateId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation
    public Domain.Entities.TaskStatus Status { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> SubTasks { get; set; }
    public ICollection<TaskAssignee> Assignees { get; set; }
    public ICollection<TaskLabel> TaskLabels { get; set; }
    public ICollection<TaskShare> Shares { get; set; }
}
```

**`TaskManagement.Domain/Entities/TaskAssignee.cs`**
- `TaskItemId` (FK), `UserId` (FK), `AssignedAt`

**`TaskManagement.Domain/Entities/TaskLabel.cs`** (中間テーブル)
- `TaskItemId` (FK), `LabelId` (FK)

**`TaskManagement.Domain/Entities/TaskShare.cs`**
- `TaskItemId` (FK), `UserId` (FK)

**`TaskManagement.Domain/Entities/Label.cs`**
- `Id`, `Name` (一意・50文字), `Color`, `IsActive`, `CreatedAt`, `UpdatedAt`

**`TaskManagement.Domain/Enums/TaskPriority.cs`**
- `High = 0`, `Medium = 1`, `Low = 2`

### 2. アプリケーション層（Interfaces）

**`ITaskRepository`**
- `GetByIdAsync(int id, bool includeSubTasks = false)` → `TaskItem?`
- `GetListAsync(TaskFilterQuery filter, int currentUserId)` → `(IEnumerable<TaskItem> items, int totalCount)`
- `CreateAsync(TaskItem task)` → `TaskItem`
- `UpdateAsync(TaskItem task)` → `TaskItem`
- `DeleteAsync(int id)` → `void`
- `GetParentSharesAsync(int parentTaskId)` → `IEnumerable<TaskShare>`

**`ILabelRepository`**
- `GetAllActiveAsync()` → `IEnumerable<Label>`
- `GetByIdAsync(int id)` → `Label?`
- `ExistsNameAsync(string name, int? excludeId)` → `bool`
- `CreateAsync(Label label)` → `Label`
- `UpdateAsync(Label label)` → `Label`

**`ITaskService`**
- `GetTasksAsync(TaskFilterQuery filter, int currentUserId)` → `TaskListResponse`
- `GetTaskByIdAsync(int id, int currentUserId)` → `TaskDetailResponse`
- `CreateTaskAsync(CreateTaskRequest request, int currentUserId)` → `TaskDetailResponse`
- `UpdateTaskAsync(int id, UpdateTaskRequest request, int currentUserId)` → `TaskDetailResponse`
- `DeleteTaskAsync(int id, int currentUserId)` → `void`

**`ILabelService`**
- `GetAllAsync()` → `IEnumerable<LabelResponse>`
- `CreateAsync(CreateLabelRequest request)` → `LabelResponse`
- `UpdateAsync(int id, UpdateLabelRequest request)` → `LabelResponse`

### 3. アプリケーション層（DTOs）

**Task DTOs**:
- `CreateTaskRequest`: `{ Title, Description?, StatusId, Priority, DueDate?, ParentTaskId?, AssigneeIds, LabelIds, ShareUserIds }`
- `UpdateTaskRequest`: `{ Title, Description?, StatusId, Priority, DueDate?, AssigneeIds, LabelIds, ShareUserIds }`
- `TaskFilterQuery`: `{ StatusId?, AssigneeId?, LabelId?, Priority?, Search?, IsRecurring?, Page, PageSize }`
- `TaskListItem`: `{ Id, Title, Status, Priority, DueDate?, Assignees[], Labels[], SubTaskCount, IsRecurring }`
- `TaskListResponse`: `{ Items[], TotalCount, Page, PageSize }`
- `TaskDetailResponse`: `{ Id, Title, Description?, Status, Priority, DueDate?, ParentTask?, SubTasks[], Assignees[], Labels[], ShareUsers[], CreatedAt }`
- `AssigneeDto`: `{ Id, DisplayName }`
- `LabelDto`: `{ Id, Name, Color }`
- `StatusDto`: `{ Id, Name, Color }`

**Label DTOs**:
- `CreateLabelRequest`: `{ Name, Color }`
- `UpdateLabelRequest`: `{ Name, Color, IsActive }`
- `LabelResponse`: `{ Id, Name, Color, IsActive }`

### 4. インフラ層

**EF Core Configurations**:
- `TaskItemConfiguration`: TaskItems テーブル、インデックス（StatusId, DueDate, ParentTaskId, CreatedByUserId）
- `TaskAssigneeConfiguration`: TaskAssignees テーブル、複合 PK
- `TaskLabelConfiguration`: TaskLabels テーブル、複合 PK
- `TaskShareConfiguration`: TaskShares テーブル、複合 PK
- `LabelConfiguration`: Labels テーブル、Name ユニーク制約

**`TaskRepository`**: EF Core による CRUD + フィルター + 共有範囲チェック
**`LabelRepository`**: EF Core による CRUD
**`AppDbContext`**: 新 DbSet 追加

### 5. プレゼンテーション層

**`TasksController`**:
- `GET /api/tasks` → `[Authorize]`（フィルター・ページネーション）
- `POST /api/tasks` → `[Authorize]`
- `GET /api/tasks/{id}` → `[Authorize]`
- `PUT /api/tasks/{id}` → `[Authorize]`
- `DELETE /api/tasks/{id}` → `[Authorize]`

**`LabelsController`**:
- `GET /api/labels` → `[Authorize]`
- `POST /api/labels` → `[Authorize(Roles="Admin")]`
- `PUT /api/labels/{id}` → `[Authorize(Roles="Admin")]`

### 6. フロントエンド

**Models**:
- `task.model.ts`: `Task`, `TaskListItem`, `TaskListResponse`, `TaskDetailResponse`, `CreateTaskRequest`, `UpdateTaskRequest`, `TaskFilterQuery`, `TaskPriority` enum, `AssigneeDto`, `LabelDto`, `StatusDto`
- `label.model.ts`: `Label`, `CreateLabelRequest`, `UpdateLabelRequest`

**Services**:
- `task.service.ts`: CRUD + フィルター HTTP 呼び出し
- `label.service.ts`: ラベル一覧・管理 HTTP 呼び出し

**Pages**:
- `task-list/task-list.component.*`: タスク一覧・フィルター・新規ボタン
- `task-detail/task-detail.component.*`: タスク詳細・サブタスク一覧・編集・削除
- `task-form/task-form.component.*`: タスク作成・編集ダイアログ

**Routes**: `/tasks`, `/tasks/:id` を追加

## 共有範囲チェックロジック

```
サブタスク（ParentTaskId != null）の場合:
  → 親タスクの TaskShares を参照してアクセス可否を判定

親タスク（ParentTaskId == null）の場合:
  → 自タスクの TaskShares を参照してアクセス可否を判定

作成者（CreatedByUserId）は常にアクセス可能
```

## データフロー

### タスク作成フロー
```
1. Angular: POST /api/tasks { title, statusId, assigneeIds, shareUserIds, ... }
2. TasksController: [Authorize] チェック → CreateTaskAsync 呼び出し
3. TaskService:
   a. 親タスクが指定された場合、親タスクの存在と閲覧権限チェック
   b. TaskItem INSERT
   c. TaskAssignees INSERT（担当者分）
   d. TaskShares INSERT（共有メンバー分）
   e. TaskLabels INSERT（ラベル分）
4. レスポンス: 201 Created { TaskDetailResponse }
```

### サブタスク閲覧フロー
```
1. Angular: GET /api/tasks/{subTaskId}
2. TaskService: TaskItem を取得（ParentTaskId あり）
3. 親タスクの TaskShares から currentUserId の閲覧権限を確認
4. 権限あり → 200 OK / なし → 403
```

## テスト戦略

### ユニットテスト (xUnit + Moq)
- `TaskService.GetTaskByIdAsync_WhenUserNotInShareList_ShouldThrowForbiddenException`
- `TaskService.GetTaskByIdAsync_WhenSubTask_ShouldCheckParentShares`
- `TaskService.CreateTaskAsync_WhenParentTaskNotFound_ShouldThrowNotFoundException`
- `TaskService.DeleteTaskAsync_WhenUserNotCreator_ShouldThrowForbiddenException`

### 統合テスト (ASP.NET Core TestServer)
- タスク CRUD の正常・異常系
- 共有範囲チェック（403）
- フィルター・ページネーション

## ディレクトリ構造

```
backend/
├── TaskManagement.Domain/
│   ├── Entities/TaskItem.cs
│   ├── Entities/TaskAssignee.cs
│   ├── Entities/TaskLabel.cs
│   ├── Entities/TaskShare.cs
│   ├── Entities/Label.cs
│   └── Enums/TaskPriority.cs
├── TaskManagement.Application/
│   ├── Interfaces/ITaskRepository.cs
│   ├── Interfaces/ITaskService.cs
│   ├── Interfaces/ILabelRepository.cs
│   ├── Interfaces/ILabelService.cs
│   ├── DTOs/Tasks/CreateTaskRequest.cs
│   ├── DTOs/Tasks/UpdateTaskRequest.cs
│   ├── DTOs/Tasks/TaskFilterQuery.cs
│   ├── DTOs/Tasks/TaskListItem.cs
│   ├── DTOs/Tasks/TaskListResponse.cs
│   ├── DTOs/Tasks/TaskDetailResponse.cs
│   ├── DTOs/Tasks/AssigneeDto.cs
│   ├── DTOs/Tasks/LabelDto.cs
│   ├── DTOs/Tasks/StatusDto.cs
│   ├── DTOs/Labels/CreateLabelRequest.cs
│   ├── DTOs/Labels/UpdateLabelRequest.cs
│   ├── DTOs/Labels/LabelResponse.cs
│   ├── Services/TaskService.cs
│   └── Services/LabelService.cs
├── TaskManagement.Infrastructure/
│   ├── Data/Configurations/TaskItemConfiguration.cs
│   ├── Data/Configurations/TaskAssigneeConfiguration.cs
│   ├── Data/Configurations/TaskLabelConfiguration.cs
│   ├── Data/Configurations/TaskShareConfiguration.cs
│   ├── Data/Configurations/LabelConfiguration.cs
│   ├── Repositories/TaskRepository.cs
│   └── Repositories/LabelRepository.cs
├── TaskManagement.Api/
│   ├── Controllers/TasksController.cs
│   └── Controllers/LabelsController.cs
└── TaskManagement.Tests/
    ├── Unit/Services/TaskServiceTests.cs
    └── Integration/Controllers/TasksControllerTests.cs

frontend/src/app/
├── models/task.model.ts
├── models/label.model.ts
├── services/task.service.ts
├── services/label.service.ts
├── pages/tasks/task-list/task-list.component.*
├── pages/tasks/task-detail/task-detail.component.*
└── pages/tasks/task-form/task-form.component.*
```
