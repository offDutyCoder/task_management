import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { DatePipe } from '@angular/common';
import { CreateTaskRequest, TaskFilterQuery, TaskListItem, TaskPriority } from '../../../models/task.model';
import { TaskStatus } from '../../../models/task-status.model';
import { Label } from '../../../models/label.model';
import { User } from '../../../models/user.model';
import { TaskService } from '../../../services/task.service';
import { StatusService } from '../../../services/status.service';
import { LabelService } from '../../../services/label.service';
import { UserService } from '../../../services/user.service';
import { TaskFormComponent, TaskFormDialogData } from '../task-form/task-form.component';
import { LabelRequestDialogComponent } from '../label-request-dialog/label-request-dialog.component';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatPaginatorModule,
    MatChipsModule,
    MatBadgeModule,
    MatCheckboxModule,
    DatePipe,
  ],
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.scss',
})
export class TaskListComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly statusService = inject(StatusService);
  private readonly labelService = inject(LabelService);
  private readonly userService = inject(UserService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly TaskPriority = TaskPriority;
  readonly tasks = signal<TaskListItem[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly statuses = signal<TaskStatus[]>([]);
  readonly labels = signal<Label[]>([]);
  readonly users = signal<User[]>([]);

  readonly displayedColumns = ['title', 'status', 'priority', 'dueDate', 'assignees', 'labels', 'subTaskCount', 'actions'];

  readonly filterForm = this.fb.group({
    statusId: [null as number | null],
    assigneeId: [null as number | null],
    labelId: [null as number | null],
    priority: [null as TaskPriority | null],
    search: [''],
    dueAfter: [null as string | null],
    dueBefore: [null as string | null],
    isRecurring: [null as boolean | null],
  });

  page = 1;
  pageSize = 20;

  ngOnInit(): void {
    void this.loadReferenceData();
    void this.loadTasks();
  }

  private async loadReferenceData(): Promise<void> {
    const [statuses, labels, users] = await Promise.all([
      firstValueFrom(this.statusService.getAll()),
      firstValueFrom(this.labelService.getAll()),
      firstValueFrom(this.userService.getAll()),
    ]);
    this.statuses.set(statuses.filter(s => s.isActive));
    this.labels.set(labels);
    this.users.set(users.filter(u => u.isActive));
  }

  async loadTasks(): Promise<void> {
    this.isLoading.set(true);
    try {
      const v = this.filterForm.getRawValue();
      const filter: TaskFilterQuery = {
        statusId: v.statusId ?? undefined,
        assigneeId: v.assigneeId ?? undefined,
        labelId: v.labelId ?? undefined,
        priority: v.priority ?? undefined,
        search: v.search || undefined,
        dueAfter: v.dueAfter ?? undefined,
        dueBefore: v.dueBefore ?? undefined,
        isRecurring: v.isRecurring ?? undefined,
        page: this.page,
        pageSize: this.pageSize,
      };
      const result = await firstValueFrom(this.taskService.getAll(filter));
      this.tasks.set(result.items);
      this.totalCount.set(result.totalCount);
    } catch {
      this.snackBar.open('タスク一覧の取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  onFilterChange(): void {
    this.page = 1;
    void this.loadTasks();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    void this.loadTasks();
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(TaskFormComponent, {
      width: '560px',
      data: { mode: 'create' } satisfies TaskFormDialogData,
    });

    dialogRef.afterClosed().subscribe(async (result: CreateTaskRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.taskService.create(result));
        this.snackBar.open('タスクを作成しました', '閉じる', { duration: 3000 });
        await this.loadTasks();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? error.error?.error ?? '入力内容を確認してください'
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  navigateToDetail(id: number): void {
    void this.router.navigate(['/tasks', id]);
  }

  getPriorityLabel(p: TaskPriority): string {
    switch (p) {
      case TaskPriority.High: return '高';
      case TaskPriority.Medium: return '中';
      case TaskPriority.Low: return '低';
    }
  }

  getPriorityColor(p: TaskPriority): string {
    switch (p) {
      case TaskPriority.High: return 'warn';
      case TaskPriority.Medium: return 'accent';
      case TaskPriority.Low: return 'primary';
    }
  }

  getAssigneeNames(task: TaskListItem): string {
    return task.assignees.map(a => a.displayName).join(', ') || '—';
  }

  openLabelRequestDialog(): void {
    this.dialog.open(LabelRequestDialogComponent, { width: '440px' });
  }
}
