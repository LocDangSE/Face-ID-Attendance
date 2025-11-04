/**
 * Attendance API Service
 * All attendance-related API calls
 */

import { apiClient } from './client';
import type {
    ApiResponse,
    AttendanceSession,
    CreateSessionRequest,
    CreateSessionResponse,
    CompleteSessionResponse,
    SessionDetailsDto,
    RecognitionResponse,
    ManualAttendanceRequest,
    AttendanceReport,
    StudentHistory
} from './types'; export const attendanceApi = {
    /**
     * Get all attendance sessions
     */
    getAllSessions: async (): Promise<AttendanceSession[]> => {
        const response = await apiClient.get<ApiResponse<AttendanceSession[]>>('/attendance/sessions');
        return response.data.data || [];
    },

    /**
     * Get active sessions only
     */
    getActiveSessions: async (): Promise<AttendanceSession[]> => {
        const response = await apiClient.get<ApiResponse<AttendanceSession[]>>('/attendance/sessions/active');
        return response.data.data || [];
    },

    /**
     * Get session by ID
     */
    getSessionById: async (sessionId: string): Promise<AttendanceSession> => {
        const response = await apiClient.get<ApiResponse<AttendanceSession>>(`/attendance/sessions/${sessionId}`);
        if (!response.data.data) {
            throw new Error('Session not found');
        }
        return response.data.data;
    },

    /**
     * Create new attendance session
     */
    createSession: async (request: CreateSessionRequest): Promise<CreateSessionResponse> => {
        const response = await apiClient.post<ApiResponse<CreateSessionResponse>>(
            '/attendance/sessions',
            request
        );
        if (!response.data.data) {
            throw new Error('Failed to create session');
        }
        return response.data.data;
    },

    /**
     * Complete attendance session
     */
    completeSession: async (sessionId: string): Promise<CompleteSessionResponse> => {
        const response = await apiClient.put<ApiResponse<CompleteSessionResponse>>(
            `/attendance/sessions/${sessionId}/complete`
        );
        if (!response.data.data) {
            throw new Error('Failed to complete session');
        }
        return response.data.data;
    },

    /**
     * Get session attendance details with student records
     */
    getSessionDetails: async (sessionId: string): Promise<SessionDetailsDto> => {
        const response = await apiClient.get<ApiResponse<SessionDetailsDto>>(
            `/attendance/sessions/${sessionId}/details`
        );
        if (!response.data.data) {
            throw new Error('Session details not found');
        }
        return response.data.data;
    },

    /**
     * Recognize face and mark attendance
     * @param sessionId - Session ID
     * @param imageFile - Image file to recognize
     */
    recognizeFace: async (sessionId: string, imageFile: File): Promise<RecognitionResponse> => {
        const formData = new FormData();
        formData.append('sessionId', sessionId);
        formData.append('image', imageFile);

        const response = await apiClient.post<RecognitionResponse>(
            '/attendance/recognize',
            formData,
            {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            }
        );

        return response.data;
    },

    /**
     * Manually mark attendance
     */
    markManual: async (request: ManualAttendanceRequest): Promise<string> => {
        const response = await apiClient.post<ApiResponse<string>>(
            '/attendance/mark',
            request
        );
        return response.data.message || 'Attendance marked successfully';
    },

    /**
     * Get attendance report for a class
     */
    getAttendanceReport: async (
        classId: string,
        dateFrom: string,
        dateTo: string,
        studentId?: string
    ): Promise<AttendanceReport> => {
        const params = new URLSearchParams({
            classId,
            dateFrom,
            dateTo,
            ...(studentId && { studentId })
        });

        const response = await apiClient.get<ApiResponse<AttendanceReport>>(
            `/attendance/reports?${params.toString()}`
        );

        if (!response.data.data) {
            throw new Error('Report not found');
        }
        return response.data.data;
    },

    /**
     * Export attendance to Excel
     */
    exportToExcel: async (
        classId: string,
        dateFrom: string,
        dateTo: string
    ): Promise<Blob> => {
        const params = new URLSearchParams({
            classId,
            dateFrom,
            dateTo
        });

        const response = await apiClient.get(`/attendance/export/excel?${params.toString()}`, {
            responseType: 'blob'
        });

        return response.data;
    },

    /**
     * Get student attendance history
     */
    getStudentHistory: async (studentId: string, classId: string): Promise<StudentHistory> => {
        const params = new URLSearchParams({ classId });
        const response = await apiClient.get<ApiResponse<StudentHistory>>(
            `/attendance/students/${studentId}/history?${params.toString()}`
        );

        if (!response.data.data) {
            throw new Error('History not found');
        }
        return response.data.data;
    }
};
