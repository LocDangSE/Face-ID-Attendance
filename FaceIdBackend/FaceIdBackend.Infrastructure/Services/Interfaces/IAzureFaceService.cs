using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace FaceIdBackend.Infrastructure.Services.Interfaces;

public interface IAzureFaceService
{
    // PersonGroup Management
    Task<string> CreatePersonGroupAsync(string personGroupId, string name, string? userData = null);
    Task DeletePersonGroupAsync(string personGroupId);
    Task TrainPersonGroupAsync(string personGroupId);
    Task<TrainingStatus> GetTrainingStatusAsync(string personGroupId);

    // Person Management
    Task<Guid> CreatePersonAsync(string personGroupId, string name, string? userData = null);
    Task DeletePersonAsync(string personGroupId, Guid personId);
    Task<Guid> AddPersonFaceAsync(string personGroupId, Guid personId, Stream imageStream);
    Task DeletePersonFaceAsync(string personGroupId, Guid personId, Guid faceId);

    // Face Detection and Recognition
    Task<List<DetectedFace>> DetectFacesAsync(Stream imageStream);
    Task<List<IdentifyResult>> IdentifyFacesAsync(string personGroupId, List<Guid> faceIds);
}
