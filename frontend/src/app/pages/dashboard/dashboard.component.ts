import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DatePipe } from '@angular/common';
import { TaskListItem, TaskPriority } from '../../models/task.model';
import { DashboardService } from '../../services/dashboard.service';

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
    DatePipe,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly TaskPriority = TaskPriority;
  readonly myTasks = signal<TaskListItem[]>([]);
  readonly teamTasks = signal<TaskListItem[]>([]);
  readonly isLoading = signal(false);
  readonly showRecurring = signal(false);

  readonly displayedColumns = ['title', 'status', 'priority', 'dueDate', 'actions'];
  readonly teamDisplayedColumns = ['title', 'status', 'priority', 'assignees', 'dueDate', 'actions'];

  ngOnInit(): void {
    void this.loadDashboard();
  }

  async loadDashboard(): Promise<void> {
    this.isLoading.set(true);
    try {
      const data = await firstValueFrom(this.dashboardService.getDashboard(this.showRecurring()));
      this.myTasks.set(data.myTasks);
      this.teamTasks.set(data.teamTasks);
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

  getAssigneeNames(task: TaskListItem): string {
    return task.assignees.map(a => a.displayName).join(', ') || '—';
  }

  navigateToDetail(id: number): void {
    void this.router.navigate(['/tasks', id]);
  }
}
