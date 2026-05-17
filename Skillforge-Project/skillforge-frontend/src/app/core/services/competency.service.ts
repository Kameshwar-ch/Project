import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CompetencyMatrixEntry, CompetencyItem, CreateCompetencyRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class CompetencyService {
  private base = `${environment.apiUrl}/Competency`;
  constructor(private http: HttpClient) {}

  getMatrix(filters: { employeeId?: number; employeeName?: string; skillName?: string; level?: string } = {}) {
    let params = new HttpParams();
    if (filters.employeeId != null) params = params.set('employeeId', filters.employeeId);
    if (filters.employeeName) params = params.set('employeeName', filters.employeeName);
    if (filters.skillName) params = params.set('skillName', filters.skillName);
    if (filters.level) params = params.set('level', filters.level);
    return this.http.get<CompetencyMatrixEntry[]>(`${this.base}/matrix`, { params });
  }

  getAll() {
    return this.http.get<CompetencyItem[]>(this.base);
  }

  create(payload: CreateCompetencyRequest) {
    return this.http.post<CompetencyItem>(this.base, payload);
  }

  update(id: number, payload: CreateCompetencyRequest) {
    return this.http.put<{ message: string }>(`${this.base}/${id}`, payload);
  }

  delete(id: number) {
    return this.http.delete<{ message: string }>(`${this.base}/${id}`);
  }
}
