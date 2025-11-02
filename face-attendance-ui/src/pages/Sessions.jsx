import React, { useState, useEffect } from 'react';
import { sessionsAPI, classesAPI } from '../services/api';
import { FaPlus, FaEdit, FaStop, FaEye, FaCalendarAlt } from 'react-icons/fa';
import { format } from 'date-fns';
import { toast } from 'react-toastify';
import { useNavigate } from 'react-router-dom';

const Sessions = () => {
    const navigate = useNavigate();
    const [sessions, setSessions] = useState([]);
    const [classes, setClasses] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [formData, setFormData] = useState({
        classId: '',
        sessionDate: format(new Date(), 'yyyy-MM-dd'),
        sessionStartTime: format(new Date(), 'HH:mm'),
        sessionEndTime: '',
        location: '',
        notes: '',
    });

    useEffect(() => {
        fetchSessions();
        fetchClasses();
    }, []);

    const fetchSessions = async () => {
        try {
            const response = await sessionsAPI.getAll();
            if (response.data.success) {
                setSessions(response.data.data);
            }
            setLoading(false);
        } catch (error) {
            console.error('Error fetching sessions:', error);
            toast.error('Failed to load sessions');
            setLoading(false);
        }
    };

    const fetchClasses = async () => {
        try {
            const response = await classesAPI.getAll();
            if (response.data.success) {
                setClasses(response.data.data);
            }
        } catch (error) {
            console.error('Error fetching classes:', error);
        }
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const resetForm = () => {
        setFormData({
            classId: '',
            sessionDate: format(new Date(), 'yyyy-MM-dd'),
            sessionStartTime: format(new Date(), 'HH:mm'),
            sessionEndTime: '',
            location: '',
            notes: '',
        });
        setShowModal(false);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!formData.classId || !formData.sessionDate) {
            toast.error('Please fill in all required fields');
            return;
        }

        try {
            const submitData = {
                classId: formData.classId,
                sessionDate: formData.sessionDate,
                sessionStartTime: formData.sessionStartTime ? `${formData.sessionDate}T${formData.sessionStartTime}:00` : null,
                sessionEndTime: formData.sessionEndTime ? `${formData.sessionDate}T${formData.sessionEndTime}:00` : null,
                location: formData.location,
                notes: formData.notes,
            };

            const response = await sessionsAPI.create(submitData);
            if (response.data.success) {
                toast.success('Session created successfully');
                fetchSessions();
                resetForm();
            }
        } catch (error) {
            console.error('Error creating session:', error);
            toast.error(error.response?.data?.message || 'Failed to create session');
        }
    };

    const handleEndSession = async (sessionId) => {
        if (!window.confirm('Are you sure you want to end this session?')) {
            return;
        }

        try {
            const response = await sessionsAPI.end(sessionId);
            if (response.data.success) {
                toast.success('Session ended successfully');
                fetchSessions();
            }
        } catch (error) {
            console.error('Error ending session:', error);
            toast.error('Failed to end session');
        }
    };

    const handleViewAttendance = (sessionId) => {
        navigate(`/take-attendance?sessionId=${sessionId}`);
    };

    const getStatusBadge = (status) => {
        const statusStyles = {
            InProgress: 'bg-green-100 text-green-800',
            Completed: 'bg-gray-100 text-gray-800',
            Cancelled: 'bg-red-100 text-red-800',
        };

        return (
            <span className={`px-3 py-1 text-xs font-semibold rounded-full ${statusStyles[status] || 'bg-gray-100 text-gray-800'}`}>
                {status}
            </span>
        );
    };

    if (loading) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header Section */}
            <div className="bg-gradient-to-r from-purple-600 to-indigo-700 rounded-2xl shadow-xl p-6 sm:p-8 text-white">
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
                    <div>
                        <h1 className="text-3xl sm:text-4xl font-bold mb-2">Attendance Sessions</h1>
                        <p className="text-lg text-purple-100">Create and manage attendance sessions</p>
                    </div>
                    <button
                        onClick={() => setShowModal(true)}
                        className="btn-primary bg-white text-purple-700 hover:bg-purple-50 flex items-center justify-center whitespace-nowrap"
                    >
                        <FaPlus className="mr-2" />
                        Create Session
                    </button>
                </div>
            </div>

            {/* Sessions Table */}
            <div className="card bg-white rounded-2xl">
                {sessions.length === 0 ? (
                    <div className="text-center py-16 sm:py-20">
                        <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-gradient-to-br from-purple-100 to-indigo-200 mb-6">
                            <FaCalendarAlt className="text-5xl text-purple-400" />
                        </div>
                        <p className="text-gray-500 text-xl font-medium">No sessions found</p>
                        <p className="text-gray-400 text-sm mt-2">Create your first session to start tracking attendance</p>
                    </div>
                ) : (
                    <div className="overflow-x-auto -mx-6 sm:mx-0">
                        <div className="inline-block min-w-full align-middle">
                            <table className="min-w-full divide-y divide-gray-200">
                                <thead className="bg-gradient-to-r from-purple-50 to-indigo-50">
                                    <tr>
                                        <th className="px-4 sm:px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                                            Class
                                        </th>
                                        <th className="px-4 sm:px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                                            Date
                                        </th>
                                        <th className="px-4 sm:px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider hidden md:table-cell">
                                            Start Time
                                        </th>
                                        <th className="px-4 sm:px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                                            End Time
                                        </th>
                                        <th className="px-4 sm:px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider hidden lg:table-cell">
                                            Location
                                        </th>
                                        <th className="px-4 sm:px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                                            Status
                                        </th>
                                        <th className="px-4 sm:px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                                            Actions
                                        </th>
                                    </tr>
                                </thead>
                                <tbody className="bg-white divide-y divide-gray-200">
                                    {sessions.map((session) => (
                                        <tr key={session.sessionId} className="hover:bg-purple-50/50 transition-colors">
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap">
                                                <div className="text-sm font-semibold text-gray-900">
                                                    {session.class?.className || 'N/A'}
                                                </div>
                                                <div className="text-xs text-gray-500">
                                                    {session.class?.classCode || ''}
                                                </div>
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap">
                                                <div className="text-sm font-medium text-gray-900">
                                                    {format(new Date(session.sessionDate), 'MMM dd, yyyy')}
                                                </div>
                                                <div className="text-xs text-gray-500 md:hidden">
                                                    {session.sessionStartTime && format(new Date(session.sessionStartTime), 'hh:mm a')}
                                                </div>
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm text-gray-600 hidden md:table-cell">
                                                {session.sessionStartTime && format(new Date(session.sessionStartTime), 'hh:mm a')}
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm text-gray-600 hidden lg:table-cell">
                                                {session.sessionEndTime ? format(new Date(session.sessionEndTime), 'hh:mm a') : '-'}
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm text-gray-600 hidden lg:table-cell">
                                                {session.location || '-'}
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap">
                                                {getStatusBadge(session.status)}
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm">
                                                <div className="flex flex-col sm:flex-row gap-2">
                                                    <button
                                                        onClick={() => handleViewAttendance(session.sessionId)}
                                                        className="text-blue-600 hover:text-blue-700 font-semibold inline-flex items-center justify-center"
                                                    >
                                                        <FaEye className="mr-1" />
                                                        View
                                                    </button>
                                                    {session.status === 'InProgress' && (
                                                        <button
                                                            onClick={() => handleEndSession(session.sessionId)}
                                                            className="text-red-600 hover:text-red-700 font-semibold inline-flex items-center justify-center"
                                                        >
                                                            <FaStop className="mr-1" />
                                                            End
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
            </div>

            {/* Create Session Modal */}
            {showModal && (
                <div className="fixed inset-0 bg-black bg-opacity-60 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-fade-in">
                    <div className="bg-white rounded-2xl max-w-md w-full p-8 shadow-2xl max-h-[90vh] overflow-y-auto">
                        <h2 className="text-3xl font-bold text-gray-900 mb-6">Create New Session</h2>
                        <form onSubmit={handleSubmit} className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Class *
                                </label>
                                <select
                                    name="classId"
                                    value={formData.classId}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                >
                                    <option value="">Select a class</option>
                                    {classes.map((classItem) => (
                                        <option key={classItem.classId} value={classItem.classId}>
                                            {classItem.className} ({classItem.classCode})
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Date *
                                </label>
                                <input
                                    type="date"
                                    name="sessionDate"
                                    value={formData.sessionDate}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Start Time
                                </label>
                                <input
                                    type="time"
                                    name="sessionStartTime"
                                    value={formData.sessionStartTime}
                                    onChange={handleInputChange}
                                    className="input-field"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    End Time
                                </label>
                                <input
                                    type="time"
                                    name="sessionEndTime"
                                    value={formData.sessionEndTime}
                                    onChange={handleInputChange}
                                    className="input-field"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Location</label>
                                <input
                                    type="text"
                                    name="location"
                                    value={formData.location}
                                    onChange={handleInputChange}
                                    className="input-field"
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
                                <textarea
                                    name="notes"
                                    value={formData.notes}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    rows="3"
                                />
                            </div>

                            <div className="flex space-x-3 pt-4">
                                <button type="submit" className="btn-primary flex-1">
                                    Create
                                </button>
                                <button type="button" onClick={resetForm} className="btn-secondary flex-1">
                                    Cancel
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
};

export default Sessions;
