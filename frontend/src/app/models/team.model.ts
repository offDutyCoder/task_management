export interface TeamMemberDto {
  userId: number;
  displayName: string;
}

export interface TeamDto {
  id: number;
  name: string;
  createdByUserId: number;
  members: TeamMemberDto[];
}

export interface CreateTeamRequest {
  name: string;
  memberUserIds: number[];
}

export interface UpdateTeamRequest {
  name: string;
  memberUserIds: number[];
}
