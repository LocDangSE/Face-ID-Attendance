import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
    FaHome,
    FaUserGraduate,
    FaChalkboardTeacher,
    FaCalendarAlt,
    FaCamera,
    FaBars,
    FaTimes
} from 'react-icons/fa';

const Navbar = () => {
    const location = useLocation();
    const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

    const navItems = [
        { path: '/', label: 'Dashboard', icon: FaHome },
        { path: '/students', label: 'Students', icon: FaUserGraduate },
        { path: '/classes', label: 'Classes', icon: FaChalkboardTeacher },
        { path: '/sessions', label: 'Sessions', icon: FaCalendarAlt },
        { path: '/take-attendance', label: 'Take Attendance', icon: FaCamera },
    ];

    const isActive = (path) => {
        if (path === '/') {
            return location.pathname === '/';
        }
        return location.pathname.startsWith(path);
    };

    const closeMobileMenu = () => {
        setMobileMenuOpen(false);
    };

    return (
        <nav className="bg-gradient-to-r from-blue-600 via-blue-700 to-indigo-700 shadow-xl sticky top-0 z-50">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="flex justify-between items-center h-16">
                    {/* Logo */}
                    <div className="flex-shrink-0 flex items-center">
                        <Link to="/" className="flex items-center space-x-2 group">
                            <div className="bg-white p-2 rounded-lg group-hover:scale-110 transition-transform">
                                <FaCamera className="text-blue-600 text-xl" />
                            </div>
                            <div>
                                <h1 className="text-xl sm:text-2xl font-bold text-white">
                                    Face Attendance
                                </h1>
                                <p className="text-xs text-blue-100 hidden sm:block">Smart Recognition System</p>
                            </div>
                        </Link>
                    </div>

                    {/* Desktop Navigation */}
                    <div className="hidden md:flex md:items-center md:space-x-2">
                        {navItems.map((item) => {
                            const Icon = item.icon;
                            return (
                                <Link
                                    key={item.path}
                                    to={item.path}
                                    className={`inline-flex items-center px-4 py-2 text-sm font-medium rounded-lg transition-all duration-200 ${isActive(item.path)
                                            ? 'bg-white text-blue-700 shadow-lg'
                                            : 'text-white hover:bg-white/20 hover:shadow-md'
                                        }`}
                                >
                                    <Icon className="mr-2 text-base" />
                                    <span>{item.label}</span>
                                </Link>
                            );
                        })}
                    </div>

                    {/* Mobile menu button */}
                    <div className="md:hidden">
                        <button
                            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
                            className="inline-flex items-center justify-center p-2 rounded-lg text-white hover:bg-white/20 transition-colors"
                            aria-label="Toggle menu"
                        >
                            {mobileMenuOpen ? (
                                <FaTimes className="text-2xl" />
                            ) : (
                                <FaBars className="text-2xl" />
                            )}
                        </button>
                    </div>
                </div>
            </div>

            {/* Mobile menu */}
            <div
                className={`md:hidden transition-all duration-300 ease-in-out ${mobileMenuOpen
                        ? 'max-h-screen opacity-100'
                        : 'max-h-0 opacity-0 overflow-hidden'
                    }`}
            >
                <div className="px-4 pt-2 pb-4 space-y-2 bg-gradient-to-b from-blue-700 to-indigo-700">
                    {navItems.map((item) => {
                        const Icon = item.icon;
                        return (
                            <Link
                                key={item.path}
                                to={item.path}
                                onClick={closeMobileMenu}
                                className={`flex items-center px-4 py-3 text-base font-medium rounded-lg transition-all duration-200 ${isActive(item.path)
                                        ? 'bg-white text-blue-700 shadow-lg'
                                        : 'text-white hover:bg-white/20'
                                    }`}
                            >
                                <Icon className="mr-3 text-lg" />
                                <span>{item.label}</span>
                            </Link>
                        );
                    })}
                </div>
            </div>
        </nav>
    );
};

export default Navbar;
