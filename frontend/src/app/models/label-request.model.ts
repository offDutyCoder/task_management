export type LabelRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface LabelRequest {
  id: number;
  requestedName: string;
  reason: string | null;
  status: LabelRequestStatus;
  requestedByDisplayName: string;
  createdAt: string;
}

export interface CreateLabelRequestDto {
  requestedName: string;
  reason?: string;
}

export interface UpdateLabelRequestStatusDto {
  status: LabelRequestStatus;
  color?: string;
}
