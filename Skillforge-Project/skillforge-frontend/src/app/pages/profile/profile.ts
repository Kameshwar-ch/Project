import { Component, inject, signal, OnInit } from '@angular/core';
import { UserService } from '../../core/services/user.service';
import { AuthService } from '../../core/services/auth.service';
import { SharedModule } from '../../shared/shared.module';
import { User } from '../../core/models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './profile.html',
})
export class ProfileComponent implements OnInit {
  private userSvc = inject(UserService);
  readonly auth   = inject(AuthService);

  profile = signal<User | null>(null);
  loading = signal(true);
  error   = signal<string | null>(null);

  ngOnInit() {
    this.userSvc.getMe().subscribe({
      next:  user => { this.profile.set(user); this.loading.set(false); },
      error: err  => { this.error.set(err?.error?.message ?? 'Failed to load profile.'); this.loading.set(false); }
    });
  }

  roleIcon(role: string): string {
    const map: Record<string, string> = {
      Employee: 'bi-person',
      Trainer:  'bi-easel',
      Manager:  'bi-briefcase',
      HR:       'bi-clipboard-check',
      Admin:    'bi-shield-lock',
    };
    return map[role] ?? 'bi-person';
  }
}
