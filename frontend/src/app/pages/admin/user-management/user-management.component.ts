import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CreateUserRequest, UpdateUserRequest, User, UserRole } from '../../../models/user.model';
import { UserService } from '../../../services/user.service';
import { UserDialogComponent } from './user-dialog.component';

@Component({
  selector: 'app-user-management',
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
    MatSlideToggleModule,
    MatCardModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss',
})
export class UserManagementComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly users = signal<User[]>([]);
  readonly isLoading = signal(false);
  readonly UserRole = UserRole;

  readonly displayedColumns = ['loginId', 'displayName', 'role', 'isActive', 'actions'];

  ngOnInit(): void {
    void this.loadUsers();
  }

  async loadUsers(): Promise<void> {
    this.isLoading.set(true);
    try {
      const users = await firstValueFrom(this.userService.getAll());
      this.users.set(users);
    } catch {
      this.snackBar.open('ユーザー一覧の取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(UserDialogComponent, {
      width: '480px',
      data: { mode: 'create' },
    });

    dialogRef.afterClosed().subscribe(async (result: CreateUserRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.userService.create(result));
        this.snackBar.open('ユーザーを作成しました', '閉じる', { duration: 3000 });
        await this.loadUsers();
      } catch (error: unknown) {
        const message =
          error instanceof HttpErrorResponse && error.status === 400
            ? error.error?.error ?? '入力内容を確認してください'
            : 'エラーが発生しました。管理者にご連絡ください';
        this.snackBar.open(message, '閉じる', { duration: 5000 });
      }
    });
  }

  openEditDialog(user: User): void {
    const dialogRef = this.dialog.open(UserDialogComponent, {
      width: '480px',
      data: { mode: 'edit', user },
    });

    dialogRef.afterClosed().subscribe(async (result: UpdateUserRequest | undefined) => {
      if (!result) return;
      try {
        await firstValueFrom(this.userService.update(user.id, result));
        this.snackBar.open('ユーザー情報を更新しました', '閉じる', { duration: 3000 });
        await this.loadUsers();
      } catch {
        this.snackBar.open('エラーが発生しました。管理者にご連絡ください', '閉じる', { duration: 5000 });
      }
    });
  }

  async toggleActive(user: User): Promise<void> {
    try {
      await firstValueFrom(
        this.userService.update(user.id, {
          displayName: user.displayName,
          role: user.role,
          isActive: !user.isActive,
        })
      );
      this.snackBar.open(
        user.isActive ? 'アカウントを無効化しました' : 'アカウントを有効化しました',
        '閉じる',
        { duration: 3000 }
      );
      await this.loadUsers();
    } catch {
      this.snackBar.open('エラーが発生しました。管理者にご連絡ください', '閉じる', { duration: 5000 });
    }
  }

  getRoleLabel(role: UserRole): string {
    return role === UserRole.Admin ? '管理者' : '一般ユーザー';
  }
}
