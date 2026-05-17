import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { CompetencyService } from '../../core/services/competency.service';
import { SkillGapService } from '../../core/services/skill-gap.service';
import { CourseService } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { UserService } from '../../core/services/user.service';
import { SharedModule } from '../../shared/shared.module';
import {
  CompetencyMatrixEntry, SkillGapItem, Course, BulkEnrollResult,
  CompetencyItem, User
} from '../../core/models';

@Component({
  selector: 'app-competency',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './competency.html',
})
export class CompetencyComponent implements OnInit {
  private auth              = inject(AuthService);
  private competencyService = inject(CompetencyService);
  private skillGapService   = inject(SkillGapService);
  private courseService     = inject(CourseService);
  private enrollmentService = inject(EnrollmentService);
  private userService       = inject(UserService);
  private fb                = inject(FormBuilder);

  // ── Role ──────────────────────────────────────────────────────────────────
  get isManager() { return this.auth.hasRole('Manager', 'Admin', 'HR'); }
  get isHR()      { return this.auth.hasRole('HR', 'Admin'); }

  // ── Raw data ──────────────────────────────────────────────────────────────
  matrix: CompetencyMatrixEntry[] = [];
  skillGaps: SkillGapItem[]       = [];

  loadingMatrix = signal(true);
  loadingGaps   = signal(true);
  matrixError   = signal<string | null>(null);
  gapsError     = signal<string | null>(null);

  // ── Filters ───────────────────────────────────────────────────────────────
  employeeSearch = signal('');
  gapLevelFilter = signal('All');

  get filteredMatrix(): CompetencyMatrixEntry[] {
    const s = this.employeeSearch().toLowerCase().trim();
    if (!s) return this.matrix;
    return this.matrix.filter(e => e.employeeName.toLowerCase().includes(s));
  }

  get filteredGaps(): SkillGapItem[] {
    let list = this.skillGaps;
    const lvl = this.gapLevelFilter();
    if (lvl !== 'All') list = list.filter(g => g.gapLevel === lvl);
    const s = this.employeeSearch().toLowerCase().trim();
    if (s) list = list.filter(g => g.employeeName.toLowerCase().includes(s));
    return list;
  }

  get highGapCount()   { return this.filteredGaps.filter(g => g.gapLevel === 'High').length; }
  get mediumGapCount() { return this.filteredGaps.filter(g => g.gapLevel === 'Medium').length; }
  get lowGapCount()    { return this.filteredGaps.filter(g => g.gapLevel === 'Low').length; }

  // ── Toast ─────────────────────────────────────────────────────────────────
  toast     = signal('');
  toastType = signal<'success' | 'error'>('success');

  // ── Assign Training modal ─────────────────────────────────────────────────
  showAssignModal      = signal(false);
  assignTarget         = signal<{ employeeId: number; employeeName: string; competencyName: string; gapLevel: string } | null>(null);
  assignCourses        = signal<Course[]>([]);
  assignCoursesLoading = signal(false);
  assignCourseId       = signal<number | null>(null);
  assignSubmitting     = signal(false);
  assignResult         = signal<BulkEnrollResult | null>(null);

  // ── Competency CRUD ───────────────────────────────────────────────────────
  competencies        = signal<CompetencyItem[]>([]);
  competenciesLoading = signal(false);
  competencySearch    = signal('');

  get filteredCompetencies(): CompetencyItem[] {
    const s = this.competencySearch().toLowerCase().trim();
    if (!s) return this.competencies();
    return this.competencies().filter(c =>
      c.name.toLowerCase().includes(s) || c.description.toLowerCase().includes(s)
    );
  }

  // Create competency
  showCreateCompetencyModal  = signal(false);
  createCompetencyLoading    = signal(false);
  createCompetencyForm = this.fb.group({
    name:        ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(200)]],
    level:       ['Beginner', Validators.required],
  });

  // Edit competency
  editingCompetency      = signal<CompetencyItem | null>(null);
  editCompetencyLoading  = signal(false);
  editCompetencyForm = this.fb.group({
    name:        ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(200)]],
    level:       ['Beginner', Validators.required],
  });

  // Delete competency
  competencyToDelete       = signal<CompetencyItem | null>(null);
  showDeleteCompetencyModal = signal(false);
  deleteCompetencyLoading  = signal(false);

  // ── Skill Gap create ──────────────────────────────────────────────────────
  showCreateGapModal  = signal(false);
  createGapLoading    = signal(false);
  employees           = signal<User[]>([]);
  employeesLoading    = signal(false);
  createGapForm = this.fb.group({
    employeeId:     [null as number | null, Validators.required],
    competencyId:   [null as number | null, Validators.required],
    gapLevel:       ['Low', Validators.required],
    dateIdentified: ['', Validators.required],
  });

  // ── Skill Gap delete ──────────────────────────────────────────────────────
  gapToDelete       = signal<SkillGapItem | null>(null);
  showDeleteGapModal = signal(false);
  deleteGapLoading  = signal(false);

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit() {
    this.competencyService.getMatrix().subscribe({
      next:  data => { this.matrix = data; this.loadingMatrix.set(false); },
      error: err  => { this.matrixError.set(err?.error?.message ?? 'Failed to load competency matrix.'); this.loadingMatrix.set(false); }
    });

    this.loadSkillGaps();

    if (this.isManager) {
      this.loadCompetencies();
    }
  }

  private loadSkillGaps() {
    this.loadingGaps.set(true);
    this.skillGapService.getAll().subscribe({
      next:  data => { this.skillGaps = data; this.loadingGaps.set(false); },
      error: err  => { this.gapsError.set(err?.error?.message ?? 'Failed to load skill gaps.'); this.loadingGaps.set(false); }
    });
  }

  private loadCompetencies() {
    this.competenciesLoading.set(true);
    this.competencyService.getAll().subscribe({
      next:  data => { this.competencies.set(data); this.competenciesLoading.set(false); },
      error: ()   => this.competenciesLoading.set(false)
    });
  }

  resetFilters() {
    this.employeeSearch.set('');
    this.gapLevelFilter.set('All');
  }

  // ── Assign Training ───────────────────────────────────────────────────────
  openAssignTraining(gap: SkillGapItem) {
    this.assignTarget.set({
      employeeId:     gap.employeeId,
      employeeName:   gap.employeeName,
      competencyName: gap.competencyName,
      gapLevel:       gap.gapLevel,
    });
    this.assignCourseId.set(null);
    this.assignResult.set(null);
    this.showAssignModal.set(true);
    if (this.assignCourses().length === 0) {
      this.assignCoursesLoading.set(true);
      this.courseService.getAll().subscribe({
        next:  data => { this.assignCourses.set(data.filter(c => c.status)); this.assignCoursesLoading.set(false); },
        error: ()   => this.assignCoursesLoading.set(false)
      });
    }
  }

  closeAssignModal() {
    this.showAssignModal.set(false);
    this.assignTarget.set(null);
    this.assignResult.set(null);
    this.assignCourseId.set(null);
  }

  submitAssignTraining() {
    const target = this.assignTarget();
    const courseId = this.assignCourseId();
    if (!target || !courseId) return;
    this.assignSubmitting.set(true);
    this.enrollmentService.bulkEnroll(courseId, [target.employeeId]).subscribe({
      next: (result: BulkEnrollResult) => {
        this.assignResult.set(result);
        this.assignSubmitting.set(false);
      },
      error: err => {
        // BadRequest with result body (e.g. already enrolled)
        if (err?.error?.results) {
          this.assignResult.set(err.error);
        } else {
          this.showToast(err?.error?.message ?? 'Training assignment failed.', 'error');
        }
        this.assignSubmitting.set(false);
      }
    });
  }

  // ── Competency CRUD ───────────────────────────────────────────────────────
  openCreateCompetency() {
    this.createCompetencyForm.reset({ name: '', description: '', level: 'Beginner' });
    this.showCreateCompetencyModal.set(true);
  }

  closeCreateCompetency() {
    this.showCreateCompetencyModal.set(false);
  }

  submitCreateCompetency() {
    if (this.createCompetencyForm.invalid) { this.createCompetencyForm.markAllAsTouched(); return; }
    this.createCompetencyLoading.set(true);
    const { name, description, level } = this.createCompetencyForm.value as any;
    this.competencyService.create({ name, description: description ?? '', level }).subscribe({
      next: item => {
        this.competencies.update(list => [...list, item]);
        this.closeCreateCompetency();
        this.createCompetencyLoading.set(false);
        this.showToast('Competency created successfully!', 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to create competency.', 'error');
        this.createCompetencyLoading.set(false);
      }
    });
  }

  openEditCompetency(c: CompetencyItem) {
    this.editingCompetency.set(c);
    this.editCompetencyForm.patchValue({ name: c.name, description: c.description ?? '', level: c.level });
  }

  closeEditCompetency() {
    this.editingCompetency.set(null);
    this.editCompetencyForm.reset({ level: 'Beginner' });
  }

  submitEditCompetency() {
    if (this.editCompetencyForm.invalid) { this.editCompetencyForm.markAllAsTouched(); return; }
    const c = this.editingCompetency();
    if (!c) return;
    this.editCompetencyLoading.set(true);
    const { name, description, level } = this.editCompetencyForm.value as any;
    this.competencyService.update(c.competencyId, { name, description: description ?? '', level }).subscribe({
      next: () => {
        this.competencies.update(list => list.map(x =>
          x.competencyId === c.competencyId ? { ...x, name, description: description ?? '', level } : x
        ));
        this.closeEditCompetency();
        this.editCompetencyLoading.set(false);
        this.showToast('Competency updated successfully!', 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to update competency.', 'error');
        this.editCompetencyLoading.set(false);
      }
    });
  }

  openDeleteCompetency(c: CompetencyItem) {
    this.competencyToDelete.set(c);
    this.showDeleteCompetencyModal.set(true);
  }

  closeDeleteCompetency() {
    this.showDeleteCompetencyModal.set(false);
    this.competencyToDelete.set(null);
  }

  confirmDeleteCompetency() {
    const c = this.competencyToDelete();
    if (!c) return;
    this.deleteCompetencyLoading.set(true);
    this.competencyService.delete(c.competencyId).subscribe({
      next: () => {
        this.competencies.update(list => list.filter(x => x.competencyId !== c.competencyId));
        this.closeDeleteCompetency();
        this.deleteCompetencyLoading.set(false);
        this.showToast('Competency deleted.', 'success');
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to delete competency.', 'error');
        this.deleteCompetencyLoading.set(false);
      }
    });
  }

  // ── Skill Gap create ──────────────────────────────────────────────────────
  openCreateGap() {
    this.createGapForm.reset({ gapLevel: 'Low', dateIdentified: new Date().toISOString().split('T')[0] });
    this.showCreateGapModal.set(true);
    if (this.employees().length === 0) {
      this.employeesLoading.set(true);
      this.userService.getAll().subscribe({
        next: data => { this.employees.set(data.filter(u => u.roleName === 'Employee')); this.employeesLoading.set(false); },
        error: ()  => this.employeesLoading.set(false)
      });
    }
    if (this.competencies().length === 0) {
      this.loadCompetencies();
    }
  }

  closeCreateGap() {
    this.showCreateGapModal.set(false);
  }

  submitCreateGap() {
    if (this.createGapForm.invalid) { this.createGapForm.markAllAsTouched(); return; }
    this.createGapLoading.set(true);
    const { employeeId, competencyId, gapLevel, dateIdentified } = this.createGapForm.value as any;
    this.skillGapService.create({ employeeId: +employeeId, competencyId: +competencyId, gapLevel, dateIdentified }).subscribe({
      next: item => {
        this.skillGaps = [...this.skillGaps, item];
        this.closeCreateGap();
        this.createGapLoading.set(false);
        this.showToast('Skill gap added successfully!', 'success');
        // Refresh matrix to reflect new gap
        this.competencyService.getMatrix().subscribe({ next: data => this.matrix = data, error: () => {} });
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to add skill gap.', 'error');
        this.createGapLoading.set(false);
      }
    });
  }

  // ── Skill Gap delete ──────────────────────────────────────────────────────
  openDeleteGap(gap: SkillGapItem) {
    this.gapToDelete.set(gap);
    this.showDeleteGapModal.set(true);
  }

  closeDeleteGap() {
    this.showDeleteGapModal.set(false);
    this.gapToDelete.set(null);
  }

  confirmDeleteGap() {
    const gap = this.gapToDelete();
    if (!gap) return;
    this.deleteGapLoading.set(true);
    this.skillGapService.delete(gap.skillGapID).subscribe({
      next: () => {
        this.skillGaps = this.skillGaps.filter(g => g.skillGapID !== gap.skillGapID);
        this.closeDeleteGap();
        this.deleteGapLoading.set(false);
        this.showToast('Skill gap deleted.', 'success');
        this.competencyService.getMatrix().subscribe({ next: data => this.matrix = data, error: () => {} });
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to delete skill gap.', 'error');
        this.deleteGapLoading.set(false);
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg); this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3500);
  }

  todayIso() { return new Date().toISOString().split('T')[0]; }
}
