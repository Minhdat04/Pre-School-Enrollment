# Pre-School-Enrollment

A comprehensive preschool enrollment management system built with ASP.NET Core Web API and Firebase.

## Features

- **Firebase Authentication** - User registration, login, password management, email verification
- **Firebase Storage** - File uploads for student photos, documents, ID verification, payment receipts
- **Firebase Cloud Messaging (FCM)** - Push notifications for application updates, payment reminders, announcements
- **MySQL Database** - Primary data storage with Entity Framework Core
- **Role-Based Access Control** - Admin, Staff, Teacher, Parent roles
- **RESTful API** - Complete API documentation with Swagger

## Tech Stack

- **Backend**: ASP.NET Core 8.0 Web API
- **Database**: MySQL 8.0+
- **Authentication**: Firebase Authentication (Admin SDK)
- **File Storage**: Firebase Cloud Storage
- **Push Notifications**: Firebase Cloud Messaging
- **ORM**: Entity Framework Core
- **API Documentation**: Swagger/OpenAPI

## Prerequisites

- .NET 8.0 SDK
- MySQL 8.0 or higher
- Firebase Project (with Authentication, Storage, and Cloud Messaging enabled)
- Node.js (optional, for Firebase CLI)

## Firebase Setup

### 1. Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Add project" and follow the setup wizard
3. Enable the following services:
   - **Authentication** (Email/Password provider)
   - **Cloud Storage**
   - **Cloud Messaging**

### 2. Generate Service Account Key

1. In Firebase Console, go to **Project Settings** > **Service Accounts**
2. Click **Generate New Private Key**
3. Download the JSON file and rename it to `firebase-adminsdk.json`
4. Place it in the `PreschoolEnrollmentSystem.API` directory
5. **IMPORTANT**: This file is already in `.gitignore` - never commit it to version control

### 3. Get Firebase Configuration

From Firebase Console > Project Settings > General:
- Copy your **Project ID** (e.g., `preschoolenrollment-bad16`)
- Copy your **Web API Key** (found under "Web API Key")
- Your **Storage Bucket** will be `{projectId}.appspot.com`

### 4. Configure User Secrets

**IMPORTANT**: Never store Firebase credentials in `appsettings.json`. Use User Secrets instead.

```bash
# Navigate to the API project
cd PreschoolEnrollmentSystem.API

# Initialize User Secrets
dotnet user-secrets init

# Set Firebase configuration
dotnet user-secrets set "Firebase:ProjectId" "your-project-id"
dotnet user-secrets set "Firebase:CredentialPath" "firebase-adminsdk.json"
dotnet user-secrets set "Firebase:ApiKey" "your-web-api-key"
dotnet user-secrets set "Firebase:StorageBucket" "your-project-id.appspot.com"
```

### 5. Deploy Firebase Security Rules

```bash
# Install Firebase CLI (if not already installed)
npm install -g firebase-tools

# Login to Firebase
firebase login

# Deploy storage rules
firebase deploy --only storage
```

### 6. Enable Firebase Storage

1. In Firebase Console, go to **Storage**
2. Click **Get Started**
3. Choose **Start in production mode** (our security rules will handle access control)
4. Select a location (choose closest to your users)

### 7. Enable Cloud Messaging

1. In Firebase Console, go to **Cloud Messaging**
2. Note: Server key is automatically configured via service account
3. For mobile apps, you'll need to configure platform-specific settings (iOS/Android)

## Database Setup

### 1. Create MySQL Database

```sql
CREATE DATABASE PreschoolEnrollmentDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 2. Configure Connection String

Add to User Secrets (recommended) or `appsettings.json`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=PreschoolEnrollmentDB;User=root;Password=your-password;"
```

### 3. Run Migrations

```bash
cd PreschoolEnrollmentSystem.API
dotnet ef database update
```

## Installation & Running

### 1. Clone Repository

```bash
git clone <repository-url>
cd Pre-School-Enrollment
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build Project

```bash
dotnet build
```

### 4. Run Application

```bash
cd PreschoolEnrollmentSystem.API
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5001`
- Swagger UI: `https://localhost:7001/swagger`

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh-token` - Refresh JWT token
- `POST /api/auth/send-password-reset` - Send password reset email
- `POST /api/auth/confirm-password-reset` - Confirm password reset
- `POST /api/auth/change-password` - Change password
- `POST /api/auth/send-email-verification` - Send verification email
- `POST /api/auth/verify-email` - Verify email

### Users
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Storage (Coming Soon)
- `POST /api/storage/upload` - Upload file
- `GET /api/storage/download/{path}` - Download file
- `DELETE /api/storage/delete/{path}` - Delete file

### Notifications (Coming Soon)
- `POST /api/notifications/send` - Send notification
- `POST /api/notifications/send-topic` - Send to topic
- `POST /api/notifications/subscribe` - Subscribe to topic

See Swagger UI for complete API documentation.

## Firebase Services Usage

### Firebase Storage Service

```csharp
public class ExampleController : ControllerBase
{
    private readonly IFirebaseStorageService _storageService;

    public ExampleController(IFirebaseStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var url = await _storageService.UploadFileAsync(
            stream,
            file.FileName,
            "students/photos",
            file.ContentType
        );
        return Ok(new { url });
    }
}
```

### Firebase Notification Service

```csharp
public class NotificationController : ControllerBase
{
    private readonly IFirebaseNotificationService _notificationService;

    public NotificationController(IFirebaseNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification(string deviceToken)
    {
        var messageId = await _notificationService.SendNotificationAsync(
            deviceToken,
            "Application Approved",
            "Your preschool application has been approved!",
            new Dictionary<string, string>
            {
                { "type", "application" },
                { "action", "approved" }
            }
        );
        return Ok(new { messageId });
    }

    [HttpPost("send-topic")]
    public async Task<IActionResult> SendToTopic()
    {
        var messageId = await _notificationService.SendToTopicAsync(
            "all-parents",
            "School Closure",
            "School will be closed tomorrow due to weather conditions."
        );
        return Ok(new { messageId });
    }
}
```

## Security

### Firebase Storage Rules

The `storage.rules` file defines access control for Firebase Storage:

- **Students photos/documents**: Staff can upload/delete, authenticated users can read
- **Parent ID verification**: Parents upload their own, staff can access all
- **Payment receipts**: Staff can upload, authenticated users can read
- **User profile photos**: Users manage their own, all authenticated users can read

### Authentication

All API endpoints (except public ones) require Firebase JWT token:

```bash
# Include in Authorization header
Authorization: Bearer <firebase-id-token>
```

### User Secrets

Sensitive configuration is stored in User Secrets (development) or environment variables (production):
- Firebase credentials
- Database connection strings
- API keys

## Project Structure

```
PreschoolEnrollmentSystem/
├── PreschoolEnrollmentSystem.API/          # Web API layer
│   ├── Controllers/                        # API controllers
│   ├── Middleware/                         # Custom middleware (Firebase auth)
│   └── firebase-adminsdk.json             # Firebase service account (gitignored)
├── PreschoolEnrollmentSystem.Core/         # Domain entities & DTOs
│   ├── Entities/                          # Database entities
│   ├── DTOs/                              # Data Transfer Objects
│   └── Enums/                             # Enumerations
├── PreschoolEnrollmentSystem.Infrastructure/ # Data access layer
│   ├── Data/                              # EF Core DbContext
│   ├── Repositories/                      # Repository pattern
│   └── Firebase/                          # Firebase initialization
├── PreschoolEnrollmentSystem.Services/     # Business logic layer
│   ├── Interfaces/                        # Service interfaces
│   │   ├── IAuthService.cs
│   │   ├── IFirebaseStorageService.cs
│   │   └── IFirebaseNotificationService.cs
│   └── Implementation/                    # Service implementations
│       ├── FirebaseAuthService.cs
│       ├── FirebaseStorageService.cs
│       └── FirebaseNotificationService.cs
├── firebase.json                          # Firebase project configuration
├── .firebaserc                           # Firebase project aliases
└── storage.rules                         # Firebase Storage security rules
```

## Testing with Firebase Emulator (Optional)

For local development without hitting production Firebase:

```bash
# Install Firebase CLI
npm install -g firebase-tools

# Start emulators
firebase emulators:start

# Emulator UI available at http://localhost:4000
```

## Troubleshooting

### Firebase Authentication Errors

**Error**: "Failed to initialize Firebase"
- **Solution**: Ensure `firebase-adminsdk.json` exists in the API directory
- Check that User Secrets are configured correctly

### Storage Upload Errors

**Error**: "Permission denied"
- **Solution**: Deploy storage.rules using `firebase deploy --only storage`
- Verify user has proper authentication token

### Cloud Messaging Errors

**Error**: "Invalid device token"
- **Solution**: Ensure FCM token is obtained from mobile app correctly
- Tokens expire - implement token refresh logic in mobile app

### Build Errors

**Error**: "Package not found"
- **Solution**: Run `dotnet restore` to restore NuGet packages

## Firebase Console Resources

- **Authentication Users**: `https://console.firebase.google.com/project/your-project-id/authentication/users`
- **Storage Browser**: `https://console.firebase.google.com/project/your-project-id/storage`
- **Cloud Messaging**: `https://console.firebase.google.com/project/your-project-id/messaging`

## Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues and questions:
- Create an issue in the repository
- Contact the development team

## Acknowledgments

- Firebase for authentication, storage, and messaging services
- ASP.NET Core team for the excellent framework
- MySQL for reliable database services
