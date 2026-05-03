import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatBadgeModule,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private pollInterval: ReturnType<typeof setInterval> | null = null;

  readonly isAdmin = this.authService.isAdmin;
  readonly unreadCount = signal(0);

  ngOnInit(): void {
    void this.loadUnreadCount();
    this.pollInterval = setInterval(() => void this.loadUnreadCount(), 30000);
  }

  ngOnDestroy(): void {
    if (this.pollInterval !== null) {
      clearInterval(this.pollInterval);
    }
  }

  async loadUnreadCount(): Promise<void> {
    try {
      const result = await firstValueFrom(this.notificationService.getAll());
      this.unreadCount.set(result.unreadCount);
    } catch {
      // ポーリングエラーは無視
    }
  }

  navigateToNotifications(): void {
    void this.router.navigate(['/notifications']);
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.authService.logout());
    } catch {
      this.authService.setCurrentUser(null);
    }
    await this.router.navigate(['/login']);
  }
}
