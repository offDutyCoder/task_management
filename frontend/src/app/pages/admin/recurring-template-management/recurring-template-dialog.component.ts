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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { NgFor } from '@angular/common';
import {
  RecurringTemplate,
  RecurringFrequency,
  CreateRecurringTemplateRequest,
  UpdateRecurringTemplateRequest,
} from '../../../models/recurring-template.model';
import { TaskPriority } from '../../../models/task.model';
import { TaskStatus } from '../../../models/task-status.model';
import { User } from '../../../models/user.model';
import { Label } from '../../../models/label.model';

export interface RecurringTemplateDialogData {
  mode: 'create' | 'edit';
  template?: RecurringTemplate;
  statuses: TaskStatus[];
  users: User[];
  labels: Label[];
}

const WEEKDAY_OPTIONS = [
  { value: 1, label: '月' },
  { value: 2, label: '火' },
  { value: 3, label: '水' },
  { value: 4, label: '木' },
  { value: 5, label: '金' },
  { value: 6, label: '土' },
  { value: 0, label: '日' },
];

@Component({
  selector: 'app-recurring-template-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    NgFor,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatCheckboxModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'テンプレート作成' : 'テンプレート編集' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>タイトル</mat-label>
          <input matInput formControlName="title" data-testid="dialog-title" />
          @if (form.controls['title'].hasError('required')) {
            <mat-error>タイトルは必須です</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>説明</mat-label>
          <textarea matInput formControlName="description" rows="2"></textarea>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>優先度</mat-label>
          <mat-select formControlName="priority">
            <mat-option [value]="0">高</mat-option>
            <mat-option [value]="1">中</mat-option>
            <mat-option [value]="2">低</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>頻度</mat-label>
          <mat-select formControlName="frequency">
            <mat-option [value]="0">毎日</mat-option>
            <mat-option [value]="1">毎週</mat-option>
            <mat-option [value]="2">毎月</mat-option>
          </mat-select>
        </mat-form-field>

        @if (form.controls['frequency'].value === 1) {
          <div class="weekdays-group">
            <label class="weekdays-label">曜日</label>
            <div class="weekdays-checkboxes">
              @for (day of weekdayOptions; track day.value) {
                <mat-checkbox
                  [checked]="isWeekdaySelected(day.value)"
                  (change)="toggleWeekday(day.value, $event.checked)"
                >{{ day.label }}</mat-checkbox>
              }
            </div>
          </div>
        }

        @if (form.controls['frequency'].value === 2) {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>月次日 (1〜31)</mat-label>
            <input matInput type="number" formControlName="dayOfMonth" min="1" max="31" />
          </mat-form-field>
        }

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>生成時刻 (HH:mm)</mat-label>
          <input matInput formControlName="generationTime" placeholder="08:00" data-testid="dialog-generation-time" />
          @if (form.controls['generationTime'].hasError('required')) {
            <mat-error>生成時刻は必須です</mat-error>
          }
          @if (form.controls['generationTime'].hasError('pattern')) {
            <mat-error>HH:mm 形式で入力してください（例: 08:00）</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>初期ステータス</mat-label>
          <mat-select formControlName="defaultStatusId">
            @for (status of data.statuses; track status.id) {
              <mat-option [value]="status.id">{{ status.name }}</mat-option>
            }
          </mat-select>
          @if (form.controls['defaultStatusId'].hasError('required')) {
            <mat-error>初期ステータスは必須です</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>担当者</mat-label>
          <mat-select formControlName="assigneeIds" multiple>
            @for (user of data.users; track user.id) {
              <mat-option [value]="user.id">{{ user.displayName }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>ラベル</mat-label>
          <mat-select formControlName="labelIds" multiple>
            @for (label of data.labels; track label.id) {
              <mat-option [value]="label.id">{{ label.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>共有ユーザー</mat-label>
          <mat-select formControlName="shareUserIds" multiple>
            @for (user of data.users; track user.id) {
              <mat-option [value]="user.id">{{ user.displayName }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <div class="toggle-row">
          <mat-slide-toggle formControlName="excludeWeekends">土日除外</mat-slide-toggle>
        </div>

        @if (data.mode === 'edit') {
          <div class="toggle-row">
            <mat-slide-toggle formControlName="isActive">有効</mat-slide-toggle>
          </div>
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
  styles: [`
    .full-width { width: 100%; margin-bottom: 8px; display: block; }
    .toggle-row { margin-bottom: 12px; }
    .weekdays-group { margin-bottom: 12px; }
    .weekdays-label { font-size: 12px; color: rgba(0,0,0,0.6); display: block; margin-bottom: 4px; }
    .weekdays-checkboxes { display: flex; gap: 8px; flex-wrap: wrap; }
  `],
})
export class RecurringTemplateDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<RecurringTemplateDialogComponent>);

  readonly weekdayOptions = WEEKDAY_OPTIONS;

  private selectedWeekDays: number[] = this.data.template?.weekDays ?? [];

  readonly form = this.fb.group({
    title: [this.data.template?.title ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.data.template?.description ?? ''],
    priority: [this.priorityValue(), Validators.required],
    frequency: [this.frequencyValue(), Validators.required],
    dayOfMonth: [this.data.template?.dayOfMonth ?? null],
    generationTime: [
      this.data.template?.generationTime ?? '08:00',
      [Validators.required, Validators.pattern(/^([01]\d|2[0-3]):[0-5]\d$/)],
    ],
    defaultStatusId: [this.data.template?.defaultStatusId ?? null, Validators.required],
    excludeWeekends: [this.data.template?.excludeWeekends ?? false],
    isActive: [this.data.template?.isActive ?? true],
    assigneeIds: [this.data.template?.assignees.map(a => a.id) ?? []],
    labelIds: [this.data.template?.labels.map(l => l.id) ?? []],
    shareUserIds: [this.data.template?.shareUsers.map(s => s.id) ?? []],
  });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: RecurringTemplateDialogData) {}

  isWeekdaySelected(value: number): boolean {
    return this.selectedWeekDays.includes(value);
  }

  toggleWeekday(value: number, checked: boolean): void {
    if (checked) {
      this.selectedWeekDays = [...this.selectedWeekDays, value];
    } else {
      this.selectedWeekDays = this.selectedWeekDays.filter(d => d !== value);
    }
  }

  onSave(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const frequency = value.frequency as RecurringFrequency;
    const weekDays = frequency === RecurringFrequency.Weekly ? this.selectedWeekDays : null;
    const dayOfMonth = frequency === RecurringFrequency.Monthly ? (value.dayOfMonth ?? null) : null;

    if (this.data.mode === 'create') {
      const request: CreateRecurringTemplateRequest = {
        title: value.title ?? '',
        description: value.description || null,
        priority: (value.priority ?? 1) as TaskPriority,
        frequency,
        weekDays,
        dayOfMonth,
        excludeWeekends: value.excludeWeekends ?? false,
        generationTime: value.generationTime ?? '08:00',
        defaultStatusId: value.defaultStatusId ?? 1,
        assigneeIds: (value.assigneeIds as number[]) ?? [],
        labelIds: (value.labelIds as number[]) ?? [],
        shareUserIds: (value.shareUserIds as number[]) ?? [],
      };
      this.dialogRef.close(request);
    } else {
      const request: UpdateRecurringTemplateRequest = {
        title: value.title ?? '',
        description: value.description || null,
        priority: (value.priority ?? 1) as TaskPriority,
        frequency,
        weekDays,
        dayOfMonth,
        excludeWeekends: value.excludeWeekends ?? false,
        generationTime: value.generationTime ?? '08:00',
        defaultStatusId: value.defaultStatusId ?? 1,
        isActive: value.isActive ?? true,
        assigneeIds: (value.assigneeIds as number[]) ?? [],
        labelIds: (value.labelIds as number[]) ?? [],
        shareUserIds: (value.shareUserIds as number[]) ?? [],
      };
      this.dialogRef.close(request);
    }
  }

  private priorityValue(): TaskPriority {
    const p = this.data.template?.priority;
    if (p === 'High') return TaskPriority.High;
    if (p === 'Low') return TaskPriority.Low;
    return TaskPriority.Medium;
  }

  private frequencyValue(): RecurringFrequency {
    const f = this.data.template?.frequency;
    if (f === 'Weekly') return RecurringFrequency.Weekly;
    if (f === 'Monthly') return RecurringFrequency.Monthly;
    return RecurringFrequency.Daily;
  }
}
