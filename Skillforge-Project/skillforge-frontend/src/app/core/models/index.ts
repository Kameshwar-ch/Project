export type UserRole = 'Employee' | 'Trainer' | 'Manager' | 'HR' | 'Admin';

export interface User {
  userID: number;
  userName: string;
  email: string;
  roleName: UserRole;
  phone: string;
  status: boolean;
}

export interface AuthTokens {
  access_token: string;
  expires: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  role: UserRole;
  phone: string;
  password: string;
}

export interface JwtPayload {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  exp: number;
  iss: string;
  aud: string;
}

export interface Course {
  courseID: number;
  title: string;
  description: string;
  trainerID: number;
  trainerName?: string;
  duration: number;
  status: boolean;
  enrollmentCount?: number;
}

export interface CourseModule {
  moduleID: number;
  courseID: number;
  title: string;
  contentURI: string;
  duration: number;
  status: boolean;
}

export interface CreateModuleRequest {
  title: string;
  contentURI: string;
  duration: number;
}

export interface CreateCourseRequest {
  title: string;
  description: string;
  trainerID: number;
  duration: number;
  status: boolean;
}

export interface Enrollment {
  enrollmentId?: number;
  employeeName?: string;
  courseName?: string;
  courseId: number;
  employeeId: number;
  enrollmentDate?: string;
  status?: string;
  attendance?: string;
}

/** Matches EnrollmentResponseDto from the backend */
export interface EnrollmentResponse {
  enrollmentId: number;
  employeeId: number;
  employeeName: string;
  courseId: number;
  courseName: string;
  enrollmentDate: string;
  status: string;
  lastAttendance: string;
}

export interface SfNotification {
  notificationId: number;
  userId: number;
  courseId?: number;
  message: string;
  category: string;
  status: string;
  createdDate: string;
}

export interface Certification {
  certificationId?: number;
  employeeId?: number;
  employeeName?: string;
  courseId?: number;
  courseName?: string;
  issuedDate?: string;
  expiryDate?: string;
  status?: string;
}

/** Matches CertificationResponseDto from the backend */
export interface CertificationResponse {
  certificationId: number;
  employeeId: number;
  employeeName: string;
  courseId: number;
  courseName: string;
  courseDescription: string;
  issueDate: string;
  expiryDate: string;
  status: string;
}

export interface Assessment {
  assessmentId?: number;
  courseId?: number;
  courseName?: string;
  type?: string;
  maxScore?: number;
  scheduledDate?: string;
}

/** Matches AssessmentResponseDto from the backend */
export interface AssessmentResponse {
  assessmentId: number;
  courseId: number;
  courseName: string;
  moduleId?: number;
  moduleName?: string;
  type: string;
  maxScore: number;
  passingScore: number;
  date: string;
  courseStatus: boolean;
}

export interface EmployeeAssessment {
  assessmentId: number;
  courseId: number;
  courseName: string;
  moduleId?: number;
  moduleName?: string;
  type: string;
  maxScore: number;
  date: string;
  isDone: boolean;
  score?: number;
  resultStatus?: 'Pending' | 'Pass' | 'Fail';
}

export interface PendingResult {
  assessmentId: number;
  employeeId: number;
  courseName: string;
  assessmentType: string;
  employeeName: string;
  score: number;
  maxScore: number;
  passingScore: number;
}

export interface CompetencyMatrix {
  competencyId?: number;
  competencyName?: string;
  description?: string;
  level?: string;
  gap?: string;
}

/** Matches CompetencyMatrixDto from the backend */
export interface CompetencyMatrixEntry {
  employeeId: number;
  employeeName: string;
  skills: { skillName: string; level: string }[];
}

/** Matches BulkEnrollmentResponseDto + EnrollmentResultItem from the backend */
export interface BulkEnrollResult {
  totalRequested: number;
  succeeded: number;
  failed: number;
  results: { employeeId: number; enrollmentId?: number; status: string; reason?: string }[];
}

/** Matches CourseAttendanceDto / GetCourseAttendanceResponseDto from the backend */
export interface AttendancePreviewRecord {
  enrollmentID: number;
  employeeName: string;
  courseStatus: string;
  loginDate: string;
}

export interface AttendancePreview {
  courseID: number;
  attendanceDate: string;
  records: AttendancePreviewRecord[];
}

/** Matches SkillGapResponseDto from the backend */
export interface SkillGapItem {
  skillGapID: number;
  employeeId: number;
  employeeName: string;
  competencyName: string;
  gapLevel: string;
  dateIdentified: string;
}

export interface CompetencyItem {
  competencyId: number;
  name: string;
  description: string;
  level: string; // 'Beginner' | 'Intermediate' | 'Advanced'
}

export interface CreateCompetencyRequest {
  name: string;
  description: string;
  level: string;
}

export interface CreateSkillGapRequest {
  employeeId: number;
  competencyId: number;
  gapLevel: string; // 'Low' | 'Medium' | 'High'
  dateIdentified: string;
}

export interface ComplianceRecord {
  recordId?: number;
  employeeId?: number;
  employeeName?: string;
  certificationName?: string;
  status?: string;
  date?: string;
}

export interface AuditLog {
  logId?: number;
  userId?: number;
  userName?: string;
  action?: string;
  resource?: string;
  timestamp?: string;
}

// API response shapes for compliance & audit
export interface AuditLogItem {
  auditID: number;
  userID?: number;
  action: string;
  resource: string;
  timestamp: string;
}

export interface ComplianceItem {
  complianceId: number;
  employeeId: number;
  employeeName: string;
  certificationId: number;
  courseName: string;
  status: boolean;
  date: string;
}

export interface ComplianceSummary {
  totalEmployees: number;
  compliantCount: number;
  nonCompliantCount: number;
  complianceRate: number;
  records: ComplianceItem[];
}

export interface ApiResponse<T> {
  data?: T;
  message?: string;
  statusCode?: number;
}

export interface AttendanceRequestItem {
  requestID: number;
  enrollmentID: number;
  employeeName: string;
  courseName: string;
  requestDate: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  trainerNote?: string;
  createdAt: string;
}

export interface AttendanceHistoryItem {
  attendanceID: number;
  attendanceDate: string;
  status: 'Present' | 'Absent';
}

export interface EnrollmentAttendanceHistory {
  enrollmentID: number;
  employeeName: string;
  courseName: string;
  presentCount: number;
  absentCount: number;
  records: AttendanceHistoryItem[];
}

export interface AuditItem {
  auditID: number;
  hrid: number;
  hrName: string;
  scope: string;
  findings: string;
  date: string;
  status: boolean;
}
