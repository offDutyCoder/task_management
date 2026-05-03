import { Component, inject, signal } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { ShellComponent } from './layout/shell/shell.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ShellComponent],
  template: `
    @if (isLoginPage()) {
      <router-outlet />
    } @else {
      <app-shell>
        <router-outlet />
      </app-shell>
    }
  `,
})
export class AppComponent {
  private readonly router = inject(Router);
  readonly isLoginPage = signal(window.location.pathname.startsWith('/login'));

  constructor() {
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed()
    ).subscribe(e => {
      this.isLoginPage.set(e.urlAfterRedirects.startsWith('/login'));
    });
  }
}
