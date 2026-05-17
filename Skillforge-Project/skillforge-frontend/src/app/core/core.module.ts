import { NgModule, Optional, SkipSelf } from '@angular/core';
import { AuthStateService } from './store/auth.state';
import { AuthService } from './services/auth.service';
import { ThemeService } from './services/theme.service';
import { CourseService } from './services/course.service';
import { EnrollmentService } from './services/enrollment.service';
import { NotificationService } from './services/notification.service';
import { ReportService } from './services/report.service';
import { UserService } from './services/user.service';

/**
 * CoreModule — registers all singleton application services.
 * Import once via importProvidersFrom() in app.config.ts.
 * Throws if accidentally imported a second time.
 */
@NgModule({
  providers: [
    AuthStateService,
    AuthService,
    ThemeService,
    CourseService,
    EnrollmentService,
    NotificationService,
    ReportService,
    UserService,
  ],
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() parent: CoreModule) {
    if (parent) {
      throw new Error('CoreModule is already loaded. Import it in the root application only.');
    }
  }
}
