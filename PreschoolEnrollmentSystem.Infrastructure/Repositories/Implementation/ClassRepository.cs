using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Infrastructure.Data;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation
{
    public class ClassroomRepository : Repository<Classroom>, IClassroomRepository
    {
        public ClassroomRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Classroom?> GetClassroomWithStudentsAsync(Guid classroomId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted)
                .Include(c => c.Students.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == classroomId);
        }

        public async Task<Classroom?> GetClassroomWithTeachersAsync(Guid classroomId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted)
                .Include(c => c.Teachers.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == classroomId);
        }

        public async Task<Classroom?> GetClassroomWithAllDetailsAsync(Guid classroomId)
        {
            return await _dbSet
                .Where(c => !c.IsDeleted)
                .Include(c => c.Students.Where(s => !s.IsDeleted))
                .Include(c => c.Teachers.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == classroomId);
        }

        public async Task<IEnumerable<Classroom>> GetAvailableClassroomsAsync()
        {
            return await _dbSet
                .Where(c => !c.IsDeleted)
                .Include(c => c.Students.Where(s => !s.IsDeleted))
                .ToListAsync()
                .ContinueWith(task =>
                {
                    return task.Result.Where(c => c.Students.Count < c.Capacity).ToList();
                });
        }

        public async Task<Classroom?> GetClassroomByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _dbSet
                .Where(c => !c.IsDeleted)
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ClassroomNameExistsAsync(string name, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var query = _dbSet.Where(c => !c.IsDeleted && c.Name.ToLower() == name.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<int> GetAvailableCapacityAsync(Guid classroomId)
        {
            var classroom = await _dbSet
                .Where(c => !c.IsDeleted)
                .Include(c => c.Students.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == classroomId);

            if (classroom == null)
                return 0;

            return classroom.Capacity - classroom.Students.Count;
        }
    }
}
