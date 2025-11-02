using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FaceIdBackend.Infrastructure.Services.Interfaces;

/// <summary>
/// Service interface for Flask Face Recognition API integration
/// Replaces Azure Face API with self-hosted Flask/DeepFace solution
/// </summary>
public interface IFlaskFaceRecognitionService
{
    /// <summary>
    /// Register a single student's face in the Flask API database
    /// </summary>
    /// <param name="studentId">Student ID from database</param>
    /// <param name="imageFile">Student's face photo</param>
    /// <returns>Success status and message</returns>
    Task<FlaskApiResponse<RegisterFaceResult>> RegisterFaceAsync(Guid studentId, IFormFile imageFile);

    /// <summary>
    /// Setup or update class face database with multiple students
    /// Used when students enroll in a class
    /// </summary>
    /// <param name="classId">Class ID from database</param>
    /// <param name="studentImages">Dictionary of StudentId -> Image file</param>
    /// <returns>Success status and list of added students</returns>
    Task<FlaskApiResponse<SetupClassResult>> SetupClassDatabaseAsync(
        Guid classId,
        Dictionary<Guid, IFormFile> studentImages);

    /// <summary>
    /// Recognize faces in an image for attendance marking
    /// Main method called during attendance sessions
    /// </summary>
    /// <param name="classId">Class ID to search within</param>
    /// <param name="imageFile">Photo containing student faces</param>
    /// <returns>List of recognized students with confidence scores</returns>
    Task<FlaskApiResponse<RecognizeFaceResult>> RecognizeFaceAsync(Guid classId, IFormFile imageFile);

    /// <summary>
    /// Delete a student's face data from a class database
    /// Called when student is removed from class or deleted
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <param name="studentId">Student ID to remove</param>
    /// <returns>Success status</returns>
    Task<FlaskApiResponse<DeleteStudentResult>> DeleteStudentFromClassAsync(Guid classId, Guid studentId);

    /// <summary>
    /// Delete all face data for a student (from all classes)
    /// Called when student is deleted from system
    /// </summary>
    /// <param name="studentId">Student ID to remove</param>
    /// <returns>Success status</returns>
    Task<FlaskApiResponse<DeleteStudentResult>> DeleteStudentAsync(Guid studentId);

    /// <summary>
    /// Verify if Flask API service is healthy and responding
    /// </summary>
    /// <returns>True if API is accessible</returns>
    Task<bool> HealthCheckAsync();

    /// <summary>
    /// Detect faces in an image without recognition
    /// Useful for validation during registration
    /// </summary>
    /// <param name="imageFile">Image to analyze</param>
    /// <returns>Number of faces detected</returns>
    Task<FlaskApiResponse<DetectFacesResult>> DetectFacesAsync(IFormFile imageFile);
}

/// <summary>
/// Generic response wrapper for Flask API responses
/// </summary>
public class FlaskApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result from face registration
/// </summary>
public class RegisterFaceResult
{
    public string StudentId { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int FacesDetected { get; set; }
}

/// <summary>
/// Result from class database setup
/// </summary>
public class SetupClassResult
{
    public string ClassId { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public List<string> AddedStudents { get; set; } = new();
    public List<string> FailedStudents { get; set; } = new();
}

/// <summary>
/// Result from face recognition
/// </summary>
public class RecognizeFaceResult
{
    public int TotalFacesDetected { get; set; }
    public List<RecognizedStudent> RecognizedStudents { get; set; } = new();
}

/// <summary>
/// Individual recognized student details
/// </summary>
public class RecognizedStudent
{
    public string StudentId { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public decimal Distance { get; set; }
}

/// <summary>
/// Result from student deletion
/// </summary>
public class DeleteStudentResult
{
    public string Message { get; set; } = string.Empty;
    public int DeletedFiles { get; set; }
}

/// <summary>
/// Result from face detection
/// </summary>
public class DetectFacesResult
{
    public int FacesDetected { get; set; }
    public List<FaceRegion> Faces { get; set; } = new();
}

/// <summary>
/// Individual face region details
/// </summary>
public class FaceRegion
{
    public decimal Confidence { get; set; }
    public FaceBoundingBox Region { get; set; } = new();
}

/// <summary>
/// Face bounding box coordinates
/// </summary>
public class FaceBoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
