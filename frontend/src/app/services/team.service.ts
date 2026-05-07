import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TeamDto, CreateTeamRequest, UpdateTeamRequest } from '../models/team.model';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<TeamDto[]> {
    return this.http.get<TeamDto[]>('/api/teams', { withCredentials: true });
  }

  getById(id: number): Observable<TeamDto> {
    return this.http.get<TeamDto>(`/api/teams/${id}`, { withCredentials: true });
  }

  create(request: CreateTeamRequest): Observable<TeamDto> {
    return this.http.post<TeamDto>('/api/teams', request, { withCredentials: true });
  }

  update(id: number, request: UpdateTeamRequest): Observable<TeamDto> {
    return this.http.put<TeamDto>(`/api/teams/${id}`, request, { withCredentials: true });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/teams/${id}`, { withCredentials: true });
  }
}
