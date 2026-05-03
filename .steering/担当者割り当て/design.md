---
feature: 担当者割り当て
date: 2026-05-04
---

# 設計書: 担当者割り当て

## バックエンド設計

### 新規 DTO

```csharp
// Application/DTOs/Tasks/AddAssigneeRequest.cs
public class AddAssigneeRequest
{
    [Required]
    public int UserId { get; set; }
}
```

### ITaskRepository 追加メソッド

```csharp
Task<TaskAssignee> AddAssigneeAsync(int taskId, int userId);
Task RemoveAssigneeAsync(int taskId, int userId);
Task<bool> IsAssigneeAsync(int taskId, int userId);
```

### ITaskService 追加メソッド

```csharp
Task<AssigneeDto> AddAssigneeAsync(int taskId, int userId, int currentUserId);
Task RemoveAssigneeAsync(int taskId, int userId, int currentUserId);
```

### TasksController 追加エンドポイント

```
POST   /api/tasks/{id}/assignees           → 201 Created { AssigneeDto }
DELETE /api/tasks/{id}/assignees/{userId}  → 204 No Content
```

### バリデーション・エラーハンドリング

| 条件 | HTTPステータス |
|------|---------------|
| タスクが存在しない | 404 Not Found |
| 閲覧権限なし | 403 Forbidden |
| ユーザーが存在しない・無効 | 404 Not Found |
| 既に割り当て済み | 409 Conflict |
| 担当者でない (削除時) | 404 Not Found |

## フロントエンド設計

### task.service.ts 追加メソッド

```typescript
addAssignee(taskId: number, userId: number): Observable<AssigneeDto>
removeAssignee(taskId: number, userId: number): Observable<void>
```

### TaskListComponent 変更

- `filterForm` に `assigneeId: null` フィールド追加
- `users` シグナルを追加し `UserService.getAll()` でロード
- フィルターパネルに担当者 `mat-select` を追加

### TaskDetailComponent 変更

- 担当者セクションに「追加」ボタン (mat-icon-button, add アイコン)
- 担当者名の横に「削除」ボタン (mat-icon-button, close アイコン)
- 追加時: ユーザー一覧から未割当ユーザーを選ぶ `MatDialog` を表示
- 削除時: 確認なしで即時削除（UIの軽快さ優先）

### 担当者選択ダイアログ

新規コンポーネント: `AssigneeSelectDialogComponent`
- 既存担当者を除いたユーザー一覧を `mat-selection-list` で表示
- 単一選択・選択後に userId を返す

## 既存コードとの整合性

- `TaskAssignee` エンティティ・EF Core 設定は変更なし
- `CreateTaskRequest` / `UpdateTaskRequest` の `AssigneeIds` フィールドは変更なし
- 権限チェックは既存 `EnsureReadAccessAsync` を流用
