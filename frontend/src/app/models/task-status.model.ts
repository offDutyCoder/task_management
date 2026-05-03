export interface TaskStatus {
  id: number;
  name: string;
  color: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateStatusRequest {
  name: string;
  color: string;
  displayOrder: number;
}

export interface UpdateStatusRequest {
  name: string;
  color: string;
  displayOrder: number;
  isActive: boolean;
}
