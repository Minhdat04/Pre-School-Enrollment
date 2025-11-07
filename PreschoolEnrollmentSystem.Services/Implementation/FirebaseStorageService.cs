using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.Services.Interfaces;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly ILogger<FirebaseStorageService> _logger;

        public FirebaseStorageService(
            IConfiguration configuration,
            ILogger<FirebaseStorageService> logger)
        {
            _logger = logger;
            _bucketName = configuration["Firebase:StorageBucket"]
                ?? throw new ArgumentNullException("Firebase:StorageBucket configuration is missing");

            try
            {
                // Initialize StorageClient using the same Firebase credentials
                var credentialPath = configuration["Firebase:CredentialPath"];
                if (string.IsNullOrEmpty(credentialPath))
                {
                    throw new ArgumentNullException("Firebase:CredentialPath configuration is missing");
                }

                // Set the GOOGLE_APPLICATION_CREDENTIALS environment variable
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
                _storageClient = StorageClient.Create();

                _logger.LogInformation("Firebase Storage initialized successfully with bucket: {BucketName}", _bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Storage");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(Stream stream, string fileName, string folderPath, string contentType)
        {
            try
            {
                if (stream == null || stream.Length == 0)
                {
                    throw new ArgumentException("File stream cannot be null or empty", nameof(stream));
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
                }

                // Sanitize file name to prevent path traversal
                fileName = Path.GetFileName(fileName);

                // Construct the full path
                var objectName = string.IsNullOrWhiteSpace(folderPath)
                    ? fileName
                    : $"{folderPath.TrimEnd('/')}/{fileName}";

                _logger.LogInformation("Uploading file to Firebase Storage: {ObjectName}", objectName);

                // Upload the file
                var uploadedObject = await _storageClient.UploadObjectAsync(
                    bucket: _bucketName,
                    objectName: objectName,
                    contentType: contentType,
                    source: stream
                );

                // Make the file publicly accessible (optional - can be controlled via storage rules)
                await _storageClient.UpdateObjectAsync(new Google.Apis.Storage.v1.Data.Object
                {
                    Bucket = _bucketName,
                    Name = objectName,
                    Acl = new List<Google.Apis.Storage.v1.Data.ObjectAccessControl>
                    {
                        new Google.Apis.Storage.v1.Data.ObjectAccessControl
                        {
                            Entity = "allUsers",
                            Role = "READER"
                        }
                    }
                });

                var publicUrl = $"https://storage.googleapis.com/{_bucketName}/{objectName}";
                _logger.LogInformation("File uploaded successfully: {PublicUrl}", publicUrl);

                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to folder {FolderPath}", fileName, folderPath);
                throw new InvalidOperationException($"Failed to upload file: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadFileFromBytesAsync(byte[] fileBytes, string fileName, string folderPath, string contentType)
        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                return await UploadFileAsync(stream, fileName, folderPath, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file from bytes {FileName} to folder {FolderPath}", fileName, folderPath);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                _logger.LogInformation("Downloading file from Firebase Storage: {FilePath}", filePath);

                var stream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(_bucketName, filePath, stream);
                stream.Position = 0;

                _logger.LogInformation("File downloaded successfully: {FilePath}", filePath);
                return stream;
            }
            catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
            {
                _logger.LogWarning("File not found in Firebase Storage: {FilePath}", filePath);
                throw new FileNotFoundException($"File not found: {filePath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to download file: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                _logger.LogInformation("Deleting file from Firebase Storage: {FilePath}", filePath);

                await _storageClient.DeleteObjectAsync(_bucketName, filePath);

                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }
            catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
            {
                _logger.LogWarning("File not found for deletion in Firebase Storage: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to delete file: {ex.Message}", ex);
            }
        }

        public async Task<string> GetSignedUrlAsync(string filePath, int expirationMinutes = 60)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                _logger.LogInformation("Generating signed URL for file: {FilePath}", filePath);

                var credentialPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (string.IsNullOrEmpty(credentialPath))
                {
                    throw new InvalidOperationException("GOOGLE_APPLICATION_CREDENTIALS environment variable is not set");
                }

                var urlSigner = UrlSigner.FromCredentialFile(credentialPath);

                var signedUrl = await urlSigner.SignAsync(
                    bucket: _bucketName,
                    objectName: filePath,
                    duration: TimeSpan.FromMinutes(expirationMinutes),
                    httpMethod: System.Net.Http.HttpMethod.Get
                );

                _logger.LogInformation("Signed URL generated successfully for file: {FilePath}", filePath);
                return signedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signed URL for file {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to generate signed URL: {ex.Message}", ex);
            }
        }

        public Task<string> GetPublicUrlAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                var publicUrl = $"https://storage.googleapis.com/{_bucketName}/{filePath}";
                _logger.LogInformation("Public URL generated for file: {FilePath} -> {PublicUrl}", filePath, publicUrl);

                return Task.FromResult(publicUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating public URL for file {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                _logger.LogDebug("Checking if file exists: {FilePath}", filePath);

                var obj = await _storageClient.GetObjectAsync(_bucketName, filePath);
                return obj != null;
            }
            catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
            {
                _logger.LogDebug("File does not exist: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to check file existence: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> ListFilesAsync(string folderPath)
        {
            try
            {
                _logger.LogInformation("Listing files in folder: {FolderPath}", folderPath);

                var files = new List<string>();
                var prefix = string.IsNullOrWhiteSpace(folderPath)
                    ? null
                    : folderPath.TrimEnd('/') + "/";

                var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);

                await foreach (var obj in objects)
                {
                    files.Add(obj.Name);
                }

                _logger.LogInformation("Found {Count} files in folder: {FolderPath}", files.Count, folderPath);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in folder {FolderPath}", folderPath);
                throw new InvalidOperationException($"Failed to list files: {ex.Message}", ex);
            }
        }
    }
}
