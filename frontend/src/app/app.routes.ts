import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard],
  },
  {
    path: 'admin/users',
    loadComponent: () =>
      import('./pages/admin/user-management/user-management.component').then(
        m => m.UserManagementComponent
      ),
    canActivate: [authGuard, adminGuard],
  },
  {
    path: 'admin/statuses',
    loadComponent: () =>
      import('./pages/admin/status-management/status-management.component').then(
        m => m.StatusManagementComponent
      ),
    canActivate: [authGuard, adminGuard],
  },
  {
    path: 'admin/labels',
    loadComponent: () =>
      import('./pages/admin/label-management/label-management.component').then(
        m => m.LabelManagementComponent
      ),
    canActivate: [authGuard, adminGuard],
  },
  {
    path: 'tasks',
    loadComponent: () =>
      import('./pages/tasks/task-list/task-list.component').then(
        m => m.TaskListComponent
      ),
    canActivate: [authGuard],
  },
  {
    path: 'tasks/:id',
    loadComponent: () =>
      import('./pages/tasks/task-detail/task-detail.component').then(
        m => m.TaskDetailComponent
      ),
    canActivate: [authGuard],
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
