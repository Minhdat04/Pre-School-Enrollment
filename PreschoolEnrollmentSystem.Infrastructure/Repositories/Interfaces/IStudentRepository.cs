using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IStudentRepository : IRepository<Student>
    {
        Task<IEnumerable<Student>> GetStudentsByClassroomAsync(Guid classroomId);
        Task<IEnumerable<Student>> GetStudentsByParentAsync(Guid parentId);
        Task<Student?> GetStudentByIdWithDetailsAsync(Guid studentId);
        Task<Student?> GetStudentWithClassroomAsync(Guid studentId);
        Task<Student?> GetStudentWithParentAsync(Guid studentId);
        Task<IEnumerable<Student>> GetStudentsByGradeAsync(string grade);
        Task<IEnumerable<Student>> GetStudentsByGenderAsync(Gender gender);
        Task<IEnumerable<Student>> GetStudentsWithoutClassroomAsync();
        Task<int> GetStudentCountByClassroomAsync(Guid classroomId);
        Task<bool> IsClassroomFullAsync(Guid classroomId);
    }
}
