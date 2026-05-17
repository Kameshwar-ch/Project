import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { NotificationService } from '../../core/services/notification.service';
import { SfNotification } from '../../core/models';
import { SharedModule } from '../../shared/shared.module';

type FilterMode = 'Category' | 'Status';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './notifications.html',
})
export class NotificationsComponent implements OnInit {
  private svc = inject(NotificationService);

  allNotifications = signal<SfNotification[]>([]);
  loading = signal(true);
  apiError = signal('');
  toast = signal('');
  markingId = signal<number | null>(null);

  // Filter mode — how the user wants to filter
  filterMode = signal<FilterMode>('Category');

  // Active filter values
  activeCategory = signal('All');
  activeStatus   = signal('Unread');

  emailAlerts   = signal(true);
  inAppAlerts   = signal(true);
  weeklySummary = signal(false);

  readonly filterModes: FilterMode[]   = ['Category', 'Status'];
  readonly categories = ['All', 'Certification', 'Enrollment', 'Assessment', 'Report', 'SkillGap', 'Compliance', 'Audit'];
  readonly statuses   = ['Unread', 'Read', 'All'];

  // Client-side filter applied on top of the API result
  readonly notifications = computed(() => {
    let list = this.allNotifications();
    if (this.filterMode() === 'Status' && this.activeStatus() !== 'All') {
      const s = this.activeStatus().toLowerCase();
      list = list.filter(n => n.status?.toLowerCase() === s);
    }
    return list;
  });

  ngOnInit() {
    this.fetchByCategory();
  }

  onFilterModeChange(mode: FilterMode) {
    this.filterMode.set(mode);
    // When switching to category mode, re-fetch with current category
    if (mode === 'Category') {
      this.fetchByCategory(this.activeCategory());
    }
    // When switching to status mode, load all from API then filter client-side
    if (mode === 'Status') {
      this.activeCategory.set('All');
      this.fetchByCategory('All');
    }
  }

  selectCategory(cat: string) {
    this.activeCategory.set(cat);
    this.fetchByCategory(cat);
  }

  selectStatus(status: string) {
    this.activeStatus.set(status);
    // status filtering is client-side, no new API call needed
  }

  fetchByCategory(category?: string) {
    this.loading.set(true);
    this.apiError.set('');
    const cat = category ?? this.activeCategory();
    this.svc.getAll(cat === 'All' ? undefined : cat).subscribe({
      next: data => {
        this.allNotifications.set(Array.isArray(data) ? data : []);
        this.loading.set(false);
      },
      error: err => {
        this.apiError.set(err?.error?.message ?? 'Failed to load notifications.');
        this.loading.set(false);
      }
    });
  }

  markRead(id: number) {
    this.markingId.set(id);
    this.svc.markAsRead(id).subscribe({
      next: () => {
        const wasUnread = this.allNotifications().find(n => n.notificationId === id)?.status?.toLowerCase() === 'unread';
        setTimeout(() => {
          this.allNotifications.update(list => list.filter(n => n.notificationId !== id));
          this.markingId.set(null);
        }, 300);
        if (wasUnread) this.svc.decrementUnread();
        this.showToast('Notification marked as read.');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to mark as read.');
        this.markingId.set(null);
      }
    });
  }

  private showToast(msg: string) {
    this.toast.set(msg);
    setTimeout(() => this.toast.set(''), 3000);
  }

  get unreadCount() { return this.allNotifications().filter(n => n.status?.toLowerCase() === 'unread').length; }

  categoryIcon(cat: string): string {
    const map: Record<string, string> = {
      'All':           'bi-grid',
      'Certification': 'bi-patch-check',
      'Enrollment':    'bi-person-check',
      'Assessment':    'bi-clipboard-check',
      'Report':        'bi-graph-up',
      'SkillGap':      'bi-bar-chart-steps',
      'Compliance':    'bi-shield-check',
      'Audit':         'bi-journal-text',
    };
    return map[cat] ?? 'bi-tag';
  }
}
