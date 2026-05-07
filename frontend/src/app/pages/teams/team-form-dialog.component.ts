import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TeamDto } from '../../models/team.model';
import { User } from '../../models/user.model';
import { TeamService } from '../../services/team.service';
import { UserService } from '../../services/user.service';

export interface TeamFormDialogData {
  mode: 'create' | 'edit';
  team?: TeamDto;
}

@Component({
  selector: 'app-team-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatSnackBarModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'チームを作成' : 'チームを編集' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="team-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>チーム名</mat-label>
          <input matInput formControlName="name" placeholder="例: 開発チーム" />
          @if (form.get('name')?.hasError('required')) {
            <mat-error>チーム名は必須です</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>メンバー</mat-label>
          <mat-select formControlName="memberUserIds" multiple>
            @for (user of users(); track user.id) {
              <mat-option [value]="user.id">{{ user.displayName }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>キャンセル</button>
      <button mat-raised-button color="primary" (click)="onSave()" [disabled]="form.invalid">
        {{ data.mode === 'create' ? '作成' : '保存' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: ['.team-form { display: flex; flex-direction: column; gap: 8px; min-width: 320px; } .full-width { width: 100%; }'],
})
export class TeamFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<TeamFormDialogComponent>);
  private readonly teamService = inject(TeamService);
  private readonly userService = inject(UserService);
  private readonly snackBar = inject(MatSnackBar);

  readonly users = signal<User[]>([]);

  readonly form = this.fb.group({
    name: [this.data.team?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    memberUserIds: [this.data.team?.members.map(m => m.userId) ?? [] as number[]],
  });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: TeamFormDialogData) {}

  ngOnInit(): void {
    void this.loadUsers();
  }

  private async loadUsers(): Promise<void> {
    const users = await firstValueFrom(this.userService.getAll());
    this.users.set(users.filter(u => u.isActive));
  }

  async onSave(): Promise<void> {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    try {
      let result: TeamDto;
      if (this.data.mode === 'create') {
        result = await firstValueFrom(this.teamService.create({
          name: value.name ?? '',
          memberUserIds: value.memberUserIds ?? [],
        }));
      } else {
        result = await firstValueFrom(this.teamService.update(this.data.team!.id, {
          name: value.name ?? '',
          memberUserIds: value.memberUserIds ?? [],
        }));
      }
      this.dialogRef.close(result);
    } catch (error: unknown) {
      const message =
        error instanceof HttpErrorResponse && error.status === 400
          ? (error.error as { error?: string })?.error ?? '保存に失敗しました'
          : '保存に失敗しました';
      this.snackBar.open(message, '閉じる', { duration: 5000 });
    }
  }
}
