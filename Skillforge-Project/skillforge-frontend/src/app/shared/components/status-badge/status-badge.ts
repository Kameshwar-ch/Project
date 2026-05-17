import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `<span class="badge rounded-pill" [ngClass]="badgeClass">{{ label }}</span>`,
})
export class StatusBadgeComponent {
  @Input() status: string = '';

  get label(): string { return this.status; }

  get badgeClass(): string {
    const s = this.status?.toLowerCase();
    if (['live', 'active', 'compliant', 'present', 'enrolled', 'scheduled'].includes(s)) return 'bg-cyan text-dark';
    if (['draft', 'waitlist', 'pending'].includes(s)) return 'bg-accent';
    if (['closed', 'absent', 'inactive'].includes(s)) return 'bg-secondary';
    if (['expiring', 'mandatory', 'medium'].includes(s)) return 'bg-warning text-dark';
    if (['expired', 'failed', 'high'].includes(s)) return 'bg-danger';
    return 'bg-secondary';
  }
}
