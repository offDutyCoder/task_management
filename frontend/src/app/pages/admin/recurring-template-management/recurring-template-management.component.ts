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
import { MatChipsModule } from '@angular/material/chips';
import { RecurringTemplate, CreateRecurringTemplateRequest, UpdateRecurringTemplateRequest } from '../../../models/recurring-template.model';
import { RecurringTemplateService } from '../../../services/recurring-template.service';
import { StatusService } from '../../../services/status.service';
import { UserService } from '../../../services/user.service';
import { LabelService } from '../../../services/label.service';
import { TaskStatus } from '../../../models/task-status.model';
import { User } from '../../../models/user.model';
import { Label } from '../../../models/label.model';
import { RecurringTemplateDialogComponent, RecurringTemplateDialogData } from './recurring-template-dialog.component';

@Component({
  selector: 'app-recurring-template-management',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatChipsModule,
  ],
  templateUrl: './recurring-template-management.component.html',
  styleUrl: './recurring-template-management.component.scss',
})
export class RecurringTemplateManagementComponent implements OnInit {
  private readonly templateService = inject(RecurringTemplateService);
  private readonly statusService = inject(StatusService);
  private readonly userService = inject(UserService);
  private readonly labelService = inject(LabelService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly templates = signal<RecurringTemplate[]>([]);
  readonly isLoading = signal(false);
  readonly statuses = signal<TaskStatus[]>([]);
  readonly users = signal<User[]>([]);
  readonly labels = signal<Label[]>([]);

  readonly displayedColumns = ['title', 'frequency', 'generationTime', 'excludeWeekends', 'assignees', 'isActive', 'actions'];

  ngOnInit(): void {
    void this.loadAll();
  }

  async loadAll(): Promise<void> {
    this.isLoading.set(true);
    try {
      const [templates, statuses, users, labels] = await Promise.all([
        firstValueFrom(this.templateService.getAll()),
        firstValueFrom(this.statusService.getAll()),
        firstValueFrom(this.userService.getAll()),
        firstValueFrom(this.labelService.getAll()),
      ]);
      this.templates.set(templates);
      this.statuses.set(statuses);
      this.users.set(users);
      this.labels.set(labels);
    } catch (error: unknown) {
      const message =
        error instanceof HttpErrorResponse && error.status === 403
          ? '管理者権限が必要です'
          : 'データの取得に失敗しました';
      this.snackBar.open(message, '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(RecurringTemplateDialogComponent, {
      width: '600px',
      data: { mode: 'create', statuses: this.statuses(), users: this.users(), labels: this.labels() } satisfies RecurringTemplateDialogData,
    });

    dialogRef.afterClosed().subscribe(async (result: CreateRecurringTemplateRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.templateService.create(result));
        this.snackBar.open('定期タスクテンプレートを作成しました', '閉じる', { duration: 3000 });
        await this.loadAll();
      } catch (error: unknown) {
        const message = this.extractErrorMessage(error, 400, '入力内容を確認してください');
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  openEditDialog(template: RecurringTemplate): void {
    const dialogRef = this.dialog.open(RecurringTemplateDialogComponent, {
      width: '600px',
      data: { mode: 'edit', template, statuses: this.statuses(), users: this.users(), labels: this.labels() } satisfies RecurringTemplateDialogData,
    });

    dialogRef.afterClosed().subscribe(async (result: UpdateRecurringTemplateRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.templateService.update(template.id, result));
        this.snackBar.open('定期タスクテンプレートを更新しました', '閉じる', { duration: 3000 });
        await this.loadAll();
      } catch (error: unknown) {
        const message = this.extractErrorMessage(error, 400, '入力内容を確認してください');
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  async deactivateTemplate(template: RecurringTemplate): Promise<void> {
    try {
      await firstValueFrom(this.templateService.deactivate(template.id));
      this.snackBar.open('定期タスクテンプレートを無効化しました', '閉じる', { duration: 3000 });
      await this.loadAll();
    } catch (error: unknown) {
      const message = this.extractErrorMessage(error, 404, 'テンプレートが見つかりません');
      this.snackBar.open(message, '閉じる', { duration: 5000 });
    }
  }

  getFrequencyLabel(frequency: string): string {
    switch (frequency) {
      case 'Daily': return '毎日';
      case 'Weekly': return '毎週';
      case 'Monthly': return '毎月';
      default: return frequency;
    }
  }

  getAssigneeNames(template: RecurringTemplate): string {
    return template.assignees.map(a => a.displayName).join(', ');
  }

  private extractErrorMessage(error: unknown, status: number, fallback: string): string {
    if (error instanceof HttpErrorResponse && error.status === status) {
      return error.error?.error ?? fallback;
    }
    return 'エラーが発生しました。管理者にご連絡ください';
  }
}
