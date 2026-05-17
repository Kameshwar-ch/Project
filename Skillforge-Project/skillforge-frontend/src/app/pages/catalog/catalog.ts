import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CourseService } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { UserService } from '../../core/services/user.service';
import { Course, CreateCourseRequest, CourseModule, CreateModuleRequest, User } from '../../core/models';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './catalog.html',
})
export class CatalogComponent implements OnInit {
  readonly auth = inject(AuthService);
  private courseService = inject(CourseService);
  private enrollmentService = inject(EnrollmentService);
  private userService = inject(UserService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  readonly role = this.auth.userRole;
  readonly userId = this.auth.userId;

  courses = signal<Course[]>([]);
  loading = signal(true);
  searchTerm = signal('');
  statusFilter = signal('All');
  toast = signal('');
  toastType = signal<'success' | 'error'>('success');

  showCreateModal = signal(false);
  createLoading = signal(false);
  trainers = signal<User[]>([]);

  selectedCourse = signal<Course | null>(null);
  enrolledCourseIds = signal<Set<number>>(new Set());
  enrollmentIdByCourse = signal<Map<number, number>>(new Map()); // courseId → enrollmentId
  completedCourseIds = signal<Set<number>>(new Set());           // completed courses
  showEnrolledOnly = signal(false);
  waitlistedCourseIds = signal<Set<number>>(new Set());

  // Module progress (employee)
  moduleProgress   = signal<Set<number>>(new Set());  // completed moduleIDs for current modal
  progressLoading  = signal(false);
  markCompleteId   = signal<number | null>(null);     // moduleID being marked

  courseToDelete = signal<Course | null>(null);
  showDeleteModal = signal(false);
  deleteLoading = signal(false);

  moduleTargetCourse = signal<Course | null>(null);
  showModuleModal = signal(false);
  modules = signal<CourseModule[]>([]);
  modulesLoading = signal(false);
  addModuleLoading = signal(false);

  // Delete module
  moduleToDelete      = signal<CourseModule | null>(null);
  showModuleDeleteModal = signal(false);
  deleteModuleLoading  = signal(false);

  moduleForm = this.fb.group({
    title:       ['', [Validators.required, Validators.maxLength(20)]],
    description: ['', [Validators.required, Validators.maxLength(50)]],
    duration:    [1,  [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  createForm = this.fb.group({
    title:       ['', [Validators.required, Validators.maxLength(20)]],
    description: ['', [Validators.required, Validators.maxLength(50)]],
    duration:    [1,  [Validators.required, Validators.min(1), Validators.max(100)]],
    status:      ['false'],
    trainerID:   [null as number | null],
  });

  get canCreate() { return this.auth.hasRole('Trainer', 'Admin'); }
  get canEnroll() { return this.auth.hasRole('Employee'); }
  get isAdmin()   { return this.role() === 'Admin'; }
  get isEmployee() { return this.role() === 'Employee'; }

  filteredCourses = computed(() => {
    let list = this.courses();
    if (this.isEmployee) {
      list = list.filter(c => c.status === true);
      if (this.showEnrolledOnly()) {
        list = list.filter(c => this.enrolledCourseIds().has(c.courseID));
      }
    } else if (this.statusFilter() !== 'All') {
      list = list.filter(c => c.status === (this.statusFilter() === 'Live'));
    }
    const term = this.searchTerm().toLowerCase();
    if (term) list = list.filter(c => c.title.toLowerCase().includes(term) || c.description?.toLowerCase().includes(term));
    return list;
  });

  isEnrolled(courseId: number): boolean {
    return this.enrolledCourseIds().has(courseId);
  }

  isWaitlisted(courseId: number): boolean {
    return this.waitlistedCourseIds().has(courseId);
  }

  ngOnInit() {
    this.loadCourses();
    if (this.isEmployee) {
      this.enrollmentService.getAll().subscribe({
        next: data => {
          const approved = data.filter(e => e.status === 'Enrolled' || e.status === 'Completed');
          this.enrolledCourseIds.set(new Set(approved.map(e => e.courseId)));
          this.enrollmentIdByCourse.set(new Map(approved.map(e => [e.courseId, e.enrollmentId])));
          this.completedCourseIds.set(new Set(approved.filter(e => e.status === 'Completed').map(e => e.courseId)));
          this.waitlistedCourseIds.set(new Set(data.filter(e => e.status === 'Waitlist').map(e => e.courseId)));
        },
        error: () => {}
      });
    }
  }

  get isTrainer() { return this.role() === 'Trainer'; }

  loadCourses() {
    this.loading.set(true);
    // Trainers only see courses they own; other roles see all
    const trainerId = this.isTrainer ? (this.userId() ?? undefined) : undefined;
    this.courseService.getAll(trainerId).subscribe({
      next: data => { this.courses.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openCreate() {
    this.showCreateModal.set(true);
    if (this.isAdmin) {
      this.createForm.get('trainerID')!.setValidators(Validators.required);
      this.createForm.get('trainerID')!.updateValueAndValidity();
      if (this.trainers().length === 0) {
        this.userService.getAll().subscribe({
          next: users => this.trainers.set(users.filter(u => u.roleName === 'Trainer')),
          error: () => {}
        });
      }
    }
  }
  closeCreate() {
    this.showCreateModal.set(false);
    this.createForm.get('trainerID')!.clearValidators();
    this.createForm.get('trainerID')!.updateValueAndValidity();
    this.createForm.reset({ duration: 1, status: 'false', trainerID: null });
  }

  submitCreate() {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.createLoading.set(true);
    const { title, description, duration, status, trainerID } = this.createForm.value as any;
    const tid = this.isAdmin ? (Number(trainerID) || 0) : (this.userId() ?? 0);
    const payload: CreateCourseRequest = { title, description, duration, status: status === 'true', trainerID: tid };

    this.courseService.create(payload).subscribe({
      next: () => {
        this.closeCreate();                        // close ONLY on confirmed success
        this.showToast('Course created successfully!', 'success');
        this.createLoading.set(false);
        this.silentRefresh();                      // fetch real data without spinner
      },
      error: err => {
        const msg = err?.error?.message ?? err?.error?.title ?? 'Failed to create course.';
        this.showToast(msg, 'error');
        this.createLoading.set(false);
      }
    });
  }

  private silentRefresh() {
    const trainerId = this.isTrainer ? (this.userId() ?? undefined) : undefined;
    this.courseService.getAll(trainerId).subscribe({
      next: data => this.courses.set(data),
      error: () => {}
    });
  }

  // ── Enroll confirmation modal ─────────────────────────────────────────────
  enrollTarget  = signal<Course | null>(null);
  showEnrollModal = signal(false);
  enrollLoading   = signal(false);

  viewModules(course: Course) {
    const enrollmentId = this.enrollmentIdByCourse().get(course.courseID);
    this.router.navigate(['/catalog', course.courseID, 'modules'], {
      state: { course, enrollmentId }
    });
  }

  openEnroll(course: Course) {
    this.enrollTarget.set(course);
    this.showEnrollModal.set(true);
  }

  closeEnroll() {
    this.showEnrollModal.set(false);
    this.enrollTarget.set(null);
  }

  confirmEnroll() {
    const course = this.enrollTarget();
    const uid    = this.userId();
    if (!course || !uid) return;
    this.enrollLoading.set(true);
    this.enrollmentService.enroll(course.courseID, uid).subscribe({
      next: () => {
        this.waitlistedCourseIds.update(ids => new Set([...ids, course.courseID]));
        this.closeEnroll();
        this.enrollLoading.set(false);
        this.showToast('Enrollment requested! Waiting for trainer approval.', 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? err?.error ?? 'Enrollment failed.', 'error');
        this.enrollLoading.set(false);
      }
    });
  }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg); this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3000);
  }

  openDelete(course: Course) {
    this.courseToDelete.set(course);
    this.showDeleteModal.set(true);
  }

  closeDelete() {
    this.showDeleteModal.set(false);
    this.courseToDelete.set(null);
  }

  confirmDelete() {
    const course = this.courseToDelete();
    if (!course) return;
    this.deleteLoading.set(true);
    this.courseService.delete(course.courseID).subscribe({
      next: () => {
        this.courses.update(list => list.filter(c => c.courseID !== course.courseID));
        this.closeDelete();
        this.deleteLoading.set(false);
        this.showToast('Course deleted successfully.', 'success');
      },
      error: err => {
        const msg = err?.error?.message ?? 'Failed to delete course.';
        this.showToast(msg, 'error');
        this.deleteLoading.set(false);
      }
    });
  }

  openModules(course: Course) {
    this.moduleTargetCourse.set(course);
    this.moduleProgress.set(new Set());
    this.showModuleModal.set(true);
    this.loadModules(course.courseID);

    // Load progress for employees who are enrolled in this course
    if (this.isEmployee) {
      const enrollmentId = this.enrollmentIdByCourse().get(course.courseID);
      if (enrollmentId) this.loadModuleProgress(enrollmentId);
    }
  }

  closeModules() {
    this.showModuleModal.set(false);
    this.moduleTargetCourse.set(null);
    this.modules.set([]);
    this.moduleProgress.set(new Set());
    this.moduleForm.reset({ duration: 1 });
  }

  private loadModuleProgress(enrollmentId: number) {
    this.progressLoading.set(true);
    this.enrollmentService.getModuleProgress(enrollmentId).subscribe({
      next: data => {
        this.moduleProgress.set(new Set(data.completedModuleIds));
        this.progressLoading.set(false);
      },
      error: () => this.progressLoading.set(false)
    });
  }

  isModuleCompleted(moduleId: number): boolean {
    return this.moduleProgress().has(moduleId);
  }

  markModuleComplete(moduleId: number) {
    const course = this.moduleTargetCourse();
    if (!course) return;
    const enrollmentId = this.enrollmentIdByCourse().get(course.courseID);
    if (!enrollmentId) return;

    this.markCompleteId.set(moduleId);
    this.enrollmentService.markModuleComplete(enrollmentId, moduleId).subscribe({
      next: res => {
        this.moduleProgress.update(set => new Set([...set, moduleId]));
        this.markCompleteId.set(null);
        if (res.courseCompleted) {
          // Update completion tracking
          this.completedCourseIds.update(set => new Set([...set, course.courseID]));
          this.showToast('Course completed! Your certificate is now available.', 'success');
        } else {
          this.showToast('Module marked as complete!', 'success');
        }
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to mark module complete.', 'error');
        this.markCompleteId.set(null);
      }
    });
  }

  private loadModules(courseId: number) {
    this.modulesLoading.set(true);
    this.courseService.getModules(courseId).subscribe({
      next: data => { this.modules.set(data); this.modulesLoading.set(false); },
      error: () => this.modulesLoading.set(false)
    });
  }

  submitModule() {
    if (this.moduleForm.invalid) { this.moduleForm.markAllAsTouched(); return; }
    const course = this.moduleTargetCourse();
    if (!course) return;
    this.addModuleLoading.set(true);
    const { title, description, duration } = this.moduleForm.value as any;
    this.courseService.addModule(course.courseID, { title, contentURI: description, duration }).subscribe({
      next: () => {
        this.moduleForm.reset({ duration: 1 });
        this.showToast('Module added successfully!', 'success');
        this.addModuleLoading.set(false);
        this.loadModules(course.courseID);
      },
      error: err => {
        const msg = err?.error?.message ?? 'Failed to add module.';
        this.showToast(msg, 'error');
        this.addModuleLoading.set(false);
      }
    });
  }

  // ── Edit Module — navigate to dedicated edit page ────────────────────────
  openEditModule(module: CourseModule) {
    const course = this.moduleTargetCourse();
    if (!course) return;
    this.closeModules();
    this.router.navigate(
      ['/catalog', course.courseID, 'modules', module.moduleID, 'edit'],
      { state: { module, course } }
    );
  }

  // ── Delete Module ─────────────────────────────────────────────────────────
  openDeleteModule(module: CourseModule) {
    this.moduleToDelete.set(module);
    this.showModuleDeleteModal.set(true);
  }

  closeDeleteModule() {
    this.showModuleDeleteModal.set(false);
    this.moduleToDelete.set(null);
  }

  confirmDeleteModule() {
    const course = this.moduleTargetCourse();
    const module = this.moduleToDelete();
    if (!course || !module) return;
    this.deleteModuleLoading.set(true);
    this.courseService.deleteModule(course.courseID, module.moduleID).subscribe({
      next: () => {
        this.modules.update(list => list.filter(m => m.moduleID !== module.moduleID));
        this.closeDeleteModule();
        this.deleteModuleLoading.set(false);
        this.showToast('Module deleted successfully.', 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to delete module.', 'error');
        this.deleteModuleLoading.set(false);
      }
    });
  }

  viewCourse(course: Course) {
    this.router.navigate(['/catalog', course.courseID], { state: { course } });
  }

  editCourse(course: Course) {
    this.router.navigate(['/catalog', course.courseID, 'edit'], { state: { course } });
  }
}
