using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Interfaces
{
    public interface IDataSeedingService
    {
        /// <summary>
        /// Seeds the database with sample data including users, classrooms, students, applications, and payments
        /// </summary>
        /// <returns>Seeding result with details</returns>
        Task<DataSeedingResult> SeedDatabaseAsync();

        /// <summary>
        /// Clears all seed data from the database and Firebase
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> ClearSeedDataAsync();

        /// <summary>
        /// Uploads sample files to Firebase Storage
        /// </summary>
        /// <returns>File upload result with URLs</returns>
        Task<FileSeedingResult> SeedFilesAsync();

        /// <summary>
        /// Checks if seed data already exists
        /// </summary>
        /// <returns>True if seed data exists</returns>
        Task<bool> SeedDataExistsAsync();
    }

    public class DataSeedingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public SeedDataSummary Summary { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class SeedDataSummary
    {
        public int UsersCreated { get; set; }
        public int ClassroomsCreated { get; set; }
        public int ChildrenCreated { get; set; }
        public int StudentsCreated { get; set; }
        public int ApplicationsCreated { get; set; }
        public int PaymentsCreated { get; set; }
        public List<SeededUser> SeededUsers { get; set; } = new();
    }

    public class SeededUser
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FirebaseUid { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class FileSeedingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int FilesUploaded { get; set; }
        public List<string> UploadedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
