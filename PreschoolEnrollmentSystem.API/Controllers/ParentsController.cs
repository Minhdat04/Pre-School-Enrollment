using Microsoft.AspNetCore.Mvc;
using PreschoolEnrollmentSystem.Services.Interfaces;
using PreschoolEnrollmentSystem.API.Filters; // Cho AuthorizeRole
using PreschoolEnrollmentSystem.Core.DTOs.Parent;
using PreschoolEnrollmentSystem.Core.DTOs.Child;
using PreschoolEnrollmentSystem.Core.Exceptions; // Cho EntityNotFoundException
using PreschoolEnrollmentSystem.API.Helpers; // Cho GetCurrentFirebaseUid()
using PreschoolEnrollmentSystem.Core.DTOs.Shared;

namespace PreschoolEnrollmentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AuthorizeRole("Parent")] 
    [Produces("application/json")]
    public class ParentsController : ControllerBase
    {
        private readonly IParentService _parentService;
        private readonly ILogger<ParentsController> _logger;

        public ParentsController(IParentService parentService, ILogger<ParentsController> logger)
        {
            _parentService = parentService;
            _logger = logger;
        }

        #region Parent Profile


        [HttpGet("profile")]
        [ProducesResponseType(typeof(ParentProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetParentProfile()
        {
            try
            {
                var firebaseUid = User.GetCurrentFirebaseUid();
                var profile = await _parentService.GetParentProfileAsync(firebaseUid);
                return Ok(profile);
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

        [HttpPut("profile")]
        [ProducesResponseType(typeof(ParentProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateParentProfile([FromBody] UpdateParentProfileDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = "Invalid data provided",
                    Details = ModelState.ToErrorString() // Dùng extension method từ AuthController
                });
            }

            try
            {
                var firebaseUid = User.GetCurrentFirebaseUid();
                var updatedProfile = await _parentService.UpdateParentProfileAsync(firebaseUid, dto);
                return Ok(updatedProfile);
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

        #endregion

        #region Child Management

        [HttpPost("children")]
        [ProducesResponseType(typeof(ChildDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddChild([FromBody] CreateChildDto dto)
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
                var firebaseUid = User.GetCurrentFirebaseUid();
                var newChild = await _parentService.AddChildAsync(firebaseUid, dto);

                return CreatedAtAction(nameof(GetParentProfile), newChild);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new ErrorResponse { Error = "Unauthorized", Message = ex.Message });
            }
        }

        [HttpPut("children/{childId:guid}")]
        [ProducesResponseType(typeof(ChildDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateChild(Guid childId, [FromBody] UpdateChildDto dto)
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
                var firebaseUid = User.GetCurrentFirebaseUid();
                var updatedChild = await _parentService.UpdateChildAsync(firebaseUid, childId, dto);
                return Ok(updatedChild);
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

        [HttpDelete("children/{childId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteChild(Guid childId)
        {
            try
            {
                var firebaseUid = User.GetCurrentFirebaseUid();
                await _parentService.DeleteChildAsync(firebaseUid, childId);
                return NoContent(); 
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

        #endregion
    }
}