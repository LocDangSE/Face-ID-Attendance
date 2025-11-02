using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using FaceIdBackend.Infrastructure.Configuration;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FaceIdBackend.Infrastructure.Services;

public class AzureFaceService : IAzureFaceService
{
    private readonly IFaceClient _faceClient;
    private readonly AzureFaceSettings _settings;

    public AzureFaceService(IOptions<AzureFaceSettings> settings)
    {
        _settings = settings.Value;
        _faceClient = new FaceClient(new ApiKeyServiceClientCredentials(_settings.SubscriptionKey))
        {
            Endpoint = _settings.Endpoint
        };
    }

    // PersonGroup Management
    public async Task<string> CreatePersonGroupAsync(string personGroupId, string name, string? userData = null)
    {
        try
        {
            await _faceClient.PersonGroup.CreateAsync(
                personGroupId,
                name,
                userData,
                recognitionModel: _settings.RecognitionModel
            );
            return personGroupId;
        }
        catch (APIErrorException ex) when (ex.Body.Error.Code == "PersonGroupExists")
        {
            // PersonGroup already exists, return the ID
            return personGroupId;
        }
    }

    public async Task DeletePersonGroupAsync(string personGroupId)
    {
        try
        {
            await _faceClient.PersonGroup.DeleteAsync(personGroupId);
        }
        catch (APIErrorException ex) when (ex.Body.Error.Code == "PersonGroupNotFound")
        {
            // Already deleted or doesn't exist
        }
    }

    public async Task TrainPersonGroupAsync(string personGroupId)
    {
        await _faceClient.PersonGroup.TrainAsync(personGroupId);
    }

    public async Task<TrainingStatus> GetTrainingStatusAsync(string personGroupId)
    {
        return await _faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId);
    }

    // Person Management
    public async Task<Guid> CreatePersonAsync(string personGroupId, string name, string? userData = null)
    {
        var person = await _faceClient.PersonGroupPerson.CreateAsync(personGroupId, name, userData);
        return person.PersonId;
    }

    public async Task DeletePersonAsync(string personGroupId, Guid personId)
    {
        try
        {
            await _faceClient.PersonGroupPerson.DeleteAsync(personGroupId, personId);
        }
        catch (APIErrorException ex) when (ex.Body.Error.Code == "PersonNotFound")
        {
            // Already deleted or doesn't exist
        }
    }

    public async Task<Guid> AddPersonFaceAsync(string personGroupId, Guid personId, Stream imageStream)
    {
        var persistedFace = await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(
            personGroupId,
            personId,
            imageStream,
            detectionModel: _settings.DetectionModel
        );
        return persistedFace.PersistedFaceId;
    }

    public async Task DeletePersonFaceAsync(string personGroupId, Guid personId, Guid faceId)
    {
        try
        {
            await _faceClient.PersonGroupPerson.DeleteFaceAsync(personGroupId, personId, faceId);
        }
        catch (APIErrorException ex) when (ex.Body.Error.Code == "FaceNotFound")
        {
            // Already deleted or doesn't exist
        }
    }

    // Face Detection and Recognition
    public async Task<List<DetectedFace>> DetectFacesAsync(Stream imageStream)
    {
        var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(
            imageStream,
            recognitionModel: _settings.RecognitionModel,
            detectionModel: _settings.DetectionModel,
            returnFaceId: true
        );

        return detectedFaces.ToList();
    }

    public async Task<List<IdentifyResult>> IdentifyFacesAsync(string personGroupId, List<Guid> faceIds)
    {
        if (!faceIds.Any())
            return new List<IdentifyResult>();

        var identifyResults = await _faceClient.Face.IdentifyAsync(
            faceIds,
            personGroupId,
            confidenceThreshold: _settings.ConfidenceThreshold
        );

        return identifyResults.ToList();
    }
}
