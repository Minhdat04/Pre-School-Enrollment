using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;
using PreschoolEnrollmentSystem.Infrastructure.Data;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation
{
    public class ChildRepository : Repository<Child>, IChildRepository
    {
        public ChildRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Child>> GetChildrenByParentAsync(Guid parentId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted && c.ParentId == parentId)
                .OrderBy(c => c.FullName)
                .ToListAsync();
        }

        public async Task<Child?> GetChildByIdWithParentAsync(Guid childId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted)
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == childId);
        }

        public async Task<IEnumerable<Child>> GetChildrenByGenderAsync(Gender gender)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted && c.Gender == gender)
                .OrderBy(c => c.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Child>> GetChildrenByAgeRangeAsync(int minAge, int maxAge)
        {
            var today = DateTime.Today;
            var maxBirthdate = today.AddYears(-minAge);
            var minBirthdate = today.AddYears(-maxAge - 1);

            return await _dbSet
                .Where(c => !c.IsDeleted &&
                           c.Birthdate >= minBirthdate &&
                           c.Birthdate <= maxBirthdate)
                .OrderBy(c => c.Birthdate)
                .ToListAsync();
        }

        public async Task<bool> ParentHasChildAsync(Guid parentId, string fullName, DateTime birthdate)
        {
            return await _dbSet
                .AnyAsync(c => !c.IsDeleted &&
                              c.ParentId == parentId &&
                              c.FullName.ToLower() == fullName.ToLower() &&
                              c.Birthdate.Date == birthdate.Date);
        }

        public async Task<int> GetChildCountByParentAsync(Guid parentId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted && c.ParentId == parentId)
                .CountAsync();
        }

        public async Task<Child?> GetChildByIdAndParentIdAsync(Guid childId, Guid parentId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted && c.Id == childId && c.ParentId == parentId)
                .FirstOrDefaultAsync();
        }
    }
}
