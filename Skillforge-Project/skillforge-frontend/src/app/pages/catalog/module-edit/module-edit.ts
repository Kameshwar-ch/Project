import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CourseService } from '../../../core/services/course.service';
import { AssessmentService } from '../../../core/services/assessment.service';
import { SharedModule } from '../../../shared/shared.module';
import { Course, CourseModule, AssessmentResponse } from '../../../core/models';

@Component({
  selector: 'app-module-edit',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './module-edit.html',
})
export class ModuleEditComponent implements OnInit {
  private router         = inject(Router);
  private courseService  = inject(CourseService);
  private assessmentSvc  = inject(AssessmentService);
  private fb             = inject(FormBuilder);

  module = signal<CourseModule | null>(null);
  course = signal<Course | null>(null);

  // ── Module form ───────────────────────────────────────────────────────────
  saving = signal(false);
  saveSuccess = signal(false);
  moduleForm = this.fb.group({
    title:       ['', [Validators.required, Validators.maxLength(20)]],
    description: ['', [Validators.required, Validators.maxLength(50)]],
    duration:    [1,  [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  // ── Assessments ───────────────────────────────────────────────────────────
  assessments        = signal<AssessmentResponse[]>([]);
  assessmentsLoading = signal(false);

  addAssessmentLoading = signal(false);
  assessmentForm = this.fb.group({
    type:         [0,   Validators.required],
    maxScore:     [100, [Validators.required, Validators.min(1), Validators.max(100)]],
    passingScore: [60,  [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  // ── Toast ─────────────────────────────────────────────────────────────────
  toast     = signal('');
  toastType = signal<'success' | 'error'>('success');

  readonly assessmentTypes = [
    { label: 'Quiz',      value: 0 },
    { label: 'Exam',      value: 1 },
    { label: 'Practical', value: 2 },
  ];

  ngOnInit() {
    const state  = history.state;
    const module = state?.module as CourseModule | undefined;
    const course = state?.course as Course | undefined;

    if (!module || !course) {
      this.router.navigate(['/catalog']);
      return;
    }

    this.module.set(module);
    this.course.set(course);

    this.moduleForm.patchValue({
      title:       module.title       ?? '',
      description: module.contentURI  ?? '',
      duration:    module.duration    ?? 1,
    });

    this.loadAssessments();
  }

  // ── Module save ───────────────────────────────────────────────────────────
  saveModule() {
    if (this.moduleForm.invalid) { this.moduleForm.markAllAsTouched(); return; }
    const course = this.course();
    const module = this.module();
    if (!course || !module) return;

    this.saving.set(true);
    const { title, description, duration } = this.moduleForm.value as any;
    this.courseService.updateModule(course.courseID, module.moduleID, { title, contentURI: description, duration }).subscribe({
      next: () => {
        this.module.update(m => m ? { ...m, title, contentURI: description, duration } : m);
        this.saving.set(false);
        this.saveSuccess.set(true);
        this.showToast('Module saved successfully!', 'success');
        setTimeout(() => this.saveSuccess.set(false), 2500);
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to save module.', 'error');
        this.saving.set(false);
      }
    });
  }

  // ── Assessments ───────────────────────────────────────────────────────────
  private loadAssessments() {
    const course  = this.course();
    const module  = this.module();
    if (!course || !module) return;
    this.assessmentsLoading.set(true);
    this.assessmentSvc.getAll().subscribe({
      next: data => {
        this.assessments.set(data.filter(a => a.moduleId === module.moduleID));
        this.assessmentsLoading.set(false);
      },
      error: () => this.assessmentsLoading.set(false)
    });
  }

  addAssessment() {
    if (this.assessmentForm.invalid) { this.assessmentForm.markAllAsTouched(); return; }
    const course  = this.course();
    const module  = this.module();
    if (!course || !module) return;

    this.addAssessmentLoading.set(true);
    const { type, maxScore, passingScore } = this.assessmentForm.value as any;
    this.assessmentSvc.create(course.courseID, +type, +maxScore, +passingScore, module.moduleID).subscribe({
      next: () => {
        this.showToast('Assessment added!', 'success');
        this.addAssessmentLoading.set(false);
        this.assessmentForm.reset({ type: 0, maxScore: 100, passingScore: 60 });
        this.loadAssessments();
      },
      error: err => {
        this.showToast(err?.error?.message ?? 'Failed to add assessment.', 'error');
        this.addAssessmentLoading.set(false);
      }
    });
  }

  goBack() {
    this.router.navigate(['/catalog']);
  }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg);
    this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3500);
  }
}
