import { TaskPriority } from './task.model';

export enum RecurringFrequency {
  Daily = 0,
  Weekly = 1,
  Monthly = 2,
}

export interface RecurringTemplateAssignee {
  id: number;
  displayName: string;
}

export interface RecurringTemplateLabel {
  id: number;
  name: string;
  color: string;
}

export interface RecurringTemplate {
  id: number;
  title: string;
  description: string | null;
  priority: string;
  frequency: string;
  weekDays: number[] | null;
  dayOfMonth: number | null;
  excludeWeekends: boolean;
  generationTime: string;
  defaultStatusId: number;
  isActive: boolean;
  assignees: RecurringTemplateAssignee[];
  labels: RecurringTemplateLabel[];
  shareUsers: RecurringTemplateAssignee[];
  createdAt: string;
}

export interface CreateRecurringTemplateRequest {
  title: string;
  description?: string | null;
  priority: TaskPriority;
  frequency: RecurringFrequency;
  weekDays?: number[] | null;
  dayOfMonth?: number | null;
  excludeWeekends: boolean;
  generationTime: string;
  defaultStatusId: number;
  assigneeIds: number[];
  labelIds: number[];
  shareUserIds: number[];
}

export interface UpdateRecurringTemplateRequest extends CreateRecurringTemplateRequest {
  isActive: boolean;
}
