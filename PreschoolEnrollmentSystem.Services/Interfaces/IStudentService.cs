using PreschoolEnrollmentSystem.Core.DTOs.Student;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Interfaces
{
    public interface IStudentService
    {
        Task<StudentDto> GetStudentByIdAsync(Guid id);
        Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
        Task<StudentDto> CreateStudentAsync(CreateStudentDto dto);
        Task<StudentDto> UpdateStudentAsync(Guid id, UpdateStudentDto dto);
        Task<bool> DeleteStudentAsync(Guid id, string deletedByFirebaseUid);
    }
}
