/**
 * API Type Definitions
 * Matches .NET backend DTOs for type safety across the full stack
 */

// ============================================================================
// Base API Response Types
// ============================================================================

export interface ApiResponse<T> {
    success: boolean;
    message?: string;
    data?: T;
    errors?: string[];
}

export interface PaginatedResponse<T> {
    data: T[];
    total: number;
    page: number;
    pageSize: number;
}

// ============================================================================
// Student Types
// ============================================================================

export interface Student {
    studentId: string;
    studentNumber: string;
    firstName: string;
    lastName: string;
    email: string;
    profilePhotoUrl?: string;
    isActive: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateStudentRequest {
    studentNumber: string;
    firstName: string;
    lastName: string;
    email: string;
    photo?: File;
}

export interface UpdateStudentRequest {
    studentNumber?: string;
    firstName?: string;
    lastName?: string;
    email?: string;
    isActive?: boolean;
}

// ============================================================================
// Class Types
// ============================================================================

export interface Class {
    classId: string;
    className: string;
    classCode: string;
    description?: string;
    semester?: string;
    year?: number;
    isActive: boolean;
    createdAt: string;
    enrolledStudentCount?: number;
}

export interface CreateClassRequest {
    className: string;
    classCode: string;
    description?: string;
    semester?: string;
    year?: number;
}

export interface UpdateClassRequest {
    className?: string;
    classCode?: string;
    description?: string;
    semester?: string;
    year?: number;
    isActive?: boolean;
}

export interface ClassEnrollment {
    enrollmentId: string;
    classId: string;
    studentId: string;
    student?: Student;
    enrolledAt: string;
}

export interface EnrollStudentsRequest {
    studentIds: string[];
}

// ============================================================================
// Attendance Session Types
// ============================================================================

export type SessionStatus = 'InProgress' | 'Completed' | 'Cancelled';

export interface AttendanceSession {
    sessionId: string;
    classId: string;
    class?: {
        classId: string;
        className: string;
        classCode: string;
    };
    sessionDate: string;
    sessionStartTime: string;
    sessionEndTime?: string;
    status: SessionStatus;
    location?: string;
    totalEnrolled: number;
    presentCount: number;
    absentCount: number;
    createdAt: string;
}

export interface CreateSessionRequest {
    classId: string;
    sessionDate: string;
    location?: string;
}

export interface CreateSessionResponse {
    sessionId: string;
    classId: string;
    className: string;
    sessionDate: string;
    location?: string;
    status: SessionStatus;
    supabaseFolderPath: string;
}

export interface CompleteSessionResponse {
    sessionId: string;
    presentCount: number;
    absentCount: number;
    attendanceRate: number;
}

// ============================================================================
// Attendance Record Types
// ============================================================================

export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'Excused';

export interface AttendanceRecord {
    recordId: string;
    sessionId: string;
    studentId: string;
    student?: Student;
    status: AttendanceStatus;
    confidenceScore?: number;
    markedAt: string;
    isManualOverride: boolean;
}

export interface SessionDetailsDto {
    sessionId: string;
    classId: string;
    className: string;
    classCode: string;
    sessionDate: string;
    sessionStartTime: string;
    sessionEndTime?: string;
    status: SessionStatus;
    location?: string;
    totalEnrolled: number;
    presentCount: number;
    absentCount: number;
    students?: StudentAttendanceInfo[];
}

export interface StudentAttendanceInfo {
    studentId: string;
    studentNumber: string;
    name: string;
    status: AttendanceStatus;
    confidenceScore?: number;
    checkInTime?: string;
    isManualOverride: boolean;
}

// ============================================================================
// Face Recognition Types
// ============================================================================

export interface RecognizeStudentRequest {
    sessionId: string;
    image: File;
}

export interface RecognizedStudentDto {
    studentId: string;
    studentNumber: string;
    name: string;
    confidenceScore: number;
    checkInTime: string;
    isNewRecord: boolean;
}

export interface RecognitionResponse {
    success: boolean;
    message: string;
    recognizedStudents: RecognizedStudentDto[];
    totalFacesDetected?: number;
}

export interface ManualAttendanceRequest {
    sessionId: string;
    studentId: string;
    status: AttendanceStatus;
    confidenceScore?: number;
}

// ============================================================================
// Dashboard Types
// ============================================================================

export interface DashboardStats {
    totalStudents: number;
    totalClasses: number;
    totalSessions: number;
    activeSessionsCount: number;
    averageAttendanceRate: number;
    todayAttendanceRate: number;
}

export interface RecentSessionDto {
    sessionId: string;
    className: string;
    sessionDate: string;
    location?: string;
    status: SessionStatus;
    presentCount: number;
    totalEnrolled: number;
    attendanceRate: number;
}

// ============================================================================
// Report Types
// ============================================================================

export interface AttendanceReport {
    classId: string;
    className: string;
    dateFrom: string;
    dateTo: string;
    totalSessions: number;
    studentReports: StudentAttendanceReport[];
}

export interface StudentAttendanceReport {
    studentId: string;
    studentNumber: string;
    studentName: string;
    totalSessions: number;
    presentCount: number;
    absentCount: number;
    lateCount: number;
    excusedCount: number;
    attendanceRate: number;
}

export interface StudentHistory {
    studentId: string;
    studentNumber: string;
    studentName: string;
    classId: string;
    className: string;
    sessions: SessionRecord[];
    totalSessions: number;
    presentCount: number;
    attendanceRate: number;
}

export interface SessionRecord {
    sessionId: string;
    sessionDate: string;
    status: AttendanceStatus;
    confidenceScore?: number;
    markedAt?: string;
    isManualOverride: boolean;
}

// ============================================================================
// Flask API Types (Python backend)
// ============================================================================

export interface FlaskRegisterRequest {
    student_id: string;
    class_id: string;
    image: File;
}

export interface FlaskRecognizeRequest {
    class_id: string;
    image: File;
}

export interface FlaskRecognitionResult {
    success: boolean;
    total_faces_detected: number;
    recognized_students: FlaskRecognizedStudent[];
    message?: string;
    error?: string;
}

export interface FlaskRecognizedStudent {
    student_id: string;
    confidence: number;
    face_location: {
        x: number;
        y: number;
        w: number;
        h: number;
    };
}

// ============================================================================
// UI State Types
// ============================================================================

export interface LoadingState {
    isLoading: boolean;
    message?: string;
}

export interface ErrorState {
    hasError: boolean;
    message?: string;
}

export interface CameraState {
    isReady: boolean;
    isCapturing: boolean;
    lastCaptureTime: number;
    error?: string;
}

// ============================================================================
// Form Types
// ============================================================================

export interface StudentFormData {
    studentNumber: string;
    firstName: string;
    lastName: string;
    email: string;
    photo?: File;
}

export interface ClassFormData {
    className: string;
    classCode: string;
    description?: string;
    semester?: string;
    year?: number;
}

export interface SessionFormData {
    classId: string;
    sessionDate: string;
    location?: string;
}
