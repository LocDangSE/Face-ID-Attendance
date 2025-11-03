"""Data models package"""

from .schemas import (
    RegisterStudentRequest,
    RegisterStudentResponse,
    RecognizeFacesRequest,
    RecognizeFacesResponse,
    RecognizedStudent,
    FaceRegion
)

__all__ = [
    'RegisterStudentRequest',
    'RegisterStudentResponse',
    'RecognizeFacesRequest',
    'RecognizeFacesResponse',
    'RecognizedStudent',
    'FaceRegion'
]
