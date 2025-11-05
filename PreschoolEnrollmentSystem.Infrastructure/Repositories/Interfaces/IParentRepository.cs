using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    /// <summary>
    /// Parent-specific repository interface
    /// Why: Extends generic repository with Parent-specific queries
    /// Inherits: All CRUD operations from IRepository<Parent>
    /// Adds: Parent-specific lookup methods
    /// </summary>
    public interface IParentRepository : IRepository<Parent>
    {
        Task<Parent?> GetByEmailAsync(string email);
        Task<Parent?> GetByFirebaseUidAsync(string firebaseUid);
        Task<IEnumerable<Parent>> GetActiveParentsAsync();
        Task<IEnumerable<Parent>> GetIncompleteProfilesAsync();
        Task<IEnumerable<Parent>> GetEnrollmentEligibleParentsAsync();
        Task<IEnumerable<Parent>> GetUnverifiedEmailParentsAsync();
        Task<IEnumerable<Parent>> SearchByNameAsync(string searchTerm);
        Task<(int TotalParents, int ActiveParents, int UnverifiedEmails, int IncompleteProfiles)> GetStatisticsAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<IEnumerable<Parent>> GetByPhoneNumberAsync(string phoneNumber);
        Task<IEnumerable<Parent>> GetRecentlyRegisteredAsync(int days = 7);
        Task<IEnumerable<Parent>> GetInactiveParentsAsync(int daysSinceLastLogin = 90);
    }
}