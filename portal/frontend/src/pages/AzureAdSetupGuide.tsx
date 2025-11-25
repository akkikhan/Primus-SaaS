import React from 'react';
import { Container, Typography, Box, Paper, Stepper, Step, StepLabel, StepContent, Button, Alert } from '@mui/material';
import { OpenInNew } from '@mui/icons-material';

const AzureAdSetupGuide = () => {
    const [activeStep, setActiveStep] = React.useState(0);

    const steps = [
        {
            label: 'Go to Azure Portal',
            description: (
                <Box>
                    <Typography variant="body2" paragraph>
                        Open the Azure Portal and navigate to Azure Active Directory.
                    </Typography>
                    <Button
                        variant="outlined"
                        startIcon={<OpenInNew />}
                        href="https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/Overview"
                        target="_blank"
                        size="small"
                    >
                        Open Azure Portal
                    </Button>
                </Box>
            ),
        },
        {
            label: 'Register New Application',
            description: (
                <Box>
                    <Typography variant="body2" paragraph>
                        1. Click on <strong>"App registrations"</strong> in the left sidebar
                    </Typography>
                    <Typography variant="body2" paragraph>
                        2. Click <strong>"+ New registration"</strong>
                    </Typography>
                    <Typography variant="body2" paragraph>
                        3. Fill in the registration form:
                    </Typography>
                    <Paper variant="outlined" sx={{ p: 2, mt: 1, bgcolor: '#f5f5f5' }}>
                        <Typography variant="body2" component="div">
                            • <strong>Name</strong>: Your application name (e.g., "Acme Dashboard API")
                        </Typography>
                        <Typography variant="body2" component="div">
                            • <strong>Supported account types</strong>: "Accounts in this organizational directory only" (Single tenant)
                        </Typography>
                        <Typography variant="body2" component="div">
                            • <strong>Redirect URI</strong>: Select "Single-page application (SPA)" and enter:
                            <br />
                            <code style={{ background: '#e0e0e0', padding: '2px 6px', borderRadius: '3px' }}>
                                http://localhost:3000
                            </code>
                        </Typography>
                    </Paper>
                    <Typography variant="body2" sx={{ mt: 2 }}>
                        4. Click <strong>"Register"</strong>
                    </Typography>
                </Box>
            ),
        },
        {
            label: 'Get Your Credentials',
            description: (
                <Box>
                    <Typography variant="body2" paragraph>
                        After registration, you'll see the application overview page. Copy these values:
                    </Typography>
                    <Paper variant="outlined" sx={{ p: 2, bgcolor: '#f5f5f5' }}>
                        <Typography variant="body2" component="div" gutterBottom>
                            <strong>Application (client) ID</strong>
                            <br />
                            <code style={{ background: '#e0e0e0', padding: '4px 8px', borderRadius: '3px', fontSize: '0.875rem' }}>
                                xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
                            </code>
                        </Typography>
                        <Typography variant="body2" component="div" sx={{ mt: 2 }}>
                            <strong>Directory (tenant) ID</strong>
                            <br />
                            <code style={{ background: '#e0e0e0', padding: '4px 8px', borderRadius: '3px', fontSize: '0.875rem' }}>
                                xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
                            </code>
                        </Typography>
                    </Paper>
                    <Alert severity="info" sx={{ mt: 2 }}>
                        <strong>Save these values!</strong> You'll need them to configure your application.
                    </Alert>
                </Box>
            ),
        },
        {
            label: 'Configure Authentication',
            description: (
                <Box>
                    <Typography variant="body2" paragraph>
                        1. In the left sidebar, click <strong>"Authentication"</strong>
                    </Typography>
                    <Typography variant="body2" paragraph>
                        2. Under "Implicit grant and hybrid flows", enable:
                    </Typography>
                    <Paper variant="outlined" sx={{ p: 2, bgcolor: '#f5f5f5' }}>
                        <Typography variant="body2" component="div">
                            ☑️ <strong>ID tokens</strong> (used for sign-in)
                        </Typography>
                        <Typography variant="body2" component="div">
                            ☑️ <strong>Access tokens</strong> (used for API calls)
                        </Typography>
                    </Paper>
                    <Typography variant="body2" sx={{ mt: 2 }}>
                        3. Add redirect URIs for all environments:
                    </Typography>
                    <Paper variant="outlined" sx={{ p: 2, mt: 1, bgcolor: '#f5f5f5' }}>
                        <Typography variant="body2" component="div">
                            • Development: <code>http://localhost:3000</code>
                        </Typography>
                        <Typography variant="body2" component="div">
                            • Production: <code>https://yourdomain.com</code>
                        </Typography>
                    </Paper>
                    <Typography variant="body2" sx={{ mt: 2 }}>
                        4. Click <strong>"Save"</strong>
                    </Typography>
                </Box>
            ),
        },
        {
            label: 'Expose an API (Optional)',
            description: (
                <Box>
                    <Typography variant="body2" paragraph>
                        If your backend API needs to validate tokens:
                    </Typography>
                    <Typography variant="body2" paragraph>
                        1. Click <strong>"Expose an API"</strong> in the left sidebar
                    </Typography>
                    <Typography variant="body2" paragraph>
                        2. Click <strong>"+ Add a scope"</strong>
                    </Typography>
                    <Typography variant="body2" paragraph>
                        3. Set Application ID URI (or use default):
                    </Typography>
                    <Paper variant="outlined" sx={{ p: 2, bgcolor: '#f5f5f5' }}>
                        <code style={{ fontSize: '0.875rem' }}>
                            api://YOUR-CLIENT-ID
                        </code>
                    </Paper>
                    <Typography variant="body2" sx={{ mt: 2 }}>
                        4. Add a scope:
                    </Typography>
                    <Paper variant="outlined" sx={{ p: 2, mt: 1, bgcolor: '#f5f5f5' }}>
                        <Typography variant="body2" component="div">
                            • <strong>Scope name</strong>: user_impersonation
                        </Typography>
                        <Typography variant="body2" component="div">
                            • <strong>Who can consent</strong>: Admins and users
                        </Typography>
                        <Typography variant="body2" component="div">
                            • <strong>Display name</strong>: Access API as user
                        </Typography>
                    </Paper>
                    <Typography variant="body2" sx={{ mt: 2 }}>
                        5. Click <strong>"Add scope"</strong>
                    </Typography>
                </Box>
            ),
        },
        {
            label: 'Use in Your Application',
            description: (
                <Box>
                    <Typography variant="body2" paragraph>
                        Now configure your application with the credentials:
                    </Typography>
                    <Alert severity="success" sx={{ mb: 2 }}>
                        <strong>For Frontend (MSAL):</strong>
                    </Alert>
                    <Paper variant="outlined" sx={{ p: 2, bgcolor: '#1e1e1e', color: '#d4d4d4', fontFamily: 'monospace', fontSize: '0.875rem', overflow: 'auto' }}>
                        <pre style={{ margin: 0 }}>{`const msalConfig = {
  auth: {
    clientId: "YOUR-CLIENT-ID",
    authority: "https://login.microsoftonline.com/YOUR-TENANT-ID",
    redirectUri: "http://localhost:3000"
  }
};`}</pre>
                    </Paper>

                    <Alert severity="success" sx={{ mt: 2, mb: 2 }}>
                        <strong>For Backend (Primus SDK):</strong>
                    </Alert>
                    <Paper variant="outlined" sx={{ p: 2, bgcolor: '#1e1e1e', color: '#d4d4d4', fontFamily: 'monospace', fontSize: '0.875rem', overflow: 'auto' }}>
                        <pre style={{ margin: 0 }}>{`const primusAuth = primusIdentityValidator({
  defaultAuthority: "https://login.microsoftonline.com/YOUR-TENANT-ID",
  allowedAudiences: ["api://YOUR-CLIENT-ID"],
  
  // Optional: for portal analytics
  primusTrackingId: "PSP-CLI-XXXXXX"
});`}</pre>
                    </Paper>
                </Box>
            ),
        },
    ];

    const handleNext = () => {
        setActiveStep((prevActiveStep) => prevActiveStep + 1);
    };

    const handleBack = () => {
        setActiveStep((prevActiveStep) => prevActiveStep - 1);
    };

    const handleReset = () => {
        setActiveStep(0);
    };

    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Typography variant="h4" gutterBottom>
                🔐 Azure AD Setup Guide
            </Typography>
            <Typography variant="body1" color="text.secondary" paragraph>
                Follow these steps to register your application in Azure Active Directory and get the credentials needed for authentication.
            </Typography>

            <Alert severity="info" sx={{ mb: 3 }}>
                <strong>Prerequisites:</strong> You need an Azure account with permissions to register applications in Azure AD.
                If you don't have one, <a href="https://azure.microsoft.com/free/" target="_blank" rel="noopener noreferrer">create a free account</a>.
            </Alert>

            <Paper elevation={2} sx={{ p: 3 }}>
                <Stepper activeStep={activeStep} orientation="vertical">
                    {steps.map((step, index) => (
                        <Step key={step.label}>
                            <StepLabel>{step.label}</StepLabel>
                            <StepContent>
                                {step.description}
                                <Box sx={{ mb: 2, mt: 2 }}>
                                    <Button
                                        variant="contained"
                                        onClick={handleNext}
                                        sx={{ mt: 1, mr: 1 }}
                                    >
                                        {index === steps.length - 1 ? 'Finish' : 'Continue'}
                                    </Button>
                                    <Button
                                        disabled={index === 0}
                                        onClick={handleBack}
                                        sx={{ mt: 1, mr: 1 }}
                                    >
                                        Back
                                    </Button>
                                </Box>
                            </StepContent>
                        </Step>
                    ))}
                </Stepper>
                {activeStep === steps.length && (
                    <Paper square elevation={0} sx={{ p: 3, bgcolor: '#f5f5f5' }}>
                        <Typography variant="h6" gutterBottom>
                            ✅ Setup Complete!
                        </Typography>
                        <Typography variant="body2" paragraph>
                            You've successfully configured Azure AD for your application.
                        </Typography>
                        <Typography variant="body2" paragraph>
                            Next steps:
                        </Typography>
                        <ul>
                            <li>
                                <Typography variant="body2">
                                    Configure your frontend with the Client ID and Tenant ID
                                </Typography>
                            </li>
                            <li>
                                <Typography variant="body2">
                                    Configure your backend with the Primus SDK
                                </Typography>
                            </li>
                            <li>
                                <Typography variant="body2">
                                    Test the authentication flow
                                </Typography>
                            </li>
                        </ul>
                        <Button onClick={handleReset} sx={{ mt: 1, mr: 1 }}>
                            Start Over
                        </Button>
                    </Paper>
                )}
            </Paper>

            <Alert severity="warning" sx={{ mt: 3 }}>
                <strong>Important:</strong> Never commit your Client ID or Tenant ID to public repositories.
                Use environment variables to store these values.
            </Alert>
        </Container>
    );
};

export default AzureAdSetupGuide;
