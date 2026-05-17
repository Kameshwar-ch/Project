import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { User, RegisterRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private base = `${environment.apiUrl}/User`;
  constructor(private http: HttpClient) {}

  getAll()                    { return this.http.get<User[]>(`${this.base}/GetAll`); }
  getMe()                     { return this.http.get<User>(`${this.base}/me`); }
  getById(id: number)         { return this.http.get<User>(`${this.base}/${id}`); }
  create(payload: RegisterRequest) { return this.http.post<{ message: string }>(`${this.base}/Register`, payload); }
  update(id: number, payload: Partial<User>) { return this.http.put<string>(`${this.base}/update/${id}`, payload); }
  updateStatus(id: number, status: boolean) { return this.http.patch<{ message: string }>(`${this.base}/${id}/status`, { status }); }
  delete(id: number)          { return this.http.delete<{ message: string }>(`${this.base}/${id}`); }
}
