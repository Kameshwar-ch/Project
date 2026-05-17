import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { UserRole } from '../../../core/models';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [SharedModule, RouterLink],
  templateUrl: './signup.html',
})
export class SignupComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  readonly theme = inject(ThemeService);

  roles: UserRole[] = ['Employee', 'Trainer', 'Manager', 'HR', 'Admin'];

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50), Validators.pattern(/^[a-zA-Z ]+$/)]],
    email: ['', [Validators.required, Validators.email]],
    role: ['Employee', Validators.required],
    phone: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{9,14}$/)]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^a-zA-Z0-9])/),
    ]],
  });

  loading = signal(false);
  error = signal('');
  success = signal(false);
  showPassword = signal(false);

  get f() { return this.form.controls; }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    this.auth.register(this.form.value).subscribe({
      next: () => {
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: err => {
        const body = err?.error;
        let msg = body?.message;
        if (!msg && body?.errors) {
          // FluentValidation returns { errors: { Field: ["msg"] } }
          msg = Object.values(body.errors as Record<string, string[]>)
            .flat().join(' ');
        }
        this.error.set(msg || 'Registration failed. Please try again.');
        this.loading.set(false);
      }
    });
  }

  socialLogin(provider: string) { /* future OAuth */ }
  togglePassword() { this.showPassword.update(v => !v); }
  toggleTheme() { this.theme.toggle(); }
}
