import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { SkillGapItem, CreateSkillGapRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class SkillGapService {
  private base = `${environment.apiUrl}/SkillGap`;
  constructor(private http: HttpClient) {}

  getAll(filters: { startDate?: string; endDate?: string; employeeId?: number; competencyId?: number; gapLevel?: number } = {}) {
    let params = new HttpParams();
    if (filters.startDate) params = params.set('startDate', filters.startDate);
    if (filters.endDate) params = params.set('endDate', filters.endDate);
    if (filters.employeeId != null) params = params.set('employeeId', filters.employeeId);
    if (filters.competencyId != null) params = params.set('competencyId', filters.competencyId);
    if (filters.gapLevel != null) params = params.set('gapLevel', filters.gapLevel);
    return this.http.get<SkillGapItem[]>(this.base, { params });
  }

  getByEmployee(employeeId: number) {
    return this.http.get<SkillGapItem[]>(`${this.base}/employee/${employeeId}`);
  }

  create(payload: CreateSkillGapRequest) {
    return this.http.post<SkillGapItem>(this.base, payload);
  }

  delete(id: number) {
    return this.http.delete<{ message: string }>(`${this.base}/${id}`);
  }
}
