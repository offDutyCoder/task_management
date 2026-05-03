import { Component, Inject, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { User } from '../../../models/user.model';

export interface AssigneeSelectDialogData {
  users: User[];
}

@Component({
  selector: 'app-assignee-select-dialog',
  standalone: true,
  imports: [MatDialogModule, MatListModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>担当者を追加</h2>
    <mat-dialog-content>
      <mat-selection-list [multiple]="false" (selectionChange)="onSelect($event.options[0].value)">
        @for (u of data.users; track u.id) {
          <mat-list-option [value]="u.id">{{ u.displayName }}</mat-list-option>
        }
        @if (data.users.length === 0) {
          <p style="padding: 8px; color: #666;">追加できるユーザーがいません</p>
        }
      </mat-selection-list>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>キャンセル</button>
    </mat-dialog-actions>
  `,
})
export class AssigneeSelectDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<AssigneeSelectDialogComponent>);

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: AssigneeSelectDialogData) {}

  onSelect(userId: number): void {
    this.dialogRef.close(userId);
  }
}
