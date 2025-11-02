import axios from 'axios';

const API_BASE_URL = 'http://localhost:5225/api';

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Students API
export const studentsAPI = {
    getAll: () => api.get('/students'),
    getById: (id) => api.get(`/students/${id}`),
    create: (formData) => {
        return api.post('/students', formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        });
    },
    update: (id, data) => api.put(`/students/${id}`, data),
    delete: (id) => api.delete(`/students/${id}`),
    updatePhoto: (id, formData) => {
        return api.put(`/students/${id}/photo`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        });
    },
    deletePhoto: (id) => api.delete(`/students/${id}/photo`),
};

// Classes API
export const classesAPI = {
    getAll: () => api.get('/classes'),
    getById: (id) => api.get(`/classes/${id}`),
    create: (data) => api.post('/classes', data),
    update: (id, data) => api.put(`/classes/${id}`, data),
    delete: (id) => api.delete(`/classes/${id}`),
    enrollStudents: (classId, studentIds) =>
        api.post(`/classes/${classId}/enroll`, { studentIds }),
    getEnrollments: (classId) => api.get(`/classes/${classId}/students`),
    unenrollStudent: (classId, studentId) =>
        api.delete(`/classes/${classId}/students/${studentId}`),
};

// Attendance Sessions API
export const sessionsAPI = {
    getAll: () => api.get('/attendance/sessions'),
    getById: (id) => api.get(`/attendance/sessions/${id}`),
    create: (data) => api.post('/attendance/sessions', data),
    update: (id, data) => api.put(`/attendance/sessions/${id}`, data),
    end: (id) => api.put(`/attendance/sessions/${id}/complete`),
    getByClass: (classId) => api.get(`/attendance/sessions/class/${classId}`),
    getRecords: (sessionId) => api.get(`/attendance/sessions/${sessionId}/details`),
};

// Attendance Recognition API
export const attendanceAPI = {
    recognizeFace: (formData) => {
        return api.post('/attendance/recognize', formData, {
            headers: { 'Content-Type': 'multipart/form-data' },
        });
    },
    markManual: (data) => api.post('/attendance/mark', data),
    getRecords: (sessionId) => api.get(`/attendance/records/${sessionId}`),
    updateRecord: (recordId, data) => api.put(`/attendance/records/${recordId}`, data),
};

// Dashboard API
export const dashboardAPI = {
    getStats: () => api.get('/dashboard/stats'),
    getRecentSessions: (limit = 5) => api.get(`/dashboard/recent-sessions?limit=${limit}`),
    getTodayAttendance: () => api.get('/dashboard/today-attendance'),
};

export default api;
