import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  MatDialogModule,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { CreateStatusRequest, TaskStatus, UpdateStatusRequest } from '../../../models/task-status.model';

interface DialogData {
  mode: 'create' | 'edit';
  status?: TaskStatus;
}

@Component({
  selector: 'app-status-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'ステータス作成' : 'ステータス編集' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>名前</mat-label>
          <input matInput formControlName="name" data-testid="dialog-name" />
          @if (form.controls['name'].hasError('required')) {
            <mat-error>名前は必須です</mat-error>
          }
          @if (form.controls['name'].hasError('maxlength')) {
            <mat-error>50文字以内で入力してください</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>色 (HEXカラーコード)</mat-label>
          <input matInput formControlName="color" placeholder="#2196F3" data-testid="dialog-color" />
          @if (form.controls['color'].hasError('required')) {
            <mat-error>色は必須です</mat-error>
          }
          @if (form.controls['color'].hasError('pattern')) {
            <mat-error>#から始まる6桁のHEXコードを入力してください</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>表示順</mat-label>
          <input matInput type="number" formControlName="displayOrder" min="1" data-testid="dialog-display-order" />
          @if (form.controls['displayOrder'].hasError('required')) {
            <mat-error>表示順は必須です</mat-error>
          }
          @if (form.controls['displayOrder'].hasError('min')) {
            <mat-error>1以上の値を入力してください</mat-error>
          }
        </mat-form-field>

        @if (data.mode === 'edit') {
          <mat-slide-toggle formControlName="isActive" data-testid="dialog-is-active">
            有効
          </mat-slide-toggle>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>キャンセル</button>
      <button
        mat-raised-button
        color="primary"
        [disabled]="form.invalid"
        (click)="onSave()"
        data-testid="dialog-save-button"
      >
        保存
      </button>
    </mat-dialog-actions>
  `,
  styles: ['.full-width { width: 100%; margin-bottom: 8px; display: block; }'],
})
export class StatusDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<StatusDialogComponent>);

  readonly form = this.fb.group({
    name: [this.data.status?.name ?? '', [Validators.required, Validators.maxLength(50)]],
    color: [
      this.data.status?.color ?? '#',
      [Validators.required, Validators.pattern(/^#[0-9A-Fa-f]{6}$/)],
    ],
    displayOrder: [this.data.status?.displayOrder ?? 1, [Validators.required, Validators.min(1)]],
    isActive: [this.data.status?.isActive ?? true],
  });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: DialogData) {}

  onSave(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();

    if (this.data.mode === 'create') {
      const request: CreateStatusRequest = {
        name: value.name ?? '',
        color: value.color ?? '',
        displayOrder: value.displayOrder ?? 1,
      };
      this.dialogRef.close(request);
    } else {
      const request: UpdateStatusRequest = {
        name: value.name ?? '',
        color: value.color ?? '',
        displayOrder: value.displayOrder ?? 1,
        isActive: value.isActive ?? true,
      };
      this.dialogRef.close(request);
    }
  }
}
