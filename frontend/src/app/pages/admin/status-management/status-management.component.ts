import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CreateStatusRequest, TaskStatus, UpdateStatusRequest } from '../../../models/task-status.model';
import { StatusService } from '../../../services/status.service';
import { StatusDialogComponent } from './status-dialog.component';

@Component({
  selector: 'app-status-management',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './status-management.component.html',
  styleUrl: './status-management.component.scss',
})
export class StatusManagementComponent implements OnInit {
  private readonly statusService = inject(StatusService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly statuses = signal<TaskStatus[]>([]);
  readonly isLoading = signal(false);

  readonly displayedColumns = ['color', 'name', 'displayOrder', 'isActive', 'actions'];

  ngOnInit(): void {
    void this.loadStatuses();
  }

  async loadStatuses(): Promise<void> {
    this.isLoading.set(true);
    try {
      const statuses = await firstValueFrom(this.statusService.getAll());
      this.statuses.set(statuses);
    } catch {
      this.snackBar.open('ステータス一覧の取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(StatusDialogComponent, {
      width: '480px',
      data: { mode: 'create' },
    });

    dialogRef.afterClosed().subscribe(async (result: CreateStatusRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.statusService.create(result));
        this.snackBar.open('ステータスを作成しました', '閉じる', { duration: 3000 });
        await this.loadStatuses();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? error.error?.error ?? '入力内容を確認してください'
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  openEditDialog(status: TaskStatus): void {
    const dialogRef = this.dialog.open(StatusDialogComponent, {
      width: '480px',
      data: { mode: 'edit', status },
    });

    dialogRef.afterClosed().subscribe(async (result: UpdateStatusRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.statusService.update(status.id, result));
        this.snackBar.open('ステータスを更新しました', '閉じる', { duration: 3000 });
        await this.loadStatuses();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? error.error?.error ?? '入力内容を確認してください'
            : error instanceof HttpErrorResponse && error.status === 404
              ? 'ステータスが見つかりません'
              : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  async deleteStatus(status: TaskStatus): Promise<void> {
    try {
      await firstValueFrom(this.statusService.delete(status.id));
      this.snackBar.open('ステータスを削除しました', '閉じる', { duration: 3000 });
      await this.loadStatuses();
    } catch (error: unknown) {
      const message =
        error instanceof HttpErrorResponse && error.status === 409
          ? error.error?.error ?? '使用中のため削除できません'
          : 'エラーが発生しました。管理者にご連絡ください';
      this.snackBar.open(message, '閉じる', { duration: 5000 });
    }
  }
}
