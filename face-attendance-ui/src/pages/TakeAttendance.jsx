import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import Webcam from 'react-webcam';
import { attendanceAPI, sessionsAPI } from '../services/api';
import { FaCamera, FaEdit, FaFileExcel, FaChalkboardTeacher, FaCalendarCheck } from 'react-icons/fa';
import { format } from 'date-fns';
import { toast } from 'react-toastify';
import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';

const TakeAttendance = () => {
    const [searchParams] = useSearchParams();
    const sessionIdParam = searchParams.get('sessionId');

    const [sessions, setSessions] = useState([]);
    const [selectedSessionId, setSelectedSessionId] = useState(sessionIdParam || '');
    const [session, setSession] = useState(null);
    const [attendanceRecords, setAttendanceRecords] = useState([]);
    const [mode, setMode] = useState('ai'); // 'ai' or 'manual'
    const [loading, setLoading] = useState(false);
    const [capturing, setCapturing] = useState(false);

    const webcamRef = useRef(null);

    useEffect(() => {
        fetchSessions();
    }, []);

    useEffect(() => {
        if (selectedSessionId) {
            fetchSessionDetails();
            fetchAttendanceRecords();
            // Refresh attendance records every 5 seconds for real-time updates
            const interval = setInterval(fetchAttendanceRecords, 5000);
            return () => clearInterval(interval);
        }
    }, [selectedSessionId]);

    const fetchSessions = async () => {
        try {
            const response = await sessionsAPI.getAll();
            if (response.data.success) {
                // Filter only InProgress sessions
                const activeSessions = response.data.data.filter(s => s.status === 'InProgress');
                setSessions(activeSessions);
            }
        } catch (error) {
            console.error('Error fetching sessions:', error);
        }
    };

    const fetchSessionDetails = async () => {
        try {
            const response = await sessionsAPI.getById(selectedSessionId);
            if (response.data.success) {
                setSession(response.data.data);
            }
        } catch (error) {
            console.error('Error fetching session details:', error);
        }
    };

    const fetchAttendanceRecords = async () => {
        if (!selectedSessionId) return;

        try {
            const response = await sessionsAPI.getRecords(selectedSessionId);
            if (response.data.success && response.data.data) {
                // Map backend response to frontend expected format
                const sessionDetails = response.data.data;
                const mappedRecords = sessionDetails.students?.map(student => ({
                    recordId: student.studentId,
                    studentId: student.studentId,
                    student: {
                        studentId: student.studentId,
                        studentNumber: student.studentNumber,
                        firstName: student.name.split(' ')[0],
                        lastName: student.name.split(' ').slice(1).join(' '),
                        email: '',
                    },
                    status: student.status,
                    confidenceScore: student.confidenceScore,
                    markedAt: student.checkInTime,
                    isManualOverride: student.isManualOverride
                })) || [];
                setAttendanceRecords(mappedRecords);
            }
        } catch (error) {
            console.error('Error fetching attendance records:', error);
        }
    };

    const captureAndRecognize = async () => {
        if (!selectedSessionId) {
            toast.error('Please select a session first');
            return;
        }

        if (!webcamRef.current) {
            toast.error('Camera not ready');
            return;
        }

        setCapturing(true);
        try {
            const imageSrc = webcamRef.current.getScreenshot();
            if (!imageSrc) {
                toast.error('Failed to capture image');
                return;
            }

            // Convert base64 to blob
            const blob = await fetch(imageSrc).then(r => r.blob());
            const file = new File([blob], 'capture.jpg', { type: 'image/jpeg' });

            const formData = new FormData();
            formData.append('sessionId', selectedSessionId);
            formData.append('image', file);

            const response = await attendanceAPI.recognizeFace(formData);

            console.log('Recognition response:', response.data);

            if (response.data.success) {
                if (response.data.recognizedStudents && response.data.recognizedStudents.length > 0) {
                    toast.success(`Recognized ${response.data.recognizedStudents.length} student(s)`);
                    fetchAttendanceRecords();
                } else {
                    toast.warning('No faces recognized');
                }
            } else {
                // Handle unsuccessful response
                console.error('Recognition failed:', response.data);
                toast.error(response.data.message || 'Face recognition failed');
            }
        } catch (error) {
            console.error('Error recognizing face:', error);
            console.error('Error details:', error.response?.data);
            toast.error(error.response?.data?.message || 'Failed to recognize face');
        } finally {
            setCapturing(false);
        }
    };

    const handleManualMark = async (studentId, status) => {
        try {
            const data = {
                sessionId: selectedSessionId,
                studentId: studentId,
                status: status,
                confidenceScore: 1.0,
            };

            const response = await attendanceAPI.markManual(data);
            if (response.data.success) {
                toast.success('Attendance marked successfully');
                fetchAttendanceRecords();
            }
        } catch (error) {
            console.error('Error marking attendance:', error);
            toast.error('Failed to mark attendance');
        }
    };

    const getStatusBadge = (status) => {
        const statusStyles = {
            Present: 'bg-green-100 text-green-800',
            Absent: 'bg-red-100 text-red-800',
            Late: 'bg-yellow-100 text-yellow-800',
            Excused: 'bg-blue-100 text-blue-800',
        };

        return (
            <span className={`px-2 py-1 text-xs font-semibold rounded-full ${statusStyles[status] || 'bg-gray-100 text-gray-800'}`}>
                {status}
            </span>
        );
    };

    const exportToExcel = async () => {
        if (!session || attendanceRecords.length === 0) {
            toast.error('No attendance records to export');
            return;
        }

        try {
            const workbook = new ExcelJS.Workbook();
            const worksheet = workbook.addWorksheet('Attendance');

            // Add title
            worksheet.mergeCells('A1:F1');
            worksheet.getCell('A1').value = `Attendance Report - ${session.class?.className}`;
            worksheet.getCell('A1').font = { size: 16, bold: true };
            worksheet.getCell('A1').alignment = { horizontal: 'center' };

            // Add session info
            worksheet.getCell('A2').value = `Date: ${format(new Date(session.sessionDate), 'MMMM dd, yyyy')}`;
            worksheet.getCell('A3').value = `Location: ${session.location || 'N/A'}`;
            worksheet.addRow([]);

            // Add headers
            const headerRow = worksheet.addRow([
                'Student Number',
                'First Name',
                'Last Name',
                'Email',
                'Status',
                'Confidence Score',
                'Marked At',
            ]);
            headerRow.font = { bold: true };
            headerRow.fill = {
                type: 'pattern',
                pattern: 'solid',
                fgColor: { argb: 'FFE0E0E0' },
            };

            // Add data
            attendanceRecords.forEach((record) => {
                worksheet.addRow([
                    record.student?.studentNumber || 'N/A',
                    record.student?.firstName || 'N/A',
                    record.student?.lastName || 'N/A',
                    record.student?.email || 'N/A',
                    record.status || 'N/A',
                    record.confidenceScore ? record.confidenceScore.toFixed(2) : 'N/A',
                    record.markedAt ? format(new Date(record.markedAt), 'yyyy-MM-dd hh:mm:ss a') : 'N/A',
                ]);
            });

            // Auto-fit columns
            worksheet.columns.forEach((column) => {
                let maxLength = 0;
                column.eachCell({ includeEmpty: true }, (cell) => {
                    const columnLength = cell.value ? cell.value.toString().length : 10;
                    if (columnLength > maxLength) {
                        maxLength = columnLength;
                    }
                });
                column.width = maxLength + 2;
            });

            // Generate file
            const buffer = await workbook.xlsx.writeBuffer();
            const blob = new Blob([buffer], {
                type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
            });
            saveAs(blob, `Attendance_${session.class?.classCode}_${format(new Date(session.sessionDate), 'yyyyMMdd')}.xlsx`);

            toast.success('Attendance exported successfully');
        } catch (error) {
            console.error('Error exporting to Excel:', error);
            toast.error('Failed to export attendance');
        }
    };

    return (
        <div className="space-y-6">
            {/* Header Section */}
            <div className="bg-gradient-to-r from-indigo-600 to-purple-700 rounded-2xl shadow-xl p-6 sm:p-8 text-white">
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
                    <div>
                        <h1 className="text-3xl sm:text-4xl font-bold mb-2">Take Attendance</h1>
                        <p className="text-lg text-indigo-100">Mark attendance using AI recognition or manually</p>
                    </div>
                    {selectedSessionId && attendanceRecords.length > 0 && (
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
                <select
                    value={selectedSessionId}
                    onChange={(e) => setSelectedSessionId(e.target.value)}
                    className="input-field text-base"
                >
                    <option value="">Choose a session...</option>
                    {sessions.map((s) => (
                        <option key={s.sessionId} value={s.sessionId}>
                            {s.class?.className} - {format(new Date(s.sessionDate), 'MMM dd, yyyy')} - {s.location || 'No location'}
                        </option>
                    ))}
                </select>

                {session && (
                    <div className="mt-4 p-5 bg-gradient-to-r from-indigo-50 to-purple-50 rounded-xl border-l-4 border-indigo-500">
                        <h3 className="font-bold text-indigo-900 text-lg mb-3">Session Details</h3>
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-sm text-indigo-800">
                            <div className="flex items-center">
                                <FaChalkboardTeacher className="mr-2 text-indigo-600" />
                                <span><span className="font-semibold">Class:</span> {session.class?.className}</span>
                            </div>
                            <div className="flex items-center">
                                <FaCalendarCheck className="mr-2 text-indigo-600" />
                                <span><span className="font-semibold">Date:</span> {format(new Date(session.sessionDate), 'MMM dd, yyyy')}</span>
                            </div>
                            <div className="flex items-center">
                                <svg className="w-4 h-4 mr-2 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                                </svg>
                                <span><span className="font-semibold">Location:</span> {session.location || 'N/A'}</span>
                            </div>
                        </div>
                    </div>
                )}
            </div>

            {selectedSessionId && (
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
                                <span className="text-lg">AI Recognition</span>
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
                                AI Face Recognition
                            </h2>
                            <div className="flex flex-col items-center space-y-6">
                                <div className="w-full max-w-3xl aspect-video bg-gradient-to-br from-gray-900 to-gray-800 rounded-2xl overflow-hidden shadow-2xl border-4 border-indigo-200">
                                    <Webcam
                                        ref={webcamRef}
                                        screenshotFormat="image/jpeg"
                                        className="w-full h-full object-cover"
                                        videoConstraints={{
                                            facingMode: 'user',
                                        }}
                                    />
                                </div>
                                <button
                                    onClick={captureAndRecognize}
                                    disabled={capturing}
                                    className="btn-primary px-10 py-4 text-lg font-bold shadow-xl"
                                >
                                    {capturing ? (
                                        <>
                                            <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                            </svg>
                                            Processing...
                                        </>
                                    ) : (
                                        <>
                                            <FaCamera className="inline mr-2" />
                                            Capture & Recognize
                                        </>
                                    )}
                                </button>
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

                    {/* Attendance Records (Real-time) */}
                    <div className="card bg-white rounded-2xl">
                        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4 mb-6">
                            <h2 className="text-2xl font-bold text-gray-900">
                                Attendance Records ({attendanceRecords.length})
                            </h2>
                            <div className="flex items-center space-x-3 text-sm bg-green-50 px-4 py-2 rounded-full border border-green-200">
                                <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
                                <span className="text-green-700 font-semibold">Real-time updates</span>
                            </div>
                        </div>

                        {attendanceRecords.length === 0 ? (
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
                                                Email
                                            </th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Status
                                            </th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Confidence
                                            </th>
                                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                                Marked At
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
                                            <tr key={record.recordId} className="hover:bg-gray-50">
                                                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                                    {record.student?.studentNumber}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.student?.firstName} {record.student?.lastName}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.student?.email}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap">
                                                    {getStatusBadge(record.status)}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.confidenceScore ? `${(record.confidenceScore * 100).toFixed(1)}%` : 'N/A'}
                                                </td>
                                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                                    {record.markedAt && format(new Date(record.markedAt), 'hh:mm:ss a')}
                                                </td>
                                                {mode === 'manual' && (
                                                    <td className="px-6 py-4 whitespace-nowrap text-sm space-x-2">
                                                        <button
                                                            onClick={() => handleManualMark(record.student?.studentId, 'Present')}
                                                            className="text-green-600 hover:text-green-700 font-medium"
                                                        >
                                                            Present
                                                        </button>
                                                        <button
                                                            onClick={() => handleManualMark(record.student?.studentId, 'Absent')}
                                                            className="text-red-600 hover:text-red-700 font-medium"
                                                        >
                                                            Absent
                                                        </button>
                                                        <button
                                                            onClick={() => handleManualMark(record.student?.studentId, 'Late')}
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
