import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

interface NavItem {
  label: string;
  path: string;
  icon: string;
  roles: string[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
})
export class SidebarComponent {
  private auth = inject(AuthService);
  readonly user = this.auth.user;
  readonly role = this.auth.userRole;

  readonly navItems: NavItem[] = [
    { label: 'Dashboard',              path: '/dashboard',      icon: 'bi-speedometer2',     roles: ['Employee','Trainer','Manager','HR','Admin'] },
    { label: 'Training Catalog',       path: '/catalog',        icon: 'bi-collection',       roles: ['Employee','Trainer','Manager','HR','Admin'] },
    { label: 'Enrollment & Attendance',path: '/enrollment',     icon: 'bi-person-check',     roles: ['Employee','Trainer','Manager','HR','Admin'] },
    { label: 'Assessments & Certs',    path: '/assessments',    icon: 'bi-patch-check',      roles: ['Employee','Trainer','Manager','HR','Admin'] },
    { label: 'Competency & Skill Gaps',path: '/competency',     icon: 'bi-bar-chart-steps',  roles: ['Manager','HR','Admin'] },
    { label: 'Compliance & Audit',     path: '/compliance',     icon: 'bi-shield-check',     roles: ['HR','Admin'] },
    { label: 'Reports & Analytics',    path: '/reports',        icon: 'bi-graph-up-arrow',   roles: ['Manager','HR','Admin'] },
    { label: 'Notifications',          path: '/notifications',  icon: 'bi-bell',             roles: ['Employee','Trainer','Manager','HR','Admin'] },
    { label: 'Identity & Access',      path: '/iam',            icon: 'bi-people',           roles: ['Admin'] },
    { label: 'My Profile',             path: '/profile',        icon: 'bi-person-circle',    roles: ['Employee','Trainer','Manager','HR','Admin'] },
  ];

  get visibleItems() {
    const r = this.role();
    if (!r) return [];
    return this.navItems.filter(item => item.roles.includes(r));
  }
}
