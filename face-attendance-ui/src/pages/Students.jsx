import React, { useState, useEffect, useRef } from 'react';
import Webcam from 'react-webcam';
import { studentsAPI } from '../services/api';
import { FaPlus, FaEdit, FaTrash, FaSearch, FaUser, FaCamera, FaUpload } from 'react-icons/fa';
import { toast } from 'react-toastify';

const Students = () => {
    const [students, setStudents] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [showModal, setShowModal] = useState(false);
    const [editingStudent, setEditingStudent] = useState(null);
    const [formData, setFormData] = useState({
        studentNumber: '',
        firstName: '',
        lastName: '',
        email: '',
        photo: null,
    });
    const [photoPreview, setPhotoPreview] = useState(null);
    const [captureMode, setCaptureMode] = useState('upload'); // 'upload' or 'webcam'
    const [showWebcam, setShowWebcam] = useState(false);
    const webcamRef = useRef(null);

    useEffect(() => {
        fetchStudents();
    }, []);

    const fetchStudents = async () => {
        try {
            const response = await studentsAPI.getAll();
            if (response.data.success) {
                setStudents(response.data.data);
            }
            setLoading(false);
        } catch (error) {
            console.error('Error fetching students:', error);
            toast.error('Failed to load students');
            setLoading(false);
        }
    };

    const handleInputChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handlePhotoChange = (e) => {
        const file = e.target.files[0];
        if (file) {
            setFormData((prev) => ({ ...prev, photo: file }));
            const reader = new FileReader();
            reader.onloadend = () => {
                setPhotoPreview(reader.result);
            };
            reader.readAsDataURL(file);
        }
    };

    const resetForm = () => {
        setFormData({
            studentNumber: '',
            firstName: '',
            lastName: '',
            email: '',
            photo: null,
        });
        setPhotoPreview(null);
        setEditingStudent(null);
        setShowModal(false);
        setCaptureMode('upload');
        setShowWebcam(false);
    };

    const capturePhoto = () => {
        const imageSrc = webcamRef.current.getScreenshot();
        if (imageSrc) {
            setPhotoPreview(imageSrc);
            // Convert base64 to blob for form submission
            fetch(imageSrc)
                .then(res => res.blob())
                .then(blob => {
                    const file = new File([blob], 'webcam-capture.jpg', { type: 'image/jpeg' });
                    setFormData(prev => ({ ...prev, photo: file }));
                });
            setShowWebcam(false);
            toast.success('Photo captured successfully!');
        }
    };

    const toggleCaptureMode = (mode) => {
        setCaptureMode(mode);
        if (mode === 'webcam') {
            setShowWebcam(true);
        } else {
            setShowWebcam(false);
        }
        setPhotoPreview(null);
        setFormData(prev => ({ ...prev, photo: null }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!formData.studentNumber || !formData.firstName || !formData.lastName || !formData.email) {
            toast.error('Please fill in all required fields');
            return;
        }

        if (!editingStudent && !formData.photo) {
            toast.error('Please upload a photo');
            return;
        }

        try {
            const submitData = new FormData();
            submitData.append('studentNumber', formData.studentNumber);
            submitData.append('firstName', formData.firstName);
            submitData.append('lastName', formData.lastName);
            submitData.append('email', formData.email);
            if (formData.photo) {
                submitData.append('photo', formData.photo);
            }

            if (editingStudent) {
                const response = await studentsAPI.update(editingStudent.studentId, {
                    studentNumber: formData.studentNumber,
                    firstName: formData.firstName,
                    lastName: formData.lastName,
                    email: formData.email,
                });

                if (formData.photo) {
                    const photoFormData = new FormData();
                    photoFormData.append('photo', formData.photo);
                    await studentsAPI.updatePhoto(editingStudent.studentId, photoFormData);
                }

                if (response.data.success) {
                    toast.success('Student updated successfully');
                }
            } else {
                const response = await studentsAPI.create(submitData);
                if (response.data.success) {
                    toast.success('Student created successfully');
                }
            }

            fetchStudents();
            resetForm();
        } catch (error) {
            console.error('Error saving student:', error);
            toast.error(error.response?.data?.message || 'Failed to save student');
        }
    };

    const handleEdit = (student) => {
        setEditingStudent(student);
        setFormData({
            studentNumber: student.studentNumber,
            firstName: student.firstName,
            lastName: student.lastName,
            email: student.email,
            photo: null,
        });
        setPhotoPreview(student.profilePhotoUrl);
        setShowModal(true);
    };

    const handleDelete = async (studentId) => {
        if (!window.confirm('Are you sure you want to delete this student?')) {
            return;
        }

        try {
            const response = await studentsAPI.delete(studentId);
            if (response.data.success) {
                toast.success('Student deleted successfully');
                fetchStudents();
            }
        } catch (error) {
            console.error('Error deleting student:', error);
            toast.error('Failed to delete student');
        }
    };

    const filteredStudents = students.filter((student) => {
        const searchLower = searchTerm.toLowerCase();
        return (
            student.studentNumber?.toLowerCase().includes(searchLower) ||
            student.firstName?.toLowerCase().includes(searchLower) ||
            student.lastName?.toLowerCase().includes(searchLower) ||
            student.email?.toLowerCase().includes(searchLower)
        );
    });

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
            <div className="bg-gradient-to-r from-blue-600 to-indigo-700 rounded-2xl shadow-xl p-6 sm:p-8 text-white">
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
                    <div>
                        <h1 className="text-3xl sm:text-4xl font-bold mb-2">Students</h1>
                        <p className="text-lg text-blue-100">Manage student information and photos</p>
                    </div>
                    <button
                        onClick={() => setShowModal(true)}
                        className="btn-primary bg-white text-blue-700 hover:bg-blue-50 flex items-center justify-center whitespace-nowrap"
                    >
                        <FaPlus className="mr-2" />
                        Add Student
                    </button>
                </div>
            </div>

            {/* Search Bar */}
            <div className="card bg-white rounded-xl">
                <div className="relative">
                    <FaSearch className="absolute left-4 top-1/2 transform -translate-y-1/2 text-gray-400 text-lg" />
                    <input
                        type="text"
                        placeholder="Search by student number, name, or email..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="input-field pl-12 py-4 text-base"
                    />
                </div>
            </div>

            {/* Students Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                {filteredStudents.map((student) => (
                    <div key={student.studentId} className="card bg-white rounded-xl hover:shadow-2xl transition-all duration-300 hover:-translate-y-1">
                        <div className="flex flex-col items-center p-4">
                            <div className="relative mb-4">
                                {student.profilePhotoUrl ? (
                                    <img
                                        src={student.profilePhotoUrl}
                                        alt={`${student.firstName} ${student.lastName}`}
                                        className="w-28 h-28 rounded-full object-cover border-4 border-blue-100 shadow-lg"
                                    />
                                ) : (
                                    <div className="w-28 h-28 rounded-full bg-gradient-to-br from-blue-100 to-indigo-200 flex items-center justify-center border-4 border-blue-100 shadow-lg">
                                        <FaUser className="text-5xl text-blue-400" />
                                    </div>
                                )}
                                <div className="absolute bottom-0 right-0 w-8 h-8 bg-green-500 rounded-full border-4 border-white"></div>
                            </div>
                            <h3 className="text-lg font-bold text-gray-900 text-center mb-1">
                                {student.firstName} {student.lastName}
                            </h3>
                            <p className="text-sm font-semibold text-blue-600 mb-1">{student.studentNumber}</p>
                            <p className="text-xs text-gray-500 mb-4 text-center truncate w-full px-2">{student.email}</p>
                            <div className="flex gap-2 w-full">
                                <button
                                    onClick={() => handleEdit(student)}
                                    className="btn-secondary flex items-center justify-center text-sm flex-1 py-2"
                                >
                                    <FaEdit className="mr-1" />
                                    Edit
                                </button>
                                <button
                                    onClick={() => handleDelete(student.studentId)}
                                    className="btn-danger flex items-center justify-center text-sm flex-1 py-2"
                                >
                                    <FaTrash className="mr-1" />
                                    Delete
                                </button>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            {filteredStudents.length === 0 && (
                <div className="text-center py-16 sm:py-20">
                    <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-gradient-to-br from-blue-100 to-indigo-200 mb-6">
                        <FaUser className="text-5xl text-blue-400" />
                    </div>
                    <p className="text-gray-500 text-xl font-medium">No students found</p>
                    <p className="text-gray-400 text-sm mt-2">Try adjusting your search or add a new student</p>
                </div>
            )}

            {/* Modal */}
            {showModal && (
                <div className="fixed inset-0 bg-black bg-opacity-60 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-fade-in">
                    <div className="bg-white rounded-2xl max-w-md w-full p-8 max-h-[90vh] overflow-y-auto shadow-2xl">
                        <h2 className="text-3xl font-bold text-gray-900 mb-6">
                            {editingStudent ? 'Edit Student' : 'Add New Student'}
                        </h2>
                        <form onSubmit={handleSubmit} className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Student Number *
                                </label>
                                <input
                                    type="text"
                                    name="studentNumber"
                                    value={formData.studentNumber}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    First Name *
                                </label>
                                <input
                                    type="text"
                                    name="firstName"
                                    value={formData.firstName}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Last Name *
                                </label>
                                <input
                                    type="text"
                                    name="lastName"
                                    value={formData.lastName}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Email *
                                </label>
                                <input
                                    type="email"
                                    name="email"
                                    value={formData.email}
                                    onChange={handleInputChange}
                                    className="input-field"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Photo {!editingStudent && '*'}
                                </label>

                                {/* Capture Mode Toggle */}
                                <div className="flex gap-2 mb-3">
                                    <button
                                        type="button"
                                        onClick={() => toggleCaptureMode('upload')}
                                        className={`flex-1 flex items-center justify-center gap-2 py-2 px-4 rounded-lg font-medium transition-all ${captureMode === 'upload'
                                                ? 'bg-blue-600 text-white shadow-md'
                                                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                                            }`}
                                    >
                                        <FaUpload />
                                        Upload
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => toggleCaptureMode('webcam')}
                                        className={`flex-1 flex items-center justify-center gap-2 py-2 px-4 rounded-lg font-medium transition-all ${captureMode === 'webcam'
                                                ? 'bg-blue-600 text-white shadow-md'
                                                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                                            }`}
                                    >
                                        <FaCamera />
                                        Webcam
                                    </button>
                                </div>

                                {/* File Upload Mode */}
                                {captureMode === 'upload' && (
                                    <input
                                        type="file"
                                        accept="image/*"
                                        onChange={handlePhotoChange}
                                        className="input-field"
                                    />
                                )}

                                {/* Webcam Capture Mode */}
                                {captureMode === 'webcam' && showWebcam && (
                                    <div className="space-y-3">
                                        <div className="relative bg-black rounded-lg overflow-hidden">
                                            <Webcam
                                                audio={false}
                                                ref={webcamRef}
                                                screenshotFormat="image/jpeg"
                                                className="w-full"
                                                videoConstraints={{
                                                    width: 640,
                                                    height: 480,
                                                    facingMode: "user"
                                                }}
                                            />
                                        </div>
                                        <button
                                            type="button"
                                            onClick={capturePhoto}
                                            className="w-full btn-primary flex items-center justify-center gap-2"
                                        >
                                            <FaCamera />
                                            Capture Photo
                                        </button>
                                    </div>
                                )}

                                {/* Photo Preview */}
                                {photoPreview && !showWebcam && (
                                    <div className="mt-3">
                                        <img
                                            src={photoPreview}
                                            alt="Preview"
                                            className="w-32 h-32 rounded-full object-cover mx-auto border-4 border-blue-100"
                                        />
                                        <button
                                            type="button"
                                            onClick={() => {
                                                setPhotoPreview(null);
                                                setFormData(prev => ({ ...prev, photo: null }));
                                                if (captureMode === 'webcam') {
                                                    setShowWebcam(true);
                                                }
                                            }}
                                            className="mt-2 text-sm text-red-600 hover:text-red-700 font-medium w-full text-center"
                                        >
                                            Remove Photo
                                        </button>
                                    </div>
                                )}
                            </div>

                            <div className="flex space-x-3 pt-4">
                                <button type="submit" className="btn-primary flex-1">
                                    {editingStudent ? 'Update' : 'Create'}
                                </button>
                                <button
                                    type="button"
                                    onClick={resetForm}
                                    className="btn-secondary flex-1"
                                >
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

export default Students;
