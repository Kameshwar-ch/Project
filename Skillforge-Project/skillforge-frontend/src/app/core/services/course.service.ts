import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Course, CreateCourseRequest, CourseModule, CreateModuleRequest, ApiResponse } from '../models';

@Injectable({ providedIn: 'root' })
export class CourseService {
  private base = `${environment.apiUrl}/Course`;
  constructor(private http: HttpClient) {}

  getAll(trainerId?: number) {
    let params = new HttpParams();
    if (trainerId != null) params = params.set('trainerId', trainerId);
    return this.http.get<Course[]>(this.base, { params });
  }

  getById(id: number) {
    return this.http.get<ApiResponse<Course>>(`${this.base}/${id}`)
      .pipe(map(res => (res as any)?.data ?? res));
  }

  create(payload: CreateCourseRequest) {
    return this.http.post<ApiResponse<Course>>(this.base, payload);
  }

  update(id: number, payload: { title: string; description: string; trainerID: number; duration: number; status: boolean }) {
    return this.http.put<{ message: string }>(`${this.base}/${id}`, payload);
  }

  updateStatus(id: number, status: boolean) {
    return this.http.patch<{ message: string }>(`${this.base}/${id}/status`, { status });
  }

  delete(id: number) {
    return this.http.delete<{ message: string }>(`${this.base}/${id}`);
  }

  getModules(courseId: number) {
    return this.http.get<CourseModule[]>(`${this.base}/${courseId}/modules`);
  }

  addModule(courseId: number, payload: CreateModuleRequest) {
    return this.http.post<{ message: string; moduleId: number }>(`${this.base}/${courseId}/modules`, payload);
  }

  updateModule(courseId: number, moduleId: number, payload: CreateModuleRequest) {
    return this.http.put<{ message: string }>(`${this.base}/${courseId}/modules/${moduleId}`, payload);
  }

  deleteModule(courseId: number, moduleId: number) {
    return this.http.delete<{ message: string }>(`${this.base}/${courseId}/modules/${moduleId}`);
  }
}
