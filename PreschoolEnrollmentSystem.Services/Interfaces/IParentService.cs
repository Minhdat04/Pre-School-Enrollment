using PreschoolEnrollmentSystem.Core.DTOs.Child;
using PreschoolEnrollmentSystem.Core.DTOs.Parent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Interfaces
{
    public interface IParentService
    {
        Task<ParentProfileDto> GetParentProfileAsync(string firebaseUid);
        Task<ParentProfileDto> UpdateParentProfileAsync(string firebaseUid, UpdateParentProfileDto dto);
        Task<ChildDto> AddChildAsync(string parentFirebaseUid, CreateChildDto dto);
        Task<ChildDto> UpdateChildAsync(string parentFirebaseUid, Guid childId, UpdateChildDto dto);
        Task<bool> DeleteChildAsync(string parentFirebaseUid, Guid childId);
    }
}
