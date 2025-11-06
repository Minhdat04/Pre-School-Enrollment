using Microsoft.AspNetCore.Mvc;
using PreschoolEnrollmentSystem.API.Filters;
using PreschoolEnrollmentSystem.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using PreschoolEnrollmentSystem.Core.DTOs.Shared; 
using PreschoolEnrollmentSystem.API.Helpers;     
using PreschoolEnrollmentSystem.Core.Exceptions;
using System;
using PreschoolEnrollmentSystem.Core.DTOs.Student;
using System.ComponentModel.DataAnnotations;

namespace PreschoolEnrollmentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AuthorizeRole("Admin", "Staff")] // Quản lý học sinh chỉ dành cho Admin hoặc Staff
    [Produces("application/json")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<StudentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _studentService.GetAllStudentsAsync();
            return Ok(students);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                return Ok(student);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Error = "NotFound", Message = ex.Message });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = "Invalid data provided",
                    Details = ModelState.ToErrorString()
                });
            }

            try
            {
                var newStudent = await _studentService.CreateStudentAsync(dto);
                return CreatedAtAction(nameof(GetStudentById), new { id = newStudent.Id }, newStudent);
            }
            catch (ValidationException ex) // Bắt lỗi nghiệp vụ từ Service
            {
                return BadRequest(new ErrorResponse { Error = "ValidationFailed", Message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = "Invalid data provided",
                    Details = ModelState.ToErrorString()
                });
            }

            try
            {
                var updatedStudent = await _studentService.UpdateStudentAsync(id, dto);
                return Ok(updatedStudent);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Error = "NotFound", Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse { Error = "ValidationFailed", Message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            try
            {
                // Lấy firebase Uid của Admin/Staff đang thực hiện hành động
                var firebaseUid = User.GetCurrentFirebaseUid();

                await _studentService.DeleteStudentAsync(id, firebaseUid);
                return NoContent(); // 204 No Content là chuẩn cho delete
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Error = "NotFound", Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ErrorResponse { Error = "Unauthorized", Message = ex.Message });
            }
        }
    }
}