import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { DatePipe } from '@angular/common';
import { AssigneeGroup, CreateTaskRequest, TaskListItem, TaskPriority } from '../../models/task.model';
import { DashboardService } from '../../services/dashboard.service';
import { TaskService } from '../../services/task.service';
import { TaskFormComponent, TaskFormDialogData } from '../tasks/task-form/task-form.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatDialogModule,
    DatePipe,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly taskService = inject(TaskService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly TaskPriority = TaskPriority;
  readonly myTasks = signal<TaskListItem[]>([]);
  readonly teamTaskGroups = signal<AssigneeGroup[]>([]);
  readonly isLoading = signal(false);
  readonly showRecurring = signal(false);

  readonly displayedColumns = ['title', 'status', 'priority', 'dueDate', 'actions'];

  ngOnInit(): void {
    void this.loadDashboard();
  }

  async loadDashboard(): Promise<void> {
    this.isLoading.set(true);
    try {
      const data = await firstValueFrom(this.dashboardService.getDashboard(this.showRecurring()));
      this.myTasks.set(data.myTasks);
      this.teamTaskGroups.set(data.teamTaskGroups);
    } catch {
      this.snackBar.open('ダッシュボードの取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  onToggleRecurring(): void {
    this.showRecurring.update(v => !v);
    void this.loadDashboard();
  }

  isOverdue(dueDate: string | null): boolean {
    if (!dueDate) return false;
    const today = new Date().toISOString().slice(0, 10);
    return dueDate.slice(0, 10) < today;
  }

  getPriorityLabel(p: TaskPriority): string {
    switch (p) {
      case TaskPriority.High: return '高';
      case TaskPriority.Medium: return '中';
      case TaskPriority.Low: return '低';
    }
  }

  getGroupIcon(group: AssigneeGroup): string {
    return group.assigneeType === 'team' ? 'group' : 'person';
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
        void this.loadDashboard();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? (error.error as { error?: string })?.error ?? '入力内容を確認してください'
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  navigateToDetail(id: number): void {
    void this.router.navigate(['/tasks', id]);
  }
}
