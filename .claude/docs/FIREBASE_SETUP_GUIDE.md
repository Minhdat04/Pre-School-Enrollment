# Firebase Setup Guide for Preschool Enrollment System

## What Was Added

### 1. Firebase Storage Service
**Files Created:**
- `PreschoolEnrollmentSystem.Services/Interfaces/IFirebaseStorageService.cs`
- `PreschoolEnrollmentSystem.Services/Implementation/FirebaseStorageService.cs`

**Features:**
- Upload files to Firebase Storage
- Download files
- Delete files
- Get signed URLs (temporary access)
- Get public URLs
- Check file existence
- List files in folders
- Upload from byte arrays or streams

**Use Cases:**
- Student photos
- Parent ID verification documents
- Application documents
- Payment receipts
- User profile pictures
- School announcements/media
- Class materials

### 2. Firebase Cloud Messaging (FCM) Service
**Files Created:**
- `PreschoolEnrollmentSystem.Services/Interfaces/IFirebaseNotificationService.cs`
- `PreschoolEnrollmentSystem.Services/Implementation/FirebaseNotificationService.cs`

**Features:**
- Send notifications to single device
- Send to multiple devices (batch)
- Send to topics (broadcast)
- Subscribe/unsubscribe devices to topics
- High-priority urgent notifications
- Data-only messages (background processing)

**Use Cases:**
- Application status updates
- Payment reminders
- School announcements
- Emergency alerts
- Class schedule changes

### 3. Security Rules
**Files Created:**
- `storage.rules` - Firebase Storage security rules
- `firebase.json` - Firebase project configuration
- `.firebaserc` - Firebase project aliases

**Security Features:**
- Role-based access control (Admin, Staff, Teacher, Parent)
- File size limits (5MB max)
- File type restrictions (images, documents)
- User-specific access for personal files
- Staff/admin elevated permissions

### 4. Configuration Security
**Changes Made:**
- Moved Firebase credentials to User Secrets
- Removed sensitive data from `appsettings.json`
- Set up environment variable support

---

## Next Steps to Complete Firebase Integration

### Step 1: Enable Firebase Storage in Console

1. Go to [Firebase Console](https://console.firebase.google.com/project/preschoolenrollment-bad16)
2. Click on **Storage** in the left sidebar
3. Click **Get Started**
4. Choose **Start in production mode** (our security rules will handle access)
5. Select a location closest to your users (e.g., `us-central1` or `asia-southeast1`)
6. Click **Done**

### Step 2: Deploy Storage Security Rules

```bash
# Install Firebase CLI (if not already installed)
npm install -g firebase-tools

# Login to Firebase
firebase login

# Deploy storage rules from the project root directory
firebase deploy --only storage
```

**Expected Output:**
```
✔ Deploy complete!

Project Console: https://console.firebase.google.com/project/preschoolenrollment-bad16/overview
```

### Step 3: Verify Storage Bucket Name

The storage bucket should be: `preschoolenrollment-bad16.appspot.com`

Verify it's already set in User Secrets:
```bash
cd PreschoolEnrollmentSystem.API
dotnet user-secrets list
```

You should see:
```
Firebase:StorageBucket = preschoolenrollment-bad16.appspot.com
```

### Step 4: Enable Cloud Messaging (Optional, but Recommended)

1. In Firebase Console, go to **Cloud Messaging**
2. The Server key is automatically configured via your service account
3. For mobile apps (iOS/Android):
   - iOS: Add GoogleService-Info.plist to your app
   - Android: Add google-services.json to your app
   - Register for push notifications in app code

### Step 5: Test Firebase Storage (Optional)

Create a test controller to verify storage is working:

```csharp
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IFirebaseStorageService _storageService;

    public TestController(IFirebaseStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload-test")]
    public async Task<IActionResult> TestUpload(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadFileAsync(
                stream,
                file.FileName,
                "test",
                file.ContentType
            );
            return Ok(new { success = true, url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
```

### Step 6: Create Storage and Notification Controllers

You'll want to create dedicated controllers for file uploads and notifications. Examples:

**StorageController.cs:**
```csharp
[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private readonly IFirebaseStorageService _storageService;

    [HttpPost("student-photo")]
    [Authorize(Roles = "Staff,Teacher,Admin")]
    public async Task<IActionResult> UploadStudentPhoto(string studentId, IFormFile photo)
    {
        var url = await _storageService.UploadFileAsync(
            photo.OpenReadStream(),
            $"{studentId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(photo.FileName)}",
            $"students/photos/{studentId}",
            photo.ContentType
        );
        return Ok(new { photoUrl = url });
    }

    [HttpPost("parent-id")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> UploadParentId(IFormFile idDocument)
    {
        var parentId = User.FindFirst("user_id")?.Value;
        var url = await _storageService.UploadFileAsync(
            idDocument.OpenReadStream(),
            $"id_{DateTime.UtcNow.Ticks}{Path.GetExtension(idDocument.FileName)}",
            $"parents/id-verification/{parentId}",
            idDocument.ContentType
        );
        return Ok(new { documentUrl = url });
    }
}
```

**NotificationController.cs:**
```csharp
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly IFirebaseNotificationService _notificationService;

    [HttpPost("send")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> SendNotification(
        [FromBody] SendNotificationRequest request)
    {
        var messageId = await _notificationService.SendNotificationAsync(
            request.DeviceToken,
            request.Title,
            request.Body,
            request.Data
        );
        return Ok(new { messageId });
    }

    [HttpPost("broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BroadcastToTopic(
        [FromBody] BroadcastRequest request)
    {
        var messageId = await _notificationService.SendToTopicAsync(
            request.Topic,
            request.Title,
            request.Body,
            request.Data
        );
        return Ok(new { messageId });
    }
}
```

---

## Important Notes

### File Upload Best Practices

1. **Validate file types** before uploading
2. **Sanitize file names** to prevent directory traversal attacks
3. **Limit file sizes** (currently set to 5MB in security rules)
4. **Use unique file names** to prevent overwrites
5. **Store file references** in MySQL database for easy lookup

### Notification Best Practices

1. **Store device tokens** in your database when users register
2. **Update tokens** when they change (mobile apps should send new tokens)
3. **Remove invalid tokens** when FCM reports them as expired
4. **Use topics** for broadcast messages (all parents, all staff, etc.)
5. **Include data payload** for deep linking in mobile apps

### Security Considerations

1. **Never commit** `firebase-adminsdk.json` to version control
2. **Use User Secrets** for development, environment variables for production
3. **Deploy security rules** before going to production
4. **Test access control** to ensure rules work correctly
5. **Monitor storage usage** in Firebase Console

### Monitoring and Debugging

**Firebase Console Resources:**
- Storage Browser: https://console.firebase.google.com/project/preschoolenrollment-bad16/storage
- Cloud Messaging: https://console.firebase.google.com/project/preschoolenrollment-bad16/messaging
- Usage & Billing: https://console.firebase.google.com/project/preschoolenrollment-bad16/usage

**Check Logs:**
```bash
# View application logs
cd PreschoolEnrollmentSystem.API
dotnet run

# Firebase emulator logs (if using emulators)
firebase emulators:start
```

---

## Troubleshooting

### Storage Upload Fails

**Error:** "Permission denied"
```
Solution:
1. Ensure storage.rules are deployed
2. Check user has valid Firebase auth token
3. Verify user role matches security rules
4. Check storage bucket is enabled in Firebase Console
```

**Error:** "Storage bucket not found"
```
Solution:
1. Verify Firebase:StorageBucket in User Secrets
2. Enable Storage in Firebase Console
3. Check GOOGLE_APPLICATION_CREDENTIALS environment variable is set
```

### Notification Send Fails

**Error:** "Invalid registration token"
```
Solution:
1. Device token expired - request new token from mobile app
2. Token format is incorrect - verify FCM token from mobile app
3. Remove invalid tokens from database
```

**Error:** "Authentication error"
```
Solution:
1. Check firebase-adminsdk.json exists and is valid
2. Verify FirebaseApp is initialized in Program.cs
3. Ensure service account has Cloud Messaging permissions
```

### Build Warnings

The build may show nullable reference warnings. These are safe to ignore for now, but should be addressed in production:
- Add null checks where needed
- Use nullable reference types properly
- Handle edge cases

---

## Production Deployment Checklist

- [ ] Firebase Storage enabled in Console
- [ ] Storage security rules deployed
- [ ] Cloud Messaging enabled
- [ ] User Secrets configured on all development machines
- [ ] Environment variables set on production server
- [ ] Database updated to store file URLs and device tokens
- [ ] Storage and Notification controllers implemented
- [ ] File upload validation implemented
- [ ] Error handling and logging configured
- [ ] Mobile app FCM tokens registered
- [ ] Testing completed for all storage operations
- [ ] Testing completed for all notification scenarios

---

## Summary

You now have:
✅ Firebase Storage service fully implemented
✅ Firebase Cloud Messaging service fully implemented
✅ Security rules configured
✅ User Secrets protecting sensitive data
✅ Services registered in DI container
✅ Complete documentation

Next: Deploy storage rules and start using the services in your controllers!
