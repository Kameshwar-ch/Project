import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Course } from '../../../core/models';
import { SharedModule } from '../../../shared/shared.module';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './course-detail.html',
})
export class CourseDetailComponent implements OnInit {
  private router = inject(Router);

  course = signal<Course | null>(null);
  loading = signal(true);
  error = signal('');

  ngOnInit() {
    const nav = this.router.getCurrentNavigation();
    const stateData = nav?.extras?.state?.['course'] ?? history.state?.['course'];
    if (stateData) {
      this.course.set(stateData);
      this.loading.set(false);
    } else {
      this.router.navigate(['/catalog']);
    }
  }

  back() { this.router.navigate(['/catalog']); }
}
