using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FaceIdBackend.Infrastructure.Configuration;
using FaceIdBackend.Infrastructure.Services.Interfaces;

namespace FaceIdBackend.Infrastructure.Services;

/// <summary>
/// Service implementation for Flask Face Recognition API integration
/// Handles all communication with Python Flask API for face recognition operations
/// </summary>
public class FlaskFaceRecognitionService : IFlaskFaceRecognitionService
{
    private readonly HttpClient _httpClient;
    private readonly FlaskApiSettings _settings;
    private readonly ILogger<FlaskFaceRecognitionService> _logger;

    public FlaskFaceRecognitionService(
        HttpClient httpClient,
        IOptions<FlaskApiSettings> settings,
        ILogger<FlaskFaceRecognitionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure HttpClient base address
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<FlaskApiResponse<RegisterFaceResult>> RegisterFaceAsync(Guid studentId, IFormFile imageFile)
    {
        try
        {
            _logger.LogInformation("Registering face for student {StudentId}", studentId);

            using var content = new MultipartFormDataContent();
            using var imageStream = imageFile.OpenReadStream();
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);

            content.Add(imageContent, "image", imageFile.FileName);
            content.Add(new StringContent(studentId.ToString()), "studentId");

            var response = await _httpClient.PostAsync("/api/face/register", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlaskRegisterResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully registered face for student {StudentId}", studentId);

                return new FlaskApiResponse<RegisterFaceResult>
                {
                    Success = true,
                    Message = result?.Message ?? "Face registered successfully",
                    Data = new RegisterFaceResult
                    {
                        StudentId = studentId.ToString(),
                        ImagePath = result?.ImagePath ?? "",
                        FacesDetected = result?.FacesDetected ?? 1
                    }
                };
            }

            var errorResult = JsonSerializer.Deserialize<FlaskErrorResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Failed to register face for student {StudentId}: {Error}", studentId, errorResult?.Error);

            return new FlaskApiResponse<RegisterFaceResult>
            {
                Success = false,
                Message = errorResult?.Error ?? "Failed to register face",
                Error = errorResult?.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering face for student {StudentId}", studentId);
            return new FlaskApiResponse<RegisterFaceResult>
            {
                Success = false,
                Message = "Internal error during face registration",
                Error = ex.Message
            };
        }
    }

    public async Task<FlaskApiResponse<SetupClassResult>> SetupClassDatabaseAsync(
        Guid classId,
        Dictionary<Guid, IFormFile> studentImages)
    {
        try
        {
            _logger.LogInformation("Setting up class database for class {ClassId} with {StudentCount} students",
                classId, studentImages.Count);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(classId.ToString()), "classId");

            foreach (var (studentId, imageFile) in studentImages)
            {
                var imageStream = imageFile.OpenReadStream();
                var imageContent = new StreamContent(imageStream);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);

                // Flask API expects format: student_{studentId}
                content.Add(imageContent, $"student_{studentId}", imageFile.FileName);
            }

            var response = await _httpClient.PostAsync("/api/face/class/setup", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlaskSetupClassResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully setup class database for {ClassId}. Added {Count} students",
                    classId, result?.AddedStudents?.Count ?? 0);

                return new FlaskApiResponse<SetupClassResult>
                {
                    Success = true,
                    Message = result?.Message ?? "Class database setup successfully",
                    Data = new SetupClassResult
                    {
                        ClassId = classId.ToString(),
                        TotalStudents = result?.TotalStudents ?? 0,
                        AddedStudents = result?.AddedStudents ?? new List<string>(),
                        FailedStudents = result?.FailedStudents ?? new List<string>()
                    }
                };
            }

            var errorResult = JsonSerializer.Deserialize<FlaskErrorResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Failed to setup class database for {ClassId}: {Error}", classId, errorResult?.Error);

            return new FlaskApiResponse<SetupClassResult>
            {
                Success = false,
                Message = errorResult?.Error ?? "Failed to setup class database",
                Error = errorResult?.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up class database for {ClassId}", classId);
            return new FlaskApiResponse<SetupClassResult>
            {
                Success = false,
                Message = "Internal error during class setup",
                Error = ex.Message
            };
        }
    }

    public async Task<FlaskApiResponse<RecognizeFaceResult>> RecognizeFaceAsync(Guid classId, IFormFile imageFile)
    {
        try
        {
            _logger.LogInformation("Recognizing faces for class {ClassId}", classId);

            using var content = new MultipartFormDataContent();
            using var imageStream = imageFile.OpenReadStream();
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);

            content.Add(imageContent, "image", imageFile.FileName);
            content.Add(new StringContent(classId.ToString()), "classId");

            var response = await _httpClient.PostAsync("/api/face/recognize", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Flask API response status: {StatusCode}, Body: {Response}",
                response.StatusCode, jsonResponse);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlaskRecognizeResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Flask response deserialized - Success: {Success}, Message: {Message}, RecognizedCount: {Count}",
                    result?.Success, result?.Message, result?.RecognizedStudents?.Count ?? 0);

                // Check if Flask API actually succeeded (not just HTTP 200)
                if (result?.Success == false)
                {
                    _logger.LogWarning("Flask API returned success=false: {Message}", result.Message);
                    return new FlaskApiResponse<RecognizeFaceResult>
                    {
                        Success = false,
                        Message = result.Message ?? "Face recognition failed",
                        Data = new RecognizeFaceResult
                        {
                            TotalFacesDetected = result.TotalFacesDetected,
                            RecognizedStudents = new List<RecognizedStudent>()
                        }
                    };
                }

                _logger.LogInformation("Successfully recognized {Count} students in class {ClassId}",
                    result?.RecognizedStudents?.Count ?? 0, classId);

                return new FlaskApiResponse<RecognizeFaceResult>
                {
                    Success = true,
                    Message = result?.Message ?? "Face recognition completed",
                    Data = new RecognizeFaceResult
                    {
                        TotalFacesDetected = result?.TotalFacesDetected ?? 0,
                        RecognizedStudents = result?.RecognizedStudents?.Select(s => new RecognizedStudent
                        {
                            StudentId = s.StudentId,
                            Confidence = s.Confidence,
                            Distance = s.Distance
                        }).ToList() ?? new List<RecognizedStudent>()
                    }
                };
            }

            var errorResult = JsonSerializer.Deserialize<FlaskErrorResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Failed to recognize faces for class {ClassId}: {Error}", classId, errorResult?.Error);

            return new FlaskApiResponse<RecognizeFaceResult>
            {
                Success = false,
                Message = errorResult?.Error ?? "Failed to recognize faces",
                Error = errorResult?.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recognizing faces for class {ClassId}", classId);
            return new FlaskApiResponse<RecognizeFaceResult>
            {
                Success = false,
                Message = "Internal error during face recognition",
                Error = ex.Message
            };
        }
    }

    public async Task<FlaskApiResponse<DeleteStudentResult>> DeleteStudentFromClassAsync(Guid classId, Guid studentId)
    {
        try
        {
            _logger.LogInformation("Deleting student {StudentId} from class {ClassId}", studentId, classId);

            var response = await _httpClient.DeleteAsync($"/api/face/class/{classId}/student/{studentId}");
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlaskDeleteResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully deleted student {StudentId} from class {ClassId}", studentId, classId);

                return new FlaskApiResponse<DeleteStudentResult>
                {
                    Success = true,
                    Message = result?.Message ?? "Student deleted successfully",
                    Data = new DeleteStudentResult
                    {
                        Message = result?.Message ?? "",
                        DeletedFiles = result?.DeletedFiles ?? 0
                    }
                };
            }

            var errorResult = JsonSerializer.Deserialize<FlaskErrorResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Failed to delete student {StudentId} from class {ClassId}: {Error}",
                studentId, classId, errorResult?.Error);

            return new FlaskApiResponse<DeleteStudentResult>
            {
                Success = false,
                Message = errorResult?.Error ?? "Failed to delete student",
                Error = errorResult?.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId} from class {ClassId}", studentId, classId);
            return new FlaskApiResponse<DeleteStudentResult>
            {
                Success = false,
                Message = "Internal error during student deletion",
                Error = ex.Message
            };
        }
    }

    public async Task<FlaskApiResponse<DeleteStudentResult>> DeleteStudentAsync(Guid studentId)
    {
        try
        {
            _logger.LogInformation("Deleting student {StudentId} from all classes", studentId);

            var response = await _httpClient.DeleteAsync($"/api/face/student/{studentId}");
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlaskDeleteResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully deleted student {StudentId} from all classes", studentId);

                return new FlaskApiResponse<DeleteStudentResult>
                {
                    Success = true,
                    Message = result?.Message ?? "Student deleted successfully",
                    Data = new DeleteStudentResult
                    {
                        Message = result?.Message ?? "",
                        DeletedFiles = result?.DeletedFiles ?? 0
                    }
                };
            }

            var errorResult = JsonSerializer.Deserialize<FlaskErrorResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Failed to delete student {StudentId}: {Error}", studentId, errorResult?.Error);

            return new FlaskApiResponse<DeleteStudentResult>
            {
                Success = false,
                Message = errorResult?.Error ?? "Failed to delete student",
                Error = errorResult?.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", studentId);
            return new FlaskApiResponse<DeleteStudentResult>
            {
                Success = false,
                Message = "Internal error during student deletion",
                Error = ex.Message
            };
        }
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            _logger.LogInformation("Checking Flask API health");

            var response = await _httpClient.GetAsync("/health");
            var isHealthy = response.IsSuccessStatusCode;

            if (isHealthy)
            {
                _logger.LogInformation("Flask API is healthy");
            }
            else
            {
                _logger.LogWarning("Flask API health check failed with status {StatusCode}", response.StatusCode);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Flask API health check");
            return false;
        }
    }

    public async Task<FlaskApiResponse<DetectFacesResult>> DetectFacesAsync(IFormFile imageFile)
    {
        try
        {
            _logger.LogInformation("Detecting faces in uploaded image");

            using var content = new MultipartFormDataContent();
            using var imageStream = imageFile.OpenReadStream();
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);

            content.Add(imageContent, "image", imageFile.FileName);

            var response = await _httpClient.PostAsync("/api/face/detect", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<FlaskDetectResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Detected {Count} faces in image", result?.FacesDetected ?? 0);

                return new FlaskApiResponse<DetectFacesResult>
                {
                    Success = true,
                    Message = result?.Message ?? "Face detection completed",
                    Data = new DetectFacesResult
                    {
                        FacesDetected = result?.FacesDetected ?? 0,
                        Faces = result?.Faces?.Select(f => new FaceRegion
                        {
                            Confidence = f.Confidence,
                            Region = new FaceBoundingBox
                            {
                                X = f.Region.X,
                                Y = f.Region.Y,
                                Width = f.Region.W,
                                Height = f.Region.H
                            }
                        }).ToList() ?? new List<FaceRegion>()
                    }
                };
            }

            var errorResult = JsonSerializer.Deserialize<FlaskErrorResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogWarning("Failed to detect faces: {Error}", errorResult?.Error);

            return new FlaskApiResponse<DetectFacesResult>
            {
                Success = false,
                Message = errorResult?.Error ?? "Failed to detect faces",
                Error = errorResult?.Error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting faces");
            return new FlaskApiResponse<DetectFacesResult>
            {
                Success = false,
                Message = "Internal error during face detection",
                Error = ex.Message
            };
        }
    }

    #region Flask API Response Models

    private class FlaskRegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public int FacesDetected { get; set; }
    }

    private class FlaskSetupClassResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public List<string> AddedStudents { get; set; } = new();
        public List<string> FailedStudents { get; set; } = new();
    }

    private class FlaskRecognizeResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalFacesDetected { get; set; }
        public List<FlaskRecognizedStudent> RecognizedStudents { get; set; } = new();
    }

    private class FlaskRecognizedStudent
    {
        public string StudentId { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public decimal Distance { get; set; }
    }

    private class FlaskDeleteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DeletedFiles { get; set; }
    }

    private class FlaskDetectResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int FacesDetected { get; set; }
        public List<FlaskFace> Faces { get; set; } = new();
    }

    private class FlaskFace
    {
        public decimal Confidence { get; set; }
        public FlaskRegion Region { get; set; } = new();
    }

    private class FlaskRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

    private class FlaskErrorResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    #endregion
}
