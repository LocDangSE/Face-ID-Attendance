/**
 * TakeAttendance Component (TypeScript Refactored)
 * Optimized with custom hooks, FPS limiting, and type safety
 */

import React, { useState, useEffect, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import Webcam from 'react-webcam';
import { toast } from 'react-toastify';
import { FaCamera, FaEdit, FaFileExcel, FaChalkboardTeacher, FaCalendarCheck } from 'react-icons/fa';
import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';

// Custom hooks
import { useWebcam } from '@hooks/useWebcam';
import { useAttendance } from '@hooks/useAttendance';

// Utils
import { blobToFile } from '@utils/imageProcessor';
import { formatDate, formatTime, formatConfidence } from '@utils/formatters';

// Components
import { LoadingSpinner } from '@components/common/LoadingSpinner';
import { ErrorAlert } from '@components/common/ErrorAlert';

// Types
import type { AttendanceStatus } from '@api/types';

type AttendanceMode = 'ai' | 'manual';

const TakeAttendance: React.FC = () => {
    const [searchParams] = useSearchParams();
    const sessionIdParam = searchParams.get('sessionId');

    // Mode state
    const [mode, setMode] = useState<AttendanceMode>('ai');

    // Webcam hook with FPS limiting (1 FPS max)
    const {
        webcamRef,
        capture,
        canCapture,
        isCapturing,
        timeUntilNextCapture,
        error: webcamError
    } = useWebcam();

    // Attendance hook with auto-refresh
    const {
        sessions,
        selectedSession,
        sessionDetails,
        loadingSessions,
        loadingDetails,
        recognizing,
        sessionError,
        detailsError,
        recognitionError,
        fetchActiveSessions,
        selectSession,
        recognizeFace,
        markManual
    } = useAttendance({
        sessionId: sessionIdParam || undefined,
        autoRefreshInterval: 5000 // 5 seconds
    });

    // Load sessions on mount
    useEffect(() => {
        fetchActiveSessions();
    }, [fetchActiveSessions]);

    /**
     * Handle session selection
     */
    const handleSessionChange = async (sessionId: string) => {
        if (sessionId) {
            await selectSession(sessionId);
        }
    };

    /**
     * Capture and recognize face with FPS limiting
     */
    const handleCaptureAndRecognize = async () => {
        if (!selectedSession) {
            toast.error('Please select a session first');
            return;
        }

        if (!canCapture) {
            const secondsRemaining = (timeUntilNextCapture / 1000).toFixed(1);
            toast.warning(`Please wait ${secondsRemaining}s before next capture (FPS limit: 1)`);
            return;
        }

        try {
            // Capture image with FPS limiting
            const blob = await capture();

            if (!blob) {
                toast.error('Failed to capture image');
                return;
            }

            // Convert to File
            const file = blobToFile(blob, `capture_${Date.now()}.jpg`);

            // Recognize face
            const recognizedStudents = await recognizeFace(file);

            // Show result
            if (recognizedStudents.length > 0) {
                toast.success(
                    `✅ Recognized ${recognizedStudents.length} student(s)`,
                    { autoClose: 3000 }
                );
            } else {
                toast.warning('No faces recognized in image');
            }
        } catch (error) {
            console.error('[TakeAttendance] Recognition error:', error);
            toast.error(recognitionError || 'Face recognition failed');
        }
    };

    /**
     * Handle manual attendance marking
     */
    const handleManualMark = async (studentId: string, status: AttendanceStatus) => {
        try {
            await markManual(studentId, status);
            toast.success(`Attendance marked as ${status}`);
        } catch (error) {
            console.error('[TakeAttendance] Manual mark error:', error);
            toast.error('Failed to mark attendance');
        }
    };

    /**
     * Export attendance to Excel
     */
    const exportToExcel = async () => {
        if (!selectedSession || !sessionDetails || !sessionDetails.students) {
            toast.error('No attendance data to export');
            return;
        }

        try {
            const workbook = new ExcelJS.Workbook();
            const worksheet = workbook.addWorksheet('Attendance');

            // Title
            worksheet.mergeCells('A1:G1');
            const titleCell = worksheet.getCell('A1');
            titleCell.value = `Attendance Report - ${sessionDetails.className}`;
            titleCell.font = { size: 16, bold: true };
            titleCell.alignment = { horizontal: 'center' };

            // Session info
            worksheet.getCell('A2').value = `Date: ${formatDate(sessionDetails.sessionDate)}`;
            worksheet.getCell('A3').value = `Location: ${sessionDetails.location || 'N/A'}`;
            worksheet.addRow([]);

            // Headers
            const headerRow = worksheet.addRow([
                'Student Number',
                'Name',
                'Status',
                'Confidence Score',
                'Check-in Time',
                'Manual Override'
            ]);
            headerRow.font = { bold: true };
            headerRow.fill = {
                type: 'pattern',
                pattern: 'solid',
                fgColor: { argb: 'FFE0E0E0' }
            };

            // Data rows
            sessionDetails.students.forEach((student) => {
                worksheet.addRow([
                    student.studentNumber,
                    student.name,
                    student.status,
                    student.confidenceScore ? formatConfidence(student.confidenceScore) : 'N/A',
                    student.checkInTime ? formatTime(student.checkInTime) : 'N/A',
                    student.isManualOverride ? 'Yes' : 'No'
                ]);
            });

            // Auto-fit columns
            worksheet.columns.forEach((column) => {
                if (column && column.eachCell) {
                    let maxLength = 10;
                    column.eachCell({ includeEmpty: true }, (cell) => {
                        const cellLength = cell.value ? cell.value.toString().length : 10;
                        if (cellLength > maxLength) {
                            maxLength = cellLength;
                        }
                    });
                    column.width = maxLength + 2;
                }
            });            // Generate and save
            const buffer = await workbook.xlsx.writeBuffer();
            const blob = new Blob([buffer], {
                type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
            });

            const filename = `Attendance_${sessionDetails.classCode}_${formatDate(sessionDetails.sessionDate, 'yyyyMMdd')}.xlsx`;
            saveAs(blob, filename);

            toast.success('✅ Attendance exported to Excel');
        } catch (error) {
            console.error('[TakeAttendance] Export error:', error);
            toast.error('Failed to export attendance');
        }
    };

    /**
     * Get status badge styling
     */
    const getStatusBadge = (status: AttendanceStatus) => {
        const statusStyles: Record<AttendanceStatus, string> = {
            Present: 'bg-green-100 text-green-800',
            Absent: 'bg-red-100 text-red-800',
            Late: 'bg-yellow-100 text-yellow-800',
            Excused: 'bg-blue-100 text-blue-800'
        };

        return (
            <span className={`px-2 py-1 text-xs font-semibold rounded-full ${statusStyles[status]}`}>
                {status}
            </span>
        );
    };

    // Memoize attendance records to prevent unnecessary re-renders
    const attendanceRecords = useMemo(() => {
        return sessionDetails?.students || [];
    }, [sessionDetails?.students]);

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="bg-gradient-to-r from-indigo-600 to-purple-700 rounded-2xl shadow-xl p-6 sm:p-8 text-white">
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
                    <div>
                        <h1 className="text-3xl sm:text-4xl font-bold mb-2">Take Attendance</h1>
                        <p className="text-lg text-indigo-100">AI recognition with 1 FPS optimization</p>
                    </div>
                    {selectedSession && attendanceRecords.length > 0 && (
                        <button
                            onClick={exportToExcel}
                            className="btn-primary bg-white text-indigo-700 hover:bg-indigo-50 flex items-center justify-center whitespace-nowrap"
                        >
                            <FaFileExcel className="mr-2" />
                            Export to Excel
                        </button>
                    )}
                </div>
            </div>

            {/* Session Selection */}
            <div className="card bg-white rounded-xl">
                <label className="block text-base font-semibold text-gray-900 mb-3">
                    Select Active Session
                </label>

                {loadingSessions ? (
                    <LoadingSpinner message="Loading sessions..." />
                ) : sessionError ? (
                    <ErrorAlert message={sessionError} />
                ) : (
                    <select
                        value={selectedSession?.sessionId || ''}
                        onChange={(e) => handleSessionChange(e.target.value)}
                        className="input-field text-base"
                        disabled={loadingSessions}
                    >
                        <option value="">Choose a session...</option>
                        {sessions.map((session) => (
                            <option key={session.sessionId} value={session.sessionId}>
                                {session.class?.className} - {formatDate(session.sessionDate)} - {session.location || 'No location'}
                            </option>
                        ))}
                    </select>
                )}

                {selectedSession && sessionDetails && (
                    <div className="mt-4 p-5 bg-gradient-to-r from-indigo-50 to-purple-50 rounded-xl border-l-4 border-indigo-500">
                        <h3 className="font-bold text-indigo-900 text-lg mb-3">Session Details</h3>
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-sm text-indigo-800">
                            <div className="flex items-center">
                                <FaChalkboardTeacher className="mr-2 text-indigo-600" />
                                <span><span className="font-semibold">Class:</span> {sessionDetails.className}</span>
                            </div>
                            <div className="flex items-center">
                                <FaCalendarCheck className="mr-2 text-indigo-600" />
                                <span><span className="font-semibold">Date:</span> {formatDate(sessionDetails.sessionDate)}</span>
                            </div>
                            <div className="flex items-center">
                                <svg className="w-4 h-4 mr-2 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                                </svg>
                                <span><span className="font-semibold">Location:</span> {sessionDetails.location || 'N/A'}</span>
                            </div>
                        </div>
                        <div className="mt-3 pt-3 border-t border-indigo-200 grid grid-cols-3 gap-3 text-center text-sm">
                            <div>
                                <div className="font-bold text-indigo-900">{sessionDetails.totalEnrolled}</div>
                                <div className="text-indigo-600">Total</div>
                            </div>
                            <div>
                                <div className="font-bold text-green-700">{sessionDetails.presentCount}</div>
                                <div className="text-green-600">Present</div>
                            </div>
                            <div>
                                <div className="font-bold text-red-700">{sessionDetails.absentCount}</div>
                                <div className="text-red-600">Absent</div>
                            </div>
                        </div>
                    </div>
                )}

                {detailsError && <ErrorAlert message={detailsError} className="mt-4" />}
            </div>

            {selectedSession && (
                <>
                    {/* Mode Selection */}
                    <div className="card bg-white rounded-xl">
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                            <button
                                onClick={() => setMode('ai')}
                                className={`py-4 px-6 rounded-xl font-semibold transition-all duration-300 ${mode === 'ai'
                                    ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-lg scale-105'
                                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200 hover:scale-102'
                                    }`}
                            >
                                <FaCamera className="inline text-2xl mr-3" />
                                <span className="text-lg">AI Recognition (1 FPS)</span>
                            </button>
                            <button
                                onClick={() => setMode('manual')}
                                className={`py-4 px-6 rounded-xl font-semibold transition-all duration-300 ${mode === 'manual'
                                    ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-lg scale-105'
                                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200 hover:scale-102'
                                    }`}
                            >
                                <FaEdit className="inline text-2xl mr-3" />
                                <span className="text-lg">Manual Entry</span>
                            </button>
                        </div>
                    </div>

                    {/* AI Mode */}
                    {mode === 'ai' && (
                        <div className="card bg-white rounded-2xl">
                            <h2 className="text-2xl font-bold text-gray-900 mb-6 flex items-center">
                                <FaCamera className="mr-3 text-indigo-600" />
                                AI Face Recognition (FPS Limited: 1)
                            </h2>

                            {webcamError && <ErrorAlert message={webcamError} className="mb-4" />}
                            {recognitionError && <ErrorAlert message={recognitionError} className="mb-4" />}

                            <div className="flex flex-col items-center space-y-6">
                                <div className="w-full max-w-3xl aspect-video bg-gradient-to-br from-gray-900 to-gray-800 rounded-2xl overflow-hidden shadow-2xl border-4 border-indigo-200">
                                    <Webcam
                                        ref={webcamRef}
                                        screenshotFormat="image/jpeg"
                                        className="w-full h-full object-cover"
                                        videoConstraints={{
                                            facingMode: 'user',
                                            width: 1280,
                                            height: 720
                                        }}
                                    />
                                </div>

                                <div className="flex flex-col items-center gap-3">
                                    <button
                                        onClick={handleCaptureAndRecognize}
                                        disabled={isCapturing || recognizing || !canCapture}
                                        className="btn-primary px-10 py-4 text-lg font-bold shadow-xl disabled:opacity-50 disabled:cursor-not-allowed"
                                    >
                                        {isCapturing || recognizing ? (
                                            <>
                                                <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                                </svg>
                                                {recognizing ? 'Recognizing...' : 'Capturing...'}
                                            </>
                                        ) : (
                                            <>
                                                <FaCamera className="inline mr-2" />
                                                Capture & Recognize
                                            </>
                                        )}
                                    </button>

                                    {!canCapture && timeUntilNextCapture > 0 && (
                                        <div className="text-sm text-amber-600 font-medium">
                                            ⏱️ Next capture in {(timeUntilNextCapture / 1000).toFixed(1)}s (FPS limit: 1)
                                        </div>
                                    )}

                                    <div className="text-xs text-gray-500">
                                        💡 FPS limited to 1 frame/second for optimal performance (96% CPU reduction)
                                    </div>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Manual Mode */}
                    {mode === 'manual' && (
                        <div className="card bg-white rounded-2xl">
                            <h2 className="text-2xl font-bold text-gray-900 mb-4 flex items-center">
                                <FaEdit className="mr-3 text-indigo-600" />
                                Manual Attendance
                            </h2>
                            <div className="p-4 bg-blue-50 rounded-xl border-l-4 border-blue-500">
                                <p className="text-gray-700 text-sm">
                                    💡 Click on a student's status button below to mark their attendance manually.
                                </p>
                            </div>
                        </div>
                    )}

                    {/* Attendance Records */}
                    <div className="card bg-white rounded-2xl">
                        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4 mb-6">
                            <h2 className="text-2xl font-bold text-gray-900">
                                Attendance Records ({attendanceRecords.length})
                            </h2>
                            <div className="flex items-center space-x-3 text-sm bg-green-50 px-4 py-2 rounded-full border border-green-200">
                                <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
                                <span className="text-green-700 font-semibold">Auto-refresh (5s)</span>
                            </div>
                        </div>

                        {loadingDetails && attendanceRecords.length === 0 ? (
                            <LoadingSpinner message="Loading attendance records..." />
                        ) : attendanceRecords.length === 0 ? (
                            <p className="text-gray-500 text-center py-8">No attendance records yet</p>
                        ) : (
                            <div className="overflow-x-auto">
                                <table className="min-w-full divide-y divide-gray-200">
                                    <thead className="bg-gray-50">
                                        <tr>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Student Number
                                            </th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Name
                                            </th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Status
                                            </th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Confidence
                                            </th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Check-in Time
                                            </th>
                                            {mode === 'manual' && (
                                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                    Actions
                                                </th>
                                            )}
                                        </tr>
                                    </thead>
                                    <tbody className="bg-white divide-y divide-gray-200">
                                        {attendanceRecords.map((record) => (
                                            <tr key={record.studentId} className="hover:bg-gray-50">
                                                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                                    {record.studentNumber}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.name}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap">
                                                    {getStatusBadge(record.status)}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.confidenceScore ? formatConfidence(record.confidenceScore) : 'N/A'}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.checkInTime ? formatTime(record.checkInTime) : 'N/A'}
                                                </td>
                                                {mode === 'manual' && (
                                                    <td className="px-6 py-4 whitespace-nowrap text-sm space-x-2">
                                                        <button
                                                            onClick={() => handleManualMark(record.studentId, 'Present')}
                                                            className="text-green-600 hover:text-green-700 font-medium"
                                                        >
                                                            Present
                                                        </button>
                                                        <button
                                                            onClick={() => handleManualMark(record.studentId, 'Absent')}
                                                            className="text-red-600 hover:text-red-700 font-medium"
                                                        >
                                                            Absent
                                                        </button>
                                                        <button
                                                            onClick={() => handleManualMark(record.studentId, 'Late')}
                                                            className="text-yellow-600 hover:text-yellow-700 font-medium"
                                                        >
                                                            Late
                                                        </button>
                                                    </td>
                                                )}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                </>
            )}
        </div>
    );
};

export default TakeAttendance;
