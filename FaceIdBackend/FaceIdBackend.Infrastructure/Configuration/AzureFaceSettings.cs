namespace FaceIdBackend.Infrastructure.Configuration;

public class AzureFaceSettings
{
    public string Endpoint { get; set; } = null!;
    public string SubscriptionKey { get; set; } = null!;
    public string RecognitionModel { get; set; } = "recognition_04";
    public string DetectionModel { get; set; } = "detection_03";
    public double ConfidenceThreshold { get; set; } = 0.75;
}
