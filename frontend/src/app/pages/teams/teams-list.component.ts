import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TeamDto } from '../../models/team.model';
import { TeamService } from '../../services/team.service';
import { TeamFormDialogComponent, TeamFormDialogData } from './team-form-dialog.component';

@Component({
  selector: 'app-teams-list',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    ReactiveFormsModule,
  ],
  templateUrl: './teams-list.component.html',
  styleUrl: './teams-list.component.scss',
})
export class TeamsListComponent implements OnInit {
  private readonly teamService = inject(TeamService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly teams = signal<TeamDto[]>([]);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    void this.loadTeams();
  }

  private async loadTeams(): Promise<void> {
    this.isLoading.set(true);
    try {
      const data = await firstValueFrom(this.teamService.getAll());
      this.teams.set(data);
    } catch {
      this.snackBar.open('チームの取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(TeamFormDialogComponent, {
      width: '480px',
      data: { mode: 'create' } satisfies TeamFormDialogData,
    });
    ref.afterClosed().subscribe(async (created: TeamDto | undefined) => {
      if (created) void this.loadTeams();
    });
  }

  navigateToDetail(id: number): void {
    void this.router.navigate(['/teams', id]);
  }

  async deleteTeam(id: number, event: Event): Promise<void> {
    event.stopPropagation();
    try {
      await firstValueFrom(this.teamService.delete(id));
      this.snackBar.open('チームを削除しました', '閉じる', { duration: 3000 });
      void this.loadTeams();
    } catch (error: unknown) {
      const message =
        error instanceof HttpErrorResponse && error.status === 400
          ? (error.error as { error?: string })?.error ?? '削除に失敗しました'
          : '削除に失敗しました';
      this.snackBar.open(message, '閉じる', { duration: 5000 });
    }
  }
}
