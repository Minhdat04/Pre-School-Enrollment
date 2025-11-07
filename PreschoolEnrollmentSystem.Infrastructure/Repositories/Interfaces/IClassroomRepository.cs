using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IClassroomRepository : IRepository<Classroom>
    {
        Task<Classroom?> GetClassroomWithStudentsAsync(Guid classroomId);
        Task<Classroom?> GetClassroomWithTeachersAsync(Guid classroomId);
        Task<Classroom?> GetClassroomWithAllDetailsAsync(Guid classroomId);
        Task<IEnumerable<Classroom>> GetAvailableClassroomsAsync();
        Task<Classroom?> GetClassroomByNameAsync(string name);
        Task<bool> ClassroomNameExistsAsync(string name, Guid? excludeId = null);
        Task<int> GetAvailableCapacityAsync(Guid classroomId);
    }
}
