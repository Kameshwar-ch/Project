import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AssessmentService } from '../../../core/services/assessment.service';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { SharedModule } from '../../../shared/shared.module';
import { Course, CourseModule, EmployeeAssessment } from '../../../core/models';

@Component({
  selector: 'app-module-detail',
  standalone: true,
  imports: [SharedModule, RouterLink],
  templateUrl: './module-detail.html',
})
export class ModuleDetailComponent implements OnInit {
  private route         = inject(ActivatedRoute);
  private router        = inject(Router);
  readonly auth         = inject(AuthService);
  private assessmentSvc = inject(AssessmentService);
  private enrollmentSvc = inject(EnrollmentService);

  courseId     = signal(0);
  moduleId     = signal(0);
  enrollmentId = signal(0);
  module       = signal<CourseModule | null>(null);
  course       = signal<Course | null>(null);
  assessments  = signal<EmployeeAssessment[]>([]);
  isCompleted  = signal(false);
  loading      = signal(true);
  markingDone  = signal(false);
  toast        = signal('');
  toastType    = signal<'success' | 'error'>('success');

  /** True only when every assessment for this module has been passed */
  readonly allAssessmentsPassed = computed(() => {
    const list = this.assessments();
    if (list.length === 0) return true;
    return list.every(a => a.resultStatus === 'Pass');
  });

  /** Assessments not yet passed (for the warning list) */
  readonly unpassed = computed(() =>
    this.assessments().filter(a => a.resultStatus !== 'Pass')
  );

  ngOnInit() {
    const courseId  = Number(this.route.snapshot.paramMap.get('courseId'));
    const moduleId  = Number(this.route.snapshot.paramMap.get('moduleId'));
    this.courseId.set(courseId);
    this.moduleId.set(moduleId);

    const state = history.state as {
      module?: CourseModule;
      course?: Course;
      enrollmentId?: number;
    };

    if (state?.module)       this.module.set(state.module);
    if (state?.course)       this.course.set(state.course);
    if (state?.enrollmentId) this.enrollmentId.set(state.enrollmentId);

    // Load assessments for this specific module via dedicated endpoint
    this.assessmentSvc.getModuleAssessments(moduleId).subscribe({
      next: data => {
        this.assessments.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    // Check if this module is already marked complete
    const resolveProgress = (enrollmentId: number) => {
      this.enrollmentSvc.getModuleProgress(enrollmentId).subscribe({
        next: data => this.isCompleted.set(data.completedModuleIds.includes(moduleId)),
        error: () => {}
      });
    };

    if (state?.enrollmentId) {
      resolveProgress(state.enrollmentId);
    } else {
      this.enrollmentSvc.getAll().subscribe({
        next: data => {
          const e = data.find(e => e.courseId === courseId);
          if (e) {
            this.enrollmentId.set(e.enrollmentId);
            resolveProgress(e.enrollmentId);
          }
        },
        error: () => {}
      });
    }
  }

  markComplete() {
    const enrollmentId = this.enrollmentId();
    const moduleId     = this.moduleId();
    if (!enrollmentId) return;
    this.markingDone.set(true);
    this.enrollmentSvc.markModuleComplete(enrollmentId, moduleId).subscribe({
      next: res => {
        this.isCompleted.set(true);
        this.markingDone.set(false);
        if (res.courseCompleted) {
          this.showToast('Course completed! Your certificate is now available.', 'success');
        } else {
          this.showToast('Module marked as complete!', 'success');
        }
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to mark complete.', 'error');
        this.markingDone.set(false);
      }
    });
  }

  goToAssessments() {
    this.router.navigate(['/assessments'], { queryParams: { courseId: this.courseId() } });
  }

  back() {
    this.router.navigate(
      ['/catalog', this.courseId(), 'modules'],
      { state: { course: this.course(), enrollmentId: this.enrollmentId() } }
    );
  }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg); this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3500);
  }
}
