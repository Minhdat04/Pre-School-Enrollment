using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using PreschoolEnrollmentSystem.Services.Interfaces;
using PreschoolEnrollmentSystem.Services.SeedData;

namespace PreschoolEnrollmentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly IDataSeedingService _seedingService;
        private readonly IFirebaseStorageService _storageService;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SeedController> _logger;

        public SeedController(
            IDataSeedingService seedingService,
            IFirebaseStorageService storageService,
            IHostEnvironment environment,
            ILogger<SeedController> logger)
        {
            _seedingService = seedingService;
            _storageService = storageService;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Seeds the database with sample data (Development only)
        /// </summary>
        /// <returns>Seeding result with details</returns>
        [HttpPost("run")]
        [ProducesResponseType(typeof(DataSeedingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SeedDatabase([FromQuery] bool confirm = false)
        {
            try
            {
                // Safety check: Warn if in Production but allow with confirmation
                if (_environment.IsProduction() && !confirm)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Seeding in Production requires explicit confirmation. Add '?confirm=true' to proceed.",
                        warning = "This will create test data in your production database!"
                    });
                }

                // Require confirmation parameter
                if (!confirm)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please add '?confirm=true' to the URL to confirm database seeding. This will create sample users, classrooms, students, applications, and payments."
                    });
                }

                // Check if seed data already exists
                if (await _seedingService.SeedDataExistsAsync())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Seed data already exists. Please run /api/seed/clear first to remove existing seed data."
                    });
                }

                _logger.LogInformation("Starting database seeding...");
                var result = await _seedingService.SeedDatabaseAsync();

                if (result.Success)
                {
                    return Ok(new
                    {
                        result.Success,
                        result.Message,
                        result.Summary,
                        instructions = new
                        {
                            message = "Seed users created! Use these credentials to login:",
                            note = "All users have the same password for testing",
                            nextSteps = new[]
                            {
                                "You can now login with any of the seeded users",
                                "To upload sample files, call POST /api/seed/upload-files?confirm=true",
                                "To clear all seed data, call DELETE /api/seed/clear?confirm=true"
                            }
                        }
                    });
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database seeding");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during seeding",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Clears all seed data from database and Firebase (Development only)
        /// </summary>
        /// <returns>Success status</returns>
        [HttpDelete("clear")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ClearSeedData([FromQuery] bool confirm = false)
        {
            try
            {
                // Safety check: Warn if in Production but allow with confirmation
                if (_environment.IsProduction() && !confirm)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Clearing seed data in Production requires explicit confirmation. Add '?confirm=true' to proceed.",
                        warning = "This will delete test data from your production database!"
                    });
                }

                // Require confirmation parameter
                if (!confirm)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please add '?confirm=true' to the URL to confirm deletion of ALL seed data. This will remove users, classrooms, students, applications, and payments from both database and Firebase."
                    });
                }

                _logger.LogInformation("Clearing seed data...");
                var success = await _seedingService.ClearSeedDataAsync();

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "All seed data has been cleared successfully from database and Firebase"
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Failed to clear seed data"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing seed data");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while clearing seed data",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Uploads sample files to Firebase Storage (Development only)
        /// </summary>
        /// <returns>File upload result</returns>
        [HttpPost("upload-files")]
        [ProducesResponseType(typeof(FileSeedingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadSampleFiles([FromQuery] bool confirm = false)
        {
            try
            {
                // Safety check: Warn if in Production but allow with confirmation
                if (_environment.IsProduction() && !confirm)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "File upload seeding in Production requires explicit confirmation. Add '?confirm=true' to proceed.",
                        warning = "This will upload test files to your production Firebase Storage!"
                    });
                }

                // Require confirmation parameter
                if (!confirm)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please add '?confirm=true' to the URL to confirm file uploads. This will upload sample profile photos, student photos, birth certificates, ID verifications, and payment receipts to Firebase Storage."
                    });
                }

                _logger.LogInformation("Starting file upload seeding...");
                var fileSeeder = new FileUploadSeeder(_storageService, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<FileUploadSeeder>());
                var result = await fileSeeder.SeedFilesAsync();

                if (result.Success)
                {
                    return Ok(new
                    {
                        result.Success,
                        result.Message,
                        result.FilesUploaded,
                        SampleFiles = result.UploadedFiles.Take(5).ToList(),
                        Note = $"Showing first 5 of {result.FilesUploaded} uploaded files"
                    });
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading sample files");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during file upload",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Checks if seed data exists
        /// </summary>
        /// <returns>Status of seed data</returns>
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSeedStatus()
        {
            try
            {
                var exists = await _seedingService.SeedDataExistsAsync();

                return Ok(new
                {
                    seedDataExists = exists,
                    environment = _environment.EnvironmentName,
                    message = exists
                        ? "Seed data exists in the database"
                        : "No seed data found in the database",
                    availableEndpoints = new
                    {
                        seedDatabase = "POST /api/seed/run?confirm=true",
                        uploadFiles = "POST /api/seed/upload-files?confirm=true",
                        clearData = "DELETE /api/seed/clear?confirm=true",
                        checkStatus = "GET /api/seed/status"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking seed status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while checking seed status",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets information about seed data (users, password, etc.)
        /// </summary>
        /// <returns>Seed data information</returns>
        [HttpGet("info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetSeedInfo()
        {
            var users = SeedDataConstants.Users.Select(u => new
            {
                u.Email,
                u.Role,
                u.FirstName,
                u.LastName,
                Password = SeedDataConstants.DEFAULT_PASSWORD
            }).ToList();

            var classrooms = SeedDataConstants.Classrooms.Select(c => new
            {
                c.Name,
                c.Grade,
                c.Capacity
            }).ToList();

            return Ok(new
            {
                DefaultPassword = SeedDataConstants.DEFAULT_PASSWORD,
                Users = users,
                Classrooms = classrooms,
                TotalChildren = SeedDataConstants.Children.Count,
                TotalApplications = SeedDataConstants.Applications.Count,
                Note = "Use these credentials to test the system. All users share the same password.",
                Warning = "This information is only available in Development environment"
            });
        }
    }
}
