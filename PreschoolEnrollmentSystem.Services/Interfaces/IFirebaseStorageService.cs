using System;
using System.IO;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Interfaces
{
    public interface IFirebaseStorageService
    {
        /// <summary>
        /// Uploads a file to Firebase Storage
        /// </summary>
        /// <param name="stream">File stream to upload</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="folderPath">Folder path in storage bucket (e.g., "students/photos")</param>
        /// <param name="contentType">MIME type of the file (e.g., "image/jpeg")</param>
        /// <returns>URL of the uploaded file</returns>
        Task<string> UploadFileAsync(Stream stream, string fileName, string folderPath, string contentType);

        /// <summary>
        /// Downloads a file from Firebase Storage
        /// </summary>
        /// <param name="filePath">Full path to the file in storage bucket</param>
        /// <returns>File stream</returns>
        Task<Stream> DownloadFileAsync(string filePath);

        /// <summary>
        /// Deletes a file from Firebase Storage
        /// </summary>
        /// <param name="filePath">Full path to the file in storage bucket</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Gets a signed URL for temporary access to a file
        /// </summary>
        /// <param name="filePath">Full path to the file in storage bucket</param>
        /// <param name="expirationMinutes">URL expiration time in minutes (default 60)</param>
        /// <returns>Signed URL</returns>
        Task<string> GetSignedUrlAsync(string filePath, int expirationMinutes = 60);

        /// <summary>
        /// Gets the public download URL for a file
        /// </summary>
        /// <param name="filePath">Full path to the file in storage bucket</param>
        /// <returns>Public URL</returns>
        Task<string> GetPublicUrlAsync(string filePath);

        /// <summary>
        /// Checks if a file exists in Firebase Storage
        /// </summary>
        /// <param name="filePath">Full path to the file in storage bucket</param>
        /// <returns>True if file exists</returns>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Lists all files in a specific folder
        /// </summary>
        /// <param name="folderPath">Folder path in storage bucket</param>
        /// <returns>List of file paths</returns>
        Task<System.Collections.Generic.List<string>> ListFilesAsync(string folderPath);

        /// <summary>
        /// Uploads a file from a byte array
        /// </summary>
        /// <param name="fileBytes">File content as byte array</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="folderPath">Folder path in storage bucket</param>
        /// <param name="contentType">MIME type of the file</param>
        /// <returns>URL of the uploaded file</returns>
        Task<string> UploadFileFromBytesAsync(byte[] fileBytes, string fileName, string folderPath, string contentType);
    }
}
