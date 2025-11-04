/**
 * Class API Service
 * All class-related API calls
 */

import { apiClient } from './client';
import type {
    ApiResponse,
    Class,
    CreateClassRequest,
    UpdateClassRequest,
    EnrollStudentsRequest,
    Student
} from './types';

export const classApi = {
    /**
     * Get all classes
     */
    getAll: async (): Promise<Class[]> => {
        const response = await apiClient.get<ApiResponse<Class[]>>('/classes');
        return response.data.data || [];
    },

    /**
     * Get class by ID
     */
    getById: async (id: string): Promise<Class> => {
        const response = await apiClient.get<ApiResponse<Class>>(`/classes/${id}`);
        if (!response.data.data) {
            throw new Error('Class not found');
        }
        return response.data.data;
    },

    /**
     * Create new class
     */
    create: async (data: CreateClassRequest): Promise<Class> => {
        const response = await apiClient.post<ApiResponse<Class>>('/classes', data);
        if (!response.data.data) {
            throw new Error('Failed to create class');
        }
        return response.data.data;
    },

    /**
     * Update class
     */
    update: async (id: string, data: UpdateClassRequest): Promise<Class> => {
        const response = await apiClient.put<ApiResponse<Class>>(`/classes/${id}`, data);
        if (!response.data.data) {
            throw new Error('Failed to update class');
        }
        return response.data.data;
    },

    /**
     * Delete class
     */
    delete: async (id: string): Promise<void> => {
        await apiClient.delete(`/classes/${id}`);
    },

    /**
     * Enroll students in class
     */
    enrollStudents: async (classId: string, studentIds: string[]): Promise<string> => {
        const request: EnrollStudentsRequest = { studentIds };
        const response = await apiClient.post<ApiResponse<string>>(
            `/classes/${classId}/enroll`,
            request
        );
        return response.data.message || 'Students enrolled successfully';
    },

    /**
     * Get enrolled students in class
     */
    getEnrollments: async (classId: string): Promise<Student[]> => {
        const response = await apiClient.get<ApiResponse<Student[]>>(
            `/classes/${classId}/students`
        );
        return response.data.data || [];
    },

    /**
     * Unenroll student from class
     */
    unenrollStudent: async (classId: string, studentId: string): Promise<void> => {
        await apiClient.delete(`/classes/${classId}/students/${studentId}`);
    }
};
