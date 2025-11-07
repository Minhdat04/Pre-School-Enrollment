using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;
using PreschoolEnrollmentSystem.Infrastructure.Data;
using PreschoolEnrollmentSystem.Services.Interfaces;
using PreschoolEnrollmentSystem.Services.SeedData;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class DataSeedingService : IDataSeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSeedingService> _logger;
        private readonly FirebaseAuth _firebaseAuth;
        private const string SEED_MARKER_EMAIL = "admin@preschool.edu.vn";

        public DataSeedingService(
            ApplicationDbContext context,
            ILogger<DataSeedingService> logger)
        {
            _context = context;
            _logger = logger;
            _firebaseAuth = FirebaseAuth.DefaultInstance;
        }

        public async Task<bool> SeedDataExistsAsync()
        {
            return await _context.Users.AnyAsync(u => u.Email == SEED_MARKER_EMAIL);
        }

        public async Task<DataSeedingResult> SeedDatabaseAsync()
        {
            var result = new DataSeedingResult { Success = true };

            try
            {
                _logger.LogInformation("Starting database seeding process...");

                // Check if seed data already exists
                if (await SeedDataExistsAsync())
                {
                    result.Success = false;
                    result.Message = "Seed data already exists. Please clear existing seed data first.";
                    _logger.LogWarning(result.Message);
                    return result;
                }

                // Begin transaction for data integrity
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. Create Classrooms
                    _logger.LogInformation("Creating classrooms...");
                    var classrooms = await SeedClassroomsAsync();
                    result.Summary.ClassroomsCreated = classrooms.Count;

                    // 2. Create Users via Firebase
                    _logger.LogInformation("Creating users in Firebase and database...");
                    var users = await SeedUsersAsync(classrooms);
                    result.Summary.UsersCreated = users.Count;
                    result.Summary.SeededUsers = users.Values.Select(u => new SeededUser
                    {
                        Email = u.Email,
                        Password = SeedDataConstants.DEFAULT_PASSWORD,
                        Role = u.Role.ToString(),
                        FirebaseUid = u.FirebaseUid,
                        FullName = $"{u.FirstName} {u.LastName}"
                    }).ToList();

                    // 3. Create Children for Parents
                    _logger.LogInformation("Creating children...");
                    var children = await SeedChildrenAsync(users);
                    result.Summary.ChildrenCreated = children.Count;

                    // 4. Create Students from Children
                    _logger.LogInformation("Creating students...");
                    var students = await SeedStudentsAsync(children, classrooms, users);
                    result.Summary.StudentsCreated = students.Count;

                    // 5. Create Applications
                    _logger.LogInformation("Creating applications...");
                    var applications = await SeedApplicationsAsync(children, users);
                    result.Summary.ApplicationsCreated = applications.Count;

                    // 6. Create Payments
                    _logger.LogInformation("Creating payments...");
                    var payments = await SeedPaymentsAsync(applications, users);
                    result.Summary.PaymentsCreated = payments.Count;

                    // Commit transaction
                    await transaction.CommitAsync();

                    result.Message = "Database seeded successfully!";
                    _logger.LogInformation("Database seeding completed successfully");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Transaction failed: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Seeding failed: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _logger.LogError(ex, "Database seeding failed");
            }

            return result;
        }

        private async Task<Dictionary<string, Classroom>> SeedClassroomsAsync()
        {
            var classrooms = new Dictionary<string, Classroom>();

            foreach (var classroomData in SeedDataConstants.Classrooms)
            {
                var classroom = new Classroom
                {
                    Id = Guid.NewGuid(),
                    Name = classroomData.Name,
                    Capacity = classroomData.Capacity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Classrooms.Add(classroom);
                classrooms[classroomData.Grade] = classroom;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} classrooms", classrooms.Count);

            return classrooms;
        }

        private async Task<Dictionary<string, User>> SeedUsersAsync(Dictionary<string, Classroom> classrooms)
        {
            var users = new Dictionary<string, User>();
            var teacherIndex = 0;

            foreach (var userData in SeedDataConstants.Users)
            {
                try
                {
                    // Create user in Firebase
                    var userRecordArgs = new UserRecordArgs
                    {
                        Email = userData.Email,
                        Password = SeedDataConstants.DEFAULT_PASSWORD,
                        EmailVerified = true,
                        DisplayName = $"{userData.FirstName} {userData.LastName}",
                        Disabled = false
                    };

                    var firebaseUser = await _firebaseAuth.CreateUserAsync(userRecordArgs);

                    // Set custom claims for role
                    var claims = new Dictionary<string, object>
                    {
                        { "role", userData.Role.ToString() }
                    };
                    await _firebaseAuth.SetCustomUserClaimsAsync(firebaseUser.Uid, claims);

                    // Create user in database
                    Guid? classroomId = null;
                    if (userData.Role == UserRole.Teacher && teacherIndex < classrooms.Count)
                    {
                        classroomId = classrooms.Values.ElementAt(teacherIndex).Id;
                        teacherIndex++;
                    }

                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        FirebaseUid = firebaseUser.Uid,
                        Email = userData.Email,
                        Username = userData.Username,
                        FirstName = userData.FirstName,
                        LastName = userData.LastName,
                        Phone = userData.Phone,
                        PasswordHash = "FIREBASE_MANAGED",
                        Role = userData.Role,
                        Status = UserStatus.Active,
                        EmailVerified = true,
                        IsActive = true,
                        AcceptedTerms = true,
                        TermsAcceptedAt = DateTime.UtcNow,
                        ProfileCompletionPercentage = 100,
                        ClassroomId = classroomId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    users[userData.Email] = user;

                    _logger.LogInformation("Created user: {Email} ({Role}) with Firebase UID: {Uid}",
                        userData.Email, userData.Role, firebaseUser.Uid);
                }
                catch (FirebaseAuthException ex)
                {
                    _logger.LogError(ex, "Failed to create Firebase user for {Email}", userData.Email);
                    throw;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} users", users.Count);

            return users;
        }

        private async Task<Dictionary<string, Child>> SeedChildrenAsync(Dictionary<string, User> users)
        {
            var children = new Dictionary<string, Child>();

            foreach (var childData in SeedDataConstants.Children)
            {
                if (!users.TryGetValue(childData.ParentEmail, out var parent))
                {
                    _logger.LogWarning("Parent not found for email: {Email}", childData.ParentEmail);
                    continue;
                }

                var child = new Child
                {
                    Id = Guid.NewGuid(),
                    FullName = childData.FullName,
                    Birthdate = childData.Birthdate,
                    Gender = childData.Gender,
                    Address = childData.Address,
                    ParentId = parent.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Children.Add(child);
                children[childData.FullName] = child;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} children", children.Count);

            return children;
        }

        private async Task<List<Student>> SeedStudentsAsync(
            Dictionary<string, Child> children,
            Dictionary<string, Classroom> classrooms,
            Dictionary<string, User> users)
        {
            var students = new List<Student>();

            // Only create students for approved applications (we'll filter later)
            var approvedChildrenData = SeedDataConstants.Children
                .Where(c => SeedDataConstants.Applications.Any(a =>
                    a.ChildFullName == c.FullName &&
                    a.Status == ApplicationStatus.Approved))
                .ToList();

            foreach (var childData in approvedChildrenData)
            {
                if (!children.TryGetValue(childData.FullName, out var child))
                {
                    continue;
                }

                var childEntity = await _context.Children
                    .Include(c => c.Parent)
                    .FirstOrDefaultAsync(c => c.Id == child.Id);

                if (childEntity?.Parent == null)
                {
                    continue;
                }

                if (!classrooms.TryGetValue(childData.Grade, out var classroom))
                {
                    _logger.LogWarning("Classroom not found for grade: {Grade}", childData.Grade);
                    continue;
                }

                var student = new Student
                {
                    Id = Guid.NewGuid(),
                    FullName = childData.FullName,
                    Birthdate = childData.Birthdate,
                    Gender = childData.Gender,
                    Grade = childData.Grade,
                    ParentId = childEntity.ParentId,
                    ClassroomId = classroom.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);
                students.Add(student);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} students", students.Count);

            return students;
        }

        private async Task<List<Application>> SeedApplicationsAsync(
            Dictionary<string, Child> children,
            Dictionary<string, User> users)
        {
            var applications = new List<Application>();

            foreach (var appData in SeedDataConstants.Applications)
            {
                if (!children.TryGetValue(appData.ChildFullName, out var child))
                {
                    _logger.LogWarning("Child not found: {ChildName}", appData.ChildFullName);
                    continue;
                }

                var childEntity = await _context.Children
                    .Include(c => c.Parent)
                    .FirstOrDefaultAsync(c => c.Id == child.Id);

                if (childEntity?.Parent == null)
                {
                    continue;
                }

                var childData = SeedDataConstants.Children
                    .FirstOrDefault(c => c.FullName == appData.ChildFullName);

                if (childData == null)
                {
                    continue;
                }

                var application = new Application
                {
                    Id = Guid.NewGuid(),
                    StudentName = appData.ChildFullName,
                    Birthdate = childEntity.Birthdate,
                    Gender = childEntity.Gender,
                    Address = childEntity.Address,
                    Grade = childData.Grade,
                    Status = appData.Status,
                    ChildId = child.Id,
                    CreatedById = childEntity.ParentId,
                    Reason = appData.RejectionReason,
                    CreatedAt = DateTime.UtcNow.AddDays(-30), // 30 days ago
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Applications.Add(application);
                applications.Add(application);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} applications", applications.Count);

            return applications;
        }

        private async Task<List<Payment>> SeedPaymentsAsync(
            List<Application> applications,
            Dictionary<string, User> users)
        {
            var payments = new List<Payment>();
            var random = new Random();

            // Create payments for applications that need them
            foreach (var application in applications)
            {
                // Payment needed for: PaymentCompleted, Approved, or Cancelled
                if (application.Status == ApplicationStatus.PaymentPending)
                {
                    continue; // No payment yet
                }

                var appWithCreator = await _context.Applications
                    .Include(a => a.CreatedBy)
                    .FirstOrDefaultAsync(a => a.Id == application.Id);

                if (appWithCreator?.CreatedBy == null)
                {
                    continue;
                }

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = application.Id,
                    MadeById = appWithCreator.CreatedById,
                    Type = PaymentType.Payment,
                    vnp_Amount = 1000000, // 1,000,000 VND (enrollment fee)
                    vnp_TxnRef = $"SEED{DateTime.UtcNow.Ticks}{random.Next(1000, 9999)}",
                    vnp_OrderInfo = $"Thanh toán hồ sơ nhập học cho {application.StudentName}",
                    vnp_TransactionNo = $"{DateTime.UtcNow:yyyyMMdd}{random.Next(100000, 999999)}",
                    vnp_BankCode = "NCB",
                    vnp_CardType = "ATM",
                    vnp_PayDate = DateTime.UtcNow.AddDays(-25).ToString("yyyyMMddHHmmss"),
                    vnp_ResponseCode = "00", // Success
                    vnp_TransactionStatus = "00", // Success
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow.AddDays(-25)
                };

                _context.Payments.Add(payment);
                payments.Add(payment);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} payments", payments.Count);

            return payments;
        }

        public async Task<bool> ClearSeedDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting to clear seed data...");

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Get all seed user emails
                    var seedEmails = SeedDataConstants.Users.Select(u => u.Email).ToList();

                    // Get seed users from database
                    var seedUsers = await _context.Users
                        .Where(u => seedEmails.Contains(u.Email))
                        .ToListAsync();

                    if (!seedUsers.Any())
                    {
                        _logger.LogInformation("No seed data found to clear");
                        return true;
                    }

                    var seedUserIds = seedUsers.Select(u => u.Id).ToList();
                    var firebaseUids = seedUsers.Select(u => u.FirebaseUid).ToList();

                    // Delete related data (in correct order due to foreign keys)
                    var payments = await _context.Payments
                        .Where(p => seedUserIds.Contains(p.MadeById))
                        .ToListAsync();
                    _context.Payments.RemoveRange(payments);

                    var applications = await _context.Applications
                        .Where(a => seedUserIds.Contains(a.CreatedById))
                        .ToListAsync();
                    _context.Applications.RemoveRange(applications);

                    var students = await _context.Students
                        .Where(s => seedUserIds.Contains(s.ParentId))
                        .ToListAsync();
                    _context.Students.RemoveRange(students);

                    var children = await _context.Children
                        .Where(c => seedUserIds.Contains(c.ParentId))
                        .ToListAsync();
                    _context.Children.RemoveRange(children);

                    // Delete classrooms (check if they have no other students)
                    var classroomNames = SeedDataConstants.Classrooms.Select(c => c.Name).ToList();
                    var classrooms = await _context.Classrooms
                        .Where(c => classroomNames.Contains(c.Name))
                        .ToListAsync();
                    _context.Classrooms.RemoveRange(classrooms);

                    // Delete users from database
                    _context.Users.RemoveRange(seedUsers);

                    await _context.SaveChangesAsync();

                    // Delete users from Firebase
                    foreach (var firebaseUid in firebaseUids)
                    {
                        try
                        {
                            await _firebaseAuth.DeleteUserAsync(firebaseUid);
                            _logger.LogInformation("Deleted Firebase user: {Uid}", firebaseUid);
                        }
                        catch (FirebaseAuthException ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete Firebase user {Uid}, may not exist", firebaseUid);
                        }
                    }

                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully cleared all seed data");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to clear seed data, transaction rolled back");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing seed data");
                return false;
            }
        }

        public async Task<Interfaces.FileSeedingResult> SeedFilesAsync()
        {
            var result = new Interfaces.FileSeedingResult { Success = true };

            try
            {
                _logger.LogInformation("File seeding not yet implemented. This will be added in the file upload utility.");
                result.Message = "File seeding requires Firebase Storage to be enabled and configured. Please run the file upload utility separately.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"File seeding failed: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _logger.LogError(ex, "File seeding failed");
            }

            return result;
        }
    }
}
