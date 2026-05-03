import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Label } from '../../../models/label.model';
import { LabelService } from '../../../services/label.service';
import { LabelRequestService } from '../../../services/label-request.service';

@Component({
  selector: 'app-label-request-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
  ],
  template: `
    <h2 mat-dialog-title>ラベル追加リクエスト</h2>
    <mat-dialog-content>
      <p class="hint">必要なラベルが見つからない場合は、管理者にリクエストを送信してください。</p>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>希望するラベル名</mat-label>
          <input matInput formControlName="requestedName" />
          @if (form.controls['requestedName'].hasError('required')) {
            <mat-error>ラベル名は必須です</mat-error>
          }
          @if (form.controls['requestedName'].hasError('maxlength')) {
            <mat-error>50文字以内で入力してください</mat-error>
          }
          @if (isDuplicateName()) {
            <mat-hint class="warn-hint">このラベル名は既に存在します</mat-hint>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>理由（任意）</mat-label>
          <textarea matInput formControlName="reason" rows="3" placeholder="このラベルが必要な理由を入力してください"></textarea>
          @if (form.controls['reason'].hasError('maxlength')) {
            <mat-error>200文字以内で入力してください</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>キャンセル</button>
      <button
        mat-raised-button
        color="primary"
        [disabled]="form.invalid || isSending()"
        (click)="onSend()"
      >
        送信
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    '.dialog-form { display: flex; flex-direction: column; min-width: 360px; padding-top: 8px; }',
    '.full-width { width: 100%; margin-bottom: 8px; }',
    '.hint { color: #757575; font-size: 13px; margin-bottom: 8px; }',
    '.warn-hint { color: #e65100 !important; }',
  ],
})
export class LabelRequestDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<LabelRequestDialogComponent>);
  private readonly labelService = inject(LabelService);
  private readonly requestService = inject(LabelRequestService);
  private readonly snackBar = inject(MatSnackBar);

  private existingLabels: Label[] = [];
  readonly isDuplicateName = signal(false);
  readonly isSending = signal(false);

  readonly form = this.fb.group({
    requestedName: ['', [Validators.required, Validators.maxLength(50)]],
    reason: ['', Validators.maxLength(200)],
  });

  ngOnInit(): void {
    void this.loadLabels();
    this.form.controls['requestedName'].valueChanges.subscribe(name => {
      const trimmed = (name ?? '').trim().toLowerCase();
      const dup = this.existingLabels.some(l => l.name.toLowerCase() === trimmed);
      this.isDuplicateName.set(dup);
    });
  }

  private async loadLabels(): Promise<void> {
    try {
      this.existingLabels = await firstValueFrom(this.labelService.getAll());
    } catch {
      // silent fail – duplicate check is a best-effort UX hint
    }
  }

  async onSend(): Promise<void> {
    if (this.form.invalid || this.isSending()) return;
    this.isSending.set(true);
    const v = this.form.getRawValue();
    try {
      await firstValueFrom(
        this.requestService.create({
          requestedName: v.requestedName ?? '',
          reason: v.reason ?? undefined,
        })
      );
      this.snackBar.open('リクエストを送信しました', '閉じる', { duration: 3000 });
      this.dialogRef.close(true);
    } catch (error: unknown) {
      const message =
        error instanceof HttpErrorResponse && error.status === 400
          ? (error.error?.error ?? '入力内容を確認してください')
          : 'エラーが発生しました。管理者にご連絡ください';
      this.snackBar.open(message, '閉じる', { duration: 5000 });
    } finally {
      this.isSending.set(false);
    }
  }
}
