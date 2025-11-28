# Send Email Test - Quick Start Guide

## 📧 Send a Real Email to akki@primussoft.com

This application uses the Primus Notification Module to send an actual email.

## How to Run

```bash
cd test-apps/SendEmailTest
dotnet run
```

## Configuration Options

### Option 1: Demo Mode (No Email Sent)
Just press Enter when prompted for SMTP host. The notification will be logged to console but no email will be sent.

### Option 2: Send Real Email

When prompted, provide your SMTP credentials:

#### Using Gmail
```
SMTP Host: smtp.gmail.com
SMTP Port: 587
SMTP Username: your-email@gmail.com
SMTP Password: your-app-password
From Email: your-email@gmail.com
```

**Note**: For Gmail, you need to use an [App Password](https://support.google.com/accounts/answer/185833), not your regular password.

#### Using Outlook/Office 365
```
SMTP Host: smtp.office365.com
SMTP Port: 587
SMTP Username: your-email@outlook.com
SMTP Password: your-password
From Email: your-email@outlook.com
```

#### Using SendGrid
```
SMTP Host: smtp.sendgrid.net
SMTP Port: 587
SMTP Username: apikey
SMTP Password: your-sendgrid-api-key
From Email: verified-sender@yourdomain.com
```

## What Gets Sent

The application sends a Welcome Email to **akki@primussoft.com** with:
- **Subject**: "Welcome to Primus, Akki!"
- **Body**: HTML email with personalized greeting
- **Template**: Uses Liquid template from `Templates/Welcome/`

## Expected Output

### Demo Mode
```
═══════════════════════════════════════════════════════════
  PRIMUS NOTIFICATION MODULE - SEND EMAIL TEST
═══════════════════════════════════════════════════════════

📧 SMTP Configuration Required
(Press Enter to use demo mode)

═══════════════════════════════════════════════════════════
  SENDING TEST EMAIL TO akki@primussoft.com
═══════════════════════════════════════════════════════════

📤 Dispatching notification...

info: Primus.Notifications.Channels.LoggerChannel[0]
      📢 [NOTIFICATION] Type: Welcome | Recipient: akki@primussoft.com
      Data: { "Name": "Akki" }

✅ SUCCESS!

⚠️  DEMO MODE: Email was not actually sent (no SMTP configured)
   Check the Logger output above to see the notification data.
```

### Real Email Mode
```
📤 Dispatching notification...

info: Primus.Notifications.Channels.LoggerChannel[0]
      📢 [NOTIFICATION] Type: Welcome | Recipient: akki@primussoft.com

info: Primus.Notifications.Channels.Email.SmtpEmailChannel[0]
      Email Channel: Message prepared
      Subject: Welcome to Primus, Akki!
      To: akki@primussoft.com

✅ SUCCESS!

📧 Email sent successfully to akki@primussoft.com
   Check your inbox!
```

## Troubleshooting

### "Authentication failed"
- Verify your SMTP username and password
- For Gmail, ensure you're using an App Password
- Check if 2FA is enabled (you'll need an App Password)

### "Connection timeout"
- Check your firewall settings
- Verify the SMTP host and port
- Some networks block outbound SMTP connections

### "Template not found"
- Ensure `Templates/Welcome/` directory exists
- Check that EmailSubject.liquid and EmailBody.liquid are present

## Security Note

⚠️ **Never commit SMTP credentials to source control!**

For production use, store credentials in:
- Environment variables
- Azure Key Vault
- AWS Secrets Manager
- Configuration files (excluded from git)

## What This Demonstrates

✅ **Real SMTP Integration**: Actual email delivery via MailKit  
✅ **Template Rendering**: Liquid templates with data binding  
✅ **Multi-Channel**: Both Logger and Email channels working  
✅ **Production Ready**: Same code used in Primus SaaS Portal  
✅ **Secure**: Password input hidden with asterisks  

---

**Ready to send?** Run `dotnet run` and follow the prompts!
