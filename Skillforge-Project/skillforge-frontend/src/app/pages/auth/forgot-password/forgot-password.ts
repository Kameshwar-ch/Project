import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ThemeService } from '../../../core/services/theme.service';
import { environment } from '../../../../environments/environment';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [SharedModule, RouterLink],
  templateUrl: './forgot-password.html',
})
export class ForgotPasswordComponent {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  readonly theme = inject(ThemeService);

  step = signal<'email' | 'reset'>('email');
  loading = signal(false);
  success = signal('');
  error = signal('');

  emailForm = this.fb.group({ email: ['', [Validators.required, Validators.email]] });
  resetForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(6)]],
  });

  submitEmail() {
    if (this.emailForm.invalid) { this.emailForm.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set('');
    this.http.post(`${environment.apiUrl}/ForgotPassword/verifyemail`, this.emailForm.value).subscribe({
      next: () => {
        this.resetForm.patchValue({ email: this.emailForm.value.email ?? '' });
        this.step.set('reset');
        this.loading.set(false);
      },
      error: err => { this.error.set(err?.error?.message ?? 'Email not found.'); this.loading.set(false); }
    });
  }

  submitReset() {
    if (this.resetForm.invalid) { this.resetForm.markAllAsTouched(); return; }
    const { newPassword, confirmPassword } = this.resetForm.value;
    if (newPassword !== confirmPassword) { this.error.set('Passwords do not match.'); return; }
    this.loading.set(true); this.error.set('');
    this.http.post(`${environment.apiUrl}/ForgotPassword/resetpassword`, this.resetForm.value).subscribe({
      next: () => { this.success.set('Password reset! You can now sign in.'); this.loading.set(false); },
      error: err => { this.error.set(err?.error?.message ?? 'Reset failed.'); this.loading.set(false); }
    });
  }
}
