$uri = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/oauth/token"
$body = @{
    client_id     = "h4CjtEYT0HiXwJVr3JkOSkJnr1aq3bHc"
    client_secret = "6Si0dfpi89xei4GGGcxblXIb2dc6r8RpfLqPAqaleN_sy3c6PmSLbrTfDAfm_sLm"
    audience      = "https://saas-api/"
    grant_type    = "client_credentials"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri $uri -Method Post -ContentType "application/json" -Body $body

Write-Host "=== AUTH0 TOKEN RECEIVED ===" -ForegroundColor Green
Write-Host "Token Type: $($response.token_type)"
Write-Host "Expires In: $($response.expires_in) seconds"
Write-Host ""
Write-Host "ACCESS_TOKEN:" -ForegroundColor Cyan
Write-Host $response.access_token

# Save to file for other scripts
$response.access_token | Out-File -FilePath "auth0-token.txt" -NoNewline
Write-Host ""
Write-Host "Token saved to auth0-token.txt" -ForegroundColor Yellow
