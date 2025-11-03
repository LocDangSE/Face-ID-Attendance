namespace FaceIdBackend.Shared.DTOs.Flask;

public class FlaskRegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int FacesDetected { get; set; }
}
