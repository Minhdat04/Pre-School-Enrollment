# Setup Credentials Guide

## Firebase Admin SDK Credentials

**⚠️ IMPORTANT: The `firebase-adminsdk.json` file is NOT in this repository for security reasons.**

### For Local Development:

1. **Download Firebase Admin SDK JSON:**
   - Go to [Firebase Console](https://console.firebase.google.com/)
   - Select project: `preschoolenrollment-bad16`
   - Click ⚙️ Settings → Project Settings
   - Go to "Service accounts" tab
   - Click "Generate new private key"
   - Save as: `PreschoolEnrollmentSystem.API/firebase-adminsdk.json`

2. **Configure User Secrets:**
   ```bash
   cd PreschoolEnrollmentSystem.API
   dotnet user-secrets set "Firebase:ProjectId" "preschoolenrollment-bad16"
   dotnet user-secrets set "Firebase:StorageBucket" "preschoolenrollment-bad16.appspot.com"
   dotnet user-secrets set "Firebase:CredentialPath" "firebase-adminsdk.json"
   dotnet user-secrets set "Firebase:ApiKey" "YOUR_API_KEY_HERE"
   ```

3. **Configure MySQL Connection (Local):**
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=PreschoolEnrollmentDB;User=root;Password=YOUR_PASSWORD;"
   ```

### For Azure Deployment:

The credentials file is uploaded directly to Azure App Service (not via GitHub):

1. **Via Kudu:**
   - Navigate to: `https://your-app-name.scm.azurewebsites.net`
   - Upload `firebase-adminsdk.json` to `/site/wwwroot/`

2. **Configure App Service Application Settings:**
   - See `AZURE_DEPLOYMENT_GUIDE.md` for details

### Security Notes:

- ✅ `firebase-adminsdk.json` is in `.gitignore`
- ✅ Never commit credentials to version control
- ✅ Use User Secrets for local development
- ✅ Use Azure App Settings for production
- ❌ Never share credentials via email/chat
- ❌ Never hardcode credentials in code

### Need Help?

Contact the project administrator to get:
- Firebase project access
- Azure deployment credentials
- Database connection details
