import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { firstValueFrom } from 'rxjs';
import {
  CreateTaskRequest,
  TaskDetailResponse,
  TaskPriority,
  UpdateTaskRequest,
} from '../../../models/task.model';
import { TaskStatus } from '../../../models/task-status.model';
import { Label } from '../../../models/label.model';
import { User } from '../../../models/user.model';
import { StatusService } from '../../../services/status.service';
import { LabelService } from '../../../services/label.service';
import { UserService } from '../../../services/user.service';

export interface TaskFormDialogData {
  mode: 'create' | 'edit';
  task?: TaskDetailResponse;
  parentTaskId?: number | null;
}

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
  ],
  templateUrl: './task-form.component.html',
  styleUrl: './task-form.component.scss',
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<TaskFormComponent>);
  private readonly statusService = inject(StatusService);
  private readonly labelService = inject(LabelService);
  private readonly userService = inject(UserService);

  readonly TaskPriority = TaskPriority;
  readonly statuses = signal<TaskStatus[]>([]);
  readonly labels = signal<Label[]>([]);
  readonly users = signal<User[]>([]);

  readonly form = this.fb.group({
    title: [this.data.task?.title ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.data.task?.description ?? ''],
    statusId: [this.data.task?.status?.id ?? null as number | null, [Validators.required]],
    priority: [this.data.task?.priority ?? TaskPriority.Medium, [Validators.required]],
    dueDate: [this.data.task?.dueDate ? new Date(this.data.task.dueDate) : null as Date | null],
    assigneeIds: [this.data.task?.assignees.map(a => a.id) ?? [] as number[]],
    labelIds: [this.data.task?.labels.map(l => l.id) ?? [] as number[]],
    shareUserIds: [this.data.task?.shareUsers?.map(u => u.id) ?? [] as number[]],
  });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: TaskFormDialogData) {}

  ngOnInit(): void {
    void this.loadReferenceData();
  }

  private async loadReferenceData(): Promise<void> {
    const [statuses, labels, users] = await Promise.all([
      firstValueFrom(this.statusService.getAll()),
      firstValueFrom(this.labelService.getAll()),
      firstValueFrom(this.userService.getAll()),
    ]);
    this.statuses.set(statuses.filter(s => s.isActive));
    this.labels.set(labels);
    this.users.set(users);
  }

  onSave(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const dueDate = value.dueDate ? (value.dueDate as Date).toISOString().split('T')[0] : null;

    if (this.data.mode === 'create') {
      const request: CreateTaskRequest = {
        title: value.title ?? '',
        description: value.description || null,
        statusId: value.statusId!,
        priority: value.priority ?? TaskPriority.Medium,
        dueDate,
        parentTaskId: this.data.parentTaskId ?? null,
        assigneeIds: value.assigneeIds ?? [],
        labelIds: value.labelIds ?? [],
        shareUserIds: value.shareUserIds ?? [],
      };
      this.dialogRef.close(request);
    } else {
      const request: UpdateTaskRequest = {
        title: value.title ?? '',
        description: value.description || null,
        statusId: value.statusId!,
        priority: value.priority ?? TaskPriority.Medium,
        dueDate,
        assigneeIds: value.assigneeIds ?? [],
        labelIds: value.labelIds ?? [],
        shareUserIds: value.shareUserIds ?? [],
      };
      this.dialogRef.close(request);
    }
  }

  getPriorityLabel(p: TaskPriority): string {
    switch (p) {
      case TaskPriority.High: return '高';
      case TaskPriority.Medium: return '中';
      case TaskPriority.Low: return '低';
    }
  }
}
