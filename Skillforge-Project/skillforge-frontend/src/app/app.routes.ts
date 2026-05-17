import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { MainLayoutComponent } from './shared/layout/main-layout/main-layout';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/auth/login/login').then(m => m.LoginComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./pages/auth/forgot-password/forgot-password').then(m => m.ForgotPasswordComponent),
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.DashboardComponent) },
      { path: 'catalog', loadComponent: () => import('./pages/catalog/catalog').then(m => m.CatalogComponent) },
      { path: 'catalog/:id', loadComponent: () => import('./pages/catalog/course-detail/course-detail').then(m => m.CourseDetailComponent) },
      { path: 'catalog/:id/edit', loadComponent: () => import('./pages/catalog/course-edit/course-edit').then(m => m.CourseEditComponent) },
      { path: 'catalog/:courseId/modules', loadComponent: () => import('./pages/catalog/course-modules/course-modules').then(m => m.CourseModulesComponent), canActivate: [roleGuard('Employee')] },
      { path: 'catalog/:courseId/modules/:moduleId', loadComponent: () => import('./pages/catalog/module-detail/module-detail').then(m => m.ModuleDetailComponent), canActivate: [roleGuard('Employee')] },
      { path: 'catalog/:courseId/modules/:moduleId/edit', loadComponent: () => import('./pages/catalog/module-edit/module-edit').then(m => m.ModuleEditComponent) },
      { path: 'enrollment', loadComponent: () => import('./pages/enrollment/enrollment').then(m => m.EnrollmentComponent) },
      { path: 'assessments', loadComponent: () => import('./pages/assessments/assessments').then(m => m.AssessmentsComponent) },
      { path: 'competency', loadComponent: () => import('./pages/competency/competency').then(m => m.CompetencyComponent), canActivate: [roleGuard('Manager', 'HR', 'Admin')] },
      { path: 'compliance', loadComponent: () => import('./pages/compliance/compliance').then(m => m.ComplianceComponent), canActivate: [roleGuard('Manager', 'HR', 'Admin')] },
      { path: 'reports', loadComponent: () => import('./pages/reports/reports').then(m => m.ReportsComponent), canActivate: [roleGuard('Manager', 'HR', 'Admin')] },
      { path: 'notifications', loadComponent: () => import('./pages/notifications/notifications').then(m => m.NotificationsComponent) },
      { path: 'iam', loadComponent: () => import('./pages/iam/iam').then(m => m.IamComponent), canActivate: [roleGuard('Admin')] },
      { path: 'profile', loadComponent: () => import('./pages/profile/profile').then(m => m.ProfileComponent) },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
