import React, { useState, useEffect } from 'react';
import { classesAPI, studentsAPI } from '../services/api';
import { FaPlus, FaEdit, FaTrash, FaUserPlus, FaUsers } from 'react-icons/fa';
import { toast } from 'react-toastify';

const Classes = () => {
    const [classes, setClasses] = useState([]);
    const [students, setStudents] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [showEnrollModal, setShowEnrollModal] = useState(false);
    const [editingClass, setEditingClass] = useState(null);
    const [selectedClass, setSelectedClass] = useState(null);
    const [selectedStudents, setSelectedStudents] = useState([]);
    const [enrolledStudents, setEnrolledStudents] = useState([]);
    const [formData, setFormData] = useState({
        className: '',
        classCode: '',
        description: '',
        location: '',
    });

    useEffect(() => {
        fetchClasses();
        fetchStudents();
    }, []);

    const fetchClasses = async () => {
        try {
            const response = await classesAPI.getAll();
            if (response.data.success) {
                setClasses(response.data.data);
            }
            setLoading(false);
        } catch (error) {
            console.error('Error fetching classes:', error);
            toast.error('Failed to load classes');
            setLoading(false);
        }
    };

    const fetchStudents = async () => {
        try {
            const response = await studentsAPI.getAll();
            if (response.data.success) {
                setStudents(response.data.data);
            }
        } catch (error) {
            console.error('Error fetching students:', error);
        }
    };

    const fetchEnrollments = async (classId) => {
        try {
            const response = await classesAPI.getEnrollments(classId);
            if (response.data.success) {
                setEnrolledStudents(response.data.data);
            }
        } catch (error) {
            console.error('Error fetching enrollments:', error);
        }
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const resetForm = () => {
        setFormData({
            className: '',
            classCode: '',
            description: '',
            location: '',
        });
        setEditingClass(null);
        setShowModal(false);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!formData.className || !formData.classCode) {
            toast.error('Please fill in all required fields');
            return;
        }

        try {
            if (editingClass) {
                const response = await classesAPI.update(editingClass.classId, formData);
                if (response.data.success) {
                    toast.success('Class updated successfully');
                }
            } else {
                const response = await classesAPI.create(formData);
                if (response.data.success) {
                    toast.success('Class created successfully');
                }
            }

            fetchClasses();
            resetForm();
        } catch (error) {
            console.error('Error saving class:', error);
            toast.error(error.response?.data?.message || 'Failed to save class');
        }
    };

    const handleEdit = (classItem) => {
        setEditingClass(classItem);
        setFormData({
            className: classItem.className,
            classCode: classItem.classCode,
            description: classItem.description || '',
            location: classItem.location || '',
        });
        setShowModal(true);
    };

    const handleDelete = async (classId) => {
        if (!window.confirm('Are you sure you want to delete this class?')) {
            return;
        }

        try {
            const response = await classesAPI.delete(classId);
            if (response.data.success) {
                toast.success('Class deleted successfully');
                fetchClasses();
            }
        } catch (error) {
            console.error('Error deleting class:', error);
            toast.error('Failed to delete class');
        }
    };

    const handleEnrollClick = async (classItem) => {
        setSelectedClass(classItem);
        await fetchEnrollments(classItem.classId);
        setSelectedStudents([]);
        setShowEnrollModal(true);
    };

    const handleStudentSelect = (studentId) => {
        setSelectedStudents((prev) =>
            prev.includes(studentId)
                ? prev.filter((id) => id !== studentId)
                : [...prev, studentId]
        );
    };

    const handleEnrollStudents = async () => {
        if (selectedStudents.length === 0) {
            toast.error('Please select at least one student');
            return;
        }

        try {
            const response = await classesAPI.enrollStudents(selectedClass.classId, selectedStudents);
            if (response.data.success) {
                toast.success('Students enrolled successfully');
                await fetchEnrollments(selectedClass.classId);
                setSelectedStudents([]);
            }
        } catch (error) {
            console.error('Error enrolling students:', error);
            toast.error(error.response?.data?.message || 'Failed to enroll students');
        }
    };

    const handleUnenroll = async (studentId) => {
        if (!window.confirm('Are you sure you want to unenroll this student?')) {
            return;
        }

        try {
            const response = await classesAPI.unenrollStudent(selectedClass.classId, studentId);
            if (response.data.success) {
                toast.success('Student unenrolled successfully');
                await fetchEnrollments(selectedClass.classId);
            }
        } catch (error) {
            console.error('Error unenrolling student:', error);
            toast.error('Failed to unenroll student');
        }
    };

    const availableStudents = students.filter(
        (student) => !enrolledStudents.some((enrolled) => enrolled.studentId === student.studentId)
    );

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
            <div className="bg-gradient-to-r from-green-600 to-emerald-700 rounded-2xl shadow-xl p-6 sm:p-8 text-white">
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
                    <div>
                        <h1 className="text-3xl sm:text-4xl font-bold mb-2">Classes</h1>
                        <p className="text-lg text-green-100">Manage classes and student enrollments</p>
                    </div>
                    <button
                        onClick={() => setShowModal(true)}
                        className="btn-primary bg-white text-green-700 hover:bg-green-50 flex items-center justify-center whitespace-nowrap"
                    >
                        <FaPlus className="mr-2" />
                        Add Class
                    </button>
                </div>
            </div>

            {/* Classes Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {classes.map((classItem) => (
                    <div key={classItem.classId} className="card bg-white rounded-xl hover:shadow-2xl transition-all duration-300 hover:-translate-y-1 border-l-4 border-green-500">
                        <div className="mb-4">
                            <h3 className="text-xl font-bold text-gray-900 mb-1">{classItem.className}</h3>
                            <p className="text-sm font-semibold text-green-600 bg-green-50 inline-block px-3 py-1 rounded-full">
                                {classItem.classCode}
                            </p>
                        </div>

                        {classItem.description && (
                            <p className="text-sm text-gray-600 mb-3 line-clamp-2 min-h-[2.5rem]">
                                {classItem.description}
                            </p>
                        )}

                        {classItem.location && (
                            <div className="flex items-center text-sm text-gray-500 mb-4 bg-gray-50 p-2 rounded-lg">
                                <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                                </svg>
                                <span className="font-medium">{classItem.location}</span>
                            </div>
                        )}

                        <div className="flex flex-col gap-2 mt-4">
                            <button
                                onClick={() => handleEnrollClick(classItem)}
                                className="btn-primary flex items-center justify-center text-sm w-full"
                            >
                                <FaUserPlus className="mr-2" />
                                Manage Students
                            </button>
                            <div className="flex gap-2">
                                <button
                                    onClick={() => handleEdit(classItem)}
                                    className="btn-secondary flex items-center justify-center text-sm flex-1"
                                >
                                    <FaEdit className="mr-1" />
                                    Edit
                                </button>
                                <button
                                    onClick={() => handleDelete(classItem.classId)}
                                    className="btn-danger flex items-center justify-center text-sm flex-1"
                                >
                                    <FaTrash className="mr-1" />
                                    Delete
                                </button>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            {classes.length === 0 && (
                <div className="text-center py-16 sm:py-20">
                    <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-gradient-to-br from-green-100 to-emerald-200 mb-6">
                        <FaUsers className="text-5xl text-green-400" />
                    </div>
                    <p className="text-gray-500 text-xl font-medium">No classes found</p>
                    <p className="text-gray-400 text-sm mt-2">Create your first class to get started</p>
                </div>
            )}

            {/* Class Form Modal */}
            {showModal && (
                <div className="fixed inset-0 bg-black bg-opacity-60 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-fade-in">
                    <div className="bg-white rounded-2xl max-w-md w-full p-8 shadow-2xl">
                        <h2 className="text-3xl font-bold text-gray-900 mb-6">
                            {editingClass ? 'Edit Class' : 'Add New Class'}
                        </h2>
                        <form onSubmit={handleSubmit} className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Class Name *
                                </label>
                                <input
                                    type="text"
                                    name="className"
                                    value={formData.className}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Class Code *
                                </label>
                                <input
                                    type="text"
                                    name="classCode"
                                    value={formData.classCode}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Description
                                </label>
                                <textarea
                                    name="description"
                                    value={formData.description}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    rows="3"
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

                            <div className="flex space-x-3 pt-4">
                                <button type="submit" className="btn-primary flex-1">
                                    {editingClass ? 'Update' : 'Create'}
                                </button>
                                <button type="button" onClick={resetForm} className="btn-secondary flex-1">
                                    Cancel
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Enrollment Modal */}
            {showEnrollModal && (
                <div className="fixed inset-0 bg-black bg-opacity-60 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-fade-in">
                    <div className="bg-white rounded-2xl max-w-4xl w-full p-8 max-h-[90vh] overflow-y-auto shadow-2xl">
                        <h2 className="text-3xl font-bold text-gray-900 mb-2">
                            Manage Student Enrollments
                        </h2>
                        <p className="text-lg text-gray-600 mb-6">{selectedClass?.className} ({selectedClass?.classCode})</p>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            {/* Available Students */}
                            <div>
                                <h3 className="text-lg font-semibold text-gray-900 mb-3">
                                    Available Students ({availableStudents.length})
                                </h3>
                                <div className="space-y-2 max-h-96 overflow-y-auto">
                                    {availableStudents.map((student) => (
                                        <label
                                            key={student.studentId}
                                            className="flex items-center p-3 bg-gray-50 rounded-lg hover:bg-gray-100 cursor-pointer"
                                        >
                                            <input
                                                type="checkbox"
                                                checked={selectedStudents.includes(student.studentId)}
                                                onChange={() => handleStudentSelect(student.studentId)}
                                                className="mr-3 h-4 w-4 text-blue-600"
                                            />
                                            <div>
                                                <p className="font-medium text-gray-900">
                                                    {student.firstName} {student.lastName}
                                                </p>
                                                <p className="text-sm text-gray-500">{student.studentNumber}</p>
                                            </div>
                                        </label>
                                    ))}
                                    {availableStudents.length === 0 && (
                                        <p className="text-gray-500 text-center py-8">No available students</p>
                                    )}
                                </div>
                                {availableStudents.length > 0 && (
                                    <button
                                        onClick={handleEnrollStudents}
                                        disabled={selectedStudents.length === 0}
                                        className="btn-primary w-full mt-4"
                                    >
                                        Enroll Selected ({selectedStudents.length})
                                    </button>
                                )}
                            </div>

                            {/* Enrolled Students */}
                            <div>
                                <h3 className="text-lg font-semibold text-gray-900 mb-3">
                                    Enrolled Students ({enrolledStudents.length})
                                </h3>
                                <div className="space-y-2 max-h-96 overflow-y-auto">
                                    {enrolledStudents.map((student) => (
                                        <div
                                            key={student.studentId}
                                            className="flex items-center justify-between p-3 bg-green-50 rounded-lg"
                                        >
                                            <div>
                                                <p className="font-medium text-gray-900">
                                                    {student.firstName} {student.lastName}
                                                </p>
                                                <p className="text-sm text-gray-500">{student.studentNumber}</p>
                                            </div>
                                            <button
                                                onClick={() => handleUnenroll(student.studentId)}
                                                className="text-red-600 hover:text-red-700 text-sm font-medium"
                                            >
                                                Remove
                                            </button>
                                        </div>
                                    ))}
                                    {enrolledStudents.length === 0 && (
                                        <p className="text-gray-500 text-center py-8">No enrolled students</p>
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="flex justify-end mt-6">
                            <button
                                onClick={() => setShowEnrollModal(false)}
                                className="btn-secondary"
                            >
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default Classes;
