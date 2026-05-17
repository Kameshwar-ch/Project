import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { KpiCardComponent } from './components/kpi-card/kpi-card';
import { SpinnerComponent } from './components/spinner/spinner';
import { StatusBadgeComponent } from './components/status-badge/status-badge';

/**
 * SharedModule — re-exports all reusable UI components plus common Angular
 * modules. Import this module in any feature component that needs one or more
 * of these building blocks instead of importing them individually.
 */
@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    KpiCardComponent,
    SpinnerComponent,
    StatusBadgeComponent,
  ],
  exports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    KpiCardComponent,
    SpinnerComponent,
    StatusBadgeComponent,
  ],
})
export class SharedModule {}
