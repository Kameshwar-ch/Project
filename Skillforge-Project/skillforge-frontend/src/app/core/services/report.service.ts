import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface ReportSchedule {
  scheduleID: number;
  scope: string;
  cronExpression: string;
  createdAt: string;
  nextRun: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private base = `${environment.apiUrl}/Report`;
  constructor(private http: HttpClient) {}

  getSchedules() {
    return this.http.get<ReportSchedule[]>(`${this.base}/schedules`);
  }

  createSchedule(scope: string, cronExpression: string) {
    return this.http.post<ReportSchedule>(`${this.base}/schedule`, { scope, cronExpression });
  }

  deactivateSchedule(id: number) {
    return this.http.patch<{ message: string }>(`${this.base}/schedules/${id}/deactivate`, {});
  }

generateReport(scope: string) {
    return this.http.post<Blob>(`${this.base}/generate`, { scope }, { responseType: 'blob' as 'json' });
  }
}
