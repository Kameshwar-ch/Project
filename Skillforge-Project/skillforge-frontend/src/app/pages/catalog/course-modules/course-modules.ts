import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CourseService } from '../../../core/services/course.service';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { SharedModule } from '../../../shared/shared.module';
import { Course, CourseModule } from '../../../core/models';

@Component({
  selector: 'app-course-modules',
  standalone: true,
  imports: [SharedModule, RouterLink],
  templateUrl: './course-modules.html',
})
export class CourseModulesComponent implements OnInit {
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  readonly auth          = inject(AuthService);
  private courseService  = inject(CourseService);
  private enrollmentSvc  = inject(EnrollmentService);

  courseId     = signal(0);
  enrollmentId = signal(0);
  course       = signal<Course | null>(null);
  modules      = signal<CourseModule[]>([]);
  completed    = signal<Set<number>>(new Set());
  loading      = signal(true);
  progressLoading = signal(false);
  markingId    = signal<number | null>(null);
  toast        = signal('');
  toastType    = signal<'success' | 'error'>('success');
  courseCompleted = signal(false);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('courseId'));
    this.courseId.set(id);

    const state = history.state as { course?: Course; enrollmentId?: number };
    if (state?.course) this.course.set(state.course);
    if (state?.enrollmentId) this.enrollmentId.set(state.enrollmentId);

    // Load modules
    // Log course access (this triggers the "CourseAccessed" audit log in backend)
    this.courseService.getById(id).subscribe({
      next: course => {
        if (!this.course()) this.course.set(course);
      },
      error: () => { }
    });

    // Load modules
    this.courseService.getModules(id).subscribe({
      next: data => { this.modules.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });

    // Resolve enrollmentId if not passed in state
    if (!state?.enrollmentId) {
      this.enrollmentSvc.getAll().subscribe({
        next: data => {
          const e = data.find(e => e.courseId === id);
          if (e) {
            this.enrollmentId.set(e.enrollmentId);
            this.courseCompleted.set(e.status === 'Completed');
            this.loadProgress(e.enrollmentId);
          }
          if (!state?.course && e) {
            // Minimal course info from enrollment
            this.course.set({
              courseID: e.courseId,
              title: e.courseName,
              description: '',
              trainerID: 0,
              duration: 0,
              status: true,
            });
          }
        },
        error: () => {}
      });
    } else {
      this.loadProgress(state.enrollmentId);
    }
  }

  private loadProgress(enrollmentId: number) {
    this.progressLoading.set(true);
    this.enrollmentSvc.getModuleProgress(enrollmentId).subscribe({
      next: data => {
        this.completed.set(new Set(data.completedModuleIds));
        this.progressLoading.set(false);
      },
      error: () => this.progressLoading.set(false)
    });
  }

  isCompleted(moduleId: number) { return this.completed().has(moduleId); }

  markComplete(moduleId: number) {
    const enrollmentId = this.enrollmentId();
    if (!enrollmentId) return;
    this.markingId.set(moduleId);
    this.enrollmentSvc.markModuleComplete(enrollmentId, moduleId).subscribe({
      next: res => {
        this.completed.update(s => new Set([...s, moduleId]));
        this.markingId.set(null);
        if (res.courseCompleted) {
          this.courseCompleted.set(true);
          this.showToast('Course completed! Certificate is now available.', 'success');
        } else {
          this.showToast('Module marked as complete!', 'success');
        }
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to mark complete.', 'error');
        this.markingId.set(null);
      }
    });
  }

  openModule(module: CourseModule) {
    this.router.navigate(
      ['/catalog', this.courseId(), 'modules', module.moduleID],
      { state: { module, course: this.course(), enrollmentId: this.enrollmentId() } }
    );
  }

  get progressPct() {
    const total = this.modules().length;
    if (total === 0) return 0;
    return Math.round((this.completed().size / total) * 100);
  }

  back() { this.router.navigate(['/catalog']); }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg); this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3500);
  }
}
