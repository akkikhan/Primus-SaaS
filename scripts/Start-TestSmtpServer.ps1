# Simple SMTP Test Server for local development
# Listens on port 1025 and logs all received emails to console and files

param(
    [int]$Port = 1025,
    [string]$OutputDir = ".\smtp-received"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Primus SaaS - Test SMTP Server" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Listening on port $Port..." -ForegroundColor Green
Write-Host "Emails will be saved to: $OutputDir" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Port)
$listener.Start()

try {
    $emailCount = 0
    while ($true) {
        Write-Host "Waiting for connection..." -ForegroundColor Gray
        $client = $listener.AcceptTcpClient()
        $stream = $client.GetStream()
        $reader = [System.IO.StreamReader]::new($stream)
        $writer = [System.IO.StreamWriter]::new($stream)
        $writer.AutoFlush = $true

        Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] Connection from $($client.Client.RemoteEndPoint)" -ForegroundColor Cyan

        # SMTP Greeting
        $writer.WriteLine("220 localhost Primus Test SMTP Server Ready")

        $from = ""
        $to = @()
        $data = ""
        $inData = $false

        while ($client.Connected -and $stream.DataAvailable -or $client.Connected) {
            try {
                $line = $reader.ReadLine()
                if ($null -eq $line) { break }

                if ($inData) {
                    if ($line -eq ".") {
                        $inData = $false
                        $emailCount++
                        
                        # Save email to file
                        $filename = Join-Path $OutputDir "email_$(Get-Date -Format 'yyyyMMdd_HHmmss')_$emailCount.eml"
                        $data | Out-File -FilePath $filename -Encoding UTF8
                        
                        Write-Host "`n========== EMAIL RECEIVED ==========" -ForegroundColor Green
                        Write-Host "From: $from" -ForegroundColor Yellow
                        Write-Host "To: $($to -join ', ')" -ForegroundColor Yellow
                        Write-Host "Saved to: $filename" -ForegroundColor Gray
                        Write-Host "-------- Content Preview --------" -ForegroundColor Gray
                        $preview = ($data -split "`n" | Select-Object -First 20) -join "`n"
                        Write-Host $preview
                        Write-Host "==================================`n" -ForegroundColor Green
                        
                        $writer.WriteLine("250 OK Message accepted")
                        $data = ""
                    } else {
                        $data += "$line`n"
                    }
                } else {
                    $cmd = $line.ToUpper()
                    
                    if ($cmd.StartsWith("HELO") -or $cmd.StartsWith("EHLO")) {
                        $writer.WriteLine("250-localhost Hello")
                        $writer.WriteLine("250-SIZE 10485760")
                        $writer.WriteLine("250 OK")
                    }
                    elseif ($cmd.StartsWith("MAIL FROM:")) {
                        $from = $line -replace "MAIL FROM:", "" -replace "[<>]", ""
                        $writer.WriteLine("250 OK Sender accepted")
                    }
                    elseif ($cmd.StartsWith("RCPT TO:")) {
                        $recipient = $line -replace "RCPT TO:", "" -replace "[<>]", ""
                        $to += $recipient
                        $writer.WriteLine("250 OK Recipient accepted")
                    }
                    elseif ($cmd -eq "DATA") {
                        $writer.WriteLine("354 Start mail input; end with <CRLF>.<CRLF>")
                        $inData = $true
                    }
                    elseif ($cmd -eq "QUIT") {
                        $writer.WriteLine("221 Bye")
                        break
                    }
                    elseif ($cmd -eq "RSET") {
                        $from = ""
                        $to = @()
                        $data = ""
                        $writer.WriteLine("250 OK Reset")
                    }
                    else {
                        $writer.WriteLine("250 OK")
                    }
                }
            } catch {
                Write-Host "Connection error: $_" -ForegroundColor Red
                break
            }
        }

        $reader.Dispose()
        $writer.Dispose()
        $client.Close()
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Connection closed" -ForegroundColor Gray
    }
} finally {
    $listener.Stop()
    Write-Host "`nSMTP Server stopped" -ForegroundColor Yellow
}
