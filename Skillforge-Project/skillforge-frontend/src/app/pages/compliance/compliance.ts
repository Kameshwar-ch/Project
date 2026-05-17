import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../shared/shared.module';
import { ComplianceService } from '../../core/services/compliance.service';
import { AuthService } from '../../core/services/auth.service';
import { ComplianceSummary, AuditItem, ComplianceItem } from '../../core/models';

@Component({
  selector: 'app-compliance',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './compliance.html',
})
export class ComplianceComponent implements OnInit {
  private svc = inject(ComplianceService);
  private fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  summary = signal<ComplianceSummary | null>(null);
  audits = signal<AuditItem[]>([]);

  complianceLoading = signal(false);
  auditLoading = signal(false);
  refreshLoading = signal(false);

  toast = signal('');
  toastType = signal<'success' | 'error'>('success');

  // ── Compliance filters ────────────────────────────────────────────────────
  employeeSearch = signal('');
  statusFilter = signal<'All' | 'Compliant' | 'Non-Compliant'>('All');

  // ── Drill-down (expand per employee) ─────────────────────────────────────
  expandedEmployee = signal<number | null>(null);

  get isHR() { return this.auth.hasRole('HR'); }
  get isAdminOrHR() { return this.auth.hasRole('Admin', 'HR'); }

  // ── Audit filters ─────────────────────────────────────────────────────────
  auditScopeFilter = signal<string>('All');
  auditStatusFilter = signal<string>('All');

  // ── Create Audit modal ────────────────────────────────────────────────────
  showCreateAudit = signal(false);
  createAuditLoading = signal(false);
  auditForm = this.fb.group({
    scope: ['', Validators.required],
    findings: ['', [Validators.required, Validators.maxLength(500)]],
    status: [null as boolean | null, Validators.required],
  });

  // ── Filtered compliance records ───────────────────────────────────────────
  get filteredRecords(): ComplianceItem[] {
    const records = this.summary()?.records ?? [];
    const search = this.employeeSearch().toLowerCase().trim();
    const status = this.statusFilter();
    return records.filter(r => {
      const nameMatch = !search || r.employeeName.toLowerCase().includes(search);
      const statusMatch = status === 'All'
        || (status === 'Compliant' && r.status)
        || (status === 'Non-Compliant' && !r.status);
      return nameMatch && statusMatch;
    });
  }

  // ── Employee groups for drill-down ────────────────────────────────────────
  get employeeGroups(): { employeeId: number; employeeName: string; records: ComplianceItem[]; compliantCount: number }[] {
    const map = new Map<number, { employeeId: number; employeeName: string; records: ComplianceItem[] }>();
    for (const r of this.filteredRecords) {
      if (!map.has(r.employeeId)) {
        map.set(r.employeeId, { employeeId: r.employeeId, employeeName: r.employeeName, records: [] });
      }
      map.get(r.employeeId)!.records.push(r);
    }
    return Array.from(map.values()).map(g => ({
      ...g,
      compliantCount: g.records.filter(r => r.status).length,
    }));
  }

  // ── Filtered audit records ────────────────────────────────────────────────
  get filteredAudits(): AuditItem[] {
    let list = this.audits();
    const scope = this.auditScopeFilter();
    if (scope !== 'All') list = list.filter(a => a.scope === scope);
    const status = this.auditStatusFilter();
    if (status !== 'All') list = list.filter(a => (status === 'Pass') === a.status);
    return list;
  }

  toggleEmployee(id: number) {
    this.expandedEmployee.set(this.expandedEmployee() === id ? null : id);
  }

  resetFilters() {
    this.employeeSearch.set('');
    this.statusFilter.set('All');
    this.expandedEmployee.set(null);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit() {
    if (this.isHR) this.loadSummary();
    if (this.isAdminOrHR) this.loadAudits();
  }

  loadSummary() {
    this.complianceLoading.set(true);
    this.svc.getComplianceSummary().subscribe({
      next: data => { this.summary.set(data); this.complianceLoading.set(false); },
      error: () => { this.complianceLoading.set(false); }
    });
  }

  loadAudits() {
    this.auditLoading.set(true);
    this.svc.getAudits().subscribe({
      next: data => { this.audits.set(data); this.auditLoading.set(false); },
      error: () => { this.auditLoading.set(false); }
    });
  }

  runComplianceCheck() {
    this.refreshLoading.set(true);
    this.svc.runComplianceCheck().subscribe({
      next: msg => {
        this.showToast(msg || 'Compliance records refreshed.', 'success');
        this.refreshLoading.set(false);
        this.loadSummary();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Compliance check failed.', 'error');
        this.refreshLoading.set(false);
      }
    });
  }

  // ── Create Audit ──────────────────────────────────────────────────────────
  openCreateAudit() {
    this.auditForm.reset();
    this.showCreateAudit.set(true);
  }

  closeCreateAudit() {
    this.showCreateAudit.set(false);
    this.auditForm.reset();
  }

  setAuditStatus(status: boolean) {
    this.auditForm.patchValue({ status });
  }

  submitCreateAudit() {
    if (this.auditForm.invalid) { this.auditForm.markAllAsTouched(); return; }
    this.createAuditLoading.set(true);
    const { scope, findings, status } = this.auditForm.value as any;
    this.svc.createAudit({ scope, findings, status }).subscribe({
      next: () => {
        this.closeCreateAudit();
        this.showToast('Audit record created successfully!', 'success');
        this.createAuditLoading.set(false);
        this.loadAudits();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to create audit.', 'error');
        this.createAuditLoading.set(false);
      }
    });
  }

  deleteAudit(id: number) {
    this.svc.deleteAudit(id).subscribe({
      next: () => {
        this.audits.update(list => list.filter(a => a.auditID !== id));
        this.showToast('Audit record deleted.', 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to delete audit.', 'error');
      }
    });
  }

  complianceStatus(status: boolean) { return status ? 'Compliant' : 'Non-Compliant'; }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg);
    this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 4000);
  }
}
