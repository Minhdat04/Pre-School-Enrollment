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
    public class TagRepository : Repository<Tag>, ITagRepository
    {
        public TagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Tag?> GetTagByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _dbSet
                .Where(t => !t.IsDeleted)
                .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<Tag>> GetTagsWithBlogsAsync()
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.Blogs.Where(b => !b.IsDeleted))
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tag?> GetTagWithBlogsAsync(Guid tagId)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.Blogs.Where(b => !b.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == tagId);
        }

        public async Task<bool> TagNameExistsAsync(string name, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var query = _dbSet.Where(t => !t.IsDeleted && t.Name.ToLower() == name.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.Blogs.Where(b => !b.IsDeleted))
                .OrderByDescending(t => t.Blogs.Count)
                .Take(count)
                .ToListAsync();
        }
    }
}
