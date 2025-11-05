# API Testing Quick Reference

Quick commands and examples for testing your Preschool Enrollment System API.

## Quick Start in Swagger

1. Open browser: **http://localhost:5098/swagger**
2. Find the endpoint you want to test
3. Click **"Try it out"**
4. Fill in the JSON
5. Click **"Execute"**

---

## Ready-to-Use Test Data

### 1. Register Parent User

```json
{
  "email": "john.parent@example.com",
  "password": "Parent123@Test",
  "confirmPassword": "Parent123@Test",
  "firstName": "John",
  "lastName": "Parent",
  "role": "Parent",
  "phoneNumber": "+84123456789",
  "acceptTerms": true
}
```

### 2. Register Staff User

```json
{
  "email": "alice.staff@example.com",
  "password": "Staff123@Test",
  "confirmPassword": "Staff123@Test",
  "firstName": "Alice",
  "lastName": "Staff",
  "role": "Staff",
  "phoneNumber": "+84987654321",
  "acceptTerms": true
}
```

### 3. Register Teacher User

```json
{
  "email": "bob.teacher@example.com",
  "password": "Teacher123@Test",
  "confirmPassword": "Teacher123@Test",
  "firstName": "Bob",
  "lastName": "Teacher",
  "role": "Teacher",
  "phoneNumber": "+84912345678",
  "acceptTerms": true
}
```

### 4. Register Admin User

```json
{
  "email": "admin@example.com",
  "password": "Admin123@Test",
  "confirmPassword": "Admin123@Test",
  "firstName": "Charlie",
  "lastName": "Admin",
  "role": "Admin",
  "phoneNumber": "+84934567890",
  "acceptTerms": true
}
```

### 5. Login

```json
{
  "email": "john.parent@example.com",
  "password": "Parent123@Test"
}
```

### 6. Change Password

```json
{
  "currentPassword": "Parent123@Test",
  "newPassword": "NewPassword456@",
  "confirmNewPassword": "NewPassword456@"
}
```

### 7. Reset Password Request

```json
{
  "email": "john.parent@example.com"
}
```

### 8. Refresh Token

```json
{
  "refreshToken": "YOUR_REFRESH_TOKEN_FROM_LOGIN_RESPONSE"
}
```

---

## How to Authorize in Swagger (Protected Endpoints)

### Step 1: Get Token
1. Use `/api/Auth/login` endpoint
2. Copy the `idToken` from response
3. Example token: `eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...`

### Step 2: Authorize
1. Click **"Authorize"** button (🔒 icon at top right)
2. Enter: `Bearer YOUR_TOKEN_HERE`
3. Click **"Authorize"**
4. Click **"Close"**

### Step 3: Test
- Now you can use protected endpoints like `/api/Auth/profile`

---

## All Available Endpoints

### Public Endpoints (No Auth Required)
- ✅ `POST /api/Auth/register` - Register new user
- ✅ `POST /api/Auth/login` - Login user
- ✅ `POST /api/Auth/reset-password` - Request password reset
- ✅ `POST /api/Auth/confirm-reset-password` - Confirm password reset
- ✅ `POST /api/Auth/refresh-token` - Refresh access token
- ✅ `POST /api/Auth/send-verification-email` - Resend verification email

### Protected Endpoints (Auth Required)
- 🔒 `GET /api/Auth/profile` - Get user profile
- 🔒 `POST /api/Auth/change-password` - Change password
- 🔒 `POST /api/Auth/logout` - Logout user

---

## Testing Workflow

### Complete Registration & Login Flow

**Step 1: Register**
- Endpoint: `POST /api/Auth/register`
- Use parent registration JSON above
- Save the `idToken` from response

**Step 2: Login**
- Endpoint: `POST /api/Auth/login`
- Use login JSON with same email/password
- Copy the new `idToken`

**Step 3: Authorize**
- Click "Authorize" button
- Enter: `Bearer YOUR_ID_TOKEN`
- Click "Authorize" and "Close"

**Step 4: Get Profile**
- Endpoint: `GET /api/Auth/profile`
- Click "Try it out" → "Execute"
- Should see your user profile

**Step 5: Change Password**
- Endpoint: `POST /api/Auth/change-password`
- Use change password JSON
- Should succeed

**Step 6: Logout**
- Endpoint: `POST /api/Auth/logout`
- Click "Execute"
- Token is now invalid

---

## Common Errors & Solutions

| Error | What It Means | Fix |
|-------|---------------|-----|
| `400 Bad Request` | Invalid input data | Check your JSON matches examples |
| `401 Unauthorized` | Not authenticated | Click "Authorize" and add Bearer token |
| `409 Conflict` | Email already exists | Use different email |
| Token expired | Token older than 1 hour | Login again to get new token |

---

## Password Requirements

✅ At least 8 characters
✅ At least 1 uppercase letter (A-Z)
✅ At least 1 lowercase letter (a-z)
✅ At least 1 number (0-9)
✅ At least 1 special character (@, !, #, $, etc.)

**Valid Examples:**
- `Parent123@Test`
- `SecurePass456!`
- `MyPassword789#`

**Invalid Examples:**
- `password` (no uppercase, no number, no special char)
- `Password` (no number, no special char)
- `Pass123` (no special char)

---

## Phone Number Format

✅ Must start with `+` (plus sign)
✅ Must include country code
✅ No spaces or dashes

**Valid Examples:**
- `+84123456789` (Vietnam)
- `+1234567890` (US)
- `+447123456789` (UK)

**Invalid Examples:**
- `0123456789` (missing + and country code)
- `+84 123 456 789` (has spaces)
- `84123456789` (missing +)

---

## User Roles

Available roles for registration:
- `Parent` - Can manage children and applications
- `Staff` - Can manage applications and students
- `Teacher` - Can manage classroom and students
- `Admin` - Full system access

---

## MySQL Verification Queries

### Check if user was created
```sql
SELECT * FROM Users WHERE Email = 'john.parent@example.com';
```

### View all users
```sql
SELECT Email, FirstName, LastName, Role, CreatedAt
FROM Users
WHERE IsDeleted = 0
ORDER BY CreatedAt DESC;
```

### Count users by role
```sql
SELECT Role, COUNT(*) as Total
FROM Users
WHERE IsDeleted = 0
GROUP BY Role;
```

---

## Server Information

- **API URL:** http://localhost:5098
- **Swagger UI:** http://localhost:5098/swagger
- **Database:** localhost:3306/PreschoolEnrollmentDB
- **MySQL User:** root
- **MySQL Password:** 123456

---

## Token Information

### ID Token
- **Purpose:** Access API endpoints
- **Expiration:** 1 hour
- **Format:** `Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...`
- **Where to use:** Authorization header in Swagger

### Refresh Token
- **Purpose:** Get new ID token without login
- **Expiration:** Long-lived
- **Usage:** Send to `/api/Auth/refresh-token`

---

## Tips & Tricks

💡 **Save your tokens** in a text file for quick copy/paste

💡 **Use consistent test emails** like parent1@test.com, parent2@test.com

💡 **Check server console** if something doesn't work

💡 **Email won't send** without proper SMTP config (expected behavior)

💡 **Token format:** Always include "Bearer " before the token

💡 **Case sensitive:** Role must be exactly "Parent", "Staff", "Teacher", or "Admin"

---

## Quick Test Checklist

- [ ] Register a Parent user
- [ ] Login with that user
- [ ] Authorize in Swagger with token
- [ ] Get user profile
- [ ] Change password
- [ ] Logout
- [ ] Login with new password
- [ ] Register Staff user
- [ ] Register Teacher user
- [ ] Register Admin user
- [ ] Verify all users in MySQL

---

## Need More Help?

📖 **Detailed Guide:** See `SWAGGER_TESTING_GUIDE.md`
📧 **Email Setup:** See `MAILGUN_SETUP_GUIDE.md`
🗄️ **Database:** Open MySQL Workbench and connect to localhost:3306

---

**Last Updated:** 2025-11-05
