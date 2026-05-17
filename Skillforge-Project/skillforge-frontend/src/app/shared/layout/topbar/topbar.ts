import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './topbar.html',
})
export class TopbarComponent implements OnInit {
  private auth = inject(AuthService);
  readonly theme = inject(ThemeService);
  private notificationSvc = inject(NotificationService);

  readonly user = this.auth.user;
  readonly unreadCount = this.notificationSvc.unreadCount;  // shared signal

  ngOnInit() {
    this.notificationSvc.loadUnreadCount();
  }

  logout() { this.auth.logout(); }
  toggleTheme() { this.theme.toggle(); }
}
