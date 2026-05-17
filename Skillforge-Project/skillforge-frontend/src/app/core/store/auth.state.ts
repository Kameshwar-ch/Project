import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';
import { JwtPayload, UserRole } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly TOKEN_KEY = 'sf_access_token';

  private readonly _user$ = new BehaviorSubject<JwtPayload | null>(this.loadInitialUser());

  /** Full user payload stream */
  readonly user$ = this._user$.asObservable();

  /** Derived observable streams — subscribe in components or services */
  readonly isLoggedIn$ = this._user$.pipe(map(u => !!u));
  readonly userRole$   = this._user$.pipe(map((u): UserRole | null => u?.role ?? null));
  readonly userName$   = this._user$.pipe(map(u => u?.name ?? ''));
  readonly userId$     = this._user$.pipe(map(u => (u?.id ? Number(u.id) : null)));

  /** Synchronous snapshot — use in guards and interceptors */
  get snapshot(): JwtPayload | null { return this._user$.value; }

  /** Update the session — called by AuthService on login/logout */
  setUser(user: JwtPayload | null): void {
    this._user$.next(user);
  }

  private loadInitialUser(): JwtPayload | null {
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (!token) return null;
    try {
      const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('')
      );
      const payload: JwtPayload = JSON.parse(json);
      if (payload && payload.exp * 1000 > Date.now()) return payload;
      localStorage.removeItem(this.TOKEN_KEY);
      return null;
    } catch {
      return null;
    }
  }
}
