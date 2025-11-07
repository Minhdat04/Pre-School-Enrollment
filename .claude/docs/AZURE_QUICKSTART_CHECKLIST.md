# Azure Deployment Quick Start Checklist

Use this checklist to deploy your Preschool Enrollment System to Azure in ~30-45 minutes.

---

## ✅ Pre-Deployment Checklist

- [ ] Azure account created ([Get free trial](https://azure.microsoft.com/free/))
- [ ] Azure CLI installed locally
- [ ] Have firebase-adminsdk.json file ready
- [ ] Know your MySQL password (create a strong one)
- [ ] Repository pushed to GitHub (✅ Already done!)

---

## 📝 Quick Deployment Steps

### Step 1: Azure Portal Setup (15 minutes)

1. **Login to Azure Portal**: https://portal.azure.com

2. **Create Resource Group**
   - Name: `preschool-enrollment-rg`
   - Region: `Southeast Asia` (or closest to you)

3. **Create MySQL Server** (~5-7 min deployment time)
   - Name: `preschool-mysql-server` (must be unique)
   - Version: `8.0`
   - Compute: Start with `Burstable B1ms`
   - Admin: `preschooladmin`
   - Password: [CREATE STRONG PASSWORD - SAVE IT!]
   - Networking: ✅ Allow Azure services
   - After creation: Create database `PreschoolEnrollmentDB`

4. **Create App Service Plan**
   - Name: `preschool-api-plan`
   - OS: `Linux`
   - Tier: `Basic B1` (for testing) or `Standard S1` (for production)

5. **Create App Service**
   - Name: `preschool-enrollment-api` (must be unique)
   - Runtime: `.NET 8 (LTS)`
   - OS: `Linux`
   - Enable Application Insights: ✅ Yes

---

### Step 2: Configure Application (10 minutes)

**In App Service → Configuration → Application settings**, add:

```
Name                          | Value
------------------------------|----------------------------------------
ASPNETCORE_ENVIRONMENT        | Production
Firebase__ProjectId           | preschoolenrollment-bad16
Firebase__ApiKey              | [YOUR_FIREBASE_API_KEY]
Firebase__StorageBucket       | preschoolenrollment-bad16.appspot.com
Firebase__CredentialPath      | firebase-adminsdk.json
Email__SmtpHost              | smtp.gmail.com
Email__SmtpPort              | 587
Email__Username              | [YOUR_EMAIL@gmail.com]
Email__Password              | [YOUR_GMAIL_APP_PASSWORD]
Email__FromEmail             | [YOUR_EMAIL@gmail.com]
```

**In Configuration → Connection strings**, add:

```
Name: DefaultConnection
Type: MySQL
Value: Server=preschool-mysql-server.mysql.database.azure.com;Port=3306;Database=PreschoolEnrollmentDB;Uid=preschooladmin;Pwd=[YOUR_MYSQL_PASSWORD];SslMode=Required;
```

Click **Save**!

---

### Step 3: Upload Firebase Credentials (5 minutes)

**Option A: Using Kudu (Simpler)**

1. App Service → Development Tools → Advanced Tools → Go
2. Debug console → CMD
3. Navigate to `site/wwwroot`
4. Drag & drop your `firebase-adminsdk.json` file

**Option B: Using Azure CLI**

```bash
az login
az webapp deployment source config-local-git --name preschool-enrollment-api --resource-group preschool-enrollment-rg
# Follow instructions to upload firebase-adminsdk.json
```

---

### Step 4: Deploy Application (10 minutes)

**Using Visual Studio (Easiest)**

1. Open solution in Visual Studio
2. Right-click `PreschoolEnrollmentSystem.API` → Publish
3. Target: Azure → Azure App Service (Linux)
4. Select your App Service
5. Click **Publish**

**Using Azure CLI**

```bash
cd PreschoolEnrollmentSystem.API
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
cd ..

az webapp deployment source config-zip \
  --resource-group preschool-enrollment-rg \
  --name preschool-enrollment-api \
  --src deploy.zip
```

---

### Step 5: Run Database Migrations (5 minutes)

**Option A: Using Azure Cloud Shell**

```bash
# In Azure Portal, click Cloud Shell icon (>_)
git clone https://github.com/Minhdat04/Pre-School-Enrollment.git
cd Pre-School-Enrollment
git checkout main

cd PreschoolEnrollmentSystem.API
CONNECTION_STRING="Server=preschool-mysql-server.mysql.database.azure.com;Port=3306;Database=PreschoolEnrollmentDB;Uid=preschooladmin;Pwd=YOUR_PASSWORD;SslMode=Required;" \
dotnet ef database update --project ../PreschoolEnrollmentSystem.Infrastructure/PreschoolEnrollmentSystem.Infrastructure.csproj
```

**Option B: From Local Machine**

```bash
# Temporarily update appsettings.json with Azure MySQL connection
cd PreschoolEnrollmentSystem.API
dotnet ef database update --project ../PreschoolEnrollmentSystem.Infrastructure/PreschoolEnrollmentSystem.Infrastructure.csproj
# Remember to revert appsettings.json!
```

---

### Step 6: Verify Deployment (5 minutes)

1. **Open Swagger**: `https://preschool-enrollment-api.azurewebsites.net/swagger`

2. **Test Seed Status**:
   ```bash
   curl https://preschool-enrollment-api.azurewebsites.net/api/seed/status
   ```

3. **Check Logs**: App Service → Monitoring → Log stream

4. **Test Registration** (using Swagger or Postman):
   ```json
   POST /api/auth/register
   {
     "email": "test@example.com",
     "password": "Test123!@#",
     "confirmPassword": "Test123!@#",
     "firstName": "Test",
     "lastName": "User",
     "phoneNumber": "+84901234567",
     "role": "Parent",
     "acceptTerms": true
   }
   ```

---

## 🎯 Your API URLs

After deployment, share these with your team:

- **API Base URL**: `https://preschool-enrollment-api.azurewebsites.net`
- **Swagger Docs**: `https://preschool-enrollment-api.azurewebsites.net/swagger`
- **Health Check**: `https://preschool-enrollment-api.azurewebsites.net/api/seed/status`

---

## 🚨 Troubleshooting

### API returns 500 error
```bash
# Check logs
az webapp log tail --name preschool-enrollment-api --resource-group preschool-enrollment-rg
```

### Database connection fails
- Verify MySQL server name is correct
- Check firewall allows Azure services
- Verify connection string has correct password

### Firebase errors
- Ensure firebase-adminsdk.json is uploaded
- Check Firebase__CredentialPath setting points to correct file
- Restart App Service after uploading file

### Configuration not loading
- Verify all settings saved in App Service Configuration
- Use double underscores `__` instead of colons `:`
- Click **Save** and wait for restart

---

## 💰 Current Cost (Approximate)

**Development Setup:**
- App Service Basic B1: ~$13/month
- MySQL Burstable B1ms: ~$12/month
- Application Insights: Free tier (1 GB/month)
- **Total: ~$25/month**

**Free Credit Available:**
- Azure Free Trial: $200 for 30 days
- Azure for Students: $100/year (if eligible)

---

## 🔐 Security Reminders

- [ ] All passwords are strong and saved securely
- [ ] Firebase credentials NOT in source control
- [ ] MySQL allows only Azure services (firewall)
- [ ] HTTPS enforced
- [ ] Secrets in Azure Configuration (not in code)

---

## 📱 Next Steps

1. ✅ Test all API endpoints thoroughly
2. ✅ Seed database if needed: `POST /api/seed/run?confirm=true`
3. ✅ Share API URLs with mobile app team
4. ✅ Set up monitoring alerts in Application Insights
5. ✅ Configure custom domain (optional)
6. ✅ Set up GitHub Actions for CI/CD (optional)
7. ✅ Create staging environment (optional)

---

## 📞 Need Help?

Refer to the complete guide: `AZURE_DEPLOYMENT_GUIDE.md`

Common sections:
- **Phase 3.3**: Upload Firebase credentials
- **Phase 5**: Database migrations
- **Phase 6**: Verify deployment
- **Troubleshooting**: Common issues and solutions

---

## ✨ Deployment Complete!

Your Preschool Enrollment API is now running on Azure!

Share your API URL with your team:
**`https://preschool-enrollment-api.azurewebsites.net`**

🎉 Happy coding!
