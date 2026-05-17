import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CourseService } from '../../../core/services/course.service';
import { UserService } from '../../../core/services/user.service';
import { Course, User } from '../../../core/models';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-course-edit',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './course-edit.html',
})
export class CourseEditComponent implements OnInit {
  private router      = inject(Router);
  private fb          = inject(FormBuilder);
  private auth        = inject(AuthService);
  private courseService = inject(CourseService);
  private userService   = inject(UserService);

  course   = signal<Course | null>(null);
  trainers = signal<User[]>([]);
  loading  = signal(false);
  toast    = signal('');
  toastType = signal<'success' | 'error'>('success');

  get isAdmin() { return this.auth.hasRole('Admin'); }

  editForm = this.fb.group({
    title:       ['', [Validators.required, Validators.maxLength(20)]],
    description: ['', [Validators.required, Validators.maxLength(50)]],
    duration:    [1,  [Validators.required, Validators.min(1), Validators.max(100)]],
    status:      ['false'],
    trainerID:   [null as number | null],
  });

  ngOnInit() {
    const nav       = this.router.getCurrentNavigation();
    const stateData: Course = nav?.extras?.state?.['course'] ?? history.state?.['course'];

    if (!stateData) { this.router.navigate(['/catalog']); return; }

    this.course.set(stateData);
    this.editForm.patchValue({
      title:       stateData.title,
      description: stateData.description,
      duration:    stateData.duration,
      status:      stateData.status ? 'true' : 'false',
      trainerID:   stateData.trainerID,
    });

    if (this.isAdmin) {
      this.editForm.get('trainerID')!.setValidators(Validators.required);
      this.editForm.get('trainerID')!.updateValueAndValidity();
      this.userService.getAll().subscribe({
        next: users => this.trainers.set(users.filter(u => u.roleName === 'Trainer')),
        error: () => {}
      });
    }
  }

  submit() {
    if (this.editForm.invalid) { this.editForm.markAllAsTouched(); return; }
    const course = this.course();
    if (!course) return;
    this.loading.set(true);
    const { title, description, duration, status, trainerID } = this.editForm.value as any;
    const payload = {
      title,
      description,
      duration: Number(duration),
      status: status === 'true',
      trainerID: this.isAdmin ? Number(trainerID) : course.trainerID,
    };
    this.courseService.update(course.courseID, payload).subscribe({
      next: () => {
        this.showToast('Course updated successfully!', 'success');
        this.loading.set(false);
        setTimeout(() => this.router.navigate(['/catalog']), 1200);
      },
      error: err => {
        const msg = err?.error?.message ?? 'Failed to update course.';
        this.showToast(msg, 'error');
        this.loading.set(false);
      }
    });
  }

  back() { this.router.navigate(['/catalog']); }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg);
    this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3000);
  }
}
