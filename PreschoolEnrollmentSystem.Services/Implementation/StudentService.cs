using AutoMapper;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.Core.DTOs.Student;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Exceptions;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;
using PreschoolEnrollmentSystem.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IClassroomRepository _classroomRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            IStudentRepository studentRepository,
            IUserRepository userRepository,
            IClassroomRepository classroomRepository,
            IMapper mapper,
            ILogger<StudentService> logger)
        {
            _studentRepository = studentRepository;
            _userRepository = userRepository;
            _classroomRepository = classroomRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<StudentDto> GetStudentByIdAsync(Guid id)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(id);
            if (student == null)
            {
                _logger.LogWarning("Student not found with ID: {StudentId}", id);
                throw new EntityNotFoundException("Student not found.");
            }
            return _mapper.Map<StudentDto>(student);
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _studentRepository.GetAllStudentsWithDetailsAsync();
            return _mapper.Map<IEnumerable<StudentDto>>(students);
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto)
        {
            // 1. Validate ParentId
            var parent = await _userRepository.GetByIdAsync(dto.ParentId);
            if (parent == null || parent.Role != Core.Enums.UserRole.Parent)
            {
                throw new ValidationException("Invalid ParentId or user is not a Parent.");
            }

            // 2. Validate ClassroomId (if provided)
            if (dto.ClassroomId.HasValue)
            {
                var classroom = await _classroomRepository.GetByIdAsync(dto.ClassroomId.Value);
                if (classroom == null)
                {
                    throw new ValidationException("Invalid ClassroomId.");
                }
            }

            // 3. Map and Create
            var student = _mapper.Map<Student>(dto);

            await _studentRepository.AddAsync(student);
            await _studentRepository.SaveChangesAsync();

            _logger.LogInformation("New student created with ID: {StudentId}", student.Id);

            // 4. Return full DTO
            return await GetStudentByIdAsync(student.Id);
        }

        public async Task<StudentDto> UpdateStudentAsync(Guid id, UpdateStudentDto dto)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                _logger.LogWarning("Student not found for update with ID: {StudentId}", id);
                throw new EntityNotFoundException("Student not found.");
            }

            // 1. Validate ClassroomId (if provided and changed)
            if (dto.ClassroomId.HasValue && dto.ClassroomId != student.ClassroomId)
            {
                var classroom = await _classroomRepository.GetByIdAsync(dto.ClassroomId.Value);
                if (classroom == null)
                {
                    throw new ValidationException("Invalid ClassroomId.");
                }
            }

            // 2. Map (bảo toàn ParentId gốc)
            _mapper.Map(dto, student);

            _studentRepository.Update(student);
            await _studentRepository.SaveChangesAsync();

            _logger.LogInformation("Student updated with ID: {StudentId}", student.Id);

            // 3. Return full DTO
            return await GetStudentByIdAsync(student.Id);
        }

        public async Task<bool> DeleteStudentAsync(Guid id, string deletedByFirebaseUid)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                _logger.LogWarning("Student not found for deletion with ID: {StudentId}", id);
                throw new EntityNotFoundException("Student not found.");
            }

            // Dùng Firebase UID của Admin/Staff làm 'DeletedBy'
            await _studentRepository.DeleteAsync(student, deletedByFirebaseUid);
            await _studentRepository.SaveChangesAsync();

            _logger.LogInformation("Student soft-deleted with ID: {StudentId} by {DeletedBy}",
                id, deletedByFirebaseUid);

            return true;
        }
    }
}
