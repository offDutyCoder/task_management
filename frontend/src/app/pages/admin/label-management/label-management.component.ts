import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { Label, CreateLabelRequest, UpdateLabelRequest } from '../../../models/label.model';
import { LabelService } from '../../../services/label.service';

@Component({
  selector: 'app-label-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ mode === 'create' ? 'ラベル作成' : 'ラベル編集' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>ラベル名</mat-label>
          <input matInput formControlName="name" />
          @if (form.controls['name'].hasError('required')) {
            <mat-error>ラベル名は必須です</mat-error>
          }
          @if (form.controls['name'].hasError('maxlength')) {
            <mat-error>50文字以内で入力してください</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>カラー（HEX）</mat-label>
          <input matInput formControlName="color" placeholder="#4CAF50" />
          @if (form.controls['color'].hasError('required')) {
            <mat-error>カラーコードは必須です</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>キャンセル</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onSave()">保存</button>
    </mat-dialog-actions>
  `,
  styles: ['.dialog-form { display: flex; flex-direction: column; min-width: 360px; } .full-width { width: 100%; margin-bottom: 8px; }'],
})
export class LabelDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<LabelDialogComponent>);

  readonly mode: 'create' | 'edit';
  private readonly label?: Label;

  readonly form = this.fb.group({
    name: [this.label?.name ?? '', [Validators.required, Validators.maxLength(50)]],
    color: [this.label?.color ?? '#4CAF50', [Validators.required]],
  });

  constructor() {
    // mode and label are injected via dialog open data — use default for now
    this.mode = 'create';
  }

  onSave(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.dialogRef.close({ name: v.name ?? '', color: v.color ?? '' });
  }
}

@Component({
  selector: 'app-label-management',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>ラベル管理</mat-card-title>
        <span class="spacer"></span>
        <button mat-raised-button color="primary" (click)="openCreateDialog()">
          <mat-icon>add</mat-icon>ラベル追加
        </button>
      </mat-card-header>
      <mat-card-content>
        @if (isLoading()) {
          <div class="spinner-wrap"><mat-spinner diameter="40"></mat-spinner></div>
        } @else {
          <table mat-table [dataSource]="labels()">
            <ng-container matColumnDef="color">
              <th mat-header-cell *matHeaderCellDef>カラー</th>
              <td mat-cell *matCellDef="let l">
                <span class="color-dot" [style.background-color]="l.color"></span>
              </td>
            </ng-container>
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>名前</th>
              <td mat-cell *matCellDef="let l">{{ l.name }}</td>
            </ng-container>
            <ng-container matColumnDef="isActive">
              <th mat-header-cell *matHeaderCellDef>有効</th>
              <td mat-cell *matCellDef="let l">{{ l.isActive ? '有効' : '無効' }}</td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let l">
                <button mat-icon-button (click)="openEditDialog(l)"><mat-icon>edit</mat-icon></button>
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
    'mat-card-header { display: flex; align-items: center; margin-bottom: 16px; }',
    '.color-dot { display: inline-block; width: 20px; height: 20px; border-radius: 50%; }',
    '.spinner-wrap { display: flex; justify-content: center; padding: 32px; }',
  ],
})
export class LabelManagementComponent implements OnInit {
  private readonly labelService = inject(LabelService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly labels = signal<Label[]>([]);
  readonly isLoading = signal(false);
  readonly displayedColumns = ['color', 'name', 'isActive', 'actions'];

  ngOnInit(): void {
    void this.loadLabels();
  }

  async loadLabels(): Promise<void> {
    this.isLoading.set(true);
    try {
      const all = await firstValueFrom(this.labelService.getAll());
      this.labels.set(all);
    } catch {
      this.snackBar.open('ラベル一覧の取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(LabelDialogComponent, { width: '400px' });
    dialogRef.afterClosed().subscribe(async (result: CreateLabelRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.labelService.create(result));
        this.snackBar.open('ラベルを作成しました', '閉じる', { duration: 3000 });
        await this.loadLabels();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? error.error?.error ?? '入力内容を確認してください'
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  openEditDialog(label: Label): void {
    const dialogRef = this.dialog.open(LabelDialogComponent, { width: '400px' });
    dialogRef.afterClosed().subscribe(async (result: { name: string; color: string } | undefined) => {
      if (!result) return;
      const req: UpdateLabelRequest = { name: result.name, color: result.color, isActive: label.isActive };
      try {
        await firstValueFrom(this.labelService.update(label.id, req));
        this.snackBar.open('ラベルを更新しました', '閉じる', { duration: 3000 });
        await this.loadLabels();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? error.error?.error ?? '入力内容を確認してください'
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }
}
