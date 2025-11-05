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
    public class BlogRepository : Repository<Blog>, IBlogRepository
    {
        public BlogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Blog>> GetBlogsWithTagsAsync()
        {
            return await _dbSet
                .Where(b => !b.IsDeleted)
                .Include(b => b.Tags)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Blog?> GetBlogByIdWithTagsAsync(Guid blogId)
        {
            return await _dbSet
                .Where(b => !b.IsDeleted)
                .Include(b => b.Tags)
                .FirstOrDefaultAsync(b => b.Id == blogId);
        }

        public async Task<IEnumerable<Blog>> SearchBlogsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var lowerSearchTerm = searchTerm.ToLower();

            return await _dbSet
                .Where(b => !b.IsDeleted &&
                           (b.Title.ToLower().Contains(lowerSearchTerm) ||
                            b.Content.ToLower().Contains(lowerSearchTerm) ||
                            b.Author.ToLower().Contains(lowerSearchTerm)))
                .Include(b => b.Tags)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> GetBlogsByAuthorAsync(string author)
        {
            if (string.IsNullOrWhiteSpace(author))
                return new List<Blog>();

            return await _dbSet
                .Where(b => !b.IsDeleted && b.Author.ToLower() == author.ToLower())
                .Include(b => b.Tags)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Blog>> GetRecentBlogsAsync(int count)
        {
            return await _dbSet
                .Where(b => !b.IsDeleted)
                .Include(b => b.Tags)
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
