import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CertificationResponse } from '../models';

/**
 * Certifications are auto-issued on course completion — there is no client
 * method to issue one manually any more. This service is read-only.
 */
@Injectable({ providedIn: 'root' })
export class CertificationService {
  private base = `${environment.apiUrl}/Certification`;
  constructor(private http: HttpClient) {}

  /** Org-wide cert list — Admin, HR, Manager, Trainer. */
  getAll() {
    return this.http.get<CertificationResponse[]>(`${this.base}/certifications`);
  }

  /** Caller's own certs — Employee only. */
  getMy() {
    return this.http.get<CertificationResponse[]>(`${this.base}/my`);
  }

  /** Downloads a single certificate as a PDF. */
  download(id: number) {
    return this.http.get(`${this.base}/${id}/download`, { responseType: 'blob' });
  }
}
