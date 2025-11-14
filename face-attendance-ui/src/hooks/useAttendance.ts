/**
 * useAttendance Hook
 * Attendance-specific business logic and state management
 * 
 * ANTI-LOOP FEATURES:
 * - Request throttling: Prevents recognition calls more frequent than RECOGNITION_THROTTLE_MS
 * - Last recognition timestamp tracking
 */

import { useState, useCallback, useEffect, useRef } from 'react';
import { attendanceApi } from '@api/attendance.api';
import { useApi } from './useApi';
import type {
    AttendanceSession,
    SessionDetailsDto,
    RecognizedStudentDto,
    AttendanceStatus
} from '@api/types';

interface UseAttendanceOptions {
    sessionId?: string;
    autoRefreshInterval?: number; // milliseconds
}

// Throttle configuration - minimum time between recognition requests (milliseconds)
const RECOGNITION_THROTTLE_MS = 2000; // 2 seconds between API calls

interface UseAttendanceReturn {
    // Session state
    sessions: AttendanceSession[];
    selectedSession: AttendanceSession | null;
    sessionDetails: SessionDetailsDto | null;

    // Loading states
    loadingSessions: boolean;
    loadingDetails: boolean;
    recognizing: boolean;

    // Error states
    sessionError: string | null;
    detailsError: string | null;
    recognitionError: string | null;

    // Actions
    fetchActiveSessions: () => Promise<void>;
    selectSession: (sessionId: string) => Promise<void>;
    recognizeFace: (imageFile: File) => Promise<RecognizedStudentDto[]>;
    markManual: (studentId: string, status: AttendanceStatus) => Promise<void>;
    refreshDetails: () => Promise<void>;
    completeSession: () => Promise<void>;
}

/**
 * Custom hook for attendance management
 * Handles session selection, face recognition, and manual marking
 * 
 * @param options - Configuration options
 * @returns Attendance state and actions
 */
export function useAttendance(options: UseAttendanceOptions = {}): UseAttendanceReturn {
    const { sessionId, autoRefreshInterval = 5000 } = options;

    // State
    const [sessions, setSessions] = useState<AttendanceSession[]>([]);
    const [selectedSession, setSelectedSession] = useState<AttendanceSession | null>(null);
    const [sessionDetails, setSessionDetails] = useState<SessionDetailsDto | null>(null);

    // ANTI-LOOP: Throttling - Track last recognition request timestamp
    const lastRecognitionTime = useRef<number>(0);

    // API hooks
    const {
        loading: loadingSessions,
        error: sessionError,
        execute: executeFetchSessions
    } = useApi(attendanceApi.getActiveSessions);

    const {
        loading: loadingDetails,
        error: detailsError,
        execute: executeFetchDetails
    } = useApi(attendanceApi.getSessionDetails);

    const {
        loading: recognizing,
        error: recognitionError,
        execute: executeRecognize
    } = useApi(attendanceApi.recognizeFace);

    const {
        execute: executeMarkManual
    } = useApi(attendanceApi.markManual);

    const {
        execute: executeComplete
    } = useApi(attendanceApi.completeSession);

    /**
     * Fetch active sessions
     */
    const fetchActiveSessions = useCallback(async () => {
        try {
            const data = await executeFetchSessions();
            setSessions(data);
        } catch (error) {
            console.error('[Attendance] Failed to fetch sessions:', error);
        }
    }, [executeFetchSessions]);

    /**
     * Select a session and load its details
     */
    const selectSession = useCallback(async (sessionId: string) => {
        try {
            // Find session in list
            const session = sessions.find(s => s.sessionId === sessionId);
            if (session) {
                setSelectedSession(session);
            }

            // Fetch detailed information
            const details = await executeFetchDetails(sessionId);
            setSessionDetails(details);
        } catch (error) {
            console.error('[Attendance] Failed to load session details:', error);
        }
    }, [sessions, executeFetchDetails]);

    /**
     * Recognize face and mark attendance
     * 
     * ANTI-LOOP: Implements request throttling
     * - Checks elapsed time since last recognition request
     * - Rejects requests that come too quickly (within RECOGNITION_THROTTLE_MS)
     * - This prevents API spam from rapid detection loops
     */
    const recognizeFace = useCallback(async (imageFile: File): Promise<RecognizedStudentDto[]> => {
        if (!selectedSession) {
            throw new Error('No session selected');
        }

        // ANTI-LOOP: Check throttle - prevent requests closer than RECOGNITION_THROTTLE_MS
        const now = Date.now();
        const timeSinceLastRecognition = now - lastRecognitionTime.current;

        if (timeSinceLastRecognition < RECOGNITION_THROTTLE_MS) {
            const waitTime = ((RECOGNITION_THROTTLE_MS - timeSinceLastRecognition) / 1000).toFixed(1);
            console.warn(
                `[Attendance] ⚠️ Recognition request throttled. Wait ${waitTime}s more. ` +
                `(Throttle: ${RECOGNITION_THROTTLE_MS}ms, Last call: ${timeSinceLastRecognition}ms ago)`
            );

            // Return empty array instead of throwing - prevents error spam in console
            return [];
        }

        // Update last recognition timestamp
        lastRecognitionTime.current = now;

        try {
            console.log('[Attendance] 🔄 Sending recognition request...');
            const response = await executeRecognize(selectedSession.sessionId, imageFile);

            // Check if API request was successful
            if (!response.success) {
                console.warn('[Attendance] ⚠️ Recognition API returned failure:', response.message);
                // Return empty array - don't throw error to avoid breaking UI flow
                return [];
            }

            // Refresh details after successful recognition
            if (response.recognizedStudents.length > 0) {
                console.log(`[Attendance] ✅ Recognized ${response.recognizedStudents.length} student(s), refreshing details...`);
                await executeFetchDetails(selectedSession.sessionId);
            } else {
                console.log('[Attendance] 🔍 No students recognized in this capture');
            }

            return response.recognizedStudents;
        } catch (error) {
            console.error('[Attendance] ❌ Recognition failed with error:', error);
            throw error;
        }
    }, [selectedSession, executeRecognize, executeFetchDetails]);

    /**
     * Manually mark attendance
     */
    const markManual = useCallback(async (studentId: string, status: AttendanceStatus) => {
        if (!selectedSession) {
            throw new Error('No session selected');
        }

        try {
            await executeMarkManual({
                sessionId: selectedSession.sessionId,
                studentId,
                status
            });

            // Refresh details after manual mark
            await executeFetchDetails(selectedSession.sessionId);
        } catch (error) {
            console.error('[Attendance] Manual mark failed:', error);
            throw error;
        }
    }, [selectedSession, executeMarkManual, executeFetchDetails]);

    /**
     * Refresh session details
     */
    const refreshDetails = useCallback(async () => {
        if (selectedSession) {
            try {
                const details = await executeFetchDetails(selectedSession.sessionId);
                setSessionDetails(details);
            } catch (error) {
                console.error('[Attendance] Failed to refresh details:', error);
            }
        }
    }, [selectedSession, executeFetchDetails]);

    /**
     * Complete current session
     */
    const completeSession = useCallback(async () => {
        if (!selectedSession) {
            throw new Error('No session selected');
        }

        try {
            await executeComplete(selectedSession.sessionId);

            // Refresh sessions list
            await fetchActiveSessions();

            // Clear selected session
            setSelectedSession(null);
            setSessionDetails(null);
        } catch (error) {
            console.error('[Attendance] Failed to complete session:', error);
            throw error;
        }
    }, [selectedSession, executeComplete, fetchActiveSessions]);

    // Auto-select session if sessionId provided
    useEffect(() => {
        if (sessionId && sessions.length > 0) {
            selectSession(sessionId);
        }
    }, [sessionId, sessions]);

    // Auto-refresh details
    useEffect(() => {
        if (selectedSession && autoRefreshInterval > 0) {
            const interval = setInterval(refreshDetails, autoRefreshInterval);
            return () => clearInterval(interval);
        }
    }, [selectedSession, autoRefreshInterval, refreshDetails]);

    // Load active sessions on mount
    useEffect(() => {
        fetchActiveSessions();
    }, []);

    return {
        // Session state
        sessions,
        selectedSession,
        sessionDetails,

        // Loading states
        loadingSessions,
        loadingDetails,
        recognizing,

        // Error states
        sessionError,
        detailsError,
        recognitionError,

        // Actions
        fetchActiveSessions,
        selectSession,
        recognizeFace,
        markManual,
        refreshDetails,
        completeSession
    };
}
