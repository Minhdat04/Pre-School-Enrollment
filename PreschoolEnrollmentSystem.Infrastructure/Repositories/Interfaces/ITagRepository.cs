using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface ITagRepository : IRepository<Tag>
    {
        Task<Tag?> GetTagByNameAsync(string name);
        Task<IEnumerable<Tag>> GetTagsWithBlogsAsync();
        Task<Tag?> GetTagWithBlogsAsync(Guid tagId);
        Task<bool> TagNameExistsAsync(string name, Guid? excludeId = null);
        Task<IEnumerable<Tag>> GetPopularTagsAsync(int count);
    }
}
