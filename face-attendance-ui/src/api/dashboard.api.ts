/**
 * Dashboard API Service
 * Dashboard statistics and recent data
 */

import { apiClient } from './client';
import type {
    ApiResponse,
    DashboardStats,
    RecentSessionDto
} from './types';

export const dashboardApi = {
    /**
     * Get dashboard statistics
     */
    getStats: async (): Promise<DashboardStats> => {
        const response = await apiClient.get<ApiResponse<DashboardStats>>(
            '/attendance/dashboard/stats'
        );
        if (!response.data.data) {
            throw new Error('Failed to get dashboard stats');
        }
        return response.data.data;
    },

    /**
     * Get recent sessions
     */
    getRecentSessions: async (limit: number = 5): Promise<RecentSessionDto[]> => {
        const response = await apiClient.get<ApiResponse<RecentSessionDto[]>>(
            `/dashboard/recent-sessions?limit=${limit}`
        );
        return response.data.data || [];
    },

    /**
     * Get today's attendance
     */
    getTodayAttendance: async (): Promise<RecentSessionDto[]> => {
        const response = await apiClient.get<ApiResponse<RecentSessionDto[]>>(
            '/dashboard/today-attendance'
        );
        return response.data.data || [];
    }
};
