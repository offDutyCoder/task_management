import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { DatePipe } from '@angular/common';
import { LabelRequest, LabelRequestStatus } from '../../../models/label-request.model';
import { LabelRequestService } from '../../../services/label-request.service';

@Component({
  selector: 'app-approve-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>ラベルリクエスト承認</h2>
    <mat-dialog-content>
      <p>「{{ data.requestedName }}」を承認してラベルを作成します。</p>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>カラー (HEXカラーコード)</mat-label>
          <input matInput formControlName="color" placeholder="#4CAF50" />
          @if (form.controls['color'].hasError('required')) {
            <mat-error>カラーコードは必須です</mat-error>
          }
          @if (form.controls['color'].hasError('pattern')) {
            <mat-error>#から始まる6桁のHEXコードを入力してください</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>キャンセル</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onApprove()">承認</button>
    </mat-dialog-actions>
  `,
  styles: ['.full-width { width: 100%; margin-top: 8px; display: block; }'],
})
export class ApproveDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ApproveDialogComponent>);

  readonly form = this.fb.group({
    color: ['#', [Validators.required, Validators.pattern(/^#[0-9A-Fa-f]{6}$/)]],
  });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: { requestedName: string }) {}

  onApprove(): void {
    if (this.form.invalid) return;
    this.dialogRef.close(this.form.getRawValue().color);
  }
}

@Component({
  selector: 'app-label-request-management',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule,
    DatePipe,
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>ラベルリクエスト管理</mat-card-title>
        <span class="spacer"></span>
        <mat-form-field appearance="outline" class="filter-select">
          <mat-label>ステータス</mat-label>
          <mat-select [value]="statusFilter()" (selectionChange)="onFilterChange($event.value)">
            <mat-option value="">全件</mat-option>
            <mat-option value="Pending">Pending</mat-option>
            <mat-option value="Approved">Approved</mat-option>
            <mat-option value="Rejected">Rejected</mat-option>
          </mat-select>
        </mat-form-field>
      </mat-card-header>
      <mat-card-content>
        @if (isLoading()) {
          <div class="spinner-wrap"><mat-spinner diameter="40"></mat-spinner></div>
        } @else if (requests().length === 0) {
          <p class="empty-message">リクエストはありません</p>
        } @else {
          <table mat-table [dataSource]="requests()">
            <ng-container matColumnDef="requestedName">
              <th mat-header-cell *matHeaderCellDef>希望ラベル名</th>
              <td mat-cell *matCellDef="let r">{{ r.requestedName }}</td>
            </ng-container>
            <ng-container matColumnDef="reason">
              <th mat-header-cell *matHeaderCellDef>理由</th>
              <td mat-cell *matCellDef="let r">{{ r.reason ?? '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="requestedBy">
              <th mat-header-cell *matHeaderCellDef>リクエスト者</th>
              <td mat-cell *matCellDef="let r">{{ r.requestedByDisplayName }}</td>
            </ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>ステータス</th>
              <td mat-cell *matCellDef="let r">
                <span [class]="'status-badge status-' + r.status.toLowerCase()">{{ r.status }}</span>
              </td>
            </ng-container>
            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef>日時</th>
              <td mat-cell *matCellDef="let r">{{ r.createdAt | date:'yyyy/MM/dd HH:mm' }}</td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let r">
                @if (r.status === 'Pending') {
                  <button mat-icon-button color="primary" (click)="openApproveDialog(r)" aria-label="承認">
                    <mat-icon>check_circle</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" (click)="reject(r)" aria-label="却下">
                    <mat-icon>cancel</mat-icon>
                  </button>
                }
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    '.spacer { flex: 1; }',
    'mat-card-header { display: flex; align-items: center; margin-bottom: 8px; }',
    '.filter-select { width: 160px; }',
    '.spinner-wrap { display: flex; justify-content: center; padding: 32px; }',
    '.empty-message { text-align: center; color: #757575; padding: 32px; }',
    '.status-badge { padding: 2px 10px; border-radius: 12px; font-size: 12px; font-weight: 500; }',
    '.status-pending { background: #fff3e0; color: #e65100; }',
    '.status-approved { background: #e8f5e9; color: #2e7d32; }',
    '.status-rejected { background: #ffebee; color: #c62828; }',
  ],
})
export class LabelRequestManagementComponent implements OnInit {
  private readonly requestService = inject(LabelRequestService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly requests = signal<LabelRequest[]>([]);
  readonly isLoading = signal(false);
  readonly statusFilter = signal<LabelRequestStatus | ''>('Pending');
  readonly displayedColumns = ['requestedName', 'reason', 'requestedBy', 'status', 'createdAt', 'actions'];

  ngOnInit(): void {
    void this.loadRequests();
  }

  async loadRequests(): Promise<void> {
    this.isLoading.set(true);
    try {
      const filter = this.statusFilter();
      const data = await firstValueFrom(
        this.requestService.getAll(filter || undefined)
      );
      this.requests.set(data);
    } catch {
      this.snackBar.open('リクエスト一覧の取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  onFilterChange(value: LabelRequestStatus | ''): void {
    this.statusFilter.set(value);
    void this.loadRequests();
  }

  openApproveDialog(request: LabelRequest): void {
    const dialogRef = this.dialog.open(ApproveDialogComponent, {
      width: '400px',
      data: { requestedName: request.requestedName },
    });
    dialogRef.afterClosed().subscribe(async (color: string | undefined) => {
      if (!color) return;
      try {
        await firstValueFrom(this.requestService.updateStatus(request.id, { status: 'Approved', color }));
        this.snackBar.open('リクエストを承認しラベルを作成しました', '閉じる', { duration: 3000 });
        await this.loadRequests();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? (error.error?.error ?? '入力内容を確認してください')
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  reject(request: LabelRequest): void {
    const snackBarRef = this.snackBar.open(
      `「${request.requestedName}」のリクエストを却下しますか？`,
      '却下する',
      { duration: 5000 }
    );
    snackBarRef.onAction().subscribe(async () => {
      try {
        await firstValueFrom(this.requestService.updateStatus(request.id, { status: 'Rejected' }));
        this.snackBar.open('リクエストを却下しました', '閉じる', { duration: 3000 });
        await this.loadRequests();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? (error.error?.error ?? '既に処理済みのリクエストです')
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }
}
