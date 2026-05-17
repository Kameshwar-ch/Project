import { Component, inject, OnInit, signal } from '@angular/core';
import { ReportService, ReportSchedule } from '../../core/services/report.service';
import { AuthService } from '../../core/services/auth.service';
import { SharedModule } from '../../shared/shared.module';

interface ScheduleForm {
  scope: string;
  frequency: string;
  minute: number;
  hour: number;
  dayOfWeek: number;
  dayOfMonth: number;
  cronExpression: string;
}

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './reports.html',
})
export class ReportsComponent implements OnInit {
  private reportSvc = inject(ReportService);
  private auth = inject(AuthService);

  get isAdmin() { return this.auth.hasRole('Admin'); }

  scope = signal('Course');
  toast = signal('');
  toastType = signal<'success' | 'error' | 'info'>('info');

  schedules = signal<ReportSchedule[]>([]);
  schedulesLoading = signal(true);
  schedulesError = signal('');

  generating = signal(false);
  deactivatingId = signal<number | null>(null);

  // Schedule creation modal
  showScheduleModal = signal(false);
  scheduleLoading = signal(false);

  // Deactivate confirmation modal
  showDeactivateModal = signal(false);
  pendingDeactivateId = signal<number | null>(null);


  form: ScheduleForm = {
    scope: 'Course',
    frequency: 'daily',
    minute: 0,
    hour: 9,
    dayOfWeek: 1,
    dayOfMonth: 1,
    cronExpression: '0 9 * * *',
  };

  // Exposed for template
  readonly dayNames = DAY_NAMES;
  readonly hours   = Array.from({ length: 24 }, (_, i) => i);
  readonly minutes = [0, 15, 30, 45];
  readonly days    = Array.from({ length: 28 }, (_, i) => i + 1);


  ngOnInit() { if (this.isAdmin) this.loadSchedules(); }

  // ─── Schedules ────────────────────────────────────────────────────────────

  loadSchedules() {
    this.schedulesLoading.set(true);
    this.schedulesError.set('');
    this.reportSvc.getSchedules().subscribe({
      next: data => { this.schedules.set(data); this.schedulesLoading.set(false); },
      error: err => {
        this.schedulesError.set(err?.error?.message ?? 'Failed to load schedules.');
        this.schedulesLoading.set(false);
      }
    });
  }

  deactivate(id: number) {
    this.pendingDeactivateId.set(id);
    this.showDeactivateModal.set(true);
  }

  confirmDeactivate() {
    const id = this.pendingDeactivateId();
    if (id == null) return;
    this.showDeactivateModal.set(false);
    this.deactivatingId.set(id);
    this.reportSvc.deactivateSchedule(id).subscribe({
      next: res => {
        this.schedules.update(list => list.map(s => s.scheduleID === id ? { ...s, isActive: false } : s));
        this.showToast(res.message ?? 'Schedule deactivated.', 'success');
        this.deactivatingId.set(null);
        this.pendingDeactivateId.set(null);
      },
      error: () => {
        this.showToast('Failed to deactivate schedule.', 'error');
        this.deactivatingId.set(null);
        this.pendingDeactivateId.set(null);
      }
    });
  }

  cancelDeactivate() {
    this.showDeactivateModal.set(false);
    this.pendingDeactivateId.set(null);
  }

// ─── Schedule Modal ────────────────────────────────────────────────────────

  openScheduleModal() {
    this.form = { scope: 'Course', frequency: 'daily', minute: 0, hour: 9, dayOfWeek: 1, dayOfMonth: 1, cronExpression: '0 9 * * *' };
    this.showScheduleModal.set(true);
  }

  closeScheduleModal() { this.showScheduleModal.set(false); }

  /** Called when any select changes — rebuilds the cron expression from the selected values. */
  onSelectChange() {
    const { frequency, minute: m, hour: h, dayOfWeek, dayOfMonth } = this.form;
    switch (frequency) {
      case 'hourly':   this.form.cronExpression = `${m} * * * *`;              break;
      case 'daily':    this.form.cronExpression = `${m} ${h} * * *`;           break;
      case 'weekly':   this.form.cronExpression = `${m} ${h} * * ${dayOfWeek}`; break;
      case 'monthly':  this.form.cronExpression = `${m} ${h} ${dayOfMonth} * *`; break;
      // custom: user manages cronExpression directly, no rebuild
    }
  }

  /** Called when user edits the cron expression manually — parses and updates the selects. */
  onCronChange() {
    const parts = this.form.cronExpression.trim().split(/\s+/);
    if (parts.length !== 5) { this.form.frequency = 'custom'; return; }
    const [m, h, dom, , dow] = parts;
    const isNum = (v: string) => /^\d+$/.test(v);

    if (isNum(m) && h === '*' && dom === '*' && dow === '*') {
      this.form.frequency = 'hourly';
      this.form.minute = +m;
    } else if (isNum(m) && isNum(h) && dom === '*' && dow === '*') {
      this.form.frequency = 'daily';
      this.form.minute = +m; this.form.hour = +h;
    } else if (isNum(m) && isNum(h) && dom === '*' && isNum(dow)) {
      this.form.frequency = 'weekly';
      this.form.minute = +m; this.form.hour = +h; this.form.dayOfWeek = +dow;
    } else if (isNum(m) && isNum(h) && isNum(dom) && dow === '*') {
      this.form.frequency = 'monthly';
      this.form.minute = +m; this.form.hour = +h; this.form.dayOfMonth = +dom;
    } else {
      this.form.frequency = 'custom';
    }
  }

  /** Human-readable description of the current cron expression. */
  get cronDescription(): string {
    const parts = this.form.cronExpression.trim().split(/\s+/);
    if (parts.length !== 5) return 'Invalid expression (must be 5 fields).';
    const [m, h, dom, , dow] = parts;
    const pad = (n: string) => String(+n).padStart(2, '0');
    const time = (h !== '*' && m !== '*') ? `${pad(h)}:${pad(m)}` : '';
    if (h === '*')              return `Every hour at :${pad(m)}`;
    if (dom === '*' && dow === '*') return `Every day at ${time}`;
    if (dom === '*' && /^\d+$/.test(dow)) return `Every ${DAY_NAMES[+dow] ?? dow} at ${time}`;
    if (/^\d+$/.test(dom) && dow === '*') return `Day ${dom} of every month at ${time}`;
    return 'Custom schedule';
  }

  submitSchedule() {
    if (!this.form.cronExpression.trim()) return;
    this.scheduleLoading.set(true);
    this.reportSvc.createSchedule(this.form.scope, this.form.cronExpression.trim()).subscribe({
      next: schedule => {
        this.schedules.update(list => [...list, schedule]);
        this.closeScheduleModal();
        this.showToast('Schedule created successfully.', 'success');
        this.scheduleLoading.set(false);
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to create schedule.', 'error');
        this.scheduleLoading.set(false);
      }
    });
  }

  // ─── Ad-hoc Report ────────────────────────────────────────────────────────

  generate() {
    this.generating.set(true);
    this.reportSvc.generateReport(this.scope()).subscribe({
      next: (blob: any) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `SkillForge-Report-${this.scope()}-${new Date().toISOString().slice(0, 10)}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
        this.showToast('Report generated and downloaded.', 'success');
        this.generating.set(false);
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Report generation failed.', 'error');
        this.generating.set(false);
      }
    });
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  get activeCount()   { return this.schedules().filter(s => s.isActive).length; }
  get inactiveCount() { return this.schedules().filter(s => !s.isActive).length; }

  pad2 = (n: number) => String(n).padStart(2, '0');

  private showToast(msg: string, type: 'success' | 'error' | 'info') {
    this.toast.set(msg);
    this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 4000);
  }
}
