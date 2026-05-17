import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { AssessmentService } from '../../core/services/assessment.service';
import { CertificationService } from '../../core/services/certification.service';
import { CourseService } from '../../core/services/course.service';
import { UserService } from '../../core/services/user.service';
import { SharedModule } from '../../shared/shared.module';
import { AssessmentResponse, CertificationResponse, Course, CourseModule, EmployeeAssessment, PendingResult, User } from '../../core/models';

// Assessment type enum values matching the backend (Quiz=0, Exam=1, Practical=2)
const ASSESSMENT_TYPES = [
  { label: 'Quiz',      value: 0 },
  { label: 'Exam',      value: 1 },
  { label: 'Practical', value: 2 },
];

@Component({
  selector: 'app-assessments',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './assessments.html',
})
export class AssessmentsComponent implements OnInit {
  readonly auth    = inject(AuthService);
  private fb       = inject(FormBuilder);
  private assessmentService    = inject(AssessmentService);
  private certificationService = inject(CertificationService);
  private courseService  = inject(CourseService);
  private userService = inject(UserService);
  private route = inject(ActivatedRoute);

  readonly assessmentTypes = ASSESSMENT_TYPES;

  // Only Trainers create / grade assessments. Admin no longer has an entry
  // point — they oversee, but don't author content.
  get canCreate() { return this.auth.hasRole('Trainer'); }
  get isTrainer()  { return this.auth.hasRole('Trainer'); }
  get isEmployee() { return this.auth.hasRole('Employee'); }
  // (canIssue removed — certifications are auto-issued on course completion.)

  // ── Data ──────────────────────────────────────────────────────────────────
  assessments: AssessmentResponse[]      = [];
  certifications: CertificationResponse[] = [];
  courses: Course[]   = [];

  loadingAssessments = signal(true);
  loadingCerts       = signal(true);
  assessmentError    = signal<string | null>(null);
  certError          = signal<string | null>(null);

  // ── Employee: My Assessments ──────────────────────────────────────────────
  myAssessments        = signal<EmployeeAssessment[]>([]);
  loadingMyAssessments = signal(true);
  showPendingOnly = signal(false);
  filterCourseId = signal<number | null>(null);
  courseFilterValue = signal<string>('All');
  readonly courseNames = computed(() => {
    const names = [...new Set(this.myAssessments().map(a => a.courseName))];
    return names.sort();
  });

  // ── Trainer/Admin: Assessment filter ─────────────────────────────────────
  assessmentCourseFilter = signal<string>('All');
  get assessmentCourseNames(): string[] {
    return [...new Set(this.assessments.map(a => a.courseName))].sort();
  }

  get filteredAssessments(): AssessmentResponse[] {
    const filter = this.assessmentCourseFilter();
    if (filter === 'All') return this.assessments;
    return this.assessments.filter(a => a.courseName === filter);
  }

  // ── Certification name filter ────────────────────────────────────────────
  certCourseFilter = signal<string>('All');
  certNameFilter = signal<string>('All');

  get certCourseNames(): string[] {
    return [...new Set(this.certifications.map(c => c.courseName))].sort();
  }

  get certEmployeeNames(): string[] {
    return [...new Set(this.certifications.map(c => c.employeeName))].sort();
  }
  readonly filteredMyAssessments = computed(() => {
    let list = this.myAssessments();
    const cid = this.filterCourseId();
    if (cid) list = list.filter(a => a.courseId === cid);
    const courseFilter = this.courseFilterValue();
    if (courseFilter !== 'All') list = list.filter(a => a.courseName === courseFilter);
    if (this.showPendingOnly()) list = list.filter(a => !a.isDone);
    return list;
  });

  // ── Trainer: Pending Results ───────────────────────────────────────────────
  pendingResults        = signal<PendingResult[]>([]);
  pendingResultsLoading = signal(false);
  evaluatingId          = signal<string | null>(null);

  // ── Evaluate modal (Trainer) ───────────────────────────────────────────────
  showEvalModal      = signal(false);
  evalTarget         = signal<PendingResult | null>(null);
  evalVerdict        = signal<'Pass' | 'Fail' | null>(null);
  evalSubmitting     = signal(false);

  // ── Take Assessment modal (employee) ──────────────────────────────────────
  showTakeModal        = signal(false);
  takeLoading          = signal(false);
  selectedMyAssessment = signal<EmployeeAssessment | null>(null);

  takeForm = this.fb.group({
    score: [null as number | null, [Validators.required, Validators.min(0)]],
  });

  // ── Certification filter ──────────────────────────────────────────────────
  certStatusFilter = signal<'All' | 'Active' | 'Expiring' | 'Expired'>('All');

  get filteredCerts(): CertificationResponse[] {
    const filter = this.certStatusFilter();
    let list = this.certifications;

    if (this.auth.hasRole('Employee')) {
      const myId = this.auth.userId();
      list = list.filter(c => c.employeeId === myId);
    }

    const courseFilter = this.certCourseFilter();
    if (courseFilter !== 'All') list = list.filter(c => c.courseName === courseFilter);

    const nameFilter = this.certNameFilter();
    if (nameFilter !== 'All') list = list.filter(c => c.employeeName === nameFilter);

    if (filter === 'All') return list;
    const now = Date.now();
    const thirtyDays = 30 * 24 * 60 * 60 * 1000;
    return list.filter(c => {
      if (filter === 'Expired') return c.status?.toLowerCase() === 'expired'
        || new Date(c.expiryDate).getTime() < now;
      if (filter === 'Expiring') {
        const exp = new Date(c.expiryDate).getTime();
        return exp >= now && exp <= now + thirtyDays;
      }
      return c.status?.toLowerCase() === 'active'
        && new Date(c.expiryDate).getTime() >= now;
    });
  }

  // ── Toast ─────────────────────────────────────────────────────────────────
  toast     = signal('');
  toastType = signal<'success' | 'error'>('success');

  // ── Create Assessment modal ───────────────────────────────────────────────
  showCreateModal = signal(false);
  createLoading   = signal(false);
  coursesLoading  = signal(false);
  modulesLoading  = signal(false);
  createModules   = signal<CourseModule[]>([]);

  createForm = this.fb.group({
    courseId:     [null as number | null, Validators.required],
    moduleId:     [null as number | null],
    type:         [null as number | null, Validators.required],
    maxScore:     [null as number | null, [Validators.required, Validators.min(1), Validators.max(100)]],
    passingScore: [null as number | null, [Validators.required, Validators.min(1), Validators.max(100)]],
  });


  // (Issue Certification modal removed — certifications are auto-issued when
  // a course is completed. There is no manual "issue cert" UI any more.)

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit() {
    const courseIdParam = this.route.snapshot.queryParamMap.get('courseId');
    if (courseIdParam) this.filterCourseId.set(Number(courseIdParam));

    if (this.isEmployee) {
      this.loadMyAssessments();
    } else {
      this.loadAssessments();
      if (this.isTrainer) this.loadPendingResults();
    }
    this.loadCertifications();
  }

  private loadPendingResults() {
    this.pendingResultsLoading.set(true);
    this.assessmentService.getPendingResults().subscribe({
      next:  data => { this.pendingResults.set(data); this.pendingResultsLoading.set(false); },
      error: ()   => this.pendingResultsLoading.set(false)
    });
  }

  openEval(result: PendingResult) {
    this.evalTarget.set(result);
    this.evalVerdict.set(null);
    this.showEvalModal.set(true);
  }

  closeEval() {
    this.showEvalModal.set(false);
    this.evalTarget.set(null);
    this.evalVerdict.set(null);
  }

  submitEval() {
    const target  = this.evalTarget();
    const verdict = this.evalVerdict();
    if (!target || !verdict) return;
    this.evalSubmitting.set(true);
    this.assessmentService.evaluateResult(target.assessmentId, target.employeeId, verdict === 'Pass').subscribe({
      next: () => {
        this.pendingResults.update(list =>
          list.filter(r => !(r.assessmentId === target.assessmentId && r.employeeId === target.employeeId)));
        this.closeEval();
        this.evalSubmitting.set(false);
        this.showToast(`Result marked as ${verdict}!`, 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to evaluate result.', 'error');
        this.evalSubmitting.set(false);
      }
    });
  }

  private loadMyAssessments() {
    this.loadingMyAssessments.set(true);
    this.assessmentService.getMyAssessments().subscribe({
      next:  data => { this.myAssessments.set(data); this.loadingMyAssessments.set(false); },
      error: ()   => this.loadingMyAssessments.set(false)
    });
  }

  openTake(assessment: EmployeeAssessment) {
    this.selectedMyAssessment.set(assessment);
    this.takeForm.reset();
    this.takeForm.get('score')!.setValidators([Validators.required, Validators.min(0), Validators.max(assessment.maxScore)]);
    this.takeForm.get('score')!.updateValueAndValidity();
    this.showTakeModal.set(true);
  }

  closeTake() {
    this.showTakeModal.set(false);
    this.selectedMyAssessment.set(null);
    this.takeForm.reset();
  }

  submitTake() {
    if (this.takeForm.invalid) { this.takeForm.markAllAsTouched(); return; }
    const assessment = this.selectedMyAssessment();
    if (!assessment) return;
    this.takeLoading.set(true);
    const score = Number(this.takeForm.value.score);
    this.assessmentService.selfSubmit(assessment.assessmentId, score).subscribe({
      next: () => {
        this.closeTake();
        this.showToast('Assessment submitted successfully!', 'success');
        this.takeLoading.set(false);
        this.loadMyAssessments();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to submit assessment.', 'error');
        this.takeLoading.set(false);
      }
    });
  }

  private loadAssessments() {
    this.loadingAssessments.set(true);
    this.assessmentService.getAll().subscribe({
      next:  data => { this.assessments = data; this.loadingAssessments.set(false); },
      error: err  => { this.assessmentError.set(err?.error?.message ?? 'Failed to load assessments.'); this.loadingAssessments.set(false); }
    });
  }

  private loadCertifications() {
    this.loadingCerts.set(true);
    const source$ = this.isEmployee
      ? this.certificationService.getMy()
      : this.certificationService.getAll();
    source$.subscribe({
      next: data => {
        if (this.isTrainer) {
          const trainerId = this.auth.userId();
          this.courseService.getAll(trainerId ?? undefined).subscribe({
            next: courses => {
              const myCourseNames = new Set(courses.map(c => c.title));
              this.certifications = data.filter(c => myCourseNames.has(c.courseName));
              this.loadingCerts.set(false);
            },
            error: () => { this.certifications = data; this.loadingCerts.set(false); }
          });
        } else {
          this.certifications = data;
          this.loadingCerts.set(false);
        }
      },
      error: err => { this.certError.set(err?.error?.message ?? 'Failed to load certifications.'); this.loadingCerts.set(false); }
    });
  }

  // ── Create Assessment ─────────────────────────────────────────────────────
  openCreate() {
    this.showCreateModal.set(true);
    if (this.courses.length === 0) {
      this.coursesLoading.set(true);
      const trainerId = this.isTrainer ? (this.auth.userId() ?? undefined) : undefined;
      this.courseService.getAll(trainerId).subscribe({
        next:  data => { this.courses = data.filter(c => !c.status); this.coursesLoading.set(false); },
        error: ()   => this.coursesLoading.set(false)
      });
    }
  }

  onCreateCourseChange(courseId: number | null | undefined) {
    this.createForm.patchValue({ moduleId: null });
    this.createModules.set([]);
    if (!courseId) return;
    this.modulesLoading.set(true);
    this.courseService.getModules(Number(courseId)).subscribe({
      next: mods => { this.createModules.set(mods); this.modulesLoading.set(false); },
      error: ()   => this.modulesLoading.set(false)
    });
  }

  closeCreate() {
    this.showCreateModal.set(false);
    this.createForm.reset();
    this.createModules.set([]);
  }

  submitCreate() {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.createLoading.set(true);
    const { courseId, moduleId, type, maxScore, passingScore } = this.createForm.value as any;
    this.assessmentService.create(Number(courseId), Number(type), Number(maxScore), Number(passingScore), moduleId ? Number(moduleId) : null).subscribe({
      next: () => {
        this.closeCreate();
        this.showToast('Assessment created successfully!', 'success');
        this.createLoading.set(false);
        this.loadAssessments();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to create assessment.', 'error');
        this.createLoading.set(false);
      }
    });
  }

  // (openIssue/closeIssue/submitIssue removed — certifications are auto-issued
  // when a course is completed; no manual issuance UI any more.)

  // ── Export Certifications (CSV) ───────────────────────────────────────────
  exportCerts() {
    const rows = [
      ['Employee', 'Course', 'Description', 'Issued Date', 'Expiry Date', 'Status'],
      ...this.filteredCerts.map(c => [
        c.employeeName,
        c.courseName,
        c.courseDescription ?? '',
        c.issueDate,
        c.expiryDate,
        c.status,
      ])
    ];
    const csv = rows.map(r => r.map(v => `"${String(v).replace(/"/g, '""')}"`).join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `certifications-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  // ── Download Certificate (print-to-PDF) ──────────────────────────────────
  downloadCert(cert: CertificationResponse) {
    const html = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>Certificate – ${cert.courseName}</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@700&family=Inter:wght@400;600&display=swap');
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { background: #fff; display: flex; align-items: center; justify-content: center; min-height: 100vh; font-family: 'Inter', sans-serif; }
    .cert {
      width: 800px; padding: 60px 80px; border: 12px solid #1a1a2e;
      outline: 4px solid #e2b04a; outline-offset: -20px;
      text-align: center; position: relative;
    }
    .logo { font-size: 28px; font-weight: 700; letter-spacing: 3px; color: #1a1a2e; text-transform: uppercase; margin-bottom: 6px; }
    .subtitle { font-size: 12px; letter-spacing: 5px; text-transform: uppercase; color: #6b7280; margin-bottom: 40px; }
    .presents { font-size: 13px; color: #6b7280; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 2px; }
    .title { font-family: 'Playfair Display', serif; font-size: 42px; color: #1a1a2e; margin-bottom: 4px; }
    .of { font-size: 13px; color: #6b7280; margin-bottom: 24px; text-transform: uppercase; letter-spacing: 2px; }
    .name { font-family: 'Playfair Display', serif; font-size: 36px; color: #e2b04a; border-bottom: 2px solid #e2b04a; display: inline-block; padding-bottom: 4px; margin-bottom: 28px; }
    .body { font-size: 14px; color: #374151; line-height: 1.8; max-width: 500px; margin: 0 auto 32px; }
    .course { font-weight: 600; color: #1a1a2e; font-size: 16px; }
    .meta { display: flex; justify-content: space-around; margin-top: 40px; }
    .meta-item { text-align: center; }
    .meta-label { font-size: 11px; letter-spacing: 2px; text-transform: uppercase; color: #9ca3af; }
    .meta-value { font-size: 13px; font-weight: 600; color: #1a1a2e; margin-top: 2px; }
    .seal { width: 80px; height: 80px; border-radius: 50%; background: #1a1a2e; color: #e2b04a;
            display: flex; flex-direction: column; align-items: center; justify-content: center;
            margin: 0 auto 32px; font-size: 10px; font-weight: 700; letter-spacing: 1px; text-transform: uppercase; }
    .seal-inner { font-size: 22px; }
    @media print {
      body { margin: 0; }
      .cert { border: 12px solid #1a1a2e !important; outline: 4px solid #e2b04a !important; }
    }
  </style>
</head>
<body>
<div class="cert">
  <div class="logo">SkillForge</div>
  <div class="subtitle">Learning & Development Platform</div>
  <div class="seal"><div class="seal-inner">★</div>Certified</div>
  <div class="presents">This is to certify that</div>
  <div class="name">${cert.employeeName}</div>
  <div class="of">has successfully completed</div>
  <div class="course">${cert.courseName}</div>
  <div class="body">${cert.courseDescription || 'and has demonstrated the required knowledge and skills as assessed by the SkillForge platform.'}</div>
  <div class="meta">
    <div class="meta-item">
      <div class="meta-label">Issue Date</div>
      <div class="meta-value">${new Date(cert.issueDate).toLocaleDateString('en-GB', { day:'numeric', month:'long', year:'numeric' })}</div>
    </div>
    <div class="meta-item">
      <div class="meta-label">Certificate ID</div>
      <div class="meta-value">#SF-${String(cert.certificationId).padStart(6, '0')}</div>
    </div>
    <div class="meta-item">
      <div class="meta-label">Valid Until</div>
      <div class="meta-value">${new Date(cert.expiryDate).toLocaleDateString('en-GB', { day:'numeric', month:'long', year:'numeric' })}</div>
    </div>
  </div>
</div>
<script>window.onload = () => { window.print(); }<\/script>
</body>
</html>`;

    const win = window.open('', '_blank');
    if (win) {
      win.document.write(html);
      win.document.close();
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  isExpiringSoon(cert: CertificationResponse): boolean {
    const now = Date.now();
    const exp = new Date(cert.expiryDate).getTime();
    return exp >= now && exp <= now + 30 * 24 * 60 * 60 * 1000;
  }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg); this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3500);
  }
}
