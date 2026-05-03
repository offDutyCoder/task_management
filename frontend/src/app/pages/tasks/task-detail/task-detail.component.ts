import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { CreateTaskRequest, TaskDetailResponse, TaskPriority, UpdateTaskRequest } from '../../../models/task.model';
import { User } from '../../../models/user.model';
import { TaskService } from '../../../services/task.service';
import { UserService } from '../../../services/user.service';
import { TaskFormComponent, TaskFormDialogData } from '../task-form/task-form.component';
import { AssigneeSelectDialogComponent, AssigneeSelectDialogData } from '../assignee-select-dialog/assignee-select-dialog.component';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatChipsModule,
    MatDividerModule,
    MatListModule,
    MatProgressSpinnerModule,
    DatePipe,
  ],
  templateUrl: './task-detail.component.html',
  styleUrl: './task-detail.component.scss',
})
export class TaskDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly taskService = inject(TaskService);
  private readonly userService = inject(UserService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly TaskPriority = TaskPriority;
  readonly task = signal<TaskDetailResponse | null>(null);
  readonly isLoading = signal(false);
  private allUsers: User[] = [];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    void this.loadTask(id);
    void this.loadUsers();
  }

  private async loadUsers(): Promise<void> {
    try {
      const users = await firstValueFrom(this.userService.getAll());
      this.allUsers = users.filter(u => u.isActive);
    } catch {
      // ユーザー一覧取得失敗は担当者追加UIに影響するが致命的ではない
    }
  }

  async loadTask(id: number): Promise<void> {
    this.isLoading.set(true);
    try {
      const task = await firstValueFrom(this.taskService.getById(id));
      this.task.set(task);
    } catch {
      this.snackBar.open('タスクの取得に失敗しました', '閉じる', { duration: 3000 });
      void this.router.navigate(['/tasks']);
    } finally {
      this.isLoading.set(false);
    }
  }

  openEditDialog(): void {
    const current = this.task();
    if (!current) return;

    const dialogRef = this.dialog.open(TaskFormComponent, {
      width: '560px',
      data: { mode: 'edit', task: current } satisfies TaskFormDialogData,
    });

    dialogRef.afterClosed().subscribe(async (result: UpdateTaskRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.taskService.update(current.id, result));
        this.snackBar.open('タスクを更新しました', '閉じる', { duration: 3000 });
        await this.loadTask(current.id);
      } catch {
        this.snackBar.open('エラーが発生しました。管理者にご連絡ください', '閉じる', { duration: 5000 });
      }
    });
  }

  openAddSubTaskDialog(): void {
    const current = this.task();
    if (!current) return;

    const dialogRef = this.dialog.open(TaskFormComponent, {
      width: '560px',
      data: { mode: 'create', parentTaskId: current.id } satisfies TaskFormDialogData,
    });

    dialogRef.afterClosed().subscribe(async (result: CreateTaskRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.taskService.create(result));
        this.snackBar.open('サブタスクを作成しました', '閉じる', { duration: 3000 });
        await this.loadTask(current.id);
      } catch {
        this.snackBar.open('エラーが発生しました。管理者にご連絡ください', '閉じる', { duration: 5000 });
      }
    });
  }

  openAddAssigneeDialog(): void {
    const current = this.task();
    if (!current) return;

    const assignedIds = new Set(current.assignees.map(a => a.id));
    const availableUsers = this.allUsers.filter(u => !assignedIds.has(u.id));

    const dialogRef = this.dialog.open(AssigneeSelectDialogComponent, {
      width: '360px',
      data: { users: availableUsers } satisfies AssigneeSelectDialogData,
    });

    dialogRef.afterClosed().subscribe(async (userId: number | undefined) => {
      if (!userId) return;
      try {
        await firstValueFrom(this.taskService.addAssignee(current.id, userId));
        this.snackBar.open('担当者を追加しました', '閉じる', { duration: 3000 });
        await this.loadTask(current.id);
      } catch {
        this.snackBar.open('エラーが発生しました。管理者にご連絡ください', '閉じる', { duration: 5000 });
      }
    });
  }

  openAddShareDialog(): void {
    const current = this.task();
    if (!current) return;

    const sharedIds = new Set(current.shareUsers.map(u => u.id));
    const availableUsers = this.allUsers.filter(u => !sharedIds.has(u.id));

    const dialogRef = this.dialog.open(AssigneeSelectDialogComponent, {
      width: '360px',
      data: { users: availableUsers } satisfies AssigneeSelectDialogData,
    });

    dialogRef.afterClosed().subscribe(async (userId: number | undefined) => {
      if (!userId) return;
      try {
        await firstValueFrom(this.taskService.addShare(current.id, userId));
        this.snackBar.open('共有ユーザーを追加しました', '閉じる', { duration: 3000 });
        await this.loadTask(current.id);
      } catch (err) {
        const message = err instanceof HttpErrorResponse && err.status === 409
          ? 'このユーザーはすでに共有されています'
          : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  async removeShare(userId: number): Promise<void> {
    const current = this.task();
    if (!current) return;

    try {
      await firstValueFrom(this.taskService.removeShare(current.id, userId));
      this.snackBar.open('共有ユーザーを削除しました', '閉じる', { duration: 3000 });
      await this.loadTask(current.id);
    } catch (err) {
      const message = err instanceof HttpErrorResponse && err.status === 403
        ? '作成者を共有ユーザーから削除することはできません'
        : 'エラーが発生しました。管理者にご連絡ください';
      this.snackBar.open(message, '閉じる', { duration: 5000 });
    }
  }

  async removeAssignee(userId: number): Promise<void> {
    const current = this.task();
    if (!current) return;

    try {
      await firstValueFrom(this.taskService.removeAssignee(current.id, userId));
      this.snackBar.open('担当者を外しました', '閉じる', { duration: 3000 });
      await this.loadTask(current.id);
    } catch {
      this.snackBar.open('エラーが発生しました。管理者にご連絡ください', '閉じる', { duration: 5000 });
    }
  }

  async deleteTask(): Promise<void> {
    const current = this.task();
    if (!current) return;

    if (!confirm(`「${current.title}」を削除しますか？`)) return;

    try {
      await firstValueFrom(this.taskService.delete(current.id));
      this.snackBar.open('タスクを削除しました', '閉じる', { duration: 3000 });
      void this.router.navigate(['/tasks']);
    } catch {
      this.snackBar.open('エラーが発生しました。管理者にご連絡ください', '閉じる', { duration: 5000 });
    }
  }

  navigateToSubTask(id: number): void {
    void this.router.navigate(['/tasks', id]);
  }

  navigateBack(): void {
    void this.router.navigate(['/tasks']);
  }

  getPriorityLabel(p: TaskPriority): string {
    switch (p) {
      case TaskPriority.High: return '高';
      case TaskPriority.Medium: return '中';
      case TaskPriority.Low: return '低';
    }
  }
}
