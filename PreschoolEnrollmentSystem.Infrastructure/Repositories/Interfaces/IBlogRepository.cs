using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IBlogRepository : IRepository<Blog>
    {
        Task<IEnumerable<Blog>> GetBlogsWithTagsAsync();
        Task<Blog?> GetBlogByIdWithTagsAsync(Guid blogId);
        Task<IEnumerable<Blog>> SearchBlogsAsync(string searchTerm);
        Task<IEnumerable<Blog>> GetBlogsByAuthorAsync(string author);
        Task<IEnumerable<Blog>> GetRecentBlogsAsync(int count);
    }
}
