export enum TaskPriority {
  High = 0,
  Medium = 1,
  Low = 2,
}

export interface StatusDto {
  id: number;
  name: string;
  color: string;
}

export interface AssigneeDto {
  id: number;
  displayName: string;
}

export interface LabelDto {
  id: number;
  name: string;
  color: string;
}

export interface TaskListItem {
  id: number;
  title: string;
  status: StatusDto;
  priority: TaskPriority;
  dueDate: string | null;
  assignees: AssigneeDto[];
  labels: LabelDto[];
  subTaskCount: number;
  isRecurring: boolean;
}

export interface TaskListResponse {
  items: TaskListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TaskDetailResponse {
  id: number;
  title: string;
  description: string | null;
  status: StatusDto;
  priority: TaskPriority;
  dueDate: string | null;
  parentTask: TaskListItem | null;
  subTasks: TaskListItem[];
  assignees: AssigneeDto[];
  assigneeTeamId: number | null;
  labels: LabelDto[];
  shareUsers: AssigneeDto[];
  createdByUserId: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  statusId: number;
  priority: TaskPriority;
  dueDate?: string | null;
  parentTaskId?: number | null;
  assigneeIds: number[];
  assigneeTeamId?: number | null;
  labelIds: number[];
  shareUserIds: number[];
}

export interface UpdateTaskRequest {
  title: string;
  description?: string | null;
  statusId: number;
  priority: TaskPriority;
  dueDate?: string | null;
  assigneeIds: number[];
  assigneeTeamId?: number | null;
  labelIds: number[];
  shareUserIds: number[];
}

export interface TaskFilterQuery {
  statusId?: number | null;
  assigneeId?: number | null;
  labelId?: number | null;
  priority?: TaskPriority | null;
  search?: string | null;
  isRecurring?: boolean | null;
  dueBefore?: string | null;
  dueAfter?: string | null;
  page?: number;
  pageSize?: number;
}

export interface AssigneeGroup {
  assigneeType: 'user' | 'team';
  assigneeId: number;
  assigneeName: string;
  tasks: TaskListItem[];
}

export interface DashboardResponse {
  myTasks: TaskListItem[];
  teamTaskGroups: AssigneeGroup[];
}
