/**
 * Student API Service
 * All student-related API calls
 */

import { apiClient } from './client';
import type {
    ApiResponse,
    Student,
    CreateStudentRequest,
    UpdateStudentRequest
} from './types';

export const studentApi = {
    /**
     * Get all students
     */
    getAll: async (): Promise<Student[]> => {
        const response = await apiClient.get<ApiResponse<Student[]>>('/students');
        return response.data.data || [];
    },

    /**
     * Get student by ID
     */
    getById: async (id: string): Promise<Student> => {
        const response = await apiClient.get<ApiResponse<Student>>(`/students/${id}`);
        if (!response.data.data) {
            throw new Error('Student not found');
        }
        return response.data.data;
    },

    /**
     * Create new student
     */
    create: async (data: CreateStudentRequest): Promise<Student> => {
        const formData = new FormData();
        formData.append('studentNumber', data.studentNumber);
        formData.append('firstName', data.firstName);
        formData.append('lastName', data.lastName);
        formData.append('email', data.email);
        if (data.photo) {
            formData.append('photo', data.photo);
        }

        const response = await apiClient.post<ApiResponse<Student>>('/students', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });

        if (!response.data.data) {
            throw new Error('Failed to create student');
        }
        return response.data.data;
    },

    /**
     * Update student
     */
    update: async (id: string, data: UpdateStudentRequest): Promise<Student> => {
        const response = await apiClient.put<ApiResponse<Student>>(`/students/${id}`, data);
        if (!response.data.data) {
            throw new Error('Failed to update student');
        }
        return response.data.data;
    },

    /**
     * Delete student
     */
    delete: async (id: string): Promise<void> => {
        await apiClient.delete(`/students/${id}`);
    },

    /**
     * Update student photo
     */
    updatePhoto: async (id: string, photo: File): Promise<Student> => {
        const formData = new FormData();
        formData.append('photo', photo);

        const response = await apiClient.put<ApiResponse<Student>>(
            `/students/${id}/photo`,
            formData,
            {
                headers: { 'Content-Type': 'multipart/form-data' }
            }
        );

        if (!response.data.data) {
            throw new Error('Failed to update photo');
        }
        return response.data.data;
    },

    /**
     * Delete student photo
     */
    deletePhoto: async (id: string): Promise<void> => {
        await apiClient.delete(`/students/${id}/photo`);
    }
};
