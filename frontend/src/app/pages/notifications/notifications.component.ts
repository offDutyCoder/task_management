import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NotificationItem, NotificationListResponse, NotificationType } from '../../models/notification.model';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
})
export class NotificationsComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly NotificationType = NotificationType;
  readonly isLoading = signal(false);
  readonly data = signal<NotificationListResponse>({ items: [], unreadCount: 0 });

  ngOnInit(): void {
    void this.loadNotifications();
  }

  async loadNotifications(): Promise<void> {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(this.notificationService.getAll());
      this.data.set(result);
    } catch {
      this.snackBar.open('通知の取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  async markAsRead(notification: NotificationItem): Promise<void> {
    if (notification.isRead) return;
    try {
      await firstValueFrom(this.notificationService.markAsRead(notification.id));
      await this.loadNotifications();
    } catch {
      this.snackBar.open('既読処理に失敗しました', '閉じる', { duration: 3000 });
    }
  }

  async markAllAsRead(): Promise<void> {
    try {
      await firstValueFrom(this.notificationService.markAllAsRead());
      await this.loadNotifications();
      this.snackBar.open('すべて既読にしました', '閉じる', { duration: 2000 });
    } catch {
      this.snackBar.open('既読処理に失敗しました', '閉じる', { duration: 3000 });
    }
  }

  navigateToTask(taskId: number | null): void {
    if (taskId == null) return;
    void this.router.navigate(['/tasks', taskId]);
  }

  getTypeIcon(type: NotificationType): string {
    return type === NotificationType.Assigned ? 'person_add' : 'schedule';
  }

  getTypeLabel(type: NotificationType): string {
    return type === NotificationType.Assigned ? '担当割り当て' : '期限超過';
  }
}
