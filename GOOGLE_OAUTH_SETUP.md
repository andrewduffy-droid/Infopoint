# Google OAuth Configuration

## Required Redirect URIs

Add these URIs to your Google OAuth 2.0 Client ID settings:

### Development
- `http://localhost:5167/signin-google`
- `https://localhost:7215/signin-google`

### Production (when deployed)
- `https://yourdomain.com/signin-google`

## Steps to Configure

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project
3. Navigate to "APIs & Services" → "Credentials"
4. Click on your OAuth 2.0 Client ID
5. In "Authorized redirect URIs" section, add the URIs above
6. Save changes

## Current OAuth Settings in Application

- **Callback Path**: `/signin-google`
- **Client ID**: Configured in appsettings.json
- **Client Secret**: Configured in appsettings.json

## Troubleshooting

If you get `redirect_uri_mismatch` errors:
1. Check that the URI in Google Console matches exactly (including http/https and port)
2. Wait a few minutes after adding new URIs (Google needs time to propagate changes)
3. Clear browser cookies and try again