# SkillForge — Static Code Audit / End-to-End Test Report

**Project**: SkillForge LMS (corporate Learning & Development platform)
**Report date**: 2026-05-14
**Audit scope**: Static code review of backend (ASP.NET Core 10, EF Core), frontend (Angular 21), and DB connectivity. No code was executed — every finding is verified against source.
**Test environment**:
- DB server: `LTIN719061\SQLEXPRESS` — **reachable**
- Database: `tmpDb8` — **exists, 19 tables present**
- Seed data: **populated** (10 users across all 5 roles, plus courses, modules, enrollments, assessments, results, certifications, competencies, skill gaps, compliance records, notifications, audit logs, attendance)
- Frontend dev server: running on `http://localhost:4200`
- Backend API: not running during this audit (static review only)

---

## 0. Executive Summary

The SkillForge codebase is **architecturally sound but has several show-stopping bugs** that would block successful end-to-end use today. Two of them are **security-critical** (anonymous Admin self-registration; tokenless password reset). Three are **feature-broken** (forgot-password reset always fails validation; attendance status flag inverted; reports `Refresh` endpoint mutates via GET). Plus extensive medium-severity drift between FE and BE contracts.

**Net verdict**: end-to-end run would surface ~10 immediate bugs before completing the first happy-path workflow. Recommend addressing all P0/P1 items before any UAT.

| Severity | Count |
|---|---|
| 🔴 **Critical (P0)** — security holes, account takeover, broken core flow | **11** |
| 🟠 **Major (P1)** — wrong status codes, role-gate gaps, business-logic inversions, contract drift | **28** |
| 🟡 **Minor (P2)** — code quality, type-safety, accessibility, naming | **30+** |

---

## 1. Environment & Database Verification

### 1.1 `.env` updated
```
CONNECTION_STRING=Data Source=LTIN719061\SQLEXPRESS; Initial Catalog=tmpDb8;Integrated Security=True;TrustServerCertificate=True
```
Status: ✅ Updated. SQL Server reachable. Database `tmpDb8` exists.

### 1.2 Schema present (19 tables)
`__EFMigrationsHistory`, `Assessments`, `Attendance`, `AttendanceRequests`, `Audit`, `AuditLog`, `Certifications`, `Competency`, `ComplianceRecord`, `Course`, `Enrollment`, `Module`, `ModuleProgress`, `Notification`, `Report`, `ReportSchedule`, `Results`, `SkillGap`, `User`.

### 1.3 Seed counts per table

| Table | Rows |
|---|---|
| User | 10 |
| Course | 3 |
| Module | 6 |
| Enrollment | 8 |
| Assessment | 3 |
| Result | 8 |
| Certification | 7 |
| Competency | 5 |
| SkillGap | 7 |
| ComplianceRecord | 7 |
| Notification | 10 |
| AuditLog | 8 |
| Attendance | 16 |

### 1.4 Seeded test users
| UserID | Name | Role | Email |
|---|---|---|---|
| 11 | John Admin | Admin | admin@skillforge.com |
| 12 | Sarah HR | HR | hr@skillforge.com |
| 13 | Mike Manager | Manager | manager@skillforge.com |
| 14 | Tom Trainer | Trainer | trainer1@skillforge.com |
| 15 | Lisa Trainer | Trainer | trainer2@skillforge.com |
| 16 | Alice Smith | Employee | alice@skillforge.com |
| 17 | Bob Jones | Employee | bob@skillforge.com |
| 18 | Carol White | Employee | carol@skillforge.com |
| 19 | David Brown | Employee | david@skillforge.com |
| 20 | Eve Taylor | Employee | eve@skillforge.com |

Password hash present for all users (assumed `Admin@123` per seed script).

---

## 2. Critical Bugs (P0) — Must fix before any E2E run

### 🔴 P0-1. Anonymous Admin self-registration (privilege escalation)
- **File**: `Skillforge/Controllers/UserController.cs:71-91`
- `POST /api/v1/User/Register` has **no** `[Authorize]` attribute. `UserService.UserRegisterAsync` parses the `Role` field from the body via `Enum.TryParse<UserRole>(...)` (`Service/UserService.cs:110`), so **any anonymous caller can register themselves as Admin**.
- Fix: either lock behind `[Authorize(Roles=Admin)]` or force `Role = UserRole.Employee` server-side.

### 🔴 P0-2. Account takeover via tokenless password reset
- **Files**: `Controllers/ForgotPasswordController.cs:31-38`, `Service/ForgotPasswordService.cs`
- `verifyemail` and `resetpassword` are two unrelated endpoints with no token/OTP linking them. `resetpassword` accepts `{Email, NewPassword, ConfirmPassword}` and immediately updates the password if email exists — no proof of email ownership.
- Also leaks account existence: `verifyemail` returns `EmailNotFound` vs `EmailFound` (user enumeration).
- Fix: issue a short-lived signed reset token on `verifyemail`, require it on `resetpassword`. Add rate-limit. Return identical message regardless of email match.

### 🔴 P0-3. Forgot-password feature **always fails** for legitimate users
- **File**: `skillforge-frontend/src/app/pages/auth/forgot-password/forgot-password.ts:26-29, 47`
- Frontend's `resetForm` declares only `email` + `newPassword`. The backend (`ForgotPasswordService.cs:44`) requires `NewPassword == ConfirmPassword` — and FE sends no `confirmPassword`, so the comparison `newPassword == ""` always fails → returns 400.
- Fix: add `confirmPassword` control to `resetForm` and include in the POST body, or remove the check on the backend.

### 🔴 P0-4. Employee can enroll **anyone** (IDOR)
- **File**: `Skillforge/Controllers/EnrollmentController.cs:22-43`
- `POST /api/v1/Enrollment` reads `dto.EmployeeId` from the request body. Authorized as `[Authorize(Roles=Employee)]` only — never validated that the JWT subject == `dto.EmployeeId`.
- An Employee can enroll any other employee in any course.
- Fix: ignore body `EmployeeId`, take from `User.FindFirstValue("id")`.

### 🔴 P0-5. Any logged-in user can list every certification in the system
- **File**: `Skillforge/Controllers/CertificationController.cs:78-91`
- `GET /api/v1/Certification/certifications` has `[Authorize]` but no role list. An Employee can fetch Admin/HR certification history.
- Fix: restrict to `[Authorize(Roles="Admin,HR,Manager")]` or filter by JWT identity.

### 🔴 P0-6. Attendance status flag inverted
- **File**: `Skillforge/Service/AttendanceService.cs:103-104, 176`
- `MarkAttendanceAsync` throws "Cannot mark attendance, not in progress" **when `enrollment.Status == true`** (which means *Enrolled* everywhere else, including `MapToDto` line 138). `GetCourseAttendanceAsync` line 176 treats `Status == false` as active. Same field, opposite semantics in the same file.
- Net: trainers cannot mark attendance for enrolled students at all — only for waitlisted ones.
- Fix: pick one semantic and apply throughout.

### 🔴 P0-7. JWT expiry never re-checked after first load
- **Files**: `skillforge-frontend/src/app/core/services/auth.service.ts:59-66`, `core/guards/auth.guard.ts`
- `AuthStateService.loadInitialUser` (`auth.state.ts:29-44`) hydrates `_user$` once at construction. After that, `isLoggedIn()` only checks signal truthiness — never re-validates `exp`. A long-lived tab keeps sending the expired token forever.
- Fix: in `isLoggedIn()` and the route guard, check `exp * 1000 < Date.now()` and force logout if expired.

### 🔴 P0-8. JWT interceptor attaches token to login/register/forgot endpoints
- **File**: `skillforge-frontend/src/app/core/interceptors/jwt.interceptor.ts:5-12`
- No URL allowlist. After re-login with a stale token in localStorage, the interceptor sends `Authorization: Bearer <stale>` to `/Auth/login`. Backend behavior on rejected token varies.
- Fix: skip Bearer header for `/Auth/*` and `/ForgotPassword/*`.

### 🔴 P0-9. No 401-handling — expired token = silent failure
- **File**: `skillforge-frontend/src/app/core/interceptors/jwt.interceptor.ts` (entire file)
- Refresh token is stored at login but never used. On 401, the request fails silently — no auto-logout, no redirect to `/login`.
- Fix: add `catchError`, call `auth.logout()` + redirect, or trigger refresh-token flow.

### 🔴 P0-10. XSS via `document.write` in certificate preview
- **File**: `skillforge-frontend/src/app/pages/assessments/assessments.ts:419-490` (especially `:485-488`)
- Backend-returned strings (`cert.employeeName`, `cert.courseName`, `cert.courseDescription`) are interpolated into raw HTML and passed to `win.document.write(html)`. Combined with localStorage token storage, this is the standard credential-theft chain.
- Fix: build the certificate via DOM API (`document.createElement` + `textContent`), or render in an Angular template with default sanitization.

### 🔴 P0-11. Trainer can hijack any course they don't own
- **File**: `Skillforge/Service/CourseService.cs:150-153` + `Controllers/CourseController.cs:189`
- `PUT /api/v1/Course/{id}` is `[Authorize(Roles="Admin,Trainer")]`. The update DTO includes `TrainerID`. There's no ownership check — any Trainer can PUT-set `TrainerID` to themselves on any course.
- Fix: enforce `course.TrainerID == jwtUserId || isAdmin` server-side. Reject mutating `TrainerID` unless caller is Admin.

---

## 3. Major Bugs (P1)

### Backend

| # | Where | Issue |
|---|---|---|
| P1-1 | `AuditLogController.cs:30-31` | Filter `AuditID <= 0` makes the list endpoint return 404 on empty results — frontend pagination/empty-state breaks. Should return 200 with `data: []`. |
| P1-2 | `EnrollmentService.cs:169-211` | `BulkEnrollAsync` calls `SaveChangesAsync` per row. No transaction → partial writes on crash. Wrap in a single transaction. |
| P1-3 | `CertificationController.cs:65-66` | `DownloadCertificate` loads every cert then `FirstOrDefault(id)` — N+1 disaster (~20k queries for 10k certs). Use `GetByIdAsync`. |
| P1-4 | `AuthController.cs:90-94` (and ~10 other controllers) | Every `catch (Exception ex)` returns `ex.Message` in the response body — leaks stack traces, SQL fragments, secrets to anonymous callers. |
| P1-5 | `AssessmentController.cs:38-51` | `GET /api/v1/Assessment` is `[Authorize]` with no role. `AssessmentResponseDto` includes `PassingScore` (DTO comment explicitly says "Hidden from employees"). |
| P1-6 | `ResultController.cs:21` + `ResultService.cs` | `SubmitAssessmentResult` allows Admin. `SubmitResultAsync` has **no trainer-ownership check** — any Trainer can submit results for any course. |
| P1-7 | `JWTProviderService.cs:47, 30-31` | Token expires in `DateTime.Now` (local), validation uses UTC — drift on non-UTC servers. Secret key not null-checked / length-validated. |
| P1-8 | `ComplianceRecordController.cs:51` | `GET /Refresh` mutates DB. Use POST/PATCH. Also CSRF-friendly via GET. |
| P1-9 | `ComplianceRecordController.cs:16` | `[Authorize(Roles=HR)]` at class level excludes Admin. Admin cannot see compliance. |
| P1-10 | `CourseController.cs:164-169` | `GET /api/v1/Course` (list) has no `[Authorize]` — anonymous list of all courses + trainers. |
| P1-11 | `UserService.UpdateUser` (`UserService.cs:28-42`) | Admin can change role of any user (including self-demotion) without re-auth or audit log. |
| P1-12 | `AttendanceService.cs:108-115` | `MarkAttendanceAsync` overwrites trainer-supplied `AttendanceDate` with "first access timestamp" from AuditLog without notice. |
| P1-13 | `CompetencyController` (lines 21,29,44 etc.) + `SkillGapController:22` | Use raw string literal `"Manager,HR,Admin"` instead of `nameof(UserRole.X)`. A rename silently breaks auth. |
| P1-14 | `EnrollmentController.cs:131-161` | `BulkEnroll` does NOT catch general `Exception` — uncaught crashes leak stack traces. |
| P1-15 | `EnrollmentController.cs:142-151` | `BulkEnroll` returns 201/200/400 ambiguously for partial vs full success. Frontend can't distinguish without body parsing. Convention: 207 Multi-Status. |
| P1-16 | `ResultService.cs:38,50,53` | Throws raw `Exception` for distinct cases (duplicate, exceeds max, negative). Controller returns 400 for all → loses semantics. Use typed exceptions. |
| P1-17 | `ComplianceRecordController.cs:37-39` | Returns 400 BadRequest on `DivideByZeroException` (no employees) — should be 200 with zero summary. |
| P1-18 | `ComplianceRecordService.cs:41-78` | `UpdateComplianceRecords` deletes then rebuilds without a transaction. Crash mid-rebuild leaves table empty. |
| P1-19 | `UpdateUserRequestDto.Phone` `^\d{10}$` vs `RegisterValidator.Phone` `^\+?[1-9]\d{9,14}$` | International-format users can't update their phone. |
| P1-20 | `ResetPasswordDto` has no `RegisterValidator` | Ad-hoc validation in service is weaker than registration rules (no lowercase / special char check). |
| P1-21 | `CertificationService.cs:108-130` | `GetAllCertificationsAsync` fires N+1 calls (`GetUserByIdAsync` + `GetCourseByIdAsync` per row). |
| P1-22 | `CourseRepository.cs:114-150` | `GetCoursesFilteredAsync` uses `Join` + `GroupJoin` — cross-join in SQL. Replace with `Include` + projection. |
| P1-23 | `ResultService.cs` + `ModuleProgressService` | Inconsistent: ModuleProgress auto-issues certification on last-module pass; Result evaluation does not. |
| P1-24 | `AttendanceService.CreateAttendanceRequestAsync` | No audit log — only state-changing service that doesn't log. |
| P1-25 | `Program.cs:120-128` | CORS allows `http://localhost:4200` hardcoded — will break or be widened in prod. Read from config. |

### Frontend

| # | Where | Issue |
|---|---|---|
| P1-26 | `app.routes.ts:23-24` | `/catalog/:id` and `/catalog/:id/edit` have no role guard — any authenticated user can load the course-editor UI. |
| P1-27 | `app.routes.ts:27` | `/catalog/:courseId/modules/:moduleId/edit` has no role guard. |
| P1-28 | `app.routes.ts:25-26` | `/catalog/:courseId/modules*` is gated on `roleGuard('Employee')` — **inverted** from intent: Trainer/Admin can't open module pages they need to view. |
| P1-29 | `pages/compliance/compliance.ts:34, 78` + `sidebar.ts:30` + `app.routes.ts:31` | Manager has route access but sidebar hides link AND component-level `isHR` excludes Admin → Admin sees empty compliance summary, Manager sees nothing. Three places disagree. |
| P1-30 | `pages/iam/iam.ts:43` | `creatableRoles = ['Employee','Trainer','HR']` — Admin can't create Admin or Manager from IAM UI. |
| P1-31 | `pages/iam/iam.ts updateStatus` | Deactivating a user has no confirmation modal, unlike other destructive actions. |
| P1-32 | `pages/dashboard/dashboard.ts:93` + template | KPI card labeled "Completion Rate" is bound to `summary.complianceRate` — wrong metric displayed. |
| P1-33 | `pages/catalog/course-modules/course-modules.ts:59-67` | On deep link with no `history.state`, fabricates a fake Course with zeros. Should call `courseService.getById(id)`. |
| P1-34 | `pages/notifications/notifications.ts:30-32` | Settings toggles (`emailAlerts`, `inAppAlerts`, `weeklySummary`) are write-only signals — no backend persistence. |
| P1-35 | `app.routes.ts` | `SignupComponent` is fully built but `/signup` route is not registered. Anyone clicking the link gets bounced to `/dashboard` → `/login`. |
| P1-36 | `app.spec.ts:21` | Test asserts `<h1>` text that doesn't exist in the template. CI will always fail. |
| P1-37 | `pages/signup/signup.ts:24` | `Validators.pattern(/^[a-zA-Z ]+$/)` rejects accented or hyphenated names. |
| P1-38 | `environments/environment.ts` ≡ `environment.development.ts` | No real prod API URL configured — will break on deploy. |

### Contract drift (FE ↔ BE)

| # | Where | Issue |
|---|---|---|
| P1-39 | `UserService.update` ↔ `UpdateUserRequestDto` | FE sends `userName / roleName / email`; BE expects `name / role` (no email field at all). Latent — no page calls this yet. |
| P1-40 | `CourseService.create` ↔ `CourseRequestDto` | FE sends `status: boolean`; BE DTO has no `Status` property → silently ignored. New courses get whatever the service defaults. |
| P1-41 | `CourseService.create` response | FE declares `ApiResponse<Course>`; BE returns `{ message }` only. `res.data` is `undefined`. |
| P1-42 | `CertificationService.issue` 409 path | BE returns `{ message, certification }` on 409; FE typed `CertificationResponse` → 409 body doesn't parse. |
| P1-43 | `EnrollmentResultItem.EnrollmentId` is `long?` everywhere else uses `int` | Inconsistent ID width across DTOs. |
| P1-44 | Status code drift | BE returns 201 for create endpoints (Assessment, Certification, Enrollment, Report schedule, Result); FE assumes 200. Works today (HttpClient resolves 201 on `next`), but tests will be wrong. |
| P1-45 | `ComplianceService.runComplianceCheck` | BE returns a bare string from `Ok(...)`; gets JSON-quoted; FE displays `"text"` to user. |

---

## 4. Minor Issues (P2)

### Backend (selected)
- **Inconsistent route casing**: `/login`, `/verifyemail` (lowercase) vs `/GetAll`, `/Summary` (PascalCase). Pick one.
- **`ApiResponseDto` used only by ForgotPassword**; every other controller hand-rolls `{message, data}`. Standardize.
- **Dead code**: `IComplianceRecord.AddComplianceRecord` never called; `using System.Net.Mail` / `Microsoft.VisualBasic` imported but unused.
- **`Console.WriteLine` debug** in `UserRepository.DeleteUser:66,74,79`.
- **`UpdateStatusDto`** is a generic `bool Status` reused across User/Course/Enrollment — fragile.
- **`UserResponseDto.UserName/Email/...`** are `required` non-nullable; if EF returns null, runtime exception.
- **Spelling**: "UnAuthenticated", "UnAuthorize" — fix.
- **`EFAuditRepository : IAuditService`** — repository file implementing a service interface. Confusing.
- **`SkillGapController.GetSkillGaps`** takes raw `int? gapLevel` instead of the enum; reorder enum → values lose meaning.

### Frontend (selected)
- **Zero `takeUntilDestroyed` / unsubscribe** patterns; 98 `.subscribe(...)` calls. Mostly fine for HTTP but risky for components that navigate mid-flight.
- **`as any` form-value extraction** in 11 places — defeats reactive-form types.
- **Inline `onmouseenter` / `onmouseleave`** in `topbar.html:29-30` — blocked under strict CSP.
- **`@auth0/angular-jwt` declared in package.json but never imported** — dead dependency.
- **No global error toast / error boundary** — each component has its own toast pattern with different durations.
- **`document.createElement('a')` not appended** in 3 places (assessments, enrollment, reports) — fragile in Firefox/Safari.
- **`History.state`-based routing** between catalog → detail/modules pages — breaks on hard refresh.
- **`reports.ts` chart bars are hardcoded heights** — fake chart.
- **`auth.service.ts.userId`** does `Number(_user.id)` — `NaN` if claim missing.
- **No `autocomplete` attributes** on login/signup forms.

---

## 5. Per-Endpoint Test Pass/Fail Matrix

Legend: ✅ Works as documented · ⚠️ Works but has issue · ❌ Broken / wrong

### Auth & User
| Method | Route | Expected role | Static verdict | Notes |
|---|---|---|---|---|
| POST | `/Auth/login` | anon | ⚠️ | Works, but leaks `ex.Message` on 500 (P1-4) |
| GET | `/User/GetAll` | Admin | ✅ | |
| GET | `/User/me` | any auth | ✅ | |
| GET | `/User/{id}` | Admin | ✅ | |
| POST | `/User/Register` | anon (!) | ❌ | **Anyone can register as Admin (P0-1)** |
| PUT | `/User/update/{id}` | Admin | ❌ | Contract drift: FE sends wrong field names (P1-39) |
| PATCH | `/User/{id}/status` | Admin | ⚠️ | No confirmation on FE (P1-31) |
| DELETE | `/User/{userId}` | Admin | ✅ | |

### Forgot Password
| Method | Route | Role | Verdict | Notes |
|---|---|---|---|---|
| POST | `/ForgotPassword/verifyemail` | anon | ⚠️ | User enumeration (P0-2) |
| POST | `/ForgotPassword/resetpassword` | anon | ❌ | No token check + FE never sends `confirmPassword` → always 400 (P0-2, P0-3) |

### Course
| Method | Route | Role | Verdict | Notes |
|---|---|---|---|---|
| GET | `/Course` | **anon** | ❌ | Should require auth (P1-10) |
| GET | `/Course/{id}` | Employee/Trainer/Admin | ⚠️ | Response wrapping inconsistent (P1-41) |
| POST | `/Course` | Admin/Trainer | ⚠️ | FE-supplied `status` is silently dropped (P1-40) |
| PUT | `/Course/{id}` | Admin/Trainer | ❌ | Trainer can hijack any course (P0-11) |
| PATCH | `/Course/{id}/status` | Admin/Trainer | ✅ | |
| DELETE | `/Course/{id}` | Admin | ⚠️ | Returns 200 (should be 204) |
| GET | `/Course/{cid}/modules` | any auth | ✅ | |
| POST | `/Course/{cid}/modules` | Admin/Trainer | ✅ | Returns 200 (should be 201) |
| PUT | `/Course/{cid}/modules/{mid}` | Admin/Trainer | ✅ | |
| DELETE | `/Course/{cid}/modules/{mid}` | Admin/Trainer | ⚠️ | Returns 200 (should be 204) |

### Enrollment & Attendance
| Method | Route | Role | Verdict | Notes |
|---|---|---|---|---|
| POST | `/Enrollment` | Employee | ❌ | Trusts body `EmployeeId` — IDOR (P0-4) |
| GET | `/Enrollment` | any auth | ⚠️ | Trainer sees other trainers' enrollments (no filter) |
| POST | `/Enrollment/bulk` | Manager | ⚠️ | No transaction (P1-2), no general catch (P1-14) |
| PATCH | `/Enrollment/{id}/status` | Trainer | ✅ | |
| POST | `/Enrollment/{eid}/modules/{mid}/complete` | Employee | ✅ | |
| GET | `/Enrollment/{eid}/modules/progress` | Employee | ✅ | |
| POST | `/Attendance/Mark-Attendance` | Trainer | ❌ | Status-flag inversion blocks marking enrolled students (P0-6) |
| GET | `/Attendance/enrollment/{eid}` | any auth | ✅ | |
| POST | `/Attendance/request` | Employee | ⚠️ | No audit log entry (P1-24) |
| GET | `/Attendance/requests/my` | Employee | ✅ | |
| GET | `/Attendance/requests/course/{cid}/pending` | Trainer | ✅ | |
| PATCH | `/Attendance/requests/{rid}/review` | Trainer | ✅ | |
| GET | `/Attendance/course/{cid}?date=` | Trainer | ✅ | |

### Assessment & Result
| Method | Route | Role | Verdict | Notes |
|---|---|---|---|---|
| GET | `/Assessment` | any auth | ❌ | Leaks `PassingScore` to employees (P1-5) |
| GET | `/Assessment/module/{mid}` | Employee | ✅ | |
| GET | `/Assessment/my` | Employee | ✅ | |
| POST | `/Assessment/save-assessments` | Trainer | ✅ | |
| POST | `/Result` | Admin/Trainer | ❌ | Trainer ownership not checked (P1-6) |
| GET | `/Result/pending` | Trainer | ✅ | |
| PATCH | `/Result/{aid}/evaluate/{eid}` | Trainer | ⚠️ | Does not auto-issue cert; inconsistent w/ module-progress (P1-23) |
| POST | `/Result/self` | Employee | ⚠️ | Generic `Exception` thrown for distinct errors (P1-16) |

### Certification
| Method | Route | Role | Verdict | Notes |
|---|---|---|---|---|
| GET | `/Certification/my` | Employee | ✅ | |
| GET | `/Certification/certifications` | any auth | ❌ | Employees can see anyone's certs (P0-5) |
| POST | `/Certification/certifications` | Admin/HR | ⚠️ | 409 body shape unhandled on FE (P1-42) |
| GET | `/Certification/{id}/download` | any auth | ❌ | N+1 query disaster (P1-3); no per-user filter |

### Competency & SkillGap
| Method | Route | Role | Verdict | Notes |
|---|---|---|---|---|
| GET | `/Competency/matrix` | Manager/HR/Admin | ✅ | |
| GET | `/Competency` | Manager/HR/Admin | ✅ | Auth uses string literal not `nameof` (P1-13) |
| POST | `/Competency` | HR/Admin | ✅ | |
| PUT | `/Competency/{id}` | HR/Admin | ✅ | |
| DELETE | `/Competency/{id}` | HR/Admin | ⚠️ | Save-order is split between repo and service (race-prone) |
| GET | `/SkillGap` | Manager/HR/Admin | ⚠️ | `gapLevel` is raw int — enum reorder breaks data |
| POST | `/SkillGap` | HR/Admin | ✅ | |
| GET | `/SkillGap/employee/{id}` | Manager/HR/Admin | ✅ | |
| DELETE | `/SkillGap/{id}` | HR/Admin | ✅ | |

### Compliance, Audit, Notification, Report
| Method | Route | Role | Verdict | Notes |
|---|---|---|---|---|
| GET | `/ComplianceRecord/Summary` | HR | ❌ | Admin excluded (P1-9); returns 400 on empty DB (P1-17) |
| GET | `/ComplianceRecord/Refresh` | HR | ❌ | GET that mutates (P1-8); not transactional (P1-18) |
| GET | `/AuditLog` | Admin/HR | ⚠️ | Returns 404 on empty results (P1-1) |
| GET | `/Notification` | any auth | ✅ | |
| PATCH | `/Notification/{id}` | any auth | ✅ | |
| POST | `/Report/schedule` | Admin | ✅ | |
| GET | `/Report/schedules` | Admin | ✅ | |
| PATCH | `/Report/schedules/{id}/deactivate` | Admin | ✅ | |
| POST | `/Report/generate` | Admin/HR | ✅ | |

### Per-page UI status

| Page | Route | Guard | Loading | Error | Role gate | Verdict |
|---|---|---|---|---|---|---|
| Login | `/login` | none | ✅ | ✅ | n/a | ⚠️ Token never re-validated (P0-7) |
| Forgot Password | `/forgot-password` | none | ✅ | ✅ | n/a | ❌ Reset broken (P0-3) |
| Signup | `/signup` | — | ✅ | ✅ | n/a | ❌ Route not registered (P1-35) |
| Dashboard | `/dashboard` | auth | ✅ | partial | ✅ | ⚠️ KPI mislabel (P1-32) |
| Catalog | `/catalog` | auth | ✅ | ✅ | ✅ | ✅ |
| Course Detail | `/catalog/:id` | auth only | ✅ | ✅ | ❌ | ⚠️ No role gate (P1-26) |
| Course Edit | `/catalog/:id/edit` | auth only | ✅ | ✅ | ❌ | ⚠️ No role gate (P1-26) |
| Course Modules | `/catalog/:cid/modules` | `roleGuard('Employee')` | ✅ | ✅ | inverted | ❌ Trainer/Admin can't access (P1-28) |
| Module Detail | `/catalog/:cid/modules/:mid` | `roleGuard('Employee')` | ✅ | ✅ | inverted | ❌ Same as above |
| Module Edit | `/catalog/:cid/modules/:mid/edit` | auth only | ✅ | ✅ | ❌ | ⚠️ No role gate (P1-27) |
| Enrollment | `/enrollment` | auth | ✅ | ✅ | template-only | ⚠️ |
| Assessments | `/assessments` | auth | ✅ | ✅ | template-only | ❌ XSS in cert preview (P0-10) |
| Competency | `/competency` | M/HR/A | ✅ | ✅ | inconsistent | ⚠️ Wider than label suggests |
| Compliance | `/compliance` | M/HR/A | ✅ | partial | ❌ | ⚠️ Manager sees empty (P1-29) |
| Reports | `/reports` | M/HR/A | ✅ | ✅ | ✅ | ⚠️ Fake chart |
| Notifications | `/notifications` | auth | ✅ | ✅ | n/a | ⚠️ Settings not persisted (P1-34) |
| IAM | `/iam` | Admin | ✅ | ✅ | ✅ | ⚠️ Limited `creatableRoles` (P1-30), no confirm on deactivate (P1-31) |
| Profile | `/profile` | auth | ✅ | ✅ | n/a | ✅ Read-only |

---

## 6. Recommended Fix Order

### Sprint 1 — Security & broken flows (P0 + a few P1)
1. Lock `POST /User/Register` (P0-1).
2. Add token to password-reset flow + fix FE `confirmPassword` (P0-2, P0-3).
3. Derive `EmployeeId` from JWT in `Enrollment.Enroll` (P0-4).
4. Restrict `GET /Certification/certifications` (P0-5).
5. Fix `Enrollment.Status` flag semantics (P0-6).
6. Add JWT expiry check + 401-handler in interceptor + skip-list (P0-7, P0-8, P0-9).
7. Replace `document.write` cert preview (P0-10).
8. Guard `TrainerID` mutation in `Course.UpdateCourseAsync` (P0-11).
9. Stop returning `ex.Message` in 500 responses (P1-4).
10. Register `/signup` route OR remove `SignupComponent` (P1-35).

### Sprint 2 — Role gating & contract drift
11. Add `roleGuard` to `/catalog/:id`, `/:id/edit`, `/modules/:mid/edit` (P1-26, P1-27).
12. Fix `/modules` and `/module-detail` role-gate inversion (P1-28).
13. Reconcile Compliance role checks across route guard / sidebar / component (P1-29).
14. Fix `UpdateUserRequestDto` field names (P1-39).
15. Add `Status` to `CourseRequestDto` (P1-40).
16. Standardize role-claim strings to `nameof(UserRole.X)` (P1-13).
17. Convert `GET /Refresh` → `POST /Refresh` (P1-8).
18. Add transactions to `BulkEnrollAsync` and `UpdateComplianceRecords` (P1-2, P1-18).
19. Replace `GetAllCertifications + FirstOrDefault` with direct lookup (P1-3, P1-21).

### Sprint 3 — Code quality
20. Replace `DateTime.Now` → `DateTime.UtcNow` everywhere (P1-7).
21. Add real prod `environment.ts` (P1-38).
22. Fix `app.spec.ts` (P1-36).
23. Add `takeUntilDestroyed` to long-lived subscriptions.
24. Replace `as any` form extractions with typed `getRawValue()`.
25. Centralize error toasts.
26. Remove dead code (`@auth0/angular-jwt`, `IComplianceRecord.AddComplianceRecord`, console.WriteLines, dead imports).

---

## 7. Tests Not Run (Out of Scope)

Per user request, this report is a **static code audit only**. The following were not executed:
- Browser-driven E2E flows per role.
- Live HTTP calls against running backend.
- Load / performance tests.
- DB integrity / referential-integrity tests after seed.
- Unit tests (`ng test`, `dotnet test`) — note that `app.spec.ts` is known broken (P1-36).

To run them later: start the backend (`cd Skillforge && dotnet run`), confirm it listens on `http://localhost:5000`, then re-invoke this audit in "Full E2E" mode.

---

## 8. Appendix — Files Examined

### Backend
- `Skillforge/Program.cs`, `appsettings.json`, `Skillforge.csproj`, `.env`
- All 14 files in `Skillforge/Controllers/`
- All files in `Skillforge/Service/` (~35 files)
- All files in `Skillforge/Repository/` (~30 files)
- All files in `Skillforge/Dto/` (~37 files)
- All files in `Skillforge/Utility/` and `Skillforge/Constants/`
- `Skillforge.Databases/Skillforge.Data/SkillForgeDB.cs`
- All domain entities in `Skillforge.Databases/Skillforge.Domain/`
- `Skillforge.Databases/ER_Diagram.md`

### Frontend
- `skillforge-frontend/package.json`, `angular.json`, `tsconfig*.json`
- `src/main.ts`, `index.html`, `styles.css`
- `src/app/app.ts`, `app.config.ts`, `app.routes.ts`, `app.spec.ts`, `app.html`, `app.css`
- All files in `src/app/core/` (guards, interceptors, services, models, store, modules)
- All files in `src/app/pages/` (auth, dashboard, catalog, enrollment, assessments, competency, compliance, reports, notifications, iam, profile)
- All files in `src/app/shared/` (layout, components, modules)
- All `environments/environment*.ts`

### Database
- `seed.sql`, `seed_employee_flow.sql`, `seed_full.sql`, `SEEDING.sql`, `See Database Tables.sql`
- Connected to `LTIN719061\SQLEXPRESS` / `tmpDb8` via `sqlcmd`.
