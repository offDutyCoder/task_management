import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  MatDialogModule,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { CreateUserRequest, UpdateUserRequest, User, UserRole } from '../../../models/user.model';

interface DialogData {
  mode: 'create' | 'edit';
  user?: User;
}

@Component({
  selector: 'app-user-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'ユーザー作成' : 'ユーザー編集' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        @if (data.mode === 'create') {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>ログインID</mat-label>
            <input matInput formControlName="loginId" data-testid="dialog-login-id" />
            @if (form.controls['loginId'].hasError('required')) {
              <mat-error>ログインIDは必須です</mat-error>
            }
            @if (form.controls['loginId'].hasError('minlength')) {
              <mat-error>3文字以上入力してください</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>パスワード</mat-label>
            <input matInput type="password" formControlName="password" data-testid="dialog-password" />
            @if (form.controls['password'].hasError('required')) {
              <mat-error>パスワードは必須です</mat-error>
            }
            @if (form.controls['password'].hasError('minlength')) {
              <mat-error>8文字以上入力してください</mat-error>
            }
          </mat-form-field>
        }

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>表示名</mat-label>
          <input matInput formControlName="displayName" data-testid="dialog-display-name" />
          @if (form.controls['displayName'].hasError('required')) {
            <mat-error>表示名は必須です</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>ロール</mat-label>
          <mat-select formControlName="role" data-testid="dialog-role">
            <mat-option [value]="UserRole.Member">一般ユーザー</mat-option>
            <mat-option [value]="UserRole.Admin">管理者</mat-option>
          </mat-select>
        </mat-form-field>
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
export class UserDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<UserDialogComponent>);

  readonly UserRole = UserRole;

  readonly form = this.fb.group({
    loginId: [
      '',
      this.data.mode === 'create' ? [Validators.required, Validators.minLength(3)] : [],
    ],
    password: [
      '',
      this.data.mode === 'create' ? [Validators.required, Validators.minLength(8)] : [],
    ],
    displayName: [this.data.user?.displayName ?? '', [Validators.required]],
    role: [this.data.user?.role ?? UserRole.Member, [Validators.required]],
  });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: DialogData) {}

  onSave(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();

    if (this.data.mode === 'create') {
      const request: CreateUserRequest = {
        loginId: value.loginId ?? '',
        password: value.password ?? '',
        displayName: value.displayName ?? '',
        role: value.role ?? UserRole.Member,
      };
      this.dialogRef.close(request);
    } else {
      const request: UpdateUserRequest = {
        displayName: value.displayName ?? '',
        role: value.role ?? UserRole.Member,
        isActive: this.data.user?.isActive ?? true,
      };
      this.dialogRef.close(request);
    }
  }
}
