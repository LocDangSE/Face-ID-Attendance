/**
 * Axios API Client
 * Configured instance with interceptors for logging and error handling
 */

import axios, { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import type { ApiResponse } from './types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5225/api';

// Create axios instance
export const apiClient = axios.create({
    baseURL: API_BASE_URL,
    timeout: 120000, // 120 seconds for file uploads
    headers: {
        'Content-Type': 'application/json'
    }
});

// Request interceptor - Add logging
apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const method = config.method?.toUpperCase();
        const url = config.url;
        console.log(`[API Request] ${method} ${url}`);

        // Log body for POST/PUT/PATCH (exclude FormData for readability)
        if (config.data && !(config.data instanceof FormData)) {
            console.log('[API Request Body]', config.data);
        }

        return config;
    },
    (error) => {
        console.error('[API Request Error]', error);
        return Promise.reject(error);
    }
);

// Response interceptor - Handle errors globally
apiClient.interceptors.response.use(
    (response: AxiosResponse) => {
        console.log(`[API Response] ${response.status} ${response.config.url}`);
        return response;
    },
    (error: AxiosError<ApiResponse<unknown>>) => {
        // Log error details
        if (error.response) {
            console.error(`[API Error] ${error.response.status} ${error.config?.url}`);
            console.error('[API Error Data]', error.response.data);
        } else if (error.request) {
            console.error('[API Error] No response received', error.request);
        } else {
            console.error('[API Error]', error.message);
        }

        return Promise.reject(error);
    }
);

// Helper function to extract error message from API response
export const getErrorMessage = (error: unknown): string => {
    if (axios.isAxiosError(error)) {
        const apiError = error as AxiosError<ApiResponse<unknown>>;

        // Check for API response error message
        if (apiError.response?.data?.message) {
            return apiError.response.data.message;
        }

        // Check for API response errors array
        if (apiError.response?.data?.errors && Array.isArray(apiError.response.data.errors)) {
            return apiError.response.data.errors.join(', ');
        }

        // Fallback to axios error message
        return apiError.message;
    }

    // Handle non-axios errors
    if (error instanceof Error) {
        return error.message;
    }

    return 'An unknown error occurred';
};

export default apiClient;
