import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TeamDto } from '../../models/team.model';
import { TeamService } from '../../services/team.service';
import { TeamFormDialogComponent, TeamFormDialogData } from './team-form-dialog.component';

@Component({
  selector: 'app-team-detail',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDialogModule,
    MatSnackBarModule,
  ],
  templateUrl: './team-detail.component.html',
  styleUrl: './team-detail.component.scss',
})
export class TeamDetailComponent implements OnInit {
  private readonly teamService = inject(TeamService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly team = signal<TeamDto | null>(null);
  readonly isLoading = signal(false);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    void this.loadTeam(id);
  }

  private async loadTeam(id: number): Promise<void> {
    this.isLoading.set(true);
    try {
      const data = await firstValueFrom(this.teamService.getById(id));
      this.team.set(data);
    } catch {
      this.snackBar.open('チームの取得に失敗しました', '閉じる', { duration: 3000 });
    } finally {
      this.isLoading.set(false);
    }
  }

  openEditDialog(): void {
    const current = this.team();
    if (!current) return;
    const ref = this.dialog.open(TeamFormDialogComponent, {
      width: '480px',
      data: { mode: 'edit', team: current } satisfies TeamFormDialogData,
    });
    ref.afterClosed().subscribe(async (updated: TeamDto | undefined) => {
      if (updated) void this.loadTeam(updated.id);
    });
  }

  async deleteTeam(): Promise<void> {
    const current = this.team();
    if (!current) return;
    try {
      await firstValueFrom(this.teamService.delete(current.id));
      this.snackBar.open('チームを削除しました', '閉じる', { duration: 3000 });
      void this.router.navigate(['/teams']);
    } catch (error: unknown) {
      const message =
        error instanceof HttpErrorResponse && error.status === 400
          ? (error.error as { error?: string })?.error ?? '削除に失敗しました'
          : '削除に失敗しました';
      this.snackBar.open(message, '閉じる', { duration: 5000 });
    }
  }
}
