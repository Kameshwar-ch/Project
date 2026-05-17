import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ComplianceSummary, AuditItem } from '../models';

@Injectable({ providedIn: 'root' })
export class ComplianceService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getComplianceSummary() {
    return this.http.get<ComplianceSummary>(`${this.base}/ComplianceRecord/Summary`);
  }

  runComplianceCheck() {
    return this.http.get(`${this.base}/ComplianceRecord/Refresh`, { responseType: 'text' });
  }

  // ── Audit CRUD ────────────────────────────────────────────────────────────
  getAudits() {
    return this.http.get<AuditItem[]>(`${this.base}/Audit`).pipe(
      catchError(err => {
        if (err.status === 404) return of([] as AuditItem[]);
        throw err;
      })
    );
  }

  createAudit(dto: { scope: string; findings: string; status: boolean }) {
    return this.http.post<AuditItem>(`${this.base}/Audit`, dto);
  }

  deleteAudit(id: number) {
    return this.http.delete<{ message: string }>(`${this.base}/Audit/${id}`);
  }
}
