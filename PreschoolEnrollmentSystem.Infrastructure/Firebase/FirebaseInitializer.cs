using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace PreschoolEnrollmentSystem.Infrastructure.Firebase
{
    /// <summary>
    /// Initializes Firebase Admin SDK
    /// Why: We need to configure Firebase once when the application starts
    /// </summary>
    public static class FirebaseInitializer
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initialize Firebase Admin SDK with service account credentials
        /// </summary>
        /// <param name="configuration">App configuration to read Firebase settings</param>
        public static void Initialize(IConfiguration configuration)
        {
            // Why: Thread-safe initialization to prevent multiple initializations
            lock (_lock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    var credentialPath = configuration["Firebase:CredentialPath"];

                    // Try multiple paths (for local dev and Azure)
                    var possiblePaths = new[]
                    {
                        credentialPath, // Original path from config
                        Path.Combine(AppContext.BaseDirectory, credentialPath ?? ""), // Relative to app directory
                        Path.Combine(Directory.GetCurrentDirectory(), credentialPath ?? ""), // Current directory
                        Path.Combine(AppContext.BaseDirectory, "firebase-adminsdk.json"), // Default in app directory
                        "D:\\home\\site\\wwwroot\\firebase-adminsdk.json" // Azure App Service path
                    };

                    string? foundPath = null;
                    foreach (var path in possiblePaths)
                    {
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            foundPath = path;
                            Console.WriteLine($"✓ Found Firebase credential file at: {path}");
                            break;
                        }
                    }

                    // Why: Check if credential file exists before trying to use it
                    if (string.IsNullOrEmpty(foundPath))
                    {
                        var searchedPaths = string.Join(", ", possiblePaths.Where(p => !string.IsNullOrEmpty(p)));
                        throw new FileNotFoundException(
                            $"Firebase credential file not found. Searched paths: {searchedPaths}. " +
                            "Download your service account key from Firebase Console and " +
                            "update the path in appsettings.json");
                    }

                    credentialPath = foundPath;

                    // Why: Initialize Firebase with the service account credentials
                    // This allows our server to verify tokens and access Firebase services
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(credentialPath),
                        ProjectId = configuration["Firebase:ProjectId"]
                    });

                    _isInitialized = true;
                    Console.WriteLine("✓ Firebase Admin SDK initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Firebase initialization failed: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Check if Firebase is initialized
        /// </summary>
        public static bool IsInitialized => _isInitialized;
    }
}