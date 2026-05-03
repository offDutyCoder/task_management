export enum NotificationType {
  Assigned = 0,
  Overdue = 1,
}

export interface NotificationItem {
  id: number;
  type: NotificationType;
  taskItemId: number | null;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationListResponse {
  items: NotificationItem[];
  unreadCount: number;
}
