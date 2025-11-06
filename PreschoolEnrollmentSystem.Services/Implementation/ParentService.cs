using AutoMapper;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.Core.DTOs.Child;
using PreschoolEnrollmentSystem.Core.DTOs.Parent;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Exceptions;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;
using PreschoolEnrollmentSystem.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class ParentService : IParentService
    {
        private readonly IUserRepository _userRepository;
        private readonly IChildRepository _childRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ParentService> _logger;

        public ParentService(
            IUserRepository userRepository,
            IChildRepository childRepository,
            IMapper mapper,
            ILogger<ParentService> logger)
        {
            _userRepository = userRepository;
            _childRepository = childRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ParentProfileDto> GetParentProfileAsync(string firebaseUid)
        {
            var parent = await _userRepository.GetParentWithChildrenAsync(firebaseUid);
            if (parent == null)
            {
                _logger.LogWarning("Parent profile not found for UID: {FirebaseUid}", firebaseUid);
                throw new EntityNotFoundException("Parent profile not found.");
            }

            return _mapper.Map<ParentProfileDto>(parent);
        }

        public async Task<ParentProfileDto> UpdateParentProfileAsync(string firebaseUid, UpdateParentProfileDto dto)
        {
            var parent = await _userRepository.GetByFirebaseUidAsync(firebaseUid);
            if (parent == null)
            {
                _logger.LogWarning("Parent profile not found for UID: {FirebaseUid}", firebaseUid);
                throw new EntityNotFoundException("Parent profile not found.");
            }

            // Map các trường từ DTO vào entity (AutoMapper sẽ bỏ qua các trường
            // đã được Ignore trong MappingProfile)
            _mapper.Map(dto, parent);

            _userRepository.Update(parent);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Parent profile updated for UID: {FirebaseUid}", firebaseUid);

            // Nạp lại thông tin children để trả về profile đầy đủ
            var updatedParentWithChildren = await _userRepository.GetParentWithChildrenAsync(firebaseUid);
            return _mapper.Map<ParentProfileDto>(updatedParentWithChildren);
        }

        public async Task<ChildDto> AddChildAsync(string parentFirebaseUid, CreateChildDto dto)
        {
            var parent = await _userRepository.GetByFirebaseUidAsync(parentFirebaseUid);
            if (parent == null)
            {
                _logger.LogWarning("Parent profile not found for UID: {FirebaseUid}", parentFirebaseUid);
                throw new EntityNotFoundException("Parent profile not found.");
            }

            var child = _mapper.Map<Child>(dto);
            child.ParentId = parent.Id; // Gán ID phụ huynh cho con

            await _childRepository.AddAsync(child);
            await _childRepository.SaveChangesAsync();

            _logger.LogInformation("New child created for parent UID: {FirebaseUid}, Child ID: {ChildId}",
                parentFirebaseUid, child.Id);

            return _mapper.Map<ChildDto>(child);
        }

        public async Task<ChildDto> UpdateChildAsync(string parentFirebaseUid, Guid childId, UpdateChildDto dto)
        {
            var parent = await _userRepository.GetByFirebaseUidAsync(parentFirebaseUid);
            if (parent == null)
            {
                _logger.LogWarning("Parent profile not found for UID: {FirebaseUid}", parentFirebaseUid);
                throw new EntityNotFoundException("Parent profile not found.");
            }

            // Dùng hàm đã tạo để kiểm tra sở hữu và lấy con
            var child = await _childRepository.GetChildByIdAndParentIdAsync(childId, parent.Id);
            if (child == null)
            {
                _logger.LogWarning("Child not found or parent {FirebaseUid} does not own child {ChildId}",
                    parentFirebaseUid, childId);
                throw new EntityNotFoundException("Child not found or access denied.");
            }

            _mapper.Map(dto, child);

            _childRepository.Update(child);
            await _childRepository.SaveChangesAsync();

            _logger.LogInformation("Child {ChildId} updated by parent {FirebaseUid}", childId, parentFirebaseUid);

            return _mapper.Map<ChildDto>(child);
        }

        public async Task<bool> DeleteChildAsync(string parentFirebaseUid, Guid childId)
        {
            var parent = await _userRepository.GetByFirebaseUidAsync(parentFirebaseUid);
            if (parent == null)
            {
                _logger.LogWarning("Parent profile not found for UID: {FirebaseUid}", parentFirebaseUid);
                throw new EntityNotFoundException("Parent profile not found.");
            }

            var child = await _childRepository.GetChildByIdAndParentIdAsync(childId, parent.Id);
            if (child == null)
            {
                _logger.LogWarning("Child not found or parent {FirebaseUid} does not own child {ChildId}",
                    parentFirebaseUid, childId);
                throw new EntityNotFoundException("Child not found or access denied.");
            }

            // Sử dụng soft delete từ Repository (DeletedBy nên là ID của parent)
            await _childRepository.DeleteAsync(child, parent.FirebaseUid);
            await _childRepository.SaveChangesAsync();

            _logger.LogInformation("Child {ChildId} deleted by parent {FirebaseUid}", childId, parentFirebaseUid);

            return true;
        }
    }
}
