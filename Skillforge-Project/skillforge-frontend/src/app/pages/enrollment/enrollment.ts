import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { CourseService } from '../../core/services/course.service';
import { UserService } from '../../core/services/user.service';
import { CertificationService } from '../../core/services/certification.service';
import { SharedModule } from '../../shared/shared.module';
import { EnrollmentResponse, Course, User, BulkEnrollResult, AttendanceRequestItem, EnrollmentAttendanceHistory, CertificationResponse } from '../../core/models';

interface AttendanceDraftRecord {
  enrollmentID: number;
  employeeName: string;
  courseStatus: string;
  status: 0 | 1;  // 0 = Present, 1 = Absent
}

@Component({
  selector: 'app-enrollment',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './enrollment.html',
})
export class EnrollmentComponent implements OnInit {
  readonly auth = inject(AuthService);
  private enrollmentService  = inject(EnrollmentService);
  private courseService      = inject(CourseService);
  private userService        = inject(UserService);
  private certificationSvc   = inject(CertificationService);

  readonly role = this.auth.userRole;

  enrollments: EnrollmentResponse[] = [];
  loading  = signal(true);
  error    = signal<string | null>(null);

  // ── Filters ───────────────────────────────────────────────────────────────
  statusFilter = signal('All');
  courseSearch = signal('');

  // ── Toast ─────────────────────────────────────────────────────────────────
  toast     = signal('');
  toastType = signal<'success' | 'error'>('success');

  // ── Role helpers ──────────────────────────────────────────────────────────
  get canAssign()  { return this.auth.hasRole('Manager', 'Admin'); }
  get canMark()    { return this.auth.hasRole('Trainer'); }
  get isManager()  { return this.auth.hasRole('Manager'); }
  get isAdmin()    { return this.auth.hasRole('Admin'); }
  get isEmployee() { return this.auth.hasRole('Employee'); }

  // ── Employee: flat filtered list ──────────────────────────────────────────
  get filtered(): EnrollmentResponse[] {
    let list = this.enrollments;
    const f = this.statusFilter();
    if (f !== 'All') list = list.filter(e => e.status === f);
    return list;
  }

  // ── Course-grouped view (Manager / Admin / Trainer) ───────────────────────
  get courseGroups(): { courseId: number; courseName: string; total: number; enrolled: number; waitlist: number; completed: number }[] {
    const map = new Map<number, { courseId: number; courseName: string; enrollments: EnrollmentResponse[] }>();
    for (const e of this.enrollments) {
      if (!map.has(e.courseId)) {
        map.set(e.courseId, { courseId: e.courseId, courseName: e.courseName, enrollments: [] });
      }
      map.get(e.courseId)!.enrollments.push(e);
    }
    const search = this.courseSearch().toLowerCase().trim();
    return Array.from(map.values())
      .filter(g => !search || g.courseName.toLowerCase().includes(search))
      .map(g => ({
        courseId:   g.courseId,
        courseName: g.courseName,
        total:      g.enrollments.length,
        enrolled:   g.enrollments.filter(e => e.status === 'Enrolled').length,
        waitlist:   g.enrollments.filter(e => e.status === 'Waitlist').length,
        completed:  g.enrollments.filter(e => e.status === 'Completed').length,
      }));
  }

  // ── Enrollees modal ───────────────────────────────────────────────────────
  showEnrolleesModal  = signal(false);
  enrolleesCourseId   = signal<number | null>(null);
  enrolleesCourseName = signal('');
  enrolleeSearch      = signal('');

  get enrolleesList(): EnrollmentResponse[] {
    const id = this.enrolleesCourseId();
    if (!id) return [];
    const s = this.enrolleeSearch().toLowerCase().trim();
    return this.enrollments
      .filter(e => e.courseId === id)
      .filter(e => !s || e.employeeName.toLowerCase().includes(s));
  }

  openEnrollees(courseId: number, courseName: string) {
    this.enrolleesCourseId.set(courseId);
    this.enrolleesCourseName.set(courseName);
    this.enrolleeSearch.set('');
    this.showEnrolleesModal.set(true);
  }

  closeEnrolleesModal() {
    this.showEnrolleesModal.set(false);
    this.enrolleesCourseId.set(null);
    this.enrolleeSearch.set('');
  }

  // ── Request Attendance modal (Employee) ───────────────────────────────────
  showReqModal       = signal(false);
  reqEnrollment      = signal<EnrollmentResponse | null>(null);
  reqDate            = signal(this.todayIso());
  reqMinDate         = signal(this.todayIso());
  reqSubmitting      = signal(false);

  // ── My Attendance Requests panel (Employee) ───────────────────────────────
  myRequests          = signal<AttendanceRequestItem[]>([]);   // pending-only (badge)
  allMyRequests       = signal<AttendanceRequestItem[]>([]);   // full history for modal
  myRequestsLoading   = signal(false);
  showMyRequestsModal = signal(false);

  // ── Pending Requests panel (Trainer) ─────────────────────────────────────
  pendingRequests    = signal<AttendanceRequestItem[]>([]);
  pendingLoading     = signal(false);
  reviewingId        = signal<number | null>(null);

  // ── Enrollment status update (Trainer) ───────────────────────────────────
  updatingStatusId   = signal<number | null>(null);

  // ── Attendance History modal ──────────────────────────────────────────────
  showHistoryModal   = signal(false);
  historyData        = signal<EnrollmentAttendanceHistory | null>(null);
  historyLoading     = signal(false);
  historyError       = signal<string | null>(null);

  // ── Mark Attendance modal ─────────────────────────────────────────────────
  showAttModal   = signal(false);
  attCourse      = signal<{ id: number; name: string } | null>(null);
  attDate        = signal(this.todayIso());
  attRecords     = signal<AttendanceDraftRecord[]>([]);
  previewLoading = signal(false);
  previewError   = signal<string | null>(null);
  attSubmitting  = signal(false);

  // ── Certificate download ──────────────────────────────────────────────────
  myCerts         = signal<CertificationResponse[]>([]);
  downloadingId   = signal<number | null>(null);

  // ── Bulk-Assign modal ─────────────────────────────────────────────────────
  showBulkModal     = signal(false);
  bulkCourses       = signal<Course[]>([]);
  bulkEmployees     = signal<User[]>([]);
  bulkDataLoading   = signal(false);
  bulkCourseId      = signal<number | null>(null);
  selectedEmployees = signal<Set<number>>(new Set());
  bulkSubmitting    = signal(false);
  bulkResult        = signal<BulkEnrollResult | null>(null);

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit() {
    this.enrollmentService.getAll().subscribe({
      next:  data => {
        this.enrollments = data;
        this.loading.set(false);
        // Auto-load pending requests for all Trainer courses
        if (this.canMark && data.length > 0) {
          this.loadPendingRequests();
        }
        // Auto-load my requests for Employee
        if (this.isEmployee) {
          this.loadMyRequests();
          this.loadMyCertifications();
        }
      },
      error: err  => { this.error.set(err?.error?.message ?? 'Failed to load enrollments.'); this.loading.set(false); }
    });
  }

  // ── Attendance modal ──────────────────────────────────────────────────────
  openMarkAttendance(course: { id: number; name: string }) {
    this.attCourse.set({ id: course.id, name: course.name });
    this.attDate.set(this.todayIso());
    this.attRecords.set([]);
    this.previewError.set(null);
    this.showAttModal.set(true);
    this.loadPreview();
  }

  closeAttModal() {
    this.showAttModal.set(false);
    this.attCourse.set(null);
    this.attRecords.set([]);
  }

  loadPreview() {
    const course = this.attCourse();
    if (!course) return;
    this.previewLoading.set(true);
    this.previewError.set(null);
    this.enrollmentService.getAttendancePreview(course.id, this.attDate()).subscribe({
      next: preview => {
        this.attRecords.set(preview.records.map(r => ({
          enrollmentID: r.enrollmentID,
          employeeName: r.employeeName,
          courseStatus: r.courseStatus,
          status: 0
        })));
        this.previewLoading.set(false);
      },
      error: err => {
        this.previewError.set(err?.error?.message ?? 'Failed to load attendance preview.');
        this.previewLoading.set(false);
      }
    });
  }

  setStatus(enrollmentID: number, status: 0 | 1) {
    this.attRecords.update(records =>
      records.map(r => r.enrollmentID === enrollmentID ? { ...r, status } : r)
    );
  }

  submitAttendance() {
    const course = this.attCourse();
    if (!course || this.attRecords().length === 0) return;
    this.attSubmitting.set(true);
    const records = this.attRecords().map(r => ({ enrollmentID: r.enrollmentID, status: r.status }));
    this.enrollmentService.markAttendance(course.id, this.attDate(), records).subscribe({
      next: () => {
        this.closeAttModal();
        this.showToast('Attendance marked successfully!', 'success');
        this.attSubmitting.set(false);
        this.reload();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to mark attendance.', 'error');
        this.attSubmitting.set(false);
      }
    });
  }

  // ── Request Attendance modal (Employee) ──────────────────────────────────
  openRequestAttendance(enrollment: EnrollmentResponse) {
    this.reqEnrollment.set(enrollment);

    // Min date: the later of (enrollment date) and (today - 7 days)
    const enrolledOn  = new Date(enrollment.enrollmentDate);
    const sevenDaysAgo = new Date();
    sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
    const minDate = enrolledOn > sevenDaysAgo ? enrolledOn : sevenDaysAgo;
    this.reqMinDate.set(minDate.toISOString().split('T')[0]);

    this.reqDate.set(this.todayIso());
    this.showReqModal.set(true);
  }

  closeReqModal() {
    this.showReqModal.set(false);
    this.reqEnrollment.set(null);
  }

  submitAttendanceRequest() {
    const enrollment = this.reqEnrollment();
    if (!enrollment) return;
    this.reqSubmitting.set(true);
    this.enrollmentService.createAttendanceRequest(enrollment.enrollmentId, this.reqDate()).subscribe({
      next: () => {
        this.closeReqModal();
        this.showToast('Attendance request submitted successfully!', 'success');
        this.reqSubmitting.set(false);
        this.loadMyRequests();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to submit request.', 'error');
        this.reqSubmitting.set(false);
      }
    });
  }

  loadMyRequests() {
    this.myRequestsLoading.set(true);
    this.enrollmentService.getMyAttendanceRequests().subscribe({
      next: data => {
        this.allMyRequests.set(data);
        this.myRequests.set(data.filter(r => r.status === 'Pending'));
        this.myRequestsLoading.set(false);
      },
      error: () => { this.myRequestsLoading.set(false); }
    });
  }

  openMyRequests() {
    this.showMyRequestsModal.set(true);
    this.loadMyRequests();
  }

  closeMyRequestsModal() {
    this.showMyRequestsModal.set(false);
  }

  // ── Attendance History modal ──────────────────────────────────────────────
  openAttendanceHistory(enrollment: EnrollmentResponse) {
    this.historyData.set(null);
    this.historyError.set(null);
    this.historyLoading.set(true);
    this.showHistoryModal.set(true);
    this.enrollmentService.getAttendanceHistory(enrollment.enrollmentId).subscribe({
      next:  data => { this.historyData.set(data); this.historyLoading.set(false); },
      error: err  => { this.historyError.set(err?.error?.message ?? 'Failed to load attendance.'); this.historyLoading.set(false); }
    });
  }

  closeHistoryModal() {
    this.showHistoryModal.set(false);
    this.historyData.set(null);
  }

  // ── Pending Requests (Trainer) ────────────────────────────────────────────
  loadPendingRequests(courseId?: number) {
    // If no courseId supplied, use all courses from loaded enrollments
    const courseIds = courseId
      ? [courseId]
      : [...new Set(this.enrollments.map(e => e.courseId))];

    if (courseIds.length === 0) return;
    this.pendingLoading.set(true);
    this.pendingRequests.set([]);

    let remaining = courseIds.length;
    const all: AttendanceRequestItem[] = [];

    courseIds.forEach(id => {
      this.enrollmentService.getPendingRequestsForCourse(id).subscribe({
        next: data => {
          all.push(...data);
          remaining--;
          if (remaining === 0) {
            this.pendingRequests.set(all.sort((a, b) =>
              new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()));
            this.pendingLoading.set(false);
          }
        },
        error: () => {
          remaining--;
          if (remaining === 0) this.pendingLoading.set(false);
        }
      });
    });
  }

  reviewRequest(requestId: number, approved: boolean) {
    this.reviewingId.set(requestId);
    this.enrollmentService.reviewAttendanceRequest(requestId, approved).subscribe({
      next: () => {
        this.pendingRequests.update(list => list.filter(r => r.requestID !== requestId));
        this.reviewingId.set(null);
        this.showToast(approved ? 'Request approved — attendance marked!' : 'Request rejected.', 'success');
        if (approved) this.reload();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to review request.', 'error');
        this.reviewingId.set(null);
      }
    });
  }

  // ── Enrollment status update (Trainer) ───────────────────────────────────
  updateEnrollmentStatus(enrollment: EnrollmentResponse, approve: boolean) {
    this.updatingStatusId.set(enrollment.enrollmentId);
    this.enrollmentService.updateEnrollmentStatus(enrollment.enrollmentId, approve).subscribe({
      next: () => {
        // Update the local list so UI reflects immediately without a full reload
        this.enrollments = this.enrollments.map(e =>
          e.enrollmentId === enrollment.enrollmentId
            ? { ...e, status: approve ? 'Enrolled' : 'Waitlist' }
            : e
        );
        this.updatingStatusId.set(null);
        this.showToast(approve ? 'Employee approved — status set to Enrolled.' : 'Employee moved to Waitlist.', 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to update status.', 'error');
        this.updatingStatusId.set(null);
      }
    });
  }

  // ── Bulk-Assign modal ─────────────────────────────────────────────────────
  openBulkAssign() {
    this.bulkCourseId.set(null);
    this.selectedEmployees.set(new Set());
    this.bulkResult.set(null);
    this.showBulkModal.set(true);

    if (this.bulkCourses().length === 0 || this.bulkEmployees().length === 0) {
      this.bulkDataLoading.set(true);
      let coursesLoaded = false, employeesLoaded = false;
      const check = () => { if (coursesLoaded && employeesLoaded) this.bulkDataLoading.set(false); };

      this.courseService.getAll().subscribe({
        next: data => { this.bulkCourses.set(data.filter(c => c.status)); coursesLoaded = true; check(); },
        error: () => { coursesLoaded = true; check(); }
      });

      this.userService.getAll().subscribe({
        next: users => { this.bulkEmployees.set(users.filter(u => u.roleName === 'Employee' && u.status)); employeesLoaded = true; check(); },
        error: () => { employeesLoaded = true; check(); }
      });
    }
  }

  closeBulkModal() {
    this.showBulkModal.set(false);
    this.bulkResult.set(null);
    this.selectedEmployees.set(new Set());
    this.bulkCourseId.set(null);
  }

  toggleEmployee(id: number) {
    this.selectedEmployees.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  isEmployeeSelected(id: number): boolean {
    return this.selectedEmployees().has(id);
  }

  toggleSelectAll() {
    const all = this.bulkEmployees().map(e => e.userID);
    const current = this.selectedEmployees();
    if (current.size === all.length) {
      this.selectedEmployees.set(new Set());
    } else {
      this.selectedEmployees.set(new Set(all));
    }
  }

  get allSelected(): boolean {
    return this.bulkEmployees().length > 0 &&
           this.selectedEmployees().size === this.bulkEmployees().length;
  }

  submitBulkEnroll() {
    const courseId = this.bulkCourseId();
    const employeeIds = [...this.selectedEmployees()];
    if (!courseId || employeeIds.length === 0) return;
    this.bulkSubmitting.set(true);
    this.bulkResult.set(null);
    this.enrollmentService.bulkEnroll(courseId, employeeIds).subscribe({
      next: (result: BulkEnrollResult) => {
        this.bulkResult.set(result);
        this.bulkSubmitting.set(false);
        this.selectedEmployees.set(new Set());
        this.bulkCourseId.set(null);
        if (result.succeeded > 0) this.reload();
      },
      error: err => {
        this.bulkResult.set(err?.error ?? null);
        this.showToast(err?.error?.message ?? 'Bulk enroll failed.', 'error');
        this.bulkSubmitting.set(false);
      }
    });
  }

  // ── Certificate download ──────────────────────────────────────────────────
  loadMyCertifications() {
    this.certificationSvc.getMy().subscribe({
      next: data => this.myCerts.set(data),
      error: () => {}
    });
  }

  certForEnrollment(enrollment: EnrollmentResponse): CertificationResponse | undefined {
    return this.myCerts().find(c => c.courseId === enrollment.courseId);
  }

  downloadCert(cert: CertificationResponse) {
    this.downloadingId.set(cert.certificationId);
    this.certificationSvc.download(cert.certificationId).subscribe({
      next: (blob: Blob) => {
        const url = URL.createObjectURL(blob);
        const a   = document.createElement('a');
        a.href     = url;
        a.download = `certificate_${cert.certificationId}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
        this.downloadingId.set(null);
      },
      error: () => {
        this.showToast('Failed to download certificate.', 'error');
        this.downloadingId.set(null);
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  private reload() {
    this.enrollmentService.getAll().subscribe({
      next:  data => this.enrollments = data,
      error: ()   => {}
    });
  }

  todayIso(): string {
    return new Date().toISOString().split('T')[0];
  }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg); this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3500);
  }
}
