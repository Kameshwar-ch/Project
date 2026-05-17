import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { SfNotification } from '../models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private base = `${environment.apiUrl}/Notification`;

  /** Shared unread count — topbar reads this, notifications page decrements it. */
  readonly unreadCount = signal(0);

  constructor(private http: HttpClient) {}

  getAll(category?: string) {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    return this.http.get<{ data: SfNotification[]; message?: string }>(this.base, { params }).pipe(
      map(res => res?.data ?? [])
    );
  }

  /** Fetch all notifications and update the shared unread count. */
  loadUnreadCount() {
    this.getAll().subscribe({
      next: data => this.unreadCount.set(data.filter(n => n.status?.toLowerCase() === 'unread').length),
      error: () => {}
    });
  }

  /** Decrement the shared unread count by 1 (min 0). */
  decrementUnread() {
    this.unreadCount.update(n => Math.max(0, n - 1));
  }

  markAsRead(id: number) {
    return this.http.patch<{ message: string; data: SfNotification }>(`${this.base}/${id}`, {});
  }
}
