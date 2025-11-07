# Microsoft Azure Deployment Guide

Complete guide to deploy your Preschool Enrollment System to Microsoft Azure.

---

## 🎯 Deployment Architecture

Your application will use:
- **Azure App Service** - Host ASP.NET Core Web API
- **Azure Database for MySQL** - MySQL database hosting
- **Azure Key Vault** - Secure secrets management
- **Application Insights** - Monitoring & logging
- **GitHub Actions** - CI/CD pipeline (optional)

---

## 📋 Prerequisites

Before deploying, ensure you have:

### 1. Azure Account
- Active Azure subscription ([Get free trial](https://azure.microsoft.com/free/))
- Azure CLI installed ([Download](https://docs.microsoft.com/cli/azure/install-azure-cli))

### 2. Required Information
- Firebase credentials (`firebase-adminsdk.json`)
- MySQL connection details
- Domain name (optional, for custom domain)

### 3. Local Tools
- .NET 8.0 SDK
- Git
- Visual Studio or VS Code with Azure extensions

---

## 🚀 Step-by-Step Deployment

### **Phase 1: Prepare Your Application**

#### Step 1.1: Add Azure App Service Configuration

Create `web.config` in the API project root:

```bash
cd PreschoolEnrollmentSystem.API
```

Create file: `web.config`
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\PreschoolEnrollmentSystem.API.dll"
                stdoutLogEnabled="true"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

#### Step 1.2: Update appsettings.Production.json

Create `appsettings.Production.json` in API project:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Firebase": {
    "ProjectId": "",
    "CredentialPath": "",
    "ApiKey": "",
    "StorageBucket": ""
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "Username": "",
    "Password": "",
    "FromEmail": ""
  },
  "Cors": {
    "AllowedOrigins": []
  }
}
```

**Note:** Leave values empty - they'll be set via Azure Configuration.

#### Step 1.3: Add .gitignore Entries

Ensure these are in `.gitignore`:
```
# Azure
*.pubxml
*.publishsettings
*.azurePubxml

# Secrets
firebase-adminsdk.json
appsettings.Production.json
```

#### Step 1.4: Commit Changes

```bash
git add web.config
git commit -m "feat: Add Azure deployment configuration"
git push origin dev1
```

---

### **Phase 2: Azure Portal Setup**

#### Step 2.1: Login to Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Sign in with your Microsoft account
3. Click **+ Create a resource**

---

#### Step 2.2: Create Resource Group

1. Search for **"Resource Group"**
2. Click **Create**
3. Fill in:
   - **Subscription**: Your subscription
   - **Resource group name**: `preschool-enrollment-rg`
   - **Region**: `Southeast Asia` or closest to your users
4. Click **Review + create** → **Create**

---

#### Step 2.3: Create Azure Database for MySQL

1. Search for **"Azure Database for MySQL Flexible Server"**
2. Click **Create**
3. Fill in **Basics**:
   - **Subscription**: Your subscription
   - **Resource group**: `preschool-enrollment-rg`
   - **Server name**: `preschool-mysql-server` (must be globally unique)
   - **Region**: Same as resource group
   - **MySQL version**: `8.0`
   - **Workload type**: `Development` or `Production` based on needs
   - **Compute + storage**:
     - Start with **Burstable, B1ms** (1 vCore, 2 GiB RAM) for development
     - Upgrade to **General Purpose** for production
   - **Admin username**: `preschooladmin`
   - **Password**: Create strong password (save it!)

4. **Networking** tab:
   - **Connectivity method**: `Public access (allowed IP addresses)`
   - **Firewall rules**:
     - ✅ Check **"Allow public access from any Azure service"**
     - Add your current IP for initial setup
   - **SSL enforcement**: `Enabled` (recommended)

5. Click **Review + create** → **Create**
6. **Wait 5-10 minutes** for deployment

7. After deployment:
   - Go to your MySQL server
   - Navigate to **Settings** → **Databases**
   - Click **+ Add**
   - Database name: `PreschoolEnrollmentDB`
   - Charset: `utf8mb4`
   - Collation: `utf8mb4_unicode_ci`

---

#### Step 2.4: Create App Service Plan

1. Search for **"App Service Plan"**
2. Click **Create**
3. Fill in:
   - **Subscription**: Your subscription
   - **Resource group**: `preschool-enrollment-rg`
   - **Name**: `preschool-api-plan`
   - **Operating System**: `Linux`
   - **Region**: Same as resource group
   - **Pricing tier**:
     - Development: **Basic B1** ($13.14/month)
     - Production: **Standard S1** ($69.35/month) or higher
4. Click **Review + create** → **Create**

---

#### Step 2.5: Create App Service (Web API)

1. Search for **"App Service"**
2. Click **Create** → **Web App**
3. Fill in **Basics**:
   - **Subscription**: Your subscription
   - **Resource group**: `preschool-enrollment-rg`
   - **Name**: `preschool-enrollment-api` (must be globally unique)
     - Your API URL will be: `https://preschool-enrollment-api.azurewebsites.net`
   - **Publish**: `Code`
   - **Runtime stack**: `.NET 8 (LTS)`
   - **Operating System**: `Linux`
   - **Region**: Same as resource group
   - **App Service Plan**: Select `preschool-api-plan` (created above)

4. **Deployment** tab:
   - **Continuous deployment**: Enable if using GitHub Actions
   - **GitHub Actions settings**: Configure later

5. **Monitoring** tab:
   - **Enable Application Insights**: `Yes`
   - **Application Insights**: Create new
     - Name: `preschool-api-insights`

6. Click **Review + create** → **Create**
7. Wait for deployment (2-3 minutes)

---

### **Phase 3: Configure Application Settings**

#### Step 3.1: Add Connection String

1. Go to your App Service (`preschool-enrollment-api`)
2. Navigate to **Settings** → **Configuration**
3. Click **Connection strings** tab
4. Click **+ New connection string**
   - **Name**: `DefaultConnection`
   - **Value**: Get from MySQL server:
     ```
     Server=preschool-mysql-server.mysql.database.azure.com;Port=3306;Database=PreschoolEnrollmentDB;Uid=preschooladmin;Pwd=YOUR_PASSWORD;SslMode=Required;
     ```
   - **Type**: `MySQL`
5. Click **OK**

#### Step 3.2: Add Application Settings

Still in **Configuration** → **Application settings** tab, add:

| Name | Value | Notes |
|------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` | Run from ZIP |
| `Firebase__ProjectId` | `preschoolenrollment-bad16` | Your Firebase project |
| `Firebase__ApiKey` | `YOUR_FIREBASE_API_KEY` | From Firebase Console |
| `Firebase__StorageBucket` | `preschoolenrollment-bad16.appspot.com` | Storage bucket |
| `Email__SmtpHost` | `smtp.gmail.com` | SMTP server |
| `Email__SmtpPort` | `587` | SMTP port |
| `Email__Username` | `your-email@gmail.com` | Email username |
| `Email__Password` | `your-app-password` | Gmail app password |
| `Email__FromEmail` | `your-email@gmail.com` | From address |

**Important:** Use double underscores `__` instead of colons `:` for nested configuration.

Click **Save** after adding all settings.

---

#### Step 3.3: Upload Firebase Admin SDK

**Option A: Using Azure Key Vault (Recommended for Production)**

1. Create Key Vault:
   - Search for **"Key Vault"**
   - Click **Create**
   - Name: `preschool-keyvault`
   - Resource group: `preschool-enrollment-rg`
   - Region: Same as others
   - Create

2. Upload firebase-adminsdk.json content:
   - Go to Key Vault → **Secrets**
   - Click **+ Generate/Import**
   - Name: `firebase-adminsdk`
   - Value: Paste entire JSON content from `firebase-adminsdk.json`
   - Save

3. Grant App Service access:
   - Go to App Service → **Settings** → **Identity**
   - **System assigned** tab → Turn **Status** to **On** → Save
   - Copy the **Object (principal) ID**

4. Add Access Policy in Key Vault:
   - Go to Key Vault → **Access policies**
   - Click **+ Create**
   - Permissions: Select **Get** and **List** for Secrets
   - Principal: Search for your App Service name
   - Save

5. Update App Service Configuration:
   - Add setting: `Firebase__CredentialPath` = `@Microsoft.KeyVault(SecretUri=https://preschool-keyvault.vault.azure.net/secrets/firebase-adminsdk/)`

**Option B: Using App Service Advanced Tools (Kudu) - Simpler for Development**

1. Go to App Service → **Development Tools** → **Advanced Tools**
2. Click **Go →** (opens Kudu dashboard)
3. Navigate to **Debug console** → **CMD**
4. Navigate to: `site\wwwroot`
5. Drag and drop `firebase-adminsdk.json` file to upload
6. In App Service Configuration:
   - Add setting: `Firebase__CredentialPath` = `firebase-adminsdk.json`

---

### **Phase 4: Deploy Your Application**

#### Method 1: Deploy from Visual Studio (Easiest)

1. **Open Visual Studio**
2. Right-click `PreschoolEnrollmentSystem.API` project
3. Select **Publish**
4. Click **Add a publish profile**
5. Choose **Azure** → **Next**
6. Choose **Azure App Service (Linux)** → **Next**
7. Sign in to Azure account
8. Select:
   - Subscription: Your subscription
   - Resource Group: `preschool-enrollment-rg`
   - App Service: `preschool-enrollment-api`
9. Click **Finish**
10. Click **Publish** button
11. Wait for deployment (2-5 minutes)

#### Method 2: Deploy using Azure CLI

```bash
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Navigate to API project
cd PreschoolEnrollmentSystem.API

# Build and publish
dotnet publish -c Release -o ./publish

# Create ZIP file
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group preschool-enrollment-rg \
  --name preschool-enrollment-api \
  --src deploy.zip

# Clean up
rm deploy.zip
```

#### Method 3: Deploy using GitHub Actions (CI/CD)

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Publish
      run: dotnet publish PreschoolEnrollmentSystem.API/PreschoolEnrollmentSystem.API.csproj -c Release -o ./publish

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'preschool-enrollment-api'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

**Setup GitHub Actions:**

1. Go to Azure Portal → App Service
2. Click **Get publish profile** (downloads a file)
3. Go to GitHub repository → **Settings** → **Secrets and variables** → **Actions**
4. Click **New repository secret**
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Value: Paste contents of downloaded publish profile
5. Push to main branch to trigger deployment

---

### **Phase 5: Run Database Migrations**

#### Option A: Using Azure Cloud Shell

1. Go to Azure Portal
2. Click **Cloud Shell** icon (top right, `>_` icon)
3. Choose **Bash**
4. Run migrations:

```bash
# Install Entity Framework tools
dotnet tool install --global dotnet-ef

# Clone your repository
git clone https://github.com/Minhdat04/Pre-School-Enrollment.git
cd Pre-School-Enrollment

# Checkout your branch
git checkout main

# Update connection string in appsettings.json temporarily
cd PreschoolEnrollmentSystem.API

# Run migrations
CONNECTION_STRING="Server=preschool-mysql-server.mysql.database.azure.com;Port=3306;Database=PreschoolEnrollmentDB;Uid=preschooladmin;Pwd=YOUR_PASSWORD;SslMode=Required;" \
dotnet ef database update --project ../PreschoolEnrollmentSystem.Infrastructure/PreschoolEnrollmentSystem.Infrastructure.csproj
```

#### Option B: Using Local Machine

1. Update connection string locally in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=preschool-mysql-server.mysql.database.azure.com;Port=3306;Database=PreschoolEnrollmentDB;Uid=preschooladmin;Pwd=YOUR_PASSWORD;SslMode=Required;"
}
```

2. Run migrations:
```bash
cd PreschoolEnrollmentSystem.API
dotnet ef database update --project ../PreschoolEnrollmentSystem.Infrastructure/PreschoolEnrollmentSystem.Infrastructure.csproj
```

3. **DON'T COMMIT** the connection string change - revert it.

---

### **Phase 6: Verify Deployment**

#### Step 6.1: Check App Service Health

1. Go to Azure Portal → App Service
2. Click **Browse** (opens your API URL)
3. You should see a response (might be 404 - that's normal)
4. Try: `https://preschool-enrollment-api.azurewebsites.net/swagger`
   - Should show Swagger UI

#### Step 6.2: Check Application Logs

1. Go to App Service → **Monitoring** → **Log stream**
2. Watch for any errors during startup
3. Common issues:
   - Database connection errors → Check connection string
   - Firebase errors → Check firebase-adminsdk.json is uploaded
   - Missing configuration → Check Application Settings

#### Step 6.3: Test API Endpoints

Using Postman or curl:

```bash
# Test health endpoint (if you have one)
curl https://preschool-enrollment-api.azurewebsites.net/health

# Test seed status
curl https://preschool-enrollment-api.azurewebsites.net/api/seed/status

# Test user registration
curl -X POST https://preschool-enrollment-api.azurewebsites.net/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "confirmPassword": "Test123!@#",
    "firstName": "Test",
    "lastName": "User",
    "phoneNumber": "+84901234567",
    "role": "Parent",
    "acceptTerms": true
  }'
```

---

### **Phase 7: Configure Custom Domain (Optional)**

If you have a custom domain:

1. Go to App Service → **Settings** → **Custom domains**
2. Click **+ Add custom domain**
3. Enter your domain: `api.yourschool.com`
4. Follow instructions to add DNS records at your registrar:
   - **CNAME**: Point to `preschool-enrollment-api.azurewebsites.net`
   - **TXT**: For domain verification
5. Wait for DNS propagation (up to 48 hours, usually 5-10 minutes)
6. Click **Validate** → **Add**
7. **Enable HTTPS**: Click **Add binding** → Select **SNI SSL** (free)

---

### **Phase 8: Enable Monitoring & Alerts**

#### Set up Application Insights

1. Go to Application Insights resource (`preschool-api-insights`)
2. Review:
   - **Live Metrics**: Real-time performance
   - **Failures**: Track errors
   - **Performance**: Response times
   - **Availability**: Set up ping tests

#### Create Alerts

1. Go to Application Insights → **Alerts**
2. Click **+ Create** → **Alert rule**
3. Create alerts for:
   - **High response time**: Alert when average > 3 seconds
   - **Error rate**: Alert when failures > 5%
   - **Server errors**: Alert when HTTP 5xx > 10 requests
   - **Low availability**: Alert when availability < 99%

---

## 🔧 Post-Deployment Configuration

### Update CORS Settings

In App Service Configuration, update:
```
Cors__AllowedOrigins__0 = https://yourmobileapp.com
Cors__AllowedOrigins__1 = https://admin.yourschool.com
```

### Enable Automatic Scaling (Production)

1. Go to App Service Plan
2. Navigate to **Settings** → **Scale out (App Service plan)**
3. Click **Custom autoscale**
4. Add rule:
   - Scale out: When CPU > 70% for 5 minutes
   - Scale in: When CPU < 30% for 10 minutes
   - Instance limits: Min 1, Max 5

### Enable Backup

1. Go to App Service → **Settings** → **Backups**
2. Configure storage account
3. Set backup schedule: Daily
4. Retention: 30 days

---

## 📊 Cost Estimation

### Development/Testing Environment
- **App Service**: Basic B1 (~$13/month)
- **MySQL**: Burstable B1ms (~$12/month)
- **Application Insights**: Free tier (1 GB/month)
- **Storage**: Minimal (<$1/month)
- **Total**: ~$26/month

### Production Environment
- **App Service**: Standard S1 (~$70/month)
- **MySQL**: General Purpose (2 vCores) (~$100/month)
- **Application Insights**: Standard (~$2.30/GB after free tier)
- **Storage**: ~$5/month
- **Total**: ~$177/month (with moderate usage)

**Ways to reduce costs:**
- Use Azure free trial ($200 credit for 30 days)
- Use Azure for Students (if eligible - $100 credit)
- Start with Basic tier, upgrade as needed
- Enable auto-shutdown for dev environments
- Use reserved instances (1-3 year commitment for 30-50% savings)

---

## 🔒 Security Checklist

Before going to production:

- [ ] All secrets in Azure Configuration (not in code)
- [ ] Firebase admin SDK not committed to repository
- [ ] HTTPS enforced (HTTP redirects to HTTPS)
- [ ] Database firewall configured (only Azure services)
- [ ] CORS configured for specific origins (not *)
- [ ] API rate limiting implemented
- [ ] Application Insights monitoring enabled
- [ ] Backup strategy configured
- [ ] Custom domain with SSL certificate
- [ ] Security headers configured
- [ ] SQL injection protection verified
- [ ] XSS protection enabled
- [ ] Authentication tested thoroughly

---

## 🐛 Troubleshooting Common Issues

### Issue 1: 500 Internal Server Error after deployment

**Causes:**
- Missing configuration
- Database connection failure
- Firebase credentials not found

**Solution:**
```bash
# Check logs
az webapp log tail --name preschool-enrollment-api --resource-group preschool-enrollment-rg

# Verify configuration
az webapp config appsettings list --name preschool-enrollment-api --resource-group preschool-enrollment-rg
```

### Issue 2: Database Connection Timeout

**Solution:**
1. Check MySQL firewall rules allow Azure services
2. Verify connection string format
3. Test connection from Cloud Shell:
```bash
mysql -h preschool-mysql-server.mysql.database.azure.com -u preschooladmin -p
```

### Issue 3: Firebase Authentication Errors

**Solution:**
1. Verify firebase-adminsdk.json is uploaded
2. Check Firebase__CredentialPath points to correct location
3. Restart App Service after uploading credentials

### Issue 4: High Memory Usage

**Solution:**
1. Check for memory leaks in Application Insights
2. Upgrade App Service Plan if needed
3. Enable garbage collection logging:
```bash
az webapp config appsettings set --name preschool-enrollment-api \
  --resource-group preschool-enrollment-rg \
  --settings COMPlus_gcServer=1
```

### Issue 5: Slow Response Times

**Solution:**
1. Enable response compression in Program.cs
2. Add output caching for static endpoints
3. Consider Azure CDN for API caching
4. Check database performance in MySQL metrics

---

## 📚 Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure MySQL Documentation](https://docs.microsoft.com/azure/mysql/)
- [ASP.NET Core Deployment Guide](https://docs.microsoft.com/aspnet/core/host-and-deploy/)
- [Azure Security Best Practices](https://docs.microsoft.com/azure/security/fundamentals/best-practices-and-patterns)

---

## 🎉 Deployment Complete!

Once deployed, your API will be accessible at:
- **API URL**: `https://preschool-enrollment-api.azurewebsites.net`
- **Swagger**: `https://preschool-enrollment-api.azurewebsites.net/swagger`
- **Health**: `https://preschool-enrollment-api.azurewebsites.net/api/seed/status`

Share these URLs with your mobile app team for integration!

---

## Next Steps After Deployment

1. ✅ Seed database with test data (if needed)
2. ✅ Test all API endpoints
3. ✅ Configure monitoring alerts
4. ✅ Set up daily backups
5. ✅ Document API URLs for mobile team
6. ✅ Plan staging environment
7. ✅ Set up CI/CD pipeline with GitHub Actions
