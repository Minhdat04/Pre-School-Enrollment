# Database Seeding Guide

## Overview

This guide explains how to seed your preschool enrollment database with realistic sample data for testing and development purposes.

---

## What Gets Seeded

The seeding process creates:

### 1. **Users** (11 total)
- **1 Admin** - Full system access
- **2 Staff** - Administrative staff
- **3 Teachers** - Each assigned to a classroom
- **5 Parents** - With children enrolled

### 2. **Classrooms** (3 total)
- Lớp Mầm (25 capacity)
- Lớp Chồi (30 capacity)
- Lớp Lá (30 capacity)

### 3. **Children** (8 total)
- Linked to parent accounts
- Various ages and grades

### 4. **Students** (4 total)
- Approved children enrolled in classrooms

### 5. **Applications** (8 total)
- Various statuses: Pending, Completed, Approved, Rejected

### 6. **Payments** (6 total)
- VNPay payment records for completed applications

### 7. **Sample Files** (30+ files)
- Profile photos for all users
- Student photos
- Birth certificates
- Parent ID verifications
- Payment receipts

---

## Seeded User Credentials

**IMPORTANT:** All users share the same password for testing: `SeedUser123!@#`

### Admin Account
| Email | Password | Role | Name |
|-------|----------|------|------|
| admin@preschool.edu.vn | SeedUser123!@# | Admin | Nguyễn Quản Trị |

### Staff Accounts
| Email | Password | Role | Name |
|-------|----------|------|------|
| staff1@preschool.edu.vn | SeedUser123!@# | Staff | Trần Thu Hà |
| staff2@preschool.edu.vn | SeedUser123!@# | Staff | Lê Minh Tuấn |

### Teacher Accounts
| Email | Password | Role | Name | Classroom |
|-------|----------|------|------|-----------|
| teacher1@preschool.edu.vn | SeedUser123!@# | Teacher | Phạm Thị Lan | Lớp Mầm |
| teacher2@preschool.edu.vn | SeedUser123!@# | Teacher | Hoàng Văn Nam | Lớp Chồi |
| teacher3@preschool.edu.vn | SeedUser123!@# | Teacher | Vũ Thị Hoa | Lớp Lá |

### Parent Accounts
| Email | Password | Role | Name | Children |
|-------|----------|------|------|----------|
| parent1@gmail.com | SeedUser123!@# | Parent | Nguyễn Văn An | 2 children |
| parent2@gmail.com | SeedUser123!@# | Parent | Trần Thị Bình | 1 child |
| parent3@gmail.com | SeedUser123!@# | Parent | Lê Hoàng Cường | 2 children |
| parent4@gmail.com | SeedUser123!@# | Parent | Phạm Thị Dung | 1 child |
| parent5@gmail.com | SeedUser123!@# | Parent | Hoàng Văn Em | 2 children |

---

## How to Seed the Database

### Prerequisites

1. ✅ Firebase project configured
2. ✅ Firebase Storage enabled
3. ✅ MySQL database created and accessible
4. ✅ User Secrets configured with Firebase credentials
5. ✅ Application running in **Development** mode

### Step 1: Start the API

```bash
cd PreschoolEnrollmentSystem.API
dotnet run
```

The API should start at: `https://localhost:7001`

### Step 2: Check Seed Status

```http
GET https://localhost:7001/api/seed/status
```

**Response:**
```json
{
  "seedDataExists": false,
  "environment": "Development",
  "message": "No seed data found in the database",
  "availableEndpoints": {
    "seedDatabase": "POST /api/seed/run?confirm=true",
    "uploadFiles": "POST /api/seed/upload-files?confirm=true",
    "clearData": "DELETE /api/seed/clear?confirm=true",
    "checkStatus": "GET /api/seed/status"
  }
}
```

### Step 3: View Seed Information

```http
GET https://localhost:7001/api/seed/info
```

This shows all users, passwords, and what will be created.

### Step 4: Run Database Seeding

```http
POST https://localhost:7001/api/seed/run?confirm=true
```

**What happens:**
1. Creates 3 classrooms
2. Creates 11 users in Firebase Authentication
3. Stores users in MySQL database
4. Creates 8 children for parents
5. Creates 4 students (enrolled children)
6. Creates 8 applications with various statuses
7. Creates 6 payment records

**Expected Response:**
```json
{
  "success": true,
  "message": "Database seeded successfully!",
  "summary": {
    "usersCreated": 11,
    "classroomsCreated": 3,
    "childrenCreated": 8,
    "studentsCreated": 4,
    "applicationsCreated": 8,
    "paymentsCreated": 6,
    "seededUsers": [
      {
        "email": "admin@preschool.edu.vn",
        "password": "SeedUser123!@#",
        "role": "Admin",
        "firebaseUid": "XXX...",
        "fullName": "Nguyễn Quản Trị"
      },
      // ... more users
    ]
  }
}
```

### Step 5: Upload Sample Files (Optional)

```http
POST https://localhost:7001/api/seed/upload-files?confirm=true
```

**What happens:**
- Generates placeholder images for:
  - Profile photos (11 images)
  - Student photos (8 images)
  - Birth certificates (8 images)
  - Parent ID verifications (5 images)
  - Payment receipts (6 images)
- Uploads to Firebase Storage

**Note:** This may take 30-60 seconds depending on your connection.

---

## Testing the Seeded Data

### Login as Admin
```http
POST https://localhost:7001/api/auth/login
Content-Type: application/json

{
  "email": "admin@preschool.edu.vn",
  "password": "SeedUser123!@#"
}
```

### Login as Parent
```http
POST https://localhost:7001/api/auth/login
Content-Type: application/json

{
  "email": "parent1@gmail.com",
  "password": "SeedUser123!@#"
}
```

### View Applications (as Parent)
After logging in, use the returned token:

```http
GET https://localhost:7001/api/applications
Authorization: Bearer <your-token>
```

### View Classrooms (as Teacher)
```http
GET https://localhost:7001/api/classrooms
Authorization: Bearer <teacher-token>
```

---

## Seeded Data Details

### Classrooms
| Name | Grade | Capacity | Teacher |
|------|-------|----------|---------|
| Lớp Mầm | Mầm | 25 | Phạm Thị Lan |
| Lớp Chồi | Chồi | 30 | Hoàng Văn Nam |
| Lớp Lá | Lá | 30 | Vũ Thị Hoa |

### Children & Students
| Child Name | Parent | Birthdate | Gender | Grade | Status |
|------------|--------|-----------|--------|-------|--------|
| Nguyễn Minh Anh | Nguyễn Văn An | 2021-03-15 | Female | Mầm | Payment Completed |
| Nguyễn Hoàng Bảo | Nguyễn Văn An | 2020-07-22 | Male | Chồi | Approved (Student) |
| Trần Thảo Chi | Trần Thị Bình | 2021-05-10 | Female | Mầm | Payment Completed |
| Lê Minh Đức | Lê Hoàng Cường | 2020-09-08 | Male | Chồi | Approved (Student) |
| Lê Thu Hà | Lê Hoàng Cường | 2019-11-25 | Female | Lá | Approved (Student) |
| Phạm Gia Huy | Phạm Thị Dung | 2021-01-30 | Male | Mầm | Payment Pending |
| Hoàng Khánh Linh | Hoàng Văn Em | 2020-06-18 | Female | Chồi | Approved (Student) |
| Hoàng Minh Khôi | Hoàng Văn Em | 2019-12-05 | Male | Lá | Rejected |

### Application Statuses
- **Payment Pending** (1): Phạm Gia Huy - Awaiting payment
- **Payment Completed** (2): Nguyễn Minh Anh, Trần Thảo Chi - Awaiting staff approval
- **Approved** (4): Nguyễn Hoàng Bảo, Lê Minh Đức, Lê Thu Hà, Hoàng Khánh Linh - Enrolled as students
- **Rejected** (1): Hoàng Minh Khôi - Reason: "Không đủ độ tuổi theo quy định nhà trường"

### Payment Information
- **Amount**: 1,000,000 VND per enrollment
- **Bank**: NCB (National Citizens Bank)
- **Payment Method**: ATM Card
- **Status**: All completed payments have response code "00" (Success)

---

## Clearing Seed Data

To remove all seeded data and start fresh:

```http
DELETE https://localhost:7001/api/seed/clear?confirm=true
```

**What gets deleted:**
1. All payments
2. All applications
3. All students
4. All children
5. All classrooms
6. All seeded users (from both database AND Firebase)

**WARNING:** This action cannot be undone!

---

## Using Seeded Data in Swagger

1. Open Swagger UI: `https://localhost:7001/swagger`
2. Scroll to **Seed** section
3. Click on `POST /api/seed/run`
4. Click "Try it out"
5. Set `confirm` to `true`
6. Click "Execute"

After seeding:
1. Use `POST /api/auth/login` to login with a seeded user
2. Copy the returned `idToken`
3. Click "Authorize" button at the top
4. Enter: `Bearer <your-token>`
5. Now you can test all protected endpoints!

---

## Troubleshooting

### Error: "Seeding is only allowed in Development environment"
**Solution:** Ensure your API is running with `ASPNETCORE_ENVIRONMENT=Development`

```bash
# Windows
set ASPNETCORE_ENVIRONMENT=Development
dotnet run

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

### Error: "Seed data already exists"
**Solution:** Clear existing seed data first:
```http
DELETE https://localhost:7001/api/seed/clear?confirm=true
```

### Error: "Failed to create Firebase user"
**Solution:**
1. Check Firebase Admin SDK credentials are correctly configured
2. Verify `firebase-adminsdk.json` exists in API directory
3. Check User Secrets contain correct Firebase configuration
4. Ensure Firebase Authentication is enabled in Firebase Console

### Error: "Storage bucket not found"
**Solution:**
1. Enable Firebase Storage in Firebase Console
2. Verify storage bucket name in User Secrets: `preschoolenrollment-bad16.appspot.com`
3. Deploy storage rules: `firebase deploy --only storage`

### File Upload Takes Too Long
**Solution:**
- File upload generates 30+ placeholder images dynamically
- First upload may take 30-60 seconds
- Subsequent uploads are faster as Firebase caches
- You can skip file upload if only testing API functionality

### Error: "Permission denied" when uploading files
**Solution:**
1. Ensure storage.rules are deployed: `firebase deploy --only storage`
2. Check Firebase Storage is enabled in Console
3. Verify files are being uploaded with correct authentication

---

## Firebase Console Verification

After seeding, verify in Firebase Console:

### Authentication Users
URL: `https://console.firebase.google.com/project/preschoolenrollment-bad16/authentication/users`

You should see 11 users with:
- ✅ Email verified
- ✅ Custom claims for roles
- ✅ Display names set

### Storage Files (after file upload)
URL: `https://console.firebase.google.com/project/preschoolenrollment-bad16/storage`

Folder structure:
```
/users/profile-photos/      (11 files)
/students/photos/           (8 files)
/students/documents/        (8 files)
/parents/id-verification/   (5 files)
/payments/receipts/         (6 files)
```

---

## Best Practices

### Development Workflow
1. **Start clean:** Always clear seed data before re-seeding
2. **Test incrementally:** Seed database first, test API, then upload files
3. **Use different users:** Test with different roles to verify authorization
4. **Check Firebase Console:** Verify users and files are created correctly

### Testing Scenarios
1. **Admin workflow:**
   - Login as admin
   - View all applications
   - Approve/reject applications
   - View all payments

2. **Parent workflow:**
   - Login as parent
   - View own children
   - Create new application
   - View application status
   - Make payment

3. **Teacher workflow:**
   - Login as teacher
   - View assigned classroom
   - View students in classroom

4. **Staff workflow:**
   - Login as staff
   - View all applications
   - Process applications
   - View payments

---

## API Endpoints Summary

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | /api/seed/status | Check if seed data exists | No |
| GET | /api/seed/info | View seed data information | No |
| POST | /api/seed/run?confirm=true | Seed database | No |
| POST | /api/seed/upload-files?confirm=true | Upload sample files | No |
| DELETE | /api/seed/clear?confirm=true | Clear all seed data | No |

**Note:** All endpoints only work in Development environment.

---

## Next Steps

After seeding your database:

1. ✅ **Test Authentication**
   - Try logging in with different user roles
   - Verify tokens are generated correctly

2. ✅ **Test Authorization**
   - Ensure admin can access admin endpoints
   - Verify parents can only see their own data
   - Check teachers can access their classroom

3. ✅ **Test Business Logic**
   - Create new applications as parents
   - Approve/reject applications as admin/staff
   - Process payments

4. ✅ **Test File Operations**
   - Upload new student photos
   - Download birth certificates
   - View payment receipts

5. ✅ **Explore Firebase Console**
   - Check user authentication records
   - Browse uploaded files in Storage
   - Review security rules

---

## Support

If you encounter issues:

1. Check application logs for detailed error messages
2. Verify Firebase configuration in User Secrets
3. Ensure MySQL database is accessible
4. Check Firebase Console for service status
5. Review `FIREBASE_SETUP_GUIDE.md` for additional troubleshooting

---

## Security Notes

⚠️ **IMPORTANT WARNINGS:**

1. **Never use seed data in production**
   - Default password is public knowledge
   - Seed data is for testing only
   - Always use real users in production

2. **Seeding is disabled in production**
   - All seed endpoints return 403 Forbidden outside Development
   - Safety checks prevent accidental production seeding

3. **Clear seed data regularly**
   - Don't let test data accumulate
   - Start fresh for each testing cycle

4. **Protect Firebase credentials**
   - Never commit `firebase-adminsdk.json`
   - Use User Secrets for local development
   - Use environment variables for servers

---

## Summary

You now have a fully populated database with:
- ✅ 11 users across all roles with Firebase authentication
- ✅ 3 classrooms with assigned teachers
- ✅ 8 children with realistic Vietnamese names
- ✅ 4 enrolled students in classrooms
- ✅ 8 applications in various states
- ✅ 6 completed payment records
- ✅ 30+ sample files in Firebase Storage (after file upload)

**All users share the password:** `SeedUser123!@#`

Start testing your preschool enrollment system!
