using AutoMapper;
using FaceIdBackend.Domain.Data;
using FaceIdBackend.Shared.DTOs;

namespace FaceIdBackend.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Student mappings
        CreateMap<Student, StudentDto>();
        CreateMap<CreateStudentRequest, Student>();
        CreateMap<UpdateStudentRequest, Student>();

        // Class mappings
        CreateMap<Class, ClassDto>();
        CreateMap<CreateClassRequest, Class>();
        CreateMap<UpdateClassRequest, Class>();

        // Attendance Session mappings
        CreateMap<AttendanceSession, AttendanceSessionDto>()
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Class.ClassName))
            .ForMember(dest => dest.Class, opt => opt.MapFrom(src => new ClassInfo
            {
                ClassId = src.Class.ClassId,
                ClassName = src.Class.ClassName,
                ClassCode = src.Class.ClassCode
            }))
            .ForMember(dest => dest.TotalEnrolled, opt => opt.Ignore())
            .ForMember(dest => dest.PresentCount, opt => opt.Ignore())
            .ForMember(dest => dest.AbsentCount, opt => opt.Ignore());

        CreateMap<AttendanceSession, SessionDetailsDto>()
            .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Class.ClassName))
            .ForMember(dest => dest.Class, opt => opt.MapFrom(src => new ClassInfo
            {
                ClassId = src.Class.ClassId,
                ClassName = src.Class.ClassName,
                ClassCode = src.Class.ClassCode
            }))
            .ForMember(dest => dest.TotalEnrolled, opt => opt.Ignore())
            .ForMember(dest => dest.PresentCount, opt => opt.Ignore())
            .ForMember(dest => dest.AbsentCount, opt => opt.Ignore())
            .ForMember(dest => dest.AttendanceRate, opt => opt.Ignore())
            .ForMember(dest => dest.Students, opt => opt.Ignore());

        // Attendance Record mappings
        CreateMap<AttendanceRecord, AttendanceDetailDto>()
            .ForMember(dest => dest.StudentNumber, opt => opt.MapFrom(src => src.Student.StudentNumber))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.Student.FirstName} {src.Student.LastName}"));
    }
}
