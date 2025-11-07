# Mailgun Email Service Setup Guide

Complete guide to setting up Mailgun for your Preschool Enrollment System.

## Why Mailgun?

- **Free Tier**: 5,000 emails per month
- **Excellent Deliverability**: Industry-leading email delivery rates
- **Developer-Friendly**: Simple SMTP and API integration
- **Real-time Analytics**: Track email opens, clicks, and deliveries
- **No Credit Card Required**: For sandbox domain testing

## Step 1: Create Mailgun Account

1. Go to https://signup.mailgun.com/new/signup
2. Fill in your details:
   - Email address
   - Password
   - Company name (can use "Preschool Enrollment System")
3. Click **Create Account**
4. Verify your email address (check inbox for verification email)

## Step 2: Access Mailgun Dashboard

1. Log in to https://app.mailgun.com/
2. You'll see your dashboard with a **Sandbox Domain** already created
3. The sandbox domain looks like: `sandboxXXXXXXXXXX.mailgun.org`

## Step 3: Get SMTP Credentials

### Option A: Using Sandbox Domain (Recommended for Testing)

**Advantages:**
- Already set up and ready to use
- No domain verification needed
- Perfect for development and testing

**Limitations:**
- Can only send to authorized recipients (you must add email addresses manually)
- "via mailgun.org" appears in email headers

**Steps:**
1. In Mailgun dashboard, go to **Sending** → **Domain settings**
2. Select your sandbox domain (e.g., `sandbox123abc.mailgun.org`)
3. Click on **SMTP credentials** tab
4. You'll see:
   ```
   SMTP hostname: smtp.mailgun.org
   Port: 587 (or 465 for SSL)
   Username: postmaster@sandbox123abc.mailgun.org
   Password: Click "Reset Password" to generate
   ```
5. Click **Reset Password** to generate a new SMTP password
6. **IMPORTANT:** Copy the password immediately (you won't see it again!)

### Option B: Using Custom Domain (For Production)

**Advantages:**
- Professional email appearance
- No sending restrictions
- Better deliverability

**Requirements:**
- You must own a domain (e.g., preschoolenrollment.com)
- Access to domain DNS settings

**Steps:**
1. Click **Add New Domain** button
2. Enter your domain name
3. Select domain region (US or EU)
4. Follow DNS verification steps:
   - Add TXT records for domain verification
   - Add MX records for receiving emails
   - Add CNAME records for tracking
5. Wait for DNS propagation (can take up to 48 hours)
6. Once verified, get SMTP credentials same as sandbox

## Step 4: Authorize Recipients (Sandbox Domain Only)

If using sandbox domain, you MUST authorize email addresses that can receive emails:

1. Go to **Sending** → **Domain settings** → **Authorized Recipients**
2. Click **Add Recipient**
3. Enter the email address you want to test with
4. Click **Save**
5. The recipient will receive a verification email
6. They must click the link to confirm
7. Repeat for all test email addresses

**Example authorized recipients:**
- Your personal email
- Test accounts
- Team members' emails

## Step 5: Get Your Credentials

After completing the steps above, you'll have:

```
SMTP Host: smtp.mailgun.org
SMTP Port: 587
Username: postmaster@sandboxXXXXXXXXXX.mailgun.org
Password: [your-generated-password]
From Email: noreply@sandboxXXXXXXXXXX.mailgun.org
```

## Step 6: Update Your Configuration

Open `PreschoolEnrollmentSystem.API/appsettings.json` and update:

```json
{
  "Email": {
    "SmtpHost": "smtp.mailgun.org",
    "SmtpPort": "587",
    "Username": "postmaster@sandbox123abc.mailgun.org",
    "Password": "your-mailgun-smtp-password",
    "FromEmail": "Preschool Enrollment <noreply@sandbox123abc.mailgun.org>"
  }
}
```

**Replace:**
- `sandbox123abc.mailgun.org` with your actual sandbox domain
- `your-mailgun-smtp-password` with the password you generated

## Step 7: Test Your Configuration

1. Restart your API server
2. Register a new user with an **authorized email address**
3. Check Mailgun dashboard → **Logs** to see if email was sent
4. Check your email inbox for the verification email

## Troubleshooting

### Error: "SMTP authentication failed"
- **Cause**: Wrong username or password
- **Solution**: Reset your SMTP password in Mailgun dashboard and update config

### Error: "Recipient not authorized"
- **Cause**: Email address not in authorized recipients list (sandbox domain)
- **Solution**: Add the email to authorized recipients and verify

### Emails not arriving
- **Check**: Mailgun Logs (Sending → Logs)
- **Look for**: "Delivered" status
- **If bounced**: Check spam folder or email address validity

### "Free account sending limit exceeded"
- **Cause**: Sent more than 5,000 emails this month
- **Solution**: Wait for next month or upgrade to paid plan

## Mailgun Dashboard Features

### Logs
- View all sent emails
- See delivery status (delivered, failed, bounced)
- Click on individual emails for details

### Analytics
- Email delivery rates
- Open rates (if tracking enabled)
- Click rates (if tracking enabled)

### Suppressions
- **Bounces**: Invalid email addresses
- **Unsubscribes**: Users who opted out
- **Complaints**: Spam reports

## Production Recommendations

When moving to production:

1. **Use Custom Domain**
   - Better branding and trust
   - No recipient restrictions
   - Professional appearance

2. **Enable DKIM Signing**
   - Improves email deliverability
   - Prevents spoofing
   - Already configured by Mailgun

3. **Monitor Bounce Rate**
   - Keep below 5%
   - Remove invalid emails regularly

4. **Set Up Webhooks**
   - Get real-time delivery notifications
   - Track bounces and complaints
   - Update user email status automatically

5. **Upgrade if Needed**
   - Foundation plan: $35/month for 50,000 emails
   - More detailed analytics
   - Better support

## Alternative: Mailgun API (Optional)

Instead of SMTP, you can use Mailgun's REST API:

**Advantages:**
- Faster sending
- More features (templates, tracking, etc.)
- Better error handling

**Implementation:**
- Install Mailgun NuGet package
- Use API key instead of SMTP
- Modify EmailService to use API calls

## Cost Breakdown

**Free Tier:**
- 5,000 emails/month
- Sandbox domain included
- Basic analytics
- Email support

**Paid Plans:**
- Start at $35/month
- Up to 50,000 emails
- Custom domains
- Advanced features
- Priority support

## Security Best Practices

1. **Never commit credentials**
   - Add `appsettings.json` to `.gitignore`
   - Use environment variables in production

2. **Use User Secrets** (Development)
   ```bash
   dotnet user-secrets set "Email:Password" "your-password"
   ```

3. **Rotate passwords regularly**
   - Change SMTP password every 90 days
   - Update in all environments

## Support & Documentation

- **Mailgun Docs**: https://documentation.mailgun.com/
- **SMTP Guide**: https://documentation.mailgun.com/en/latest/user_manual.html#sending-via-smtp
- **Support**: https://app.mailgun.com/support
- **Status Page**: https://status.mailgun.com/

## Quick Commands

### Check if SMTP is working (PowerShell)
```powershell
Test-NetConnection smtp.mailgun.org -Port 587
```

### Send test email (curl)
```bash
curl -s --user 'api:YOUR_API_KEY' \
    https://api.mailgun.net/v3/YOUR_DOMAIN_NAME/messages \
    -F from='noreply@YOUR_DOMAIN' \
    -F to='test@example.com' \
    -F subject='Test Email' \
    -F text='This is a test email'
```

---

**Next Steps:**
1. Complete Mailgun setup following this guide
2. Update `appsettings.json` with your credentials
3. Restart your API server
4. Refer to `SWAGGER_TESTING_GUIDE.md` to test the endpoints
