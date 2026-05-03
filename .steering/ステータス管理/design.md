# 設計書: ステータス管理

## アーキテクチャ概要

既存のClean Architectureパターンに従い、以下の4層に実装する。

```
Domain → Application → Infrastructure → API
```

---

## バックエンド設計

### Domain層

#### `TaskManagement.Domain/Entities/TaskStatus.cs`
```csharp
public class TaskStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

### Application層

#### DTOs
- `DTOs/Statuses/StatusResponse.cs` - レスポンス用 (Id, Name, Color, DisplayOrder, IsActive, CreatedAt)
- `DTOs/Statuses/CreateStatusRequest.cs` - 作成用 (Name, Color, DisplayOrder)
- `DTOs/Statuses/UpdateStatusRequest.cs` - 更新用 (Name, Color, DisplayOrder, IsActive)

#### Interfaces
- `Interfaces/IStatusRepository.cs` - GetAll, GetById, Create, Update, ExistsUsedByTask
- `Interfaces/IStatusService.cs` - GetAll, GetById, Create, Update, Delete

#### Services
- `Services/StatusService.cs` - ビジネスロジック
  - Delete時: タスクで使用中か確認 → 使用中なら ValidationException (409の代わりにValidationExceptionで統一)
  - 実際には使用中チェックは ConflictException を別途作るか ValidationException を流用する

---

### Infrastructure層

#### `Infrastructure/Data/AppDbContext.cs` (変更)
```csharp
public DbSet<TaskStatus> TaskStatuses => Set<TaskStatus>();
```

#### `Infrastructure/Data/Configurations/TaskStatusConfiguration.cs`
- テーブル名: `TaskStatuses`
- Name: required, MaxLength(50)
- Color: required, MaxLength(7) (#RRGGBB)
- DisplayOrder: required
- HasData: 初期5件のシードデータ

#### `Infrastructure/Repositories/StatusRepository.cs`
- `GetAllAsync()`: IsActive = true のみ, DisplayOrder順
- `GetByIdAsync(int id)`: 全件（論理削除済も含む）
- `CreateAsync(TaskStatus status)`
- `UpdateAsync(TaskStatus status)`
- `ExistsUsedByTaskAsync(int statusId)`: TaskItemsテーブルで使用中か確認（将来実装。現時点では TaskItems が存在しないので常にfalse）

---

### API層

#### `Controllers/StatusesController.cs`
```
GET  /api/statuses          → 全ユーザー参照可 [Authorize]
GET  /api/statuses/{id}     → 管理者のみ [Authorize(Roles = "Admin")]
POST /api/statuses          → 管理者のみ [Authorize(Roles = "Admin")]
PUT  /api/statuses/{id}     → 管理者のみ [Authorize(Roles = "Admin")]
DELETE /api/statuses/{id}   → 管理者のみ [Authorize(Roles = "Admin")]
```

---

## フロントエンド設計

### `models/task-status.model.ts`
```typescript
export interface TaskStatus {
  id: number;
  name: string;
  color: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
}
export interface CreateStatusRequest { name: string; color: string; displayOrder: number; }
export interface UpdateStatusRequest { name: string; color: string; displayOrder: number; isActive: boolean; }
```

### `services/status.service.ts`
- `getAll()`: GET /api/statuses
- `getById(id)`: GET /api/statuses/{id}
- `create(request)`: POST /api/statuses
- `update(id, request)`: PUT /api/statuses/{id}
- `delete(id)`: DELETE /api/statuses/{id}

### `pages/admin/status-management/`
- `status-management.component.ts/.html/.scss` - user-managementと同パターン
  - MatTable: name, color (バッジ表示), displayOrder, isActive, actions
  - 作成ダイアログ・編集ダイアログ・有効/無効切り替え・削除ボタン
- `status-dialog.component.ts` - inline templateパターン (user-dialogと同様)

### ルーティング `app.routes.ts`
```typescript
{
  path: 'admin/statuses',
  loadComponent: () => import('./pages/admin/status-management/status-management.component').then(m => m.StatusManagementComponent),
  canActivate: [authGuard, adminGuard],
}
```

---

## テスト設計

### `Unit/Services/StatusServiceTests.cs`
- GetAll: 正常系
- GetById: 存在する/存在しない
- Create: 正常系
- Update: 存在する/存在しない
- Delete: 使用中でない場合/使用中の場合(ValidationException)

### `Integration/Controllers/StatusesControllerTests.cs`
- GET /api/statuses: 認証なし→401, 認証あり→200
- POST /api/statuses: 管理者→201, 一般ユーザー→403
- DELETE /api/statuses/{id}: 存在しない→404

---

## 考慮事項

- `TaskStatus` は C# 標準の `System.Threading.Tasks.TaskStatus` enum と名前が衝突する可能性があるが、
  異なるnamespace (`TaskManagement.Domain.Entities`) で定義するため、using文の管理で回避可能
- `ExistsUsedByTaskAsync` はTaskItemエンティティがまだ存在しないため、現時点では常に false を返す実装にする
- 削除時の競合チェックは ConflictException を新規作成して409を返す（ValidationExceptionとは区別する）
