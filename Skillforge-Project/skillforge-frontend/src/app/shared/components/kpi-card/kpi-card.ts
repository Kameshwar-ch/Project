import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="sf-card h-100 fade-in">
      <div class="d-flex justify-content-between align-items-start">
        <div>
          <p class="sf-muted small mb-1">{{ title }}</p>
          <div class="sf-kpi-value">{{ value }}</div>
          <p class="sf-muted small mt-1 mb-0">{{ subtitle }}</p>
        </div>
        <div class="sf-kpi-icon">
          <i class="bi {{ icon }} fs-4"></i>
        </div>
      </div>
      <div class="mt-3" *ngIf="trend">
        <span class="badge" [ngClass]="trendClass">
          <i class="bi" [ngClass]="trendUp ? 'bi-arrow-up' : 'bi-arrow-down'"></i>
          {{ trend }}
        </span>
      </div>
    </div>
  `
})
export class KpiCardComponent {
  @Input() title = '';
  @Input() value: string | number = '';
  @Input() subtitle = '';
  @Input() icon = 'bi-graph-up';
  @Input() trend = '';
  @Input() trendUp = true;

  get trendClass() {
    return this.trendUp ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger';
  }
}
