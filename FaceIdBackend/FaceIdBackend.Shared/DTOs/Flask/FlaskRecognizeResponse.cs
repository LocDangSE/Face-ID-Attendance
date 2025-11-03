using System.Text.Json.Serialization;

namespace FaceIdBackend.Shared.DTOs.Flask;

public class FlaskRecognizeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("total_faces_detected")]
    public int TotalFacesDetected { get; set; }

    [JsonPropertyName("recognized_students")]
    public List<FlaskRecognizedStudent> RecognizedStudents { get; set; } = new();
}

public class FlaskRecognizedStudent
{
    [JsonPropertyName("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    [JsonPropertyName("distance")]
    public decimal Distance { get; set; }
}
