import { Injectable, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, catchError, throwError } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { environment } from '../../../environments/environment';
import { AuthTokens, JwtPayload, LoginRequest, RegisterRequest, UserRole } from '../models';
import { AuthStateService } from '../store/auth.state';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authState = inject(AuthStateService);
  private readonly http      = inject(HttpClient);
  private readonly router    = inject(Router);

  private readonly TOKEN_KEY   = 'sf_access_token';


  // ── RxJS observables (centralized reactive state) ────────────────────────
  // Subscribe to these in components or effects that need to react to changes.
  readonly user$       = this.authState.user$;
  readonly isLoggedIn$ = this.authState.isLoggedIn$;
  readonly userRole$   = this.authState.userRole$;
  readonly userName$   = this.authState.userName$;
  readonly userId$     = this.authState.userId$;

  // ── Signals derived from the observable state ─────────────────────────────
  // BehaviorSubject emits synchronously on subscribe, so requireSync is safe.
  private readonly _user = toSignal(this.authState.user$, { requireSync: true });

  readonly user       = this._user;
  readonly isLoggedIn = computed(() => !!this._user());
  readonly userRole   = computed(() => this._user()?.role ?? null);
  readonly userName   = computed(() => this._user()?.name ?? '');
  readonly userId     = computed(() => (this._user()?.id ? Number(this._user()!.id) : null));

  // ── Auth operations ───────────────────────────────────────────────────────

  login(request: LoginRequest) {
    return this.http.post<AuthTokens>(`${environment.apiUrl}/Auth/login`, request).pipe(
      tap(tokens => this.handleTokens(tokens)),
      catchError(err => throwError(() => err))
    );
  }

  register(request: RegisterRequest) {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/User/Register`, request).pipe(
      catchError(err => throwError(() => err))
    );
  }

  logout() {
    localStorage.removeItem(this.TOKEN_KEY);
    this.authState.setUser(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  hasRole(...roles: UserRole[]): boolean {
    const role = this.userRole();
    return role ? roles.includes(role) : false;
  }

  private handleTokens(tokens: AuthTokens): void {
    localStorage.setItem(this.TOKEN_KEY, tokens.access_token);
    this.authState.setUser(this.decodeJwt(tokens.access_token));
  }

  private decodeJwt(token: string): JwtPayload {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('')
    );
    return JSON.parse(json);
  }
}
