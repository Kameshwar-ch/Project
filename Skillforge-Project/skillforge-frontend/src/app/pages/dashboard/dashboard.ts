import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CourseService } from '../../core/services/course.service';
import { NotificationService } from '../../core/services/notification.service';
import { CertificationService } from '../../core/services/certification.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { AssessmentService } from '../../core/services/assessment.service';
import { ComplianceService } from '../../core/services/compliance.service';
import { UserService } from '../../core/services/user.service';
import { SharedModule } from '../../shared/shared.module';
import { EnrollmentResponse, EmployeeAssessment } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [SharedModule, RouterLink],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private courses = inject(CourseService);
  private notifications = inject(NotificationService);
  private certifications = inject(CertificationService);
  private enrollmentSvc = inject(EnrollmentService);
  private assessmentSvc = inject(AssessmentService);
  private compliance = inject(ComplianceService);
  private userSvc = inject(UserService);

  readonly role = this.auth.userRole;
  readonly userName = signal<string>('');

  courseCount      = signal(0);
  notifCount       = signal(0);
  certTotal        = signal(0);           // Employee: own certs | Manager/HR/Admin: org-wide
  pendingReviews   = signal(0);           // Trainer: pending assessment submissions
  myEnrolled       = signal(0);           // Employee: active enrolled count
  completionRate   = signal<string | number>('—');
  complianceTrend  = signal('Team average');
  loading          = signal(true);

  upcomingEvents = signal<{
    id: string;
    date: string;
    course: string;
    label: 'Assessment' | 'Start Module' | 'Course Completion' | 'Enrolled';
    detail: string;
    link: string;
  }[]>([]);

  ngOnInit() {
    this.userSvc.getMe().subscribe({
      next: (user) => this.userName.set(user.userName ?? ''),
      error: () => {}
    });
    const role = this.role();
    
    const trainerId = role === 'Trainer' ? (this.auth.userId() ?? undefined) : undefined;
    this.courses.getAll(trainerId).subscribe({
      next: (data) => { this.courseCount.set(data.filter(c => c.status).length); this.loading.set(false); },
      error: () => this.loading.set(false)
    });

    this.notifications.getAll().subscribe({
      next: (data: any[]) => this.notifCount.set(data.length),
      error: () => {}
    });



    if (role === 'Employee') {
      // Employee: show only their own certifications
      this.certifications.getMy().subscribe({
        next: (data) => this.certTotal.set(data.length),
        error: () => {}
      });
      this.enrollmentSvc.getAll().subscribe({
        next: (data) => this.myEnrolled.set(data.filter(e => e.status === 'Enrolled').length),
        error: () => {}
      });
    } else if (role === 'Trainer') {
      // Trainer: show pending assessment reviews — no cert card
      this.assessmentSvc.getPendingResults().subscribe({
        next: (data) => this.pendingReviews.set(data.length),
        error: () => {}
      });
    } else {
      // Manager / HR / Admin: org-wide cert count + compliance
      this.certifications.getAll().subscribe({
        next: (data) => this.certTotal.set(data.length),
        error: () => {}
      });
      this.compliance.getComplianceSummary().subscribe({
        next: (summary) => {
          this.completionRate.set(`${Math.round(summary.complianceRate)}%`);
          this.complianceTrend.set(`${summary.compliantCount} of ${summary.totalEmployees} compliant`);
        },
        error: () => {}
      });
    }

    // Build upcoming events from enrollments + assessments (employee) or enrollments only (others)
    if (this.isEmployee) {
      let enrollments: EnrollmentResponse[] = [];
      let assessments: EmployeeAssessment[] = [];
      let enrollDone = false, assessDone = false;

      const merge = () => {
        if (!enrollDone || !assessDone) return;
        const events: { id: string; date: string; course: string; label: 'Assessment' | 'Start Module' | 'Course Completion' | 'Enrolled'; detail: string; link: string }[] = [];

        // Pending assessments first
        assessments.filter(a => !a.isDone).forEach(a => {
          events.push({
            id:     `a-${a.assessmentId}`,
            date:   new Date(a.date).toLocaleDateString('en-US', { month: 'short', day: '2-digit' }),
            course: a.courseName,
            label:  'Assessment' as const,
            detail: `${a.type} · Max ${a.maxScore}`,
            link:   '/assessments',
          });
        });

        // Enrolled courses — suggest starting a module
        enrollments.filter(e => e.status === 'Enrolled').forEach(e => {
          events.push({
            id:     `e-${e.enrollmentId}`,
            date:   new Date(e.enrollmentDate).toLocaleDateString('en-US', { month: 'short', day: '2-digit' }),
            course: e.courseName,
            label:  'Start Module' as const,
            detail: 'Continue your learning',
            link:   '/catalog',
          });
        });

        // Near-completed / Completed courses
        enrollments.filter(e => e.status === 'Completed').forEach(e => {
          events.push({
            id:     `c-${e.enrollmentId}`,
            date:   new Date(e.enrollmentDate).toLocaleDateString('en-US', { month: 'short', day: '2-digit' }),
            course: e.courseName,
            label:  'Course Completion' as const,
            detail: 'Completed',
            link:   '/enrollment',
          });
        });

        this.upcomingEvents.set(events);
      };

      this.enrollmentSvc.getAll().subscribe({
        next: (data) => { enrollments = data; enrollDone = true; merge(); },
        error: () => { enrollDone = true; merge(); }
      });

      this.assessmentSvc.getMyAssessments().subscribe({
        next: (data) => { assessments = data; assessDone = true; merge(); },
        error: () => { assessDone = true; merge(); }
      });

    } else {
      // Non-employee: show recent enrollments across the team
      this.enrollmentSvc.getAll().subscribe({
        next: (data) => {
          const events = data.slice(0, 8).map(e => ({
            id:     `e-${e.enrollmentId}`,
            date:   new Date(e.enrollmentDate).toLocaleDateString('en-US', { month: 'short', day: '2-digit' }),
            course: e.courseName,
            label:  'Enrolled' as const,
            detail: e.employeeName,
            link:   '/enrollment',
          }));
          this.upcomingEvents.set(events);
        },
        error: () => {}
      });
    }
  }

  get isEmployee() { return this.role() === 'Employee'; }
  get isTrainer()  { return this.role() === 'Trainer'; }
  get isManager()  { return this.role() === 'Manager'; }
  get isHR()       { return this.role() === 'HR'; }
  get isAdmin()    { return this.role() === 'Admin'; }
}
