import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { EnrollmentResponse, AttendancePreview, AttendanceRequestItem, EnrollmentAttendanceHistory } from '../models';

@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private base = `${environment.apiUrl}/Enrollment`;
  private attendanceBase = `${environment.apiUrl}/Attendance`;
  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<EnrollmentResponse[]>(this.base);
  }

  enroll(courseId: number, employeeId: number) {
    return this.http.post<{ enrollmentId: number }>(this.base, { courseId, employeeId });
  }

  bulkEnroll(courseId: number, employeeIds: number[]) {
    return this.http.post<any>(`${this.base}/bulk`, { courseId, employeeIds });
  }

  updateEnrollmentStatus(enrollmentId: number, status: boolean) {
    return this.http.patch<{ message: string }>(`${this.base}/${enrollmentId}/status`, { status });
  }

  getAttendancePreview(courseId: number, date: string) {
    const params = new HttpParams().set('date', date);
    return this.http.get<AttendancePreview>(`${this.attendanceBase}/course/${courseId}`, { params });
  }

  markAttendance(courseId: number, attendanceDate: string, records: { enrollmentID: number; status: number }[]) {
    return this.http.post<any>(`${this.attendanceBase}/Mark-Attendance`, {
      courseID: courseId,
      attendanceDate,
      records
    });
  }

  // ── Attendance Request Workflow ───────────────────────────────────────────

  createAttendanceRequest(enrollmentID: number, requestDate: string) {
    return this.http.post<AttendanceRequestItem>(`${this.attendanceBase}/request`, {
      enrollmentID,
      requestDate
    });
  }

  getMyAttendanceRequests() {
    return this.http.get<AttendanceRequestItem[]>(`${this.attendanceBase}/requests/my`);
  }

  getPendingRequestsForCourse(courseId: number) {
    return this.http.get<AttendanceRequestItem[]>(
      `${this.attendanceBase}/requests/course/${courseId}/pending`
    );
  }

  reviewAttendanceRequest(requestId: number, approved: boolean, note?: string) {
    return this.http.patch<AttendanceRequestItem>(
      `${this.attendanceBase}/requests/${requestId}/review`,
      { approved, note }
    );
  }

  getAttendanceHistory(enrollmentId: number) {
    return this.http.get<EnrollmentAttendanceHistory>(
      `${this.attendanceBase}/enrollment/${enrollmentId}`
    );
  }

  // ── Module Progress ───────────────────────────────────────────────────────

  markModuleComplete(enrollmentId: number, moduleId: number) {
    return this.http.post<{ message: string; courseCompleted: boolean }>(
      `${this.base}/${enrollmentId}/modules/${moduleId}/complete`, {}
    );
  }

  getModuleProgress(enrollmentId: number) {
    return this.http.get<{ completedModuleIds: number[] }>(
      `${this.base}/${enrollmentId}/modules/progress`
    );
  }
}
