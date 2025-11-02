import React, { useState, useEffect } from 'react';
import { dashboardAPI, sessionsAPI } from '../services/api';
import { FaUserGraduate, FaChalkboardTeacher, FaCalendarCheck, FaClock } from 'react-icons/fa';
import { format } from 'date-fns';
import { toast } from 'react-toastify';

const Dashboard = () => {
    const [stats, setStats] = useState({
        totalStudents: 0,
        totalClasses: 0,
        activeSessions: 0,
        todayAttendance: 0,
    });
    const [recentSessions, setRecentSessions] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchDashboardData();
        // Refresh data every 30 seconds for real-time updates
        const interval = setInterval(fetchDashboardData, 30000);
        return () => clearInterval(interval);
    }, []);

    const fetchDashboardData = async () => {
        try {
            const [statsRes, sessionsRes] = await Promise.all([
                dashboardAPI.getStats(),
                sessionsAPI.getAll(),
            ]);

            if (statsRes.data.success) {
                setStats(statsRes.data.data);
            }

            if (sessionsRes.data.success) {
                // Get recent 5 sessions
                const recent = sessionsRes.data.data
                    .sort((a, b) => new Date(b.sessionDate) - new Date(a.sessionDate))
                    .slice(0, 5);
                setRecentSessions(recent);
            }

            setLoading(false);
        } catch (error) {
            console.error('Error fetching dashboard data:', error);
            toast.error('Failed to load dashboard data');
            setLoading(false);
        }
    };

    const StatCard = ({ icon: Icon, title, value, color, bgColor, gradient }) => (
        <div className={`relative overflow-hidden rounded-xl shadow-lg hover:shadow-2xl transition-all duration-300 hover:-translate-y-1 ${gradient}`}>
            <div className="absolute top-0 right-0 w-32 h-32 -mr-8 -mt-8 opacity-10">
                <Icon className="text-white text-9xl" />
            </div>
            <div className="relative p-6">
                <div className="flex items-center justify-between mb-4">
                    <div className={`p-3 rounded-xl ${bgColor} shadow-md`}>
                        <Icon className={`text-3xl ${color}`} />
                    </div>
                </div>
                <div>
                    <p className="text-sm font-semibold text-white/90 mb-1">{title}</p>
                    <p className="text-4xl font-bold text-white">{value}</p>
                </div>
            </div>
        </div>
    );

    const getStatusBadge = (status) => {
        const statusStyles = {
            InProgress: 'bg-green-100 text-green-800',
            Completed: 'bg-gray-100 text-gray-800',
            Cancelled: 'bg-red-100 text-red-800',
        };

        return (
            <span className={`px-2 py-1 text-xs font-semibold rounded-full ${statusStyles[status] || 'bg-gray-100 text-gray-800'}`}>
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
        <div className="space-y-8">
            {/* Header Section */}
            <div className="bg-gradient-to-r from-blue-600 to-indigo-700 rounded-2xl shadow-xl p-6 sm:p-8 text-white">
                <h1 className="text-3xl sm:text-4xl font-bold mb-2">Dashboard</h1>
                <p className="text-lg text-blue-100">Welcome to Face Attendance System</p>
                <p className="text-sm text-blue-200 mt-2">
                    {format(new Date(), 'EEEE, MMMM dd, yyyy')}
                </p>
            </div>

            {/* Statistics Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
                <StatCard
                    icon={FaUserGraduate}
                    title="Total Students"
                    value={stats.totalStudents}
                    color="text-blue-700"
                    bgColor="bg-white/20"
                    gradient="bg-gradient-to-br from-blue-500 to-blue-700"
                />
                <StatCard
                    icon={FaChalkboardTeacher}
                    title="Total Classes"
                    value={stats.totalClasses}
                    color="text-green-700"
                    bgColor="bg-white/20"
                    gradient="bg-gradient-to-br from-green-500 to-emerald-700"
                />
                <StatCard
                    icon={FaClock}
                    title="Active Sessions"
                    value={stats.activeSessions}
                    color="text-amber-700"
                    bgColor="bg-white/20"
                    gradient="bg-gradient-to-br from-amber-500 to-orange-600"
                />
                <StatCard
                    icon={FaCalendarCheck}
                    title="Today's Attendance"
                    value={stats.todayAttendance}
                    color="text-purple-700"
                    bgColor="bg-white/20"
                    gradient="bg-gradient-to-br from-purple-500 to-indigo-700"
                />
            </div>

            {/* Recent Sessions */}
            <div className="card bg-white rounded-2xl">
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4 mb-6">
                    <h2 className="text-2xl font-bold text-gray-900">Recent Sessions</h2>
                    <button
                        onClick={fetchDashboardData}
                        className="btn-secondary flex items-center justify-center w-full sm:w-auto"
                    >
                        <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                        </svg>
                        Refresh
                    </button>
                </div>

                {recentSessions.length === 0 ? (
                    <div className="text-center py-12 sm:py-16">
                        <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-gray-100 mb-4">
                            <FaClock className="text-3xl text-gray-400" />
                        </div>
                        <p className="text-gray-500 text-lg">No sessions found</p>
                    </div>
                ) : (
                    <div className="overflow-x-auto -mx-6 sm:mx-0">
                        <div className="inline-block min-w-full align-middle">
                            <table className="min-w-full divide-y divide-gray-200">
                                <thead className="bg-gradient-to-r from-gray-50 to-gray-100">
                                    <tr>
                                        <th className="px-4 sm:px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                                            Class
                                        </th>
                                        <th className="px-4 sm:px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                                            Date
                                        </th>
                                        <th className="px-4 sm:px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider hidden sm:table-cell">
                                            Time
                                        </th>
                                        <th className="px-4 sm:px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider hidden md:table-cell">
                                            Location
                                        </th>
                                        <th className="px-4 sm:px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                                            Status
                                        </th>
                                    </tr>
                                </thead>
                                <tbody className="bg-white divide-y divide-gray-200">
                                    {recentSessions.map((session) => (
                                        <tr key={session.sessionId} className="hover:bg-blue-50/50 transition-colors">
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap">
                                                <div className="text-sm font-semibold text-gray-900">
                                                    {session.class?.className || 'N/A'}
                                                </div>
                                                <div className="text-xs text-gray-500 sm:hidden">
                                                    {session.sessionStartTime && format(new Date(session.sessionStartTime), 'hh:mm a')}
                                                </div>
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                                                {format(new Date(session.sessionDate), 'MMM dd, yyyy')}
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm text-gray-600 hidden sm:table-cell">
                                                {session.sessionStartTime && format(new Date(session.sessionStartTime), 'hh:mm a')}
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm text-gray-600 hidden md:table-cell">
                                                {session.location || 'N/A'}
                                            </td>
                                            <td className="px-4 sm:px-6 py-4 whitespace-nowrap text-sm">
                                                {getStatusBadge(session.status)}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default Dashboard;
