import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AssessmentResponse, EmployeeAssessment, PendingResult } from '../models';

@Injectable({ providedIn: 'root' })
export class AssessmentService {
  private base = `${environment.apiUrl}/Assessment`;
  private resultBase = `${environment.apiUrl}/Result`;
  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<AssessmentResponse[]>(this.base);
  }

  getMyAssessments() {
    return this.http.get<EmployeeAssessment[]>(`${this.base}/my`);
  }

  getModuleAssessments(moduleId: number) {
    return this.http.get<EmployeeAssessment[]>(`${this.base}/module/${moduleId}`);
  }

  create(courseId: number, type: number, maxScore: number, passingScore: number, moduleId?: number | null) {
    return this.http.post<{ assessmentId: number }>(`${this.base}/save-assessments`, {
      courseId,
      moduleId: moduleId ?? null,
      type,
      maxScore,
      passingScore
    });
  }

  submitResult(assessmentID: number, employeeID: number, score: number) {
    return this.http.post<{ message: string }>(this.resultBase, {
      assessmentID,
      employeeID,
      score
    });
  }

  selfSubmit(assessmentId: number, score: number) {
    return this.http.post<{ message: string }>(`${this.resultBase}/self`, { assessmentId, score });
  }

  getPendingResults() {
    return this.http.get<PendingResult[]>(`${this.resultBase}/pending`);
  }

  evaluateResult(assessmentId: number, employeeId: number, pass: boolean) {
    return this.http.patch<{ message: string }>(`${this.resultBase}/${assessmentId}/evaluate/${employeeId}`, { pass });
  }
}
