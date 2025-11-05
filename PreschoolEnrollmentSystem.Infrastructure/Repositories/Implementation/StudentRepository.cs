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
    public class StudentRepository : Repository<Student>, IStudentRepository
    {
        public StudentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Student>> GetStudentsByClassroomAsync(Guid classroomId)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.ClassroomId == classroomId)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsByParentAsync(Guid parentId)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.ParentId == parentId)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<Student?> GetStudentByIdWithDetailsAsync(Guid studentId)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted)
                .Include(s => s.Parent)
                .Include(s => s.Classroom)
                .FirstOrDefaultAsync(s => s.Id == studentId);
        }

        public async Task<Student?> GetStudentWithClassroomAsync(Guid studentId)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted)
                .Include(s => s.Classroom)
                .FirstOrDefaultAsync(s => s.Id == studentId);
        }

        public async Task<Student?> GetStudentWithParentAsync(Guid studentId)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted)
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.Id == studentId);
        }

        public async Task<IEnumerable<Student>> GetStudentsByGradeAsync(string grade)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.Grade == grade)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsByGenderAsync(Gender gender)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.Gender == gender)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsWithoutClassroomAsync()
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.ClassroomId == null)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<int> GetStudentCountByClassroomAsync(Guid classroomId)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.ClassroomId == classroomId)
                .CountAsync();
        }

        public async Task<bool> IsClassroomFullAsync(Guid classroomId)
        {
            var classroom = await _context.Classrooms
                .FirstOrDefaultAsync(c => c.Id == classroomId);

            if (classroom == null)
                return true;

            var studentCount = await GetStudentCountByClassroomAsync(classroomId);
            return studentCount >= classroom.Capacity;
        }
    }
}
