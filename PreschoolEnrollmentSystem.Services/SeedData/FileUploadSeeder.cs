using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.Services.Interfaces;
using FileSeedingResult = PreschoolEnrollmentSystem.Services.Interfaces.FileSeedingResult;

namespace PreschoolEnrollmentSystem.Services.SeedData
{
    public class FileUploadSeeder
    {
        private readonly IFirebaseStorageService _storageService;
        private readonly ILogger<FileUploadSeeder> _logger;

        public FileUploadSeeder(
            IFirebaseStorageService storageService,
            ILogger<FileUploadSeeder> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<FileSeedingResult> SeedFilesAsync()
        {
            var result = new FileSeedingResult { Success = true };

            try
            {
                _logger.LogInformation("Starting file upload seeding...");

                // 1. Upload profile photos for users
                var profilePhotos = await UploadProfilePhotosAsync();
                result.UploadedFiles.AddRange(profilePhotos);

                // 2. Upload student photos
                var studentPhotos = await UploadStudentPhotosAsync();
                result.UploadedFiles.AddRange(studentPhotos);

                // 3. Upload birth certificate images
                var birthCertificates = await UploadBirthCertificatesAsync();
                result.UploadedFiles.AddRange(birthCertificates);

                // 4. Upload sample parent ID verification
                var idVerifications = await UploadIdVerificationAsync();
                result.UploadedFiles.AddRange(idVerifications);

                // 5. Upload sample payment receipts
                var receipts = await UploadPaymentReceiptsAsync();
                result.UploadedFiles.AddRange(receipts);

                result.FilesUploaded = result.UploadedFiles.Count;
                result.Message = $"Successfully uploaded {result.FilesUploaded} files";
                _logger.LogInformation("File seeding completed. Uploaded {Count} files", result.FilesUploaded);
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

        private async Task<List<string>> UploadProfilePhotosAsync()
        {
            var uploadedFiles = new List<string>();
            var users = SeedDataConstants.Users;

            foreach (var user in users)
            {
                try
                {
                    var imageBytes = GeneratePlaceholderImage(200, 200, $"{user.FirstName[0]}{user.LastName[0]}", GetColorForRole(user.Role));
                    var fileName = $"{user.Username}_profile.png";

                    var url = await _storageService.UploadFileFromBytesAsync(
                        imageBytes,
                        fileName,
                        "users/profile-photos",
                        "image/png"
                    );

                    uploadedFiles.Add(url);
                    _logger.LogInformation("Uploaded profile photo for {Username}", user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload profile photo for {Username}", user.Username);
                }
            }

            return uploadedFiles;
        }

        private async Task<List<string>> UploadStudentPhotosAsync()
        {
            var uploadedFiles = new List<string>();
            var children = SeedDataConstants.Children;

            foreach (var child in children)
            {
                try
                {
                    var initials = GetInitials(child.FullName);
                    var imageBytes = GeneratePlaceholderImage(300, 300, initials, GetColorForGender(child.Gender));
                    var fileName = $"{SanitizeFileName(child.FullName)}_photo.png";

                    var url = await _storageService.UploadFileFromBytesAsync(
                        imageBytes,
                        fileName,
                        "students/photos",
                        "image/png"
                    );

                    uploadedFiles.Add(url);
                    _logger.LogInformation("Uploaded student photo for {StudentName}", child.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload student photo for {StudentName}", child.FullName);
                }
            }

            return uploadedFiles;
        }

        private async Task<List<string>> UploadBirthCertificatesAsync()
        {
            var uploadedFiles = new List<string>();
            var children = SeedDataConstants.Children;

            foreach (var child in children)
            {
                try
                {
                    var imageBytes = GenerateDocumentPlaceholder(400, 300, "GIẤY KHAI SINH");
                    var fileName = $"{SanitizeFileName(child.FullName)}_birth_cert.png";

                    var url = await _storageService.UploadFileFromBytesAsync(
                        imageBytes,
                        fileName,
                        "students/documents",
                        "image/png"
                    );

                    uploadedFiles.Add(url);
                    _logger.LogInformation("Uploaded birth certificate for {StudentName}", child.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload birth certificate for {StudentName}", child.FullName);
                }
            }

            return uploadedFiles;
        }

        private async Task<List<string>> UploadIdVerificationAsync()
        {
            var uploadedFiles = new List<string>();
            var parents = SeedDataConstants.Users.Where(u => u.Role == Core.Enums.UserRole.Parent);

            foreach (var parent in parents)
            {
                try
                {
                    var imageBytes = GenerateDocumentPlaceholder(350, 220, "CMND/CCCD");
                    var fileName = $"{parent.Username}_id.png";

                    var url = await _storageService.UploadFileFromBytesAsync(
                        imageBytes,
                        fileName,
                        "parents/id-verification",
                        "image/png"
                    );

                    uploadedFiles.Add(url);
                    _logger.LogInformation("Uploaded ID verification for {Username}", parent.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload ID verification for {Username}", parent.Username);
                }
            }

            return uploadedFiles;
        }

        private async Task<List<string>> UploadPaymentReceiptsAsync()
        {
            var uploadedFiles = new List<string>();

            // Upload receipts for completed payments (6 receipts based on seed data)
            for (int i = 1; i <= 6; i++)
            {
                try
                {
                    var imageBytes = GenerateDocumentPlaceholder(400, 500, $"HÓA ĐƠN #{i:D6}");
                    var fileName = $"receipt_{i:D6}.png";

                    var url = await _storageService.UploadFileFromBytesAsync(
                        imageBytes,
                        fileName,
                        "payments/receipts",
                        "image/png"
                    );

                    uploadedFiles.Add(url);
                    _logger.LogInformation("Uploaded payment receipt #{Number}", i);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload payment receipt #{Number}", i);
                }
            }

            return uploadedFiles;
        }

        private byte[] GeneratePlaceholderImage(int width, int height, string text, Color backgroundColor)
        {
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Arial", 40, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            using var bgBrush = new SolidBrush(backgroundColor);

            // Fill background
            graphics.FillRectangle(bgBrush, 0, 0, width, height);

            // Draw text in center
            var textSize = graphics.MeasureString(text, font);
            var x = (width - textSize.Width) / 2;
            var y = (height - textSize.Height) / 2;
            graphics.DrawString(text, font, brush, x, y);

            // Convert to byte array
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        private byte[] GenerateDocumentPlaceholder(int width, int height, string documentType)
        {
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            using var titleFont = new Font("Arial", 20, FontStyle.Bold);
            using var textFont = new Font("Arial", 12, FontStyle.Regular);
            using var brush = new SolidBrush(Color.Black);
            using var bgBrush = new SolidBrush(Color.White);
            using var borderPen = new Pen(Color.Gray, 2);

            // Fill background
            graphics.FillRectangle(bgBrush, 0, 0, width, height);
            graphics.DrawRectangle(borderPen, 5, 5, width - 10, height - 10);

            // Draw document type
            var titleSize = graphics.MeasureString(documentType, titleFont);
            var x = (width - titleSize.Width) / 2;
            graphics.DrawString(documentType, titleFont, brush, x, 20);

            // Draw sample text
            graphics.DrawString($"Mẫu tài liệu để kiểm tra", textFont, brush, 20, 60);
            graphics.DrawString($"Sample Document for Testing", textFont, brush, 20, 80);
            graphics.DrawString($"Ngày tạo: {DateTime.Now:dd/MM/yyyy}", textFont, brush, 20, 100);

            // Convert to byte array
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        private Color GetColorForRole(Core.Enums.UserRole role)
        {
            return role switch
            {
                Core.Enums.UserRole.Admin => Color.FromArgb(220, 53, 69),    // Red
                Core.Enums.UserRole.Staff => Color.FromArgb(0, 123, 255),    // Blue
                Core.Enums.UserRole.Teacher => Color.FromArgb(40, 167, 69),  // Green
                Core.Enums.UserRole.Parent => Color.FromArgb(108, 117, 125), // Gray
                _ => Color.FromArgb(108, 117, 125)
            };
        }

        private Color GetColorForGender(Core.Enums.Gender gender)
        {
            return gender switch
            {
                Core.Enums.Gender.Male => Color.FromArgb(0, 123, 255),    // Blue
                Core.Enums.Gender.Female => Color.FromArgb(255, 105, 180), // Pink
                _ => Color.FromArgb(255, 193, 7)                           // Yellow
            };
        }

        private string GetInitials(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            }
            return fullName.Length >= 2 ? fullName[..2].ToUpper() : fullName.ToUpper();
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove Vietnamese diacritics and special characters
            fileName = fileName.ToLower()
                .Replace(" ", "_")
                .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                .Replace("đ", "d")
                .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y");

            return fileName;
        }
    }
}
