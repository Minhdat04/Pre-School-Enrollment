using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IChildRepository : IRepository<Child>
    {
        Task<IEnumerable<Child>> GetChildrenByParentAsync(Guid parentId);
        Task<Child?> GetChildByIdWithParentAsync(Guid childId);
        Task<IEnumerable<Child>> GetChildrenByGenderAsync(Gender gender);
        Task<IEnumerable<Child>> GetChildrenByAgeRangeAsync(int minAge, int maxAge);
        Task<bool> ParentHasChildAsync(Guid parentId, string fullName, DateTime birthdate);
        Task<int> GetChildCountByParentAsync(Guid parentId);
        Task<Child?> GetChildByIdAndParentIdAsync(Guid childId, Guid parentId);
    }
}
