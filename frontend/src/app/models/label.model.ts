export interface Label {
  id: number;
  name: string;
  color: string;
  isActive: boolean;
}

export interface CreateLabelRequest {
  name: string;
  color: string;
}

export interface UpdateLabelRequest {
  name: string;
  color: string;
  isActive: boolean;
}
