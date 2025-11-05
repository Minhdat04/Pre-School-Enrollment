# Swagger UI Testing Guide
## Complete Guide to Testing Your Preschool Enrollment System API

---

## Table of Contents
1. [Getting Started](#getting-started)
2. [Authentication Endpoints](#authentication-endpoints)
3. [Using Bearer Token Authorization](#using-bearer-token-authorization)
4. [Complete Testing Workflows](#complete-testing-workflows)
5. [Testing All User Roles](#testing-all-user-roles)
6. [Troubleshooting](#troubleshooting)
7. [Verifying Data in MySQL](#verifying-data-in-mysql)

---

## Getting Started

### Step 1: Access Swagger UI

1. Make sure your API is running (you should see the server logs)
2. Open your web browser
3. Navigate to: **http://localhost:5098/swagger**
4. You should see the Swagger UI interface with all your API endpoints

### Step 2: Understanding the Swagger Interface

**Sections you'll see:**
- **Auth** - Authentication endpoints (green POST/GET buttons)
- **Schemas** - Data models at the bottom

**Color Codes:**
- 🟢 **Green (GET)** - Retrieve data
- 🔵 **Blue (POST)** - Create or send data
- 🟡 **Yellow (PUT)** - Update data
- 🔴 **Red (DELETE)** - Delete data

### Step 3: How to Use an Endpoint

1. Click on any endpoint to expand it
2. Click **"Try it out"** button (top right of the expanded section)
3. Fill in the request body or parameters
4. Click **"Execute"** button
5. Scroll down to see the response

---

## Authentication Endpoints

### 1. POST /api/Auth/register
**Purpose:** Create a new user account

**When to use:**
- Creating a new Parent account
- Creating Staff, Teacher, or Admin accounts

**Steps:**

1. Expand **POST /api/Auth/register**
2. Click **"Try it out"**
3. Replace the JSON with:

```json
{
  "email": "parent@example.com",
  "password": "SecurePass123@",
  "confirmPassword": "SecurePass123@",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Parent",
  "phoneNumber": "+84123456789",
  "acceptTerms": true
}
```

**Field Explanations:**
- `email` - Valid email address (must be unique)
- `password` - Must include: uppercase, lowercase, number, special character
- `confirmPassword` - Must match password exactly
- `firstName` - User's first name
- `lastName` - User's last name
- `role` - Must be one of: `"Parent"`, `"Staff"`, `"Teacher"`, `"Admin"`
- `phoneNumber` - International format (start with + and country code)
- `acceptTerms` - Must be `true`

4. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Registration successful. Please check your email to verify your account.",
  "data": {
    "idToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "",
    "expiresAt": "2025-11-05T07:39:14Z",
    "userId": "32c1d7c7-3337-423d-b7d8-0ed35c3c7d2b",
    "firebaseUid": "rtLFfjqb7ZO2M1nS1BWKvSjgrOp1",
    "email": "parent@example.com",
    "emailVerified": false,
    "fullName": "John Doe",
    "role": "Parent",
    "isActive": true,
    "profileCompletionPercentage": 60,
    "canEnroll": false
  }
}
```

**What Happens:**
1. Creates user in Firebase Authentication
2. Creates user record in MySQL database
3. Sends verification email (if email service configured)
4. Returns Firebase ID token for immediate use

**Common Errors:**

| Error | Cause | Solution |
|-------|-------|----------|
| 400 Bad Request - "Email already exists" | Email is already registered | Use a different email |
| 400 Bad Request - "Password must contain..." | Password too weak | Use stronger password with all required characters |
| 400 Bad Request - "Phone number must be in international format" | Missing + or country code | Add `+` and country code (e.g., `+84`) |
| 400 Bad Request - "AcceptTerms required" | acceptTerms is false or missing | Set to `true` |

**Tips:**
- Save the `idToken` - you'll need it for protected endpoints
- Save the `userId` to track this user
- Check MySQL to see the new user record

---

### 2. POST /api/Auth/login
**Purpose:** Authenticate existing user and get access token

**When to use:**
- After registration
- When you need a new token
- Testing authentication flow

**Steps:**

1. Expand **POST /api/Auth/login**
2. Click **"Try it out"**
3. Enter credentials:

```json
{
  "email": "parent@example.com",
  "password": "SecurePass123@"
}
```

4. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjU0NTEz...",
    "refreshToken": "AMf-vBzurhSU-ba2BTvSdfZ5_BpVVg...",
    "expiresAt": "2025-11-05T07:39:30Z",
    "userId": "32c1d7c7-3337-423d-b7d8-0ed35c3c7d2b",
    "firebaseUid": "rtLFfjqb7ZO2M1nS1BWKvSjgrOp1",
    "email": "parent@example.com",
    "emailVerified": false,
    "fullName": "John Doe",
    "role": "Parent",
    "isActive": true,
    "profileCompletionPercentage": 78,
    "canEnroll": false
  }
}
```

**What Happens:**
1. Verifies credentials with Firebase
2. Fetches user profile from MySQL
3. Updates last login timestamp
4. Recalculates profile completion percentage
5. Returns new ID token

**Common Errors:**

| Error | Cause | Solution |
|-------|-------|----------|
| 400 Bad Request - "Invalid email or password" | Wrong credentials | Check email and password |
| 400 Bad Request - "User profile not found" | User exists in Firebase but not in database | Contact support or re-register |
| 401 Unauthorized | Firebase authentication failed | Verify email and password are correct |

**Important:**
- Copy the `idToken` - you'll need it for the next steps
- Token expires in 1 hour (check `expiresAt`)
- Use `refreshToken` to get a new token without logging in again

---

### 3. GET /api/Auth/profile
**Purpose:** Get current user's profile information

**Authentication Required:** ✅ Yes (Bearer Token)

**When to use:**
- After login to verify user data
- To check profile completion percentage
- To display user information in UI

**Steps:**

1. **First, authorize in Swagger** (see [Using Bearer Token Authorization](#using-bearer-token-authorization))
2. Expand **GET /api/Auth/profile**
3. Click **"Try it out"**
4. Click **"Execute"** (no request body needed)

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Profile retrieved successfully",
  "data": {
    "userId": "32c1d7c7-3337-423d-b7d8-0ed35c3c7d2b",
    "firebaseUid": "rtLFfjqb7ZO2M1nS1BWKvSjgrOp1",
    "email": "parent@example.com",
    "emailVerified": false,
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "phoneNumber": "+84123456789",
    "phoneVerified": false,
    "role": "Parent",
    "isActive": true,
    "profileCompletionPercentage": 78,
    "lastLoginAt": "2025-11-05T06:39:30Z",
    "createdAt": "2025-11-05T06:32:54Z"
  }
}
```

**Common Errors:**

| Error | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | No Bearer token provided | Click "Authorize" and enter token |
| 401 Unauthorized | Token expired | Login again to get new token |
| 401 Unauthorized | Invalid token format | Ensure token starts with "Bearer " |

---

### 4. POST /api/Auth/logout
**Purpose:** Log out current user and invalidate token

**Authentication Required:** ✅ Yes (Bearer Token)

**Steps:**

1. Make sure you're authorized with a Bearer token
2. Expand **POST /api/Auth/logout**
3. Click **"Try it out"**
4. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Logout successful",
  "data": null
}
```

**What Happens:**
- Revokes Firebase refresh token
- Invalidates current session
- User must login again to access protected endpoints

---

### 5. POST /api/Auth/change-password
**Purpose:** Change password for currently logged-in user

**Authentication Required:** ✅ Yes (Bearer Token)

**Steps:**

1. Make sure you're authorized
2. Expand **POST /api/Auth/change-password**
3. Click **"Try it out"**
4. Enter:

```json
{
  "currentPassword": "SecurePass123@",
  "newPassword": "NewSecurePass456@",
  "confirmNewPassword": "NewSecurePass456@"
}
```

5. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Password changed successfully",
  "data": null
}
```

**Common Errors:**

| Error | Cause | Solution |
|-------|-------|----------|
| 400 Bad Request - "Current password is incorrect" | Wrong current password | Verify current password |
| 400 Bad Request - "New passwords do not match" | newPassword ≠ confirmNewPassword | Make sure they match |
| 400 Bad Request - "New password must be different" | New password same as current | Use a different password |

---

### 6. POST /api/Auth/reset-password
**Purpose:** Request a password reset email

**Authentication Required:** ❌ No

**When to use:**
- User forgot password
- Testing password reset flow

**Steps:**

1. Expand **POST /api/Auth/reset-password**
2. Click **"Try it out"**
3. Enter:

```json
{
  "email": "parent@example.com"
}
```

4. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Password reset email sent. Please check your inbox.",
  "data": null
}
```

**What Happens:**
1. Generates password reset token
2. Sends email with reset link
3. Link expires in 1 hour

**Note:** Requires email service to be configured

---

### 7. POST /api/Auth/confirm-reset-password
**Purpose:** Complete password reset with token from email

**Authentication Required:** ❌ No

**Steps:**

1. Get the reset token from the email
2. Expand **POST /api/Auth/confirm-reset-password**
3. Click **"Try it out"**
4. Enter:

```json
{
  "email": "parent@example.com",
  "token": "oob_code_from_email",
  "newPassword": "NewSecurePass789@",
  "confirmPassword": "NewSecurePass789@"
}
```

5. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Password has been reset successfully",
  "data": null
}
```

---

### 8. POST /api/Auth/refresh-token
**Purpose:** Get a new access token without logging in again

**Authentication Required:** ❌ No (but requires refresh token)

**When to use:**
- Current ID token expired
- Don't want to ask user to login again

**Steps:**

1. Expand **POST /api/Auth/refresh-token**
2. Click **"Try it out"**
3. Enter the refresh token you got from login:

```json
{
  "refreshToken": "AMf-vBzurhSU-ba2BTvSdfZ5_BpVVg..."
}
```

4. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "idToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "AMf-vBzurhSU-ba2BTvSdfZ5...",
    "expiresAt": "2025-11-05T08:39:30Z"
  }
}
```

---

### 9. POST /api/Auth/send-verification-email
**Purpose:** Resend email verification link

**Authentication Required:** ❌ No

**When to use:**
- User didn't receive verification email
- Verification email expired

**Steps:**

1. Expand **POST /api/Auth/send-verification-email**
2. Click **"Try it out"**
3. Enter:

```json
{
  "email": "parent@example.com"
}
```

4. Click **"Execute"**

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Verification email sent. Please check your inbox.",
  "data": null
}
```

**Note:** Requires email service to be configured

---

## Using Bearer Token Authorization

Many endpoints require authentication. Here's how to set it up in Swagger:

### Step-by-Step Guide

1. **Get an ID Token**
   - Login using `/api/Auth/login` endpoint
   - Copy the `idToken` from the response
   - Example: `eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...`

2. **Authorize in Swagger**
   - Look for the **"Authorize"** button at the top right of Swagger UI (lock icon 🔒)
   - Click it
   - A popup appears with "Available authorizations"

3. **Enter Token**
   - In the "Value" field, enter: `Bearer YOUR_TOKEN_HERE`
   - Example: `Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...`
   - **Important:** Include the word "Bearer" followed by a space

4. **Click Authorize**
   - The lock icon should now be closed 🔒 (indicating you're authorized)
   - Click **"Close"**

5. **Test Protected Endpoints**
   - Now you can call any endpoint that requires authentication
   - The token is automatically included in requests

### How to Know if Authorization Worked

- **Closed lock icon** 🔒 = Authorized
- **Open lock icon** 🔓 = Not authorized
- **401 responses** = Authorization failed or expired

### When Token Expires

Tokens expire after 1 hour. When this happens:
1. You'll get `401 Unauthorized` responses
2. Either:
   - **Option A:** Login again and get a new token
   - **Option B:** Use `/api/Auth/refresh-token` with your refresh token

---

## Complete Testing Workflows

### Workflow 1: New User Registration & Login

**Goal:** Create a new user, login, and view profile

1. **Register User**
   - Use `/api/Auth/register`
   - Save the `idToken` and `userId`

2. **Authorize in Swagger**
   - Click "Authorize" button
   - Enter: `Bearer YOUR_ID_TOKEN`
   - Click "Authorize" and "Close"

3. **Get Profile**
   - Use `/api/Auth/profile`
   - Verify user data matches registration

4. **Verify in Database**
   - Open MySQL Workbench
   - Run: `SELECT * FROM Users WHERE Email = 'parent@example.com';`
   - Verify user record exists

### Workflow 2: Password Change Flow

**Goal:** Change user password and verify

1. **Login**
   - Use `/api/Auth/login` with current credentials
   - Authorize with the token

2. **Change Password**
   - Use `/api/Auth/change-password`
   - Provide current and new passwords

3. **Logout**
   - Use `/api/Auth/logout`

4. **Login with New Password**
   - Use `/api/Auth/login` with new credentials
   - Should succeed

5. **Try Old Password**
   - Use `/api/Auth/login` with old credentials
   - Should fail with "Invalid credentials"

### Workflow 3: Password Reset Flow

**Goal:** Reset forgotten password

1. **Request Reset**
   - Use `/api/Auth/reset-password`
   - Enter user email

2. **Check Email**
   - Check Mailgun logs or email inbox
   - Copy the reset token from email

3. **Confirm Reset**
   - Use `/api/Auth/confirm-reset-password`
   - Provide email, token, and new password

4. **Login**
   - Use `/api/Auth/login` with new password
   - Should succeed

### Workflow 4: Token Refresh Flow

**Goal:** Refresh expired token without re-login

1. **Login**
   - Use `/api/Auth/login`
   - Save both `idToken` and `refreshToken`

2. **Wait for Expiration** (or simulate)
   - Token expires in 1 hour
   - Or manually use an expired token

3. **Refresh Token**
   - Use `/api/Auth/refresh-token`
   - Provide the `refreshToken`
   - Receive new `idToken`

4. **Use New Token**
   - Authorize with new token
   - Use `/api/Auth/profile` to verify it works

---

## Testing All User Roles

Test the system with different user roles to ensure proper functionality.

### Parent Role Testing

**Registration:**
```json
{
  "email": "parent.test@example.com",
  "password": "Parent123@Test",
  "confirmPassword": "Parent123@Test",
  "firstName": "Mary",
  "lastName": "Parent",
  "role": "Parent",
  "phoneNumber": "+84987654321",
  "acceptTerms": true
}
```

**Expected Behavior:**
- Can register children
- Can submit enrollment applications
- Can view own children's information
- Cannot access staff-only features

### Staff Role Testing

**Registration:**
```json
{
  "email": "staff.test@example.com",
  "password": "Staff123@Test",
  "confirmPassword": "Staff123@Test",
  "firstName": "Alice",
  "lastName": "Staff",
  "role": "Staff",
  "phoneNumber": "+84912345678",
  "acceptTerms": true
}
```

**Expected Behavior:**
- Can manage applications
- Can view all students
- Cannot modify system settings

### Teacher Role Testing

**Registration:**
```json
{
  "email": "teacher.test@example.com",
  "password": "Teacher123@Test",
  "confirmPassword": "Teacher123@Test",
  "firstName": "Bob",
  "lastName": "Teacher",
  "role": "Teacher",
  "phoneNumber": "+84923456789",
  "acceptTerms": true
}
```

**Expected Behavior:**
- Can manage classroom
- Can view assigned students
- Can update student progress

### Admin Role Testing

**Registration:**
```json
{
  "email": "admin.test@example.com",
  "password": "Admin123@Test",
  "confirmPassword": "Admin123@Test",
  "firstName": "Charlie",
  "lastName": "Admin",
  "role": "Admin",
  "phoneNumber": "+84934567890",
  "acceptTerms": true
}
```

**Expected Behavior:**
- Full system access
- Can manage all users
- Can modify settings
- Can view all data

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: "400 Bad Request - Validation Error"

**Cause:** Request body doesn't match requirements

**Solutions:**
- Check all required fields are present
- Verify data types (strings in quotes, booleans as true/false)
- Ensure password meets complexity requirements
- Check phone number format (must start with +)
- Verify email format is valid

#### Issue: "401 Unauthorized"

**Cause:** Missing or invalid authentication token

**Solutions:**
- Click "Authorize" button and enter token
- Make sure token includes "Bearer " prefix
- Check if token expired (login again)
- Verify you copied the entire token

#### Issue: "404 Not Found"

**Cause:** Endpoint doesn't exist or typo in URL

**Solutions:**
- Refresh Swagger UI page
- Check if API server is running
- Verify endpoint URL is correct

#### Issue: "500 Internal Server Error"

**Cause:** Server-side error (usually configuration or database)

**Solutions:**
- Check API server console logs for error details
- Verify MySQL database is running
- Check Firebase configuration is correct
- Verify email service configuration (if using email endpoints)

#### Issue: Email Not Sent

**Cause:** Email service not configured or misconfigured

**Solutions:**
- Check `appsettings.json` email settings
- Verify Mailgun credentials are correct
- Check authorized recipients in Mailgun (sandbox domain)
- View Mailgun logs for delivery status
- Check spam folder in email client

#### Issue: "Firebase Error"

**Cause:** Firebase authentication or configuration issue

**Solutions:**
- Verify `firebase-adminsdk.json` file exists
- Check Firebase API Key in `appsettings.json`
- Ensure Firebase project is active
- Check Firebase console for user status

---

## Verifying Data in MySQL

After testing endpoints, verify data was saved correctly:

### Connect to MySQL

1. Open **MySQL Workbench**
2. Connect to `localhost:3306`
3. Username: `root`
4. Password: `123456`
5. Select database: `PreschoolEnrollmentDB`

### Common Queries

**View all users:**
```sql
SELECT
    Id,
    Email,
    FirstName,
    LastName,
    Role,
    EmailVerified,
    IsActive,
    CreatedAt,
    LastLoginAt
FROM Users
WHERE IsDeleted = 0
ORDER BY CreatedAt DESC;
```

**Find specific user:**
```sql
SELECT * FROM Users
WHERE Email = 'parent@example.com';
```

**Count users by role:**
```sql
SELECT
    Role,
    COUNT(*) as Total
FROM Users
WHERE IsDeleted = 0
GROUP BY Role;
```

**View recent logins:**
```sql
SELECT
    Email,
    FirstName,
    LastName,
    LastLoginAt
FROM Users
WHERE LastLoginAt IS NOT NULL
ORDER BY LastLoginAt DESC
LIMIT 10;
```

**Check profile completion:**
```sql
SELECT
    Email,
    ProfileCompletionPercentage,
    EmailVerified,
    PhoneVerified,
    AcceptedTerms
FROM Users
ORDER BY ProfileCompletionPercentage DESC;
```

---

## Best Practices

### For Testing

1. **Use Consistent Data**
   - Create a set of test users for each role
   - Use predictable emails (e.g., parent1@test.com, parent2@test.com)
   - Save test credentials in a secure note

2. **Test in Order**
   - Start with registration
   - Then login
   - Then protected endpoints
   - Finally, test edge cases

3. **Clean Up Test Data**
   - Regularly delete test users from database
   - Or use a separate test database
   - Keep production and testing separate

4. **Document Issues**
   - Take screenshots of errors
   - Note the exact request body used
   - Record server logs for debugging

### For Development

1. **Use Environment Variables**
   - Don't commit sensitive data
   - Use `appsettings.Development.json` for local settings
   - Use User Secrets for sensitive configs

2. **Monitor Logs**
   - Keep server console visible while testing
   - Check for warnings and errors
   - Use logs to debug issues

3. **Version Your API**
   - Consider adding `/v1/` to routes for versioning
   - Makes it easier to update later

4. **Rate Limiting**
   - Implement rate limiting in production
   - Prevent abuse of registration/login endpoints

---

## Next Steps

1. **Complete Mailgun Setup**
   - Follow `MAILGUN_SETUP_GUIDE.md`
   - Test email functionality

2. **Test All Endpoints**
   - Go through each workflow above
   - Test with different user roles
   - Verify data in database

3. **Build Mobile App**
   - Use these same endpoints in your mobile app
   - Implement proper token management
   - Handle errors gracefully

4. **Add More Endpoints**
   - Children management
   - Application submission
   - Classroom management
   - Payment processing

---

## Quick Tips

💡 **Tip 1:** Keep a text file with your test tokens handy - copy/paste is faster than logging in repeatedly

💡 **Tip 2:** Use Swagger's "Schema" section at the bottom to understand data structure

💡 **Tip 3:** Right-click on Execute → "Copy as cURL" to test in terminal

💡 **Tip 4:** Check server console logs if something unexpected happens

💡 **Tip 5:** Use meaningful test data - it makes debugging easier

---

## Support

If you encounter issues not covered in this guide:

1. Check server console logs
2. Verify MySQL database is running
3. Confirm Firebase configuration
4. Review `appsettings.json` settings
5. Check `MAILGUN_SETUP_GUIDE.md` for email issues

---

**Document Version:** 1.0
**Last Updated:** 2025-11-05
**API Version:** v1.0
