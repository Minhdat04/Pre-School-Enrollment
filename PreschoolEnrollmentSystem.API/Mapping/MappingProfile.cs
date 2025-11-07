using AutoMapper;
using PreschoolEnrollmentSystem.Core.DTOs.Child;
using PreschoolEnrollmentSystem.Core.DTOs.Parent;
using PreschoolEnrollmentSystem.Core.DTOs.Student;
using PreschoolEnrollmentSystem.Core.Entities;

namespace PreschoolEnrollmentSystem.API.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Parent Mappings
            CreateMap<User, ParentProfileDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
            CreateMap<UpdateParentProfileDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Không update các trường quan trọng
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.FirebaseUid, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            // Child Mappings
            CreateMap<Child, ChildDto>();
            CreateMap<CreateChildDto, Child>();
            CreateMap<UpdateChildDto, Child>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Không update Id và ParentId
                .ForMember(dest => dest.ParentId, opt => opt.Ignore());

            // Student Mappings
            CreateMap<CreateStudentDto, Student>();
            CreateMap<UpdateStudentDto, Student>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ParentId, opt => opt.Ignore()); // Không cho phép map ParentId khi update

            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src =>
                    src.Parent != null ? $"{src.Parent.FirstName} {src.Parent.LastName}" : null))
                .ForMember(dest => dest.ClassroomName, opt => opt.MapFrom(src =>
                    src.Classroom != null ? src.Classroom.Name : null));
        }
    }
}
